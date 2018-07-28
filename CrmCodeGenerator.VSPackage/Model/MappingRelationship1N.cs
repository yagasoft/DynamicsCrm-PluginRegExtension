#region File header

// Project / File: CrmPluginRegExt.VSPackage / MappingRelationship1N.cs
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
	public class MappingRelationship1N
	{
		public Guid? MetadataId { get; set; }
		public CrmRelationshipAttribute Attribute { get; set; }
		public string DisplayName { get; set; }
		public string ForeignKey { get; set; }
		public string LogicalName { get; set; }
		public string SchemaName { get; set; }
		public string HybridName { get; set; }
		public string PrivateName { get; set; }
		public string EntityRole { get; set; }
		public string Type { get; set; }

		public MappingEntity FromEntity { get; set; }
		public MappingEntity ToEntity { get; set; }
		public MappingField ToField { get; set; }

		public static void UpdateCache(List<OneToManyRelationshipMetadata> relMetadataList, MappingEntity mappingEntity
			, MappingField[] properties)
		{
			// update modified fields
			var modifiedRelations = mappingEntity.RelationshipsOneToMany
				.Where(relation => relMetadataList.Exists(relMeta => relMeta.MetadataId == relation.MetadataId)).ToList();
			modifiedRelations.ForEach(
				rel => Update(relMetadataList.First(relMeta => relMeta.MetadataId == rel.MetadataId), rel, properties));

			// add new attributes
			var newRelMeta = relMetadataList
				.Where(
					relMeta =>
					!Array.Exists(mappingEntity.RelationshipsOneToMany, relation => relation.MetadataId == relMeta.MetadataId))
				.ToList();
			newRelMeta.ForEach(
				relMeta =>
				{
					var newRelations = new MappingRelationship1N[mappingEntity.RelationshipsOneToMany.Length + 1];
					Array.Copy(mappingEntity.RelationshipsOneToMany, newRelations, mappingEntity.RelationshipsOneToMany.Length);
					mappingEntity.RelationshipsOneToMany = newRelations;
					mappingEntity.RelationshipsOneToMany[mappingEntity.RelationshipsOneToMany.Length - 1] = Parse(relMeta, properties);
				});
		}

		public static MappingRelationship1N Parse(OneToManyRelationshipMetadata rel, MappingField[] properties)
		{
			var property =
				properties.First(p => p.Attribute.LogicalName.ToLower() == rel.ReferencedAttribute.ToLower());

			var propertyName = property.DisplayName;

			var result = new MappingRelationship1N
			             {
				             Attribute = new CrmRelationshipAttribute
				                         {
					                         FromEntity = rel.ReferencedEntity,
					                         FromKey = rel.ReferencedAttribute,
					                         ToEntity = rel.ReferencingEntity,
					                         ToKey = rel.ReferencingAttribute,
					                         IntersectingEntity = ""
				                         },
				             ForeignKey = propertyName,
				             DisplayName = Naming.GetProperVariableName(rel.SchemaName),
				             SchemaName = Naming.GetProperVariableName(rel.SchemaName),
				             LogicalName = rel.ReferencingAttribute,
				             PrivateName = Naming.GetEntityPropertyPrivateName(rel.SchemaName),
				             HybridName = Naming.GetPluralName(Naming.GetProperVariableName(rel.SchemaName)),
				             EntityRole = "null",
				             Type = Naming.GetProperVariableName(rel.ReferencingEntity),
				             MetadataId = rel.MetadataId
			             };

			if (rel.ReferencedEntity == rel.ReferencingEntity)
			{
				result.DisplayName = "Referenced" + result.DisplayName;
				result.EntityRole = "Microsoft.Xrm.Sdk.EntityRole.Referenced";
			}

			return result;
		}

		private static void Update(OneToManyRelationshipMetadata rel, MappingRelationship1N relationshipOneToMany,
			MappingField[] properties)
		{
			var propertyName = relationshipOneToMany.ForeignKey;

			if (rel.ReferencedAttribute != null)
			{
				var property = properties.First(p => p.Attribute.LogicalName.ToLower() == rel.ReferencedAttribute.ToLower());
				propertyName = property.DisplayName;
			}

			relationshipOneToMany.Attribute.FromEntity = rel.ReferencedEntity ?? relationshipOneToMany.Attribute.FromEntity;
			relationshipOneToMany.Attribute.FromKey = rel.ReferencedAttribute ?? relationshipOneToMany.Attribute.FromKey;
			relationshipOneToMany.Attribute.ToEntity = rel.ReferencingEntity ?? relationshipOneToMany.Attribute.ToEntity;
			relationshipOneToMany.Attribute.ToKey = rel.ReferencingAttribute ?? relationshipOneToMany.Attribute.ToKey;
			relationshipOneToMany.Attribute.IntersectingEntity = "";
			relationshipOneToMany.ForeignKey = propertyName;

			if (rel.SchemaName != null)
			{
				relationshipOneToMany.DisplayName = Naming.GetProperVariableName(rel.SchemaName);
				relationshipOneToMany.SchemaName = Naming.GetProperVariableName(rel.SchemaName);
				relationshipOneToMany.PrivateName = Naming.GetEntityPropertyPrivateName(rel.SchemaName);
				relationshipOneToMany.HybridName = Naming.GetPluralName(Naming.GetProperVariableName(rel.SchemaName));
			}

			relationshipOneToMany.LogicalName = rel.ReferencingAttribute ?? relationshipOneToMany.LogicalName;

			relationshipOneToMany.EntityRole = "null";
			relationshipOneToMany.Type = Naming.GetProperVariableName(rel.ReferencingEntity);

			relationshipOneToMany.MetadataId = rel.MetadataId;


			if (rel.ReferencedEntity != null && rel.ReferencingEntity != null
			    && rel.ReferencedEntity == rel.ReferencingEntity)
			{
				relationshipOneToMany.DisplayName = "Referenced" + relationshipOneToMany.DisplayName;
				relationshipOneToMany.EntityRole = "Microsoft.Xrm.Sdk.EntityRole.Referenced";
			}
		}
	}
}
