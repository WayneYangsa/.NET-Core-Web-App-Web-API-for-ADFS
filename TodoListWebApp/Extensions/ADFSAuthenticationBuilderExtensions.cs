using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;
using TodoListWebApp;

namespace Microsoft.AspNetCore.Authentication
{
    public static class ADFSAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddADFS(this AuthenticationBuilder builder)
            => builder.AddADFS(_ => { });

        public static AuthenticationBuilder AddADFS(this AuthenticationBuilder builder, Action<ADFSOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureADFSOptions>();
            builder.AddOpenIdConnect();
            return builder;
        }

        private class ConfigureADFSOptions : IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly ADFSOptions _adfsOptions;

            public ConfigureADFSOptions(IOptions<ADFSOptions> adfsOptions)
            {
                _adfsOptions = adfsOptions.Value;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                options.ClientId = _adfsOptions.ClientId;
                options.MetadataAddress = _adfsOptions.ADFSDiscoveryDoc;
                options.UseTokenLifetime = true;
                options.CallbackPath = _adfsOptions.CallbackPath;
                options.RequireHttpsMetadata = false;
                options.ClientSecret = _adfsOptions.ClientSecret;
                options.Resource = _adfsOptions.TodoListResourceId; // WebAPI Resource Identifier

                // Without overriding the response type (which by default is id_token), the OnAuthorizationCodeReceived event is not called.
                // but instead OnTokenValidated event is called. Here we request both so that OnTokenValidated is called first which 
                // ensures that context.Principal has a non-null value when OnAuthorizeationCodeReceived is called
                options.ResponseType = "id_token code";

                // Subscribing to the OIDC events
                options.Events.OnAuthorizationCodeReceived = OnAuthorizationCodeReceived;
                options.Events.OnAuthenticationFailed = OnAuthenticationFailed;
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }

            /// <summary>
            /// Redeems the authorization code by calling AcquireTokenByAuthorizationCodeAsync in order to ensure
            /// that the cache has a token for the signed-in user, which will then enable the controllers (like the
            /// TodoController, to call AcquireTokenSilentAsync successfully.
            /// </summary>
            private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
            {
                // Acquire a Token for the Graph API and cache it using ADAL. In the TodoListController, we'll use the cache to acquire a token for the Todo List API
                string NameIdentifier = (context.Principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"))?.Value;
                var authContext = new AuthenticationContext(context.Options.MetadataAddress,false, new NaiveSessionCache(NameIdentifier, context.HttpContext.Session));
                var credential = new ClientCredential(context.Options.ClientId, context.Options.ClientSecret);


                var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(context.TokenEndpointRequest.Code,
                    new Uri(context.TokenEndpointRequest.RedirectUri, UriKind.RelativeOrAbsolute), credential, context.Options.Resource);

                // Notify the OIDC middleware that we already took care of code redemption.
                context.HandleCodeRedemption(authResult.AccessToken, context.ProtocolMessage.IdToken);
            }

            /// <summary>
            /// this method is invoked if exceptions are thrown during request processing
            /// </summary>
            private Task OnAuthenticationFailed(AuthenticationFailedContext context)
            {
                context.HandleResponse();
                context.Response.Redirect("/Home/Error?message=" + context.Exception.Message);
                return Task.FromResult(0);
            }
        }
    }
}
