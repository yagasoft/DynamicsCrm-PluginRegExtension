#region Imports

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Threading;
using CrmPluginEntities;
using CrmPluginRegExt.VSPackage.Helpers;
using CrmPluginRegExt.VSPackage.Model;
using EnvDTE80;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Yagasoft.Libraries.Common;
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
		#region Properties

		private const string WindowTitle = "Plugin Registration Extension v2.1.3";

		private Settings settings;
		private readonly SettingsArray settingsArray;
		private readonly AssemblyRegistration assemblyRegistration;

		private readonly CrmAssembly crmAssembly = new CrmAssembly();

		private bool isSandbox;

		public bool IsSandbox
		{
			get => isSandbox;
			set
			{
				isSandbox = value;
				OnPropertyChanged("IsSandbox");
			}
		}

		private bool isPluginTypeStepsEnabled;

		public bool IsPluginTypeStepsEnabled
		{
			get => isPluginTypeStepsEnabled;
			set
			{
				isPluginTypeStepsEnabled = value;
				OnPropertyChanged("IsPluginTypeStepsEnabled");
			}
		}

		private bool isTypeStepSelected;

		public bool IsTypeStepSelected
		{
			get => isTypeStepSelected;
			set
			{
				isTypeStepSelected = value;
				OnPropertyChanged("IsTypeStepSelected");
			}
		}

		private bool isStepImageSelected;

		public bool IsStepImageSelected
		{
			get => isStepImageSelected;
			set
			{
				isStepImageSelected = value;
				OnPropertyChanged("IsStepImageSelected");
			}
		}

		private bool isAddImageAllowed;

		public bool IsAddImageAllowed
		{
			get => isAddImageAllowed;
			set
			{
				isAddImageAllowed = value;
				OnPropertyChanged("IsAddImageAllowed");
			}
		}

		private bool isRegistered;

		private bool IsRegistered
		{
			get => isRegistered;
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

			settingsArray = Configuration.LoadSettings();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Title = $"{DteHelper.CurrentProject.Name} - {WindowTitle}";
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

			TextBoxFilterStep.DataContext = this;
			ButtonFilterStep.DataContext = this;
			ButtonFilterClearStep.DataContext = this;
		}

		private void InitSettingsChange()
		{
			// make sure something valid is selected
			settingsArray.SelectedSettingsIndex = Math.Max(0, settingsArray.SelectedSettingsIndex);

			settings = settingsArray.GetSelectedSettings();
			assemblyRegistration.Settings = settings;
			crmAssembly.Id = assemblyRegistration.Id = Guid.Empty;

			// data contexts for UI controls
			DataContext = settings;

			new Thread(
				() =>
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
			CheckContext(isClearCache);

			crmAssembly.Clear();
			Dispatcher.Invoke(() => TextBoxFilterType.Clear());

			try
			{
				ShowBusy("Checking assembly registration ...");
				IsRegistered = CrmAssemblyHelper.IsAssemblyRegistered(settings.ConnectionString);
			}
			catch
			{
				IsRegistered = false;
			}

			if (!IsRegistered)
			{
				return;
			}

			ShowBusy("Getting assembly ID ...");
			crmAssembly.Id = assemblyRegistration.Id = CrmAssemblyHelper.GetAssemblyId(settings.ConnectionString);
			UpdateStatus("Updating assembly info ...", true);
			crmAssembly.UpdateInfo(settings.ConnectionString);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			this.HideCloseButton();
			this.HideMinimizeButton();
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
			assemblyRegistration.PropertyChanged +=
				(o, args) =>
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
									   UpdateStatus("Updating assembly info ...", true);
									   crmAssembly.Id = assemblyRegistration.Id;
									   crmAssembly.UpdateInfo(settings.ConnectionString);

									   UpdateStatus("-- Finished registering/updating assembly.", false);
									   UpdateStatus($"-- Ran on: {Regex.Replace(settings.ConnectionString, @"Password\s*?=.*?(?:;{0,1}$|;)", "Password=********;").Replace("\r\n", " ")}", false);

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
							   UpdateStatus($"-- Ran on: {Regex.Replace(settings.ConnectionString, @"Password\s*?=.*?(?:;{0,1}$|;)", "Password=********;").Replace("\r\n", " ")}", false);
							   break;

						   default:
							   throw new ArgumentOutOfRangeException(nameof(e), e, null);
					   }
				   };

			crmAssembly.Updated +=
				(o, args) =>
			                       {
				                       ParseCrmAssembly();

				                       // update UI
				                       Dispatcher.Invoke(
					                       () => { ListPluginTypes.ItemsSource = crmAssembly.Children; });
			                       };
		}

		#endregion

		#region Status stuff

		private void PopException(Exception exception)
		{
			Dispatcher.Invoke(
				() =>
			                  {
				                  var message = exception.Message
				                                + (exception.InnerException != null ? "\n" + exception.InnerException.Message : "");
				                  MessageBox.Show(message, exception.GetType().FullName, MessageBoxButton.OK, MessageBoxImage.Error);
			                  });
		}

		private void ShowBusy(string message)
		{
			Dispatcher.Invoke(
				() =>
			                  {
				                  BusyIndicator.IsBusy = true;
				                  BusyIndicator.BusyContent =
					                  string.IsNullOrEmpty(message) ? "Please wait ..." : message;
			                  }, DispatcherPriority.Send);
		}

		private void HideBusy()
		{
			Dispatcher.Invoke(
				() =>
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
				// keep sandbox state across updating the assembly info
				var isSandbox = IsSandbox;

				InitActions();

				IsSandbox = isSandbox;

				ShowBusy("Saving settings ...");
				Configuration.SaveConfigs(settingsArray);

				new Thread(
					() =>
				           {
					           try
					           {
						           if (IsRegistered)
						           {
									   assemblyRegistration.UpdateAssembly(IsSandbox);
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

			new Thread(
				() =>
				{
					try
					{
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

			new Thread(
				() =>
				{
					// don't fetch from CRM if updated once (caching)
					if (type != null && !type.IsUpdated)
					{
						try
						{
							UpdateStatus("Updating plugin type info ...", true);
							type.UpdateInfo(settings.ConnectionString);
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

					Dispatcher.Invoke(() => TextBoxFilterStep.Clear());
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

						   Dispatcher.Invoke(
							   () =>
							   {
								   dialogue = new TypeStep(newStep, settings.ConnectionString);
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
								   clone.UpdateInfo(settings.ConnectionString);
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
												 dialogue = new TypeStep(clone, settings.ConnectionString);
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

			new Thread(
				() =>
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
								step.UpdateInfo(settings.ConnectionString);
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

						IsAddImageAllowed =
							(step.Message == "Create" && step.Stage == SdkMessageProcessingStep.Enums.Stage.Postoperation)
								|| (step.Message != "Create"
									&& !string.IsNullOrEmpty(CrmDataHelper.GetMessagePropertyName(step.Message, step.Entity)));
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

			new Thread(
				() =>
				{
					try
					{
						UpdateStatus("Getting attribute list ...", true);
						newImage.AttributeList = new ObservableCollection<string>(CrmDataHelper
							.GetEntityFieldNames(step.Entity, settings.ConnectionString));
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
					Dispatcher.Invoke(
						() =>
						{
							dialogue = new StepImage(newImage);
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
								   .GetEntityFieldNames(step.Entity, settings.ConnectionString));
					           UpdateStatus("-- Finished getting attribute list.", false);

					           if (!image.IsUpdated)
					           {
						           UpdateStatus("Updating image info ...", true);
						           // only basic info exists in the image when it is loaded through the step class
								   image.UpdateInfo(settings.ConnectionString);
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
												 dialogue = new StepImage(clone);
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

		#region Filtering

		private enum FilterControl
		{
			Type,
			Step
		}

		private void ButtonFilterType_Click(object sender, RoutedEventArgs e)
		{
			SelectEntitiesByRegex(TextBoxFilterType, FilterControl.Type);
		}

		private void ButtonFilterClearType_Click(object sender, RoutedEventArgs e)
		{
			TextBoxFilterType.Text = string.Empty;
			SelectEntitiesByRegex(TextBoxFilterType, FilterControl.Type);
		}

		private void TextBoxFilterType_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				SelectEntitiesByRegex(TextBoxFilterType, FilterControl.Type);
			}
		}

		private void ButtonFilterStep_Click(object sender, RoutedEventArgs e)
		{
			SelectEntitiesByRegex(TextBoxFilterStep, FilterControl.Step);
		}

		private void ButtonFilterClearStep_Click(object sender, RoutedEventArgs e)
		{
			TextBoxFilterStep.Text = string.Empty;
			SelectEntitiesByRegex(TextBoxFilterStep, FilterControl.Step);
		}

		private void TextBoxFilterStep_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				SelectEntitiesByRegex(TextBoxFilterStep, FilterControl.Step);
			}
		}

		private void SelectEntitiesByRegex(TextBox textBoxFilter, FilterControl control)
		{
			var text = textBoxFilter.Text.ToLower();
			
			// filter entities
			new Thread(
				() =>
				{
					try
					{
						ShowBusy("Filtering ...");

						switch (control)
						{
							case FilterControl.Type:
								Dispatcher.Invoke(() => crmAssembly.Filter(text));
								break;

							case FilterControl.Step:
								Dispatcher.Invoke(() => ((CrmPluginType)ListPluginTypes.SelectedItem)?.Filter(text));
								break;

							default:
								throw new ArgumentOutOfRangeException(nameof(control), control, null);
						}

						Dispatcher.Invoke(textBoxFilter.Focus);

						HideBusy();
					}
					catch (Exception ex)
					{
						PopException(ex);
						Dispatcher.InvokeAsync(Close);
					}
				}).Start();
		}

		#endregion

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}

		#endregion

		private void UpdateListBindingToEntityChildren(object source, ListView listView)
		{
			Dispatcher.Invoke(
				() =>
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

		private void ParseCrmAssembly()
		{
			IsRegistered = crmAssembly.Id != Guid.Empty;
			IsSandbox = crmAssembly.IsSandbox;
			OnPropertyChanged("IsSandboxEnabled");
		}

		private void CheckContext(bool noCache = false)
		{
			if (!noCache)
			{
				return;
			}

			ShowBusy("Resetting connection ...");
			ConnectionHelper.ResetCache(settings.ConnectionString);
		}
	}
}
