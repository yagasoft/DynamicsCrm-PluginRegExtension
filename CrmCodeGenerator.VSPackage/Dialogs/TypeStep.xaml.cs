#region Imports

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CrmPluginEntities;
using CrmPluginRegExt.VSPackage.Helpers;
using CrmPluginRegExt.VSPackage.Model;

#endregion

namespace CrmPluginRegExt.VSPackage.Dialogs
{
	/// <summary>
	///     Interaction logic for TypeStep.xaml
	/// </summary>
	public partial class TypeStep : INotifyPropertyChanged, IDataErrorInfo
	{
		#region Properties

		private ComboUser user;
		private List<string> entityList;
		private List<string> messageList;
		private List<ComboUser> userList;
		private ObservableCollection<string> attributeList = new ObservableCollection<string>();
		private ObservableCollection<string> attributesSelected = new ObservableCollection<string>();

		public bool IsInitEntity { get; set; }
		public bool IsInitMessage { get; set; }
		public bool IsUpdate { get; set; }
		public XrmServiceContext Context { get; set; }
		public CrmTypeStep CrmStep { get; set; }

		public string StepName
		{
			get { return CrmStep.Name; }
			set
			{
				CrmStep.Name = value;
				OnPropertyChanged("StepName");
			}
		}

		public List<string> EntityList
		{
			get { return entityList; }
			set
			{
				entityList = value;
				OnPropertyChanged("EntityList");
			}
		}

		public List<string> MessageList
		{
			get { return messageList; }
			set
			{
				messageList = value;
				OnPropertyChanged("MessageList");
			}
		}

		public List<ComboUser> UserList
		{
			get { return userList; }
			set
			{
				userList = value;
				OnPropertyChanged("UserList");
			}
		}

		public string Entity
		{
			get { return CrmStep.Entity; }
			set
			{
				CrmStep.Entity = value;
				OnPropertyChanged("Entity");
			}
		}

		public string Message
		{
			get { return CrmStep.Message; }
			set
			{
				CrmStep.Message = value;
				OnPropertyChanged("Message");
			}
		}

		public int ExecutionOrder
		{
			get { return CrmStep.ExecutionOrder; }
			set
			{
				CrmStep.ExecutionOrder = value < 1 ? 1 : value;
				OnPropertyChanged("ExecutionOrder");
			}
		}

		public string Description
		{
			get { return CrmStep.Description; }
			set
			{
				CrmStep.Description = value;
				OnPropertyChanged("Description");
			}
		}

		public ComboUser User
		{
			get { return user; }
			set
			{
				user = value;
				CrmStep.UserId = user.Id;
				OnPropertyChanged("User");
			}
		}

		public bool IsPreValidation
		{
			get { return CrmStep.Stage == SdkMessageProcessingStep.Enums.Stage.Prevalidation; }
			set
			{
				if (value)
				{
					CrmStep.Stage = SdkMessageProcessingStep.Enums.Stage.Prevalidation;
					IsSync = true;
				}

				OnPropertyChanged("IsPreValidation");
				OnPropertyChanged("IsAsyncEnabled");
			}
		}

		public bool IsPreOperation
		{
			get { return CrmStep.Stage == SdkMessageProcessingStep.Enums.Stage.Preoperation; }
			set
			{
				if (value)
				{
					CrmStep.Stage = SdkMessageProcessingStep.Enums.Stage.Preoperation;
					IsSync = true;
				}

				OnPropertyChanged("IsPreOperation");
				OnPropertyChanged("IsAsyncEnabled");
			}
		}

		public bool IsPostOperation
		{
			get { return CrmStep.Stage == SdkMessageProcessingStep.Enums.Stage.Postoperation; }
			set
			{
				if (value)
				{
					CrmStep.Stage = SdkMessageProcessingStep.Enums.Stage.Postoperation;
				}

				OnPropertyChanged("IsPostOperation");
			}
		}

		public bool IsAsync
		{
			get { return CrmStep.Mode == SdkMessageProcessingStep.Enums.Mode.Asynchronous; }
			set
			{
				if (value)
				{
					CrmStep.Mode = SdkMessageProcessingStep.Enums.Mode.Asynchronous;
				}

				OnPropertyChanged("IsAsync");
			}
		}

		public bool IsSync
		{
			get { return CrmStep.Mode == SdkMessageProcessingStep.Enums.Mode.Synchronous; }
			set
			{
				if (value)
				{
					CrmStep.Mode = SdkMessageProcessingStep.Enums.Mode.Synchronous;
					IsDeleteJob = false;
				}

				OnPropertyChanged("IsSync");
			}
		}

		public bool IsAsyncEnabled
		{
			get { return !IsPreValidation && !IsPreOperation; }
		}

		public bool IsServer
		{
			get
			{
				return CrmStep.Deployment == SdkMessageProcessingStep.Enums.SupportedDeployment.Both
				       || CrmStep.Deployment == SdkMessageProcessingStep.Enums.SupportedDeployment.ServerOnly;
			}

			set
			{
				if (value && IsOffline)
				{
					CrmStep.Deployment = SdkMessageProcessingStep.Enums.SupportedDeployment.Both;
				}
				else if (value)
				{
					CrmStep.Deployment = SdkMessageProcessingStep.Enums.SupportedDeployment.ServerOnly;
				}
				else if (IsOffline)
				{
					CrmStep.Deployment = SdkMessageProcessingStep.Enums.SupportedDeployment.MicrosoftDynamics365ClientforOutlookOnly;
				}

				OnPropertyChanged("IsServer");
			}
		}

		public bool IsOffline
		{
			get
			{
				return CrmStep.Deployment == SdkMessageProcessingStep.Enums.SupportedDeployment.Both
				       ||
				       CrmStep.Deployment ==
				       SdkMessageProcessingStep.Enums.SupportedDeployment.MicrosoftDynamics365ClientforOutlookOnly;
			}

			set
			{
				if (IsServer && value)
				{
					CrmStep.Deployment = SdkMessageProcessingStep.Enums.SupportedDeployment.Both;
				}
				else if (IsServer)
				{
					CrmStep.Deployment = SdkMessageProcessingStep.Enums.SupportedDeployment.ServerOnly;
				}
				else if (value)
				{
					CrmStep.Deployment = SdkMessageProcessingStep.Enums.SupportedDeployment.MicrosoftDynamics365ClientforOutlookOnly;
				}

				OnPropertyChanged("IsOffline");
			}
		}

		public string UnsecureConfig
		{
			get { return CrmStep.UnsecureConfig; }
			set
			{
				CrmStep.UnsecureConfig = value;
				OnPropertyChanged("UnsecureConfig");
			}
		}

		public string SecureConfig
		{
			get { return CrmStep.SecureConfig; }
			set
			{
				CrmStep.SecureConfig = value;
				OnPropertyChanged("SecureConfig");
			}
		}

		public bool IsDeleteJob
		{
			get { return CrmStep.IsDeleteJob; }
			set
			{
				CrmStep.IsDeleteJob = value;
				OnPropertyChanged("IsDeleteJob");
			}
		}

		public ObservableCollection<string> AttributeList
		{
			get { return attributeList; }
			set
			{
				attributeList = value;
				OnPropertyChanged("AttributeList");
			}
		}

		public ObservableCollection<string> AttributesSelected
		{
			get { return attributesSelected; }
			set
			{
				attributesSelected = value;
				OnPropertyChanged("AttributesSelected");
			}
		}

		public string AttributesSelectedString
		{
			get
			{
				if (!IsInitEntity || !IsInitMessage)
				{
					return CrmStep.Attributes;
				}

				if (AttributesSelected.Count == AttributeList.Count)
				{
					return "";
				}

				var sb = new StringBuilder();

				foreach (var value in AttributesSelected)
				{
					if (sb.Length != 0)
					{
						sb.Append(',');
					}
					sb.Append(value);
				}

				return sb.ToString();
			}
			set
			{
				if (!IsInitEntity || !IsInitMessage)
				{
					value = CrmStep.Attributes;
				}

				// if the string is empty, then all attributes are selected!
				if (string.IsNullOrEmpty(value))
				{
					if (Message == "Update")
					{
						AttributesSelected = new ObservableCollection<string>(AttributeList);
					}
					else
					{
						AttributesSelected = new ObservableCollection<string>();
						Dispatcher.Invoke(() => Attributes.UpdateLayout());
					}
				}
				else
				{
					AttributesSelected = new ObservableCollection<string>(value.Split(',')
						.Select(p => p.Trim()));
				}

				OnPropertyChanged("AttributesSelectedString");
			}
		}

		private bool entityChanged;
		private bool messageChanged;

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region Init

		public TypeStep(CrmTypeStep crmStep, XrmServiceContext context)
		{
			Context = context;
			CrmStep = crmStep;

			InitializeComponent();
		}

		private void DialogWindow_Loaded(object sender, RoutedEventArgs e)
		{
			DataContext = this;

			new Thread(() =>
			           {
				           try
				           {
					           ShowBusy("Getting user list ...");
					           UserList = new List<ComboUser>(CrmDataHelper
						           .GetUsers(Context));

					           ShowBusy("Getting entity list ...");
					           EntityList = new List<string>(CrmDataHelper
						           .GetEntityNames(Context));

							   User = CrmStep.UserId == Guid.Empty
										  ? UserList.First()
										  : UserList.First(userQ => userQ.Id == CrmStep.UserId);

							   Entity = string.IsNullOrEmpty(Entity) ? "none" : Entity;

							   Dispatcher.Invoke(() =>ComboBoxEntities.Focus());
						   }
						   catch (Exception exception)
				           {
					           PopException(exception);
				           }
				           finally
				           {
					           HideBusy();
				           }
			           }).Start();
		}

		#endregion

		#region UI Events

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateFields())
			{
				return;
			}

			UpdateStepName();

			CrmStep.Attributes = AttributesSelectedString;

			IsUpdate = true;
			Dispatcher.InvokeAsync(Close);
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			Dispatcher.InvokeAsync(Close);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
			}

			base.OnKeyDown(e);
		}

		private void ComboBoxEntities_CbSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			entityChanged = true;

			if (!IsInitEntity)
			{
				ProcessEntityChange();
			}
		}

		private void ComboBoxEntities_OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			ProcessEntityChange();
		}

		private void ComboBoxMessages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			messageChanged = true;

			if (!IsInitMessage)
			{
				ProcessMessageChange();
			}
		}

		private void ComboBoxMessages_OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			ProcessMessageChange(); 
		}

		#endregion

		private void LoadAttributes()
		{
			string entitySelected = null;
			string messageSelected = null;

			Dispatcher.Invoke(() =>
			                  {
				                  entitySelected = (string)ComboBoxEntities.SelectedItem;
				                  messageSelected = (string)ComboBoxMessages.SelectedItem;
			                  });

			if (messageSelected == "Update")
			{
				try
				{
					ShowBusy("Getting attribute list ...");
					AttributeList = new ObservableCollection<string>(CrmDataHelper
						.GetEntityFieldNames(entitySelected, Context));
					AttributesSelectedString = CrmStep.Attributes;
				}
				catch (Exception exception)
				{
					PopException(exception);
				}
				finally
				{
					HideBusy();
				}
			}
			else
			{
				AttributesSelectedString = null;
			}
		}

		private void ProcessEntityChange()
		{
			if (!entityChanged)
			{
				return;
			}

			var entitySelected = (string)ComboBoxEntities.SelectedItem;

			new Thread(() =>
			           {
				           if (!string.IsNullOrEmpty(entitySelected))
				           {
					           try
					           {
						           ShowBusy("Getting message list ...");
						           MessageList = new List<string>(CrmDataHelper.GetMessageNames(entitySelected, Context));
						           
								   Dispatcher.Invoke(() => ComboBoxMessages.ItemsSource = MessageList);

						           IsInitEntity = true;
							   }
							   catch (Exception exception)
					           {
						           PopException(exception);
					           }
					           finally
					           {
						           HideBusy();
					           }
				           }

				           entityChanged = false;
			           }).Start();
		}

		private void ProcessMessageChange()
		{
			if (!messageChanged)
			{
				return;
			}

			var messageSelected = (string)ComboBoxMessages.SelectedItem;

			Attributes.IsEnabled = messageSelected == "Update";

			new Thread(() =>
			           {
				           IsInitMessage = true;
				           messageChanged = false;

						   LoadAttributes();
					   }).Start();
		}

		private void UpdateStepName()
		{
			var entitySelected = (string)ComboBoxEntities.SelectedItem;
			var messageSelected = (string)ComboBoxMessages.SelectedItem;
			var typeName = CrmStep.Type.Name;
			var execOrder = ExecutionOrder;
			var stage = IsPreValidation ? "PreVal" : (IsPreOperation ? "PreOp" : "PostOp");
			var mode = IsSync ? "Sync" : "Async";

			//if (CanFormStepName(entitySelected, messageSelected) && IsNameOverwritable(StepName))
			//{
				StepName = GetStepName(typeName, messageSelected, entitySelected,
					execOrder.ToString(), stage, mode);
			//}
		}

		#region Step name formation

		//private static bool CanFormStepName(string entitySelected, string messageSelected)
		//{
		//	return !string.IsNullOrEmpty(entitySelected) && !string.IsNullOrEmpty(messageSelected);
		//}

		//private static bool IsNameOverwritable(string stepName)
		//{
		//	return string.IsNullOrEmpty(stepName);
		//}

		private static string GetStepName(string typeName, string messageName, string entityName, string execOrder, string stage, string mode)
		{
			var builder = new StringBuilder();

			builder.Append(typeName);
			builder.Append(": ");
			builder.Append(messageName);
			builder.Append(" of ");
			builder.Append(entityName == "none" ? "any entity" : entityName);
			builder.Append(": ");
			builder.Append(stage);
			builder.Append(" ");
			builder.Append(mode);
			builder.Append(" at ");
			builder.Append(execOrder);

			return builder.ToString();
		}

		#endregion

		#region Status and alert stuff

		private void PopException(Exception exception)
		{
			Dispatcher.Invoke(() =>
			                  {
				                  var message = exception.Message
				                                + (exception.InnerException != null ? "\n" + exception.InnerException.Message : "");
				                  MessageBox.Show(message, exception.GetType().FullName, MessageBoxButton.OK, MessageBoxImage.Error);
			                  });
		}

		private void PopAlert(string title, string message, MessageBoxImage severity)
		{
			Dispatcher.Invoke(() => { MessageBox.Show(message, title, MessageBoxButton.OK, severity); });
		}

		private void ShowBusy(string message)
		{
			Dispatcher.Invoke(() =>
			                  {
				                  BusyIndicator.IsBusy = true;
				                  BusyIndicator.BusyContent =
					                  string.IsNullOrEmpty(message) ? "Please wait ..." : message;
			                  }, DispatcherPriority.Send);
		}

		private void HideBusy()
		{
			Dispatcher.Invoke(() =>
			                  {
				                  BusyIndicator.IsBusy = false;
				                  BusyIndicator.BusyContent = "Please wait ...";
			                  }, DispatcherPriority.Send);
		}

		#endregion

		#region Validation

		private bool ValidateFields()
		{
			if (GridInputs.Children.Cast<DependencyObject>().Any(Validation.GetHasError))
			{
				PopAlert("Validation Error!",
					"All required fields must be populated. Please review red bordered fields.", MessageBoxImage.Error);

				return false;
			}

			if (!IsServer && !IsOffline)
			{
				PopAlert("Validation Error!",
					"Please choose either 'Server' or 'Offline' for deployment.", MessageBoxImage.Error);

				return false;
			}

			return true;
		}

		public string this[string columnName]
		{
			get
			{
				switch (columnName)
				{
					case "Name":
						if (string.IsNullOrEmpty(Name))
						{
							return "'Name' is required!";
						}
						break;

					case "Entity":
					case "Message":
						if (string.IsNullOrEmpty((string) GetType().GetProperty(columnName).GetValue(this)))
						{
							return "'" + columnName + "' is required!";
						}
						break;

					case "User":
					{
						if (User == null)
						{
							return "'" + columnName + "' is required!";
						}
						break;
					}
				}

				// no errors
				return null;
			}
		}

		public string Error
		{
			get { return null; }
		}

		#endregion
	}
}
