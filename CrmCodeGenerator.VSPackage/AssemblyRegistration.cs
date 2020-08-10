#region Imports

using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using CrmPluginEntities;
using CrmPluginRegExt.VSPackage.Helpers;
using CrmPluginRegExt.VSPackage.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using static CrmPluginRegExt.VSPackage.Helpers.ConnectionHelper;

#endregion

namespace CrmPluginRegExt.VSPackage
{
	public enum RegistrationEvent
	{
		Create,
		Update,
		Delete
	}

	/// <summary>
	///     Author: Ahmed el-Sawalhy
	/// </summary>
	internal class AssemblyRegistration : INotifyPropertyChanged
	{
		public string ConnectionString;

		#region Properties

		public readonly object LoggingLock = new object();
		public readonly object ActionLock = new object();

		private string logMessage;
		private int indentIndex;

		public string LogMessage
		{
			get => logMessage;
			set
			{
				logMessage = value;
				OnPropertyChanged();
			}
		}

		private Exception error;

		public Exception Error
		{
			get => error;
			set
			{
				error = value;
				OnPropertyChanged();
			}
		}

		private Guid id;

		public Guid Id
		{
			get => id;
			set
			{
				id = value;
				OnPropertyChanged();
			}
		}

		private bool cancelRegistration;

		public bool CancelRegistration
		{
			get => cancelRegistration;
			set
			{
				cancelRegistration = value;
				OnPropertyChanged();
			}
		}

		#endregion

		private void UpdateStatus(string message, int indexModifier = 0)
		{
			lock (LoggingLock)
			{
				indentIndex = indexModifier > 0 ? (indentIndex + indexModifier) : indentIndex;
				LogMessage = $"{new string('>', indentIndex)} {message}";
				indentIndex = indexModifier < 0 ? (indentIndex + indexModifier) : indentIndex;
			}
		}

		#region Events

		#region Property event

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region Registration event

		public event EventHandler<RegistrationEvent> RegistrationActionTaken;

		protected virtual void OnRegActionTaken(RegistrationEvent e)
		{
			RegistrationActionTaken?.Invoke(this, e);
		}

		#endregion

		#endregion

		public AssemblyRegistration()
		{ }

		public AssemblyRegistration(string connectionString)
		{
			ConnectionString = connectionString;
		}

		#region Assembly actions

		/// <summary>
		///     Create the assembly in CRM with the contents of the assembly in the appropriate folder.
		/// </summary>
		internal Guid CreateAssembly(bool sandbox)
		{
			lock (ActionLock)
			{
				return _CreateAssembly(AssemblyHelper.GetAssemblyData(true), sandbox);
			}
		}

		/// <summary>
		///     Update the assembly in CRM with the contents of the assembly in the appropriate folder.
		/// </summary>
		internal void UpdateAssembly(bool? sandbox)
		{
			lock (ActionLock)
			{
				Id = CrmAssemblyHelper.GetAssemblyId(ConnectionString);
				_UpdateAssembly(AssemblyHelper.GetAssemblyData(true), sandbox);
			}
		}

		internal void DeleteAssembly(Guid assemblyId)
		{
			lock (ActionLock)
			{
				Id = assemblyId;
				_DeleteAssembly();
			}
		}

		#endregion

		#region Step image actions

		internal void CreateStepImage(CrmStepImage image)
		{
			lock (ActionLock)
			{
				_CreateImage(image);
			}
		}

		internal void UpdateStepImage(CrmStepImage image)
		{
			lock (ActionLock)
			{
				_UpdateImage(image);
			}
		}

		internal void DeleteStepImage(Guid imageId)
		{
			lock (ActionLock)
			{
				_DeleteImage(imageId);
			}
		}

		#endregion

		#region Type step actions

		internal void CreateTypeStep(CrmTypeStep step)
		{
			lock (ActionLock)
			{
				_CreateStep(step);
			}
		}

		internal void UpdateTypeStep(CrmTypeStep step)
		{
			lock (ActionLock)
			{
				_UpdateStep(step);
			}
		}

		internal void DeleteTypeStep(Guid stepId)
		{
			lock (ActionLock)
			{
				_DeleteStep(stepId);
			}
		}

		internal void SetTypeStepState(Guid stepId, SdkMessageProcessingStepState state)
		{
			lock (ActionLock)
			{
				_SetStepState(stepId, state);
			}
		}

		#endregion

		#region Assembly CRM Actions

		private Guid _CreateAssembly(byte[] assemblyData, bool sandbox)
		{
			UpdateStatus("Creating assembly ... ", 1);

			var assemblyInfo = AssemblyHelper.GetAssemblyInfo(typeof(IPlugin).FullName);

			var assembly = new PluginAssembly
						   {
							   Name = DteHelper.GetProjectName(),
							   IsolationMode = (sandbox
								   ? PluginAssembly.Enums.IsolationMode.Sandbox
								   : PluginAssembly.Enums.IsolationMode.None).ToOptionSetValue(),
							   Content = Convert.ToBase64String(assemblyData),
							   SourceType = PluginAssembly.Enums.SourceType.Database.ToOptionSetValue(),
							   Culture = assemblyInfo.CultureInfo.LCID == CultureInfo.InvariantCulture.LCID
								   ? "neutral"
								   : assemblyInfo.CultureInfo.Name
						   };

			UpdateStatus("Saving new assembly to CRM ...");

			using (var service = GetConnection(ConnectionString))
			{
				Id = service.Create(assembly);
			}

			AddNewTypes(GetExistingTypeNames());

			//UpdateStatus("Saving new types to CRM ...");
			//Context.SaveChanges();

			UpdateStatus("Finished creating assembly. ID => " + Id, -1);

			OnRegActionTaken(RegistrationEvent.Create);

			return Id;
		}

		private void _UpdateAssembly(byte[] assemblyData, bool? sandbox)
		{
			UpdateStatus("Updating assembly ... ", 1);

			DeleteObsoleteTypes();

			//UpdateStatus("Fetching existing assembly ... ");

			var updatedAssembly =
				new PluginAssembly
				{
					Id = Id,
					Content = Convert.ToBase64String(assemblyData)
				};
			//var updatedAssembly = (from assembly in Context.PluginAssemblySet
			//                       where assembly.PluginAssemblyId == Id
			//                       select new PluginAssembly
			//                              {
			//	                              PluginAssemblyId = assembly.PluginAssemblyId
			//                              }).FirstOrDefault()
			//                      ?? new PluginAssembly();

			//UpdateStatus("Finished fetching existing assembly.");

			//if (updatedAssembly.Id == Guid.Empty)
			//{
			//	updatedAssembly.Id = Id;
			//}

			if (sandbox.HasValue)
			{
				updatedAssembly.IsolationMode =
					new OptionSetValue((int)(sandbox.Value
						? PluginAssembly.Enums.IsolationMode.Sandbox
						: PluginAssembly.Enums.IsolationMode.None));
			}

			//Context.ConfirmAttached(updatedAssembly);
			//Context.UpdateObject(updatedAssembly);

			UpdateStatus("Updating assembly ... ");

			using (var service = GetConnection(ConnectionString))
			{
				service.Update(updatedAssembly);
			}

			var existingTypeNames = GetExistingTypeNames();

			AddNewTypes(existingTypeNames);
			RefreshTypes(existingTypeNames);

			//UpdateStatus("Saving modified assembly to CRM ...");
			//Context.SaveChanges();

			UpdateStatus("Finished updating assembly.", -1);

			OnRegActionTaken(RegistrationEvent.Update);
		}

		private void _DeleteAssembly()
		{
			UpdateStatus("Deleting assembly ... ", 1);

			DeleteTree(Id, Dependency.Enums.RequiredComponentType.PluginAssembly);

			UpdateStatus("Finished deleting assembly. ID => " + Id, -1);

			Id = Guid.Empty;

			OnRegActionTaken(RegistrationEvent.Delete);
		}

		private void AddNewTypes(ICollection<string> existingTypeNames)
		{
			UpdateStatus("Adding new types ... ", 1);

			var pluginClasses = AssemblyHelper.GetClasses<IPlugin>();
			var wfClasses = AssemblyHelper.GetClasses<CodeActivity>();

			pluginClasses.Where(pluginType => !existingTypeNames.Contains(pluginType)).ToList()
				.ForEach(pluginType =>
						 {
							 var className = pluginType.Split('.')[pluginType.Split('.').Length - 1];

							 UpdateStatus($"Adding plugin '{className}' ... ", 1);

							 var newType = new PluginType
										   {
											   PluginTypeId = Guid.NewGuid(),
											   Name = pluginType,
											   TypeName = pluginType,
											   FriendlyName = className,
											   PluginAssemblyId =
												   new EntityReference(PluginAssembly.EntityLogicalName, Id)
										   };

							 using (var service = GetConnection(ConnectionString))
							 {
								 service.Create(newType);
							 }

							 //using (var service = GetConnection(ConnectionString)) { var id = service.Create(newType);

							 UpdateStatus($"Finished adding plugin '{className}'.", -1);
						 });

			// create new types
			wfClasses.Where(pluginType => !existingTypeNames.Contains(pluginType)).ToList()
				.ForEach(pluginType =>
						 {
							 var className = pluginType.Split('.')[pluginType.Split('.').Length - 1];

							 UpdateStatus($"Adding custom step '{className}' ... ", 1);

							 var newType = new PluginType
										   {
											   PluginTypeId = Guid.NewGuid(),
											   Name = pluginType,
											   TypeName = pluginType,
											   FriendlyName = className,
											   PluginAssemblyId =
												   new EntityReference(PluginAssembly.EntityLogicalName, Id),
											   WorkflowActivityGroupName
												   = string.Format(CultureInfo.InvariantCulture, "{0} ({1})",
													   DteHelper.GetProjectName(), AssemblyHelper.GetAssemblyVersion())
										   };

							 using (var service = GetConnection(ConnectionString))
							 {
								 service.Create(newType);
							 }

							 //using (var service = GetConnection(ConnectionString)) { var id = service.Create(newType);

							 UpdateStatus($"Finished adding custom step '{className}'.", -1);
						 });

			UpdateStatus("Finished adding new types.", -1);
		}

		private List<string> GetExistingTypeNames()
		{
			var existingTypeNames = CrmAssemblyHelper.GetCrmTypes(Id, ConnectionString)
				.Select(type => type.TypeName).ToList();
			return existingTypeNames;
		}

		private void RefreshTypes(List<string> existingTypeNames)
		{
			var wfClasses = AssemblyHelper.GetClasses<CodeActivity>();

			if (wfClasses.Any())
			{
				UpdateStatus("Refreshing custom steps ... ", 1);

				// create new types
				wfClasses.Where(existingTypeNames.Contains).ToList()
					.ForEach(pluginType =>
							 {
								 var className = pluginType.Split('.')[pluginType.Split('.').Length - 1];

								 UpdateStatus($"Refreshing '{className}' ... ", 1);

								 Guid? typeId;

								 using (var service = GetConnection(ConnectionString))
								 using (var context = new XrmServiceContext(service) { MergeOption = MergeOption.NoTracking })
								 {
									 typeId =
										 (from typeQ in context.PluginTypeSet
										  where typeQ.TypeName == pluginType
										  select typeQ.PluginTypeId).First();
								 }

								 if (typeId == null)
								 {
									 throw new Exception("Failed to get plugin type ID.");
								 }

								 //var updatedType = Context.PluginTypeSet.FirstOrDefault(entity => entity.PluginTypeId == typeId)
								 //				   ?? new PluginType();

								 var updatedType =
									 new PluginType
									 {
										 PluginTypeId = typeId,
										 Name = pluginType,
										 TypeName = pluginType,
										 FriendlyName = className,
										 PluginAssemblyId = new EntityReference(PluginAssembly.EntityLogicalName, Id),
										 WorkflowActivityGroupName = string.Format(CultureInfo.InvariantCulture, "{0} ({1})",
											 DteHelper.GetProjectName(), AssemblyHelper.GetAssemblyVersion())
									 };

								 //if (updatedType.Id == Guid.Empty)
								 //{
								 // updatedType.Id = typeId.Value;
								 //}

								 //Context.ConfirmAttached(updatedType);
								 //Context.UpdateObject(updatedType);

								 UpdateStatus($"Refreshing type '{updatedType.Id}' ... ");

								 using (var service = GetConnection(ConnectionString))
								 {
									 service.Update(updatedType);
								 }

								 UpdateStatus($"Finished refreshing '{className}'.", -1);
							 });

				UpdateStatus("Finished refreshing custom steps.", -1);
			}
		}

		private void DeleteObsoleteTypes()
		{
			var pluginClasses = AssemblyHelper.GetClasses<IPlugin>();
			var wfClasses = AssemblyHelper.GetClasses<CodeActivity>();

			var existingTypes = CrmAssemblyHelper.GetCrmTypes(Id, ConnectionString);

			var nonExistentTypes = existingTypes
				.Where(pluginType => !pluginClasses.Contains(pluginType.TypeName)
					&& !wfClasses.Contains(pluginType.TypeName)).ToList();

			// delete non-existing types
			if (nonExistentTypes.Any())
			{
				if (DteHelper.IsConfirmed("Please confirm that you want to DELETE non-existent types in this assembly." +
					" This means that all its steps will be deleted!", "Type deletion"))
				{
					UpdateStatus("Deleting non-existent types ... ", 1);

					nonExistentTypes.ForEach(pluginType => DeleteTree(pluginType.Id,
						Dependency.Enums.RequiredComponentType.PluginType));

					UpdateStatus("Finished deleting non-existent types.", -1);
				}
				else
				{
					throw new Exception("Can't update assembly with obsolete types in the system.");
				}
			}
		}

		#endregion

		#region Step CRM actions

		private void _CreateStep(CrmTypeStep step)
		{
			UpdateStatus($"Creating step '{step.Name}' in type ... ", 1);

			var secureId = Guid.Empty;

			if (!string.IsNullOrEmpty(step.SecureConfig))
			{
				using (var service = GetConnection(ConnectionString))
				using (var context = new XrmServiceContext(service) { MergeOption = MergeOption.NoTracking })
				{
					secureId = step.SecureId =
						((CreateResponse)context.Execute(
							new CreateRequest
							{
								Target = new SdkMessageProcessingStepSecureConfig
										 {
											 SecureConfig = step.SecureConfig
										 }
							})).id;
				}
			}

			var newStep = new SdkMessageProcessingStep
						  {
							  SdkMessageId = new EntityReference(SdkMessage.EntityLogicalName,
								  step.MessageId),
							  Name = step.Name,
							  FilteringAttributes = step.Attributes ?? "",
							  Rank = step.ExecutionOrder,
							  Description = step.Description ?? "",
							  Stage = step.Stage.ToOptionSetValue(),
							  Mode = step.Mode.ToOptionSetValue(),
							  SupportedDeployment = step.Deployment.ToOptionSetValue(),
							  AsyncAutoDelete = step.IsDeleteJob,
							  Configuration = step.UnsecureConfig ?? "",
							  EventHandler = new EntityReference(PluginType.EntityLogicalName,
								  step.Type.Id)
						  };

			if (step.UserId != Guid.Empty)
			{
				newStep.ImpersonatingUserId = new EntityReference(SystemUser.EntityLogicalName,
					step.UserId);
			}

			if (step.FilterId != Guid.Empty)
			{
				newStep.SdkMessageFilterId = new EntityReference(SdkMessageFilter.EntityLogicalName,
					step.FilterId);
			}

			if (secureId != Guid.Empty)
			{
				newStep.SdkMessageProcessingStepSecureConfigId
					= new EntityReference(SdkMessageProcessingStepSecureConfig.EntityLogicalName,
						secureId);
			}

			//using (var service = GetConnection(ConnectionString)) { var id = service.Create(newStep);

			UpdateStatus("Saving new step to CRM ... ");

			using (var service = GetConnection(ConnectionString))
			{
				step.Id = service.Create(newStep);
			}

			//Context.SaveChanges();
			//step.Id = newStep.Id;

			UpdateStatus($"Finished creating step '{step.Name}'.", -1);
		}

		private void _UpdateStep(CrmTypeStep step)
		{
			UpdateStatus($"Updating step '{step.Name}' in type ... ", 1);

			//var updatedStep = Context.SdkMessageProcessingStepSet.FirstOrDefault(entity => entity.SdkMessageProcessingStepId == step.Id)
			//					  ?? new SdkMessageProcessingStep();
			//if (updatedStep.Id == Guid.Empty)
			//{
			//	updatedStep.Id = step.Id;
			//}
			var updatedStep = new SdkMessageProcessingStep
							  {
								  Id = step.Id,
								  SdkMessageId = new EntityReference(SdkMessage.EntityLogicalName, step.MessageId),
								  FilteringAttributes = step.Attributes ?? "",
								  Name = step.Name,
								  Rank = step.ExecutionOrder,
								  Description = step.Description ?? "",
								  Stage = step.Stage.ToOptionSetValue(),
								  Mode = step.Mode.ToOptionSetValue(),
								  SupportedDeployment = step.Deployment.ToOptionSetValue(),
								  AsyncAutoDelete = step.IsDeleteJob,
								  Configuration = step.UnsecureConfig ?? ""
							  };

			if (step.UserId == Guid.Empty)
			{
				updatedStep.ImpersonatingUserId = null;
			}
			else
			{
				updatedStep.ImpersonatingUserId = new EntityReference(SystemUser.EntityLogicalName,
					step.UserId);
			}

			if (step.FilterId == Guid.Empty)
			{
				updatedStep.SdkMessageFilterId = null;
			}
			else
			{
				updatedStep.SdkMessageFilterId = new EntityReference(SdkMessageFilter.EntityLogicalName,
					step.FilterId);
			}

			var secureId = step.SecureId;

			if (string.IsNullOrEmpty(step.SecureConfig))
			{
				secureId = Guid.Empty;
			}
			else
			{
				if (secureId == Guid.Empty)
				{
					using (var service = GetConnection(ConnectionString))
					using (var context = new XrmServiceContext(service) { MergeOption = MergeOption.NoTracking })
					{
						secureId = step.SecureId =
							((CreateResponse)context.Execute(
								new CreateRequest
								{
									Target = new SdkMessageProcessingStepSecureConfig
											 {
												 SecureConfig = step.SecureConfig
											 }
								})).id;
					}
				}
				else
				{
					//var updatedSecure = Context.SdkMessageProcessingStepSecureConfigSet.FirstOrDefault(
					//	entity => entity.SdkMessageProcessingStepSecureConfigId == step.SecureId)
					//                    ?? new SdkMessageProcessingStepSecureConfig();
					//if (updatedSecure.Id == Guid.Empty)
					//{
					//	updatedSecure.Id = step.SecureId;
					//}
					var updatedSecure = new SdkMessageProcessingStepSecureConfig
										{
											Id = step.SecureId,
											SecureConfig = step.SecureConfig
										};

					//Context.ConfirmAttached(updatedSecure);
					//Context.UpdateObject(updatedSecure);

					UpdateStatus("Updated secure config ... ");

					using (var service = GetConnection(ConnectionString))
					{
						service.Update(updatedSecure);
					}
				}
			}

			if (secureId == Guid.Empty)
			{
				updatedStep.SdkMessageProcessingStepSecureConfigId = null;
			}
			else
			{
				updatedStep.SdkMessageProcessingStepSecureConfigId
					= new EntityReference(SdkMessageProcessingStepSecureConfig.EntityLogicalName,
						secureId);
			}

			//Context.ConfirmAttached(updatedStep);
			//Context.UpdateObject(updatedStep);
			UpdateStatus("Updated step ... ");

			using (var service = GetConnection(ConnectionString))
			{
				service.Update(updatedStep);
			}

			//UpdateStatus("Saving updated step to CRM ... ");
			//Context.SaveChanges();

			UpdateStatus($"Finished updating step '{step.Name}'.", -1);
		}

		private void _DeleteStep(Guid stepId)
		{
			UpdateStatus($"Deleting step '{stepId}' in type ... ", 1);

			DeleteTree(stepId, Dependency.Enums.RequiredComponentType.SDKMessageProcessingStep);

			UpdateStatus("Finished deleting step. ID => " + stepId, -1);
		}

		private void _SetStepState(Guid stepId, SdkMessageProcessingStepState state)
		{
			var toggledState = state == SdkMessageProcessingStepState.Enabled
				? SdkMessageProcessingStepState.Disabled
				: SdkMessageProcessingStepState.Enabled;

			UpdateStatus($"Settings step state to '{toggledState}' ... ", 1);

			using (var service = GetConnection(ConnectionString))
			{
				service.Execute(
					new SetStateRequest
					{
						EntityMoniker = new EntityReference(SdkMessageProcessingStep.EntityLogicalName, stepId),
						State = toggledState.ToOptionSetValue(),
						Status = (toggledState == SdkMessageProcessingStepState.Enabled
							? SdkMessageProcessingStep.Enums.StatusCode.Enabled
							: SdkMessageProcessingStep.Enums.StatusCode.Disabled)
							.ToOptionSetValue()
					});
			}

			UpdateStatus("Finished setting step state. ID => " + stepId, -1);
		}

		#endregion

		#region Image CRM actions

		private void _CreateImage(CrmStepImage image)
		{
			UpdateStatus($"Creating image '{image.Name}' in step ... ", 1);

			var newImage = new SdkMessageProcessingStepImage
						   {
							   Name = image.Name,
							   EntityAlias = image.EntityAlias,
							   Attributes1 = image.AttributesSelectedString,
							   ImageType = image.ImageType.ToOptionSetValue(),
							   MessagePropertyName = image.Step.MessagePropertyName,
							   SdkMessageProcessingStepId = new EntityReference(
								   SdkMessageProcessingStep.EntityLogicalName, image.Step.Id)
						   };

			//using (var service = GetConnection(ConnectionString)) { var id = service.Create(newImage);

			UpdateStatus("Saving new image to CRM ... ");

			using (var service = GetConnection(ConnectionString))
			{
				image.Id = service.Create(newImage);
			}

			//Context.SaveChanges();
			//image.Id = newImage.Id;

			UpdateStatus($"Finished creating image '{image.Name}'.", -1);
		}

		private void _UpdateImage(CrmStepImage image)
		{
			UpdateStatus($"Updating image '{image.Name}' in step ... ", 1);

			var updatedImage =
				new SdkMessageProcessingStepImage
				{
					Id = image.Id,
					Name = image.Name,
					EntityAlias = image.EntityAlias,
					Attributes1 = image.AttributesSelectedString,
					ImageType = image.ImageType.ToOptionSetValue(),
					MessagePropertyName = image.Step.MessagePropertyName,
					SdkMessageProcessingStepId = new EntityReference(
						SdkMessageProcessingStep.EntityLogicalName, image.Step.Id)
				};

			UpdateStatus("Updating image ... ");

			using (var service = GetConnection(ConnectionString))
			{
				service.Update(updatedImage);
			}

			UpdateStatus($"Finished updating image '{image.Name}'.", -1);
		}

		private void _DeleteImage(Guid imageId)
		{
			UpdateStatus($"Deleting image '{imageId}' in type ... ", 1);

			DeleteTree(imageId, Dependency.Enums.RequiredComponentType.SDKMessageProcessingStepImage);

			UpdateStatus("Finished deleting image. ID => " + imageId, -1);
		}

		#endregion

		private void DeleteTree(Guid objectId, Dependency.Enums.RequiredComponentType type)
		{
			List<Entity> dependencies;

			using (var service = GetConnection(ConnectionString))
			{
				dependencies = ((RetrieveDependenciesForDeleteResponse)
					service.Execute(
						new RetrieveDependenciesForDeleteRequest
						{
							ComponentType = (int)type,
							ObjectId = objectId
						})).EntityCollection.Entities.ToList();
			}

			string logicalname;

			switch (type)
			{
				case Dependency.Enums.RequiredComponentType.PluginAssembly:
					logicalname = PluginAssembly.EntityLogicalName;
					break;
				case Dependency.Enums.RequiredComponentType.PluginType:
					logicalname = PluginType.EntityLogicalName;
					break;
				case Dependency.Enums.RequiredComponentType.SDKMessageProcessingStep:
					logicalname = SdkMessageProcessingStep.EntityLogicalName;
					break;
				case Dependency.Enums.RequiredComponentType.SDKMessageProcessingStepImage:
					dependencies.Clear();
					logicalname = SdkMessageProcessingStepImage.EntityLogicalName;
					break;
				default:
					throw new ArgumentOutOfRangeException("type", type,
						$"Please delete record with ID \"{objectId}\"" + " before attempting to delete this record.");
			}

			dependencies.ForEach(
				entity =>
				{
					var entityQ = entity.ToEntity<Dependency>();

					if (entityQ.DependentComponentObjectId != null)
					{
						UpdateStatus($"Deleting dependency '{entityQ.DependentComponentObjectId.Value}'"
							+ $" of type '{(Dependency.Enums.RequiredComponentType)entityQ.DependentComponentType.Value}' ... ",
							1);

						DeleteTree(entityQ.DependentComponentObjectId.Value,
							(Dependency.Enums.RequiredComponentType)entityQ.DependentComponentType.Value);

						UpdateStatus($"Finished deleting dependency '{entityQ.DependentComponentObjectId.Value}'.", -1);
					}
				});

			using (var service = GetConnection(ConnectionString))
			{
				service.Execute(
					new DeleteRequest
					{
						Target = new EntityReference(logicalname, objectId)
					});
			}
		}
	}

	public static class ContextExtensions
	{
		public static void ConfirmAttached(this XrmServiceContext context, Entity entity)
		{
			try
			{
				context.ReAttach(entity);
			}
			catch
			{
				try
				{
					context.Attach(entity);
				}
				catch
				{
					// ignored
				}
			}
		}
	}
}
