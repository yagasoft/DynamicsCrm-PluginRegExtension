﻿#region Imports

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.CrmPluginRegistration.Model
{
	public class Settings : INotifyPropertyChanged
	{
		public Guid? Id { get; set; }

		#region Init

		public Settings()
		{
			InitFields();
		}

		private void InitFields()
		{
			ConnectionString = ConnectionString ?? "";
			Id = Id ?? Guid.NewGuid();
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
			OnPropertyChanged(propertyName);
			return true;
		}

		#endregion

		public string DisplayName => ProfileName;

		private string profileName = "";
		private string connectionString = "";

		public string ProfileName
		{
			get => profileName;
			set
			{
				SetField(ref profileName, value);
				OnPropertyChanged("DisplayName");
			}
		}

		[JsonIgnore]
		public string ConnectionString
		{
			get => connectionString;
			set
			{
				var clauses = value?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(e => e.Trim()).Where(e => e.Contains("=")).ToArray();

				if (clauses?.Any() == true)
				{
					var subclauses = clauses.Select(e => e.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries)
						.Select(s => s.Trim())).ToArray();
					var longestKeyLength = subclauses.Select(e => e.FirstOrDefault()?.Length ?? 0).Max();
					clauses = subclauses?
						.Select(e => e.StringAggregate(" = ".PadLeft(longestKeyLength + 3 - e.FirstOrDefault()?.Length ?? 0)))
						.ToArray();
				}

				var formattedString = clauses.StringAggregate(";\r\n").Trim();

				SetField(ref connectionString, formattedString);
				OnPropertyChanged();
			}
		}
	}
}
