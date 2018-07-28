#region File header

// Project / File: CrmPluginRegExt.VSPackage / MappingRelationshipN1.cs
//          Authors / Contributors:
//                      Ahmed el-Sawalhy (LINK Development - MBS)
//        Created: 2015 / 06 / 12
//       Modified: 2015 / 06 / 12

#endregion

#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using CrmPluginRegExt.VSPackage.Helpers;
using Microsoft.Xrm.Sdk.Metadata;

#endregion

namespace CrmPluginRegExt.VSPackage.Model
{
	[Serializable]
	public class MappingRelationshipN1
	{
		public Guid? MetadataId { get; set; }
		public CrmRelationshipAttribute Attribute { get; set; }

		public string DisplayName { get; set; }
		public string SchemaName { get; set; }
		public string LogicalName { get; set; }
		public string HybridName { get; set; }
		public string ForeignKey { get; set; }
		public string PrivateName { get; set; }
		public string EntityRole { get; set; }
		public string Type { get; set; }

		public MappingEntity FromEntity { get; set; }
		public MappingEntity ToEntity { get; set; }
		public MappingField FromField { get; set; }
		public MappingField Property { get; set; }

		public static void UpdateCache(List<OneToManyRelationshipMetadata> relMetadataList, MappingEntity mappingEntity
			, MappingField[] properties)
		{
			// update modified fields
			var modifiedRelations = mappingEntity.RelationshipsManyToOne
				.Where(relation => relMetadataList.Exists(relMeta => relMeta.MetadataId == relation.MetadataId)).ToList();
			modifiedRelations.ForEach(
				rel => Update(relMetadataList.First(relMeta => relMeta.MetadataId == rel.MetadataId), rel, properties));

			// add new attributes
			var newRelMeta = relMetadataList
				.Where(
					relMeta =>
					!Array.Exists(mappingEntity.RelationshipsManyToOne, relation => relation.MetadataId == relMeta.MetadataId))
				.ToList();
			newRelMeta.ForEach(
				relMeta =>
				{
					var newRelations = new MappingRelationshipN1[mappingEntity.RelationshipsManyToOne.Length + 1];
					Array.Copy(mappingEntity.RelationshipsManyToOne, newRelations, mappingEntity.RelationshipsManyToOne.Length);
					mappingEntity.RelationshipsManyToOne = newRelations;
					mappingEntity.RelationshipsManyToOne[mappingEntity.RelationshipsManyToOne.Length - 1] = Parse(relMeta, properties);
				});
		}

		public static MappingRelationshipN1 Parse(OneToManyRelationshipMetadata rel, MappingField[] properties)
		{
			var property = properties.First(p => p.Attribute.LogicalName.ToLower() == rel.ReferencingAttribute.ToLower());

			var propertyName = property.DisplayName;

			var result = new MappingRelationshipN1
			             {
				             Attribute = new CrmRelationshipAttribute
				                         {
					                         ToEntity = rel.ReferencedEntity,
					                         ToKey = rel.ReferencedAttribute,
					                         FromEntity = rel.ReferencingEntity,
					                         FromKey = rel.ReferencingAttribute,
					                         IntersectingEntity = ""
				                         },
				             DisplayName = Naming.GetProperVariableName(rel.SchemaName),
				             SchemaName = Naming.GetProperVariableName(rel.SchemaName),
				             LogicalName = rel.ReferencingAttribute,
				             HybridName = Naming.GetProperVariableName(rel.SchemaName) + "_N1",
				             PrivateName = "_n1" + Naming.GetEntityPropertyPrivateName(rel.SchemaName),
				             ForeignKey = propertyName,
				             Type = Naming.GetProperVariableName(rel.ReferencedEntity),
				             Property = property,
				             EntityRole = "null",
				             MetadataId = rel.MetadataId
			             };

			if (rel.ReferencedEntity == rel.ReferencingEntity)
			{
				result.EntityRole = "Microsoft.Xrm.Sdk.EntityRole.Referencing";
				result.DisplayName = "Referencing" + result.DisplayName;
			}

			return result;
		}

		public static void Update(OneToManyRelationshipMetadata rel, MappingRelationshipN1 relationshipOneToMany,
			MappingField[] properties)
		{
			var propertyName = relationshipOneToMany.ForeignKey;
			if (rel.ReferencingAttribute != null)
			{
				var property = properties.First(p => p.Attribute.LogicalName.ToLower() == rel.ReferencingAttribute.ToLower());
				propertyName = property.DisplayName;
				relationshipOneToMany.FromField = property;
			}

			relationshipOneToMany.Attribute.ToEntity = rel.ReferencedEntity ?? relationshipOneToMany.Attribute.FromEntity;
			relationshipOneToMany.Attribute.ToKey = rel.ReferencedAttribute ?? relationshipOneToMany.Attribute.FromKey;
			relationshipOneToMany.Attribute.FromEntity = rel.ReferencingEntity ?? relationshipOneToMany.Attribute.ToEntity;
			relationshipOneToMany.Attribute.FromKey = rel.ReferencingAttribute ?? relationshipOneToMany.Attribute.ToKey;
			relationshipOneToMany.Attribute.IntersectingEntity = "";
			relationshipOneToMany.ForeignKey = propertyName;

			if (rel.SchemaName != null)
			{
				relationshipOneToMany.DisplayName = Naming.GetProperVariableName(rel.SchemaName);
				relationshipOneToMany.SchemaName = Naming.GetProperVariableName(rel.SchemaName);
				relationshipOneToMany.PrivateName = "_n1" + Naming.GetEntityPropertyPrivateName(rel.SchemaName);
				relationshipOneToMany.HybridName = Naming.GetProperVariableName(rel.SchemaName) + "_N1";
			}

			relationshipOneToMany.LogicalName = rel.ReferencingAttribute ?? relationshipOneToMany.LogicalName;

			relationshipOneToMany.EntityRole = "null";
			relationshipOneToMany.Type = Naming.GetProperVariableName(rel.ReferencedEntity);

			relationshipOneToMany.MetadataId = rel.MetadataId;


			if (rel.ReferencedEntity != null && rel.ReferencingEntity != null
			    && rel.ReferencedEntity == rel.ReferencingEntity)
			{
				relationshipOneToMany.DisplayName = "Referencing" + relationshipOneToMany.DisplayName;
				relationshipOneToMany.EntityRole = "Microsoft.Xrm.Sdk.EntityRole.Referencing";
			}
		}
	}
}
