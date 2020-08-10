#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using CrmPluginEntities;
using Microsoft.Xrm.Sdk.Client;
using static CrmPluginRegExt.VSPackage.Helpers.ConnectionHelper;

#endregion

namespace CrmPluginRegExt.VSPackage.Helpers
{
	internal static class CrmAssemblyHelper
	{
		/// <summary>
		///     Returns the plugin entity, but only the ID is included.
		/// </summary>
		/// <returns>Assembly entity.</returns>
		internal static PluginAssembly GetCrmAssembly(string connectionString)
		{
			Status.Update("Fetching CRM assembly ... ");

			PluginAssembly assembly;

			using (var service = GetConnection(connectionString))
			using (var context = new XrmServiceContext(service) {MergeOption = MergeOption.NoTracking})
			{
				assembly =
					(from assemblyQ in context.PluginAssemblySet
					 where assemblyQ.Name == DteHelper.GetProjectName(null)
					 select new PluginAssembly
							{
								Id = assemblyQ.Id,
								Name = assemblyQ.Name,
								IsolationMode = assemblyQ.IsolationMode
							}).FirstOrDefault();
			}

			Status.Update("** Finished fetching CRM assembly.");

			return assembly;
		}

		/// <summary>
		///     Returns the plugin ID.
		/// </summary>
		/// <returns>Assembly Id.</returns>
		internal static Guid GetAssemblyId(string connectionString)
		{
			Status.Update("Getting assembly ID ... ");

			var assembly = GetCrmAssembly(connectionString);

			if (assembly == null)
			{
				throw new Exception("Assembly doesn't exist in CRM.");
			}

			var id = assembly.Id;

			Status.Update("** Finished getting assembly ID. ID => " + id);

			return id;
		}

		internal static bool IsAssemblyRegistered(string connectionString)
		{
			Status.Update("Checking assembly registration ... ");

			var registered = GetCrmAssembly(connectionString) != null;

			Status.Update("** Finished checking assembly registration. Registered => " + registered);

			return registered;
		}

		internal static bool IsAssemblySandbox(string connectionString)
		{
			Status.Update("Checking assembly isolation ... ");

			var assembly = GetCrmAssembly(connectionString);

			if (assembly == null)
			{
				throw new Exception("Assembly doesn't exist in CRM.");
			}

			var isolated = assembly.IsolationMode.Value
				== (int)PluginAssembly.Enums.IsolationMode.Sandbox;

			Status.Update("** Finished checking assembly isolation. Isolated => " + isolated);

			return isolated;
		}

		internal static List<PluginType> GetCrmTypes(Guid assemblyId, string connectionString)
		{
			Status.Update("Getting types from CRM ... ");

			List<PluginType> types;

			using (var service = GetConnection(connectionString))
			using (var context = new XrmServiceContext(service) {MergeOption = MergeOption.NoTracking})
			{
				types =
					(from type in context.PluginTypeSet
					 where type.PluginAssemblyId.Id == assemblyId
					 select type).ToList();
			}

			Status.Update("** Finished getting types from CRM.");

			return types;
		}
	}
}
