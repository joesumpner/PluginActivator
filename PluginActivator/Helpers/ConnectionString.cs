using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginActivator.Helpers
{
    internal class ConnectionString
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _dynamicsUrl;

        /// <summary>
        /// The connection string to Dynamics 365 using client secret authentication.
        /// </summary>
        public string ClientSecretConnectionString 
        { 
            get
            {
                return $"AuthType=ClientSecret;ClientId={_clientId};ClientSecret={_clientSecret};url={_dynamicsUrl}";
            }
        }

        public ConnectionString(string clientId, string clientSecret, string dynamicsUrl)
        {
            _clientId= clientId;
            _clientSecret= clientSecret;
            _dynamicsUrl= dynamicsUrl;
        }
    }
}
