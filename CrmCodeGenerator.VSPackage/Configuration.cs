#region Imports

using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using CrmPluginRegExt.VSPackage.Helpers;
using CrmPluginRegExt.VSPackage.Model;
using EnvDTE;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Yagasoft.Libraries.Common;

#endregion

namespace CrmPluginRegExt.VSPackage
{
	public class Configuration
	{
		private const string FileName = "PluginRegExt-Config.json";

		public static SettingsArray LoadSettings()
		{
			try
			{
				Status.Update("Loading settings ... ");

				var dte = (DTE)Package.GetGlobalService(typeof(SDTE));
				var file = $@"{dte.Solution.GetPath()}\{FileName}";

				if (File.Exists(file))
				{
					Status.Update($"\tFound settings file: {file}.");
					Status.Update($"\tReading content ...");

					var fileContent = File.ReadAllText(file);
					var settings = JsonConvert.DeserializeObject<SettingsArray>(fileContent);

					Status.Update(">>> Finished loading settings.");

					return settings;
				}

				Status.Update("\tSettings file does not exist.");
			}
			catch (Exception ex)
			{
				Status.Update("\t [ERROR] Failed to load settings.");
				Status.Update(ex.BuildExceptionMessage(isUseExStackTrace: true));
			}

			var newSettings = new SettingsArray();
			newSettings.SettingsList.Add(new Settings());
			newSettings.SelectedSettingsIndex = 0;

			return newSettings;
		}

		public static void SaveConfigs(SettingsArray settings)
		{
			Status.Update("Writing settings ... ");

			var dte = (DTE)Package.GetGlobalService(typeof(SDTE));
			var file = $@"{dte.Solution.GetPath()}\{FileName}";

			if (!File.Exists(file))
			{
				File.Create(file).Dispose();
				Status.Update("\tCreated a new settings file.");
			}

			Status.Update("\tSerialising settings ...");
			var serialisedSettings = JsonConvert.SerializeObject(settings, Formatting.Indented);

			Status.Update("\tWriting to file ...");
			File.WriteAllText(file, serialisedSettings);

			Status.Update(">>> Finished writing settings.");
		}
	}
}
