using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Authentication
{
    public static class ADFSServiceCollectionExtensions
    {
        public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder)
            => builder.AddAzureAdBearer(_ => { });

        public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder, Action<ADFSOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureAzureOptions>();
            builder.AddJwtBearer();
            return builder;
        }

        private class ConfigureAzureOptions: IConfigureNamedOptions<JwtBearerOptions>
        {
            private readonly ADFSOptions _adfsOptions;

            public ConfigureAzureOptions(IOptions<ADFSOptions> adfsOptions)
            {
                _adfsOptions = adfsOptions.Value;
            }

            public void Configure(string name, JwtBearerOptions options)
            {
                options.Audience = _adfsOptions.ClientId; //Set the audiance value to be the Web API identifier registered in ADFS.
                options.Authority = _adfsOptions.Authority; //Set the Authority URL to be the ADFS OIDC metadata endpoint 
                options.TokenValidationParameters = new IdentityModel.Tokens.TokenValidationParameters() { SaveSigninToken = true, ValidateIssuer = false }; // Set not to validate the issuer url as the ADFS access_token issuer is with "/trust"
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = AuthenticationFailed,
                    OnTokenValidated = context =>
                    {
                        var claimsidentity = new ClaimsIdentity(context.Principal.Claims); // let you check the cliams in the access_token sent to this Web API
                        string upn = string.Format(claimsidentity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn").Value); 
                        return Task.FromResult(0);
                    }
                };
            }

            private Task AuthenticationFailed(AuthenticationFailedContext arg)
            {
                // For debugging purposes only!
                var s = $"AuthenticationFailed: {arg.Exception.Message}"; 
                arg.Response.ContentLength = s.Length;
                arg.Response.Body.Write(Encoding.UTF8.GetBytes(s), 0, s.Length);
                return Task.FromResult(0);

            }

            public void Configure(JwtBearerOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}
