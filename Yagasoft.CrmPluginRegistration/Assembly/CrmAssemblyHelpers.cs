#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using CrmPluginEntities;
using Microsoft.Xrm.Sdk.Client;
using Yagasoft.CrmPluginRegistration.Connection;
using Yagasoft.CrmPluginRegistration.Logger;

#endregion

namespace Yagasoft.CrmPluginRegistration.Assembly
{
	public static class CrmAssemblyHelpers
	{
		public static PluginAssembly GetCrmAssembly(string assemblyName,
			IConnectionManager connectionManager, IPluginRegLogger pluginRegLogger)
		{
			pluginRegLogger.Log("Fetching CRM assembly ... ");

			PluginAssembly assembly;

			using (var context = new XrmServiceContext(connectionManager.Get()) { MergeOption = MergeOption.NoTracking })
			{
				assembly =
					(from assemblyQ in context.PluginAssemblySet
					 where assemblyQ.Name == assemblyName
					 orderby assemblyQ.Version descending
					 select new PluginAssembly
							{
								Id = assemblyQ.Id,
								Name = assemblyQ.Name,
								IsolationMode = assemblyQ.IsolationMode,
								Version = assemblyQ.Version,
								Culture = assemblyQ.Culture,
								PublicKeyToken = assemblyQ.PublicKeyToken
							}).FirstOrDefault();
			}

			pluginRegLogger.Log("** Finished fetching CRM assembly.");

			return assembly;
		}

		public static Guid GetAssemblyId(string assemblyName,
			IConnectionManager connectionManager, IPluginRegLogger pluginRegLogger)
		{
			pluginRegLogger.Log("Getting assembly ID ... ");

			var assembly = GetCrmAssembly(assemblyName, connectionManager, pluginRegLogger);

			if (assembly == null)
			{
				throw new Exception("Assembly doesn't exist in CRM.");
			}

			var id = assembly.Id;

			pluginRegLogger.Log("** Finished getting assembly ID. ID => " + id);

			return id;
		}

		public static bool IsAssemblyRegistered(string assemblyName,
			IConnectionManager connectionManager, IPluginRegLogger pluginRegLogger)
		{
			pluginRegLogger.Log("Checking assembly registration ... ");

			var registered = GetCrmAssembly(assemblyName, connectionManager, pluginRegLogger) != null;

			pluginRegLogger.Log("** Finished checking assembly registration. Registered => " + registered);

			return registered;
		}

		public static bool IsAssemblySandbox(string assemblyName,
			IConnectionManager connectionManager, IPluginRegLogger pluginRegLogger)
		{
			pluginRegLogger.Log("Checking assembly isolation ... ");

			var assembly = GetCrmAssembly(assemblyName, connectionManager, pluginRegLogger);

			if (assembly == null)
			{
				throw new Exception("Assembly doesn't exist in CRM.");
			}

			var isolated = assembly.IsolationMode.Value
				== (int)PluginAssembly.Enums.IsolationMode.Sandbox;

			pluginRegLogger.Log("** Finished checking assembly isolation. Isolated => " + isolated);

			return isolated;
		}

		public static IReadOnlyList<PluginType> GetCrmTypes(IConnectionManager connectionManager, IPluginRegLogger pluginRegLogger,
			Guid assemblyId)
		{
			pluginRegLogger.Log("Getting types from CRM ... ");

			PluginType[] types;

			using (var context = new XrmServiceContext(connectionManager.Get()) { MergeOption = MergeOption.NoTracking })
			{
				types =
					(from type in context.PluginTypeSet
					 where type.PluginAssemblyId.Id == assemblyId
					 select type).ToArray();
			}

			pluginRegLogger.Log("** Finished getting types from CRM.");

			return types;
		}

		public static IReadOnlyList<SdkMessageProcessingStep> GetCrmSteps(IConnectionManager connectionManager, IPluginRegLogger pluginRegLogger,
			Guid assemblyId)
		{
			pluginRegLogger.Log("Getting steps from CRM ... ");

			SdkMessageProcessingStep[] steps;

			using (var context = new XrmServiceContext(connectionManager.Get()) { MergeOption = MergeOption.NoTracking })
			{
				steps =
					(from step in context.SdkMessageProcessingStepSet
					 join type in context.PluginTypeSet
						 on step.EventHandler.Id equals type.PluginTypeId
					 where type.PluginAssemblyId.Id == assemblyId
					 select 
						 new SdkMessageProcessingStep
						 {
							 Id = step.SdkMessageProcessingStepId.GetValueOrDefault(),
							 EventHandler = step.EventHandler
						 }).ToArray();
			}

			pluginRegLogger.Log("** Finished getting steps from CRM.");

			return steps;
		}
	}
}
