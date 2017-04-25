using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.VehicleCabChassis
{
    public class VehicleCabChassisHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleCabChassisHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie G. Baliguat, Created On: 9/16/2016
        /*Purpose: Replicate Item Details to Vehicle Cab Chassis
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: Item Description
         * Primary Entity: Quote
         */
        public void ReplicateAccessoryDetail(Entity vehicleCabChassis)
        {
            _tracingService.Trace("Star ReplicateAccessoryDetail Method ");
            var item = vehicleCabChassis.GetAttributeValue<EntityReference>("gsc_itemid") != null
                ? vehicleCabChassis.GetAttributeValue<EntityReference>("gsc_itemid")
                : null;

            EntityCollection itemRecords = CommonHandler.RetrieveRecordsByOneValue("product", "productid", item.Id, _organizationService, null, OrderType.Ascending,
                new[] { "productnumber", "pricelevelid", "gsc_defaultsellinguomid", "name"});
            _tracingService.Trace("1 ");
            if (itemRecords != null && itemRecords.Entities.Count > 0)
             {
                var itemEntity = itemRecords.Entities[0];
                _tracingService.Trace("2 ");
                var pricelistId = itemEntity.GetAttributeValue<EntityReference>("pricelevelid") != null
                    ? itemEntity.GetAttributeValue<EntityReference>("pricelevelid").Id
                    : Guid.Empty;
               
                var productConditionList = new List<ConditionExpression>
                    {

                        new ConditionExpression("pricelevelid", ConditionOperator.Equal, pricelistId),
                        new ConditionExpression("productid", ConditionOperator.Equal, itemEntity.Id)
                    };

                //Retrieve related products from Product Relationship entity
                EntityCollection priceListItemRecords = CommonHandler.RetrieveRecordsByConditions("productpricelevel", productConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "amount" });
                _tracingService.Trace("3 ");
                if (priceListItemRecords != null && priceListItemRecords.Entities.Count > 0)
                {
                    var priceListItemEntity = priceListItemRecords.Entities[0];
                    _tracingService.Trace("4");
                    vehicleCabChassis["gsc_sellingprice"] = priceListItemEntity.Contains("amount")
                        ? priceListItemEntity.GetAttributeValue<Money>("amount")
                        : new Money(0);
                }

                vehicleCabChassis["gsc_vehiclecabchassispn"] = itemEntity.Contains("name")
                    ? itemEntity.GetAttributeValue<String>("name")
                    : String.Empty;
                vehicleCabChassis["gsc_itemnumber"] = itemEntity.Contains("productnumber")
                    ? itemEntity.GetAttributeValue<String>("productnumber")
                    : String.Empty;
                vehicleCabChassis["gsc_pricelevelid"] = itemEntity.GetAttributeValue<EntityReference>("pricelevelid") != null
                    ? itemEntity.GetAttributeValue<EntityReference>("pricelevelid")
                    : null;
                vehicleCabChassis["gsc_defaultsellinguom"] = itemEntity.GetAttributeValue<EntityReference>("gsc_defaultsellinguomid") != null
                    ? itemEntity.GetAttributeValue<EntityReference>("gsc_defaultsellinguomid").Name
                    : String.Empty;
                _tracingService.Trace("5");
                
             }
        }

        //Created By: Artum M. Ramos, Created On: 1/3/2017
        /*Purpose: import Vehicle Cab Chassis
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: Item Number
         *      Post/Update: 
         * Primary Entity: Vehicle Cab Chassis
         */
        public Entity OnImportVehicleCabChassis(Entity vehicleCabChassis)
        {
            if (vehicleCabChassis.Contains("gsc_itemid"))
            {
                if (vehicleCabChassis.GetAttributeValue<EntityReference>("gsc_itemid") != null)
                    return null;
            }
            _tracingService.Trace("Started OnImportVehicleCabChassis method..");

            var itemNumber = vehicleCabChassis.Contains("gsc_itemnumber")
                ? vehicleCabChassis.GetAttributeValue<String>("gsc_itemnumber")
                : String.Empty;

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productnumber", itemNumber, _organizationService, null, OrderType.Ascending,
                new[] { "name", "gsc_shortdescription" });
            
            _tracingService.Trace("Check if Product Collection is Null");
            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                Entity product = productCollection.Entities[0];
                _tracingService.Trace("Product Retrieved..");
                vehicleCabChassis["gsc_itemid"] = new EntityReference("product", product.Id);;
                return productCollection.Entities[0];
            }
            else
            {
                throw new InvalidPluginExecutionException("The Item Number doesn't exist.");
            }
        }

        //Create By: Leslie Baliguat, Created On: 4/17/2017 /*
        /* Purpose:  Restrict adding of cab chassis in vehicle model that is already been added.
         * Registration Details:
         * Event/Message: 
         *      Pre-Create:
         * Primary Entity:  Vehicle Cab Chassis
         */
        public bool IsCabChassisDuplicate(Entity cabChassis)
        {
            var productId = CommonHandler.GetEntityReferenceIdSafe(cabChassis, "gsc_productid");
            var itemId = CommonHandler.GetEntityReferenceIdSafe(cabChassis, "gsc_itemid");

            var productConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                                new ConditionExpression("gsc_itemid", ConditionOperator.Equal, itemId)
                            };

            EntityCollection cabChassisCollection = CommonHandler.RetrieveRecordsByConditions("gsc_sls_vehiclecabchassis", productConditionList, _organizationService, null, OrderType.Ascending,
                     new[] { "gsc_productid" });

            if (cabChassisCollection != null && cabChassisCollection.Entities.Count > 0)
            {
                return true;
            }

            return false;
        }
    }
}
