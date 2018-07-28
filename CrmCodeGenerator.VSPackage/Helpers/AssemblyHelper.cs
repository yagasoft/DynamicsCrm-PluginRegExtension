#region Imports

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrmPluginRegExt.AssemblyInfoLoader;
using CrmPluginRegExt.VSPackage.Model;
using EnvDTE;
using Microsoft.Xrm.Sdk;

#endregion

namespace CrmPluginRegExt.VSPackage.Helpers
{
	internal static class AssemblyHelper
	{
		internal static List<string> GetClasses<TClassType>(Project project = null, bool buildProject = false)
		{
			if (buildProject)
			{
				BuildProject();
			}

			project = project ?? DteHelper.GetSelectedProject();
			return GetAssemblyInfo(typeof(TClassType).FullName, project).Classes.ToList();
		}

		internal static string GetAssemblyVersion(Project project = null, bool buildProject = false)
		{
			if (buildProject)
			{
				BuildProject();
			}

			project = project ?? DteHelper.GetSelectedProject();
			return GetAssemblyInfo(project: project).Version;
		}

		internal static AssemblyInfo GetAssemblyInfo(string fullClassName = "", Project project = null, bool buildProject = false)
		{
			if (buildProject)
			{
				BuildProject();
			}

			project = project ?? DteHelper.GetSelectedProject();
			return new AssemblyInfoLoader.AssemblyInfoLoader().GetAssemblyInfo(DteHelper.GetAssemblyPath(project),
				DteHelper.GetAssemblyDirectory() + "\\CrmPluginRegExt.AssemblyInfoLoader.dll", fullClassName);
		}

		internal static byte[] GetAssemblyData(bool buildProject = false)
		{
			if (buildProject)
			{
				BuildProject();
			}

			return File.ReadAllBytes(DteHelper.GetAssemblyPath());
		}

		internal static void BuildProject()
		{
			Status.Update("Building project ... ", false);
			DteHelper.BuildSelectedProject();
			Status.Update("done!");
		}
	}
}
