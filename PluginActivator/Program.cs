using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PluginActivator.Helpers;

namespace PluginActivator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateBuilder(args).Build().RunAsync();           
        }

        private static IHostBuilder CreateBuilder(string[] arguments)
        {
            return Host.CreateDefaultBuilder(arguments)
                       .ConfigureAppConfiguration(ConfigureConfiguration)
                       .ConfigureServices(ConfigureServices);
        }

        /// <summary>
        /// Sets up the configuration in the application. This implementation allows for UserSecrets and environment variables (from the default builder).
        /// </summary>
        /// <param name="builder"></param>
        private static void ConfigureConfiguration(IConfigurationBuilder builder)
        {
            builder.AddUserSecrets(typeof(Program).Assembly);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(GetConnectionStringParameters)
                    .AddSingleton(GetSolutionParameter)
                    .AddHostedService<Activator>();
        }

        private static ConnectionString GetConnectionStringParameters(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            string clientId = configuration.GetValue<string>("CLIENT_ID");
            string clientSecret = configuration.GetValue<string>("CLIENT_SECRET");
            string dynamicsUrl = configuration.GetValue<string>("DYNAMICS_URL");

            return new ConnectionString(clientId, clientSecret, dynamicsUrl);
        }

        private static Solution GetSolutionParameter(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            string solutionName = configuration.GetValue<string>("SOLUTION_UNIQUE_NAME");
            if (string.IsNullOrEmpty(solutionName))
            {
                throw new InvalidOperationException("A parameter with the name SOLUTION_UNIQUE_NAME must be provided as an environment variable or in UserSecrets.");
            }

            bool enablePluginSteps = configuration.GetValue<bool>("ENABLE_PLUGIN_STEPS");

            return new Solution(solutionName, enablePluginSteps);
        }
    }
}