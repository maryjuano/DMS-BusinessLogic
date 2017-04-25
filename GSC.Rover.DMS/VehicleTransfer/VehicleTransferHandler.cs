using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.InventoryMovement;

namespace GSC.Rover.DMS.BusinessLogic.VehicleTransfer
{
    public class VehicleTransferHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleTransferHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Raphael Herrera, Created On: 8/1/2016
        /*Purpose: Set created records to gsc_transferstatus = unposted
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: VehicleTransfer
         * Primary Entity: gsc_iv_vehicletransfer
         */
        public void PopulateFields(Entity vehicleTransfer)
        {
            _tracingService.Trace("Started PopulateFields method...");
            vehicleTransfer["gsc_transferstatus"] = new OptionSetValue(100000001);
            _tracingService.Trace("Ending PopulateFields method...");

        }

        //Created By: Raphael Herrera, Created On: 8/3/2016
        /*Purpose: Create new allocated vehicle record. 
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_inventoryidtoallocate
         * Primary Entity: gsc_iv_vehicletransfer
         */
        public void AllocateVehicle(Entity vehicleTransfer)
        {
            _tracingService.Trace("Started AllocateVehicle method...");

            var inventoryId = vehicleTransfer.Contains("gsc_inventoryidtoallocate")
                ? vehicleTransfer.GetAttributeValue<string>("gsc_inventoryidtoallocate")
                : String.Empty;

            EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_status", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_productquantityid", "gsc_modelyear", "gsc_siteid", "gsc_productid", "gsc_basemodelid" });

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
                        null, OrderType.Ascending, new[] { "gsc_allocated", "gsc_available", "gsc_siteid", "gsc_productid", "gsc_vehiclemodelid", "gsc_vehiclecolorid" });

                    _tracingService.Trace("ProductQuantity records retrieved: " + productQuantityCollection.Entities.Count);
                    if (productQuantityCollection.Entities.Count > 0)
                    {
                        Entity productQuantityEntity = productQuantityCollection.Entities[0];

                        Int32 allocated = productQuantityEntity.Contains("gsc_allocated") ? productQuantityEntity.GetAttributeValue<Int32>("gsc_allocated")
                            : 0;
                        Int32 available = productQuantityEntity.Contains("gsc_available") ? productQuantityEntity.GetAttributeValue<Int32>("gsc_available")
                            : 0;

                        productQuantityEntity["gsc_allocated"] = allocated + 1;

                        if (available > 0)
                        {
                            productQuantityEntity["gsc_available"] = available - 1;
                        }                        

                        _organizationService.Update(productQuantityEntity);
                        _tracingService.Trace("Updated productquantity count...");
                        
                        #region Create VehicleAllocation Record

                        Entity transferredVehicleDetails = new Entity("gsc_iv_vehicletransferdetails");
                        var destinationSiteId = vehicleTransfer.Contains("gsc_siteid") ? vehicleTransfer.GetAttributeValue<EntityReference>("gsc_siteid").Id
                            : Guid.Empty;
                        var sourceSiteId = productQuantityEntity.Contains("gsc_siteid") ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                            : Guid.Empty;

                        transferredVehicleDetails["gsc_color"] = inventoryEntity.GetAttributeValue<String>("gsc_color");
                        transferredVehicleDetails["gsc_csno"] = inventoryEntity.GetAttributeValue<String>("gsc_csno");
                        transferredVehicleDetails["gsc_engineno"] = inventoryEntity.GetAttributeValue<String>("gsc_engineno");
                        transferredVehicleDetails["gsc_modelcode"] = inventoryEntity.GetAttributeValue<String>("gsc_modelcode");
                        transferredVehicleDetails["gsc_optioncode"] = inventoryEntity.GetAttributeValue<String>("gsc_optioncode");
                        transferredVehicleDetails["gsc_productionno"] = inventoryEntity.GetAttributeValue<String>("gsc_productionno");
                        transferredVehicleDetails["gsc_modelyear"] = inventoryEntity.GetAttributeValue<String>("gsc_modelyear");
                        transferredVehicleDetails["gsc_vin"] = inventoryEntity.GetAttributeValue<String>("gsc_vin");
                        transferredVehicleDetails["gsc_productid"] = productQuantityEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                        ? new EntityReference("product", productQuantityEntity.GetAttributeValue<EntityReference>("gsc_productid").Id)
                        : null;
                        transferredVehicleDetails["gsc_basemodel"] = productQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                        ? new EntityReference("gsc_iv_vehiclebasemodel", productQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id)
                        : null;
                        transferredVehicleDetails["gsc_inventoryid"] = new EntityReference(inventoryEntity.LogicalName, inventoryEntity.Id);
                        transferredVehicleDetails["gsc_vehicletransferid"] = new EntityReference(vehicleTransfer.LogicalName, vehicleTransfer.Id);
                        transferredVehicleDetails["gsc_destinationsiteid"] = new EntityReference("gsc_iv_site", destinationSiteId);
                        transferredVehicleDetails["gsc_sourcesiteid"] = new EntityReference("gsc_iv_site", sourceSiteId);
                        _organizationService.Create(transferredVehicleDetails);

                        _tracingService.Trace("Created vehicle allocation record...");

                        vehicleTransfer["gsc_inventoryidtoallocate"] = String.Empty;

                        _organizationService.Update(vehicleTransfer);

                        #endregion


                        //Create Inventory History Log
                        InventoryMovementHandler inventoryMovement = new InventoryMovementHandler(_organizationService, _tracingService);
                        inventoryMovement.CreateInventoryQuantityAllocated(vehicleTransfer, inventoryEntity, productQuantityEntity, vehicleTransfer.GetAttributeValue<string>("gsc_vehicletransferpn"),
                            DateTime.UtcNow, "Open", destinationSiteId, 100000001);
                    }

                    #endregion

                }

                else
                    throw new InvalidPluginExecutionException("!_The inventory for entered vehicle is not available.");
            }

            _tracingService.Trace("Ending AllocateVehicle method...");
        }

        //Created By: Raphael Herrera, Created On: 8/4/2016
        /*Purpose: Handle Delete BL for vehicle transfer record
         * Registration Details:
         * Event/Message: 
         *      Pre/Delete: VehicleTransfer
         * Primary Entity: gsc_iv_vehicletransfer
         */
        public void ValidateDelete(Entity vehicleTransfer)
        {
            _tracingService.Trace("Started ValidateDelete method...");
            //Unposted status
            if (vehicleTransfer.GetAttributeValue<OptionSetValue>("gsc_transferstatus").Value == 100000001)
            {
                _tracingService.Trace("Status is Unposted...");
                EntityCollection transferDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehicletransferdetails", "gsc_vehicletransferid", vehicleTransfer.Id, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_inventoryid" });

                _tracingService.Trace("Transfer Details records retrieved: " + transferDetailsCollection.Entities.Count);
                if (transferDetailsCollection.Entities.Count > 0)
                {
                    foreach (Entity transferDetails in transferDetailsCollection.Entities)
                    {
                        var inventoryId = transferDetails.Contains("gsc_inventoryid") ? transferDetails.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                           : Guid.Empty;

                        //Retrieve and update inventory
                        EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService,
                            null, OrderType.Ascending, new[] { "gsc_status", "gsc_productquantityid", "gsc_modelcode", "gsc_optioncode", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelyear", "gsc_productionno", "gsc_vin", "gsc_siteid", "gsc_productid", "gsc_basemodelid" });

                        _tracingService.Trace("Inventory records retrieved: " + inventoryCollection.Entities.Count);
                        if (inventoryCollection.Entities.Count > 0)
                        {
                            Entity inventory = inventoryCollection.Entities[0];

                            inventory["gsc_status"] = new OptionSetValue(100000000);

                            

                            var productQuantityId = inventory.Contains("gsc_productquantityid") ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                                : Guid.Empty;

                            //Retrieve and update product quantity
                            EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService,
                                null, OrderType.Ascending, new[] { "gsc_available", "gsc_allocated", "gsc_vehiclemodelid", "gsc_vehiclecolorid" });

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

                                _organizationService.Update(inventory);
                                _tracingService.Trace("Updated inventory record...");

                                //Delete Transfer Details
                                _organizationService.Delete(transferDetails.LogicalName, transferDetails.Id);


                                //Create inventory history log
                                InventoryMovementHandler inventoryMovement = new InventoryMovementHandler(_organizationService, _tracingService);
                                inventoryMovement.CreateInventoryQuantityAllocated(vehicleTransfer, inventory, productQuantity, vehicleTransfer.GetAttributeValue<string>("gsc_vehicletransferpn"),
                                    DateTime.UtcNow, "Deleted", vehicleTransfer.GetAttributeValue<EntityReference>("gsc_siteid").Id, 100000005);
                            }
                        }
                    }
                }
            }

            //Posted status
            else if (vehicleTransfer.GetAttributeValue<OptionSetValue>("gsc_transferstatus").Value == 100000000)
            {
                _tracingService.Trace("Status is posted...");
                throw new InvalidPluginExecutionException("!_Unable to delete Posted Vehicle Transfer transactions.");
            }
            _tracingService.Trace("Ending ValidateDelete method...");
        }

        //Created By: Raphael Herrera, Created On: 8/8/2016
        /*Purpose: BL for posting vehicletransfer transaction
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_transferstatus 
         * Primary Entity: gsc_iv_vehicletransfer
         */
        public void PostTransaction(Entity vehicleTransfer)
        {
            _tracingService.Trace("Started PostTransaction Method...");

            //Retrieve allocatedVehicle to retrieve inventoryid
            EntityCollection transferredVehicleDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehicletransferdetails", "gsc_vehicletransferid", vehicleTransfer.Id, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_inventoryid", "gsc_destinationsiteid", "gsc_sourcesiteid" });
            InventoryMovementHandler inventoryMovementHandler = new InventoryMovementHandler(_organizationService, _tracingService);
            _tracingService.Trace("AllocatedVehicle records retrieved: " + transferredVehicleDetailsCollection.Entities.Count);

            if (transferredVehicleDetailsCollection != null && transferredVehicleDetailsCollection.Entities.Count > 0)
            {
                foreach (Entity allocatedVehicle in transferredVehicleDetailsCollection.Entities)
                {
                    _tracingService.Trace("Running through allocatedvehicle record...");
                    var inventoryId = allocatedVehicle.Contains("gsc_inventoryid") ? allocatedVehicle.GetAttributeValue<EntityReference>("gsc_inventoryid").Id :
                        Guid.Empty;
                    Guid destinationSiteId = allocatedVehicle.Contains("gsc_destinationsiteid") ? allocatedVehicle.GetAttributeValue<EntityReference>("gsc_destinationsiteid").Id :
                        Guid.Empty;
                    String destinationSiteName = allocatedVehicle.Contains("gsc_destinationsiteid") ? allocatedVehicle.GetAttributeValue<EntityReference>("gsc_destinationsiteid").Name :
                        String.Empty;
                    String transactionNumber = vehicleTransfer.Contains("gsc_vehicletransferpn") ? vehicleTransfer.GetAttributeValue<String>("gsc_vehicletransferpn") : string.Empty;
                    DateTime transactionDate = DateTime.UtcNow;
                    Guid fromSite = allocatedVehicle.Contains("gsc_sourcesiteid") ? allocatedVehicle.GetAttributeValue<EntityReference>("gsc_sourcesiteid").Id : Guid.Empty;
                     
                    //Retrieve inventory to update status
                    EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService,
                        null, OrderType.Ascending, new[] { "gsc_status", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_productquantityid", "gsc_modelyear", "gsc_siteid", "gsc_productid", "gsc_basemodelid" });
                    _tracingService.Trace("Inventory records retrieved: " + inventoryCollection.Entities.Count);

                    if (inventoryCollection != null && inventoryCollection.Entities.Count > 0)
                    {
                        var productQuantityId = inventoryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_productquantityid").Id;

                        //Retrieve ProductQuantity where inventory came from to be updated
                        EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService,
                            null, OrderType.Ascending, new[] { "gsc_allocated", "gsc_onhand", "gsc_productid", "gsc_vehiclecolorid", "gsc_vehiclemodelid", "gsc_siteid" });
                        _tracingService.Trace("ProductQuantity records retrieved: " + productQuantityCollection.Entities.Count);

                        if (productQuantityCollection != null && productQuantityCollection.Entities.Count > 0)
                        {
                            Entity productQuantity = productQuantityCollection.Entities[0];
                            //Adjustment of 'site from'
                            Int32 allocated = productQuantity.GetAttributeValue<Int32>("gsc_allocated");
                            Int32 onHand = productQuantity.GetAttributeValue<Int32>("gsc_onhand");

                            productQuantity["gsc_allocated"] = allocated - 1;
                            productQuantity["gsc_onhand"] = onHand - 1;
                            _organizationService.Update(productQuantity);
                            _tracingService.Trace("Updated productquantity from site record...");

                            //Log inventory history upon decreasing onhand value in source site
                            inventoryMovementHandler.CreateInventoryHistory("Vehicle Transfer", string.Empty, string.Empty, transactionNumber, transactionDate, 1, 1, onHand - 1,destinationSiteId, fromSite, fromSite, inventoryCollection.Entities[0], productQuantity, true,false);

                            Guid productId = productQuantity.Contains("gsc_productid") ? productQuantity.GetAttributeValue<EntityReference>("gsc_productid").Id : Guid.Empty;
                            Guid colorId = productQuantity.Contains("gsc_vehiclecolorid") ? productQuantity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id : Guid.Empty;
                            Guid baseModel = productQuantity.Contains("gsc_vehiclemodelid") ? productQuantity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id : Guid.Empty;
                            String productName = productQuantity.Contains("gsc_productid") ? productQuantity.GetAttributeValue<EntityReference>("gsc_productid").Name : String.Empty;
                            
                            var destinationConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_siteid", ConditionOperator.Equal, destinationSiteId),
                                new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                                new ConditionExpression("gsc_vehiclecolorid", ConditionOperator.Equal, colorId)
                            };

                            //Retrieve productquantity of destination site to be updated
                            EntityCollection productQuantityDestinationCollection = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", destinationConditionList, _organizationService,
                                null, OrderType.Ascending, new[] { "gsc_allocated", "gsc_onhand" });
                            _tracingService.Trace("ProductQuantity(Destination) records retrieved: " + productQuantityDestinationCollection.Entities.Count);

                            Entity inventory = new Entity("gsc_iv_inventory");
                            Entity productQuantityDestination = new Entity("gsc_iv_productquantity");
                            Int32 onHandCount = 1;
                            if (productQuantityDestinationCollection != null && productQuantityDestinationCollection.Entities.Count > 0)
                            {
                                //Adjustment of destination site
                                productQuantityDestination = productQuantityDestinationCollection.Entities[0];
                                Int32 availableDestination = productQuantityDestination.GetAttributeValue<Int32>("gsc_available");
                                Int32 onHandDestination = productQuantityDestination.GetAttributeValue<Int32>("gsc_onhand");

                                productQuantityDestination["gsc_available"] = availableDestination + 1;
                                productQuantityDestination["gsc_onhand"] = onHandDestination + 1;
                                _organizationService.Update(productQuantityDestination);
                                _tracingService.Trace("Updated productquantity destination record...");

                                //Update of inventory status
                                inventory = inventoryCollection.Entities[0];
                                inventory["gsc_status"] = new OptionSetValue(100000000);
                                inventory["gsc_productquantityid"] = new EntityReference(productQuantityDestination.LogicalName, productQuantityDestination.Id);
                                _organizationService.Update(inventory);
                                _tracingService.Trace("Updated inventory status to available...");

                                onHandCount = onHandDestination + 1;
                            }
                            else
                            {
                                //Create productQuantity
                                Entity prodQuantity = new Entity("gsc_iv_productquantity");
                                _tracingService.Trace("Set product quantity count");
                                prodQuantity["gsc_onhand"] = 1;
                                prodQuantity["gsc_available"] = 1;
                                prodQuantity["gsc_allocated"] = 0;
                                prodQuantity["gsc_onorder"] = 0;
                                prodQuantity["gsc_sold"] = 0;

                                _tracingService.Trace("Set site field");
                                if (destinationSiteId != Guid.Empty)
                                {
                                    prodQuantity["gsc_siteid"] = new EntityReference("gsc_iv_site", destinationSiteId);
                                }
                                _tracingService.Trace("Set Vehicle Base Model field");
                                if (baseModel != Guid.Empty)
                                {
                                    prodQuantity["gsc_vehiclemodelid"] = new EntityReference("gsc_iv_vehiclebasemodel", baseModel);
                                }

                                if (colorId != Guid.Empty)
                                {
                                    prodQuantity["gsc_vehiclecolorid"] = new EntityReference("gsc_cmn_vehiclecolor", colorId);
                                }
                                _tracingService.Trace("Set Product Name field");
                                prodQuantity["gsc_productid"] = new EntityReference("product", productId);
                                prodQuantity["gsc_productquantitypn"] = productName + "-" + destinationSiteName;

                                Guid newProductQuantityId = _organizationService.Create(prodQuantity);

                                EntityCollection productQuantityEC = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", newProductQuantityId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_vehiclecolorid", "gsc_vehiclemodelid", "gsc_productid" });
                                productQuantityDestination = productQuantityEC.Entities[0];
                                
                                //Update of inventory status
                                inventory = inventoryCollection.Entities[0];
                                inventory["gsc_status"] = new OptionSetValue(100000000);
                                inventory["gsc_productquantityid"] = new EntityReference(prodQuantity.LogicalName, newProductQuantityId);
                                _organizationService.Update(inventory);
                                _tracingService.Trace("Updated inventory status to available...");
                            }

                            inventoryMovementHandler.CreateInventoryHistory("Vehicle Transfer", string.Empty, string.Empty, transactionNumber, transactionDate, 1, 1, onHandCount, destinationSiteId,fromSite, destinationSiteId, inventory, productQuantityDestination, true,true);
                            inventoryMovementHandler.CreateInventoryQuantityAllocated(vehicleTransfer, inventory, productQuantity, vehicleTransfer.GetAttributeValue<string>("gsc_vehicletransferpn"),
                               DateTime.UtcNow, "Posted", destinationSiteId, 100000008);
                        }
                    }
                }

            }
            else
            {
                throw new InvalidPluginExecutionException("!_Please select first vehicle to transfer");
            }
            _tracingService.Trace("Ending PostTransasction method...");
        }

        //Created By: Raphael Herrera, Created On: 2/14/2017
        /*Purpose: BL for canceling vehicletransfer transaction
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_transferstatus to Cancelled
         * Primary Entity: gsc_iv_vehicletransfer
         */
        public void CancelTransaction(Entity vehicleTransfer)
        {
            if (vehicleTransfer.FormattedValues["gsc_transferstatus"] != "Cancelled")
                return;
            _tracingService.Trace("Starting CancelTransaction Method...");
            EntityCollection transferDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehicletransferdetails", "gsc_vehicletransferid", vehicleTransfer.Id, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_inventoryid" });

            _tracingService.Trace("Transfer Details records retrieved: " + transferDetailsCollection.Entities.Count);
            if (transferDetailsCollection.Entities.Count > 0)
            {
                foreach (Entity transferDetails in transferDetailsCollection.Entities)
                {
                    var inventoryId = transferDetails.Contains("gsc_inventoryid") ? transferDetails.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                       : Guid.Empty;

                    //Retrieve and update inventory
                    EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService,
                        null, OrderType.Ascending, new[] { "gsc_status", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_productquantityid", "gsc_modelyear", "gsc_siteid", "gsc_productid", "gsc_basemodelid" });

                    _tracingService.Trace("Inventory records retrieved: " + inventoryCollection.Entities.Count);
                    if (inventoryCollection.Entities.Count > 0)
                    {
                        Entity inventory = inventoryCollection.Entities[0];

                        inventory["gsc_status"] = new OptionSetValue(100000000);



                        var productQuantityId = inventory.Contains("gsc_productquantityid") ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                            : Guid.Empty;

                        //Retrieve and update product quantity
                        EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService,
                            null, OrderType.Ascending, new[] { "gsc_available", "gsc_allocated", "gsc_vehiclecolorid", "gsc_vehiclemodelid" });

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

                            _organizationService.Update(inventory);
                            _tracingService.Trace("Updated inventory record...");

                            //Create Inventory History Log
                            InventoryMovementHandler inventoryMovement = new InventoryMovementHandler(_organizationService, _tracingService);
                            inventoryMovement.CreateInventoryQuantityAllocated(vehicleTransfer, inventory, productQuantity, vehicleTransfer.GetAttributeValue<string>("gsc_vehicletransferpn"),
                                DateTime.UtcNow, "Cancelled", vehicleTransfer.GetAttributeValue<EntityReference>("gsc_siteid").Id, 100000004);
                        }
                    }
                }
            }
            _tracingService.Trace("Ending CancelTransaction Method...");
        }

        //Created By: Jessica Casupanan, Created On: 2/10/2017
        /*Purpose: Update Status Copy
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_transferstatus 
         * Primary Entity: gsc_iv_vehicletransfer
         */
        public void UpdateVTSatus(Entity vehicleTransfer)
        {
            _tracingService.Trace("Started UpdateVTSatus method...");

            var status = vehicleTransfer.Contains("gsc_transferstatus") ? vehicleTransfer.GetAttributeValue<OptionSetValue>("gsc_transferstatus") : null;

            Entity statusUpdate = _organizationService.Retrieve(vehicleTransfer.LogicalName, vehicleTransfer.Id,
                new ColumnSet("gsc_transferstatuscopy"));

            statusUpdate["gsc_transferstatuscopy"] = status; 

            _organizationService.Update(statusUpdate);
            _tracingService.Trace("Ending UpdateVTSatus method...");
        }

        //Created By: Jerome Anthony Gerero, Created On: 2/15/2017
        /*Purpose: Delete selected allocated vehicle
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_allocateditemstodelete
         * Primary Entity: Vehicle Transfer
         */
        public Entity DeleteAllocatedVehicle(Entity vehicleTransferEntity)
        {
            _tracingService.Trace("Started DeleteAllocatedVehicle method...");

            Guid vehicleTransferDetailId = vehicleTransferEntity.Contains("gsc_allocateditemstodelete")
                ? new Guid(vehicleTransferEntity.GetAttributeValue<String>("gsc_allocateditemstodelete"))
                : Guid.Empty;

            if (vehicleTransferDetailId == Guid.Empty) { return null; }

            //Retrieve allocated vehicle record
            EntityCollection vehicleTransferDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehicletransferdetails", "gsc_iv_vehicletransferdetailsid", vehicleTransferDetailId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid" });

            if (vehicleTransferDetailRecords != null && vehicleTransferDetailRecords.Entities.Count > 0)
            {
                Entity vehicleTransferDetail = vehicleTransferDetailRecords.Entities[0];

                Guid inventoryId = vehicleTransferDetail.Contains("gsc_inventoryid")
                    ? vehicleTransferDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                    : Guid.Empty;

                //Retrieve inventory record
                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_status", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_productquantityid", "gsc_modelyear", "gsc_siteid", "gsc_productid", "gsc_basemodelid" });
                
                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    Entity inventory = inventoryRecords.Entities[0];

                    inventory["gsc_status"] = new OptionSetValue(100000000);

                    _organizationService.Update(inventory);

                    InventoryMovementHandler inventoryMovementHandler = new InventoryMovementHandler(_organizationService, _tracingService);

                    Entity productQuantity = inventoryMovementHandler.UpdateProductQuantity(inventory, 0, 1, -1, 0, 0, 0, 0, 0);
                    inventoryMovementHandler.CreateInventoryQuantityAllocated(vehicleTransferEntity, inventory, productQuantity, vehicleTransferEntity.GetAttributeValue<string>("gsc_vehicletransferpn"),
                                DateTime.UtcNow, "Cancelled", vehicleTransferEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id, 100000003);


                }

                _organizationService.Delete("gsc_iv_vehicletransferdetails", vehicleTransferDetail.Id);
            }            

            vehicleTransferEntity["gsc_allocateditemstodelete"] = null;

            _organizationService.Update(vehicleTransferEntity);

            _tracingService.Trace("Ended DeleteAllocatedVehicle method...");
            return vehicleTransferEntity;
        }

        //Created By: Jessica Casupanan, Created On: 02/16/2017
        public Boolean RestrictPosting(Entity VehicleTransfer)
        {
            _tracingService.Trace("Started RestrictPosting method...");
            EntityCollection transferredDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehicletransferdetails", "gsc_vehicletransferid", VehicleTransfer.Id,
                _organizationService, null, OrderType.Ascending, "gsc_sourcesiteid");

            if (transferredDetailsCollection != null && transferredDetailsCollection.Entities.Count > 0)
            {
               
                foreach (Entity transferredDetails in transferredDetailsCollection.Entities)
                {
                    Guid siteId = transferredDetails.Contains("gsc_sourcesiteid") ? transferredDetails.GetAttributeValue<EntityReference>("gsc_sourcesiteid").Id
                        : Guid.Empty;

                    EntityCollection siteCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_site", "gsc_iv_siteid", siteId,
                    _organizationService, null, OrderType.Ascending, "gsc_sellsite");

                    if (siteCollection != null && siteCollection.Entities.Count > 0)
                    {
                      Boolean sellSite = siteCollection.Entities[0].GetAttributeValue<Boolean>("gsc_sellsite");
                        if(sellSite == false)
                        {
                            Entity VehicleTransferToUpdate = _organizationService.Retrieve(VehicleTransfer.LogicalName, VehicleTransfer.Id,
                            new ColumnSet("gsc_transferstatus"));
                            VehicleTransferToUpdate["gsc_transferstatus"] = new OptionSetValue(100000001);
                            _organizationService.Update(VehicleTransferToUpdate);
                            return true;
                        }
                    }
                }
            }
            _tracingService.Trace("Ended RestrictPosting method...");
            return false;
        }

        public Entity UpdateDestinationSite(Entity vehicleTransfer)
        {
            var site = vehicleTransfer.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? vehicleTransfer.GetAttributeValue<EntityReference>("gsc_siteid")
                : null;

            EntityCollection detailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehicletransferdetails", "gsc_vehicletransferid", vehicleTransfer.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_destinationsiteid"});


            if (detailsCollection.Entities.Count > 0)
            {
                foreach(Entity transferDetail in detailsCollection.Entities)
                {
                    transferDetail["gsc_destinationsiteid"] = site;

                    _organizationService.Update(transferDetail);
                }
            }

            return vehicleTransfer;
        }
    }
}