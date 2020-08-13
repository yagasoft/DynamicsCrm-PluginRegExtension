using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using CrmPluginRegExt.VSPackage.Model;
using Microsoft.Xrm.Sdk.Discovery;
using System.Collections.ObjectModel;
using System.Runtime.Caching;
using System.ServiceModel.Description;
using System.Text.RegularExpressions;
using CrmPluginEntities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Yagasoft.Libraries.EnhancedOrgService.Builders;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Services;

namespace CrmPluginRegExt.VSPackage.Helpers
{
	public class ConnectionHelper
    {
		private static readonly object lockObj = new object();
		private static readonly IDictionary<string, EnhancedServicePool<EnhancedOrgService>> poolCache
			= new Dictionary<string, EnhancedServicePool<EnhancedOrgService>>();

		internal static void ResetCache(string connectionString)
		{
			Status.Update($"Clearing cache ... ");

			lock (lockObj)
			{
				poolCache.TryGetValue(connectionString, out var pool);
				pool?.ClearFactoryCache();
			}

			Status.Update($"Finished clearing cache.");
		}

		internal static IEnhancedOrgService GetConnection(string connectionString, bool noCache = false)
		{
			if (noCache)
			{
				ResetCache(connectionString);
			}

			EnhancedServicePool<EnhancedOrgService> pool;

			lock (lockObj)
			{
				poolCache.TryGetValue(connectionString, out pool);

				if (pool == null)
				{
					Status.Update($"Creating connection pool to CRM ... ");
					Status.Update($"Connection String:"
						+ $" '{Regex.Replace(connectionString, @"Password\s*?=.*?(?:;{0,1}$|;)", "Password=********;").Replace("\r\n", " ")}'.");

					poolCache[connectionString] = pool = EnhancedServiceHelper
						.GetPool(
							new EnhancedServiceParams(connectionString)
							{
								CachingParams = new CachingParams(),
								PoolParams = new PoolParams { PoolSize = 10 }
							});

					Status.Update($"Created connection pool.");
				} 
			}

			var service = pool.GetService();

			return service;
		}
	}
}
