using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Microsoft.Extensions.Hosting;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Extensions;
using PluginActivator.Helpers;
using Microsoft.Extensions.Logging;

namespace PluginActivator
{
    internal class Activator : BackgroundService
    {
        private readonly string _connectionString;
        private readonly string _solutionUniqueName;
        private readonly bool _enablePluginSteps;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<Activator> _logger;

        public Activator(
            ConnectionString connectionString, 
            Solution solution, 
            IHostApplicationLifetime hostApplicationLifetime, 
            ILogger<Activator> logger
            )
        {
            _connectionString = connectionString.ClientSecretConnectionString;
            _solutionUniqueName = solution.SolutionUniqueName;
            _enablePluginSteps = solution.EnablePluginSteps;
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Running PluginActivator...");

                await RunAsync(cancellationToken);

                _logger.LogInformation("PluginActivator finished...");
            }
            catch (OperationCanceledException)
            {
                // Don't throw if the operation was canceled
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error in application.");
                Environment.ExitCode = -1;
                throw;
            }
            finally
            {
                _hostApplicationLifetime.StopApplication();
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connecting to Dynamics 365");
            using (ServiceClient serviceClient = new(_connectionString))
            {
                _logger.LogInformation("Successfully connected");

                _logger.LogInformation($"Retrieving solution components in solution {_solutionUniqueName}...");

                var solutionComponents = await GetComponentsInSolutionAsync(serviceClient, _solutionUniqueName, cancellationToken);

                _logger.LogInformation($"Solution components retrieved.");

                var pluginSteps = GetPluginStepsInSolution(solutionComponents);

                if (pluginSteps != null)
                {
                    foreach (var pluginStep in pluginSteps)
                    {
                        // Get the GUID of this plugin step from it's objectid property in the solution component entity
                        if (Guid.TryParse(pluginStep.Attributes[SolutionComponent.ObjectId].ToString(), out Guid pluginId))
                        {
                            ChangePluginStepState(serviceClient, pluginId, doEnable: _enablePluginSteps);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the plugin steps from the <see cref="EntityCollection"/>. This expects that the <see cref="EntityCollection"/> is a collection of solution components.
        /// </summary>
        /// <param name="solutionComponents">The <see cref="EntityCollection"/> containing the solution components.</param>
        /// <returns>An <see cref="IEnumerable{Entity}"/> of only the plugin steps in the given <see cref="EntityCollection"/>, where T is an <see cref="Entity"/>.</returns>
        private static IEnumerable<Entity> GetPluginStepsInSolution(EntityCollection solutionComponents)
        {
            var pluginSteps = solutionComponents.Entities.Where(x =>
            {
                var optionSet = (OptionSetValue)x[SolutionComponent.ComponentType];
                if (optionSet != null)
                {
                    // 92 represents an SDK Message Processing Step, i.e., a Plugin Step
                    // See https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
                    return optionSet.Value == 92;
                }

                return false;
            });

            return pluginSteps;
        }

        /// <summary>
        /// Gets an <see cref="EntityCollection"/> of all the components within the solution with the given unique name.
        /// </summary>
        /// <param name="serviceClient">The service client object connected to the relevant organization instance.</param>
        /// <param name="solutionUniqueName">The unique name of the solution to return the components for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<EntityCollection> GetComponentsInSolutionAsync(ServiceClient serviceClient, string solutionUniqueName, CancellationToken cancellationToken)
        {
            var query = new QueryExpression(SolutionComponent.EntityName)
            {
                // To make use of the components in the solution, we return the ComponentType and ObjectId in the columns,
                // so that we know what each component is, and what it's unqiue Id (GUID) is
                ColumnSet = new ColumnSet(SolutionComponent.SolutionId, SolutionComponent.ComponentType, SolutionComponent.ObjectId)
            };

            // Here we add the criteria that means we only return the components in the given solution
            var condition = new ConditionExpression(SolutionComponent.SolutionIdName, ConditionOperator.Equal, solutionUniqueName);
            var filter = new FilterExpression();
            filter.AddCondition(condition);

            query.Criteria = filter;

            return await serviceClient.RetrieveMultipleAsync(query, cancellationToken);
        }

        /// <summary>
        /// Enables/disables a plugin step with the given Id. 
        /// </summary>
        /// <param name="serviceClient">The service client object connected to the relevant organization instance.</param>
        /// <param name="pluginStepId">The Id of the plugin step to enable/disable.</param>
        /// <param name="doEnable">Whether to enable the plugin step. Passing true means the plugin step will be enabled. Passing false means the plugin step will be disabled.</param>
        /// <returns>A <see cref="bool"/> indicating whether the status was updated successfully (true) or not (false).</returns>
        private bool ChangePluginStepState(ServiceClient serviceClient, Guid pluginStepId, bool doEnable = true)
        {
            // State Codes: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sdkmessageprocessingstep#BKMK_StateCode
            // 0 = Enabled
            // 1 = Disabled
            int stateCode = doEnable ? 0 : 1;


            // Status Codes: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sdkmessageprocessingstep#BKMK_StatusCode
            // 1 = Enabled
            // 2 = Disabled
            int statusCode = doEnable ? 1 : 2;

            _logger.LogInformation($"Updating plugin step with Id {pluginStepId} to be {(doEnable ? "enabled" : "disabled")}...");

            bool result = serviceClient.UpdateStateAndStatusForEntity(PluginStep.EntityName, pluginStepId, stateCode, statusCode);

            if (result == true)
            {
                _logger.LogInformation($"Successfuly updated plugin step with Id {pluginStepId} to be {(doEnable ? "enabled" : "disabled")}");
            }
            else
            {
                _logger.LogError($"Error updating plugin step with Id {pluginStepId} to be {(doEnable ? "enabled" : "disabled")}");
            }

            return result;
        }
    }
}
