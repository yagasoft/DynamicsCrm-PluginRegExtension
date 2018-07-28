#region Imports

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CrmPluginEntities;
using CrmPluginRegExt.VSPackage.Model;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Xrm.Sdk;

#endregion

namespace CrmPluginRegExt.VSPackage.Dialogs
{
	/// <summary>
	///     Interaction logic for StepImage.xaml
	/// </summary>
	public partial class StepImage : INotifyPropertyChanged
	{
		#region Properties

		public CrmStepImage CrmImage { get; set; }
		public XrmServiceContext Context { get; set; }
		public bool IsUpdate { get; set; }

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

		public StepImage(CrmStepImage crmImage, XrmServiceContext context)
		{
			Context = context;
			CrmImage = crmImage;

			InitializeComponent();
		}

		private void DialogWindow_Loaded(object sender, RoutedEventArgs e)
		{
			DataContext = CrmImage;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			IsUpdate = true;
			Dispatcher.InvokeAsync(Close);
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			Dispatcher.InvokeAsync(Close);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Dispatcher.Invoke(Close);
			}

			base.OnKeyDown(e);
		}
	}
}
