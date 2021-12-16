#region Imports

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

#endregion

namespace Yagasoft.CrmPluginRegistration.Model
{
	public class SettingsArray : INotifyPropertyChanged
	{
		private int selectedSettingsIndex;
		private ObservableCollection<Settings> settingsList = new ObservableCollection<Settings>();

		public int SelectedSettingsIndex
		{
			get => selectedSettingsIndex;
			set
			{
				selectedSettingsIndex = value;
				OnPropertyChanged();
			}
		}

		[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
		public ObservableCollection<Settings> SettingsList
		{
			get => settingsList;
			set
			{
				settingsList = value;
				OnPropertyChanged();
			}
		}

		public Settings GetSelectedSettings()
		{
			return SettingsList[SelectedSettingsIndex];
		}

		#region Property events

		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
