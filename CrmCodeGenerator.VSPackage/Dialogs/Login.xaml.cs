#region Imports

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CrmPluginEntities;
using CrmPluginRegExt.VSPackage.Helpers;
using CrmPluginRegExt.VSPackage.Model;
using EnvDTE80;
using LinkDev.Libraries.EnhancedOrgService.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Application = System.Windows.Forms.Application;
using MultiSelectComboBoxClass = CrmCodeGenerator.Controls.MultiSelectComboBox;

#endregion

namespace CrmPluginRegExt.VSPackage.Dialogs
{
	/// <summary>
	///     Interaction logic for Login.xaml
	/// </summary>
	public partial class Login : INotifyPropertyChanged
	{
		#region Hide close button stuff

		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		#endregion

		#region Properties

		private readonly string title = "Plugin Registration Extension v1.12.4";

		private Settings settings;
		private readonly SettingsArray settingsArray;
		private readonly AssemblyRegistration assemblyRegistration;

		private readonly CrmAssembly crmAssembly = new CrmAssembly();

		private IEnhancedOrgService service;
		private XrmServiceContext context;

		private bool isSandbox;

		public bool IsSandbox
		{
			get { return isSandbox; }
			set
			{
				isSandbox = value;
				OnPropertyChanged("IsSandbox");
			}
		}

		public bool IsSandboxEnabled => !settings.UseOnline;


		private bool isPluginTypeStepsEnabled;

		public bool IsPluginTypeStepsEnabled
		{
			get { return isPluginTypeStepsEnabled; }
			set
			{
				isPluginTypeStepsEnabled = value;
				OnPropertyChanged("IsPluginTypeStepsEnabled");
			}
		}

		private bool isTypeStepSelected;

		public bool IsTypeStepSelected
		{
			get { return isTypeStepSelected; }
			set
			{
				isTypeStepSelected = value;
				OnPropertyChanged("IsTypeStepSelected");
			}
		}

		private bool isStepImageSelected;

		public bool IsStepImageSelected
		{
			get { return isStepImageSelected; }
			set
			{
				isStepImageSelected = value;
				OnPropertyChanged("IsStepImageSelected");
			}
		}

		private bool isAddImageAllowed;

		public bool IsAddImageAllowed
		{
			get { return isAddImageAllowed; }
			set
			{
				isAddImageAllowed = value;
				OnPropertyChanged("IsAddImageAllowed");
			}
		}

		private bool isRegistered;

		private bool IsRegistered
		{
			get { return isRegistered; }
			set
			{
				isRegistered = value;

				// assembly already registered, so any action is otherwise
				if (isRegistered)
				{
					Dispatcher.BeginInvoke(
						new Action(() => { GenerateCodeButton.Content = IsRegistered ? "Update" : "Register"; }));
				}

				OnPropertyChanged("IsRegisteredString");
			}
		}

		public string IsRegisteredString
		{
			get
			{
				// visual indicator of registration status
				if (IsRegistered)
				{
					TextBlockIsRegistered.Foreground = new SolidColorBrush(Colors.Green);
					return "Yes";
				}
				else
				{
					TextBlockIsRegistered.Foreground = new SolidColorBrush(Colors.Red);
					return "No";
				}
			}
		}

		public bool StillOpen { get; private set; } = true;

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region Initialisation

		public Login(DTE2 dte)
		{
			Assembly.Load("Xceed.Wpf.Toolkit");

			InitializeComponent();

			var main = dte.GetMainWindow();
			Owner = main;
			//Loaded += delegate  { this.CenterWindow(main); };

			assemblyRegistration = new AssemblyRegistration();

			RegisterEvents();

			settingsArray = Configuration.LoadConfigs();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// hide close button
			var hwnd = new WindowInteropHelper(this).Handle;
			SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

			Title = $"{DteHelper.CurrentProject.Name} - {title}";
			SetUiContext();
		}

		private void SetUiContext()
		{
			ComboBoxSettings.DataContext = settingsArray;
			ComboBoxSettings.DisplayMemberPath = "DisplayName";

			CheckBoxSandbox.DataContext = this;
			TextBlockIsRegistered.DataContext = this;
			ButtonAddStep.DataContext = this;
			ButtonAddImage.DataContext = this;
			ButtonEditStep.DataContext = this;
			ButtonEditImage.DataContext = this;
			ButtonRemoveStep.DataContext = this;
			ButtonToggleStep.DataContext = this;
			ButtonRemoveImage.DataContext = this;
		}

		private void InitSettingsChange()
		{
			try
			{
				settings.PropertyChanged -= settings_PropertyChanged;
			}
			catch
			{
				// ignored
			}

			// make sure something valid is selected
			settingsArray.SelectedSettingsIndex = Math.Max(0, settingsArray.SelectedSettingsIndex);

			settings = settingsArray.GetSelectedSettings();

			txtPassword.Password = settings.Password; // PasswordBox doesn't allow 2 way binding

			// data contexts for UI controls
			DataContext = settings;

			settings.PropertyChanged += settings_PropertyChanged;
			if (settings.OrgList.Contains(settings.CrmOrg) == false)
			{
				settings.OrgList.Add(settings.CrmOrg);
			}

			new Thread(() =>
			           {
				           UpdateStatus("Initialising UI data ... ", true);

				           try
				           {
					           InitActions();
				           }
				           catch
				           {
					           // ignored
				           }
				           finally
				           {
					           HideBusy();
				           }

				           UpdateStatus("-- READY!", false);
			           }).Start();
		}

		private void InitActions(bool isClearCache = false)
		{
			service?.Dispose();
			CheckContext(isClearCache);

			crmAssembly.Clear();

			try
			{
				ShowBusy("Checking assembly registration ...");
				IsRegistered = CrmAssemblyHelper.IsAssemblyRegistered(context);
			}
			catch
			{
				IsRegistered = false;
			}

			if (!IsRegistered)
			{
				settings.Id = crmAssembly.Id = assemblyRegistration.Id = Guid.Empty;
				return;
			}

			ShowBusy("Getting assembly ID ...");
			settings.Id = crmAssembly.Id = assemblyRegistration.Id = CrmAssemblyHelper.GetAssemblyId(context);
			UpdateStatus("Updating assembly info ...", true);
			crmAssembly.UpdateInfo(context);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			this.HideMinimizeAndMaximizeButtons();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			assemblyRegistration.CancelRegistration = true;
			//UpdateStatus("Cancelled generator!", false);
			//_StillOpen = false;
			//Close();
		}

		private void RegisterEvents()
		{
			assemblyRegistration.PropertyChanged += (o, args) =>
			                                        {
				                                        try
				                                        {
					                                        switch (args.PropertyName)
					                                        {
						                                        case "LogMessage":
							                                        lock (assemblyRegistration.LoggingLock)
							                                        {
								                                        UpdateStatus(assemblyRegistration.LogMessage, true);
							                                        }
							                                        break;

						                                        case "CancelRegistration":
							                                        if (assemblyRegistration.CancelRegistration)
							                                        {
								                                        //UpdateStatus("-- Cancelled tool!", false);
								                                        StillOpen = false;
								                                        Dispatcher.Invoke(Close);
							                                        }
							                                        break;

						                                        case "Error":
							                                        break;

						                                        case "Id":
							                                        break;
					                                        }
				                                        }
				                                        catch
				                                        {
					                                        // ignored
				                                        }
			                                        };

			assemblyRegistration.RegistrationActionTaken
				+= (sender, e) =>
				   {
					   switch (e)
					   {
						   case RegistrationEvent.Create:
						   case RegistrationEvent.Update:
							   // if we have an ID, then get plugin types to display in the GUI list
							   if (assemblyRegistration.Id != Guid.Empty)
							   {
								   try
								   {
									   settings.Id = assemblyRegistration.Id;

									   UpdateStatus("Updating assembly info ...", true);
									   crmAssembly.Id = assemblyRegistration.Id;
									   crmAssembly.UpdateInfo(context);

									   UpdateStatus("-- Finished registering/updating assembly.", false);
									   UpdateStatus($"-- Ran on: {settings.ServerName} - {settings.CrmOrg} - {settings.Username}", false);

									   ShowBusy("Saving settings ...");
									   Configuration.SaveConfigs(settingsArray);
								   }
								   catch (Exception exception)
								   {
									   PopException(exception);
								   }
								   finally
								   {
									   HideBusy();
								   }

								   IsRegistered = true;
							   }
							   break;

						   case RegistrationEvent.Delete:
							   UpdateStatus($"-- Ran on: {settings.ServerName} - {settings.CrmOrg} - {settings.Username}", false);
							   break;

						   default:
							   throw new ArgumentOutOfRangeException(nameof(e), e, null);
					   }
				   };

			crmAssembly.Updated += (o, args) =>
			                       {
				                       ParseCrmAssembly();

				                       // update UI
				                       Dispatcher.Invoke(
					                       () => { ListPluginTypes.ItemsSource = crmAssembly.Children; });
			                       };

			EventManager.RegisterClassHandler(typeof(TextBox), MouseDoubleClickEvent, new RoutedEventHandler(SelectAddress));
			EventManager.RegisterClassHandler(typeof(TextBox), GotKeyboardFocusEvent, new RoutedEventHandler(SelectAddress));
			EventManager.RegisterClassHandler(typeof(TextBox), PreviewMouseLeftButtonDownEvent,
				new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
			EventManager.RegisterClassHandler(typeof(PasswordBox), MouseDoubleClickEvent, new RoutedEventHandler(SelectAddress));
			EventManager.RegisterClassHandler(typeof(PasswordBox), GotKeyboardFocusEvent, new RoutedEventHandler(SelectAddress));
			EventManager.RegisterClassHandler(typeof(PasswordBox), PreviewMouseLeftButtonDownEvent,
				new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
		}

		#endregion

		#region Status stuff

		private void PopException(Exception exception)
		{
			Dispatcher.Invoke(() =>
			                  {
				                  var message = exception.Message
				                                + (exception.InnerException != null ? "\n" + exception.InnerException.Message : "");
				                  MessageBox.Show(message, exception.GetType().FullName, MessageBoxButton.OK, MessageBoxImage.Error);
			                  });
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

		internal void UpdateStatus(string message, bool working, bool newLine = true)
		{
			//Dispatcher.Invoke(() => SetEnabledChildren(Inputs, !working, "ButtonCancel"));

			if (working)
			{
				ShowBusy(message);
			}
			else
			{
				HideBusy();
			}

			if (!string.IsNullOrWhiteSpace(message))
			{
				Dispatcher.BeginInvoke(new Action(() => { Status.Update(message, newLine); }));
			}

			Application.DoEvents();
			// Needed to allow the output window to update (also allows the cursor wait and form disable to show up)
		}

		#endregion

		#region UI events

		#region Main

		private void Logon_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				settings.Password = ((PasswordBox) ((Button) sender).CommandParameter).Password;
				// PasswordBox doesn't allow 2 way binding, so we have to manually read it
				settings.Dirty = true;

				// keep sandbox state across updating the assembly info
				var isSandbox = IsSandbox;

				InitActions();

				IsSandbox = isSandbox;

				ShowBusy("Saving settings ...");
				Configuration.SaveConfigs(settingsArray);

				new Thread(() =>
				           {
					           try
					           {
						           if (IsRegistered)
						           {
									   assemblyRegistration.UpdateAssembly(settings.Id, IsSandbox);
						           }
						           else
						           {
									   assemblyRegistration.CreateAssembly(IsSandbox);
						           }
					           }
					           catch (Exception ex)
					           {
						           var error = "[ERROR] " + ex.Message +
						                       (ex.InnerException != null ? "\n" + "[ERROR] " + ex.InnerException.Message : "");
						           UpdateStatus(error, false);
						           UpdateStatus(ex.StackTrace, false);
						           UpdateStatus("Unable to register assembly, see error above.", false);
						           PopException(ex);
					           }
					           finally
					           {
								   HideBusy();
					           }
				           }).Start();
			}
			catch (Exception ex)
			{
				var error = "[ERROR] " + ex.Message +
				            (ex.InnerException != null ? "\n" + "[ERROR] " + ex.InnerException.Message : "");
				UpdateStatus(error, false);
				UpdateStatus(ex.StackTrace, false);
				UpdateStatus("Unable to register assembly, see error above.", false);
				PopException(ex);
			}
			finally
			{
				HideBusy();
			}
		}

		private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
		{
			var isClearCache = CheckBoxClearCache.IsChecked ?? false;

			new Thread(() =>
			           {
						   try
						   {
							   Dispatcher.Invoke(() =>
								   settings.Password = ((PasswordBox)((Button)sender).CommandParameter).Password);

							   // PasswordBox doesn't allow 2 way binding, so we have to manually read it
							   settings.Dirty = true;

							   UpdateStatus("Re-initialising data ...", true);
							   InitActions(isClearCache);
							   UpdateStatus("-- Finished re-initialising data ...", false);

							   if (isClearCache)
							   {
								   CrmDataHelper.ClearCache();
							   }
						   }
						   catch (Exception exception)
						   {
							   PopException(exception);
						   }
						   finally
						   {
							   Dispatcher.Invoke(() => CheckBoxClearCache.IsChecked = false);
							   UpdateStatus("", false);
						   }
			           }).Start();
		}

		private void ButtonDelete_Click(object sender, RoutedEventArgs e)
		{
			// can't fetch assembly without an ID
			if (assemblyRegistration.Id == Guid.Empty)
			{
				DteHelper.ShowInfo("Can't delete a non-existent assembly.", "Non-existent assembly!");
				return;
			}

			if (DteHelper.IsConfirmed("Are you sure you want to UNregister this plugin?" +
			                          " This means that the plugin and all its steps will be deleted!", "Unregistration"))
			{
				new Thread(() =>
				           {
					           try
					           {
								   assemblyRegistration.DeleteAssembly(assemblyRegistration.Id);
								   crmAssembly.Clear();
					           }
					           catch (Exception exception)
					           {
						           PopException(exception);
					           }
					           finally
					           {
						           UpdateStatus("", false);
					           }
				           }).Start();
			}
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			assemblyRegistration.CancelRegistration = true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Dispatcher.Invoke(Close);
			}

			base.OnKeyDown(e);
		}

		#endregion

		private void ClearSelectionOnClickEmpty(object sender, MouseButtonEventArgs e)
		{
			Dispatcher.Invoke(() =>
			                  {
				                  var hitTestResult = VisualTreeHelper.HitTest((ListView) sender, e.GetPosition((ListView) sender));
				                  var controlType = hitTestResult.VisualHit.DependencyObjectType.SystemType;

				                  if (controlType == typeof (ScrollViewer))
				                  {
					                  ((ListView) sender).SelectedItem = null;
				                  }
			                  });
		}

		#region Types

		private void ListPluginTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var type = (CrmPluginType) ListPluginTypes.SelectedItem;

			IsPluginTypeStepsEnabled = type != null && !type.IsWorkflow;

			new Thread(() =>
			           {
				           // don't fetch from CRM if updated once (caching)
				           if (type != null && !type.IsUpdated)
				           {
					           try
					           {
						           UpdateStatus("Updating plugin type info ...", true);
								   type.UpdateInfo(context);
						           UpdateStatus("-- Finished updating plugin type info.", false);
					           }
					           catch (Exception exception)
					           {
						           PopException(exception);
					           }
					           finally
					           {
						           UpdateStatus("", false);
					           }
				           }

				           // update UI
				           UpdateListBindingToEntityChildren(type, ListTypeSteps);
			           }).Start();
		}

		private void ListPluginTypes_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			ClearSelectionOnClickEmpty(sender, e);
		}

		#endregion

		#region Steps

		private void ButtonAddStep_Click(object sender, RoutedEventArgs e)
		{
			var type = (CrmPluginType) ListPluginTypes.SelectedItem;
			var newStep = new CrmTypeStep {Type = type};

			new Thread(() =>
			           {
				           TypeStep dialogue = null;

						   Dispatcher.Invoke(() =>
				                             {
												 dialogue = new TypeStep(newStep, context);
					                             dialogue.ShowDialog();
				                             });

				           try
				           {
					           if (!dialogue.IsUpdate)
					           {
						           return;
					           }

					           var filter = CrmDataHelper.MessageList
						           .FirstOrDefault(messageQ => messageQ.EntityName == newStep.Entity
						                                       && messageQ.MessageName == newStep.Message);

					           newStep.FilterId = filter?.FilteredId ?? Guid.Empty;

					           var message = CrmDataHelper.MessageList
						           .First(messageQ => messageQ.MessageName == newStep.Message);

					           newStep.MessageId = message.MessageId;

							   assemblyRegistration.CreateTypeStep(newStep);

					           Dispatcher.Invoke(() => type.Children.Add(newStep));
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

		private void ButtonEditStep_Click(object sender, RoutedEventArgs e)
		{
			var type = (CrmPluginType) ListPluginTypes.SelectedItem;
			var step = (CrmTypeStep) ListTypeSteps.SelectedItem;

			var clone = step.Clone<CrmTypeStep>();

			new Thread(() =>
			           {
				           try
				           {
					           if (!clone.IsUpdated)
					           {
						           UpdateStatus("Updating step info ...", true);
						           // only basic info exists in the step when it is loaded through the type class
								   clone.UpdateInfo(context);
						           UpdateStatus("-- Finished updating step info.", false);
					           }
				           }
				           catch (Exception exception)
				           {
					           PopException(exception);
				           }
				           finally
				           {
					           UpdateStatus("", false);
				           }

				           TypeStep dialogue = null;

				           Dispatcher.Invoke(() =>
				                             {
												 dialogue = new TypeStep(clone, context);
					                             dialogue.ShowDialog();
				                             });

				           try
				           {
					           if (!dialogue.IsUpdate)
					           {
						           return;
					           }

					           var filter = CrmDataHelper.MessageList
						           .FirstOrDefault(messageQ => messageQ.EntityName == clone.Entity
						                                       && messageQ.MessageName == clone.Message);

					           clone.FilterId = filter?.FilteredId ?? Guid.Empty;

					           var message = CrmDataHelper.MessageList
						           .First(messageQ => messageQ.MessageName == clone.Message);

					           clone.MessageId = message.MessageId;

							   assemblyRegistration.UpdateTypeStep(clone);

					           Dispatcher.Invoke(() =>
					                             {
						                             type.Children.Remove(step);
						                             type.Children.Add(clone);
					                             });
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

		private void ButtonRemoveStep_Click(object sender, RoutedEventArgs e)
		{
			var type = (CrmPluginType) ListPluginTypes.SelectedItem;
			var steps = ListTypeSteps.SelectedItems.Cast<CrmTypeStep>().ToList();

			var isConfirmed = DteHelper.IsConfirmed(
				"Are you sure you want to DELETE steps(s):" +
				$" {steps.Select(step => step.Name).Aggregate((step1Name, step2Name) => step1Name + ", " + step2Name)}?",
				"Step(s) Deletion");

			if (!isConfirmed)
			{
				return;
			}

			new Thread(() =>
			           {
				           try
				           {
					           steps.ForEach(step =>
					                         {
												 assemblyRegistration.DeleteTypeStep(step.Id);
						                         Dispatcher.Invoke(() => type.Children.Remove(step));
					                         });
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

		private void ButtonToggleStep_Click(object sender, RoutedEventArgs e)
		{
			var steps = ListTypeSteps.SelectedItems.Cast<CrmTypeStep>().ToList();

			new Thread(() =>
			           {
				           try
				           {
					           steps.ForEach(step =>
					                         {
						                         assemblyRegistration.SetTypeStepState(step.Id,
							                         step.IsDisabled
								                         ? SdkMessageProcessingStepState.Disabled
								                         : SdkMessageProcessingStepState.Enabled);
						                         step.IsDisabled = !step.IsDisabled;
					                         });
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

		private void ListTypeSteps_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = ListTypeSteps.SelectedItem as CrmTypeStep;

			if (item == null && IsPluginTypeStepsEnabled)
			{
				ButtonAddStep_Click(sender, e);
			}
			else if (item != null && IsTypeStepSelected)
			{
				ButtonEditStep_Click(sender, e);
			}
		}

		private void ListTypeSteps_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			ClearSelectionOnClickEmpty(sender, e);
		}

		private void ListTypeSteps_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var step = (CrmTypeStep) ListTypeSteps.SelectedItem;

			new Thread(() =>
			           {
				           if (step != null)
				           {
					           IsTypeStepSelected = true;

					           // don't fetch from CRM if updated once (caching)
					           if (!step.IsUpdated)
					           {
						           try
						           {
							           UpdateStatus("Updating type step info ...", true);
									   step.UpdateInfo(context);
							           UpdateStatus("-- Finished updating type step info.", false);
						           }
						           catch (Exception exception)
						           {
							           PopException(exception);
						           }
						           finally
						           {
							           UpdateStatus("", false);
						           }
					           }

					           IsAddImageAllowed = (step.Message == "Create"
					                                && step.Stage == SdkMessageProcessingStep.Enums.Stage.Postoperation)
					                               || (step.Message != "Create"
					                                   &&
					                                   !string.IsNullOrEmpty(CrmDataHelper.GetMessagePropertyName(step.Message,
						                                   step.Entity)));
				           }
				           else
				           {
					           IsAddImageAllowed = false;
				           }

				           // update UI
				           UpdateListBindingToEntityChildren(step, ListStepImages);
			           }).Start();
		}

		#endregion

		#region Images

		private void ButtonAddImage_Click(object sender, RoutedEventArgs e)
		{
			var step = (CrmTypeStep) ListTypeSteps.SelectedItem;
			var newImage = new CrmStepImage {Step = step};

			new Thread(() =>
			           {
				           try
				           {
					           UpdateStatus("Getting attribute list ...", true);
					           newImage.AttributeList = new ObservableCollection<string>(CrmDataHelper
								   .GetEntityFieldNames(step.Entity, context));
					           UpdateStatus("-- Finished getting attribute list.", false);
				           }
				           catch (Exception exception)
				           {
					           PopException(exception);
				           }
				           finally
				           {
					           UpdateStatus("", false);
				           }

				           StepImage dialogue = null;
				           Dispatcher.Invoke(() =>
				                             {
												 dialogue = new StepImage(newImage, context);
					                             dialogue.ShowDialog();
				                             });

				           try
				           {
					           if (dialogue.IsUpdate)
					           {
								   assemblyRegistration.CreateStepImage(newImage);
						           Dispatcher.Invoke(() => step.Children.Add(newImage));
					           }
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

		private void ButtonEditImage_Click(object sender, RoutedEventArgs e)
		{
			var step = (CrmTypeStep) ListTypeSteps.SelectedItem;
			var image = (CrmStepImage) ListStepImages.SelectedItem ?? new CrmStepImage();

			new Thread(() =>
			           {
				           try
				           {
					           UpdateStatus("Getting attribute list ...", true);
					           image.AttributeList = new ObservableCollection<string>(CrmDataHelper
								   .GetEntityFieldNames(step.Entity, context));
					           UpdateStatus("-- Finished getting attribute list.", false);

					           if (!image.IsUpdated)
					           {
						           UpdateStatus("Updating image info ...", true);
						           // only basic info exists in the image when it is loaded through the step class
								   image.UpdateInfo(context);
						           UpdateStatus("-- Finished updating image info.", false);
					           }
				           }
				           catch (Exception exception)
				           {
					           PopException(exception);
				           }
				           finally
				           {
					           UpdateStatus("", false);
				           }

				           var clone = image.Clone<CrmStepImage>();

				           StepImage dialogue = null;
				           Dispatcher.Invoke(() =>
				                             {
												 dialogue = new StepImage(clone, context);
					                             dialogue.ShowDialog();
				                             });

				           try
				           {
					           if (dialogue.IsUpdate)
					           {
								   assemblyRegistration.UpdateStepImage(clone);
						           Dispatcher.Invoke(() =>
						                             {
							                             step.Children.Remove(image);
							                             step.Children.Add(clone);
						                             });
					           }
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

		private void ButtonRemoveImage_Click(object sender, RoutedEventArgs e)
		{
			var step = (CrmTypeStep) ListTypeSteps.SelectedItem;
			var images = ListStepImages.SelectedItems.Cast<CrmStepImage>().ToList();

			var isConfirmed = DteHelper.IsConfirmed(
				"Are you sure you want to DELETE image(s):" +
				$" {images.Select(image => image.Name).Aggregate((image1Name, image2Name) => image1Name + ", " + image2Name)}?",
				"Image(s) Deletion");

			if (!isConfirmed)
			{
				return;
			}

			new Thread(() =>
			           {
				           try
				           {
					           images.ForEach(image =>
					                          {
												  assemblyRegistration.DeleteStepImage(image.Id);
						                          Dispatcher.Invoke(() => step.Children.Remove(image));
					                          });
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

		private void ListStepImages_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = ListStepImages.SelectedItem as CrmStepImage;

			if (item == null && IsAddImageAllowed)
			{
				ButtonAddImage_Click(sender, e);
			}
			else if (item != null && IsStepImageSelected)
			{
				ButtonEditImage_Click(sender, e);
			}
		}

		private void ListStepImages_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			ClearSelectionOnClickEmpty(sender, e);
		}

		private void ListStepImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			IsStepImageSelected = (CrmStepImage) ListStepImages.SelectedItem != null;
		}

		#endregion

		#region Settings profiles

		private void ButtonNewSettings_Click(object sender, RoutedEventArgs e)
		{
			var newSettings = new Settings();
			settingsArray.SettingsList.Add(newSettings);
			settingsArray.SelectedSettingsIndex = settingsArray.SettingsList.IndexOf(newSettings);
			//InitSettingsChange();
		}

		private void ButtonDuplicateSettings_Click(object sender, RoutedEventArgs e)
		{
			var newSettings = ObjectCopier.Clone(settingsArray.GetSelectedSettings());
			settingsArray.SettingsList.Add(newSettings);
			settingsArray.SelectedSettingsIndex = settingsArray.SettingsList.IndexOf(newSettings);

			DteHelper.ShowInfo("The profile chosen has been duplicated, and the duplicate is now the " +
			                   "selected profile.", "Profile duplicated!");
		}

		private void ButtonDeleteSettings_Click(object sender, RoutedEventArgs e)
		{
			if (settingsArray.SettingsList.Count <= 1)
			{
				PopException(new Exception("Can't delete the last settings profile."));
				return;
			}

			if (DteHelper.IsConfirmed("Are you sure you want to delete this settings profile?",
				"Confirm delete action ..."))
			{
				settingsArray.SettingsList.Remove(settingsArray.GetSelectedSettings());
				settingsArray.SelectedSettingsIndex = 0;
			}
		}

		private void ButtonSaveSettings_Click(object sender, RoutedEventArgs e)
		{
			Configuration.SaveConfigs(settingsArray);
			DteHelper.ShowInfo("All settings profiles have been saved to disk.", "Settings saved!");
		}

		private void ComboBoxSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			InitSettingsChange();
		}

		#endregion

		// credit: https://social.msdn.microsoft.com/Forums/vstudio/en-US/564b5731-af8a-49bf-b297-6d179615819f/how-to-selectall-in-textbox-when-textbox-gets-focus-by-mouse-click?forum=wpf&prof=required
		#region Textbox selection

		private static void SelectAddress(object sender, RoutedEventArgs e)
		{
			if (sender is TextBox || sender is PasswordBox)
			{
				((dynamic) sender).SelectAll();
			}
		}

		private static void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
		{
			if (!(sender is TextBox || sender is PasswordBox))
			{
				return;
			}

			var tb = (dynamic) sender;

			if (tb.IsKeyboardFocusWithin)
			{
				return;
			}

			e.Handled = true;
			tb.Focus();
		}

		#endregion

		#endregion

		private void UpdateListBindingToEntityChildren(object source, ListView listView)
		{
			Dispatcher.Invoke(() =>
			                  {
				                  var binding = new Binding
				                                {
					                                Source = source,
					                                Mode = BindingMode.OneWay,
					                                Path = new PropertyPath("Children")
				                                };

				                  listView.SetBinding(ItemsControl.ItemsSourceProperty, binding);
			                  });
		}

		private void RefreshOrgs(object sender, RoutedEventArgs e)
		{
			settings.Password = ((PasswordBox) ((Button) sender).CommandParameter).Password;
			// PasswordBox doesn't allow 2 way binding, so we have to manually read it

			new Thread(() =>
			           {
				           UpdateStatus("Refreshing organisations ...", true);
				           try
				           {
					           //var orgs = QuickConnection.GetOrganizations(settings.CrmSdkUrl, settings.Domain, settings.Username, settings.Password);
					           //var newOrgs = new ObservableCollection<String>(orgs);
					           //settings.OrgList = newOrgs;

					           var newOrgs = ConnectionHelper.GetOrgList(settings);
					           settings.OrgList = newOrgs;
					           UpdateStatus("-- Finished refreshing organisations.", false);
				           }
				           catch (Exception ex)
				           {
					           var error = "[ERROR] " + ex.Message +
					                       (ex.InnerException != null ? "\n" + "[ERROR] " + ex.InnerException.Message : "");
					           UpdateStatus(error, false);
					           UpdateStatus("Unable to refresh organizations, check connection information", false);
					           PopException(ex);
				           }
				           finally
				           {
					           UpdateStatus("", false);
				           }
			           }).Start();
		}

		private void ParseCrmAssembly()
		{
			IsRegistered = crmAssembly.Id != Guid.Empty;
			IsSandbox = crmAssembly.IsSandbox || settings.UseOnline;
			OnPropertyChanged("IsSandboxEnabled");
		}

		private void settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "UseOnline")
			{
				OnPropertyChanged("IsSandboxEnabled");

				if (settings.UseOnline)
				{
					IsSandbox = true;
				}
			}
		}

		private void CheckContext(bool noCache = false)
		{
			ShowBusy("Checking context ...");

			var connectionString = settings.GetOrganizationCrmConnectionString();
			UpdateStatus($"Connection string: '{Regex.Replace(connectionString, @"Password\s*?=.*?(?:;{0,1}$|;)", "Password=********;")}'.", true);
			ShowBusy("Connecting to CRM ...");
			var connection = ConnectionHelper.GetConnection(connectionString, noCache);
			assemblyRegistration.Context
				= context = new XrmServiceContext(connection)
				            {
					            MergeOption = MergeOption.PreserveChanges
				            };
			assemblyRegistration.Service = service = connection;
		}
	}
}
