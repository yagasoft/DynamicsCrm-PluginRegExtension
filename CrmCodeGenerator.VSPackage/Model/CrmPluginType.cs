#region Imports

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CrmPluginEntities;
using Microsoft.Xrm.Sdk.Client;
using static CrmPluginRegExt.VSPackage.Helpers.ConnectionHelper;

#endregion

namespace CrmPluginRegExt.VSPackage.Model
{
	public class CrmPluginType : CrmEntity<CrmTypeStep>
	{
		public bool IsWorkflow { get; set; }

		internal CrmAssembly Assembly;

		public string IsWorkflowString => IsWorkflow ? "[WF] " : "";

		protected override void RunUpdateLogic(string connectionString)
		{
			if (IsWorkflow)
			{
				return;
			}

			using (var context = new XrmServiceContext(GetConnection(connectionString)) {MergeOption = MergeOption.NoTracking})
			{
				var result =
					(from type in context.PluginTypeSet
					 join step in context.SdkMessageProcessingStepSet
						 on type.PluginTypeId equals step.EventHandler.Id
						 into stepOuterT
					 from stepOuterQ in stepOuterT.DefaultIfEmpty()
					 where type.PluginTypeId == Id
					 select new
							{
								id = type.PluginTypeId.Value,
								name = type.Name,
								isWorkflow = type.IsWorkflowActivity.HasValue
									&& type.IsWorkflowActivity.Value,
								stepId = stepOuterQ.SdkMessageProcessingStepId ?? Guid.Empty,
								stepName = stepOuterQ.Name,
								stepDisabled = stepOuterQ.StateCode == SdkMessageProcessingStepState.Disabled
							}).ToList();

				if (!result.Any())
				{
					Children = Unfiltered = null;
					return;
				}

				Name = result.First().name;
				Children = Unfiltered = new ObservableCollection<CrmTypeStep>(result
					.GroupBy(step => step.stepId)
					.Where(stepGroup => stepGroup.First().stepId != Guid.Empty)
					.Select(stepGroup =>
						new CrmTypeStep
						{
							Id = stepGroup.First().stepId,
							Name = stepGroup.First().stepName,
							IsDisabled = stepGroup.First().stepDisabled,
							Type = this
						}).OrderBy(step => step.Name));
			}
		}
	}
}
