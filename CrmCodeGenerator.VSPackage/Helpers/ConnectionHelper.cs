#region Imports

using System;
using System.Text.RegularExpressions;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Cache;

#endregion

namespace CrmPluginRegExt.VSPackage.Helpers
{
	public class ConnectionHelper
	{
		private static readonly object lockObj = new object();
		private const string ConnCacheMemKey = "ys_CrmPluginExt_Conn_846194";

		public static string SecureConnectionString(string connectionString)
		{
			return Regex
				.Replace(Regex
					.Replace(connectionString, @"Password\s*?=.*?(?:;{0,1}$|;)", "Password=********;")
					.Replace("\r\n", " "),
					@"\s+", " ")
				.Replace(" = ", "=");
		}

		internal static void ResetCache(string connectionString)
		{
			Status.Update($"Clearing cache ... ");

			lock (lockObj)
			{
				CacheHelpers.GetFromMemCache<ICachingOrgService>($"{ConnCacheMemKey}_{connectionString}")?
					.ClearCache();
			}

			Status.Update($"Finished clearing cache.");
		}

		internal static IEnhancedOrgService GetConnection(string connectionString, bool noCache = false)
		{
			if (noCache)
			{
				ResetCache(connectionString);
			}

			var memKey = $"{ConnCacheMemKey}_{connectionString}";

			lock (lockObj)
			{
				try
				{
					var service = CacheHelpers.GetFromMemCache<IEnhancedOrgService>(memKey);

					if (service != null)
					{
						return service;
					}

					Status.Update($"Creating connection to CRM ... ");
					Status.Update($"Connection String:" + $" '{CrmHelpers.SecureConnectionString(connectionString)}'.");

					service = EnhancedServiceHelper.GetCachingPoolingService(
						new ServiceParams
						{
							ConnectionParams =
								new ConnectionParams
								{
									ConnectionString = connectionString
								},
							PoolParams =
								new PoolParams
								{
									PoolSize = 5,
									DequeueTimeout = TimeSpan.FromSeconds(20)
								},
							CachingParams = new CachingParams { CacheScope = CacheScope.Service },
							OperationHistoryLimit = 1
						});
					CacheHelpers.AddToMemCache(memKey, service, TimeSpan.MaxValue);

					Status.Update($"Created connection.");

					return service;
				}
				catch
				{
					CacheHelpers.RemoveFromMemCache(memKey);
					throw;
				}
			}
		}
	}
}
