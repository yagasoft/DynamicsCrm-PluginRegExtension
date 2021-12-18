#region Imports

using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CrmPluginEntities;
using CrmPluginRegExt.AssemblyInfoLoader;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Yagasoft.CrmPluginRegistration.Assembly;
using Yagasoft.CrmPluginRegistration.Connection;
using Yagasoft.CrmPluginRegistration.Feedback;
using Yagasoft.CrmPluginRegistration.Logger;
using Yagasoft.CrmPluginRegistration.Model;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.CrmPluginRegistration
{
	public enum RegistrationEvent
	{
		Create,
		Update,
		Delete,
		Abort
	}

	/// <summary>
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public class AssemblyRegistration : INotifyPropertyChanged
	{
		#region Properties

		public readonly object LoggingLock = new object();
		public readonly object ActionLock = new object();

		public Settings Settings;

		public Exception Error
		{
			get => error;
			private set
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

		private AssemblyInfo assemblyInfo;

		public AssemblyInfo AssemblyInfo
		{
			get => assemblyInfo ?? GetAssemblyInfo();
			private set => assemblyInfo = value;
		}

		private readonly IConnectionManager connectionManager;
		private readonly IPluginRegLogger log;
		private readonly IFeedback feedback;

		private readonly string assemblyPath;
		private readonly string assemblyName;

		private int indentIndex;

		private Exception error;

		#endregion

		private void UpdateStatus(string message, int indexModifier = 0)
		{
			lock (LoggingLock)
			{
				indentIndex = indexModifier > 0 ? (indentIndex + indexModifier) : indentIndex;
				OnRegLogAdded($"{new string('>', indentIndex)} {message}");
				indentIndex = indexModifier < 0 ? (indentIndex + indexModifier) : indentIndex;
			}
		}

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public event EventHandler<RegistrationEvent> RegistrationActionTaken;

		protected virtual void OnRegActionTaken(RegistrationEvent e)
		{
			RegistrationActionTaken?.Invoke(this, e);
		}

		public event EventHandler<RegLogEventArgs> RegLogEntryAdded;

		protected virtual void OnRegLogAdded(string e)
		{
			RegLogEntryAdded?.Invoke(this, new RegLogEventArgs { Message = e });
		}

		protected virtual void OnRegLogAdded(RegLogEventArgs e)
		{
			RegLogEntryAdded?.Invoke(this, e);
		}

		#endregion

		public AssemblyRegistration(string assemblyPath, IConnectionManager connectionManager, IFeedback feedback)
		{
			this.connectionManager = connectionManager;

			log = new DefaultPluginRegLogger(OnRegLogAdded);

			this.assemblyPath = assemblyPath;
			assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
			GetAssemblyInfo();

			this.feedback = feedback;
		}

		public AssemblyRegistration(Settings settings, string assemblyPath, IConnectionManager connectionManager, IFeedback feedback)
			: this(assemblyPath, connectionManager, feedback)
		{
			Settings = settings;
		}

		#region Assembly actions

		/// <summary>
		///     Create the assembly in CRM with the contents of the assembly in the appropriate folder.
		/// </summary>
		public Guid CreateAssembly(bool sandbox)
		{
			lock (ActionLock)
			{
				return _CreateAssembly(GetAssemblyData(), sandbox);
			}
		}

		/// <summary>
		///     Update the assembly in CRM with the contents of the assembly in the appropriate folder.
		/// </summary>
		public void UpdateAssembly(bool? sandbox)
		{
			lock (ActionLock)
			{
				Id = CrmAssemblyHelpers.GetAssemblyId(assemblyName, connectionManager, log);
				_UpdateAssembly(GetAssemblyData(), sandbox);
			}
		}

		public void DeleteAssembly(Guid assemblyId)
		{
			lock (ActionLock)
			{
				Id = assemblyId;
				_DeleteAssembly();
			}
		}

		#endregion

		#region Step image actions

		public void CreateStepImage(CrmStepImage image)
		{
			lock (ActionLock)
			{
				_CreateImage(image);
			}
		}

		public void UpdateStepImage(CrmStepImage image)
		{
			lock (ActionLock)
			{
				_UpdateImage(image);
			}
		}

		public void DeleteStepImage(Guid imageId)
		{
			lock (ActionLock)
			{
				_DeleteImage(imageId);
			}
		}

		#endregion

		#region Type step actions

		public void CreateTypeStep(CrmTypeStep step)
		{
			lock (ActionLock)
			{
				_CreateStep(step);
			}
		}

		public void UpdateTypeStep(CrmTypeStep step)
		{
			lock (ActionLock)
			{
				_UpdateStep(step);
			}
		}

		public void DeleteTypeStep(Guid stepId)
		{
			lock (ActionLock)
			{
				_DeleteStep(stepId);
			}
		}

		public void SetTypeStepState(Guid stepId, SdkMessageProcessingStepState state)
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
			var localVersion = GetAssemblyVersion();
			UpdateStatus($"Creating assembly (v{localVersion}) ... ", 1);

			var matchingAssembly = GetMatchingCrmAssembly(localVersion);
			var matchingVersion = matchingAssembly?.Version;

			if (matchingVersion != null)
			{
				UpdateStatus($"Found an existing match on CRM, updating instead ... ");
				UpdateAssembly(sandbox);
				return Id;
			}

			var cultureInfo = GetAssemblyCultureInfo();

			var newAssembly =
				new PluginAssembly
				{
					Name = assemblyName,
					IsolationMode = (sandbox
						? PluginAssembly.Enums.IsolationMode.Sandbox
						: PluginAssembly.Enums.IsolationMode.None).ToOptionSetValue(),
					Content = Convert.ToBase64String(assemblyData),
					SourceType = PluginAssembly.Enums.SourceType.Database.ToOptionSetValue(),
					Culture = cultureInfo.LCID == CultureInfo.InvariantCulture.LCID
						? "neutral"
						: cultureInfo.Name
				};

			UpdateStatus("Saving new assembly to CRM ...");
			Id = connectionManager.Get().Create(newAssembly);
			AddNewTypes(GetExistingTypeNames(CrmAssemblyHelpers.GetCrmTypes(connectionManager, log, Id)));

			UpdateStatus("Finished creating assembly. ID => " + Id, -1);
			OnRegActionTaken(RegistrationEvent.Create);

			return Id;
		}

		private void _UpdateAssembly(byte[] assemblyData, bool? sandbox, bool isSkipUgrade = false)
		{
			UpdateStatus("Updating assembly ... ", 1);

			if (!isSkipUgrade && UpgradeAssembly(assemblyData, ref sandbox))
			{
				return;
			}

			var existingTypes = CrmAssemblyHelpers.GetCrmTypes(connectionManager, log, Id);
			DeleteObsoleteTypes(existingTypes);

			var updatedAssembly =
				new PluginAssembly
				{
					Id = Id,
					Content = Convert.ToBase64String(assemblyData)
				};

			if (sandbox.HasValue)
			{
				updatedAssembly.IsolationMode =
					new OptionSetValue((int)(sandbox.Value
						? PluginAssembly.Enums.IsolationMode.Sandbox
						: PluginAssembly.Enums.IsolationMode.None));
			}

			UpdateStatus("Updating assembly ... ");
			connectionManager.Get().Update(updatedAssembly);

			existingTypes = CrmAssemblyHelpers.GetCrmTypes(connectionManager, log, Id);
			var existingTypeNames = GetExistingTypeNames(existingTypes);
			AddNewTypes(existingTypeNames);

			UpdateStatus("Finished updating assembly.", -1);
			OnRegActionTaken(RegistrationEvent.Update);
		}

		private bool UpgradeAssembly(byte[] assemblyData, ref bool? sandbox)
		{
			var existingAssemblies = CrmAssemblyHelpers.GetCrmAssembly(assemblyName, connectionManager, log);

			var oldestAssembly = existingAssemblies.FirstOrDefault();
			var oldestVersion = oldestAssembly?.Version.IsFilled() == true ? new Version(oldestAssembly.Version) : null;
			UpdateStatus($"Existing version: v{oldestVersion} ... ");

			sandbox ??= (PluginAssembly.Enums.IsolationMode?)oldestAssembly?.IsolationMode.Value
				== PluginAssembly.Enums.IsolationMode.Sandbox;

			var newVersion = GetAssemblyVersion();
			UpdateStatus($"New version: v{newVersion} ... ");

			if (oldestVersion == null)
			{
				UpdateStatus($"Failed to find any assembly on CRM, creating instead ... ");
				CreateAssembly(sandbox == true);
				return true;
			}

			if (newVersion.Major > oldestVersion.Major || newVersion.Minor > oldestVersion.Minor)
			{
				var message =
					$@"Local assembly ({newVersion}) is an UPGRADE of the CRM assembly ({oldestVersion}).

If you press 'yes':
- A new assembly will be created

- Plugins:
  -- Existing steps will be moved to the new assembly

- WF Custom Steps:
  -- Existing WF XAML will be updated to reference the new new version
  -- If out parameters were removed, it is HIGHLY recommended to remove all their references in the WFs first
  -- Failing to do so, will cause 'possible' issues in those WFs
  -- BE CAREFUL!

- Finally, the old assembly will be deleted

Do you want to continue with the upgrade (press 'yes') or exit (press 'no')?";

				if (feedback.IsConfirmed(message))
				{
					UpdateStatus("Upgrading assembly ... ");

					UpdateStatus("Retrieving existing plugin steps ... ");
					var existingSteps = CrmAssemblyHelpers
						.GetCrmSteps(connectionManager, log, oldestAssembly.Id);
					UpdateStatus($"Found: {existingSteps.Count}.");

					var existingAssemblyFullName = $"{oldestAssembly.Name}, Version={oldestAssembly.Version},"
						+ $" Culture={oldestAssembly.Culture}, PublicKeyToken={oldestAssembly.PublicKeyToken}";

					var existingWfs = RetrieveExistingWfs(existingAssemblyFullName);

					var oldId = oldestAssembly.Id;

					var existingAssembly =
						existingAssemblies
							.FirstOrDefault(
								a =>
								{
									var version = a?.Version.IsFilled() == true ? new Version(a.Version) : null;
									return version == newVersion;
								});

					if (existingAssembly == null)
					{
						UpdateStatus($"Creating new assembly (v{newVersion}) ... ");
						CreateAssembly(sandbox == true);
					}
					else
					{
						UpdateStatus($"Updating assembly (v{newVersion}) ... ");
						Id = existingAssembly.Id;
						_UpdateAssembly(assemblyData, sandbox, true);
					}

					var newId = Id;

					if (existingSteps.Any())
					{
						UpdateStatus($"Moving steps from old assembly (v{oldestVersion}) to new one (v{newVersion}) ... ");
						MovePluginSteps(newId, existingSteps);
					}

					if (existingWfs.Any() && UpgradeWfDefinitions(existingAssemblyFullName, existingWfs))
					{
						return true;
					}

					UpdateStatus($"Deleting old assembly (v{oldestVersion}) ... ");
					DeleteAssembly(oldId);

					Id = newId;

					UpdateStatus("Finished upgrading assembly.", -1);
					OnRegActionTaken(RegistrationEvent.Update);
				}
				else
				{
					OnRegActionTaken(RegistrationEvent.Abort);
				}

				return true;
			}

			return false;
		}

		private void MovePluginSteps(Guid newId, IReadOnlyList<SdkMessageProcessingStep> existingSteps)
		{
			var newTypes = CrmAssemblyHelpers.GetCrmTypes(connectionManager, log, newId);

			foreach (var groupedSteps in existingSteps.GroupBy(s => s.EventHandler?.Name))
			{
				var typeName = groupedSteps.Key;

				UpdateStatus($"Trying to find new ID for plugin type: {typeName} ... ", 1);
				var newType = newTypes.FirstOrDefault(t => t.Name == typeName);

				if (newType == null)
				{
					UpdateStatus($"Unable to find the new ID for type: {typeName ?? "NULL"}.", -1);
					continue;
				}

				Parallel.ForEach(groupedSteps, new ParallelOptions { MaxDegreeOfParallelism = groupedSteps.Count() },
					step =>
					{
						var service = connectionManager.Get();

						UpdateStatus($"Moving step: {step.Id} ... ");
						service.Update(
							new SdkMessageProcessingStep
							{
								Id = step.Id,
								EventHandler = newType.ToEntityReference()
							});
					});

				UpdateStatus($"Finished moving steps of plugin type: {typeName}.", -1);
			}
		}

		private bool UpgradeWfDefinitions(string existingAssemblyFullName, IReadOnlyList<Workflow> existingWfs)
		{
			string message;
			UpdateStatus($"Updating WF definitions to the new version ... ");

			var newAssembly = CrmAssemblyHelpers.GetCrmAssembly(assemblyName, connectionManager, log).LastOrDefault();

			if (newAssembly == null)
			{
				throw new Exception($"Cannot find the new assembly in CRM ({Id})");
			}

			var newAssemblyFullName = $"{newAssembly.Name}, Version={newAssembly.Version},"
				+ $" Culture={newAssembly.Culture}, PublicKeyToken={newAssembly.PublicKeyToken}";

			UpdateStatus($"Replacing '{existingAssemblyFullName}' with '{newAssemblyFullName}' ... ", 1);

			var activeWfs = existingWfs
				.Where(w => w.StateCode == WorkflowState.Activated
					&& (Workflow.Enums.StatusCode?)w.StatusCode?.Value == Workflow.Enums.StatusCode.Activated).ToArray();

			if (activeWfs.Any())
			{
				var warning = $@"The following WFs are active, and they must be deactivated before the upgrade:
- {activeWfs.Select(w => $"{w.Name} ({w.Id})").StringAggregate("\r\n- ")}";

				message = $@"{warning}

Do you want to deactivate (press 'yes') them before continuing, or exit (press 'no')?";

				if (feedback.IsConfirmed(message))
				{
					UpdateStatus($"Deactivating WFs ... ");

					Parallel.ForEach(activeWfs, new ParallelOptions { MaxDegreeOfParallelism = activeWfs.Length },
						wf =>
						{
							var service = connectionManager.Get();

							UpdateStatus($"Deactivating WF: {wf.Name} ({wf.Id}) ... ");
							service.Update(
								new Workflow
								{
									Id = wf.Id,
									StateCode = WorkflowState.Draft,
									StatusCode = Workflow.Enums.StatusCode.Draft.ToOptionSetValue()
								});
						});
				}
				else
				{
					OnRegActionTaken(RegistrationEvent.Abort);
					return true;
				}
			}

			Parallel.ForEach(existingWfs, new ParallelOptions { MaxDegreeOfParallelism = existingWfs.Count },
				wf =>
				{
					var service = connectionManager.Get();

					UpdateStatus($"Updating WF: {wf.Name} ({wf.Id}) ... ");
					service.Update(
						new Workflow
						{
							Id = wf.Id,
							Xaml = wf.Xaml.Replace(existingAssemblyFullName, newAssemblyFullName)
						});
				});

			UpdateStatus($"Finished updating WF definitions.", -1);
			return false;
		}

		private void _DeleteAssembly()
		{
			UpdateStatus("Deleting assembly ... ", 1);

			DeleteTree(Id, Dependency.Enums.RequiredComponentType.PluginAssembly);

			UpdateStatus("Finished deleting assembly. ID => " + Id, -1);

			Id = Guid.Empty;

			OnRegActionTaken(RegistrationEvent.Delete);
		}

		private void AddNewTypes(IReadOnlyList<string> existingTypeNames)
		{
			UpdateStatus("Adding new types ... ", 1);

			var pluginClasses = GetClasses<IPlugin>();
			var wfClasses = GetClasses<CodeActivity>();

			var assemblyVersion = GetAssemblyVersion();

			foreach (var pluginType in pluginClasses.Where(pluginType => !existingTypeNames.Contains(pluginType)))
			{
				var className = pluginType.Split('.').LastOrDefault();

				UpdateStatus($"Adding plugin '{className}' ... ", 1);

				var newType =
					new PluginType
					{
						PluginTypeId = Guid.NewGuid(),
						Name = pluginType,
						TypeName = pluginType,
						FriendlyName = $"{className} ({assemblyVersion})",
						PluginAssemblyId = new EntityReference(PluginAssembly.EntityLogicalName, Id)
					};

				connectionManager.Get().Create(newType);

				UpdateStatus($"Finished adding plugin '{className}'.", -1);
			}

			// create new types
			foreach (var pluginType in wfClasses.Where(pluginType => !existingTypeNames.Contains(pluginType)))
			{
				var className = pluginType.Split('.').LastOrDefault();

				UpdateStatus($"Adding custom step '{className}' ... ", 1);

				var newType =
					new PluginType
					{
						PluginTypeId = Guid.NewGuid(),
						Name = pluginType,
						TypeName = pluginType,
						FriendlyName = $"{className} ({assemblyVersion})",
						PluginAssemblyId = new EntityReference(PluginAssembly.EntityLogicalName, Id),
						WorkflowActivityGroupName = string.Format(CultureInfo.InvariantCulture, "{0} ({1})",
							assemblyName, GetAssemblyVersion())
					};

				connectionManager.Get().Create(newType);

				UpdateStatus($"Finished adding custom step '{className}'.", -1);
			}

			UpdateStatus("Finished adding new types.", -1);
		}

		private void DeleteObsoleteTypes(IReadOnlyList<PluginType> existingTypes)
		{
			var pluginClasses = GetClasses<IPlugin>();
			var wfClasses = GetClasses<CodeActivity>();

			var nonExistentTypes = existingTypes
				.Where(pluginType => !pluginClasses.Contains(pluginType.TypeName)
					&& !wfClasses.Contains(pluginType.TypeName)).ToList();

			// delete non-existing types
			if (nonExistentTypes.Any())
			{
				if (feedback.IsConfirmed("Please confirm that you want to DELETE non-existent types in this assembly." +
					" This means that all its steps will be deleted.\r\n\r\n"
					+ "Non-existent plugins:\r\n"
					+ nonExistentTypes.Select(t => $"-{t.TypeName}").StringAggregate("\r\n")))
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
				using (var context = new XrmServiceContext(connectionManager.Get()) { MergeOption = MergeOption.NoTracking })
				{
					secureId = step.SecureId =
						((CreateResponse)context.Execute(
							new CreateRequest
							{
								Target =
									new SdkMessageProcessingStepSecureConfig
									{
										SecureConfig = step.SecureConfig
									}
							})).id;
				}
			}

			var newStep =
				new SdkMessageProcessingStep
				{
					SdkMessageId = new EntityReference(SdkMessage.EntityLogicalName, step.MessageId),
					Name = step.Name,
					FilteringAttributes = step.Attributes ?? "",
					Rank = step.ExecutionOrder,
					Description = step.Description ?? "",
					Stage = step.Stage.ToOptionSetValue(),
					Mode = step.Mode.ToOptionSetValue(),
					SupportedDeployment = step.Deployment.ToOptionSetValue(),
					AsyncAutoDelete = step.IsDeleteJob,
					Configuration = step.UnsecureConfig ?? "",
					EventHandler = new EntityReference(PluginType.EntityLogicalName, step.Type.Id)
				};

			if (step.UserId != Guid.Empty)
			{
				newStep.ImpersonatingUserId = new EntityReference(SystemUser.EntityLogicalName, step.UserId);
			}

			if (step.FilterId != Guid.Empty)
			{
				newStep.SdkMessageFilterId = new EntityReference(SdkMessageFilter.EntityLogicalName, step.FilterId);
			}

			if (secureId != Guid.Empty)
			{
				newStep.SdkMessageProcessingStepSecureConfigId =
					new EntityReference(SdkMessageProcessingStepSecureConfig.EntityLogicalName, secureId);
			}

			UpdateStatus("Saving new step to CRM ... ");

			step.Id = connectionManager.Get().Create(newStep);

			UpdateStatus($"Finished creating step '{step.Name}'.", -1);
		}

		private void _UpdateStep(CrmTypeStep step)
		{
			UpdateStatus($"Updating step '{step.Name}' in type ... ", 1);

			var updatedStep =
				new SdkMessageProcessingStep
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
				updatedStep.ImpersonatingUserId = new EntityReference(SystemUser.EntityLogicalName, step.UserId);
			}

			if (step.FilterId == Guid.Empty)
			{
				updatedStep.SdkMessageFilterId = null;
			}
			else
			{
				updatedStep.SdkMessageFilterId = new EntityReference(SdkMessageFilter.EntityLogicalName, step.FilterId);
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
					using (var context = new XrmServiceContext(connectionManager.Get()) { MergeOption = MergeOption.NoTracking })
					{
						secureId = step.SecureId =
							((CreateResponse)context.Execute(
								new CreateRequest
								{
									Target =
										new SdkMessageProcessingStepSecureConfig
										{
											SecureConfig = step.SecureConfig
										}
								})).id;
					}
				}
				else
				{
					var updatedSecure =
						new SdkMessageProcessingStepSecureConfig
						{
							Id = step.SecureId,
							SecureConfig = step.SecureConfig
						};

					UpdateStatus("Updated secure config ... ");

					connectionManager.Get().Update(updatedSecure);
				}
			}

			updatedStep.SdkMessageProcessingStepSecureConfigId =
				secureId == Guid.Empty
					? null
					: new EntityReference(SdkMessageProcessingStepSecureConfig.EntityLogicalName, secureId);

			UpdateStatus("Updated step ... ");

			connectionManager.Get().Update(updatedStep);

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
			var toggledState =
				state == SdkMessageProcessingStepState.Enabled
					? SdkMessageProcessingStepState.Disabled
					: SdkMessageProcessingStepState.Enabled;

			UpdateStatus($"Setting step state to '{toggledState}' ... ", 1);

			connectionManager.Get().Execute(
				new SetStateRequest
				{
					EntityMoniker = new EntityReference(SdkMessageProcessingStep.EntityLogicalName, stepId),
					State = toggledState.ToOptionSetValue(),
					Status =
						(toggledState == SdkMessageProcessingStepState.Enabled
							? SdkMessageProcessingStep.Enums.StatusCode.Enabled
							: SdkMessageProcessingStep.Enums.StatusCode.Disabled).ToOptionSetValue()
				});

			UpdateStatus("Finished setting step state. ID => " + stepId, -1);
		}

		#endregion

		#region Image CRM actions

		private void _CreateImage(CrmStepImage image)
		{
			UpdateStatus($"Creating image '{image.Name}' in step ... ", 1);

			var newImage =
				new SdkMessageProcessingStepImage
				{
					Name = image.Name,
					EntityAlias = image.EntityAlias,
					Attributes1 = image.AttributesSelectedString,
					ImageType = image.ImageType.ToOptionSetValue(),
					MessagePropertyName = image.Step.MessagePropertyName,
					SdkMessageProcessingStepId =
						new EntityReference(SdkMessageProcessingStep.EntityLogicalName, image.Step.Id)
				};

			UpdateStatus("Saving new image to CRM ... ");

			image.Id = connectionManager.Get().Create(newImage);

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
					SdkMessageProcessingStepId =
						new EntityReference(SdkMessageProcessingStep.EntityLogicalName, image.Step.Id)
				};

			UpdateStatus("Updating image ... ");

			connectionManager.Get().Update(updatedImage);

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
			var dependencies = ((RetrieveDependenciesForDeleteResponse)
				connectionManager.Get().Execute(
					new RetrieveDependenciesForDeleteRequest
					{
						ComponentType = (int)type,
						ObjectId = objectId
					})).EntityCollection.Entities.ToList();

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

			connectionManager.Get().Execute(
				new DeleteRequest
				{
					Target = new EntityReference(logicalname, objectId)
				});
		}

		private PluginAssembly GetMatchingCrmAssembly(Version localVersion)
		{
			var matchingAssembly = CrmAssemblyHelpers.GetCrmAssembly(assemblyName, connectionManager, log)
				.FirstOrDefault(
					a =>
					{
						var version = a?.Version.IsFilled() == true ? new Version(a.Version) : null;
						return version == localVersion;
					});
			return matchingAssembly;
		}

		private IReadOnlyList<string> GetExistingTypeNames(IReadOnlyList<PluginType> existingTypes)
		{
			var existingTypeNames = existingTypes.Select(type => type.TypeName).ToList();
			return existingTypeNames;
		}

		private IReadOnlyList<Workflow> RetrieveExistingWfs(string existingAssemblyFullName)
		{
			UpdateStatus("Retrieving existing WFs ... ");

			using var xrmContext = new XrmServiceContext(connectionManager.Get()) { MergeOption = MergeOption.NoTracking };

			var existingWfs =
				(from wf in xrmContext.WorkflowSet
				 where wf.Xaml.Contains(existingAssemblyFullName)
					 && (wf.Type.Value == (int)Workflow.Enums.Type.Definition
						 || wf.Type.Value == (int)Workflow.Enums.Type.Template)
				 select
					 new Workflow
					 {
						 WorkflowId = wf.WorkflowId,
						 Name = wf.Name,
						 Xaml = wf.Xaml,
						 StateCode = wf.StateCode,
						 StatusCode = wf.StatusCode
					 }).ToArray();

			UpdateStatus($"Found: {existingWfs.Length}.");

			return existingWfs;
		}

		#region Assembly

		private IReadOnlyList<string> GetClasses<TClassType>()
		{
			return AssemblyInfo.Classes
				.Where(c => c.BaseType == typeof(TClassType).FullName)
				.Select(c => c.Type).ToArray();
		}

		private string GetAssemblyName()
		{
			return AssemblyInfo.Name;
		}

		private Version GetAssemblyVersion()
		{
			return new Version(AssemblyInfo.Version);
		}

		private CultureInfo GetAssemblyCultureInfo()
		{
			return AssemblyInfo.CultureInfo;
		}

		private AssemblyInfo GetAssemblyInfo()
		{
			return AssemblyInfo = new AssemblyInfoLoader().GetAssemblyInfo(assemblyPath,
				System.Reflection.Assembly.GetExecutingAssembly().CodeBase, typeof(IPlugin).FullName, typeof(CodeActivity).FullName);
		}

		private byte[] GetAssemblyData()
		{
			return File.ReadAllBytes(assemblyPath);
		}

		#endregion
	}
}
