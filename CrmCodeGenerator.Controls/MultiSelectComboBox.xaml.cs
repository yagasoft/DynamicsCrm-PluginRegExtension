#region Imports

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

#endregion

namespace CrmCodeGenerator.Controls
{
	/// <summary>
	///     Interaction logic for MultiSelectComboBox.xaml
	///     From Original Source from http://www.codeproject.com/Articles/563862/Multi-Select-ComboBox-in-WPF  modified to use
	///     a simple collection
	/// </summary>
	public partial class MultiSelectComboBox : UserControl
	{
		private readonly ObservableCollection<Node> nodeList;

		public MultiSelectComboBox()
		{
			InitializeComponent();
			nodeList = new ObservableCollection<Node>();
		}

		public Action<string> ButtonAction { get; set; }

		#region Dependency Properties

		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof (Collection<string>), typeof (MultiSelectComboBox),
				new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

		public static readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.Register("SelectedItems", typeof (Collection<string>), typeof (MultiSelectComboBox),
				new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));

		public static readonly DependencyProperty FilteredItemsProperty =
			DependencyProperty.Register("FilteredItems", typeof (Collection<string>), typeof (MultiSelectComboBox),
				new FrameworkPropertyMetadata(null, OnFilteredItemsChanged));

		public static readonly DependencyProperty IsButtonVisibleProperty =
			DependencyProperty.Register("IsButtonVisible", typeof (bool), typeof (MultiSelectComboBox),
				new FrameworkPropertyMetadata(false, OnIsButtonVisibleChanged));

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof (string), typeof (MultiSelectComboBox),
				new UIPropertyMetadata(string.Empty));

		public static readonly DependencyProperty DefaultTextProperty =
			DependencyProperty.Register("DefaultText", typeof (string), typeof (MultiSelectComboBox),
				new UIPropertyMetadata(string.Empty));


		public Collection<string> ItemsSource
		{
			get { return (Collection<string>) GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public Collection<string> SelectedItems
		{
			get { return (Collection<string>) GetValue(SelectedItemsProperty); }
			set { SetValue(SelectedItemsProperty, value); }
		}

		public Collection<string> FilteredItems
		{
			get { return (Collection<string>) GetValue(FilteredItemsProperty); }
			set { SetValue(FilteredItemsProperty, value); }
		}

		public bool IsButtonVisible
		{
			get { return (bool)GetValue(IsButtonVisibleProperty); }
			set { SetValue(IsButtonVisibleProperty, value); }
		}

		public string Text
		{
			get { return (string) GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public string DefaultText
		{
			get { return (string) GetValue(DefaultTextProperty); }
			set { SetValue(DefaultTextProperty, value); }
		}

		#endregion

		#region Events

		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (MultiSelectComboBox) d;
			control.DisplayInControl();
		}

		private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (MultiSelectComboBox) d;
			control.SelectNodes();
			control.SetText();
		}

		private static void OnFilteredItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (MultiSelectComboBox) d;
			control.FilterNodes();
		}

		private static void OnIsButtonVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (MultiSelectComboBox) d;
			control.SetButtonVisibility(control.IsButtonVisible);
		}

		private void CheckBox_Click(object sender, RoutedEventArgs e)
		{
			var clickedBox = (CheckBox) sender;

			if ((string) clickedBox.Content == "All")
			{
				if (clickedBox.IsChecked.Value)
				{
					foreach (var node in nodeList)
					{
						node.IsSelected = true;
					}
				}
				else
				{
					foreach (var node in nodeList)
					{
						node.IsSelected = false;
					}
				}
			}
			else
			{
				var _selectedCount = 0;
				foreach (var s in nodeList)
				{
					if (s.IsSelected && s.Title != "All")
					{
						_selectedCount++;
					}
				}
				if (_selectedCount == nodeList.Count - 1)
				{
					nodeList.FirstOrDefault(i => i.Title == "All").IsSelected = true;
				}
				else
				{
					nodeList.FirstOrDefault(i => i.Title == "All").IsSelected = false;
				}
			}
			SetSelectedItems();
			SetText();
		}

		private void MultiSelectCombo_DropDownClosed(object sender, EventArgs e)
		{
			SortNodes();
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			var comboBox = (sender as Button).GetParent<ComboBoxItem>();
			var logicalName = (string) comboBox.Content.GetType().GetProperty("Title")
				                           .GetValue(comboBox.Content, new object[] {});

			ButtonAction(logicalName);
		}

		#endregion

		#region Methods

		private void SelectNodes()
		{
			nodeList.ToList().ForEach(node => node.IsSelected = false);

			foreach (var keyValue in SelectedItems)
			{
				var node = nodeList.FirstOrDefault(i => i.Title == keyValue.Replace("_", "__"));
				if (node != null)
				{
					node.IsSelected = true;
				}
			}

			SortNodes();

			if (SelectedItems.Count + 1 < nodeList.Count)
			{
				return;
			}

			var allNode = nodeList.FirstOrDefault(i => i.Title == "All");

			if (allNode != null)
			{
				allNode.IsSelected = true;
			}
		}

		private void FilterNodes()
		{
			nodeList.ToList().ForEach(node => node.IsFiltered = false);

			foreach (var keyValue in FilteredItems)
			{
				var node = nodeList.FirstOrDefault(i => i.Title == keyValue.Replace("_", "__"));
				if (node != null)
				{
					node.IsFiltered = true;
				}
			}
		}

		private void SetButtonVisibility(bool visible)
		{
			nodeList.ToList().ForEach(node => node.IsButtonVisible = visible);
		}

		private void SetSelectedItems()
		{
			if (SelectedItems == null)
			{
				SelectedItems = new Collection<string>();
			}
			SelectedItems.Clear();
			foreach (var node in nodeList)
			{
				if (node.IsSelected && node.Title != "All")
				{
					if (ItemsSource.Count > 0)
					{
						SelectedItems.Add(node.Title.Replace("__", "_"));
					}
				}
			}
		}

		private void DisplayInControl()
		{
			nodeList.Clear();
			if (ItemsSource.Count > 0)
			{
				nodeList.Add(new Node("All", IsButtonVisible));
			}
			foreach (var keyValue in ItemsSource)
			{
				var node = new Node(keyValue, IsButtonVisible);
				nodeList.Add(node);
			}
		}

		private void SetText()
		{
			if (SelectedItems != null)
			{
				var displayText = new StringBuilder();
				foreach (var s in nodeList)
				{
					if (s.IsSelected == true && s.Title == "All")
					{
						displayText = new StringBuilder();
						displayText.Append("All");
						break;
					}
					else if (s.IsSelected == true && s.Title != "All")
					{
						displayText.Append(s.Title.Replace("__", "_"));
						displayText.Append(',');
					}
				}
				Text = displayText.ToString().TrimEnd(new char[] {','});
			}
			// set DefaultText if nothing else selected
			if (string.IsNullOrEmpty(Text))
			{
				Text = DefaultText;
			}
		}

		private void SortNodes()
		{
			var empty = nodeList.FirstOrDefault(node => string.IsNullOrEmpty(node.Title));

			if (empty != null)
			{
				nodeList.Remove(empty);
			}

			var temp = nodeList.FirstOrDefault(node => node.Title == "All");

			if (temp != null)
			{
				nodeList.Remove(temp);
			}

			var sorted = nodeList.OrderByDescending(node => node.IsSelected)
				.ThenBy(node => node.Title).ToList();

			if (temp != null)
			{
				sorted.Reverse();
				sorted.Add(temp);
				sorted.Reverse();
			}

			if (empty != null)
			{
				sorted.Add(empty);
			}

			nodeList.Clear();
			sorted.ForEach(node => nodeList.Add(node));

			MultiSelectCombo.ItemsSource = nodeList;
		}

		#endregion
	}

	public class Node : INotifyPropertyChanged
	{
		private string title;
		private bool isSelected;
		private bool isFiltered;

		#region ctor

		public Node(string title, bool buttonVisible)
		{
			Title = title;
			IsButtonVisible = buttonVisible;
		}

		#endregion

		#region Properties

		public string Title
		{
			get { return title.Replace("_", "__"); }
			set
			{
				title = value;
				NotifyPropertyChanged("Title");
			}
		}

		public bool IsSelected
		{
			get { return isSelected; }
			set
			{
				isSelected = value;
				NotifyPropertyChanged("IsSelected");
			}
		}

		public Brush Colour => IsFiltered ? Brushes.Red : Brushes.Black;

		public bool IsFiltered
		{
			get { return isFiltered; }
			set
			{
				isFiltered = value;
				NotifyPropertyChanged("IsFiltered");
				NotifyPropertyChanged("Colour");
			}
		}

		public bool IsButtonVisible { get; set; }

		public Visibility ButtonVisibility => IsButtonVisible ? Visibility.Visible : Visibility.Collapsed;

		public bool IsButtonEnabled => !string.IsNullOrEmpty(Title) && Title != "All";

		#endregion

		#region Property events

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion
	}

	public static class Extensions
	{
		public static T GetChild<T>(this DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null)
			{
				return null;
			}

			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				var child = VisualTreeHelper.GetChild(depObj, i);

				var result = (child as T) ?? GetChild<T>(child);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}

		public static T GetParent<T>(this DependencyObject child) where T : DependencyObject
		{
			while (true)
			{
				//get parent item
				var parentObject = VisualTreeHelper.GetParent(child);

				//we've reached the end of the tree
				if (parentObject == null)
				{
					return null;
				}

				//check if the parent matches the type we're looking for
				var parent = parentObject as T;

				if (parent != null)
				{
					return parent;
				}

				child = parentObject;
			}
		}
	}
}
