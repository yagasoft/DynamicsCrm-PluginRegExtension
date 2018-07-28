using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk.Metadata;
using CrmPluginRegExt.VSPackage.Helpers;
using Microsoft.Xrm.Sdk;

namespace CrmPluginRegExt.VSPackage.Model
{
    [Serializable]
    public class MappingEnum
    {
		public Guid? MetadataId { get; set; }
		public string DisplayName { get; set; }
		public string LogicalName { get; set; }
        public MapperEnumItem[] Items { get; set; }

		public static void UpdateMappingEnum(EnumAttributeMetadata picklist, MappingEnum mappingEnum)
		{
			if (picklist.SchemaName != null)
			{
				mappingEnum.DisplayName = Naming.GetProperVariableName(Naming.GetProperVariableName(picklist.SchemaName));
			}

			mappingEnum.LogicalName = picklist.LogicalName ?? mappingEnum.LogicalName;

			if (picklist.OptionSet != null)
			{
				mappingEnum.Items =
					picklist.OptionSet.Options.Select(
						o => new MapperEnumItem
						{
							Attribute = new CrmPicklistAttribute
							{
								DisplayName = o.Label.UserLocalizedLabel.Label,
								Value = o.Value ?? 1,
								LocalizedLabels = o.Label.LocalizedLabels.Select(label => new LocalizedLabelSerialisable
								                                                          {
									                                                          LanguageCode = label.LanguageCode,
																							  Label = label.Label
								                                                          }).ToArray()
							},
							Name = Naming.GetProperVariableName(o.Label.UserLocalizedLabel.Label)
						}
						).ToArray();
			}

			Dictionary<string, int> duplicates = new Dictionary<string, int>();

			foreach (var i in mappingEnum.Items)
				if (duplicates.ContainsKey(i.Name))
				{
					duplicates[i.Name] = duplicates[i.Name] + 1;
					i.Name += "_" + duplicates[i.Name];
				}
				else
					duplicates[i.Name] = 1;
		}

		public static MappingEnum GetMappingEnum(EnumAttributeMetadata picklist)
        {
			var enm = new MappingEnum
			          {
				          DisplayName = Naming.GetProperVariableName(Naming.GetProperVariableName(picklist.SchemaName)),
							LogicalName = picklist.LogicalName,
				          Items =
					          picklist.OptionSet.Options.Select(
						          o => new MapperEnumItem
						               {
							               Attribute = new CrmPicklistAttribute
							                           {
								                           DisplayName = o.Label.UserLocalizedLabel.Label,
								                           Value = o.Value ?? 1,
														   LocalizedLabels = o.Label.LocalizedLabels.Select(label => new LocalizedLabelSerialisable
														   {
															   LanguageCode = label.LanguageCode,
															   Label = label.Label
														   }).ToArray()
													   },
							               Name = Naming.GetProperVariableName(o.Label.UserLocalizedLabel.Label)
						               }
					          ).ToArray(),
				          MetadataId = picklist.MetadataId
			          };

			Dictionary<string, int> duplicates = new Dictionary<string, int>();

            foreach (var i in enm.Items)
                if (duplicates.ContainsKey(i.Name))
                {
                    duplicates[i.Name] = duplicates[i.Name] + 1;
                    i.Name += "_" + duplicates[i.Name];
                }
                else
                    duplicates[i.Name] = 1;

            return enm;
        }
    }

    [Serializable]
    public class MapperEnumItem
    {
        public CrmPicklistAttribute Attribute { get; set; }

        public string Name { get; set; }

        public int Value
        {
            get
            {
                return Attribute.Value;
            }
        }

		public string DisplayName
		{
			get
			{
				return Attribute.DisplayName;
			}
		}

		public LocalizedLabelSerialisable[] LocalizedLabels
		{
			get
			{
				return Attribute.LocalizedLabels;
			}
		}
	}
}
