#region File header

// Project / File: CrmPluginRegExt.VSPackage / Settings.cs
//          Authors / Contributors:
//                      Ahmed el-Sawalhy (LINK Development - MBS)
//        Created: 2015 / 06 / 12
//       Modified: 2015 / 06 / 12

#endregion

#region Imports

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.Text;
using System.Text.RegularExpressions;
using CrmPluginRegExt.VSPackage.Helpers;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;

#endregion

namespace CrmPluginRegExt.VSPackage.Model
{
	[Serializable]
	public class Settings : INotifyPropertyChanged, IDeserializationCallback
	{
		#region Init

		public Settings()
		{
			InitFields();
		}

		public void OnDeserialization(object sender)
		{
			InitFields();
		}

		private void InitFields()
		{
			EntityList = EntityList ?? new ObservableCollection<string>();
			EntitiesSelected = EntitiesSelected ?? new ObservableCollection<string>();

			OnLineServers = OnLineServers ?? new ObservableCollection<string>();
			OrgList = OrgList ?? new ObservableCollection<string>();
			EntitiesSelected = EntitiesSelected ?? new ObservableCollection<string>();
			EntityList = EntityList ?? new ObservableCollection<string>();

			CrmSdkUrl = CrmSdkUrl ?? @"https://disco.crm.dynamics.com/XRMServices/2011/Discovery.svc";
			ProjectName = ProjectName ?? "";
			Domain = Domain ?? "";
			CrmOrg = CrmOrg ?? "";
			EntitiesString = EntitiesString ?? "account,contact,lead,opportunity,systemuser";
			EntitiesToIncludeString = EntitiesToIncludeString ?? "account,contact,lead,opportunity,systemuser";
			OutputPath = OutputPath ?? "";
			Username = Username ?? "";
			Password = Password ?? "";
			Namespace = Namespace ?? "";

			if (string.IsNullOrWhiteSpace(ServerPort) || Regex.IsMatch(ServerPort, "^\\d+$"))
			{
				ServerName = $"{(UseSSL ? "https" : "http")}://{ServerName}";
				ServerName += string.IsNullOrWhiteSpace(ServerPort) ? "" : $":{ServerPort}";
				ServerName += string.IsNullOrWhiteSpace(CrmOrg) ? "" : $"/{CrmOrg}";
				ServerPort = UseOffice365 ? "Office365" : (UseIFD ? "IFD" : "AD");
			}
		}

		#endregion

		#region boiler-plate INotifyPropertyChanged

		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return false;
			}
			field = value;
			Dirty = true;
			OnPropertyChanged(propertyName);
			return true;
		}

		#endregion

		public string DisplayName =>
			string.IsNullOrWhiteSpace(ProfileName)
				? (string.IsNullOrWhiteSpace(ServerName)
					? "Unnamed Profile"
					: $"{ServerName.Replace("http://", "").Replace("https://", "")} - {Username}")
				: ProfileName;

		private bool _UseSSL;
		private bool _UseIFD;
		private bool _UseOnline;
		private bool _UseOffice365;
		private string _EntitiesToIncludeString;
		private string _CrmOrg;
		private string _Password;
		private string _Username;
		private string _Domain;
		private bool _IncludeNonStandard;
		private string _Namespace;
		private string _ProjectName;
		private string _ProfileName = "";
		private string _ServerName = "";
		private string _ServerPort = "";
		private string _HomeRealm = "";
		private bool _UseWindowsAuth;
		private string _EntitiesString;
		private string _SelectPrefixes = "";

		public Guid Id { get; set; }
		public int Threads = 2;
		public int EntitiesPerThread = 5;

		public bool UseSSL
		{
			get { return _UseSSL; }
			set
			{
				if (SetField(ref _UseSSL, value))
				{
					ReEvalReadOnly();
				}
			}
		}

		public bool UseIFD
		{
			get { return _UseIFD; }
			set
			{
				if (SetField(ref _UseIFD, value))
				{
					if (value)
					{
						UseOnline = false;
						UseOffice365 = false;
						UseSSL = true;
					}
					ReEvalReadOnly();
				}
			}
		}

		public bool UseOnline
		{
			get { return _UseOnline; }
			set
			{
				if (SetField(ref _UseOnline, value))
				{
					if (value)
					{
						UseIFD = false;
						UseOffice365 = true;
						UseSSL = true;
					}
					else
					{
						UseOffice365 = false;
					}
					ReEvalReadOnly();
				}
				OnPropertyChanged("UseOnline");
			}
		}

		public bool UseOffice365
		{
			get { return _UseOffice365; }
			set
			{
				if (SetField(ref _UseOffice365, value))
				{
					if (value)
					{
						UseIFD = false;
						UseOnline = true;
						UseSSL = true;
						UseWindowsAuth = false;
					}
					ReEvalReadOnly();
				}
			}
		}

		public string EntitiesToIncludeString
		{
			get
			{
				var sb = new StringBuilder();
				foreach (var value in _EntitiesSelected)
				{
					if (sb.Length != 0)
					{
						sb.Append(',');
					}
					sb.Append(value);
				}
				return sb.ToString();
			}
			set
			{
				var newList = new ObservableCollection<string>();
				var split = value.Split(',').Select(p => p.Trim()).ToList();
				foreach (var s in split)
				{
					newList.Add(s);
					if (!_EntityList.Contains(s))
					{
						_EntityList.Add(s);
					}
				}
				EntitiesSelected = newList;
				SetField(ref _EntitiesToIncludeString, value);
				OnPropertyChanged("EnableExclude");
			}
		}

		public string CrmOrg
		{
			get { return _CrmOrg; }
			set
			{
				SetField(ref _CrmOrg, value);
				OnPropertyChanged("DisplayName");
			}
		}

		public string Password
		{
			get { return _Password; }
			set { SetField(ref _Password, value); }
		}

		public string Username
		{
			get { return _Username; }
			set
			{
				SetField(ref _Username, value);
				OnPropertyChanged("DisplayName");
			}
		}

		public string Domain
		{
			get { return _Domain; }
			set { SetField(ref _Domain, value); }
		}

		public bool IncludeNonStandard
		{
			get { return _IncludeNonStandard; }
			set { SetField(ref _IncludeNonStandard, value); }
		}

		public string Namespace
		{
			get { return _Namespace; }
			set { SetField(ref _Namespace, value); }
		}

		public string ProjectName
		{
			get { return _ProjectName; }
			set { SetField(ref _ProjectName, value); }
		}

		public string ProfileName
		{
			get { return _ProfileName; }
			set
			{
				SetField(ref _ProfileName, value);
				OnPropertyChanged("DisplayName");
			}
		}

		public string ServerName
		{
			get { return _ServerName; }
			set
			{
				SetField(ref _ServerName, value);
				OnPropertyChanged("DisplayName");
			}
		}

		public string ServerPort
		{
			get
			{
				return _ServerPort;
			}
			set { SetField(ref _ServerPort, value); }
		}

		public string HomeRealm
		{
			get { return _HomeRealm; }
			set { SetField(ref _HomeRealm, value); }
		}

		public bool UseWindowsAuth
		{
			get { return _UseWindowsAuth; }
			set
			{
				SetField(ref _UseWindowsAuth, value);
				ReEvalReadOnly();
			}
		}

		public string EntitiesString
		{
			get
			{
				var sb = new StringBuilder();

				foreach (var value in _EntityList)
				{
					if (sb.Length != 0)
					{
						sb.Append(',');
					}
					sb.Append(value);
				}

				_EntitiesString = sb.ToString();

				return _EntitiesString;
			}
			set
			{
				var split = value.Split(',').Select(p => p.Trim()).ToList();

				foreach (var s in split.Where(s => !_EntityList.Contains(s)))
				{
					_EntityList.Add(s);
				}

				_EntitiesString = value;
			}
		}

		public string SelectPrefixes
		{
			get { return _SelectPrefixes; }
			set { _SelectPrefixes = value; }
		}

		#region Non serialisable

		[field: NonSerialized] private string _CrmSdkUrl;
		[field: NonSerialized] private string _OutputPath;
		[field: NonSerialized] private string _Folder = "";
		[field: NonSerialized] private ObservableCollection<string> _OnLineServers;
		[field: NonSerialized] private ObservableCollection<string> _OrgList;
		[field: NonSerialized] public ObservableCollection<string> _EntitiesSelected;
		[field: NonSerialized] public ObservableCollection<string> _EntityList;

		public string CrmSdkUrl
		{
			get { return _CrmSdkUrl; }
			set { SetField(ref _CrmSdkUrl, value); }
		}

		public string OutputPath
		{
			get { return _OutputPath; }
			set { SetField(ref _OutputPath, value); }
		}

		public string Folder
		{
			get { return _Folder; }
			set { SetField(ref _Folder, value); }
		}

		public ObservableCollection<string> OnLineServers
		{
			get { return _OnLineServers; }
			set { SetField(ref _OnLineServers, value); }
		}


		public ObservableCollection<string> OrgList
		{
			get { return _OrgList; }
			set { SetField(ref _OrgList, value); }
		}

		public ObservableCollection<string> EntityList
		{
			get { return _EntityList; }
			set { SetField(ref _EntityList, value); }
		}

		public ObservableCollection<string> EntitiesSelected
		{
			get { return _EntitiesSelected; }
			set { SetField(ref _EntitiesSelected, value); }
		}

		public IOrganizationService CrmConnection { get; set; }

		public bool Dirty { get; set; }

		#endregion

		#region Read Only Properties

		private void ReEvalReadOnly()
		{
			OnPropertyChanged("NeedServer");
			OnPropertyChanged("NeedOnlineServer");
			OnPropertyChanged("NeedServerPort");
			OnPropertyChanged("NeedHomeRealm");
			OnPropertyChanged("NeedCredentials");
			OnPropertyChanged("CanUseWindowsAuth");
			OnPropertyChanged("CanUseSSL");
		}

		public bool NeedServer
		{
			get { return !(UseOnline || UseOffice365); }
		}

		public bool NeedOnlineServer
		{
			get { return (UseOnline || UseOffice365); }
		}

		public bool NeedServerPort
		{
			get { return !(UseOffice365 || UseOnline); }
		}

		public bool NeedHomeRealm
		{
			get { return !(UseIFD || UseOffice365 || UseOnline); }
		}

		public bool NeedCredentials
		{
			get { return !UseWindowsAuth; }
		}

		public bool CanUseWindowsAuth
		{
			get { return !(UseIFD || UseOnline || UseOffice365); }
		}

		public bool CanUseSSL
		{
			get { return !(UseOnline || UseOffice365 || UseIFD); }
		}

		#endregion

		#region Conntection Strings

		public AuthenticationProviderType AuthType
		{
			get
			{
				if (UseIFD)
				{
					return AuthenticationProviderType.Federation;
				}
				else if (UseOffice365)
				{
					return AuthenticationProviderType.OnlineFederation;
				}
				else if (UseOnline)
				{
					return AuthenticationProviderType.LiveId;
				}

				return AuthenticationProviderType.ActiveDirectory;
			}
		}

		public Uri GetDiscoveryUri()
		{
			var url =
				$"{(UseSSL ? "https" : "http")}://{(UseIFD ? ServerName : UseOffice365 ? "disco." + ServerName : UseOnline ? "dev." + ServerName : ServerName)}:{(ServerPort.Length == 0 ? (UseSSL ? 443 : 80) : int.Parse(ServerPort))}";
			return new Uri(url + "/XRMServices/2011/Discovery.svc");
		}

		public string GetDiscoveryCrmConnectionString()
		{
			var connectionString = string.Format("Url={0}://{1}:{2};",
				UseSSL ? "https" : "http",
				UseIFD ? ServerName : UseOffice365 ? "disco." + ServerName : UseOnline ? "dev." + ServerName : ServerName,
				ServerPort.Length == 0 ? (UseSSL ? 443 : 80) : int.Parse(ServerPort));

			if (!UseWindowsAuth)
			{
				if (!UseIFD)
				{
					if (!string.IsNullOrEmpty(Domain))
					{
						connectionString += string.Format("Domain={0};", Domain);
					}
				}

				var sUsername = Username;
				if (UseIFD)
				{
					if (!string.IsNullOrEmpty(Domain))
					{
						sUsername = string.Format("{0}\\{1}", Domain, Username);
					}
				}

				connectionString += string.Format("Username={0};Password={1};", sUsername, Password);
			}

			if (UseOnline && !UseOffice365)
			{
				ClientCredentials deviceCredentials;

				do
				{
					deviceCredentials = DeviceIdManager.LoadDeviceCredentials() ??
					                    DeviceIdManager.RegisterDevice();
				} while (deviceCredentials.UserName.Password.Contains(";")
				         || deviceCredentials.UserName.Password.Contains("=")
				         || deviceCredentials.UserName.Password.Contains(" ")
				         || deviceCredentials.UserName.UserName.Contains(";")
				         || deviceCredentials.UserName.UserName.Contains("=")
				         || deviceCredentials.UserName.UserName.Contains(" "));

				connectionString += string.Format("DeviceID={0};DevicePassword={1};",
					deviceCredentials.UserName.UserName,
					deviceCredentials.UserName.Password);
			}

			if (UseIFD && !string.IsNullOrEmpty(HomeRealm))
			{
				connectionString += string.Format("HomeRealmUri={0};", HomeRealm);
			}

			return connectionString;
		}


		private string currentString = "";
		private string checkString = "";

		private string GetCurrentUniqueConnectionString()
		{
			var connectionString = $"AuthType={ServerPort.Trim(';')};Url={ServerName.Trim(';')};";

			if (!UseWindowsAuth)
			{
				if (!string.IsNullOrWhiteSpace(Domain))
				{
					connectionString += $"Domain={Domain.Trim(';')};";
				}

				connectionString += $"Username={Username.Trim(';')};Password={Password.Trim(';')};";
			}

			if (!string.IsNullOrWhiteSpace(HomeRealm))
			{
				connectionString += $"HomeRealmUri={HomeRealm.Trim(';')};";
			}

			return connectionString;
		}

		public string GetOrganizationCrmConnectionString()
		{
			return GetCurrentUniqueConnectionString();
		}

		#endregion
	}
}
