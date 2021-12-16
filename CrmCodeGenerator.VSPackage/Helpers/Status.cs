#region Imports

using System;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace CrmPluginRegExt.VSPackage.Helpers
{
	public static class Status
	{
		private static bool isNewLine = true;

		public static void Update(string message, bool newLine = true)
		{
			//Configuration.Instance.DTE.ExecuteCommand("View.Output");
			var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
			var win = dte.Windows.Item("{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}");
			win.Visible = true;

			var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
			var guidGeneral = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
			IVsOutputWindowPane pane;
			var hr = outputWindow.CreatePane(guidGeneral, "Plugin Registration Extension", 1, 0);
			hr = outputWindow.GetPane(guidGeneral, out pane);
			pane.Activate();

			if (isNewLine)
			{
				message = string.Format("{0:yyyy/MMM/dd hh:mm:ss tt: }", DateTime.Now) + message;
			}

			pane.OutputString(message);

			if (newLine)
			{
				pane.OutputString("\n");
			}

			pane.FlushToTaskList();
			Application.DoEvents();

			isNewLine = newLine;
		}

		public static void Clear()
		{
			var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
			var guidGeneral = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
			IVsOutputWindowPane pane;
			var hr = outputWindow.CreatePane(guidGeneral, "Crm Code Generator", 1, 0);
			hr = outputWindow.GetPane(guidGeneral, out pane);
			pane.Clear();
			pane.FlushToTaskList();
			Application.DoEvents();
		}
	}
}
