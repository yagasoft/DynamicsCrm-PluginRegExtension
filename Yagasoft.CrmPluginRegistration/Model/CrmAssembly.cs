#region Imports

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Client;
using Yagasoft.CrmPluginRegistration.Connection;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.CrmPluginRegistration.Model
{
	public class CrmAssembly : CrmEntity<CrmPluginType>
	{
		public bool IsSandbox { get; set; }

		public CrmAssembly(IConnectionManager connectionManager) : base(connectionManager)
		{ }

		protected override void RunUpdateLogic()
		{
			using var context = new XrmServiceContext(ConnectionManager.Get()) { MergeOption = MergeOption.NoTracking };

			var result =
				(from assembly in context.PluginAssemblySet
				 join type in context.PluginTypeSet
					 on assembly.PluginAssemblyIdId equals type.PluginAssembly
				 where assembly.PluginAssemblyIdId == Id
				 select new
						{
							id = assembly.PluginAssemblyIdId,
							name = assembly.Name,
							isSandbox = assembly.IsolationMode == PluginAssembly.IsolationModeEnum.Sandbox,
							typeId = type.PluginTypeId.Value,
							typeName = type.Name,
							typeIsWorkflow = type.IsWorkflowActivity.HasValue
								&& type.IsWorkflowActivity.Value
						}).ToList();

			Children = Unfiltered = new ObservableCollection<CrmPluginType>();

			if (!result.Any())
			{
				return;
			}

			Name = result.First().name;
			IsSandbox = result.First().isSandbox;
			Children = Unfiltered = new ObservableCollection<CrmPluginType>(result
				.GroupBy(assembly => assembly.typeId)
				.Where(typeGroup => typeGroup.First().id == Id)
				.Select(typeGroup =>
					new CrmPluginType(ConnectionManager)
					{
						Id = typeGroup.First().typeId,
						Name = typeGroup.First().typeName,
						IsWorkflow = typeGroup.First().typeIsWorkflow,
						Assembly = this
					})
				.OrderBy(type => type.IsWorkflow)
				.ThenBy(type => type.Name));
		}

		public void Clear()
		{
			Children = Unfiltered = new ObservableCollection<CrmPluginType>();
			IsUpdated = false;
			Name = "";
			Id = Guid.Empty;

			OnUpdate();
		}
	}
}
