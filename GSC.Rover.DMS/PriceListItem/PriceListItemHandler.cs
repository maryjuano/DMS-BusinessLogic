using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.PriceListItem
{
    public class PriceListItemHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public PriceListItemHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        public Entity OnImportGetVehicle(Entity entity)
        {
            if (entity.Contains("gsc_productid"))
            {
                if (entity.GetAttributeValue<EntityReference>("gsc_productid") != null)
                    return null;
            }

            _tracingService.Trace("Started OnImport method..");

            var optionCode = entity.Contains("gsc_optioncode")
                ? entity.GetAttributeValue<String>("gsc_optioncode")
                : String.Empty;
            var modelCode = entity.Contains("gsc_modelcode")
                ? entity.GetAttributeValue<String>("gsc_modelcode")
                : String.Empty;

            var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_optioncode", ConditionOperator.Equal, optionCode),
                        new ConditionExpression("gsc_modelcode", ConditionOperator.Equal, modelCode)
                    };

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByConditions("product", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "name", "gsc_optioncode", "gsc_modelcode", "gsc_modelyear", "gsc_vehiclemodelid" });

            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Product Retrieved..");
                entity["gsc_productid"] = new EntityReference("product", productCollection.Entities[0].Id);
                return productCollection.Entities[0];
            }
            else
            {
                throw new InvalidPluginExecutionException("The combination of Model Code and Option Code doesn't exist.");
            }
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/6/2016
        public Entity CreateExtendedPriceListItemRecord(Entity priceListItemEntity)
        {
            if (CheckifExtendedPriceListItemExist(priceListItemEntity)) { return null; };

            _tracingService.Trace("Started CreateManyToManyRelationship method..");

            var priceList = priceListItemEntity.GetAttributeValue<EntityReference>("pricelevelid") != null
                ? priceListItemEntity.GetAttributeValue<EntityReference>("pricelevelid")
                : null;
            var priceListId = Guid.Empty;
            var priceListName = String.Empty;

            if(priceList != null)
            {
                priceListId = priceList.Id;
                priceListName = priceList.Name;
            }

            var productId = priceListItemEntity.GetAttributeValue<EntityReference>("productid") != null
                ? priceListItemEntity.GetAttributeValue<EntityReference>("productid").Id
                : Guid.Empty;

            _tracingService.Trace("1");
            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId, _organizationService, null, OrderType.Ascending,
                new[] { "name", "gsc_optioncode", "gsc_modelcode", "gsc_modelyear", "gsc_vehiclemodelid" });
            _tracingService.Trace("2");
            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                Entity product = productCollection.Entities[0];
                _tracingService.Trace("3");
                var productName = product.Contains("name")
                    ? product.GetAttributeValue<String>("name")
                    : String.Empty;
                _tracingService.Trace("4");
                //Create Extended Price List Item record on Price List Item create
                Entity extendedPriceListItem = new Entity("gsc_cmn_extendedpricelistitem");
                extendedPriceListItem["gsc_extendedpricelistitempn"] = priceListName + "-" + productName;
                extendedPriceListItem["gsc_productid"] = new EntityReference("product", product.Id);
                extendedPriceListItem["gsc_pricelistid"] = new EntityReference("pricelevel", priceListId);
                extendedPriceListItem["gsc_modeldescription"] = productName;
                extendedPriceListItem["gsc_modelcode"] = product.Contains("gsc_modelcode")
                    ? product.GetAttributeValue<String>("gsc_modelcode")
                    : String.Empty;
                extendedPriceListItem["gsc_optioncode"] = product.Contains("gsc_optioncode")
                    ? product.GetAttributeValue<String>("gsc_optioncode")
                    : String.Empty;
                extendedPriceListItem["gsc_modelyear"] = product.Contains("gsc_modelyear")
                    ? product.GetAttributeValue<String>("gsc_modelyear")
                    : String.Empty;
                extendedPriceListItem["gsc_basemodel"] = product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                    ? product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Name
                    : String.Empty;
                _tracingService.Trace("5");
                Guid extendedPriceListItemId = _organizationService.Create(extendedPriceListItem);
                _tracingService.Trace("6");
                //Collection of entities to be associated to Price List Item
                EntityReferenceCollection extendedPriceListItemCollection = new EntityReferenceCollection();
                extendedPriceListItemCollection.Add(new EntityReference("gsc_cmn_extendedpricelistitem", extendedPriceListItemId));
                _tracingService.Trace("7");
                //Associate Extended Price List Item with Price List Item
                _organizationService.Associate("productpricelevel", priceListItemEntity.Id, new Relationship("gsc_gsc_cmn_extendedpricelistitem_productpric"), extendedPriceListItemCollection);

            }

            _tracingService.Trace("Ended CreateManyToManyRelationship method..");
            return priceListItemEntity;
        }

        private Boolean CheckifExtendedPriceListItemExist(Entity priceListItem)
        {
            var priceList = priceListItem.GetAttributeValue<EntityReference>("pricelevelid") != null
                ? priceListItem.GetAttributeValue<EntityReference>("pricelevelid").Id
                : Guid.Empty;

            var productId = priceListItem.GetAttributeValue<EntityReference>("productid") != null
                ? priceListItem.GetAttributeValue<EntityReference>("productid").Id
                : Guid.Empty;

            var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_pricelistid", ConditionOperator.Equal, priceList),
                        new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId)
                    };

            EntityCollection priceListItemRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_extendedpricelistitem", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_productid"});

            if (priceListItemRecords != null && priceListItemRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Extended Price List Exists..");

                return true;
            }

            _tracingService.Trace("Extended Price List Does not exist..");
            return false;
        }

        public void CheckifPriceListisDefault(Entity priceListItem)
        {
            _tracingService.Trace("Started CheckifPriceListisDefault Method..");

            var priceListID = priceListItem.GetAttributeValue<EntityReference>("pricelevelid") != null
                ? priceListItem.GetAttributeValue<EntityReference>("pricelevelid").Id
                : Guid.Empty;

            EntityCollection priceListCollection = CommonHandler.RetrieveRecordsByOneValue("pricelevel", "pricelevelid", priceListID, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_default" });

            if (priceListCollection != null && priceListCollection.Entities.Count > 0)
            {
                _tracingService.Trace("PriceList Retrieved..");

                Entity priceList = priceListCollection.Entities.FirstOrDefault();

                if (priceList.GetAttributeValue<Boolean>("gsc_default"))
                {
                    UpdateVehicleSellPrice(priceListItem);
                }
            }

        }

        public void UpdateVehicleSellPrice(Entity priceListItem)
        {
            var productID = priceListItem.GetAttributeValue<EntityReference>("productid") != null
                ? priceListItem.GetAttributeValue<EntityReference>("productid").Id
                : Guid.Empty;
            var pricelevelId = priceListItem.GetAttributeValue<EntityReference>("pricelevelid") != null
                ? priceListItem.GetAttributeValue<EntityReference>("pricelevelid")
                : null;

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productID, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_sellprice", "pricelevelid" });

            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Product.");

                Entity product = productCollection.Entities.FirstOrDefault();

                product["gsc_sellprice"] = priceListItem.Contains("amount")
                ? priceListItem.GetAttributeValue<Money>("amount")
                : new Money(0);
                product["pricelevelid"] = new EntityReference(pricelevelId.LogicalName, pricelevelId.Id);

                _organizationService.Update(product);

                _tracingService.Trace("Sell Price Updated.");
            }
        }
    }
}
