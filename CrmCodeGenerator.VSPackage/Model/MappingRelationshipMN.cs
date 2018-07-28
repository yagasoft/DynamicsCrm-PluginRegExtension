#region File header

// Project / File: CrmPluginRegExt.VSPackage / MappingRelationshipMN.cs
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
	public class MappingRelationshipMN : ICloneable
	{
		public Guid? MetadataId { get; set; }
		public CrmRelationshipAttribute Attribute { get; set; }

		public string DisplayName { get; set; }
		public string SchemaName { get; set; }
		public string HybridName { get; set; }
		public string ForeignKey { get; set; }
		public string PrivateName { get; set; }
		public string EntityRole { get; set; }
		public string Type { get; set; }
		public bool IsSelfReferenced { get; set; }

		public MappingEntity FromEntity { get; set; }
		public MappingEntity ToEntity { get; set; }
		public MappingEntity IntersectingEntity { get; set; }

		public static void UpdateCache(List<ManyToManyRelationshipMetadata> relMetadataList, MappingEntity mappingEntity
			, string thisEntityLogicalName)
		{
			// update modified fields
			var modifiedRelations = mappingEntity.RelationshipsManyToMany
				.Where(relation => relMetadataList.Exists(relMeta => relMeta.MetadataId == relation.MetadataId)).ToList();
			modifiedRelations.ForEach(
				rel => Update(relMetadataList.First(relMeta => relMeta.MetadataId == rel.MetadataId), rel, thisEntityLogicalName));

			// add new attributes
			var newRelMeta = relMetadataList
				.Where(
					relMeta =>
					!Array.Exists(mappingEntity.RelationshipsManyToMany, relation => relation.MetadataId == relMeta.MetadataId))
				.ToList();
			newRelMeta.ForEach(
				relMeta =>
				{
					var newRelations = new MappingRelationshipMN[mappingEntity.RelationshipsManyToMany.Length + 1];
					Array.Copy(mappingEntity.RelationshipsManyToMany, newRelations, mappingEntity.RelationshipsManyToMany.Length);
					mappingEntity.RelationshipsManyToMany = newRelations;
					mappingEntity.RelationshipsManyToMany[mappingEntity.RelationshipsManyToMany.Length - 1] = Parse(relMeta,
						thisEntityLogicalName);
				});
		}

		public static MappingRelationshipMN Parse(ManyToManyRelationshipMetadata rel, string thisEntityLogicalName)
		{
			var result = new MappingRelationshipMN();
			if (rel.Entity1LogicalName == thisEntityLogicalName)
			{
				result.Attribute = new CrmRelationshipAttribute
				                   {
					                   FromEntity = rel.Entity1LogicalName,
					                   FromKey = rel.Entity1IntersectAttribute,
					                   ToEntity = rel.Entity2LogicalName,
					                   ToKey = rel.Entity2IntersectAttribute,
					                   IntersectingEntity = rel.IntersectEntityName
				                   };
			}
			else
			{
				result.Attribute = new CrmRelationshipAttribute
				                   {
					                   ToEntity = rel.Entity1LogicalName,
					                   ToKey = rel.Entity1IntersectAttribute,
					                   FromEntity = rel.Entity2LogicalName,
					                   FromKey = rel.Entity2IntersectAttribute,
					                   IntersectingEntity = rel.IntersectEntityName
				                   };
			}

			result.EntityRole = "null";
			result.SchemaName = Naming.GetProperVariableName(rel.SchemaName);
			result.DisplayName = Naming.GetProperVariableName(rel.SchemaName);
			if (rel.Entity1LogicalName == rel.Entity2LogicalName && rel.Entity1LogicalName == thisEntityLogicalName)
			{
				result.DisplayName = "Referenced" + result.DisplayName;
				result.EntityRole = "Microsoft.Xrm.Sdk.EntityRole.Referenced";
				result.IsSelfReferenced = true;
			}
			if (result.DisplayName == thisEntityLogicalName)
			{
				result.DisplayName += "1"; // this is what CrmSvcUtil does
			}

			result.HybridName = Naming.GetProperVariableName(rel.SchemaName) + "_NN";
			result.PrivateName = "_nn" + Naming.GetEntityPropertyPrivateName(rel.SchemaName);
			result.ForeignKey = Naming.GetProperVariableName(result.Attribute.ToKey);
			result.Type = Naming.GetProperVariableName(result.Attribute.ToEntity);
			result.MetadataId = rel.MetadataId;

			return result;
		}

		public static void Update(ManyToManyRelationshipMetadata rel, MappingRelationshipMN relationshipManyToMany,
			string thisEntityLogicalName)
		{
			if (rel.Entity1LogicalName != null)
			{
				if (rel.Entity1LogicalName == thisEntityLogicalName)
				{
					relationshipManyToMany.Attribute.FromEntity = rel.Entity1LogicalName ?? relationshipManyToMany.Attribute.FromEntity;
					relationshipManyToMany.Attribute.FromKey = rel.Entity1IntersectAttribute ??
					                                           relationshipManyToMany.Attribute.FromKey;
					relationshipManyToMany.Attribute.ToEntity = rel.Entity2LogicalName ?? relationshipManyToMany.Attribute.ToEntity;
					relationshipManyToMany.Attribute.ToKey = rel.Entity2IntersectAttribute ?? relationshipManyToMany.Attribute.ToKey;
					relationshipManyToMany.Attribute.IntersectingEntity = rel.IntersectEntityName ??
					                                                      relationshipManyToMany.Attribute.IntersectingEntity;
				}
				else
				{
					relationshipManyToMany.Attribute.ToEntity = rel.Entity1LogicalName ?? relationshipManyToMany.Attribute.ToEntity;
					relationshipManyToMany.Attribute.ToKey = rel.Entity1IntersectAttribute ?? relationshipManyToMany.Attribute.ToKey;
					relationshipManyToMany.Attribute.FromEntity = rel.Entity2LogicalName ?? relationshipManyToMany.Attribute.FromEntity;
					relationshipManyToMany.Attribute.FromKey = rel.Entity2IntersectAttribute ??
					                                           relationshipManyToMany.Attribute.FromKey;
					relationshipManyToMany.Attribute.IntersectingEntity = rel.IntersectEntityName ??
					                                                      relationshipManyToMany.Attribute.IntersectingEntity;
				}
			}

			relationshipManyToMany.EntityRole = "null";

			if (rel.SchemaName != null)
			{
				relationshipManyToMany.SchemaName = Naming.GetProperVariableName(rel.SchemaName);
				relationshipManyToMany.DisplayName = Naming.GetProperVariableName(rel.SchemaName);
			}

			if (rel.Entity1LogicalName != null && rel.Entity2LogicalName != null)
			{
				if (rel.Entity1LogicalName == rel.Entity2LogicalName && rel.Entity1LogicalName == thisEntityLogicalName)
				{
					relationshipManyToMany.DisplayName = "Referenced" + relationshipManyToMany.DisplayName;
					relationshipManyToMany.EntityRole = "Microsoft.Xrm.Sdk.EntityRole.Referenced";
					relationshipManyToMany.IsSelfReferenced = true;
				}
			}

			if (relationshipManyToMany.DisplayName == thisEntityLogicalName)
			{
				relationshipManyToMany.DisplayName += "1"; // this is what CrmSvcUtil does
			}

			if (rel.SchemaName != null)
			{
				relationshipManyToMany.HybridName = Naming.GetProperVariableName(rel.SchemaName) + "_NN";
				relationshipManyToMany.PrivateName = "_nn" + Naming.GetEntityPropertyPrivateName(rel.SchemaName);
			}

			relationshipManyToMany.ForeignKey = Naming.GetProperVariableName(relationshipManyToMany.Attribute.ToKey);
			relationshipManyToMany.Type = Naming.GetProperVariableName(relationshipManyToMany.Attribute.ToEntity);

			relationshipManyToMany.MetadataId = rel.MetadataId;
		}

		public object Clone()
		{
			var newPerson = (MappingRelationshipMN) MemberwiseClone();
			newPerson.Attribute = (CrmRelationshipAttribute) Attribute.Clone();
			return newPerson;
		}
	}
}
