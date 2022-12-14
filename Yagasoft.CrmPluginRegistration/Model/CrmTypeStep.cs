#region Imports

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xrm.Sdk.Client;
using Yagasoft.CrmPluginRegistration.Connection;
using Yagasoft.CrmPluginRegistration.Helpers;

#endregion

namespace Yagasoft.CrmPluginRegistration.Model
{
	public class CrmTypeStep : CrmEntity<CrmStepImage>
	{
		#region Properties

		private string message;
		private bool isDisabled;

		public override string DisplayName => Name.Replace(Type.Name + ": ", "");

		public string Message
		{
			get => message;
			set
			{
				message = value;
				MessagePropertyName = CrmDataHelpers.GetMessagePropertyName(message);
			}
		}

		public string Entity { get; set; }
		public Guid FilterId { get; set; }
		public Guid MessageId { get; set; }
		public Guid UserId { get; set; }
		public Guid SecureId { get; set; }
		public int ExecutionOrder { get; set; }
		public string Description { get; set; }

		public bool IsDisabled
		{
			get => isDisabled;
			set
			{
				isDisabled = value;
				OnPropertyChanged("IsDisabledString");
			}
		}

		public SdkMessageProcessingStep.ExecutionStageEnum Stage { get; set; }
		public SdkMessageProcessingStep.ExecutionModeEnum Mode { get; set; }
		public SdkMessageProcessingStep.DeploymentEnum Deployment { get; set; }
		public bool IsDeleteJob { get; set; }
		public string UnsecureConfig { get; set; }
		public string SecureConfig { get; set; }
		public string Attributes { get; set; }
		internal string MessagePropertyName { get; set; }

		public CrmPluginType Type { get; set; }

		public string IsDisabledString => IsDisabled ? "[Disabled] " : "";

		#endregion

		public CrmTypeStep(IConnectionManager connectionManager) : base(connectionManager)
		{
			ExecutionOrder = 1;
			Mode = SdkMessageProcessingStep.ExecutionModeEnum.Synchronous;
			Stage = SdkMessageProcessingStep.ExecutionStageEnum.Postoperation;
			Deployment = SdkMessageProcessingStep.DeploymentEnum.ServerOnly;
		}

		protected override void RunUpdateLogic()
		{
			using (var context = new XrmServiceContext(ConnectionManager.Get()) { MergeOption = MergeOption.NoTracking })
			{
				var result =
					(from step in context.SdkMessageProcessingStepSet
					 join image in context.SdkMessageProcessingStepImageSet
						 on step.SdkMessageProcessingStepIdId equals image.SDKMessageProcessingStep
						 into imageOuterT
					 from imageOuterQ in imageOuterT.DefaultIfEmpty()
					 where step.SdkMessageProcessingStepIdId == Id
					 select new
							{
								id = step.SdkMessageProcessingStepIdId,
								name = step.Name,
								imageId = imageOuterQ.SdkMessageProcessingStepImageIdId ?? Guid.Empty,
								imageName = imageOuterQ.Name
							}).ToList();

				var messageResult =
					(from step in context.SdkMessageProcessingStepSet
					 join message in context.SdkMessageSet
						 on step.SDKMessage equals message.SdkMessageIdId
					 where step.SdkMessageProcessingStepIdId == Id
					 select new
							{
								messageName = message.Name,
								messageId = message.SdkMessageIdId ?? Guid.Empty,
								attributes = step.FilteringAttributes,
								userId = step.ImpersonatingUser,
								rank = step.ExecutionOrder,
								description = step.Description,
								stage = step.ExecutionStage,
								mode = step.ExecutionMode,
								deployment = step.Deployment,
								deleteJob = step.AsynchronousAutomaticDelete,
								unsecureConfiguration = step.Configuration,
								isDisabled = step.Status == SdkMessageProcessingStep.StatusEnum.Disabled
							}).FirstOrDefault();

				var filterResult =
					(from step in context.SdkMessageProcessingStepSet
					 join filter in context.SdkMessageFilterSet
						 on step.SdkMessageFilter equals filter.SdkMessageFilterIdId
					 where step.SdkMessageProcessingStepIdId == Id
					 select new
							{
								entityName = filter.PrimaryObjectTypeCode,
								filterId = filter.SdkMessageFilterIdId ?? Guid.Empty,
							}).FirstOrDefault();

				var secureResult =
					(from step in context.SdkMessageProcessingStepSet
					 join secureConfig in context.SdkMessageProcessingStepSecureConfigurationSet
						 on step.SDKMessageProcessingStepSecureConfiguration
						 equals secureConfig.SdkMessageProcessingStepSecureConfigIdId
					 where step.SdkMessageProcessingStepIdId == Id
					 select new
							{
								id = secureConfig.SdkMessageProcessingStepSecureConfigIdId,
								secureConfig = secureConfig.SecureConfiguration
							}).FirstOrDefault();

				if (messageResult != null)
				{
					if (filterResult != null)
					{
						Entity = filterResult.entityName;
						FilterId = filterResult.filterId;
					}
					else
					{
						Entity = "none";
					}

					Message = messageResult.messageName;
					MessageId = messageResult.messageId;
					Attributes = messageResult.attributes;

					if (messageResult.userId != null)
					{
						UserId = messageResult.userId.GetValueOrDefault();
					}

					ExecutionOrder = messageResult.rank ?? 1;
					Description = messageResult.description;
					Stage = messageResult.stage.GetValueOrDefault();
					Mode = messageResult.mode.GetValueOrDefault();
					Deployment = messageResult.deployment.GetValueOrDefault();
					IsDeleteJob = messageResult.deleteJob ?? false;
					UnsecureConfig = messageResult.unsecureConfiguration;
					IsDisabled = messageResult.isDisabled;

					if (secureResult != null)
					{
						SecureId = secureResult.id ?? Guid.Empty;
						SecureConfig = secureResult.secureConfig;
					}
				}

				if (!result.Any())
				{
					Children = Unfiltered = null;
					return;
				}

				Name = result.First().name;
				Children = Unfiltered = new ObservableCollection<CrmStepImage>(result
					.GroupBy(image => image.imageId)
					.Where(imageGroup => imageGroup.First().imageId != Guid.Empty)
					.Select(imageGroup =>
						new CrmStepImage(ConnectionManager)
						{
							Id = imageGroup.First().imageId,
							Name = imageGroup.First().imageName,
							Step = this
						}).OrderBy(image => image.Name));
			}
		}
	}
}
