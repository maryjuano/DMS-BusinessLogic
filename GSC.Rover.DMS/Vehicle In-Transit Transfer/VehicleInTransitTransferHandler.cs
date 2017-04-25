using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk.Query;

namespace GSC.Rover.DMS.BusinessLogic.VehicleInTransitTransfer
{
    public class VehicleInTransitTransferHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleInTransitTransferHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Raphael Herrera, Created On: 8/22/2016
        /*Purpose: Set created records to gsc_intransitttransferstatus = picked
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: Vehicle In-Transit Transfer
         * Primary Entity: gsc_iv_vehicleintransittransfer
         */
        public void PopulateFields(Entity vehicleInTransitTransfer)
        {
            _tracingService.Trace("Started Populate Fields method...");
            //Set status to Picked
            vehicleInTransitTransfer["gsc_intransittransferstatus"] = new OptionSetValue(100000000);
            vehicleInTransitTransfer["gsc_siteid"] = new EntityReference("gsc_iv_site", vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_sourcesiteid").Id);
            _tracingService.Trace("Ending Populate Fields method...");

        }

        //Created By: Raphael Herrera, Created On: 8/23/2016
        /*Purpose: Create new allocated vehicle record. 
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_inventoryidtoallocate
         * Primary Entity: gsc_iv_vehicleintransittransfer
         */
        public void AllocateVehicle(Entity vehicleInTransitTransfer)
        {
            _tracingService.Trace("Started AllocateVehicle method...");

            var inventoryId = vehicleInTransitTransfer.Contains("gsc_inventoryidtoallocate") ? vehicleInTransitTransfer.GetAttributeValue<string>("gsc_inventoryidtoallocate")
                : String.Empty;

            EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_status", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_productquantityid", "gsc_modelyear" });

            _tracingService.Trace("Inventory records retrieved: " + inventoryCollection.Entities.Count);

            if (inventoryCollection.Entities.Count > 0)
            {
                Entity inventoryEntity = inventoryCollection.Entities[0];

                if (inventoryEntity.GetAttributeValue<OptionSetValue>("gsc_status").Value == 100000000)
                {
                    _tracingService.Trace("Status of inventory is available...");
                    #region Update Inventory and product quantity

                    //set status to allocated
                    inventoryEntity["gsc_status"] = new OptionSetValue(100000001);
                    _organizationService.Update(inventoryEntity);
                    _tracingService.Trace("Updated inventory status to allocated...");

                    var productQuantityId = inventoryEntity.Contains("gsc_productquantityid") ? inventoryEntity.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                        : Guid.Empty;

                    EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService,
                        null, OrderType.Ascending, new[] { "gsc_allocated", "gsc_available", "gsc_siteid", "gsc_vehiclemodelid", "gsc_productid" });

                    _tracingService.Trace("ProductQuantity records retrieved: " + productQuantityCollection.Entities.Count);
                    if (productQuantityCollection.Entities.Count > 0)
                    {
                        Entity productQuantityEntity = productQuantityCollection.Entities[0];

                        Int32 allocated = productQuantityEntity.Contains("gsc_allocated") ? productQuantityEntity.GetAttributeValue<Int32>("gsc_allocated")
                            : 0;
                        Int32 available = productQuantityEntity.Contains("gsc_available") ? productQuantityEntity.GetAttributeValue<Int32>("gsc_available")
                            : 0;

                        productQuantityEntity["gsc_allocated"] = allocated + 1;
                        productQuantityEntity["gsc_available"] = available - 1;

                        _organizationService.Update(productQuantityEntity);
                        _tracingService.Trace("Updated productquantity count...");

                        #region Create VehicleAllocation Record

                        Entity allocatedVehicle = new Entity("gsc_iv_allocatedvehicle");
                        var destinationSiteId = vehicleInTransitTransfer.Contains("gsc_destinationsiteid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_destinationsiteid").Id
                            : Guid.Empty;
                        var sourceSiteId = productQuantityEntity.Contains("gsc_siteid") ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                            : Guid.Empty;
                        var viaSiteId = vehicleInTransitTransfer.Contains("gsc_viasiteid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_viasiteid").Id
                            : Guid.Empty;
                        var baseModelId = productQuantityEntity.Contains("gsc_vehiclemodelid") ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id
                            : Guid.Empty;
                        var productId = productQuantityEntity.Contains("gsc_productid") ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                            : Guid.Empty;

                        allocatedVehicle["gsc_color"] = inventoryEntity.GetAttributeValue<String>("gsc_color");
                        allocatedVehicle["gsc_csno"] = inventoryEntity.GetAttributeValue<String>("gsc_csno");
                        allocatedVehicle["gsc_engineno"] = inventoryEntity.GetAttributeValue<String>("gsc_engineno");
                        allocatedVehicle["gsc_modelcode"] = inventoryEntity.GetAttributeValue<String>("gsc_modelcode");
                        allocatedVehicle["gsc_optioncode"] = inventoryEntity.GetAttributeValue<String>("gsc_optioncode");
                        allocatedVehicle["gsc_productionno"] = inventoryEntity.GetAttributeValue<String>("gsc_productionno");
                        allocatedVehicle["gsc_vin"] = inventoryEntity.GetAttributeValue<String>("gsc_vin");
                        //Set transaction type to In-Transit Transfer
                        allocatedVehicle["gsc_transactiontype"] = new OptionSetValue(100000001);
                        allocatedVehicle["gsc_vehicleallocateddate"] = DateTime.Today;
                        allocatedVehicle["gsc_vehiclebasemodelid"] = new EntityReference("gsc_iv_vehiclebasemodel", baseModelId);
                        allocatedVehicle["gsc_productid"] = new EntityReference("product", productId);
                        allocatedVehicle["gsc_modelyear"] = inventoryEntity.GetAttributeValue<string>("gsc_modelyear");
                        allocatedVehicle["gsc_inventoryid"] = new EntityReference(inventoryEntity.LogicalName, inventoryEntity.Id);
                        allocatedVehicle["gsc_vehicleintransittransferid"] = new EntityReference(vehicleInTransitTransfer.LogicalName, vehicleInTransitTransfer.Id);
                        if(destinationSiteId != Guid.Empty)
                            allocatedVehicle["gsc_destinationsiteid"] = new EntityReference("gsc_iv_site", destinationSiteId);
                        allocatedVehicle["gsc_sourcesiteid"] = new EntityReference("gsc_iv_site", sourceSiteId);
                        allocatedVehicle["gsc_viasiteid"] = new EntityReference("gsc_iv_site", viaSiteId);
                        _organizationService.Create(allocatedVehicle);

                        _tracingService.Trace("Created vehicle allocation record...");

                        #endregion
                    }

                    #endregion

                }

                else
                    throw new InvalidPluginExecutionException("The inventory for entered vehicle is not available.");
            }

            _tracingService.Trace("Ending AllocateVehicle method...");
        }


        //Created By: Raphael Herrera, Created On: 8/24/2016
        /*Purpose: Handle Delete AND Cancel BL for Vehicle In-Transit Transfer record
         * Registration Details:
         * Event/Message: 
         *      Pre/Delete: VehicleInTransitTransfer
         *      Post/Update: gsc_intransittransferstatus
         * Primary Entity: gsc_iv_vehicleintransittransfer
         */
        public void ValidateTransaction(Entity vehicleInTransitTransfer, string message)
        {
            _tracingService.Trace("Started ValidateDelete method...");
            //Status == Picked
            if (vehicleInTransitTransfer.GetAttributeValue<OptionSetValue>("gsc_intransittransferstatus").Value == 100000000)
            {
                _tracingService.Trace("Status is Picked...");
                EntityCollection allocatedVehicleCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_allocatedvehicle", "gsc_vehicleintransittransferid", vehicleInTransitTransfer.Id, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_inventoryid" });

                _tracingService.Trace("AllocatedVehicle records retrieved: " + allocatedVehicleCollection.Entities.Count);
                if (allocatedVehicleCollection.Entities.Count > 0)
                {
                    foreach (Entity allocatedVehicleEntity in allocatedVehicleCollection.Entities)
                    {
                        var inventoryId = allocatedVehicleEntity.Contains("gsc_inventoryid") ? allocatedVehicleEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                           : Guid.Empty;

                        //Retrieve and update inventory
                        EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService,
                            null, OrderType.Ascending, new[] { "gsc_status", "gsc_productquantityid" });

                        _tracingService.Trace("Inventory records retrieved: " + inventoryCollection.Entities.Count);
                        if (inventoryCollection.Entities.Count > 0)
                        {
                            Entity inventory = inventoryCollection.Entities[0];

                            //Status = Available
                            inventory["gsc_status"] = new OptionSetValue(100000000);

                            _organizationService.Update(inventory);
                            _tracingService.Trace("Updated inventory record...");

                            var productQuantityId = inventory.Contains("gsc_productquantityid") ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                                : Guid.Empty;

                            //Retrieve and update product quantity
                            EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService,
                                null, OrderType.Ascending, new[] { "gsc_available", "gsc_allocated"});

                            _tracingService.Trace("ProductQuantity records retrieved: " + productQuantityCollection.Entities.Count);
                            if (productQuantityCollection.Entities.Count > 0)
                            {
                                Entity productQuantity = productQuantityCollection.Entities[0];
                                Int32 available = productQuantity.GetAttributeValue<Int32>("gsc_available");
                                Int32 allocated = productQuantity.GetAttributeValue<Int32>("gsc_allocated");

                                productQuantity["gsc_available"] = available + 1;
                                productQuantity["gsc_allocated"] = allocated - 1;

                                _organizationService.Update(productQuantity);
                                _tracingService.Trace("Product Quantity updated...");

                                //Delete Vehicle Allocation
                                _organizationService.Delete(allocatedVehicleEntity.LogicalName, allocatedVehicleEntity.Id);
                                _tracingService.Trace("Deleted associated Allocated Vehicle record...");

                                //Clear inventoryidtoallocate field
                                vehicleInTransitTransfer["gsc_inventoryidtoallocate"] = "";
                                _organizationService.Update(vehicleInTransitTransfer);
                                _tracingService.Trace("Updated Vehicle In-Transit Transfer record...");
                            }           
                        }
                    }
                }
            }

            //Status != Picked
            else
            {
                _tracingService.Trace("Status is not Picked...");
                if (message == "Delete")
                    throw new InvalidPluginExecutionException("Unable to delete Shipped Vehicle In-Transit Transfer record.");
                else if (message == "Update")
                {
                    throw new InvalidPluginExecutionException("Unable to cancel Shipped Vehicle In-Transit Transfer record.");
                }
            }
            _tracingService.Trace("Ending ValidateDelete method...");
        }

        //Created By: Raphael Herrera, Created On: 8/26/2016
        /*Purpose: Handle BL for setting Vehicle In-Transit Transfer status to 'Shipped'
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_intransittransferstatus
         * Primary Entity: gsc_iv_vehicleintransittransfer
         */
        public void ShipVehicle(Entity vehicleInTransitTransfer)
        {
            _tracingService.Trace("Started ShipVehicle method...");

            //Status == Picked
            if (vehicleInTransitTransfer.GetAttributeValue<OptionSetValue>("gsc_intransittransferstatus").Value == 100000000)
            {
                _tracingService.Trace("Status is Picked...");

                EntityCollection allocatedVehicleCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_allocatedvehicle", "gsc_vehicleintransittransferid", vehicleInTransitTransfer.Id, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_inventoryid" });

                _tracingService.Trace("AllocatedVehicle records retrieved: " + allocatedVehicleCollection.Entities.Count);
                if (allocatedVehicleCollection.Entities.Count > 0)
                {
                    #region Create Vehicle In-Transit Transfer Receiving Entity
                    Entity inTransitReceivingEntity = new Entity("gsc_iv_vehicleintransittransferreceiving");

                    var receivingDestinationSiteId = vehicleInTransitTransfer.Contains("gsc_destinationsiteid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_destinationsiteid").Id
                       : Guid.Empty;
                    var receivingRecordOwnerId = vehicleInTransitTransfer.Contains("gsc_recordownerid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_recordownerid").Id
                        : Guid.Empty;
                    var receivingBranchId = vehicleInTransitTransfer.Contains("gsc_branchid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_branchid").Id
                        : Guid.Empty;
                    var receivingDealerId = vehicleInTransitTransfer.Contains("gsc_dealerid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_dealerid").Id
                        : Guid.Empty;

                    inTransitReceivingEntity["gsc_actualreceiptdate"] = DateTime.UtcNow;
                    inTransitReceivingEntity["gsc_description"] = vehicleInTransitTransfer.Contains("gsc_description") ? vehicleInTransitTransfer.GetAttributeValue<string>("gsc_description")
                        : String.Empty;
                    inTransitReceivingEntity["gsc_destinationsiteid"] = new EntityReference("gsc_iv_site", receivingDestinationSiteId);
                    inTransitReceivingEntity["gsc_destinationbranch"] = vehicleInTransitTransfer.Contains("gsc_destinationbranchid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_destinationbranchid").Name
                        : string.Empty;
                    //Status = Shipped
                    inTransitReceivingEntity["gsc_intransitstatus"] = new OptionSetValue(100000000);
                    inTransitReceivingEntity["gsc_intransittransferid"] = new EntityReference(vehicleInTransitTransfer.LogicalName, vehicleInTransitTransfer.Id);
                    inTransitReceivingEntity["gsc_intransittransferremarks"] = vehicleInTransitTransfer.Contains("gsc_remarks") ? vehicleInTransitTransfer.GetAttributeValue<string>("gsc_remarks")
                        : string.Empty;
                    inTransitReceivingEntity["gsc_sourcebranch"] = vehicleInTransitTransfer.Contains("gsc_sourcebranchid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_sourcebranchid").Name
                        : string.Empty;
                    inTransitReceivingEntity["gsc_sourcesite"] = vehicleInTransitTransfer.Contains("gsc_sourcesiteid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_sourcesiteid").Name
                        : string.Empty;
                    inTransitReceivingEntity["gsc_viasite"] = vehicleInTransitTransfer.Contains("gsc_viasiteid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_viasiteid").Name
                        : string.Empty;
                    inTransitReceivingEntity["gsc_recordownerid"] = new EntityReference("contact", receivingRecordOwnerId);
                    inTransitReceivingEntity["gsc_branchid"] = new EntityReference("account", receivingBranchId);
                    inTransitReceivingEntity["gsc_dealerid"] = new EntityReference("account", receivingDealerId);

                    _organizationService.Create(inTransitReceivingEntity);
                    _tracingService.Trace("Created Vehicle In-Transit Transfer Receiving record...");
                    #endregion

                    foreach (Entity allocatedVehicleEntity in allocatedVehicleCollection.Entities)
                    {
                        var inventoryId = allocatedVehicleEntity.Contains("gsc_inventoryid") ? allocatedVehicleEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                           : Guid.Empty;

                        //Retrieve inventory
                        EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService,
                            null, OrderType.Ascending, new[] { "gsc_status", "gsc_productquantityid" });

                        _tracingService.Trace("Inventory records retrieved: " + inventoryCollection.Entities.Count);

                        if (inventoryCollection.Entities.Count > 0)
                        {
                            Entity inventory = inventoryCollection.Entities[0];

                            var sourceProdQuantityId = inventory.Contains("gsc_productquantityid") ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                                : Guid.Empty;

                            //Retrieve  source site product quantity
                            EntityCollection sourceProdQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", sourceProdQuantityId, _organizationService,
                                null, OrderType.Ascending, new[] { "gsc_onhand", "gsc_allocated", "gsc_productid"});

                            _tracingService.Trace("Source ProductQuantity records retrieved: " + sourceProdQuantityCollection.Entities.Count);

                            if (sourceProdQuantityCollection.Entities.Count > 0)
                            {
                                Entity sourceProdQuantity = sourceProdQuantityCollection.Entities[0];

                                //Retrieve destination site product quantity
                                var viaSiteId = vehicleInTransitTransfer.Contains("gsc_viasiteid") ? vehicleInTransitTransfer.GetAttributeValue<EntityReference>("gsc_viasiteid").Id
                                    : Guid.Empty;
                                var productId = sourceProdQuantity.Contains("gsc_productid") ? sourceProdQuantity.GetAttributeValue<EntityReference>("gsc_productid").Id
                                    : Guid.Empty;

                                var destinationConditionList = new List<ConditionExpression>
                                {
                                    new ConditionExpression("gsc_siteid", ConditionOperator.Equal, viaSiteId),
                                    new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId)
                                };

                                EntityCollection destinationProdQuantityCollection = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", destinationConditionList, _organizationService, null,
                                    OrderType.Ascending, new[] { "gsc_onhand", "gsc_available" });

                                _tracingService.Trace("Destination ProductQuantity records retrieved: " + destinationProdQuantityCollection.Entities.Count);
                                if (destinationProdQuantityCollection.Entities.Count > 0)
                                {
                                    #region BL execution of method
                                    Entity viaProdQuantity = destinationProdQuantityCollection.Entities[0];

                                    Int32 sourceOnHand = sourceProdQuantity.GetAttributeValue<Int32>("gsc_onhand");
                                    Int32 sourceAllocated = sourceProdQuantity.GetAttributeValue<Int32>("gsc_allocated");
                                    Int32 viaOnHand = viaProdQuantity.GetAttributeValue<Int32>("gsc_onhand");
                                    Int32 viaAvailable = viaProdQuantity.GetAttributeValue<Int32>("gsc_available");

                                    //Adjust source product quantity
                                    sourceProdQuantity["gsc_onhand"] = sourceOnHand - 1;
                                    sourceProdQuantity["gsc_allocated"] = sourceAllocated - 1;

                                    _organizationService.Update(sourceProdQuantity);
                                    _tracingService.Trace("Source Product Quantity updated...");

                                    //Adjust destination product quantity
                                    viaProdQuantity["gsc_onhand"] = viaOnHand + 1;
                                    viaProdQuantity["gsc_available"] = viaAvailable + 1;

                                    _organizationService.Update(viaProdQuantity);
                                    _tracingService.Trace("Destination Product Quantity updated...");

                                    //Update Inventory Status = Available
                                    inventory["gsc_status"] = new OptionSetValue(100000000);
                                    inventory["gsc_productquantityid"] = new EntityReference(viaProdQuantity.LogicalName, viaProdQuantity.Id);

                                    _organizationService.Update(inventory);
                                    _tracingService.Trace("Updated inventory record...");

                                    //Clear inventoryidtoallocate field
                                    vehicleInTransitTransfer["gsc_inventoryidtoallocate"] = "";
                                    _organizationService.Update(vehicleInTransitTransfer);
                                    _tracingService.Trace("Updated Vehicle In-Transit Transfer record...");

                                    #endregion
                                }
                            }
                        }
                    }
                }
                else
                    throw new InvalidPluginExecutionException("No Allocated Vehicle to Ship.");
            }

            //Status != Picked
            else
            {
                _tracingService.Trace("Status is not Picked...");
                throw new InvalidPluginExecutionException("Unable to Ship Vehicle In-Transit Transfer record with this status.");
            }
            _tracingService.Trace("Ending ShipVehicle method...");
        }
    }
}
