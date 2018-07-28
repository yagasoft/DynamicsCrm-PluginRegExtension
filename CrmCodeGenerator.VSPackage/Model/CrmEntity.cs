#region Imports

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using CrmPluginEntities;
using Microsoft.Xrm.Sdk;

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
			get { return name; }
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

		public void UpdateInfo(XrmServiceContext context)
		{
			if (Id == Guid.Empty)
			{
				throw new Exception("Can't fetch '" + GetType().Name + "' info with empty ID.");
			}

			RunUpdateLogic(context);

			IsUpdated = true;
			OnUpdate();
		}

		protected abstract void RunUpdateLogic(XrmServiceContext context);

		public TEntityType Clone<TEntityType>() where TEntityType : CrmEntityNonGeneric
		{
			return (TEntityType)MemberwiseClone();
		}
	}

	public abstract class CrmEntity<TChildType> : CrmEntityNonGeneric where TChildType : CrmEntityNonGeneric
	{
		private ObservableCollection<TChildType> children = new ObservableCollection<TChildType>();
		public ObservableCollection<TChildType> Children
		{
			get { return children; }
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
	}
}
