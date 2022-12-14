#region Imports

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Xrm.Sdk;
using Yagasoft.CrmPluginRegistration.Model;
using Yagasoft.Libraries.Common;

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

		public StepImage(CrmStepImage crmImage)
		{
			CrmImage = crmImage;

			InitializeComponent();
		}

		private void DialogWindow_Loaded(object sender, RoutedEventArgs e)
		{
			DataContext = CrmImage;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (CrmImage.Name.IsEmpty() || CrmImage.EntityAlias.IsEmpty())
			{
				PopException(new Exception("Name and Entity Alias must be filled."));
				return;
			}

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

		private void PopException(Exception exception)
		{
			Dispatcher.Invoke(
				() =>
				{
					var message = exception.Message
						+ (exception.InnerException != null ? "\n" + exception.InnerException.Message : "");
					MessageBox.Show(message, exception.GetType().FullName, MessageBoxButton.OK, MessageBoxImage.Error);
				});
		}

	}
}
