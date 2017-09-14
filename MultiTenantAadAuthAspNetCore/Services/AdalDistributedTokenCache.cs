using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiTenantAadAuthAspNetCore.Services
{
    public class AdalDistributedTokenCache : TokenCache
    {
        private IDistributedCache _distributedCache;
        private string _userId;
        private string _cacheKey;

        public AdalDistributedTokenCache(IDistributedCache distributedCache, string userId)
        {
            _distributedCache = distributedCache;
            _userId = userId;
            _cacheKey = BuildCacheKey(userId);

            //These are the notifications fired by adal while accessing TokenCache
            //BeforeAccess = BeforeAccessNotification;
            AfterAccess = AfterAccessNotification;

            LoadFromCache();
        }

        private static string BuildCacheKey(string userId)
        {
            return string.Format("UserId:{0}::AccessToken", userId);
        }

        private void LoadFromCache()
        {
            byte[] cacheData = _distributedCache.Get(_cacheKey);
            if (cacheData != null)
            {
                this.Deserialize(cacheData);
            }
        }

        /// <summary>
        /// If cache data is changed, the "HasStateChanged" property is set to true.
        /// In that case, update the _distributedCache with new value and reset "HasStateChanged".
        /// </summary>
        /// <param name="args"></param>
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if(this.HasStateChanged)
            {
                try
                {
                    if(this.Count > 0)
                    {
                        var newData = this.Serialize();
                        _distributedCache.SetAsync(_cacheKey, newData);
                    }
                    else
                    {
                        //there is no token for this user, so remove the item from the cache
                        _distributedCache.RemoveAsync(_cacheKey);
                    }
                }
                catch (Exception ex)
                {
                    //log this exception
                }
            }
        }
    }
}
