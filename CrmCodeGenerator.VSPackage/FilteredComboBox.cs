#region Imports

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

#endregion

namespace CrmPluginRegExt.VSPackage
{
	//-----------------------------------------------------------------------
	// <copyright file="FilteredComboBox.cs" company="DockOfTheBay">
	//     http://www.dotbay.be
	// </copyright>
	// <summary>Defines the FilteredComboBox class.</summary>
	//-----------------------------------------------------------------------
	
	/// <summary>
	///     Editable combo box which uses the text in its editable textbox to perform a lookup
	///     in its data source.
	/// </summary>
	public class FilteredComboBox : ComboBox
	{
		/// <summary>
		///     Confirm or cancel the selection when Tab, Enter, or Escape are hit.
		///     Open the DropDown when the Down Arrow is hit.
		/// </summary>
		/// <param name="e">Key Event Args.</param>
		/// <remarks>
		///     The 'KeyDown' event is not raised for Arrows, Tab and Enter keys.
		///     It is swallowed by the DropDown if it's open.
		///     So use the Preview instead.
		/// </remarks>
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Back
				 && e.Key != Key.Delete && e.Key != Key.Tab && e.Key != Key.Enter)
			{
				IsDropDownOpen = true;
				RefreshFilter();
			}
		}

		/// <summary>
		///     Modify and apply the filter.
		/// </summary>
		/// <param name="e">Key Event Args.</param>
		/// <remarks>
		///     Alternatively, you could react on 'OnTextChanged', but navigating through
		///     the DropDown will also change the text.
		/// </remarks>
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Back
				 && e.Key != Key.Delete && e.Key != Key.Tab && e.Key != Key.Enter)
			{
				IsDropDownOpen = true;
				RefreshFilter();
			}
		}

		////
		// Helpers
		////

		/// <summary>
		///     Re-apply the Filter.
		/// </summary>
		private void RefreshFilter()
		{
			if (ItemsSource != null)
			{
				var view = CollectionViewSource.GetDefaultView(ItemsSource);
				view.Refresh();
			}
		}

		/// <summary>
		///     Keep the filter if the ItemsSource is explicitly changed.
		/// </summary>
		/// <param name="oldValue">The previous value of the filter.</param>
		/// <param name="newValue">The current value of the filter.</param>
		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			if (newValue != null)
			{
				var view = CollectionViewSource.GetDefaultView(newValue);
				view.Filter += FilterPredicate;
			}

			if (oldValue != null)
			{
				var view = CollectionViewSource.GetDefaultView(oldValue);
				view.Filter -= FilterPredicate;
			}

			base.OnItemsSourceChanged(oldValue, newValue);
		}

		/// <summary>
		///     Gets a reference to the internal editable textbox.
		/// </summary>
		/// <value>A reference to the internal editable textbox.</value>
		/// <remarks>
		///     We need this to get access to the Selection.
		/// </remarks>
		protected TextBox EditableTextBox
		{
			get { return GetTemplateChild("PART_EditableTextBox") as TextBox; }
		}

		/// <summary>
		///     The Filter predicate that will be applied to each row in the ItemsSource.
		/// </summary>
		/// <param name="value">A row in the ItemsSource.</param>
		/// <returns>Whether or not the item will appear in the DropDown.</returns>
		private bool FilterPredicate(object value)
		{
			// No filter, no text
			if (value == null)
			{
				return false;
			}

			// No text, no filter
			if (Text.Length == 0)
			{
				return true;
			}

			var text = value.ToString().ToLower();

			var filterText = EditableTextBox.Text.ToLower()
				.Remove(EditableTextBox.Text.ToLower()
				.LastIndexOf(EditableTextBox.SelectedText.ToLower(), StringComparison.Ordinal));

			// Case insensitive search
			return text.Contains(filterText);
		}
	}
}
