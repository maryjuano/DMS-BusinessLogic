using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk.Query;
using System.Xml;
using GSC.Rover.DMS.BusinessLogic.PriceListItem;

namespace GSC.Rover.DMS.BusinessLogic.PriceList
{
    public class PriceListHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public PriceListHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        public int itemType { get; set; }
        public string productFieldName { get; set; }

        //Created By: Leslie g. Baliguat, Created On: 01/06/17
        /*Purpose: Unchecks other price list tagged as default and updates vehicle sell price from
         * Event/Message:
         *      Post/Update: Default Price List = gsc_default
         *      Post/Create: Default Price List = gsc_default
         * Primary Entity: Sales Return Detail
         */
        public void ChangeDefaultPriceList(Entity priceList)
        {
            _tracingService.Trace("Started ChangeDefaultPriceList method..");

            if (!priceList.GetAttributeValue<Boolean>("gsc_default")) { return; }

            var transactionType = priceList.Contains("gsc_transactiontype")
                ? priceList.GetAttributeValue<OptionSetValue>("gsc_transactiontype").Value
                : 0;

            var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("pricelevelid", ConditionOperator.NotEqual, priceList.Id),
                        new ConditionExpression("gsc_transactiontype", ConditionOperator.Equal, transactionType),
                        new ConditionExpression("gsc_default", ConditionOperator.Equal, true)
                    };

            EntityCollection priceListCollection = CommonHandler.RetrieveRecordsByConditions("pricelevel", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_default" });


            if (priceListCollection != null && priceListCollection.Entities.Count > 0)
            {
                foreach (var defaultPriceList in priceListCollection.Entities)
                {
                    defaultPriceList["gsc_default"] = false;
                    _organizationService.Update(defaultPriceList);

                    _tracingService.Trace("Other default price list updated.");
                }
            }
            //productpricelevel

            if (transactionType == 100000000)
            {
                EntityCollection priceListItemCollection = CommonHandler.RetrieveRecordsByOneValue("productpricelevel", "pricelevelid", priceList.Id, _organizationService, null, OrderType.Ascending,
                   new[] { "amount", "productid", "pricelevelid" });

                if (priceListItemCollection != null && priceListItemCollection.Entities.Count > 0)
                {
                    foreach (var priceListItem in priceListItemCollection.Entities)
                    {
                        _tracingService.Trace("Price List Item Retrieved.");

                        PriceListItemHandler priceListItemHandler = new PriceListItemHandler(_organizationService, _tracingService);
                        priceListItemHandler.UpdateVehicleSellPrice(priceListItem);
                    }
                }
            }

            _tracingService.Trace("Ended ChangeDefaultPriceList method..");
        }

        //Created By : Jerome Anthony Gerero, Created On : 01/06/2017
        /*Purpose: Send e-mail notification to branches that the default price list has been changed
         * Event/Message:
         *      Post/Update: Default Price List = gsc_default
         *      Post/Create: Default Price List = gsc_default
         * Primary Entity: Sales Return Detail
         */
        public Entity SendDefaultPriceListNotification(Entity priceListEntity)
        {
            _tracingService.Trace("Started SendDefaultPriceListNotification method...");

            if (priceListEntity.GetAttributeValue<Boolean>("gsc_default") == false) { return null; }

            SendNotification(priceListEntity, "Default Price List Change Notification", "PriceList", 11);

           /* String priceListName = priceListEntity.Contains("name")
                ? priceListEntity.GetAttributeValue<String>("name")
                : String.Empty;

            Int32 branchType = 100000001;

            EntityCollection accountRecords = CommonHandler.RetrieveRecordsByOneValue("account", "gsc_recordtype", branchType, _organizationService, null, OrderType.Ascending,
                new[] { "emailaddress1", "name" });

            if (accountRecords != null && accountRecords.Entities.Count > 0)
            {
                foreach (Entity account in accountRecords.Entities)
                {
                    String branchName = account.Contains("name")
                        ? account.GetAttributeValue<String>("name")
                        : String.Empty;
                    String emailAddress = account.Contains("emailaddress1")
                        ? account.GetAttributeValue<String>("emailaddress1")
                        : String.Empty;
                    
                    if (!String.IsNullOrEmpty(emailAddress))
                    {
                        EmailSender(branchName, emailAddress, priceListName);
                    }
                    
                }
            }*/

            _tracingService.Trace("Ended SendDefaultPriceListNotification method...");
            return priceListEntity;
        }

        public Entity PublishPromoNotification(Entity priceListEntity)
        {
            _tracingService.Trace("Started PublishPromoNotification method...");
            var isPromo = priceListEntity.GetAttributeValue<Boolean>("gsc_promo");

            if (!priceListEntity.GetAttributeValue<Boolean>("gsc_promo")) { return null; }

            if (!priceListEntity.GetAttributeValue<Boolean>("gsc_publish")) { return null; }

            SendNotification(priceListEntity, "Publish Promo Notification", "Promo", 7);

            _tracingService.Trace("Ended SendDefaultPriceListNotification method...");
            return priceListEntity;
        }

        public Entity PublishPriceListNotification(Entity priceListEntity)
        {
            _tracingService.Trace("Started PublishPromoNotification method...");
            var isPromo = priceListEntity.GetAttributeValue<Boolean>("gsc_promo");

            if (priceListEntity.GetAttributeValue<Boolean>("gsc_promo")) { return null; }

            if (!priceListEntity.GetAttributeValue<Boolean>("gsc_publish")) { return null; }

            SendNotification(priceListEntity, "Publish Price List Notification", "PriceList", 11);

            _tracingService.Trace("Ended SendDefaultPriceListNotification method...");
            return priceListEntity;
        }

        private Entity SendNotification(Entity priceListEntity, String title, String entity, int characterCount)
        {
            _tracingService.Trace("Started SendDefaultPriceListNotification method...");

            String priceListName = priceListEntity.Contains("name")
                ? priceListEntity.GetAttributeValue<String>("name")
                : String.Empty;

            Int32 branchType = 100000001;

            EntityCollection accountRecords = CommonHandler.RetrieveRecordsByOneValue("account", "gsc_recordtype", branchType, _organizationService, null, OrderType.Ascending,
                new[] { "emailaddress1", "name" });

            if (accountRecords != null && accountRecords.Entities.Count > 0)
            {
                foreach (Entity account in accountRecords.Entities)
                {
                    String branchName = account.Contains("name")
                        ? account.GetAttributeValue<String>("name")
                        : String.Empty;
                    String emailAddress = account.Contains("emailaddress1")
                        ? account.GetAttributeValue<String>("emailaddress1")
                        : String.Empty;
                    
                    if (!String.IsNullOrEmpty(emailAddress))
                    {
                        EmailSender(branchName, emailAddress, priceListName, title, entity, characterCount);
                    }
                }
            }

            _tracingService.Trace("Ended SendDefaultPriceListNotification method...");
            return priceListEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 01/06/2017
        //Purpose: Method that will execute email request
        private void EmailSender(String branchName, String emailAddress, String priceListName, String title, String entity, int characterCount)
        {
            _tracingService.Trace("Started EmailSender method...");

            List<ConditionExpression> emailTemplateCondition = new List<ConditionExpression>
                {
                    new ConditionExpression("templatetypecode", ConditionOperator.Equal, 8),
                    new ConditionExpression("title", ConditionOperator.Equal, title)
                };

            EntityCollection templateRecords = CommonHandler.RetrieveRecordsByConditions("template", emailTemplateCondition, _organizationService, null, OrderType.Ascending,
                new[] { "subjectpresentationxml", "presentationxml" });

            _tracingService.Trace("Template records retrieved : " + String.Concat(templateRecords.Entities.Count));

            if (templateRecords != null && templateRecords.Entities.Count > 0)
            {
                Entity template = templateRecords.Entities[0];

                XmlDocument subjectXml = new XmlDocument();
                subjectXml.LoadXml(template.GetAttributeValue<String>("subjectpresentationxml"));

                XmlDocument bodyXml = new XmlDocument();
                bodyXml.LoadXml(template.GetAttributeValue<String>("presentationxml"));

                EntityCollection systemUserRecords = CommonHandler.RetrieveRecordsByOneValue("systemuser", "fullname", "System Administrator", _organizationService, null, OrderType.Ascending,
                    new[] { "fullname" });

                _tracingService.Trace("System User records retrieved : " + String.Concat(systemUserRecords.Entities.Count));

                if (systemUserRecords != null && systemUserRecords.Entities.Count > 0)
                {
                    Entity systemUser = systemUserRecords.Entities[0];
                    Entity fromParty = new Entity("activityparty");
                    Entity toParty = new Entity("activityparty");

                    fromParty["partyid"] = new EntityReference("systemuser", systemUser.Id);
                    toParty["addressused"] = emailAddress;

                    Entity email = new Entity("email");
                    email["from"] = new Entity[] { fromParty };
                    email["to"] = new Entity[] { toParty };
                    email["subject"] = subjectXml.DocumentElement.InnerText;

                    #region Change dynamic string values
                    Int32 branchIndex = bodyXml.DocumentElement.InnerText.IndexOf("{Branch}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Insert(branchIndex, branchName);

                    Int32 priceListIndex = bodyXml.DocumentElement.InnerText.IndexOf("{" + entity + "}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Insert(priceListIndex, priceListName);

                    branchIndex = bodyXml.DocumentElement.InnerText.IndexOf("{Branch}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Remove(branchIndex, 8);

                    priceListIndex = bodyXml.DocumentElement.InnerText.IndexOf("{" + entity + "}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Remove(priceListIndex, characterCount);
                    #endregion

                    email["description"] = bodyXml.DocumentElement.InnerText;
                    email["directioncode"] = true;

                    
                    Guid emailId = _organizationService.Create(email);

                    _organizationService.Execute(new SendEmailRequest
                    { 
                        EmailId = emailId,
                        TrackingToken = "",
                        IssueSend = true
                    });
                }
            }

            _tracingService.Trace("Ended EmailSender method...");
        }

        //Created By: Leslie Baliguat
        public void CheckIfThereIsExistingDefaultPriceList(Entity priceList)
        {
            if (!priceList.GetAttributeValue<Boolean>("gsc_default")) { return; }
            var transactionType = priceList.Contains("gsc_transactiontype") ? priceList.GetAttributeValue<OptionSetValue>("gsc_transactiontype").Value : 0;

            var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_default", ConditionOperator.Equal, true),
                        new ConditionExpression("gsc_transactiontype", ConditionOperator.Equal,transactionType)
                    };

            EntityCollection priceListCollection = CommonHandler.RetrieveRecordsByConditions("pricelevel", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_default" });

            if (priceListCollection != null && priceListCollection.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("Cannot create default price list because there is already an existing default price list.");
            }
        }

        public List<Entity> RetrievePriceList(Entity entity, Int32 transactionType, Int32 priceListType)
        {
            _tracingService.Trace("Started RetrievePriceList Method.");
            _tracingService.Trace("Item Type: " + itemType);

            List<Entity> effectivePriceList = new List<Entity>();

            QueryExpression retrievePriceList = new QueryExpression("pricelevel");
            retrievePriceList.ColumnSet.AddColumns("gsc_transactiontype", "begindate", "enddate", "gsc_taxstatus");
            retrievePriceList.AddOrder("createdon", OrderType.Descending);
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_promo", ConditionOperator.Equal, false));
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_transactiontype", ConditionOperator.Equal, transactionType));
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));

            if (transactionType == 100000000)
            {
                if (priceListType == 100000003)
                    retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_default", ConditionOperator.Equal, true));
                else if (priceListType == 100000002)
                    retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_publish", ConditionOperator.Equal, true));

                retrievePriceList.LinkEntities.Add(new LinkEntity("pricelevel", "gsc_cmn_classmaintenance", "gsc_pricelisttype", "gsc_cmn_classmaintenanceid", JoinOperator.Inner));
                retrievePriceList.LinkEntities[0].LinkCriteria.AddCondition(new ConditionExpression("gsc_type", ConditionOperator.Equal, priceListType));

                if (itemType == 1)
                    retrievePriceList.LinkEntities[0].LinkCriteria.AddCondition(new ConditionExpression("gsc_classmaintenancepn", ConditionOperator.Like, "%Accessory"));
                else if (itemType == 2)
                    retrievePriceList.LinkEntities[0].LinkCriteria.AddCondition(new ConditionExpression("gsc_classmaintenancepn", ConditionOperator.Like, "%Chassis"));
            }
            else
                retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_default", ConditionOperator.Equal, true));

            EntityCollection defaultPriceListCollection = _organizationService.RetrieveMultiple(retrievePriceList);

            if (defaultPriceListCollection != null && defaultPriceListCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Default Price List.");

                Entity defaultPriceList = defaultPriceListCollection.Entities[0];

                effectivePriceList = GetPriceListitem(entity, defaultPriceList, effectivePriceList);

                if (effectivePriceList.Count != 0)
                {
                    if (CheckifDefaultPriceListIsLatest(defaultPriceList))
                        effectivePriceList.Add(defaultPriceList);
                    else
                        throw new InvalidPluginExecutionException("Default Price List effectivity date is out of date.");
                }

                else
                    RetrieveLatestPriceList(entity, transactionType, effectivePriceList, priceListType);
            }

            else
                RetrieveLatestPriceList(entity, transactionType, effectivePriceList, priceListType);

            _tracingService.Trace("Ended RetrievePriceList Method.");

            return effectivePriceList;
        }

        private List<Entity> GetPriceListitem(Entity entity, Entity priceList, List<Entity> effectivePriceList)
        {
            _tracingService.Trace("Get Price List Item Method Started.");

            var productId = CommonHandler.GetEntityReferenceIdSafe(entity, productFieldName);

            var priceLevelItemConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("pricelevelid", ConditionOperator.Equal, priceList.Id),
                    new ConditionExpression("productid", ConditionOperator.Equal, productId)
                };

            EntityCollection priceLevelItemCollection = CommonHandler.RetrieveRecordsByConditions("productpricelevel", priceLevelItemConditionList, _organizationService, null, OrderType.Ascending,
                        new[] { "amount" });

            if (priceLevelItemCollection != null && priceLevelItemCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Price List Item.");
                effectivePriceList.Add(priceLevelItemCollection.Entities[0]);
            }

            _tracingService.Trace("Get Price List Item Method Ended.");

            return effectivePriceList;
        }

        private List<Entity> RetrieveLatestPriceList(Entity entity, Int32 transactionType, List<Entity> effectivePriceList, Int32 priceListType)
        {
            _tracingService.Trace("Retrieve Latest Price List.");

            var dateToday = DateTime.Now.ToShortDateString();
            var branchId = entity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? entity.GetAttributeValue<EntityReference>("gsc_branchid").Id
                : Guid.Empty;

            _tracingService.Trace("Branch - " + branchId.ToString());

            QueryExpression retrievePriceList = new QueryExpression("pricelevel");
            retrievePriceList.ColumnSet.AddColumns("name", "gsc_transactiontype", "begindate", "enddate", "gsc_taxstatus");
            retrievePriceList.AddOrder("createdon", OrderType.Descending);
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_promo", ConditionOperator.Equal, false));
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_transactiontype", ConditionOperator.Equal, transactionType));
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_branchid", ConditionOperator.Equal, branchId));
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("begindate", ConditionOperator.LessEqual, dateToday));
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("enddate", ConditionOperator.GreaterEqual, dateToday));
            retrievePriceList.Criteria.Conditions.Add(new ConditionExpression("gsc_default", ConditionOperator.LessEqual, false));

            if (transactionType == 100000000)
            {
                retrievePriceList.LinkEntities.Add(new LinkEntity("pricelevel", "gsc_cmn_classmaintenance", "gsc_pricelisttype", "gsc_cmn_classmaintenanceid", JoinOperator.Inner));
                retrievePriceList.LinkEntities[0].LinkCriteria.AddCondition(new ConditionExpression("gsc_type", ConditionOperator.Equal, priceListType));

                if (itemType == 1)
                    retrievePriceList.LinkEntities[0].LinkCriteria.AddCondition(new ConditionExpression("gsc_classmaintenancepn", ConditionOperator.Like, "%Accessory"));
                else if (itemType == 2)
                    retrievePriceList.LinkEntities[0].LinkCriteria.AddCondition(new ConditionExpression("gsc_classmaintenancepn" , ConditionOperator.Like, "%Chassis"));
            }
            
            EntityCollection priceListCollection = _organizationService.RetrieveMultiple(retrievePriceList);

            //Get Price List where begin date is less than or equal to current date and end date is greater than than or equal to current date
            /*var priceListConditionList = new List<ConditionExpression>
                 {
                     new ConditionExpression("gsc_promo", ConditionOperator.Equal, false),
                     new ConditionExpression("gsc_transactiontype", ConditionOperator.Equal, transactionType),
                     new ConditionExpression("begindate", ConditionOperator.LessEqual, dateToday),
                     new ConditionExpression("enddate", ConditionOperator.GreaterEqual, dateToday),
                     new ConditionExpression("gsc_default", ConditionOperator.LessEqual, false),
                     new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                     new ConditionExpression("gsc_branchid", ConditionOperator.Equal, branchId)
                 };

            EntityCollection priceListCollection = CommonHandler.RetrieveRecordsByConditions("pricelevel", priceListConditionList, _organizationService, "createdon", OrderType.Ascending,
                        new[] { "gsc_transactiontype", "begindate", "enddate", "gsc_taxstatus" });*/

            _tracingService.Trace(priceListCollection.Entities.Count + " price list records retrieved...");

            if (priceListCollection != null && priceListCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Price List.");

                Entity priceList = priceListCollection.Entities[0];

                _tracingService.Trace("Price List =" + priceList.GetAttributeValue<String>("name"));

                effectivePriceList = GetPriceListitem(entity, priceList, effectivePriceList);

                if (effectivePriceList.Count != 0)
                    effectivePriceList.Add(priceList);
            }
            else
                throw new InvalidPluginExecutionException("There is no effecive Price List for the selected Vehicle.");

            _tracingService.Trace("Retrieve Latest Price List Ended.");

            return effectivePriceList;
        }

        public Boolean CheckifDefaultPriceListIsLatest(Entity priceList)
        {
            _tracingService.Trace("Started CheckifDefaultPriceListIsLatest Method.");

            var dateToday = Convert.ToDateTime(DateTime.Now.ToShortDateString());
            var beginDate =  Convert.ToDateTime(priceList.GetAttributeValue<DateTime>("begindate").ToShortDateString());
            var endDate =  Convert.ToDateTime(priceList.GetAttributeValue<DateTime>("enddate").ToShortDateString());

            if (beginDate <= dateToday && endDate >= dateToday)
            {
                _tracingService.Trace("true");
                _tracingService.Trace("Started CheckifDefaultPriceListIsLatest Method.");

                return true;
            }

            _tracingService.Trace("false");
            _tracingService.Trace("Started CheckifDefaultPriceListIsLatest Method.");

            return false;
        }

        //UnTag record as global if not yet published
        public Entity ValidateGlobal(Entity priceList)
        {
            var isGlobal = priceList.GetAttributeValue<Boolean>("gsc_isglobalrecord");
            var isPublish = priceList.GetAttributeValue<Boolean>("gsc_publish");
            var publishEnabled = priceList.GetAttributeValue<Boolean>("gsc_publishenabled");

            if (isPublish == false && isGlobal == true)
            {
                Entity priceListToUpdate = _organizationService.Retrieve(priceList.LogicalName, priceList.Id,
                    new ColumnSet("gsc_isglobalrecord", "gsc_publishenabled"));
                priceListToUpdate["gsc_isglobalrecord"] = false;

                if (isGlobal == true && publishEnabled == false)
                {
                    priceListToUpdate["gsc_publishenabled"] = true;
                }

                _organizationService.Update(priceListToUpdate);

                return priceListToUpdate;
            }
            else if (isPublish == true && isGlobal == false)
            {
                Entity priceListToUpdate = _organizationService.Retrieve(priceList.LogicalName, priceList.Id,
                    new ColumnSet("gsc_isglobalrecord"));
                priceListToUpdate["gsc_isglobalrecord"] = true;
                _organizationService.Update(priceListToUpdate);

                return priceListToUpdate;
            }

            return null;
        }

        //Created By: Jerome Anthony Gerero, Created On: 04/21/17
        /*Purpose: Update accessory sell price if price list has been published.
         * Event/Message:
         *      Post/Update: Published = gsc_publish
         * Primary Entity: Price List
         */
        public Entity UpdateAccessorySellPrice(Entity priceListEntity)
        {
            _tracingService.Trace("Started UpdateAccessorySellPrice method.");

            Boolean isPublished = priceListEntity.GetAttributeValue<Boolean>("gsc_publish");

            EntityReference classId = priceListEntity.GetAttributeValue<EntityReference>("gsc_pricelisttypeid") != null
                ? priceListEntity.GetAttributeValue<EntityReference>("gsc_pricelisttypeid")
                : null;

            Entity classEntity = _organizationService.Retrieve("gsc_cmn_classmaintenance", classId.Id, new ColumnSet("gsc_type"));

            Boolean isLatest = CheckifDefaultPriceListIsLatest(priceListEntity);

            if (isPublished == false || !classEntity.FormattedValues["gsc_type"].Equals("Item") || isLatest == false || !classId.Name.Contains("accessory")) { return null; }

            EntityCollection productPriceLevelRecords = CommonHandler.RetrieveRecordsByOneValue("productpricelevel", "pricelevelid", priceListEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "amount" });

            if (productPriceLevelRecords != null && productPriceLevelRecords.Entities.Count > 0)
            {
                foreach (Entity productPriceLevel in productPriceLevelRecords.Entities)
                {
                    Decimal amount = productPriceLevel.Contains("amount")
                        ? productPriceLevel.GetAttributeValue<Money>("amount").Value
                        : Decimal.Zero;

                    Guid productId = productPriceLevel.GetAttributeValue<EntityReference>("productid") != null
                        ? productPriceLevel.GetAttributeValue<EntityReference>("productid").Id
                        : Guid.Empty;

                    EntityCollection vehicleAccessoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_vehicleaccessory", "gsc_itemid", productId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_sellingprice" });

                    if (vehicleAccessoryRecords != null && vehicleAccessoryRecords.Entities.Count > 0)
                    {
                        Entity vehicleAccessory = vehicleAccessoryRecords.Entities[0];

                        if (amount > 0)
                        {
                            vehicleAccessory["gsc_sellprice"] = new Money(amount);

                            _organizationService.Update(vehicleAccessory);
                        }
                    }

                }
            }

            _tracingService.Trace("Ended UpdateAccessorySellPrice method.");
            return priceListEntity;
        }
    }
}
