#region Imports

using System;
using CrmPluginRegExt.VSPackage.Helpers;
using Microsoft.Xrm.Sdk;
using Yagasoft.CrmPluginRegistration.Connection;

#endregion

namespace CrmPluginRegExt.VSPackage.PluginRegistration
{
	internal class ConnectionManager : IConnectionManager
	{
		public string ConnectionString { get; set; }

		public ConnectionManager(string connectionString = null)
		{
			ConnectionString = connectionString;
		}

		public IOrganizationService Get()
		{
			return ConnectionHelper.GetConnection(ConnectionString);
		}

		public void ClearCache()
		{
			ConnectionHelper.ResetCache(ConnectionString);
		}
	}
}
