#region Imports

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CrmPluginEntities;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.CrmPluginRegistration.Connection;

#endregion

namespace Yagasoft.CrmPluginRegistration.Helpers
{
	public sealed class ComboMessage
	{
		public Guid MessageId { get; set; }
		public Guid FilteredId { get; set; }
		public string MessageName { get; set; }
		public string EntityName { get; set; }
	}

	public sealed class ComboUser
	{
		public Guid Id { get; set; }
		public string Name { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}

	public static class CrmDataHelpers
	{
		public static List<ComboMessage> MessageList = new List<ComboMessage>();
		public static List<ComboUser> UserList = new List<ComboUser>();
		public static readonly IDictionary<string, List<string>> AttributeList = new ConcurrentDictionary<string, List<string>>();

		public static List<string> GetEntityNames(IConnectionManager connectionManager, bool cached = true)
		{
			if (!MessageList.Any() || !cached)
			{
				RefreshMessageCache(connectionManager);
			}

			return MessageList.Select(message => message.EntityName).Distinct()
				.OrderBy(name => name).ToList();
		}

		public static List<string> GetMessageNames(string entityName, IConnectionManager connectionManager,
			bool cached = true)
		{
			if (!MessageList.Any() || !cached)
			{
				RefreshMessageCache(connectionManager);
			}

			return MessageList.Where(message => entityName == "none" || message.EntityName == entityName)
				.Select(message => message.MessageName).Distinct()
				.OrderBy(name => name).ToList();
		}

		public static List<ComboUser> GetUsers(IConnectionManager connectionManager, bool cached = true)
		{
			if (UserList.Any() && cached)
			{
				return UserList;
			}

			lock (UserList)
			{
				UserList = new List<ComboUser>
						   {
							   new ComboUser
							   {
								   Id = Guid.Empty,
								   Name = "Calling User"
							   }
						   };

					using (var context = new XrmServiceContext(connectionManager.Get()) { MergeOption = MergeOption.NoTracking })
				{
					UserList.AddRange(
						(from user in context.SystemUserSet
						 where user.IsDisabled == false
						 select new ComboUser
								{
									Id = user.Id,
									Name = user.FirstName + " " + user.LastName
								}).ToList().OrderBy(user => user.Name));
				}
			}

			return UserList;
		}

		public static List<string> GetEntityFieldNames(string entityName, IConnectionManager connectionManager, bool cached = true)
		{
			if (entityName == "none")
			{
				return new List<string>();
			}

			if (AttributeList.ContainsKey(entityName) && cached)
			{
				return AttributeList[entityName];
			}

			lock (AttributeList)
			{
				var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
				entityFilter.Conditions.Add(
					new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityName));

				var entityProperties = new MetadataPropertiesExpression
									   {
										   AllProperties = false
									   };
				entityProperties.PropertyNames.AddRange("Attributes");

				var attributesFilter = new MetadataFilterExpression(LogicalOperator.And);
				attributesFilter.Conditions.Add(
					new MetadataConditionExpression("AttributeOf", MetadataConditionOperator.Equals, null));

				var attributeProperties = new MetadataPropertiesExpression
										  {
											  AllProperties = false
										  };
				attributeProperties.PropertyNames.AddRange("LogicalName");

				var entityQueryExpression = new EntityQueryExpression
											{
												Criteria = entityFilter,
												Properties = entityProperties,
												AttributeQuery = new AttributeQueryExpression
																 {
																	 Criteria = attributesFilter,
																	 Properties = attributeProperties
																 }
											};

				var retrieveMetadataChangesRequest = new RetrieveMetadataChangesRequest
													 {
														 Query = entityQueryExpression,
														 ClientVersionStamp = null
													 };

				var attributeNames = ((RetrieveMetadataChangesResponse)connectionManager.Get().Execute(retrieveMetadataChangesRequest))
					.EntityMetadata.First().Attributes
					.Select(attribute => attribute.LogicalName).OrderBy(name => name).ToList();

				AttributeList[entityName] = attributeNames;
			}

			return AttributeList[entityName];
		}

		private static void RefreshMessageCache(IConnectionManager connectionManager)
		{
			using (var context = new XrmServiceContext(connectionManager.Get()) { MergeOption = MergeOption.NoTracking })
			{
				MessageList =
					(from message in context.SdkMessageSet
					 join filter in context.SdkMessageFilterSet
						 on message.SdkMessageId equals filter.SdkMessageId.Id
					 // where filter.IsCustomProcessingStepAllowed == true
					 select new ComboMessage
							{
								MessageId = message.SdkMessageId.GetValueOrDefault(),
								FilteredId = filter.SdkMessageFilterId.GetValueOrDefault(),
								MessageName = message.Name,
								EntityName = filter.PrimaryObjectTypeCode
							}).ToList();
			}
		}

		public static void ClearCache()
		{
			MessageList.Clear();
			UserList.Clear();
			AttributeList.Clear();
		}

		public static string GetMessagePropertyName(string message, string entity = null)
		{
			switch (message)
			{
				case "Assign":
				case "Delete":
				case "ExecuteWorkflow":
				case "Merge":
				case "Route":
				case "Update":
					return "Target";

				case "Create":
					return "Id";

				case "DeliverIncoming":
				case "DeliverPromote":
					return "EmailId";

				case "Send":
					if (string.IsNullOrEmpty(entity))
					{
						throw new ArgumentNullException("entity", "Entity can not be null!");
					}

					switch (entity)
					{
						case "template":
							return "TemplateId";
						case "fax":
							return "FaxId";
						default:
							return "EmailId";
					}

				case "SetState":
				case "SetStateDynamicEntity":
					return "EntityMoniker";

				default:
					//There are no valid message property names for images for any other messages
					return "";
			}
		}
	}
}
