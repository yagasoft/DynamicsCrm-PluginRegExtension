#region Imports

using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Yagasoft.CrmPluginRegistration.Helpers;
using Yagasoft.Libraries.Common;

#endregion

namespace CrmPluginRegExt.VSPackage
{
	public class FilteredComboBox : ComboBox
	{
		private readonly IList<Key> buffer = new List<Key>();

		private Timer decay;
		private readonly object decaySync = new();

		protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			IsDropDownOpen = true;
		}

		protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			lock (decaySync)
			{
				decay?.Stop();
				buffer.Clear();
			}
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			lock (decaySync)
			{
				decay?.Stop();
				decay = new(300);
				decay.Elapsed += (_, _) => Filter();
				decay.AutoReset = false;
				decay.Enabled = true;
			}

			if (e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Left && e.Key != Key.Right
				&& e.Key != Key.Tab && e.Key != Key.Enter)
			{
				lock (buffer)
				{
					buffer.Add(e.Key);
				}
			}
		}

		private void Filter()
		{
			bool isBuffered;

			lock (buffer)
			{
				isBuffered = buffer.Any();
				buffer.Clear();
			}

			if (isBuffered)
			{
				try
				{
					// credit: AnjumSKhan (https://stackoverflow.com/a/34390669/1919456)
					Dispatcher.InvokeAsync(
						() =>
						{
							var itemsViewOriginal = (CollectionView)CollectionViewSource.GetDefaultView(ItemsSource);

							itemsViewOriginal.Filter =
								o => Text.IsEmpty()
									|| (o is not string && o != null)
									|| ((string)o)?.Contains(Text) == true;
						});
				}
				catch
				{
					// will check later how a control is disposed and properly handle issues that arise
				}
			}
		}
	}
}
