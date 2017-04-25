using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.SalesReturnDetail
{
    public class SalesReturnDetailHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public SalesReturnDetailHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Raphael Herrera, Created On : 06/02/2016
        /*Purpose: Adjust Inventory of related vehicle
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Sales Return Detail
         */
        public void PostTransaction(Entity salesReturnDetail)
        {
            _tracingService.Trace("Started PostTransaction method...");
            var inventoryId = salesReturnDetail.Contains("gsc_inventoryid") ? salesReturnDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id : Guid.Empty;


            EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_status" , "gsc_productquantityid", });

            _tracingService.Trace(inventoryCollection.Entities.Count + " Inventory Records Retrieved...");
            if (inventoryCollection.Entities.Count > 0)
            {
                Entity inventoryEntity = inventoryCollection.Entities[0];

                var productQuantityId = inventoryEntity.Contains("gsc_productquantityid") ? inventoryEntity.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                    : Guid.Empty;
                var branchSiteId = salesReturnDetail.Contains("gsc_branchsiteid") ? salesReturnDetail.GetAttributeValue<EntityReference>("gsc_branchsiteid").Id
                    : Guid.Empty;
                _tracingService.Trace("Retrieved ProductQuantity and Branch...");

                inventoryEntity["gsc_status"] = new OptionSetValue(100000000);

                _organizationService.Update(inventoryEntity);
                _tracingService.Trace("Updated Inventory Status to Release...");

                var productQuantityConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_iv_productquantityid", ConditionOperator.Equal, productQuantityId),
                    new ConditionExpression("gsc_siteid", ConditionOperator.Equal, branchSiteId)
                };

                EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_sold", "gsc_available", "gsc_onhand" });

                _tracingService.Trace(productQuantityCollection.Entities.Count + " Product Quantity Records Retrieved...");

                Entity productQuantity = productQuantityCollection.Entities[0];
                productQuantity["gsc_sold"] = productQuantity.GetAttributeValue<Int32>("gsc_sold") - 1;
                productQuantity["gsc_available"] = productQuantity.GetAttributeValue<Int32>("gsc_available") + 1;
                productQuantity["gsc_onhand"] = productQuantity.GetAttributeValue<Int32>("gsc_onhand") + 1;
                _tracingService.Trace("Adjusting Product Quantity...");

                _organizationService.Update(productQuantity);
            }
            _tracingService.Trace("Product Quantity Updated... Ending PostTransaction Method...");
        }
    }
}
