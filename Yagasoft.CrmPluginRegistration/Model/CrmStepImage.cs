﻿#region Imports

using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CrmPluginEntities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Yagasoft.CrmPluginRegistration.Connection;

#endregion

namespace Yagasoft.CrmPluginRegistration.Model
{
	public class CrmStepImage : CrmEntityNonGeneric
	{
		#region Properties

		private string entityAlias;

		public string EntityAlias
		{
			get => entityAlias;
			set
			{
				entityAlias = value;
				OnPropertyChanged("EntityAlias");
			}
		}

		private bool isPreImage;

		public bool IsPreImage
		{
			get => isPreImage;
			set
			{
				isPreImage = value;

				if (IsPreImage && IsPostImage)
				{
					ImageType = SdkMessageProcessingStepImage.Enums.ImageType.Both;
				}
				else if (IsPreImage)
				{
					ImageType = SdkMessageProcessingStepImage.Enums.ImageType.PreImage;
				}
				else if (IsPostImage)
				{
					ImageType = SdkMessageProcessingStepImage.Enums.ImageType.PostImage;
				}

				OnPropertyChanged("IsPreImage");
			}
		}

		private bool isPostImage;

		public bool IsPostImage
		{
			get => isPostImage;
			set
			{
				isPostImage = value;

				if (IsPreImage && IsPostImage)
				{
					ImageType = SdkMessageProcessingStepImage.Enums.ImageType.Both;
				}
				else if (IsPreImage)
				{
					ImageType = SdkMessageProcessingStepImage.Enums.ImageType.PreImage;
				}
				else if (IsPostImage)
				{
					ImageType = SdkMessageProcessingStepImage.Enums.ImageType.PostImage;
				}

				OnPropertyChanged("IsPostImage");
			}
		}

		public bool IsPreImageEnabled => Step.Message == "Update" || Step.Message == "Delete";

		public bool IsPostImageEnabled => (Step.Message == "Create"
			&& Step.Stage == SdkMessageProcessingStep.Enums.Stage.Postoperation)
			|| (Step.Message == "Update"
				&& Step.Stage == SdkMessageProcessingStep.Enums.Stage.Postoperation);

		public SdkMessageProcessingStepImage.Enums.ImageType ImageType { get; set; }

		private ObservableCollection<string> attributeList = new ObservableCollection<string>();

		public ObservableCollection<string> AttributeList
		{
			get => attributeList;
			set
			{
				attributeList = value;
				OnPropertyChanged("AttributeList");
			}
		}

		private ObservableCollection<string> attributesSelected = new ObservableCollection<string>();

		public ObservableCollection<string> AttributesSelected
		{
			get => attributesSelected;
			set
			{
				attributesSelected = value;
				OnPropertyChanged("AttributesSelected");
			}
		}

		public string AttributesSelectedString
		{
			get
			{
				if (AttributesSelected.Count == AttributeList.Count)
				{
					return "";
				}

				var sb = new StringBuilder();

				foreach (var value in AttributesSelected)
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
				// if the string is empty, then all attributes are selected!
				if (string.IsNullOrEmpty(value))
				{
					AttributesSelected = new ObservableCollection<string>(AttributeList);
				}
				else
				{
					var newList = new ObservableCollection<string>();

					var split = value.Split(',').Select(p => p.Trim()).ToList();

					foreach (var s in split)
					{
						newList.Add(s);
					}

					AttributesSelected = newList;
				}

				OnPropertyChanged("AttributesSelectedString");
			}
		}

		private CrmTypeStep step;

		public CrmTypeStep Step
		{
			get => step;
			set
			{
				step = value;
				IsPreImage = IsPreImageEnabled;
				IsPostImage = IsPostImageEnabled;
			}
		}

		#endregion

		public CrmStepImage(IConnectionManager connectionManager) : base(connectionManager)
		{ }

		protected override void RunUpdateLogic()
		{
			using (var context = new XrmServiceContext(ConnectionManager.Get()) {MergeOption = MergeOption.NoTracking})
			{
				var result =
					(from imageQ in context.SdkMessageProcessingStepImageSet
					 where imageQ.SdkMessageProcessingStepImageId == Id
					 select new
							{
								id = imageQ.SdkMessageProcessingStepImageId.Value,
								name = imageQ.Name,
								entityAlias = imageQ.EntityAlias,
								attributes = imageQ.Attributes1,
								imageType = imageQ.ImageType.Value,
							}).ToList();

				if (!result.Any())
				{
					AttributesSelectedString = null;
					return;
				}

				var image = result.First();

				Name = image.name;
				EntityAlias = image.entityAlias;
				IsPreImage = image.imageType == (int)SdkMessageProcessingStepImage.Enums.ImageType.PreImage
					|| image.imageType == (int)SdkMessageProcessingStepImage.Enums.ImageType.Both;
				IsPostImage = image.imageType == (int)SdkMessageProcessingStepImage.Enums.ImageType.PostImage
					|| image.imageType == (int)SdkMessageProcessingStepImage.Enums.ImageType.Both;
				AttributesSelectedString = image.attributes;
			}
		}
	}
}
