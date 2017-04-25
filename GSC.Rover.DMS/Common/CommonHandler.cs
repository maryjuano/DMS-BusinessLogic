using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Messages;

namespace GSC.Rover.DMS.BusinessLogic.Common
{
    public class CommonHandler
    {
        /// <summary>
        /// Common method to get Message Description from System Message entity
        /// </summary>
        /// <param name="name"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static string GetSystemMessageValue(string name, IOrganizationService service)
        {
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                  <entity name='hp_systemmessages'>                                                          
                                   <attribute name='hp_value' />
                                    <filter type='and'>
                                      <condition attribute='hp_name' operator='eq' value='{0}' /> 
                                   </filter>
                                 </entity>
                              </fetch>";
            fetchXml = String.Format(fetchXml, name);
            EntityCollection systemMessageEntity = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (systemMessageEntity != null && systemMessageEntity.Entities.Count > 0 && systemMessageEntity.Entities[0].Contains("hp_value") && systemMessageEntity.Entities[0]["hp_value"] != null)
            {
                return systemMessageEntity.Entities[0]["hp_value"].ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Generic exception handler to re-throw exceptions to CRM as InvalidPluginExecutionException with trace handling
        /// </summary>
        /// <param name="exception">Exception that was thrown</param>
        /// <param name="trace">The trace service</param>
        /// <param name="pluginName">The plugin name in which the exception has occurred</param>
        /// <param name="service"></param>
        /// <param name="userId"></param>
        /// <param name="applicationId"></param>
        public static void ThrowException(Exception exception, ITracingService trace, string pluginName,
               IOrganizationService service = null, object userId = null, object applicationId = null)
        {
            exception = exception.InnerException ?? exception;

            trace.Trace("Error message: {0}", exception.Message);
            trace.Trace("Error StackTrace: {0}", exception.StackTrace);
            trace.Trace("Error Source: {0}", exception.Source);
            trace.Trace("Error TargetSite: {0}", exception.TargetSite);

            if (exception is System.Data.SqlClient.SqlException)
                throw new InvalidPluginExecutionException("An error has occurred while connecting to the database. Please try again after sometime.");
            if (exception is InvalidPluginExecutionException)
                throw exception;
            throw new InvalidPluginExecutionException(string.Format("An error has occurred in the {0} plugin. Please download the error trace and contact the system administrator. Message: {1}", pluginName, exception.Message), exception);
        }

        /// <summary>
        /// Common method to get the data from Context or Image
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contextRecord"></param>
        /// <param name="fieldName"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static T GetValueFromContextOrImage<T>(Entity contextRecord, string fieldName, Entity image = null)
        {
            if (contextRecord != null && contextRecord.Contains(fieldName))
            {
                return contextRecord.GetAttributeValue<T>(fieldName);
            }
            if (image != null && image.Contains(fieldName))
            {
                return image.GetAttributeValue<T>(fieldName);
            }

            return default(T);
        }

        /// <summary>
        /// Common method to get text from Optionset Attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contextRecord"></param>
        /// <param name="fieldName"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string GetTextFromOptionSetAttribute(Entity contextRecord, string fieldName, Entity image = null)
        {
            if (contextRecord != null && contextRecord.Contains(fieldName))
            {
                return contextRecord.FormattedValues[fieldName];
            }
            if (image != null && image.Contains(fieldName))
            {
                return image.FormattedValues[fieldName];
            }

            return string.Empty;
        }
        
        /// <summary>
        /// Generic retrieve option set text values
        /// </summary>
        public static string GetOptionsSetTextOnValue(IOrganizationService service, string entityName, string attributeName, int selectedValue)
        {
            var retrieveAttributeRequest = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };
            // Execute the request.
            var retrieveAttributeResponse = (RetrieveAttributeResponse)service.Execute(retrieveAttributeRequest);
            // Access the retrieved attribute.

            var retrievedPicklistAttributeMetadata = (PicklistAttributeMetadata)

            retrieveAttributeResponse.AttributeMetadata;// Get the current options list for the retrieved attribute.
            OptionMetadata[] optionList = retrievedPicklistAttributeMetadata.OptionSet.Options.ToArray();
            string selectedOptionLabel = string.Empty;
            foreach (OptionMetadata optionMetadata in optionList)
            {
                if (optionMetadata.Value == selectedValue)
                {
                    selectedOptionLabel = optionMetadata.Label.UserLocalizedLabel.Label;

                }
            }
            return selectedOptionLabel;
        }

        /// <summary>
        /// Set the field value to an entity record or image
        /// </summary>
        /// <param name="contextRecord">The Entity</param>
        /// <param name="fieldName">The string</param>
        /// <param name="value">The object</param>
        public static void SetValueToContextOrImage(Entity contextRecord, string fieldName, object value)
        {
            if (contextRecord != null)
            {
                if (contextRecord.Contains(fieldName))
                    contextRecord.Attributes[fieldName] = value;
                else
                    contextRecord.Attributes.Add(fieldName, value);
            }
            else
            {
                throw new Exception("Record is null");
            }
        }

        /// <summary>
        /// Generic retrieve function to retrieve records by validating a field equality to a certain value
        /// </summary>
        public static EntityCollection RetrieveRecordsByOneValue(string entity, string field, object value, IOrganizationService service, string orderField = null, OrderType orderType = OrderType.Ascending, params string[] columns)
        {
            var query = new QueryExpression(entity) { ColumnSet = new ColumnSet() };
            if (columns.Length > 0)
            {
                query.ColumnSet.AddColumns(columns);
            }

            var filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition(new ConditionExpression(field, ConditionOperator.Equal, value));
            query.Criteria.Filters.Add(filter);

            if (!string.IsNullOrEmpty(orderField))
            {
                query.Orders.Add(new OrderExpression(orderField, orderType));
            }

            return service.RetrieveMultiple(query);
        }

        /// <summary>
        /// Generic method to Retrieve Organization Base Currency Id
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static Guid GetBaseCurrency(IOrganizationService service)
        {
            Guid baseCurrencyId = Guid.Empty;
            var query = new QueryExpression("organization") { ColumnSet = new ColumnSet("basecurrencyid") };
            EntityCollection baseCurrency = service.RetrieveMultiple(query);

            if (baseCurrency != null && baseCurrency.Entities != null && baseCurrency.Entities.Count > 0)
            {
                Entity baseCurrencyFirstRecord = baseCurrency.Entities.First();
                return ((EntityReference)(baseCurrencyFirstRecord.Attributes["basecurrencyid"])).Id;

            }
            return baseCurrencyId;
        }

        /// <summary>
        /// Generic retrieve function to retrieve records by validating certain conditions passed to the method
        /// </summary>
        public static EntityCollection RetrieveRecordsByConditions(string entity, IList<ConditionExpression> conditions, IOrganizationService service, string orderField = null, OrderType orderType = OrderType.Ascending, params string[] columns)
        {
            var query = new QueryExpression(entity) { ColumnSet = new ColumnSet() };
            if (columns.Length > 0)
            {
                query.ColumnSet.AddColumns(columns);
            }

            var filter = new FilterExpression(LogicalOperator.And);
            foreach (ConditionExpression condition in conditions)
            {
                filter.AddCondition(condition);
            }
            query.Criteria.Filters.Add(filter);

            if (!string.IsNullOrEmpty(orderField))
            {
                query.Orders.Add(new OrderExpression(orderField, orderType));
            }

            return service.RetrieveMultiple(query);
        }

        /// <summary>
        /// Getting an Entity Reference's Id safely, returns Empty Guid if Entity Reference is null
        /// </summary>
        /// <param name="name"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static Guid GetEntityReferenceIdSafe(Entity entity, string attribute)
        {      
            return entity.GetAttributeValue<EntityReference>(attribute) != null ? entity.GetAttributeValue<EntityReference>(attribute).Id : Guid.Empty;
        }

        /// <summary>
        /// Getting an attribute value of an entity safely, returns default value if attribute is null
        /// </summary>
        /// <param name="name"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static T GetEntityAttributeSafe<T>(Entity entity, string attribute)
        {
            T result = entity.GetAttributeValue<T>(attribute);

            if (result == null)
            {               
                result = default(T);
            }
          
            return result;
        }
    }
}
