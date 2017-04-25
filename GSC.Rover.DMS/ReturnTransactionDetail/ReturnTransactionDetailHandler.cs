using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace ReturnTransactionDetail
{
    public class ReturnTransactionDetailHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ReturnTransactionDetailHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 7/15/2016
        /*Purpose: Set newly created Return Transaction Detail fields
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: Inventory = gsc_inventoryid
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Return Transaction Detail
         */
        public Entity SetReturnTransactionDetailFields(Entity returnTransactionDetailEntity)
        {
            _tracingService.Trace("Started SetReturnTransactionDetailFields Method...");

            var inventoryId = returnTransactionDetailEntity.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                ? returnTransactionDetailEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                : Guid.Empty;

            //Retrieve Inventory records
            EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventorypn" });

            if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
            {
                Entity inventory = inventoryRecords.Entities[0];

                returnTransactionDetailEntity["gsc_returntransactiondetailpn"] = inventory.Contains("gsc_inventorypn")
                    ? inventory.GetAttributeValue<String>("gsc_inventorypn")
                    : String.Empty;

                _organizationService.Update(returnTransactionDetailEntity);
            }

            _tracingService.Trace("Ended SetReturnTransactionDetailFields Method...");
            return returnTransactionDetailEntity;
        }
    }
}
