using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.PriceListItem;

namespace GSC.Rover.DMS.BusinessLogic.ExtendedPriceListItem
{
    public class ExtendedPriceListItemHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ExtendedPriceListItemHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }


        public void OnImportVehiclePriceListItem(Entity extendedPriceListItem)
        {
            if (extendedPriceListItem.Contains("gsc_productid"))
            {
                if (extendedPriceListItem.GetAttributeValue<EntityReference>("gsc_productid") != null)
                {
                    _tracingService.Trace("Extended Created manually.");
                    return;
                }
            }

            var optionCode = extendedPriceListItem.Contains("gsc_optioncode")
                ? extendedPriceListItem.GetAttributeValue<String>("gsc_optioncode")
                : String.Empty;
            var modelCode = extendedPriceListItem.Contains("gsc_modelcode")
                ? extendedPriceListItem.GetAttributeValue<String>("gsc_modelcode")
                : String.Empty;

            //Not Vehicle Price List item
            if (optionCode == String.Empty && modelCode == String.Empty)
                return;

            _tracingService.Trace("Started UpdateExtendedPriceListItem Method..");

            PriceListItemHandler priceListItemHandler = new PriceListItemHandler(_organizationService, _tracingService);
            var productEntity = priceListItemHandler.OnImportGetVehicle(extendedPriceListItem);

            var priceListName = extendedPriceListItem.GetAttributeValue<EntityReference>("gsc_pricelistid") != null
                ? extendedPriceListItem.GetAttributeValue<EntityReference>("gsc_pricelistid").Name
                : "";

            extendedPriceListItem["gsc_productid"] = new EntityReference("product", productEntity.Id);
            extendedPriceListItem["gsc_extendedpricelistitempn"] = priceListName;
            extendedPriceListItem["gsc_modelyear"] = productEntity.Contains("gsc_modelyear")
                ? productEntity.GetAttributeValue<String>("gsc_modelyear")
                : String.Empty;
            extendedPriceListItem["gsc_basemodel"] = productEntity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                ? productEntity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Name
                : String.Empty;

            _organizationService.Update(extendedPriceListItem);

            _tracingService.Trace("Ended UpdateExtendedPriceListItem Method..");

            CreatePriceLisItem(extendedPriceListItem, productEntity);

        }

        public void CreatePriceLisItem(Entity entity, Entity productEntity)
        {
            _tracingService.Trace("Started CreatePriceLisItem Method..");

            var uomName = entity.Contains("gsc_unit")
                ? entity.GetAttributeValue<String>("gsc_unit")
                : null;
            var uomId = Guid.Empty;
            _tracingService.Trace("UOm Name=" + uomName.ToString());
            EntityCollection uomCollection = CommonHandler.RetrieveRecordsByOneValue("uom", "name", uomName, _organizationService, null, OrderType.Ascending,
                new[] { "name" });

            if (uomCollection != null && uomCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Unit Retrieved..");

                uomId = uomCollection.Entities[0].Id;
            }
            _tracingService.Trace("Entity Price List Item..");
            Entity priceListItem = new Entity("productpricelevel");
            priceListItem["pricelevelid"] = entity.GetAttributeValue<EntityReference>("gsc_pricelistid") != null
                ? entity.GetAttributeValue<EntityReference>("gsc_pricelistid")
                : null;
            priceListItem["productid"] = new EntityReference("product", productEntity.Id);
            priceListItem["uomid"] = new EntityReference("uom", uomId);
            
            priceListItem["amount"] = entity.Contains("gsc_amount")
                ? entity.GetAttributeValue<Money>("gsc_amount")
                : new Money(0);
            priceListItem["transactioncurrencyid"] = entity.GetAttributeValue<EntityReference>("transactioncurrencyid") != null
                ? entity.GetAttributeValue<EntityReference>("transactioncurrencyid")
                : null;

            _tracingService.Trace("Product " + productEntity.Id.ToString());
            Guid priceListItemId = _organizationService.Create(priceListItem);

            _tracingService.Trace("Price List Created.." + priceListItemId.ToString());

            //Collection of entities to be associated to Price List Item
            EntityReferenceCollection priceListItemCollection = new EntityReferenceCollection();
            priceListItemCollection.Add(new EntityReference("productpricelevel", priceListItemId));

            //Associate Extended Price List Item with Price List Item
            _organizationService.Associate("gsc_cmn_extendedpricelistitem", entity.Id, new Relationship("gsc_gsc_cmn_extendedpricelistitem_productpric"), priceListItemCollection);

            _tracingService.Trace("Price List Associated..");

            _tracingService.Trace("Ended CreatePriceLisItem Method..");
        }

        //Created By : Artum M. Ramos, Created On : 2/21/2017
        /*Purpose: On Import Extended Price list Item
         * Event/Message:
         *      Pre/Create: Item Id
         *      Post/Update:
         * Primary Entity: Vehicle Sales Return
         */
        public Entity OnImportItemPriceListItem(Entity extendedPriceListItem)
        {
            if (extendedPriceListItem.Contains("gsc_productid"))
            {
                if (extendedPriceListItem.GetAttributeValue<EntityReference>("gsc_productid") != null)
                {
                    _tracingService.Trace("Extended Created manually.");
                    return extendedPriceListItem;
                }
            }

            _tracingService.Trace("Started OnImportExtendedPricelistItem method..");

            var itemNumber = extendedPriceListItem.Contains("gsc_itemnumber")
                ? extendedPriceListItem.GetAttributeValue<String>("gsc_itemnumber")
                : String.Empty;

            //Not Item Price List item
            if (itemNumber == String.Empty)
                return null;

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productnumber", itemNumber, _organizationService, null, OrderType.Ascending,
                new[] { "name" });

            _tracingService.Trace("Check if Product Collection is Null");
            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                Entity productEntity = productCollection.Entities[0];
                _tracingService.Trace("Product Retrieved..");
                extendedPriceListItem["gsc_productid"] = new EntityReference("product", productEntity.Id);

                _organizationService.Update(extendedPriceListItem);

                _tracingService.Trace("Ended OnImportExtendedPricelistItem Method..");
                CreatePriceLisItem(extendedPriceListItem, productEntity);
                
                return extendedPriceListItem;
            }
            else
            {
                throw new InvalidPluginExecutionException("The Item Number doesn't exist.");
            }
        }
    }
}
