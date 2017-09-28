using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using MultiTenantAadAuthAspNetCore.Services;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace MultiTenantAadAuthAspNetCore.Pages
{
    public class IndexModel : PageModel
    {
        private IConfiguration _config;
        private IDistributedCache _cache;

        public IndexModel(IConfiguration config, IDistributedCache cache)
        {
            _config = config;
            _cache = cache;
        }

        public void OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                string userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                var cache = new AdalDistributedTokenCache(_cache, userId);

                var authContext = new AuthenticationContext(String.Format(_config["AzureAD:Authority"], "common"), cache);
                string clientId = _config["AzureAD:ClientId"];
                string clientSecret = _config["AzureAD:ClientSecret"];
                var clientCred = new ClientCredential(clientId, clientSecret);

                //Use the following definition for the acquiretokenSilent method.
                var result = authContext.AcquireTokenSilentAsync(_config["AzureAD:VstsResourceId"], clientCred, new UserIdentifier(userId, UserIdentifierType.UniqueId)).Result;

                ViewData["AccessToken"] = result.AccessToken;
            }
        }
    }
}
