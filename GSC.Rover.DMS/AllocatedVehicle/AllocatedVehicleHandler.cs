using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.InventoryMovement;

namespace GSC.Rover.DMS.BusinessLogic.AllocatedVehicle
{
    public class AllocatedVehicleHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService; 

        public AllocatedVehicleHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 5/18/2016
        /*Purpose: Update Order Status, Vehcicle Allocated Date and Inventory Id to Allocate fields of Sales Order to where Allocated vehicle is associated
         *         Update Inventory Status of Inventory Record that is related to Allocated Vehicle
         *         Update Available and Allocated fields of Product Quantity to where Inventory is associated
         * Registration Details: 
         * Event/Message: 
         *      PreValidate/Delete: 
         * Primary Entity: Allocated Vehicle
         */
        public void RemoveAllocation(Entity allocatedEntity)
        {
            _tracingService.Trace("Started RemoveAllocation Method");

            Entity salesOrderToUpdate = new Entity("salesorder");
            if (allocatedEntity.GetAttributeValue<EntityReference>("gsc_orderid") != null)
            {
                _tracingService.Trace("Order Id not null");

                var orderId = allocatedEntity.GetAttributeValue<EntityReference>("gsc_orderid").Id;

                EntityCollection orderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", orderId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_status", "gsc_vehicleallocateddate", "gsc_inventoryidtoallocate", "name" });

               
                if (orderRecords != null && orderRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved Related Order Details ");

                    salesOrderToUpdate = orderRecords.Entities[0];

                    var status = salesOrderToUpdate.Contains("gsc_status")
                        ? salesOrderToUpdate.GetAttributeValue<OptionSetValue>("gsc_status")
                        : null;

                    if (status.Value == 100000003)
                    {
                        salesOrderToUpdate["gsc_status"] = new OptionSetValue(100000002);
                        salesOrderToUpdate["gsc_vehicleallocateddate"] = (DateTime?)null;
                        salesOrderToUpdate["gsc_inventoryidtoallocate"] = null;
                        _organizationService.Update(salesOrderToUpdate);
                    }

                      _tracingService.Trace("Related Order Updated");
                  }
            }
            /*************************************************************/
            //Modified By: Raphael Herrera, Modified On: 8/14/2016
            //Included cleanup for VehicleTransfer entity
            if (allocatedEntity.GetAttributeValue<EntityReference>("gsc_vehicletransferid") != null)
            {
                _tracingService.Trace("VehicleTransfer is not null");

                var vehicleTransferId = allocatedEntity.GetAttributeValue<EntityReference>("gsc_vehicletransferid").Id;

                EntityCollection vehicleTransferCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehicletransfer", "gsc_iv_vehicletransferid", vehicleTransferId, _organizationService, null, OrderType.Ascending,
                    new[] {"gsc_inventoryidtoallocate" });

                _tracingService.Trace("Vehicle Transfer records retrieved: " + vehicleTransferCollection.Entities.Count);
                if (vehicleTransferCollection != null && vehicleTransferCollection.Entities.Count > 0)
                {
                    Entity vehicleTransferEntity = vehicleTransferCollection.Entities[0];

                    vehicleTransferEntity["gsc_inventoryidtoallocate"] = null;
                    _organizationService.Update(vehicleTransferEntity);

                    _tracingService.Trace("Vehicle Transfer Record Updated");
                }
            }
            /*************************************************************/

            /*************************************************************/
            //Modified By: Raphael Herrera, Modified On: 8/25/2016
            //Included cleanup for Vehicle In-Transit Transfer Entity
            if (allocatedEntity.GetAttributeValue<EntityReference>("gsc_vehicleintransittransferid") != null)
            {
                _tracingService.Trace("Vehicle In-Transit Transfer is not null");

                var vehicleInTransitId = allocatedEntity.GetAttributeValue<EntityReference>("gsc_vehicleintransittransferid").Id;

                EntityCollection vehicleInTransitCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehicleintransittransfer", "gsc_iv_vehicleintransittransferid", vehicleInTransitId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_inventoryidtoallocate", "gsc_intransittransferstatus" });

                _tracingService.Trace("Vehicle In-Transit Transfer records retrieved: " + vehicleInTransitCollection.Entities.Count);
                if (vehicleInTransitCollection != null && vehicleInTransitCollection.Entities.Count > 0)
                {
                    Entity vehicleInTransit = vehicleInTransitCollection.Entities[0];

                    //In-Transit Transfer Status != Picked
                    if (vehicleInTransit.GetAttributeValue<OptionSetValue>("gsc_intransittransferstatus").Value != 100000000)
                        throw new InvalidPluginExecutionException("Unable to delete record that is already shipped.");

                    vehicleInTransit["gsc_inventoryidtoallocate"] = null;
                    _organizationService.Update(vehicleInTransit);

                    _tracingService.Trace("Vehicle Transfer Record Updated");
                }
            }
            /*************************************************************/

            if (allocatedEntity.GetAttributeValue<EntityReference>("gsc_inventoryid") != null)
            {
                _tracingService.Trace("Inventory Id not null");

                var inventoryId = allocatedEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id;

                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] {"gsc_status", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_productquantityid", "gsc_modelyear", "gsc_siteid", "gsc_productid", "gsc_basemodelid" });

                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved Related Inventory Details");

                    InventoryMovementHandler inventoryMovementHandler = new InventoryMovementHandler(_organizationService, _tracingService);
                    inventoryMovementHandler.UpdateInventoryStatus(inventoryRecords.Entities[0], 100000000);
                    Entity productQuantityEntity = inventoryMovementHandler.UpdateProductQuantity(inventoryRecords.Entities[0], 0, 1, -1, 0, 0, 0, 0, 0);

                    // Create Inventory History Log
                    inventoryMovementHandler.CreateInventoryQuantityAllocated(salesOrderToUpdate, inventoryRecords.Entities[0], productQuantityEntity, salesOrderToUpdate.GetAttributeValue<string>("name"),
                        DateTime.UtcNow, "For Allocation", Guid.Empty, 100000003);
                }
            }

            _tracingService.Trace("Ended RemoveAllocation Method.");
        }
    }
}
