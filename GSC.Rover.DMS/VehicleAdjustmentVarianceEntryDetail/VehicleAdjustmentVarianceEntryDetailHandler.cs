using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.VehicleAdjustmentVarianceEntryDetail
{
    public class VehicleAdjustmentVarianceEntryDetailHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleAdjustmentVarianceEntryDetailHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 8/30/2016
        /*Purpose: Validate if new record exists in Inventory entity
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: All input fields
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Vehicle Adjustment/Variance Entry Detail
         */
        public Entity CheckExistingInventoryRecord(Entity vehicleAdjustmentVarianceEntryDetailEntity, String message)
        {
            _tracingService.Trace("Started CheckExistingInventoryRecord Method...");
            
            //Return if record is made through plugin by checking if Inventory field contains data
            if (vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_inventoryid") != null) { return null; }

            //Retrieve Vehicle Adjustment/Variance Entry record
            Entity vehicleAdjustmentVarianceEntry = _organizationService.Retrieve("gsc_sls_vehicleadjustmentvarianceentry", vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_vehicleadjustmentvarianceentryid").Id, new ColumnSet("gsc_adjustmentvariancestatus"));

            //Return error message if record is already posted
            if (vehicleAdjustmentVarianceEntry.FormattedValues["gsc_adjustmentvariancestatus"].Equals("Posted"))
            { 
                throw new InvalidPluginExecutionException("Record is already posted. Cannot allocate new vehicle.");
            }

            Guid productId = vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;
            String modelCode = vehicleAdjustmentVarianceEntryDetailEntity.Contains("gsc_modelcode")
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<String>("gsc_modelcode")
                : String.Empty;
            String optionCode = vehicleAdjustmentVarianceEntryDetailEntity.Contains("gsc_optioncode")
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<String>("gsc_optioncode")
                : String.Empty;
            Guid colorId = vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id
                : Guid.Empty;
            String colorName = String.Empty;
            String productionNo = vehicleAdjustmentVarianceEntryDetailEntity.Contains("gsc_productionno")
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<String>("gsc_productionno")
                : String.Empty;
            String csNo = vehicleAdjustmentVarianceEntryDetailEntity.Contains("gsc_csno")
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<String>("gsc_csno")
                : String.Empty;
            String engineNo = vehicleAdjustmentVarianceEntryDetailEntity.Contains("gsc_engineno")
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<String>("gsc_engineno")
                : String.Empty;
            String vin = vehicleAdjustmentVarianceEntryDetailEntity.Contains("gsc_vin")
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<String>("gsc_vin")
                : String.Empty;

            //Retrieve Vehicle Color record for filtering
            EntityCollection vehicleColorRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_vehiclecolor", "gsc_cmn_vehiclecolorid", colorId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_vehiclecolorpn" });
            
            _tracingService.Trace(vehicleColorRecords.Entities.Count.ToString() + " Vehicle Color record/records retrieved.");
            
            if (vehicleColorRecords != null && vehicleColorRecords.Entities.Count > 0)
            {
                Entity vehicleColor = vehicleColorRecords.Entities[0];
                
                colorName = vehicleColor.Contains("gsc_vehiclecolorpn")
                    ? vehicleColor.GetAttributeValue<String>("gsc_vehiclecolorpn")
                    : String.Empty;
            }

            //Create filter for Product in Product entity
            var productConditionList = new List<ConditionExpression>
            {
                new ConditionExpression("productid", ConditionOperator.Equal, productId),
                new ConditionExpression("gsc_modelcode", ConditionOperator.Equal, modelCode),
                new ConditionExpression("gsc_optioncode", ConditionOperator.Equal, optionCode)
            };

            //Retrieve Product record
            EntityCollection productRecords = CommonHandler.RetrieveRecordsByConditions("product", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "productid", "gsc_modelyear", "gsc_vehiclemodelid" });

            _tracingService.Trace("Product Records Retrieved: " + productRecords.Entities.Count);
            if (productRecords != null && productRecords.Entities.Count > 0)
            {
                Entity product = productRecords.Entities[0];

                vehicleAdjustmentVarianceEntryDetailEntity["gsc_modelyear"] = product.Contains("gsc_modelyear")
                    ? product.GetAttributeValue<String>("gsc_modelyear")
                    : String.Empty;
                vehicleAdjustmentVarianceEntryDetailEntity["gsc_vehiclebasemodelid"] = product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                    ? new EntityReference("gsc_iv_vehiclebasemodel", product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id)
                    : null;
                vehicleAdjustmentVarianceEntryDetailEntity["gsc_productid"] = new EntityReference(product.LogicalName, product.Id);
            }
            else
            {
                throw new InvalidPluginExecutionException("Product with Model Code: " + modelCode + " and Option Code: " + optionCode + " does not exist yet. Please insert product information in Vehicle and Item Catalog.");
            }

            //Create filter for Product in Adjustment/Variance Entry and Inventory entities
            FilterExpression productFilter = new FilterExpression(LogicalOperator.And);
            
            FilterExpression productInfoFilter = new FilterExpression(LogicalOperator.Or);
            productInfoFilter.Conditions.Add(new ConditionExpression("gsc_productionno", ConditionOperator.Equal, productionNo));
            productInfoFilter.Conditions.Add(new ConditionExpression("gsc_csno", ConditionOperator.Equal, csNo));
            productInfoFilter.Conditions.Add(new ConditionExpression("gsc_engineno", ConditionOperator.Equal, engineNo));
            productInfoFilter.Conditions.Add(new ConditionExpression("gsc_vin", ConditionOperator.Equal, vin));
            
            FilterExpression statusFilter = new FilterExpression(LogicalOperator.And);
            statusFilter.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));

            productFilter.AddFilter(productInfoFilter);
            productFilter.AddFilter(statusFilter);

            //Verify if vehicle exists in Adjustment/Variance Detail entity
            QueryExpression vehicleAdjustmentVarianceEntryDetailQuery = new QueryExpression("gsc_sls_adjustmentvariancedetail");
            vehicleAdjustmentVarianceEntryDetailQuery.ColumnSet = new ColumnSet("gsc_sls_adjustmentvariancedetailid");
            vehicleAdjustmentVarianceEntryDetailQuery.Criteria.AddFilter(productInfoFilter);
            EntityCollection vehicleAdjustmentVarianceEntryDetailRecords = _organizationService.RetrieveMultiple(vehicleAdjustmentVarianceEntryDetailQuery);

            _tracingService.Trace(vehicleAdjustmentVarianceEntryDetailRecords.Entities.Count.ToString() + " Vehicle Adjustment/Variance Entry Detail record/records retrieved.");

            if (vehicleAdjustmentVarianceEntryDetailRecords != null && vehicleAdjustmentVarianceEntryDetailRecords.Entities.Count > 0)
            {
                if (message.Equals("Create"))
                {
                    throw new InvalidPluginExecutionException("Vehicle already exists in Adjustment/Variance Entry.");
                }
                else if (message.Equals("Update"))
                {
                    foreach (Entity vehicleAdjustmentVarianceEntryDetail in vehicleAdjustmentVarianceEntryDetailRecords.Entities)
                    {
                        if (vehicleAdjustmentVarianceEntryDetail.Id != vehicleAdjustmentVarianceEntryDetailEntity.Id)
                        {
                            throw new InvalidPluginExecutionException("Vehicle already exists in Adjustment/Variance Entry.");
                        }
                    }    
                }                                
            }

            //Retrieve Inventory records using ConditionList
            QueryExpression inventoryQuery = new QueryExpression("gsc_iv_inventory");
            inventoryQuery.ColumnSet = new ColumnSet("gsc_status", "gsc_productquantityid");
            inventoryQuery.Criteria.AddFilter(productFilter);
            EntityCollection inventoryRecords = _organizationService.RetrieveMultiple(inventoryQuery);

            _tracingService.Trace(inventoryRecords.Entities.Count.ToString() + " Inventory record/records retrieved.");

            if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("Vehicle already exists in inventory.");       
            }

            if (message.Equals("Create"))
            {
                _tracingService.Trace("Create...");
                //Set Vehicle Adjustment/Variance Entry Detail Operation field to 'Add'
                vehicleAdjustmentVarianceEntryDetailEntity["gsc_operation"] = new OptionSetValue(100000000);
                vehicleAdjustmentVarianceEntryDetailEntity["gsc_quantity"] = 1;
            }
            else if (message.Equals("Update"))
            {
                _tracingService.Trace("Update...");
                _organizationService.Update(vehicleAdjustmentVarianceEntryDetailEntity);
            }            

            _tracingService.Trace("Ended CheckExistingInventoryRecord Method...");
            return vehicleAdjustmentVarianceEntryDetailEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 8/31/2016
        /*Purpose: Update Inventory record fields on Vehicle Adjustment/Variance Entry Detail record delete
         * Registration Details: 
         * Event/Message:
         *      Pre-Validate/Delete: gsc_sls_adjustmentvariancedetailid
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Vehicle Adjustment/Variance Entry Detail
         */
        public Entity AdjustInventoryRecordOnDelete(Entity vehicleAdjustmentVarianceEntryDetailEntity)
        {
            _tracingService.Trace("Started AdjustInventoryRecordOnDelete Method...");

            Guid vehicleAdjustmentVarianceEntryId = vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_vehicleadjustmentvarianceentryid") != null
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_vehicleadjustmentvarianceentryid").Id
                : Guid.Empty;

            //Retrieve Vehicle Adjustment/Variance Entry record
            EntityCollection vehicleAdjustmentVarianceEntryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_vehicleadjustmentvarianceentry", "gsc_sls_vehicleadjustmentvarianceentryid", vehicleAdjustmentVarianceEntryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_adjustmentvariancestatus" });

            if (vehicleAdjustmentVarianceEntryRecords != null && vehicleAdjustmentVarianceEntryRecords.Entities.Count > 0)
            {
                Entity vehicleAdjustmentVarianceEntry = vehicleAdjustmentVarianceEntryRecords.Entities[0];

                if (vehicleAdjustmentVarianceEntry.FormattedValues["gsc_adjustmentvariancestatus"].Equals("Posted"))
                {
                    throw new InvalidPluginExecutionException("Unable to delete already posted Vehicle Adjustment/Variance Entry");
                }
            }

            Guid inventoryId = vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                ? vehicleAdjustmentVarianceEntryDetailEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                : Guid.Empty;

            //Retrieve Inventory records
            EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_status", "gsc_productquantityid" });

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
                    new[] { "gsc_allocated", "gsc_available" });

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
                }
            }

            _tracingService.Trace("Ended AdjustInventoryRecordOnDelete Method...");
            return vehicleAdjustmentVarianceEntryDetailEntity;        
        }
    }
}
