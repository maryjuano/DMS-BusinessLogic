using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.ShippingClaim
{
    public class ShippingClaimHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ShippingClaimHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 8/4/2016
        /*Purpose: Replicate Receiving Transaction fields into newly created Shipping Claim record
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: Invoice No = gsc_receivingtransactionid
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Shipping Claim
         */
        public Entity ReplicateReceivingTransactionFields(Entity shippingClaimEntity, String message)
        {
            _tracingService.Trace("Started ReplicateReceivingTransactionFields method..");

            Guid receivingTransactionId = shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_receivingtransactionid") != null
                ? shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_receivingtransactionid").Id
                : Guid.Empty;

            //Retrieve Invoice record
            EntityCollection receivingTransactionRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransaction", "gsc_cmn_receivingtransactionid", receivingTransactionId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_purchaseorderid", "gsc_invoiceno", "gsc_siteid" });

            if (receivingTransactionRecords != null && receivingTransactionRecords.Entities.Count > 0)
            {
                Entity receivingTransaction = receivingTransactionRecords.Entities[0];

                shippingClaimEntity["gsc_siteid"] = receivingTransaction.Contains("gsc_siteid")
                    ? new EntityReference("gsc_iv_site", receivingTransaction.GetAttributeValue<EntityReference>("gsc_siteid").Id)
                    : null;
                shippingClaimEntity["gsc_invoiceno"] = receivingTransaction.Contains("gsc_invoiceno")
                    ? receivingTransaction.GetAttributeValue<String>("gsc_invoiceno")
                    : String.Empty;
                shippingClaimEntity["gsc_vpono"] = receivingTransaction.Contains("gsc_purchaseorderid")
                    ? receivingTransaction.GetAttributeValue<EntityReference>("gsc_purchaseorderid").Name
                    : String.Empty;
                
                if (message.Equals("Update"))
                {
                    _organizationService.Update(shippingClaimEntity);
                }
            }

            _tracingService.Trace("Ended ReplicateReceivingTransactionFields method..");
            return shippingClaimEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 8/2/2016
        /*Purpose: Replicate Receiving Transaction Detail fields into newly created Shipping Claim Detail record
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create: Invoice No = gsc_receivingtransactionid
         * Primary Entity: Shipping Claim
         */
        public Entity ReplicateReceivingTransactionDetailFields(Entity shippingClaimEntity)
        {
            _tracingService.Trace("Started ReplicateReceivingTransactionDetailFields method..");

            Guid receivingTransactionId = shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_receivingtransactionid") != null
                ? shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_receivingtransactionid").Id
                : Guid.Empty;

            //Retrieve receiving transaction detail record using receiving transaction guid from shipping claim form
            EntityCollection receivingTransactionDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransactiondetail", "gsc_receivingtransactionid", receivingTransactionId, _organizationService, null, OrderType.Ascending, 
                new[] { "gsc_receivingtransactiondetailpn", "gsc_inventoryid", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_modeldescription", "gsc_modelyear", "gsc_optioncode", "gsc_productionno", "gsc_vin" });

            if (receivingTransactionDetailRecords != null && receivingTransactionDetailRecords.Entities.Count > 0)
            {
                foreach (Entity receivingTransactionDetail in receivingTransactionDetailRecords.Entities)
                {
                    Entity shippingClaimDetail = new Entity("gsc_sls_shippingclaimdetail");
                    shippingClaimDetail["gsc_shippingclaimid"] = new EntityReference(shippingClaimEntity.LogicalName, shippingClaimEntity.Id);
                    shippingClaimDetail["gsc_modeldescription"] = receivingTransactionDetail.Contains("gsc_modeldescription")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_modeldescription")
                        : String.Empty;
                    shippingClaimDetail["gsc_inventoryid"] = receivingTransactionDetail.Contains("gsc_inventoryid")
                        ? new EntityReference(receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").LogicalName, receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id)
                        : null;
                    shippingClaimDetail["gsc_color"] = receivingTransactionDetail.Contains("gsc_color")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_color")
                        : String.Empty;
                    shippingClaimDetail["gsc_csno"] = receivingTransactionDetail.Contains("gsc_csno")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_csno")
                        : String.Empty;
                    shippingClaimDetail["gsc_engineno"] = receivingTransactionDetail.Contains("gsc_engineno")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_engineno")
                        : String.Empty;
                    shippingClaimDetail["gsc_modelcode"] = receivingTransactionDetail.Contains("gsc_modelcode")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_modelcode")
                        : String.Empty;
                    shippingClaimDetail["gsc_modelyear"] = receivingTransactionDetail.Contains("gsc_modelyear")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_modelyear")
                        : String.Empty;
                    shippingClaimDetail["gsc_optioncode"] = receivingTransactionDetail.Contains("gsc_optioncode")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_optioncode")
                        : String.Empty;
                    shippingClaimDetail["gsc_productionno"] = receivingTransactionDetail.Contains("gsc_productionno")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_productionno")
                        : String.Empty;
                    shippingClaimDetail["gsc_vin"] = receivingTransactionDetail.Contains("gsc_vin")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_vin")
                        : String.Empty;
                    
                    _organizationService.Create(shippingClaimDetail);

                    //Inventory movement
                    EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_status", "gsc_productquantityid" });

                    if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                    {
                        Entity inventory = inventoryRecords.Entities[0];
                        inventory["gsc_status"] = new OptionSetValue(100000001);
                        
                        _organizationService.Update(inventory);

                        Guid productQuantityId = inventory.Contains("gsc_productquantityid")
                            ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                            : Guid.Empty;

                        //Retrieve Product Quantity record
                        EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_available", "gsc_allocated" });

                        if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                        {
                            Entity productQuantity = productQuantityRecords.Entities[0];

                            Int32 availableAmount = productQuantity.Contains("gsc_available")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                                : 1;
                            Int32 allocatedAmount = productQuantity.Contains("gsc_allocated")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_allocated")
                                : 0;

                            if (availableAmount != 0)
                            {
                                productQuantity["gsc_available"] = availableAmount - 1;    
                            }                     
                            productQuantity["gsc_allocated"] = allocatedAmount + 1;

                            _organizationService.Update(productQuantity);
                        }
                    }
                }
            }
            _tracingService.Trace("Ended ReplicateInvoicedVehicleFields method..");
            return shippingClaimEntity;
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 8/3/2016
        /*Purpose: Delete existing Shipping Claim Detail records
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Invoice No = gsc_invoiceid
         *      Post/Create:
         * Primary Entity: Shipping Claim
         */
        public Entity DeleteExistingShippingClaimDetail(Entity shippingClaimEntity)
        {
            _tracingService.Trace("Started DeleteExistingShippingClaimDetail method..");

            //Retrieve Shipping Claim Detail
            EntityCollection shippingClaimDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_shippingclaimdetail", "gsc_shippingclaimid", shippingClaimEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_sls_shippingclaimdetailid", "gsc_inventoryid" });

            if (shippingClaimDetailRecords != null && shippingClaimDetailRecords.Entities.Count > 0)
            {
                foreach (Entity shippingClaimDetail in shippingClaimDetailRecords.Entities)
                {
                    Guid inventoryId = shippingClaimDetail.Contains("gsc_inventoryid")
                        ? shippingClaimDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                        : Guid.Empty;

                    //Inventory movement
                    EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_status", "gsc_productquantityid" });

                    if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                    {
                        Entity inventory = inventoryRecords.Entities[0];
                        inventory["gsc_status"] = new OptionSetValue(100000001);

                        _organizationService.Update(inventory);

                        Guid productQuantityId = inventory.Contains("gsc_productquantityid")
                            ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                            : Guid.Empty;

                        //Retrieve Product Quantity record
                        EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_available", "gsc_allocated" });

                        if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                        {
                            Entity productQuantity = productQuantityRecords.Entities[0];

                            Int32 availableAmount = productQuantity.Contains("gsc_available")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                                : 0;
                            Int32 allocatedAmount = productQuantity.Contains("gsc_allocated")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_allocated")
                                : 1;

                            productQuantity["gsc_available"] = availableAmount + 1;
                            if (allocatedAmount != 0)
                            {
                                productQuantity["gsc_allocated"] = allocatedAmount - 1;    
                            }

                            _organizationService.Update(productQuantity);
                        }
                    }
                    _organizationService.Delete(shippingClaimDetail.LogicalName, shippingClaimDetail.Id);
                }
            }

            //Call method to populate shipping claim detail entity.
            ReplicateReceivingTransactionDetailFields(shippingClaimEntity);

            _tracingService.Trace("Ended DeleteExistingShippingClaimDetail method..");
            return shippingClaimEntity;
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 8/3/2016
        /*Purpose: Adjust Product Quantity on Open status
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: SCR Status = gsc_scrstatus
         *      Post/Create:
         * Primary Entity: Shipping Claim
         */
        public Entity AdjustCancelledOpenInventoryMovement(Entity shippingClaimEntity)
        {
            _tracingService.Trace("Started AdjustCancelledOpenInventoryMovement method..");

            if (!shippingClaimEntity.FormattedValues["gsc_scrstatus"].Equals("Open")) { return null; }

            //Retrieve Shipping Claim Detail records
            EntityCollection shippingClaimDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_shippingclaimdetail", "gsc_shippingclaimid", shippingClaimEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid" });

            if (shippingClaimDetailRecords != null && shippingClaimDetailRecords.Entities.Count > 0)
            {
                foreach (Entity shippingClaimDetail in shippingClaimDetailRecords.Entities)
                {
                    Guid inventoryId = shippingClaimDetail.Contains("gsc_inventoryid")
                        ? shippingClaimDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                        : Guid.Empty;
                    
                    //Retrieve Inventory record
                    EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_status", "gsc_productquantityid" });

                    if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                    {
                        Entity inventory = inventoryRecords.Entities[0];

                        inventory["gsc_status"] = new OptionSetValue(100000000);

                        _organizationService.Update(inventory);

                        Guid productQuantityId = inventory.Contains("gsc_productquantityid")
                            ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                            : Guid.Empty;

                        //Retrieve Product Quantity record
                        EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_allocated", "gsc_available" });

                        if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                        {
                            Entity productQuantity = productQuantityRecords.Entities[0];

                            Int32 availableAmount = productQuantity.Contains("gsc_available")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                                : 0;
                            Int32 allocatedAmount = productQuantity.Contains("gsc_allocated")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_allocated")
                                : 1;

                            productQuantity["gsc_available"] = availableAmount + 1;
                            
                            if (allocatedAmount != 0)
                            {
                                productQuantity["gsc_allocated"] = allocatedAmount - 1;   
                            }                           

                            _organizationService.Update(productQuantity);
                        }
                    }
                }                
            }

            _tracingService.Trace("Ended AdjustCancelledOpenInventoryMovement method..");
            return shippingClaimEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 8/3/2016
        /*Purpose: Adjust Product Quantity on Submitted status
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: SCR Status = gsc_scrstatus
         *      Post/Create:
         * Primary Entity: Shipping Claim
         */
        public Entity AdjustCancelledSubmittedInventoryMovement(Entity shippingClaimEntity)
        {
            _tracingService.Trace("Started AdjustCancelledSubmittedInventoryMovement method..");

            if (!shippingClaimEntity.FormattedValues["gsc_scrstatus"].Equals("Submitted")) { return null; }

            Guid siteId = shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                : Guid.Empty;
            
            //Retrieve Shipping Claim Detail records
            EntityCollection shippingClaimDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_shippingclaimdetail", "gsc_shippingclaimid", shippingClaimEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid", "gsc_modeldescription" });

            if (shippingClaimDetailRecords != null && shippingClaimDetailRecords.Entities.Count > 0)
            {
                Entity shippingClaimDetail = shippingClaimDetailRecords.Entities[0];

                Guid inventoryId = shippingClaimDetail.Contains("gsc_inventoryid")
                    ? shippingClaimDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                    : Guid.Empty;

                String modelDescription = shippingClaimDetail.Contains("gsc_modeldescription")
                    ? shippingClaimDetail.GetAttributeValue<String>("gsc_modeldescription")
                    : String.Empty;

                Guid productId = Guid.Empty;

                //Retrieve Product entity using Model Description
                EntityCollection productRecords = CommonHandler.RetrieveRecordsByOneValue("product", "name", modelDescription, _organizationService, null, OrderType.Ascending,
                    new[] { "productid" });

                if (productRecords != null && productRecords.Entities.Count > 0)
	            {
		            Entity product = productRecords.Entities[0];
   
                    productId = product.Id;
	            }

                #region Site
                //Create filter for retrieving Product Quantity
                var productQuantityConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_siteid", ConditionOperator.Equal, siteId),
                        new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId)
                    };

                //Retrieve Product Quantity record using Site + Product
                EntityCollection productQuantitySiteRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_onhand", "gsc_available" });

                if (productQuantitySiteRecords != null && productQuantitySiteRecords.Entities.Count > 0)
                {
                    Entity productQuantitySite = productQuantitySiteRecords.Entities[0];

                    Int32 onHandAmountSite = productQuantitySite.Contains("gsc_onhand")
                        ? productQuantitySite.GetAttributeValue<Int32>("gsc_onhand")
                        : 1;
                    Int32 availableAmountSite = productQuantitySite.Contains("gsc_available")
                        ? productQuantitySite.GetAttributeValue<Int32>("gsc_onhand")
                        : 1;
                    
                    if (onHandAmountSite != 0)
                    {
                        productQuantitySite["gsc_onhand"] = onHandAmountSite - 1;    
                    }

                    if (availableAmountSite != 0)
                    {
                        productQuantitySite["gsc_available"] = availableAmountSite - 1;    
                    }                  

                    _organizationService.Update(productQuantitySite);
                }
                
                #endregion

                #region Destination Site
                //Retrieve Inventory record
                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_status", "gsc_productquantityid" });

                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    Entity inventory = inventoryRecords.Entities[0];

                    inventory["gsc_status"] = new OptionSetValue(100000000);

                    _organizationService.Update(inventory);

                    Guid productQuantityId = inventory.Contains("gsc_productquantityid")
                            ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                            : Guid.Empty;

                    //Retrieve Product Quantity record
                    EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_onhand", "gsc_available" });

                    if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                    {
                        Entity productQuantity = productQuantityRecords.Entities[0];

                        Int32 availableAmount = productQuantity.Contains("gsc_available")
                            ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                            : 0;
                        Int32 onHandAmount = productQuantity.Contains("gsc_onhand")
                            ? productQuantity.GetAttributeValue<Int32>("gsc_onhand")
                            : 0;

                        productQuantity["gsc_available"] = availableAmount + 1;
                        productQuantity["gsc_onhand"] = onHandAmount + 1;

                        _organizationService.Update(productQuantity);
                    }
                #endregion
                
                }
            }
            _tracingService.Trace("Ended AdjustCancelledSubmittedInventoryMovement method..");
            return shippingClaimEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 8/5/2016
        /*Purpose: Adjust Product Quantity on Submitted status
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: SCR Status = gsc_scrstatus
         *      Post/Create:
         * Primary Entity: Shipping Claim
         */
        public Entity AdjustSubmittedInventoryMovement(Entity shippingClaimEntity)
        {
            _tracingService.Trace("Started AdjustSubmittedInventoryMovement method..");

            if (!shippingClaimEntity.FormattedValues["gsc_scrstatus"].Equals("Submitted")) { return null; }

            Guid siteId = shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                : Guid.Empty;

            //Retrieve Shipping Claim Detail records
            EntityCollection shippingClaimDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_shippingclaimdetail", "gsc_shippingclaimid", shippingClaimEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid", "gsc_modeldescription" });

            if (shippingClaimDetailRecords != null && shippingClaimDetailRecords.Entities.Count > 0)
            {
                Entity shippingClaimDetail = shippingClaimDetailRecords.Entities[0];

                Guid inventoryId = shippingClaimDetail.Contains("gsc_inventoryid")
                    ? shippingClaimDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                    : Guid.Empty;

                String modelDescription = shippingClaimDetail.Contains("gsc_modeldescription")
                    ? shippingClaimDetail.GetAttributeValue<String>("gsc_modeldescription")
                    : String.Empty;

                Guid productId = Guid.Empty;

                //Retrieve Product entity using Model Description
                EntityCollection productRecords = CommonHandler.RetrieveRecordsByOneValue("product", "name", modelDescription, _organizationService, null, OrderType.Ascending,
                    new[] { "productid" });

                if (productRecords != null && productRecords.Entities.Count > 0)
                {
                    Entity product = productRecords.Entities[0];

                    productId = product.Id;
                }

                #region Site
                //Create filter for retrieving Product Quantity
                var productQuantityConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_siteid", ConditionOperator.Equal, siteId),
                        new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId)
                    };
                
                //Retrieve Product Quantity record using Site + Product
                EntityCollection productQuantitySiteRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_onhand", "gsc_available" });

                if (productQuantitySiteRecords != null && productQuantitySiteRecords.Entities.Count > 0)
                {
                    Entity productQuantitySite = productQuantitySiteRecords.Entities[0];

                    Int32 onHandAmountSite = productQuantitySite.Contains("gsc_onhand")
                        ? productQuantitySite.GetAttributeValue<Int32>("gsc_onhand")
                        : 0;
                    Int32 availableAmountSite = productQuantitySite.Contains("gsc_available")
                        ? productQuantitySite.GetAttributeValue<Int32>("gsc_onhand")
                        : 0;

                    productQuantitySite["gsc_onhand"] = onHandAmountSite + 1;
                    productQuantitySite["gsc_available"] = availableAmountSite + 1;

                    _organizationService.Update(productQuantitySite);
                }

                #endregion

                #region Destination Site
                //Retrieve Inventory record
                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_status", "gsc_productquantityid" });

                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    Entity inventory = inventoryRecords.Entities[0];

                    //inventory["gsc_status"] = new OptionSetValue(100000000);

                    //_organizationService.Update(inventory);

                    Guid productQuantityId = inventory.Contains("gsc_productquantityid")
                            ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                            : Guid.Empty;

                    //Retrieve Product Quantity record
                    EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_onhand", "gsc_available" });

                    if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                    {
                        Entity productQuantity = productQuantityRecords.Entities[0];

                        Int32 availableAmount = productQuantity.Contains("gsc_available")
                            ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                            : 1;
                        Int32 onHandAmount = productQuantity.Contains("gsc_onhand")
                            ? productQuantity.GetAttributeValue<Int32>("gsc_onhand")
                            : 1;

                        if (availableAmount != 0)
                        {
                            productQuantity["gsc_available"] = availableAmount - 1;    
                        }

                        if (onHandAmount != 0)
                        {
                            productQuantity["gsc_onhand"] = onHandAmount - 1;    
                        }

                        _organizationService.Update(productQuantity);
                    }
                #endregion

                }
            }

            _tracingService.Trace("Ended AdjustSubmittedInventoryMovement method..");
            return shippingClaimEntity;
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 8/8/2016
        /*Purpose: Adjust Product Quantity on Submitted status
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: SCR Status = gsc_scrstatus
         *      Post/Create:
         * Primary Entity: Shipping Claim
         */
        public Entity AdjustRepairedInventoryMovement(Entity shippingClaimEntity)
        {
            _tracingService.Trace("Started AdjustRepairedInventoryMovement method..");

            if (!shippingClaimEntity.FormattedValues["gsc_scrstatus"].Equals("Repaired")) { return null; }

            Guid siteId = shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? shippingClaimEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                : Guid.Empty;

            //Retrieve Shipping Claim Detail records
            EntityCollection shippingClaimDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_shippingclaimdetail", "gsc_shippingclaimid", shippingClaimEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid", "gsc_modeldescription" });

            if (shippingClaimDetailRecords != null && shippingClaimDetailRecords.Entities.Count > 0)
            {
                Entity shippingClaimDetail = shippingClaimDetailRecords.Entities[0];

                Guid inventoryId = shippingClaimDetail.Contains("gsc_inventoryid")
                    ? shippingClaimDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                    : Guid.Empty;

                String modelDescription = shippingClaimDetail.Contains("gsc_modeldescription")
                    ? shippingClaimDetail.GetAttributeValue<String>("gsc_modeldescription")
                    : String.Empty;

                Guid productId = Guid.Empty;

                //Retrieve Product entity using Model Description
                EntityCollection productRecords = CommonHandler.RetrieveRecordsByOneValue("product", "name", modelDescription, _organizationService, null, OrderType.Ascending,
                    new[] { "productid" });

                if (productRecords != null && productRecords.Entities.Count > 0)
                {
                    Entity product = productRecords.Entities[0];

                    productId = product.Id;
                }

                #region Site
                //Create filter for retrieving Product Quantity
                var productQuantityConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_siteid", ConditionOperator.Equal, siteId),
                        new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId)
                    };

                //Retrieve Product Quantity record using Site + Product
                EntityCollection productQuantitySiteRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_onhand", "gsc_available" });

                if (productQuantitySiteRecords != null && productQuantitySiteRecords.Entities.Count > 0)
                {
                    Entity productQuantitySite = productQuantitySiteRecords.Entities[0];

                    Int32 onHandAmountSite = productQuantitySite.Contains("gsc_onhand")
                        ? productQuantitySite.GetAttributeValue<Int32>("gsc_onhand")
                        : 1;
                    Int32 availableAmountSite = productQuantitySite.Contains("gsc_available")
                        ? productQuantitySite.GetAttributeValue<Int32>("gsc_onhand")
                        : 1;

                    if (onHandAmountSite != 0)
                    {
                        productQuantitySite["gsc_onhand"] = onHandAmountSite - 1;    
                    }

                    if (availableAmountSite != 0)
                    {
                        productQuantitySite["gsc_available"] = availableAmountSite - 1;    
                    }

                    _organizationService.Update(productQuantitySite);
                }
                #endregion

                #region Destination Site
                //Retrieve Inventory record
                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_status", "gsc_productquantityid" });

                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    Entity inventory = inventoryRecords.Entities[0];

                    //inventory["gsc_status"] = new OptionSetValue(100000000);

                    //_organizationService.Update(inventory);

                    Guid productQuantityId = inventory.Contains("gsc_productquantityid")
                            ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                            : Guid.Empty;

                    //Retrieve Product Quantity record
                    EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_onhand", "gsc_available" });

                    if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                    {
                        Entity productQuantity = productQuantityRecords.Entities[0];

                        Int32 availableAmount = productQuantity.Contains("gsc_available")
                            ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                            : 0;
                        Int32 onHandAmount = productQuantity.Contains("gsc_onhand")
                            ? productQuantity.GetAttributeValue<Int32>("gsc_onhand")
                            : 0;
                            
                        productQuantity["gsc_available"] = availableAmount + 1;
                        productQuantity["gsc_onhand"] = onHandAmount + 1;

                        _organizationService.Update(productQuantity);
                    }
                #endregion

                }
            }

            _tracingService.Trace("Ended AdjustRepairedInventoryMovement method..");
            return shippingClaimEntity;
        }
    }
}
