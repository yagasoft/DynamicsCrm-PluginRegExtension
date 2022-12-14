#region Imports

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xrm.Sdk.Client;
using Yagasoft.CrmPluginRegistration.Connection;

#endregion

namespace Yagasoft.CrmPluginRegistration.Model
{
	public class CrmPluginType : CrmEntity<CrmTypeStep>
	{
		public bool IsWorkflow { get; set; }

		internal CrmAssembly Assembly;

		public string IsWorkflowString => IsWorkflow ? "[WF] " : "";

		public CrmPluginType(IConnectionManager connectionManager) : base(connectionManager)
		{ }

		protected override void RunUpdateLogic()
		{
			if (IsWorkflow)
			{
				return;
			}

			using var context = new XrmServiceContext(ConnectionManager.Get()) {MergeOption = MergeOption.NoTracking};

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
							stepId = stepOuterQ.SdkMessageProcessingStepIdId ?? Guid.Empty,
							stepName = stepOuterQ.Name,
							stepDisabled = stepOuterQ.Status == SdkMessageProcessingStep.StatusEnum.Disabled
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
					new CrmTypeStep(ConnectionManager)
					{
						Id = stepGroup.First().stepId,
						Name = stepGroup.First().stepName,
						IsDisabled = stepGroup.First().stepDisabled,
						Type = this
					}).OrderBy(step => step.Name));
		}
	}
}
