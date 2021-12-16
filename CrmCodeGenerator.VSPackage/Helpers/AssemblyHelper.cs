#region Imports

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrmPluginRegExt.AssemblyInfoLoader;
using EnvDTE;
using Microsoft.Xrm.Sdk;

#endregion

namespace CrmPluginRegExt.VSPackage.Helpers
{
	internal static class AssemblyHelper
	{
		internal static string GetAssemblyPath()
		{
			return DteHelper.GetAssemblyPath();
		}

		internal static string GetAssemblyName()
		{
			return Path.GetFileNameWithoutExtension(GetAssemblyPath());
		}

		internal static void BuildProject()
		{
			Status.Update("Building project ... ", false);
			DteHelper.BuildSelectedProject();
			Status.Update("done!");
		}
	}
}
