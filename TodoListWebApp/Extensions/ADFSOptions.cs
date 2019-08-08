namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Settings relative to the ADFS applications involved in this Web Application
    /// These are deserialized from the ADFS section of the appsettings.json file
    /// </summary>
    public class ADFSOptions
    {
        /// <summary>
        /// ClientId (Application Id) of this Web Application
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client Secret (Application password) added in the ADFS in the Keys section for the application
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// URL on which this Web App will be called back by ADFS (normally "/signin-oidc")
        /// </summary>
        public string CallbackPath { get; set; }

        /// <summary>
        /// OIDC metadata delivering the token for your ADFS
        /// </summary>
        public string ADFSDiscoveryDoc { get; set; }


        /// <summary>
        /// Client Id (Web API identifier) of the TodoListService, obtained from the ADFS Application Group
        /// </summary>
        public string TodoListResourceId { get; set; }

        /// <summary>
        /// Base URL of the TodoListService
        /// </summary>
        public string TodoListBaseAddress { get; set; }

        /// <summary>
        /// Instance of the settings for this Web application (to be used in controllers)
        /// </summary>
        public static ADFSOptions Settings { set; get; }
    }
}
