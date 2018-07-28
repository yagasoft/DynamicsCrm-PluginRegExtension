#region Imports

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using CrmPluginEntities;
using CrmPluginRegExt.VSPackage.Dialogs;
using CrmPluginRegExt.VSPackage.Helpers;
using CrmPluginRegExt.VSPackage.Model;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace CrmPluginRegExt.VSPackage
{
	/// <summary>
	///     This is the class that implements the package exposed by this assembly.
	///     The minimum requirement for a class to be considered a valid package for Visual Studio
	///     is to implement the IVsPackage interface and register itself with the shell.
	///     This package uses the helper classes defined inside the Managed Package Framework (MPF)
	///     to do it: it derives from the Package class that provides the implementation of the
	///     IVsPackage interface and uses the registration attributes defined in the framework to
	///     register itself and its components with the shell.
	/// </summary>
	// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
	// a package.
	[PackageRegistration(UseManagedResourcesOnly = true)]
	// This attribute is used to register the information needed to show this package
	// in the Help/About dialog of Visual Studio.
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	//this causes the class to load when VS starts [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string)]
	// This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(GuidList.guidPluginRegExt_VSPackagePkgString)]
	public sealed class CrmPluginRegExt_VSPackagePackage : Package,
		IVsSolutionEvents3
	{
		/// <summary>
		///     Default constructor of the package.
		///     Inside this method you can place any initialization code that does not require
		///     any Visual Studio service because at this point the package object is created but
		///     not sited yet inside Visual Studio environment. The place to do all the other
		///     initialization is the Initialize method.
		/// </summary>
		public CrmPluginRegExt_VSPackagePackage()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
		}


		/////////////////////////////////////////////////////////////////////////////
		// Overridden Package Implementation

		#region Package Members

		/// <summary>
		///     Initialization of the package; this method is called right after the package is sited, so this is the place
		///     where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			AssemblyHelpers.RedirectAssembly("Microsoft.Xrm.Sdk", new Version("9.0.0.0"), "31bf3856ad364e35");
			//AssemblyHelpers.RedirectAssembly("Microsoft.Xrm.Sdk.Workflow", new Version("9.0.0.0"), "31bf3856ad364e35");
			//AssemblyHelpers.RedirectAssembly("Microsoft.IdentityModel.Clients.ActiveDirectory",
			//	new Version("2.22.0.0"), "31bf3856ad364e35");

			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
			base.Initialize();

			// Add our command handlers for menu (commands must exist in the .vsct file)
			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

			if (null != mcs)
			{
				var registerCmd = new CommandID(GuidList.guidPluginRegExt_VSPackageCmdSet,
					(int) PkgCmdIDList.cmdidRegisterModifyPlugin);
				var registerItem = new MenuCommand(RegisterModifyPluginCallback, registerCmd);
				mcs.AddCommand(registerItem);

				var updateCmd = new CommandID(GuidList.guidPluginRegExt_VSPackageCmdSet, (int) PkgCmdIDList.cmdidUpdatePlugin);
				var updateItem = new MenuCommand(UpdatePluginCallback, updateCmd);
				mcs.AddCommand(updateItem);

				var multiRegisterCmd = new CommandID(GuidList.guidPluginRegExt_VSPackageCmdSet,
					(int) PkgCmdIDList.cmdidMultiRegisterModifyPlugin);
				var multiRegisterItem = new MenuCommand(RegisterModifyPluginCallback, multiRegisterCmd);
				mcs.AddCommand(multiRegisterItem);

				var multiUpdateCmd = new CommandID(GuidList.guidPluginRegExt_VSPackageCmdSet,
					(int) PkgCmdIDList.cmdidMultiUpdatePlugin);
				var multiUpdateItem = new MenuCommand(UpdatePluginCallback, multiUpdateCmd);
				mcs.AddCommand(multiUpdateItem);

				var copySettingsCmd = new CommandID(GuidList.guidPluginRegExt_VSPackageCmdSet,
					(int) PkgCmdIDList.cmdidCopyPluginSettings);
				var copySettingsItem = new MenuCommand(CopySettingsCallback, copySettingsCmd);
				mcs.AddCommand(copySettingsItem);

				//var deleteCmd = new CommandID(GuidList.guidPluginRegExt_VSPackageCmdSet, (int) PkgCmdIDList.cmdidDeletePlugin);
				//var deleteItem = new MenuCommand(DeletePluginCallback, deleteCmd);
				//mcs.AddCommand(deleteItem);
			}

			AdviseSolutionEvents();
		}

		protected override void Dispose(bool disposing)
		{
			UnadviseSolutionEvents();

			base.Dispose(disposing);
		}

		private IVsSolution solution = null;
		private uint _handleCookie;

		private void AdviseSolutionEvents()
		{
			UnadviseSolutionEvents();

			solution = GetService(typeof(SVsSolution)) as IVsSolution;
			solution?.AdviseSolutionEvents(this, out _handleCookie);
		}

		private void UnadviseSolutionEvents()
		{
			if (solution != null)
			{
				if (_handleCookie != uint.MaxValue)
				{
					solution.UnadviseSolutionEvents(_handleCookie);
					_handleCookie = uint.MaxValue;
				}

				solution = null;
			}
		}

		#endregion

		/// <summary>
		///     This function is the callback used to execute a command when the a menu item is clicked.
		///     See the Initialize method to see how the menu item is associated to this function using
		///     the OleMenuCommandService service and the MenuCommand class.
		/// </summary>
		private void RegisterModifyPluginCallback(object sender, EventArgs args)
		{
			try
			{
				var session = Math.Abs(DateTime.Now.ToString(CultureInfo.CurrentCulture).GetHashCode());
				Status.Update($">>>>> Starting new session: {session} <<<<<");

				var selected = DteHelper.GetSelectedProjects().ToArray();

				if (!selected.Any())
				{
					throw new UserException("Please select a project first.");
				}

				foreach (var project in selected)
				{
					Status.Update($">>> Processing project: {DteHelper.GetProjectName(project)} <<<");
					RegisterModifyPlugin(project);
					Status.Update($"^^^ Finished processing project: {DteHelper.GetProjectName(project)} ^^^");
				}

				Status.Update($"^^^^^ Finished session: {session} ^^^^^");
			}
			catch (UserException e)
			{
				VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, e.Message, "Error", OLEMSGICON.OLEMSGICON_WARNING,
					OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
			catch (Exception e)
			{
				var error1 = "[ERROR] " + e.Message
					+ (e.InnerException != null ? "\n" + "[ERROR] " + e.InnerException.Message : "");
				Status.Update(error1);
				Status.Update(e.StackTrace);
				Status.Update("Unable to register assembly, see error above.");
				var error2 = e.Message + "\n" + e.StackTrace;
				MessageBox.Show(error2, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				Status.Update(">>>>> DONE! <<<<<");
			}
		}

		private void RegisterModifyPlugin(Project project)
		{
			DteHelper.SetCurrentProject(project);
			var regWindow = new Login(GetService(typeof(SDTE)) as DTE2);
			regWindow.ShowModal();
		}

		private void UpdatePluginCallback(object sender, EventArgs args)
		{
			try
			{
				var session = Math.Abs(DateTime.Now.ToString(CultureInfo.CurrentCulture).GetHashCode());
				Status.Update($">>>>> Starting new session: {session} <<<<<");

				if (!DteHelper.GetSelectedProjects().Any())
				{
					throw new UserException("Please select a project first.");
				}

				foreach (var project in DteHelper.GetSelectedProjects())
				{
					Status.Update($">>> Processing project: {DteHelper.GetProjectName(project)} <<<");
					UpdatePlugin(project);
					Status.Update($"^^^ Finished processing project: {DteHelper.GetProjectName(project)} ^^^");
				}

				Status.Update($"^^^^^ Finished session: {session} ^^^^^");
			}
			catch (UserException e)
			{
				VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, e.Message, "Error", OLEMSGICON.OLEMSGICON_WARNING,
					OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
			catch (Exception e)
			{
				var error1 = "[ERROR] " + e.Message
					+ (e.InnerException != null ? "\n" + "[ERROR] " + e.InnerException.Message : "");
				Status.Update(error1);
				Status.Update(e.StackTrace);
				Status.Update("Unable to update assembly, see error above.");
				var error2 = e.Message + "\n" + e.StackTrace;
				MessageBox.Show(error2, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				Status.Update(">>>>> DONE! <<<<<");
			}
		}

		private void UpdatePlugin(Project project)
		{
			DteHelper.SetCurrentProject(project);

			var settingsArray = Configuration.LoadConfigs();
			var settings = settingsArray.GetSelectedSettings();

			// if an ID does not exist and no connection info, then it's a new run
			if (settings.Id == Guid.Empty
				&& (string.IsNullOrEmpty(settings.ServerName) || string.IsNullOrEmpty(settings.Username)))
			{
				CopySettings();
				RegisterModifyPlugin(project);
			}
			else
			{
				using (var service = ConnectionHelper.GetConnection(settings.GetOrganizationCrmConnectionString()))
				{
					var context = new XrmServiceContext(service);
					var registration = new AssemblyRegistration(context, service);

					registration.PropertyChanged +=
						(o, args) =>
						{
							try
							{
								switch (args.PropertyName)
								{
									case "LogMessage":
										lock (registration.LoggingLock)
										{
											Status.Update(registration.LogMessage);
										}
										break;
								}
							}
							catch
							{
								// ignored
							}
						};

					// if the assembly is registered, get ID and update
					if (CrmAssemblyHelper.IsAssemblyRegistered(context))
					{
						var id = settings.Id == Guid.Empty
							? CrmAssemblyHelper.GetAssemblyId(context)
							: settings.Id;
						registration.UpdateAssembly(id, null);
						Status.Update($"Ran update on: {settings.ServerName} - {settings.CrmOrg} - {settings.Username}.");
						Status.Update($"For project: {DteHelper.GetProjectName(project)}.");
					}
					else
					{
						// else, reset and open dialogue
						// reset ID
						settings.Id = Guid.Empty;
						Configuration.SaveConfigs(settingsArray);

						RegisterModifyPlugin(project);
					}
				}
			}
		}

		private void CopySettingsCallback(object sender, EventArgs args)
		{
			try
			{
				var session = Math.Abs(DateTime.Now.ToString(CultureInfo.CurrentCulture).GetHashCode());
				Status.Update($">>>>> Starting new session: {session} <<<<<");

				var selected = DteHelper.GetSelectedProjects().ToArray();

				if (selected.Length < 2)
				{
					throw new UserException("Please select at least two projects first.");
				}

				Status.Update($">>> Processing copy <<<");

				if (!CopySettings())
				{
					throw new UserException("Couldn't find settings in any of the selected projects to copy.");
				}

				Status.Update($"^^^ Finished processing copy ^^^");

				Status.Update($"^^^^^ Finished session: {session} ^^^^^");
			}
			catch (UserException e)
			{
				VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, e.Message, "Error", OLEMSGICON.OLEMSGICON_WARNING,
					OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
			catch (Exception e)
			{
				var error1 = "[ERROR] " + e.Message
					+ (e.InnerException != null ? "\n" + "[ERROR] " + e.InnerException.Message : "");
				Status.Update(error1);
				Status.Update(e.StackTrace);
				Status.Update("Unable to update assembly, see error above.");
				var error2 = e.Message + "\n" + e.StackTrace;
				MessageBox.Show(error2, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				Status.Update(">>>>> DONE! <<<<<");
			}
		}

		private static bool CopySettings()
		{
			var projects = DteHelper.GetSelectedProjects().ToArray();

			if (projects.Length <= 1)
			{
				return false;
			}

			var currentProject = DteHelper.CurrentProject;
			SettingsArray sourceSettingsArray = null;

			foreach (var project in projects)
			{
				DteHelper.SetCurrentProject(project);

				sourceSettingsArray = Configuration.LoadConfigs();
				var settings = sourceSettingsArray.GetSelectedSettings();

				if (settings.Id == Guid.Empty
					&& (string.IsNullOrEmpty(settings.ServerName) || string.IsNullOrEmpty(settings.Username)))
				{
					continue;
				}
				else
				{
					break;
				}
			}

			if (sourceSettingsArray == null)
			{
				return false;
			}

			Status.Update($"Copying settings from project: {DteHelper.CurrentProject.Name} ...");

			foreach (var settingsQ in sourceSettingsArray.SettingsList)
			{
				settingsQ.Id = Guid.Empty;
			}

			foreach (var project in projects)
			{
				DteHelper.SetCurrentProject(project);

				var settingsArray = Configuration.LoadConfigs();
				var settings = settingsArray.GetSelectedSettings();

				if (settings.Id == Guid.Empty
					&& (string.IsNullOrEmpty(settings.ServerName) || string.IsNullOrEmpty(settings.Username)))
				{
					Configuration.SaveConfigs(sourceSettingsArray);
					Status.Update($"Copied settings to project: {project.Name}.");
				}
			}

			DteHelper.SetCurrentProject(currentProject);

			return true;
		}

		private void DeletePluginCallback(object sender, EventArgs args)
		{
			try
			{
				if (DteHelper.IsConfirmed("Are you sure you want to UNregister this plugin?" +
					" This means that the plugin and all its steps will be deleted!", "Unregistration"))
				{
					Status.Update(">>>>> Starting new session <<<<<");
					DeletePlugin();
				}
			}
			catch (UserException e)
			{
				VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, e.Message, "Error", OLEMSGICON.OLEMSGICON_WARNING,
					OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
			catch (Exception e)
			{
				var error1 = "[ERROR] " + e.Message
					+ (e.InnerException != null ? "\n" + "[ERROR] " + e.InnerException.Message : "");
				Status.Update(error1);
				Status.Update(e.StackTrace);
				Status.Update("Unable to delete assembly, see error above.");
				var error2 = e.Message + "\n" + e.StackTrace;
				MessageBox.Show(error2, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				Status.Update(">>>>> DONE! <<<<<");
			}
		}

		private void DeletePlugin()
		{
			var dte2 = GetService(typeof(SDTE)) as DTE2;

			var project = dte2.GetSelectedProject();

			if (string.IsNullOrWhiteSpace(project?.FullName))
			{
				throw new UserException("Please select a project first!");
			}

			var settingsArray = Configuration.LoadConfigs();
			var settings = settingsArray.GetSelectedSettings();

			// if an ID does not exist and no connection info, then it's a new run
			if (settings.Id == Guid.Empty
				&& (string.IsNullOrEmpty(settings.ServerName) || string.IsNullOrEmpty(settings.Username)))
			{
				RegisterModifyPlugin(project);
			}
			else
			{
				using (var service = ConnectionHelper.GetConnection(settings.GetOrganizationCrmConnectionString()))
				{
					var context = new XrmServiceContext(service);
					var registration = new AssemblyRegistration(context, service);

					// if the assembly is registered, get ID and delete
					if (CrmAssemblyHelper.IsAssemblyRegistered(context))
					{
						var id = settings.Id == Guid.Empty
							? CrmAssemblyHelper.GetAssemblyId(context)
							: settings.Id;
						registration.DeleteAssembly(id);
					}
					else
					{
						Status.Update("Assembly already deleted!");

						// else, reset ID
						settings.Id = Guid.Empty;
						Configuration.SaveConfigs(settingsArray);
					}
				}
			}
		}

		#region SolutionEvents

		public int OnAfterCloseSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterClosingChildren(IVsHierarchy pHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterMergeSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterOpeningChildren(IVsHierarchy pHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseSolution(object pUnkReserved)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeClosingChildren(IVsHierarchy pHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
		{
			return VSConstants.S_OK;
		}

		#endregion
	}
}
