using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.InventoryMovement
{
    public class InventoryMovementHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public InventoryMovementHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        public Entity PopulateVehicleDetails(Entity productQuantity, String message)
        {
            _tracingService.Trace("Method PopulateVehicleDetails Started");

            var productId = productQuantity.Contains("gsc_productid")
                ? productQuantity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_modelcode", "gsc_optioncode", "gsc_modelyear" });

            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Product Details");

                Entity productEntity = productCollection.Entities[0];

                productQuantity["gsc_modelcode"] = productEntity.Contains("gsc_modelcode")
                    ? productEntity.GetAttributeValue<String>("gsc_modelcode")
                    : null;
                productQuantity["gsc_optioncode"] = productEntity.Contains("gsc_optioncode")
                    ? productEntity.GetAttributeValue<String>("gsc_optioncode")
                    : null;
                productQuantity["gsc_modelyear"] = productEntity.Contains("gsc_modelyear")
                    ? productEntity.GetAttributeValue<String>("gsc_modelyear")
                    : null;

                if (message.Equals("Update"))
                    _organizationService.Update(productQuantity);
            }

            _tracingService.Trace("Method PopulateVehicleDetails Ended");

            return productQuantity;
        }

        public void UpdateInventoryStatus(Entity inventory, int status)
        {
            _tracingService.Trace("UpdateInventoryStatus Started ...");

            inventory["gsc_status"] = new OptionSetValue(status);

            _organizationService.Update(inventory);

            _tracingService.Trace("UpdateInventoryStatus Ended ...");
        }

        public Entity UpdateProductQuantity(Entity inventoryEntity, int onhand, int available, int allocated, int onOrder, int sold, int inTransit, int damaged, int backOrder)
        {
            var quantityId = inventoryEntity.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                ? inventoryEntity.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                : Guid.Empty;

            EntityCollection quantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", quantityId, _organizationService, null, OrderType.Ascending,
           new[] { "gsc_onhand", "gsc_available", "gsc_allocated", "gsc_onorder", "gsc_sold", "gsc_intransit", "gsc_damaged", "gsc_backorder", "gsc_vehiclecolorid", "gsc_vehiclemodelid" });

            if (quantityRecords != null && quantityRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieved Product Quantity Details");

                Entity quantityEntity = quantityRecords.Entities[0];

                return UpdateProductQuantityDirectly(quantityEntity, onhand, available, allocated, onOrder, sold, inTransit, damaged, backOrder);
            }

            return null;
        }

        public Entity UpdateInventoryFields(Entity inventoryEntity, String message)
        {
            _tracingService.Trace("Starting UpdateInventoryFields Method...");
            var productQuantityId = inventoryEntity.Contains("gsc_productquantityid") ? inventoryEntity.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                : Guid.Empty;

            EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_productid", "gsc_vehiclemodelid", "gsc_siteid", "gsc_vehiclecolorid" });

            _tracingService.Trace("Product Quantity Records Retrieved: " + productQuantityCollection.Entities.Count);
            if (productQuantityCollection.Entities.Count > 0)
            {
                Entity productQuantityEntity = productQuantityCollection.Entities[0];

                var productId = CommonHandler.GetEntityReferenceIdSafe(productQuantityEntity, "gsc_productid");

                inventoryEntity["gsc_productid"] = productQuantityEntity.Contains("gsc_productid")
                    ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_productid")
                    : null;
                inventoryEntity["gsc_basemodelid"] = productQuantityEntity.Contains("gsc_vehiclemodelid")
                    ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid")
                    : null;
                inventoryEntity["gsc_siteid"] = productQuantityEntity.Contains("gsc_siteid")
                    ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_siteid")
                    : null;
                inventoryEntity["gsc_color"] = productQuantityEntity.Contains("gsc_vehiclecolorid")
                    ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Name
                    : String.Empty;


                //Retrieve Vehicle Model Details
                EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_optioncode", "gsc_modelcode", "gsc_modelyear" });

                if (productCollection.Entities.Count > 0)
                {
                    Entity product = productCollection.Entities[0];

                    inventoryEntity["gsc_optioncode"] = product.Contains("gsc_optioncode")
                        ? product.GetAttributeValue<String>("gsc_optioncode")
                        : String.Empty;
                    inventoryEntity["gsc_modelcode"] = product.Contains("gsc_modelcode")
                        ? product.GetAttributeValue<String>("gsc_modelcode")
                        : String.Empty;
                    inventoryEntity["gsc_modelyear"] = product.Contains("gsc_modelyear")
                        ? product.GetAttributeValue<String>("gsc_modelyear")
                        : String.Empty;

                }

                if (message != "Create")
                    _organizationService.Update(inventoryEntity);

                _tracingService.Trace("Updated Inventory Record...");
            }

            _tracingService.Trace("Ending UpdateInventoryFields Method");
            return inventoryEntity;

        }

        public Entity UpdateProductQuantityDirectly(Entity quantityEntity, int onhand, int available, int allocated, int onOrder, int sold, int inTransit, int damaged, int backOrder)
        {
            quantityEntity["gsc_onhand"] = quantityEntity.GetAttributeValue<Int32>("gsc_onhand") + onhand;
            quantityEntity["gsc_available"] = quantityEntity.GetAttributeValue<Int32>("gsc_available") + available;
            quantityEntity["gsc_allocated"] = quantityEntity.GetAttributeValue<Int32>("gsc_allocated") + allocated;
            quantityEntity["gsc_onorder"] = quantityEntity.GetAttributeValue<Int32>("gsc_onorder") + onOrder;
            quantityEntity["gsc_sold"] = quantityEntity.GetAttributeValue<Int32>("gsc_sold") + sold;
            quantityEntity["gsc_intransit"] = quantityEntity.GetAttributeValue<Int32>("gsc_intransit") + inTransit;
            quantityEntity["gsc_damaged"] = quantityEntity.GetAttributeValue<Int32>("gsc_damaged") + damaged;
            quantityEntity["gsc_backorder"] = quantityEntity.GetAttributeValue<Int32>("gsc_backorder") + backOrder;

            _organizationService.Update(quantityEntity);

            _tracingService.Trace("Product Quantity Updated");

            return quantityEntity;

        }

        //Delete ProductQuantity used in VPO  when on_order becomes zero (0)
        public void DeleteProductQuantity(Entity productQuantity)
        {
            var onOrder = productQuantity.Contains("gsc_onorder")
                ? productQuantity.GetAttributeValue<Int32>("gsc_onorder")
                : 0;

            if (productQuantity.GetAttributeValue<Boolean>("gsc_isorder") == true && onOrder == 0)
                _organizationService.Delete(productQuantity.LogicalName, productQuantity.Id);
        }

        //When cancelling a Vehicle Purchase Order
        public Entity UnTagLatestHistoryUnservedPO(Entity inventoryHistoryEntity)
        {
            if (inventoryHistoryEntity.GetAttributeValue<Boolean>("gsc_isvimi") == true)
                return null;

            var inventoryId = inventoryHistoryEntity.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                ? inventoryHistoryEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                : Guid.Empty;
            var productConditionList = new List<ConditionExpression>{};
                    
            if (inventoryId != Guid.Empty)
            {
                productConditionList.Add(new ConditionExpression("gsc_inventoryid", ConditionOperator.Equal, inventoryId));
                productConditionList.Add(new ConditionExpression("gsc_latest", ConditionOperator.Equal, true));
            }
            else
            {
                var productQuantityId = inventoryHistoryEntity.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                    ? inventoryHistoryEntity.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                    : Guid.Empty;
                var transactionNo = inventoryHistoryEntity.Contains("gsc_transactionnumber")
                    ? inventoryHistoryEntity.GetAttributeValue<String>("gsc_transactionnumber")
                    : null;
                var transactionDate = inventoryHistoryEntity.Contains("gsc_transactiondate")
                    ? inventoryHistoryEntity.GetAttributeValue<DateTime?>("gsc_transactiondate")
                    : null;

                if(transactionDate != null)
                    productConditionList.Add(new ConditionExpression("gsc_transactiondate", ConditionOperator.Equal, transactionDate));

                productConditionList.Add(new ConditionExpression("gsc_productquantityid", ConditionOperator.Equal, productQuantityId));
                productConditionList.Add(new ConditionExpression("gsc_transactionnumber", ConditionOperator.Equal, transactionNo));
                productConditionList.Add(new ConditionExpression("gsc_latest", ConditionOperator.Equal, true));
            }

            EntityCollection historyCollection = CommonHandler.RetrieveRecordsByConditions("gsc_iv_inventoryhistory", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_latest" });

            if (historyCollection != null && historyCollection.Entities.Count > 0)
            {
                Entity historyEntity = historyCollection.Entities[0];
                historyEntity["gsc_latest"] = false;
                _organizationService.Update(historyEntity);
            }

            return inventoryHistoryEntity;
        }

        public Entity LogInventoryQuantityOnorder(Entity transactionEntity, Entity transactionDetailsEntity, Entity productQuantityEntity, Boolean isLatest, int type)
        {
            Entity history = new Entity("gsc_iv_inventoryhistory");
            history["gsc_vehiclebasemodelid"] = transactionDetailsEntity.GetAttributeValue<EntityReference>("gsc_basemodelid") != null
                ? transactionDetailsEntity.GetAttributeValue<EntityReference>("gsc_basemodelid")
                : null;
            history["gsc_productid"] = transactionDetailsEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? transactionDetailsEntity.GetAttributeValue<EntityReference>("gsc_productid")
                : null;
            history["gsc_modelcode"] = transactionDetailsEntity.Contains("gsc_modelcode")
                ? transactionDetailsEntity.GetAttributeValue<String>("gsc_modelcode")
                : null;
            history["gsc_optioncode"] = transactionDetailsEntity.Contains("gsc_optioncode")
                ? transactionDetailsEntity.GetAttributeValue<String>("gsc_optioncode")
                : null;
            history["gsc_modelyear"] = transactionDetailsEntity.Contains("gsc_modelyear")
                ? transactionDetailsEntity.GetAttributeValue<String>("gsc_modelyear")
                : null;
            history["gsc_vehiclecolorid"] = transactionDetailsEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                ? transactionDetailsEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                : null;
            history["gsc_siteid"] = transactionEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? transactionEntity.GetAttributeValue<EntityReference>("gsc_siteid")
                : null;
            history["gsc_transactionnumber"] = transactionEntity.Contains("gsc_purchaseorderpn")
                ? transactionEntity.GetAttributeValue<String>("gsc_purchaseorderpn")
                : null;
            history["gsc_transactiontype"] = transactionEntity.Contains("gsc_vpotype")
                ? transactionEntity.FormattedValues["gsc_vpotype"].ToString()
                : null;
            history["gsc_transactiondate"] = transactionEntity.Contains("gsc_vpodate")
                ? transactionEntity.GetAttributeValue<DateTime?>("gsc_vpodate")
                : null;
            history["gsc_transactionstatus"] = transactionEntity.Contains("gsc_vpostatus")
                ? transactionEntity.FormattedValues["gsc_vpostatus"].ToString()
                : null;

            history["gsc_quantitytype"] = new OptionSetValue(type);
            history["gsc_latest"] = isLatest;
            history["gsc_productquantityid"] = new EntityReference(productQuantityEntity.LogicalName, productQuantityEntity.Id);

            _organizationService.Create(history);

            return history;
        }

        //Log Inventory History
        public Entity CreateInventoryHistory(String transactionType, String customerId, String customerName, String transactionNumber, DateTime transactionDate, Int32 quantityOut, Int32 quantityIn, Int32 balance, Guid toSite, Guid fromSite, Guid site, Entity inventory, Entity productQuantity, bool isVIMI, bool displayAllSite)
        {
            Entity ivHistory = new Entity("gsc_iv_inventoryhistory");
            //DateTime transactionDate = DateTime.UtcNow.Date;
            ivHistory["gsc_transactionnumber"] = transactionNumber;
            ivHistory["gsc_transactiontype"] = transactionType;
            ivHistory["gsc_transactiondate"] = transactionDate;
            ivHistory["gsc_customerid"] = customerId;
            ivHistory["gsc_customername"] = customerName;
            ivHistory["gsc_quantityout"] = quantityOut;
            ivHistory["gsc_quantityin"] = quantityIn;
            ivHistory["gsc_tosite"] = toSite != Guid.Empty ? new EntityReference("gsc_iv_site", toSite) : null;
            ivHistory["gsc_balance"] = balance;
            ivHistory["gsc_modelcode"] = inventory.Contains("gsc_modelcode") ? inventory.GetAttributeValue<String>("gsc_modelcode") : String.Empty;
            ivHistory["gsc_optioncode"] = inventory.Contains("gsc_optioncode") ? inventory.GetAttributeValue<String>("gsc_optioncode") : String.Empty;
            ivHistory["gsc_fromsite"] = fromSite != Guid.Empty ? new EntityReference("gsc_iv_site", fromSite) : null;
            ivHistory["gsc_vehiclecolorid"] = productQuantity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                        ? new EntityReference("gsc_cmn_vehiclecolor", productQuantity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id)
                        : null;
            ivHistory["gsc_vehiclebasemodelid"] = productQuantity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                        ? new EntityReference("gsc_iv_vehiclebasemodel", productQuantity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id)
                        : null;
            ivHistory["gsc_productid"] = productQuantity.GetAttributeValue<EntityReference>("gsc_productid") != null
                        ? new EntityReference("product", productQuantity.GetAttributeValue<EntityReference>("gsc_productid").Id)
                        : null;
            ivHistory["gsc_siteid"] = site != Guid.Empty ? new EntityReference("gsc_iv_site", site) : null;
            ivHistory["gsc_isvimi"] = isVIMI;
            ivHistory["gsc_displayallsite"] = displayAllSite;
            _organizationService.Create(ivHistory);

            return ivHistory;
        }

        public Entity CreateInventoryQuantityAllocated(Entity transactionEntity, Entity inventoryEntity, Entity productQuantityEntity, string transactionNumber, DateTime transactionDate, string transactionStatus, Guid destinationSite,int quantityType)
        {
            _tracingService.Trace("Started CreateInventoryQuantityAllocated Method...");
            Entity inventoryHistory = new Entity("gsc_iv_inventoryhistory");

            string transactionType = "";
            switch(transactionEntity.LogicalName)
            {
                case "gsc_sls_shippingclaim": transactionType = "Vehicle Shipping Claims";
                    break;
                case "gsc_cmn_returntransaction": transactionType = "Vehicle Return";
                    break;
                case "gsc_iv_vehicletransfer": transactionType = "Vehicle Transfer";
                    break;
                case "gsc_sls_vehicleadjustmentvarianceentry": transactionType = "Vehicle Adjustment/Variance";
                    break;
                case "salesorder": transactionType = "Vehicle Sales Order";
                    break;
                case "invoice": transactionType = "Vehicle Sales Invoice";
                    break;
                case "gsc_iv_vehicleintransittransfer": transactionType = "Vehicle In Transit Transfer";
                    break;
                
            }

            var baseModelId = productQuantityEntity.Contains("gsc_vehiclemodelid") ? productQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id
                : Guid.Empty;
            var productId = inventoryEntity.Contains("gsc_productid") ? inventoryEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;
            inventoryHistory["gsc_productquantityid"] = new EntityReference("gsc_iv_productquantity", productQuantityEntity.Id);
            inventoryHistory["gsc_inventoryid"] = new EntityReference("gsc_iv_inventory", inventoryEntity.Id);
            if (baseModelId != Guid.Empty)
                inventoryHistory["gsc_vehiclebasemodelid"] = new EntityReference("gsc_iv_vehiclebasemodel", baseModelId);
            if(productId != Guid.Empty)
                inventoryHistory["gsc_productid"] = new EntityReference("product", productId);
            inventoryHistory["gsc_modelcode"] = inventoryEntity.Contains("gsc_modelcode") ? inventoryEntity.GetAttributeValue<string>("gsc_modelcode")
                : String.Empty;
            inventoryHistory["gsc_optioncode"] = inventoryEntity.Contains("gsc_optioncode") ? inventoryEntity.GetAttributeValue<string>("gsc_optioncode")
                : String.Empty;
            inventoryHistory["gsc_modelyear"] = inventoryEntity.Contains("gsc_modelyear") ? inventoryEntity.GetAttributeValue<string>("gsc_modelyear")
                : String.Empty;
            if(productQuantityEntity.Contains("gsc_vehiclecolorid"))
                inventoryHistory["gsc_vehiclecolorid"] = new EntityReference("gsc_cmn_vehiclecolor", productQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id);
            if (inventoryEntity.Contains("gsc_siteid"))
                inventoryHistory["gsc_siteid"] = new EntityReference("gsc_iv_site", inventoryEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id);
            if(destinationSite != Guid.Empty)
                inventoryHistory["gsc_destinationsiteid"] =  new EntityReference("gsc_iv_site", destinationSite);
            inventoryHistory["gsc_transactiontype"] = transactionType;
            inventoryHistory["gsc_transactionnumber"] = transactionNumber;
            inventoryHistory["gsc_transactiondate"] = transactionDate;
            inventoryHistory["gsc_transactionstatus"] = transactionStatus;
            inventoryHistory["gsc_vin"] = inventoryEntity.Contains("gsc_vin") ? inventoryEntity.GetAttributeValue<string>("gsc_vin") : String.Empty;
            inventoryHistory["gsc_csno"] = inventoryEntity.Contains("gsc_csno") ? inventoryEntity.GetAttributeValue<string>("gsc_csno") : String.Empty;
            inventoryHistory["gsc_productionno"] = inventoryEntity.Contains("gsc_productionno") ? inventoryEntity.GetAttributeValue<string>("gsc_productionno") : String.Empty;
            inventoryHistory["gsc_engineno"] = inventoryEntity.Contains("gsc_engineno") ? inventoryEntity.GetAttributeValue<string>("gsc_engineno") : String.Empty;
            inventoryHistory["gsc_allocationage"] = transactionEntity.Contains("gsc_vehicleallocationage") ? transactionEntity.GetAttributeValue<Int32>("gsc_vehicleallocationage")
                : 0;
            inventoryHistory["gsc_latest"] = true;
            inventoryHistory["gsc_quantitytype"] = new OptionSetValue(quantityType);

            _organizationService.Create(inventoryHistory);
        
            _tracingService.Trace("Ending CreateInventoryQuantityAllocated Method...");
            return inventoryHistory;
        }
    }
}
