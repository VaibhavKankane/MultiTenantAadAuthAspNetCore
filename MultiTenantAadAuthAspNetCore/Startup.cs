using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using MultiTenantAadAuthAspNetCore.Services;

namespace MultiTenantAadAuthAspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                options.ClientId = Configuration["AzureAD:ClientId"];
                options.ClientSecret = Configuration["AzureAD:ClientSecret"];
                options.Authority = String.Format(Configuration["AzureAD:Authority"], "common");
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                options.Events = new OpenIdConnectEvents()
                {
                    OnAuthorizationCodeReceived = OnAuthCodeReceivedAsync,
                    OnAuthenticationFailed = OnAuthFailed,
                    OnRemoteFailure = OnRemoteFailed,
                };
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    ValidateIssuer = false
                };
            });

            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = Configuration["Redis:ConnectionString"];
            });
        }

        private Task OnRemoteFailed(RemoteFailureContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("/Error?message=" + context.Failure.Message);
            return Task.FromResult(0);
        }

        private Task OnAuthFailed(AuthenticationFailedContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("/Error?message=" + context.Exception.Message);
            return Task.FromResult(0);
        }

        private async Task OnAuthCodeReceivedAsync(AuthorizationCodeReceivedContext context)
        {
            string userId = context.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            var distributedCache = context.HttpContext.RequestServices.GetService<IDistributedCache>();
            var cache = new AdalDistributedTokenCache(distributedCache, userId);

            var authContext = new AuthenticationContext(String.Format(Configuration["AzureAD:Authority"], "common"), cache);
            var result = await authContext.AcquireTokenByAuthorizationCodeAsync(context.ProtocolMessage.Code, new Uri(Configuration["AzureAD:RedirectUri"]), new ClientCredential(Configuration["AzureAD:ClientId"], Configuration["AzureAD:ClientSecret"]), Configuration["AzureAD:VstsResourceId"]);

            // Notify the OIDC middleware that we already took care of code redemption.
            context.HandleCodeRedemption(result.AccessToken, result.IdToken);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });
        }
    }
}
