#region Imports

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using CrmPluginRegExt.VSPackage.Helpers;
using CrmPluginRegExt.VSPackage.Model;
using EnvDTE;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;

#endregion

namespace CrmPluginRegExt.VSPackage
{
	public class Configuration
	{
		private const string FileName = "PluginRegExt";

		public static SettingsArray LoadSettings()
		{
			try
			{
				Status.Update("Loading settings ... ");

				var file = $@"{BuildBaseFileName()}-Config.json";

				if (File.Exists(file))
				{
					Status.Update($"\tFound settings file: {file}.");
					Status.Update($"\tReading content ...");

					var fileContent = File.ReadAllText(file);
					var settings = JsonConvert.DeserializeObject<SettingsArray>(fileContent);
					FillConnectionStrings(settings?.SettingsList);

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

		private static void FillConnectionStrings(IEnumerable<Settings> settings)
		{
			var connectionStrings = LoadConnections();

			if (connectionStrings == null)
			{
				return;
			}

			foreach (var s in settings)
			{
				if (connectionStrings.TryGetValue(s.Id.GetValueOrDefault(), out var c))
				{
					s.ConnectionString = c;
				}
			}
		}

		private static IDictionary<Guid, string> LoadConnections()
		{
			var baseFileName = BuildBaseFileName();

			var file = $@"{baseFileName}-Connection.dat";

			IDictionary<Guid, string> connectionStrings = null;

			if (File.Exists(file))
			{
				Status.Update($"\tFound connection file: {file}.");
				Status.Update($"\tReading content ...");

				var fileContent = File.ReadAllText(file);
				var serialised = Encoding.UTF8.GetString(Convert.FromBase64String(fileContent));
				connectionStrings = JsonConvert.DeserializeObject<IDictionary<Guid, string>>(serialised);
			}

			return connectionStrings;
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

			SaveConnection(settings.SettingsList);

			Status.Update(">>> Finished writing settings.");
		}

		private static void SaveConnection(IEnumerable<Settings> settings)
		{
			var baseFileName = BuildBaseFileName();

			var file = $@"{baseFileName}-Connection.dat";

			if (!File.Exists(file))
			{
				File.Create(file).Dispose();
				Status.Update("\tCreated a new connection file.");
			}

			Status.Update("\tWriting to file ...");

			var serialised = JsonConvert.SerializeObject(settings.ToDictionary(e => e.Id, e => e.ConnectionString));
			var encodedString = Convert.ToBase64String(Encoding.UTF8.GetBytes(serialised));
			File.WriteAllText(file, encodedString);
		}

		private static string BuildBaseFileName()
		{
			var dte = (DTE)Package.GetGlobalService(typeof(SDTE));
			return $@"{dte.Solution.GetPath()}\{FileName}";
		}
	}
}
