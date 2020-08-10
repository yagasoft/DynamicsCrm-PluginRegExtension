#region Imports

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using CrmPluginEntities;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;

#endregion

namespace CrmPluginRegExt.VSPackage.Model
{
	public abstract class CrmEntityNonGeneric : INotifyPropertyChanged
	{
		public Guid Id { get; set; }

		internal bool IsUpdated;

		private string name;

		public string Name
		{
			get => name;
			set
			{
				name = value;
				OnPropertyChanged("Name");
			}
		}

		public virtual string DisplayName => Name;

		#region Events

		public event EventHandler Updated;

		protected virtual void OnUpdate()
		{
			if (Updated != null)
			{
				Updated(this, EventArgs.Empty);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

		public void UpdateInfo(string connectionString)
		{
			if (Id == Guid.Empty)
			{
				throw new Exception("Can't fetch '" + GetType().Name + "' info with empty ID.");
			}

			RunUpdateLogic(connectionString);

			IsUpdated = true;
			OnUpdate();
		}

		protected abstract void RunUpdateLogic(string connectionString);

		public TEntityType Clone<TEntityType>() where TEntityType : CrmEntityNonGeneric
		{
			return (TEntityType)MemberwiseClone();
		}
	}

	public abstract class CrmEntity<TChildType> : CrmEntityNonGeneric where TChildType : CrmEntityNonGeneric
	{
		private ObservableCollection<TChildType> children = new ObservableCollection<TChildType>();
		protected ObservableCollection<TChildType> Unfiltered { get; set; }

		public ObservableCollection<TChildType> Children
		{
			get => children;
			set
			{
				if (children == value)
				{
					return;
				}

				children = value;

				OnPropertyChanged("Children");
			}
		}

		public void Filter(string filterString)
		{
			Children = Unfiltered;

			if (Id == Guid.Empty)
			{
				return;
			}

			Children
				= new ObservableCollection<TChildType>(Children
					.Where(e => Regex.IsMatch(e.Name.ToLower(), filterString)
						|| Regex.IsMatch(e.DisplayName.ToLower(), filterString)));

			IsUpdated = true;
			OnUpdate();
		}
	}
}
