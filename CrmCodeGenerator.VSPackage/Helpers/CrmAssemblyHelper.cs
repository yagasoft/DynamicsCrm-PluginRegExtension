using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrmPluginEntities;
using CrmPluginRegExt.VSPackage.Model;
using Microsoft.Xrm.Sdk;

namespace CrmPluginRegExt.VSPackage.Helpers
{
	internal static class CrmAssemblyHelper
	{
		/// <summary>
		///     Returns the plugin entity, but only the ID is included.
		/// </summary>
		/// <returns>Assembly entity.</returns>
		internal static PluginAssembly GetCrmAssembly(XrmServiceContext context)
		{
			Status.Update("Fetching CRM assembly ... ");

			var assembly = (from assemblyQ in context.PluginAssemblySet
							where assemblyQ.Name == DteHelper.GetProjectName(null)
							select new PluginAssembly
							{
								Id = assemblyQ.Id,
								Name = assemblyQ.Name,
								IsolationMode = assemblyQ.IsolationMode
							}).FirstOrDefault();

			Status.Update("** Finished fetching CRM assembly.");

			return assembly;
		}

		/// <summary>
		///     Returns the plugin ID.
		/// </summary>
		/// <returns>Assembly Id.</returns>
		internal static Guid GetAssemblyId(XrmServiceContext context)
		{
			Status.Update("Getting assembly ID ... ");

			var assembly = GetCrmAssembly(context);

			if (assembly == null)
			{
				throw new Exception("Assembly doesn't exist in CRM.");
			}

			var id = assembly.Id;

			Status.Update("** Finished getting assembly ID. ID => " + id);

			return id;
		}

		internal static bool IsAssemblyRegistered(XrmServiceContext context)
		{
			Status.Update("Checking assembly registration ... ");

			var registered = GetCrmAssembly(context) != null;

			Status.Update("** Finished checking assembly registration. Registered => " + registered);

			return registered;
		}

		internal static bool IsAssemblySandbox(XrmServiceContext context)
		{
			Status.Update("Checking assembly isolation ... ");

			var assembly = GetCrmAssembly(context);

			if (assembly == null)
			{
				throw new Exception("Assembly doesn't exist in CRM.");
			}

			var isolated = assembly.IsolationMode.Value
						   == (int)PluginAssembly.Enums.IsolationMode.Sandbox;

			Status.Update("** Finished checking assembly isolation. Isolated => " + isolated);

			return isolated;
		}

		internal static List<PluginType> GetCrmTypes(Guid assemblyId, XrmServiceContext context)
		{
			Status.Update("Getting types from CRM ... ");

			var types = (from type in context.PluginTypeSet
						 where type.PluginAssemblyId.Id == assemblyId
						 select type).ToList();

			Status.Update("** Finished getting types from CRM.");

			return types;
		}

	}
}
