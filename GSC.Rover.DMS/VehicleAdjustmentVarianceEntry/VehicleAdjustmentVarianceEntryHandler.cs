using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.InventoryMovement;

namespace GSC.Rover.DMS.BusinessLogic.VehicleAdjustmentVarianceEntry
{
    public class VehicleAdjustmentVarianceEntryHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleAdjustmentVarianceEntryHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 8/25/2016
        //Modified By : Jessica Casupanan, Modified On : 01/09/2017
        /*Purpose: Create new allocated vehicle record
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: Inventory Id to Allocate = gsc_inventoryidtoallocate
         *      Post/Create:
         * Primary Entity: Vehicle Adjustment/Variance Entry
         */
        public Entity CreateVehicleAdjustmentVarianceEntryDetailRecord(Entity vehicleAdjustmentVarianceEntryEntity)
        {
            _tracingService.Trace("Started CreateVehicleAdjustmentVarianceEntryDetailRecord Method...");

            //Return if Inventory ID to Allocate is null
            if (vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<String>("gsc_inventoryidtoallocate") == null)
            {
                _tracingService.Trace("Inventory ID to Allocate is null.. exiting.");
                return null;
            }

            Guid inventoryId = vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<String>("gsc_inventoryidtoallocate") != null
                ? new Guid(vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<String>("gsc_inventoryidtoallocate"))
                : Guid.Empty;

            EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_status", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_modelyear", "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_productquantityid", "gsc_siteid", "gsc_productid", "gsc_basemodelid" });

            if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
            {
                Entity inventory = inventoryRecords.Entities[0];

                if (!inventory.FormattedValues["gsc_status"].Equals("Available"))
                {
                    throw new InvalidPluginExecutionException("The inventory for entered vehicle is not available.");
                }                

                Entity vehicleAdjustmentVarianceEntryDetail = new Entity("gsc_sls_adjustmentvariancedetail");

                vehicleAdjustmentVarianceEntryDetail["gsc_vehicleadjustmentvarianceentryid"] = new EntityReference(vehicleAdjustmentVarianceEntryEntity.LogicalName, vehicleAdjustmentVarianceEntryEntity.Id);
                vehicleAdjustmentVarianceEntryDetail["gsc_inventoryid"] = new EntityReference(inventory.LogicalName, inventory.Id);
                vehicleAdjustmentVarianceEntryDetail["gsc_modelcode"] = inventory.Contains("gsc_modelcode")
                    ? inventory.GetAttributeValue<String>("gsc_modelcode")
                    : String.Empty;
                vehicleAdjustmentVarianceEntryDetail["gsc_modelyear"] = inventory.Contains("gsc_modelyear")
                    ? inventory.GetAttributeValue<String>("gsc_modelyear")
                    : String.Empty;
                vehicleAdjustmentVarianceEntryDetail["gsc_optioncode"] = inventory.Contains("gsc_optioncode")
                    ? inventory.GetAttributeValue<String>("gsc_optioncode")
                    : String.Empty;
                String color = inventory.Contains("gsc_color")
                    ? inventory.GetAttributeValue<String>("gsc_color")
                    : String.Empty;

                //Retrieve Vehicle Color
                EntityCollection vehicleColorRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_vehiclecolor", "gsc_vehiclecolorpn", color, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_cmn_vehiclecolorid" });

                if (vehicleColorRecords != null && vehicleColorRecords.Entities.Count > 0)
                {
                    Entity vehicleColor = vehicleColorRecords.Entities[0];

                    vehicleAdjustmentVarianceEntryDetail["gsc_vehiclecolorid"] = new EntityReference(vehicleColor.LogicalName, vehicleColor.Id);
                }

                vehicleAdjustmentVarianceEntryDetail["gsc_csno"] = inventory.Contains("gsc_csno")
                    ? inventory.GetAttributeValue<String>("gsc_csno")
                    : String.Empty;
                vehicleAdjustmentVarianceEntryDetail["gsc_vin"] = inventory.Contains("gsc_vin")
                    ? inventory.GetAttributeValue<String>("gsc_vin")
                    : String.Empty;
                vehicleAdjustmentVarianceEntryDetail["gsc_productionno"] = inventory.Contains("gsc_productionno")
                    ? inventory.GetAttributeValue<String>("gsc_productionno")
                    : String.Empty;
                vehicleAdjustmentVarianceEntryDetail["gsc_engineno"] = inventory.Contains("gsc_engineno")
                    ? inventory.GetAttributeValue<String>("gsc_engineno")
                    : String.Empty;
                vehicleAdjustmentVarianceEntryDetail["gsc_quantity"] = -1;
                vehicleAdjustmentVarianceEntryDetail["gsc_operation"] = new OptionSetValue(100000001);
                vehicleAdjustmentVarianceEntryDetail["gsc_recordownerid"] = vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                    ? new EntityReference("contact", vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                    : null;
                vehicleAdjustmentVarianceEntryDetail["gsc_dealerid"] = vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                    ? new EntityReference("account", vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                    : null;
                vehicleAdjustmentVarianceEntryDetail["gsc_branchid"] = vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                    ? new EntityReference("account", vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                    : null;

                Guid productQuantityId = inventory.Contains("gsc_productquantityid")
                    ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                    : Guid.Empty;

                //Retrieve Product Quantity record for additional fields
                EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_siteid", "gsc_productid", "gsc_vehiclemodelid", "gsc_allocated", "gsc_available", "gsc_vehiclemodelid" });

                Entity productQuantity = new Entity("gsc_iv_productquantity");
                if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                {
                    productQuantity = productQuantityRecords.Entities[0];

                    vehicleAdjustmentVarianceEntryDetail["gsc_vehiclebasemodelid"] = productQuantity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                        ? new EntityReference("gsc_iv_vehiclebasemodel", productQuantity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id)
                        : null;
                    vehicleAdjustmentVarianceEntryDetail["gsc_productid"] = productQuantity.GetAttributeValue<EntityReference>("gsc_productid") != null
                        ? new EntityReference("product", productQuantity.GetAttributeValue<EntityReference>("gsc_productid").Id)
                        : null;
                    vehicleAdjustmentVarianceEntryDetail["gsc_siteid"] = productQuantity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                        ? new EntityReference("gsc_iv_site", productQuantity.GetAttributeValue<EntityReference>("gsc_siteid").Id)
                        : null;

                    Int32 allocatedCount = productQuantity.Contains("gsc_allocated")
                        ? productQuantity.GetAttributeValue<Int32>("gsc_allocated")
                        : 0;
                    Int32 availableCount = productQuantity.Contains("gsc_available")
                        ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                        : 1;

                        productQuantity["gsc_allocated"] = allocatedCount + 1;

                        if (availableCount != 0)
                        {
                            productQuantity["gsc_available"] = availableCount - 1;
                        }

                       // throw new InvalidPluginExecutionException("test" + (allocatedCount + 1).ToString() + " " + (availableCount - 1).ToString());
                        _organizationService.Update(productQuantity);                   
                }               

                _organizationService.Create(vehicleAdjustmentVarianceEntryDetail);

                //Set Inventory status to 'Allocated'
                inventory["gsc_status"] = new OptionSetValue(100000001);

                _organizationService.Update(inventory);

                InventoryMovementHandler inventoryMovement = new InventoryMovementHandler(_organizationService, _tracingService);
                inventoryMovement.CreateInventoryQuantityAllocated(vehicleAdjustmentVarianceEntryEntity, inventory, productQuantity, vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<string>("gsc_vehicleadjustmentvarianceentrypn"),
                    DateTime.UtcNow, vehicleAdjustmentVarianceEntryEntity.FormattedValues["gsc_adjustmentvariancestatus"], Guid.Empty, 100000001);
            }
            
            //Clear Inventory ID to Allocate field
            vehicleAdjustmentVarianceEntryEntity["gsc_inventoryidtoallocate"] = String.Empty;
            _organizationService.Update(vehicleAdjustmentVarianceEntryEntity);

            _tracingService.Trace("Ended CreateVehicleAdjustmentVarianceEntryDetailRecord Method...");

            

            return vehicleAdjustmentVarianceEntryEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 8/31/2016
        /*Purpose: Update Inventory record fields on Vehicle Adjustment/Variance Entry record delete
         * Registration Details: 
         * Event/Message:
         *      Pre-Validate/Delete: gsc_sls_adjustmentvarianceid
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Vehicle Adjustment/Variance Entry Detail
         */
        public Entity AdjustInventoryOnUnpostedDelete(Entity vehicleAdjustmentVarianceEntryEntity)
        {
            _tracingService.Trace("Started AdjustInventoryOnUnpostedDelete Method...");

            if (vehicleAdjustmentVarianceEntryEntity.FormattedValues["gsc_adjustmentvariancestatus"].Equals("Posted"))
            {
                throw new InvalidPluginExecutionException("Unable to delete already posted Vehicle Adjustment/Variance Entry");
            }
            else if (vehicleAdjustmentVarianceEntryEntity.FormattedValues["gsc_adjustmentvariancestatus"].Equals("Cancelled"))
            {
                throw new InvalidPluginExecutionException("Unable to delete cancelled Vehicle Adjustment/Variance Entry");
            }

            //Retrieve Vehicle Adjustment/Variance Entry Detail records
            EntityCollection vehicleAdjustmentVarianceEntryDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_adjustmentvariancedetail", "gsc_vehicleadjustmentvarianceentryid", vehicleAdjustmentVarianceEntryEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid" });

            if (vehicleAdjustmentVarianceEntryDetailRecords != null && vehicleAdjustmentVarianceEntryDetailRecords.Entities.Count > 0)
            {
                foreach (Entity vehicleAdjustmentVarianceEntryDetail in vehicleAdjustmentVarianceEntryDetailRecords.Entities)
                {
                    Guid inventoryId = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                        ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                        : Guid.Empty;

                    //Retrieve Inventory records
                    EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_status", "gsc_productquantityid", "gsc_optioncode", "gsc_productid", "gsc_modelcode", "gsc_modelyear", "gsc_siteid",
                            "gsc_vin", "gsc_csno", "gsc_productionno", "gsc_engineno" });

                    if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                    {
                        Entity inventory = inventoryRecords.Entities[0];

                        inventory["gsc_status"] = new OptionSetValue(100000000);

                        _organizationService.Update(inventory);

                        Guid productQuantityId = inventory.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                            ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                            : Guid.Empty;

                        //Retrieve Product Quantity records
                        EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_allocated", "gsc_available", "gsc_vehiclecolorid", "gsc_vehiclemodelid" });

                        if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                        {
                            Entity productQuantity = productQuantityRecords.Entities[0];

                            Int32 allocatedCount = productQuantity.Contains("gsc_allocated")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_allocated")
                                : 1;
                            Int32 availableCount = productQuantity.Contains("gsc_available")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                                : 0;

                            if (allocatedCount != 0)
                            {
                                productQuantity["gsc_allocated"] = allocatedCount - 1;
                            }
                            productQuantity["gsc_available"] = availableCount + 1;

                            _organizationService.Update(productQuantity);


                            //Create inventory history log
                            InventoryMovementHandler inventoryMovement = new InventoryMovementHandler(_organizationService, _tracingService);
                            inventoryMovement.CreateInventoryQuantityAllocated(vehicleAdjustmentVarianceEntryEntity, inventory, productQuantity, vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<string>("gsc_vehicleadjustmentvarianceentrypn"),
                                DateTime.UtcNow, "Deleted", Guid.Empty, 100000005);
                        }
                    }
                    _organizationService.Delete("gsc_sls_adjustmentvariancedetail", vehicleAdjustmentVarianceEntryDetail.Id);
                }
            }

            _tracingService.Trace("Ended AdjustInventoryOnUnpostedDelete Method...");
            return vehicleAdjustmentVarianceEntryEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 9/2/2016
        //Modified By : Jessica Casupanan, Modified On : 01/10/2017
        /*Purpose: Update Inventory record fields on Vehicle Adjustment/Variance Entry 'Posted' status
         * Registration Details: 
         * Event/Message:
         *      Pre-Validate/Delete:
         *      Post/Update: gsc_adjustmentvariancestatus
         *      Post/Create:
         * Primary Entity: Vehicle Adjustment/Variance Entry Detail
         */
        public Entity PostVehicleAdjustmentVarianceEntry(Entity vehicleAdjustmentVarianceEntryEntity)
        {
            _tracingService.Trace("Started PostVehicleAdjustmentVarianceEntry Method...");

            if (!vehicleAdjustmentVarianceEntryEntity.FormattedValues["gsc_adjustmentvariancestatus"].Equals("Posted")) { return null; }

            String transactionNumber = vehicleAdjustmentVarianceEntryEntity.Contains("gsc_vehicleadjustmentvarianceentrypn") ? vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<String>("gsc_vehicleadjustmentvarianceentrypn") : String.Empty;
            DateTime transactionDate = DateTime.UtcNow;
            InventoryMovementHandler inventoryMovementHandler = new InventoryMovementHandler(_organizationService, _tracingService);

            _tracingService.Trace("Retrieve Vehicle Adjustment/Variance Entry Detail records");
            EntityCollection vehicleAdjustmentVarianceEntryDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_adjustmentvariancedetail", "gsc_vehicleadjustmentvarianceentryid", vehicleAdjustmentVarianceEntryEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_vehiclebasemodelid", "gsc_vehiclecolorid", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_productid", "gsc_modelyear", "gsc_optioncode", "gsc_productionno", "gsc_siteid", "gsc_vin", "gsc_operation", "gsc_inventoryid", "statecode", "gsc_quantity" });

            if (vehicleAdjustmentVarianceEntryDetailRecords != null && vehicleAdjustmentVarianceEntryDetailRecords.Entities.Count > 0)
            {
                foreach (Entity vehicleAdjustmentVarianceEntryDetail in vehicleAdjustmentVarianceEntryDetailRecords.Entities)
                {
                    Int32 quantity = vehicleAdjustmentVarianceEntryDetail.Contains("gsc_quantity") 
                         ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<Int32>("gsc_quantity") : 0;

                    #region Subtract
                    if (vehicleAdjustmentVarianceEntryDetail.FormattedValues["gsc_operation"].Equals("Subtract"))
                    {
                        Guid inventoryId = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                           ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                           : Guid.Empty;
               
                        _tracingService.Trace("Retrieve Inventory records using value from Inventory field");
                        EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_productquantityid", "gsc_optioncode", "gsc_productid", "gsc_modelcode", "gsc_modelyear", "gsc_siteid",
                            "gsc_vin", "gsc_csno", "gsc_productionno", "gsc_engineno"});

                        if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                        {
                            Entity inventory = inventoryRecords.Entities[0];

                            Guid productQuantityId = inventory.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                                ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                                : Guid.Empty;

                            _tracingService.Trace("Retrieve Product Quantity record using value from Product Quantity field");
                            EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_allocated", "gsc_onhand", "gsc_available", "gsc_siteid", "gsc_vehiclecolorid", "gsc_vehiclemodelid", "gsc_productid"});

                            if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                            {
                                Entity productQuantity = productQuantityRecords.Entities[0];

                                Int32 allocatedCount = productQuantity.Contains("gsc_allocated")
                                    ? productQuantity.GetAttributeValue<Int32>("gsc_allocated")
                                    : 1;
                                Int32 availableCount = productQuantity.Contains("gsc_available")
                                    ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                                    : 0;
                                Int32 onHandCount = productQuantity.Contains("gsc_onhand")
                                    ? productQuantity.GetAttributeValue<Int32>("gsc_onhand")
                                    : 1;

                                _tracingService.Trace("Adjust Allocated count");
                                if (allocatedCount != 0)
                                {
                                    productQuantity["gsc_allocated"] = allocatedCount - 1;
                                }
                                _tracingService.Trace("Adjust On Hand count");
                                if (onHandCount != 0)
                                {
                                    productQuantity["gsc_onhand"] = onHandCount - 1;
                                }
                                
                                _organizationService.Update(productQuantity);
                                Guid fromSite = productQuantity.Contains("gsc_siteid") ? productQuantity.GetAttributeValue<EntityReference>("gsc_siteid").Id : Guid.Empty; 
                                inventoryMovementHandler.CreateInventoryHistory("Negative Adjustment", null, null, transactionNumber, transactionDate, 1, 0, onHandCount - 1, Guid.Empty,fromSite,fromSite, inventory, productQuantity, true,true);
                                inventoryMovementHandler.CreateInventoryQuantityAllocated(vehicleAdjustmentVarianceEntryEntity, inventory, productQuantity, vehicleAdjustmentVarianceEntryEntity.GetAttributeValue<string>("gsc_vehicleadjustmentvarianceentrypn"),
                                    DateTime.UtcNow, "Posted", Guid.Empty, 100000008);

                                _tracingService.Trace("Deactivate record");
                                if (vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
                                {
                                    SetStateRequest setStateRequest = new SetStateRequest()
                                    {
                                        EntityMoniker = new EntityReference
                                        {
                                            Id = inventory.Id,
                                            LogicalName = inventory.LogicalName,
                                        },
                                        State = new OptionSetValue(1),
                                        Status = new OptionSetValue(2)
                                    };
                                    _organizationService.Execute(setStateRequest);
                                }
                            }
                        }
                    }
                    #endregion

                    #region Add
                    else if (vehicleAdjustmentVarianceEntryDetail.FormattedValues["gsc_operation"].Equals("Add"))
                    {
                        _tracingService.Trace("Get Vehicle Adjustment/Variance Entry Detail fields");
                        Guid vehicleBaseModelId = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id
                            : Guid.Empty;
                        String color = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Name
                            : String.Empty;
                        Guid colorId = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id
                            : Guid.Empty;
                        String csNo = vehicleAdjustmentVarianceEntryDetail.Contains("gsc_csno")
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<String>("gsc_csno")
                            : String.Empty;
                        String engineNo = vehicleAdjustmentVarianceEntryDetail.Contains("gsc_engineno")
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<String>("gsc_engineno")
                            : String.Empty;
                        String modelCode = vehicleAdjustmentVarianceEntryDetail.Contains("gsc_modelcode")
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<String>("gsc_modelcode")
                            : String.Empty;
                        Guid productId = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_productid").Id
                            : Guid.Empty;
                        String productName = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_productid").Name
                            : String.Empty;
                        String modelYear = vehicleAdjustmentVarianceEntryDetail.Contains("gsc_modelyear")
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<String>("gsc_modelyear")
                            : String.Empty;
                        String optionCode = vehicleAdjustmentVarianceEntryDetail.Contains("gsc_optioncode")
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<String>("gsc_optioncode")
                            : String.Empty;
                        String productionNo = vehicleAdjustmentVarianceEntryDetail.Contains("gsc_productionno")
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<String>("gsc_productionno")
                            : String.Empty;
                        Guid siteId = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_siteid") != null
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_siteid").Id
                            : Guid.Empty;
                        String siteName = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_siteid") != null
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_siteid").Name
                            : String.Empty;
                        String vin = vehicleAdjustmentVarianceEntryDetail.Contains("gsc_vin")
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<String>("gsc_vin")
                            : String.Empty;

                        _tracingService.Trace("Create filter for Product in Product Relationship entity");
                        var productQuantityConditionList = new List<ConditionExpression>
                        {
                            new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                            new ConditionExpression("gsc_siteid", ConditionOperator.Equal, siteId),
                            new ConditionExpression("gsc_vehiclecolorid", ConditionOperator.Equal, colorId)
                        };

                        _tracingService.Trace("Retrieve Product Quantity records");
                        EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityConditionList, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_onhand", "gsc_available", "gsc_siteid", "gsc_vehiclecolorid", "gsc_vehiclemodelid", "gsc_productid" });

                        Entity productQuantity;
                        Entity inventory = new Entity("gsc_iv_inventory");

                        Int32 onHandCount = 0;
                        Int32 availableCount;

                        if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                        {
                            _tracingService.Trace("Update existing product quantity record");
                            productQuantity = productQuantityRecords.Entities[0];

                            onHandCount = productQuantity.Contains("gsc_onhand")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_onhand")
                                : 0;
                            availableCount = productQuantity.Contains("gsc_available")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                                : 0;

                            _tracingService.Trace("Set product quantity count");
                            productQuantity["gsc_onhand"] = onHandCount + 1;
                            productQuantity["gsc_available"] = availableCount + 1;

                            _organizationService.Update(productQuantity);

                            inventory["gsc_productquantityid"] = new EntityReference("gsc_iv_productquantity", productQuantity.Id);
                        }
                        else
                        {
                            _tracingService.Trace("Create new product quantity product");
                            productQuantity = new Entity("gsc_iv_productquantity");                            

                            _tracingService.Trace("Set product quantity count");
                            productQuantity["gsc_onhand"] = 1;
                            productQuantity["gsc_available"] = 1;

                            _tracingService.Trace("Set site field");
                            if (siteId != Guid.Empty)
                            { 
                                productQuantity["gsc_siteid"] = new EntityReference("gsc_iv_site", siteId); 
                            }
                            _tracingService.Trace("Set Vehicle Base Model field");
                            if (vehicleBaseModelId != Guid.Empty)
                            { 
                                productQuantity["gsc_vehiclemodelid"] = new EntityReference("gsc_iv_vehiclebasemodel", vehicleBaseModelId); 
                            }

                            if (colorId != Guid.Empty)
                            {
                                productQuantity["gsc_vehiclecolorid"] = new EntityReference("gsc_cmn_vehiclecolor", colorId); 
                            }
                            _tracingService.Trace("Set Product Name field");
                            productQuantity["gsc_productid"] = new EntityReference("product", productId);
                            productQuantity["gsc_productquantitypn"] = productName;

                            Guid newProductQuantityId = _organizationService.Create(productQuantity);

                            inventory["gsc_productquantityid"] = new EntityReference("gsc_iv_productquantity", newProductQuantityId);
                        }

                        _tracingService.Trace("Create Inventory record");
                        inventory["gsc_inventorypn"] = productName + "-" + siteName;
                        inventory["gsc_status"] = new OptionSetValue(100000000);
                        inventory["gsc_color"] = color;
                        inventory["gsc_engineno"] = engineNo;
                        inventory["gsc_csno"] = csNo;
                        inventory["gsc_productionno"] = productionNo;
                        inventory["gsc_vin"] = vin;
                        inventory["gsc_modelcode"] = modelCode;
                        inventory["gsc_optioncode"] = optionCode;
                        inventory["gsc_modelyear"] = modelYear;
                        inventory["gsc_siteid"] = vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_siteid") != null
                            ? vehicleAdjustmentVarianceEntryDetail.GetAttributeValue<EntityReference>("gsc_siteid")
                            : null;

                        Guid inventoryId = _organizationService.Create(inventory);
                        EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_productquantityid", "gsc_modelcode", "gsc_optioncode", "gsc_productid", "gsc_modelyear", "gsc_siteid",
                            "gsc_vin", "gsc_csno", "gsc_productionno", "gsc_engineno" });
                        if(inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                        {
                        Entity inventoryE = inventoryRecords.Entities[0];
                        Guid fromSite = productQuantity.Contains("gsc_siteid") ? productQuantity.GetAttributeValue<EntityReference>("gsc_siteid").Id : Guid.Empty;
                        inventoryMovementHandler.CreateInventoryHistory("Positive Adjustment", null, null, transactionNumber, transactionDate, 0, 1, onHandCount + 1, Guid.Empty, fromSite, fromSite, inventoryE, productQuantity, true,true);
                        }
                    }

                    #endregion
                }
            }

            _tracingService.Trace("Ended PostVehicleAdjustmentVarianceEntry Method...");
            return vehicleAdjustmentVarianceEntryEntity;
        }

        //Created By : Raphael Herrera, Created On : 1/11/2017
        /*Purpose: Delete adjustment details. Set inventory record to available and adjust product quantity count
         * Registration Details: 
         * Event/Message:
         *      Pre-Validate/Delete:
         *      Post/Update: gsc_adjustmentidtounallocate
         *      Post/Create:
         * Primary Entity: Vehicle Adjustment/Variance Entry Detail
         */
        public Entity RemoveAdjustedVehicle(Entity vehicleAdjustmentEntity)
        {
            _tracingService.Trace("Started RemoveAdjustedVehicle Method...");
            Guid adjustedVehicleIdToRemove = vehicleAdjustmentEntity.Contains("gsc_adjustmentidtounallocate") ?
                new Guid(vehicleAdjustmentEntity.GetAttributeValue<string>("gsc_adjustmentidtounallocate")) : Guid.Empty;

            EntityCollection adjustedDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_adjustmentvariancedetail", "gsc_sls_adjustmentvariancedetailid", adjustedVehicleIdToRemove,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_inventoryid", "gsc_quantity" });
            _tracingService.Trace("Adjsuted details records: " + adjustedDetailsCollection.Entities.Count);

            if (adjustedDetailsCollection.Entities.Count > 0)
            {
                Entity adjustedDetailsEntity = adjustedDetailsCollection.Entities[0];
                UnallocateVehicleAdjustment(adjustedDetailsEntity, vehicleAdjustmentEntity, "Remove");

                
            }
            _tracingService.Trace("Ending RemoveAdjustedVehicle Method...");
            return vehicleAdjustmentEntity;
        }

        //Handles logic for removing vehicle adjustment details
        private Entity UnallocateVehicleAdjustment(Entity adjustedVehcileDetailsEntity, Entity vehicleAdjustmentEntity, string caller)
        {
            var inventoryId = adjustedVehcileDetailsEntity.Contains("gsc_inventoryid") ? adjustedVehcileDetailsEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                        : Guid.Empty;

            if (inventoryId != Guid.Empty)
            {
                var quantity = adjustedVehcileDetailsEntity.Contains("gsc_quantity") ? adjustedVehcileDetailsEntity.GetAttributeValue<Int32>("gsc_quantity") : 0;
                EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_status", "gsc_productquantityid", "gsc_basemodelid", "gsc_productid", "gsc_modelcode", "gsc_optioncode", "gsc_modelyear", "gsc_siteid",
                    "gsc_vin", "gsc_csno", "gsc_productionno", "gsc_engineno", });

                _tracingService.Trace("Inventory records retrieved: " + inventoryCollection.Entities.Count);
                if (inventoryCollection.Entities.Count > 0)
                {
                    _organizationService.Delete(adjustedVehcileDetailsEntity.LogicalName, adjustedVehcileDetailsEntity.Id);

                    _tracingService.Trace("Deleted inventory record...");

                    //negative adjustment logic
                    if (quantity < 0)
                    {
                        Entity inventoryEntity = inventoryCollection.Entities[0];
                        InventoryMovementHandler inventoryMovementHandler = new InventoryMovementHandler(_organizationService, _tracingService);
                        inventoryMovementHandler.UpdateInventoryStatus(inventoryEntity, 100000000);
                        Entity productQuantityEntity = inventoryMovementHandler.UpdateProductQuantity(inventoryEntity, 0, 1, -1, 0, 0, 0, 0, 0);

                         #region Inventory History Log Creation
                        InventoryMovementHandler inventoryMovement = new InventoryMovementHandler(_organizationService, _tracingService);

                        if(caller == "Cancel")
                            inventoryMovement.CreateInventoryQuantityAllocated(vehicleAdjustmentEntity, inventoryEntity, productQuantityEntity, vehicleAdjustmentEntity.GetAttributeValue<string>("gsc_vehicleadjustmentvarianceentrypn"),
                            DateTime.UtcNow, "Cancel", Guid.Empty, 100000004);
                        else
                            inventoryMovement.CreateInventoryQuantityAllocated(vehicleAdjustmentEntity, inventoryEntity, productQuantityEntity, vehicleAdjustmentEntity.GetAttributeValue<string>("gsc_vehicleadjustmentvarianceentrypn"),
                            DateTime.UtcNow, "Open", Guid.Empty, 100000003);
                        #endregion
                    }
                }

               
            }
            else
                _organizationService.Delete(adjustedVehcileDetailsEntity.LogicalName, adjustedVehcileDetailsEntity.Id);

            return adjustedVehcileDetailsEntity;
        }
        //Created By:  Leslie Baliguat, Created On: 1/11/2017
        public void UpdateStatus(Entity vehicleAdjustmentEntity)
        {
            _tracingService.Trace("Started UpdateStatus Method...");

            Entity adjustmentToUpdate = _organizationService.Retrieve(vehicleAdjustmentEntity.LogicalName, vehicleAdjustmentEntity.Id,
                new ColumnSet("gsc_adjustmentvariancestatuscopy"));

            adjustmentToUpdate["gsc_adjustmentvariancestatuscopy"] = vehicleAdjustmentEntity.Contains("gsc_adjustmentvariancestatus")
                ? vehicleAdjustmentEntity.GetAttributeValue<OptionSetValue>("gsc_adjustmentvariancestatus")
                : null;

            _organizationService.Update(adjustmentToUpdate);

            _tracingService.Trace("Ending UpdateStatus Method");
        }

        //Created By : Raphael Herrera, Created On : 1/11/2017
        /*Purpose: Cancel Vehicle Adjustment record. Delete all adjusted vehicle details records. Set inventory record to available and adjust product quantity count for each adjusted vehicle records.
         * Registration Details: 
         * Event/Message:
         *      Pre-Validate/Delete:
         *      Post/Update: gsc_adjustmentvariancestatus
         *      Post/Create:
         * Primary Entity: Vehicle Adjustment/Variance Entry Detail
         */
        public Entity CancelAdjustedVehicle(Entity vehicleAdjustmentEntity)
        {
            _tracingService.Trace("Started CancelAdjustedVehicle Method...");

            EntityCollection adjustedDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_adjustmentvariancedetail", "gsc_vehicleadjustmentvarianceentryid", vehicleAdjustmentEntity.Id,
                _organizationService, null, OrderType.Ascending, "gsc_inventoryid", "gsc_quantity");

            _tracingService.Trace("Adjusted Details Records retrieved: " + adjustedDetailsCollection.Entities.Count);

            if (adjustedDetailsCollection.Entities.Count > 0)
            {
                foreach (Entity adjustedDetails in adjustedDetailsCollection.Entities)
                {
                    UnallocateVehicleAdjustment(adjustedDetails, vehicleAdjustmentEntity, "Cancel");
                }
            
            }

            SetStateRequest request = new SetStateRequest
            {
                EntityMoniker = new EntityReference("gsc_sls_vehicleadjustmentvarianceentry", vehicleAdjustmentEntity.Id),
                State = new OptionSetValue(1),
                Status = new OptionSetValue(2)
            };
            _organizationService.Execute(request);

            _tracingService.Trace("Started CancelAdjustedVehicle Method...");
            return vehicleAdjustmentEntity;
        }

        //Created By: Leslie G. Baliguat, Modified By: 01/11/2017
        public Boolean RestrictPosting(Entity adjustmentEntry)
        {
            EntityCollection adjustedDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_adjustmentvariancedetail", "gsc_vehicleadjustmentvarianceentryid", adjustmentEntry.Id,
                _organizationService, null, OrderType.Ascending, "gsc_siteid");

            if (adjustedDetailsCollection != null && adjustedDetailsCollection.Entities.Count > 0)
            {
                Entity adjustedEntry = adjustedDetailsCollection.Entities[0];

                var siteId = CommonHandler.GetEntityReferenceIdSafe(adjustedEntry, "gsc_siteid");

                EntityCollection siteCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_site", "gsc_iv_siteid", siteId,
                _organizationService, null, OrderType.Ascending, "gsc_sellsite");

                if (siteCollection != null && siteCollection.Entities.Count > 0)
                {
                    //Added By : Jessica Casupanan, Added On : 01/12/2017
                    //Revert to status = Open
                    Boolean sellSite = siteCollection.Entities[0].GetAttributeValue<Boolean>("gsc_sellsite");
                    if (sellSite == false)
                    {
                        Entity adjustmentToUpdate = _organizationService.Retrieve(adjustmentEntry.LogicalName, adjustmentEntry.Id,
                        new ColumnSet("gsc_adjustmentvariancestatus"));
                        adjustmentToUpdate["gsc_adjustmentvariancestatus"] = new OptionSetValue(100000000);
                        _organizationService.Update(adjustmentToUpdate);
                    }
                    //End 
                    return sellSite;
                }
            }

            return false;
        }
           
    }
}
