#region Imports

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using CrmPluginRegExt.AssemblyInfoLoader;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Window = EnvDTE.Window;
using Project = EnvDTE.Project;
using ProjectItem = EnvDTE.ProjectItem;

#endregion

namespace CrmPluginRegExt.VSPackage.Helpers
{
	public static class DteHelper
	{
		public enum SolutionConfiguration
		{
			Debug,
			Release
		}

		internal static Project CurrentProject;

		internal static void SetCurrentProject(Project project)
		{
			CurrentProject = project;
		}

		public static bool HasProjectItem(this Project project, string projectFile)
		{
			var projectItem = project.GetProjectItem(projectFile);
			return projectItem != null;
		}

		public static void BuildSelectedProject()
		{
			var dte = Package.GetGlobalService(typeof (SDTE)) as DTE;

			if (dte == null)
			{
				throw new Exception("Failed to build project.");
			}

			var configName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
			var projectName = GetSelectedProject().FullName;

			dte.Solution.SolutionBuild.BuildProject(configName, projectName, true);

			if (dte.Solution.SolutionBuild.LastBuildInfo > 0)
			{
				throw new Exception("Failed to build project.");
			}
		}

		public static string GetPath(this Project project)
		{
			return System.IO.Path.GetDirectoryName(project.FullName);
		}

		public static ProjectItem GetProjectItem(this Project project, string projectFile)
		{
			return GetProjectItemRecursive(project.ProjectItems, projectFile);
		}

		private static ProjectItem GetProjectItemRecursive(ProjectItems projectItems, string projectFile, string folder = "")
		{
			// initial value
			ProjectItem result = null;

			projectFile = projectFile.Replace(@"\", "/");

			// iterate project items
			foreach (ProjectItem projectItem in projectItems)
			{
				// if the name matches
				if (folder + projectItem.Name == projectFile)
				{
					result = projectItem;
					break;
				}
				else if ((projectItem.ProjectItems != null) && (projectItem.ProjectItems.Count > 0))
				{
					// Drilldown on folders
					result = GetProjectItemRecursive(projectItem.ProjectItems, projectFile, folder + projectItem.Name + "/");

					// if the file does exist
					if (result != null)
					{
						// break out of loop & Recursion
						break;
					}
				}
			}
			return result;
		}

		public static string MakeRelative(string fromAbsolutePath, string toDirectory)
		{
			if (!Path.IsPathRooted(fromAbsolutePath))
			{
				return fromAbsolutePath;
				// we can't make a relative if it's not rooted(C:\)  so we'll assume we already have a relative path.
			}

			if (!toDirectory[toDirectory.Length - 1].Equals('\\'))
			{
				toDirectory += "\\";
			}

			var from = new Uri(fromAbsolutePath);
			var to = new Uri(toDirectory);

			var relativeUri = to.MakeRelativeUri(from);
			return relativeUri.ToString();
		}


		public static Property SetValue(this Properties props, string name, object value)
		{
			foreach (Property p in props)
			{
				if (p.Name == name)
				{
					p.Value = value;
					return p;
				}
			}
			return null;
		}

		public static string GetAssemblyDirectory()
		{
			var codeBase = Assembly.GetExecutingAssembly().CodeBase;
			var uri = new UriBuilder(codeBase);
			var path = Uri.UnescapeDataString(uri.Path);
			return Path.GetDirectoryName(path);
		}

		public static string GetDefaultNamespace(this Project project)
		{
			return project.Properties.Item("DefaultNamespace").Value.ToString();
		}

		public static string GetPath(this Solution solution)
		{
			return Path.GetDirectoryName(solution.FullName);
		}

		public static string GetProjectDirectory(this Project project)
		{
			return Path.GetDirectoryName(project.FullName);
		}

		public static Project GetSelectedProject(this DTE2 dte)
		{
			return GetSelectedProject();
		}

		public static Project GetSelectedProject(this DTE dte)
		{
			return GetSelectedProject();
		}

		public static Project GetSelectedProject()
		{
			return CurrentProject ?? throw new NullReferenceException("Current project has not been set yet.");
		}

		public static IEnumerable<Project> GetSelectedProjects()
		{
			var dte = Package.GetGlobalService(typeof (SDTE)) as DTE;
			return (dte?.ActiveSolutionProjects as Array)?.Cast<Project>();
		}

		public static System.Windows.Window GetMainWindow(this DTE2 dte)
		{
			if (dte == null)
			{
				throw new ArgumentNullException(nameof(dte));
			}

			var hwndMainWindow = (IntPtr) dte.MainWindow.HWnd;
			if (hwndMainWindow == IntPtr.Zero)
			{
				throw new NullReferenceException("DTE.MainWindow.HWnd is null.");
			}

			var hwndSource = HwndSource.FromHwnd(hwndMainWindow);
			if (hwndSource == null)
			{
				throw new NullReferenceException("HwndSource for DTE.MainWindow is null.");
			}

			return (System.Windows.Window) hwndSource.RootVisual;
		}

		public static SolutionConfiguration GetSolutionConfiguration()
		{
			var dte = Package.GetGlobalService(typeof (SDTE)) as DTE;

			if (dte == null)
			{
				throw new Exception("Can't determine solution configuration.");
			}

			return dte.Solution.SolutionBuild.ActiveConfiguration.Name == "Debug"
				       ? SolutionConfiguration.Debug
				       : SolutionConfiguration.Release;
		}

		public static string GetProjectName(Project project = null)
		{
			return Path.GetFileNameWithoutExtension((project ?? GetSelectedProject()).FullName);
		}

		public static string GetAssemblyName(Project project = null)
		{
			return GetProjectName(project) + ".dll";
		}

		internal static string GetAssemblyPath(Project project = null)
		{
			project = project ?? GetSelectedProject();
			return project.GetProjectDirectory()
			       + "\\bin\\" + GetSolutionConfiguration() + "\\"
			       + GetAssemblyName(project);
		}

		internal static bool IsConfirmed(string text, string title)
		{
			var results = VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
				text, title, OLEMSGICON.OLEMSGICON_WARNING,
				OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);

			return results == 6;
		}

	    internal static void ShowInfo(string text, string title)
	    {
			MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
	    }
	}
}
