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

namespace GSC.Rover.DMS.BusinessLogic.ReceivingTransaction
{
    public class ReceivingTransactionHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ReceivingTransactionHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 7/11/2016
        //Modified By : Artum M. Ramos, Modified On : 2/1/2017
        /*Purpose: Set record status to 'Inactive'
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: MMPC Status = gsc_mmpcstatus
         *      Post/Create:
         * Primary Entity: Receiving Transaction
         */
        public Entity CancelReceivingTransaction(Entity receivingTransactionEntity)
        {
            _tracingService.Trace("Started CancelReceivingTransaction Method...");
            DateTime transactionDate = DateTime.UtcNow;
            var vpoStatus = receivingTransactionEntity.Contains("gsc_vpostatus")
                ? receivingTransactionEntity.GetAttributeValue<String>("gsc_vpostatus")
                : String.Empty;
            Entity productQuantity = new Entity();

            Guid intransitSite = receivingTransactionEntity.Contains("gsc_intransitsiteid") ? receivingTransactionEntity.GetAttributeValue<EntityReference>("gsc_intransitsiteid").Id : Guid.Empty;

            var purchaseOrderId = CommonHandler.GetEntityReferenceIdSafe(receivingTransactionEntity, "gsc_purchaseorderid");

            _tracingService.Trace("Check if vpoStatus is already Received");
            if (vpoStatus.Equals("Received")) { throw new InvalidPluginExecutionException("Cannot cancel an already received record."); }

            //Retrieve and update Product Quantity through Receiving Transaction Detail record
            EntityCollection receivingTransactionDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransactiondetail", "gsc_receivingtransactionid", receivingTransactionEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid" });

            _tracingService.Trace("Receiving Transaction Detail record count : " + receivingTransactionDetailRecords.Entities.Count.ToString());

            if (receivingTransactionDetailRecords != null && receivingTransactionDetailRecords.Entities.Count > 0)
            {
                Entity receivingTransactionDetail = receivingTransactionDetailRecords.Entities[0];

                var inventoryId = CommonHandler.GetEntityReferenceIdSafe(receivingTransactionDetail, "gsc_inventoryid");

                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_productquantityid" });

                _tracingService.Trace("Inventory record count : " + inventoryRecords.Entities.Count.ToString());

                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    Entity inventory = inventoryRecords.Entities[0];

                    var productQuantityId = CommonHandler.GetEntityReferenceIdSafe(inventory, "gsc_productquantityid");

                    EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_onhand", "gsc_available" });

                    _tracingService.Trace("Product Quantity record count : " + productQuantityRecords.Entities.Count.ToString());

                    if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                    {
                        productQuantity = productQuantityRecords.Entities[0];

                        Int32 onHandCount = productQuantity.Contains("gsc_onhand")
                            ? productQuantity.GetAttributeValue<Int32>("gsc_onhand")
                            : 0;
                        Int32 availableCount = productQuantity.Contains("gsc_available")
                            ? productQuantity.GetAttributeValue<Int32>("gsc_available")
                            : 0;

                        _tracingService.Trace("Adjust Inventory");
                        productQuantity["gsc_onhand"] = onHandCount - 1;
                        productQuantity["gsc_available"] = availableCount - 1;

                        CreateReceivingInventoryHistory(receivingTransactionEntity, transactionDate, 0, 1, intransitSite, Guid.Empty, intransitSite, onHandCount - 1,true);

                        _organizationService.Update(productQuantity);
                        _tracingService.Trace("update ProductQuantity...");
                        _organizationService.Delete(inventory.LogicalName, inventory.Id);
                        _tracingService.Trace("Deleted Associated Inventory record...");
                    }
                }
            }

            //Retrieve and update Purchase Order records
            EntityCollection purchaseOrderRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_vpostatus", "gsc_isreceivedrecordcreated","gsc_productquantityid", "gsc_vpodate", "gsc_vpotype", "gsc_purchaseorderpn" });

            _tracingService.Trace("Retrieve and update Purchase Order records");
            if (purchaseOrderRecords != null && purchaseOrderRecords.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderRecords.Entities[0];

                _tracingService.Trace("Update Purchase Order status to 'Ordered'");
                //Update Purchase Order status to 'Ordered'
                purchaseOrder["gsc_vpostatus"] = new OptionSetValue(100000002);
                purchaseOrder["gsc_isreceivedrecordcreated"] = false;

                _organizationService.Update(purchaseOrder);

                EntityCollection purchaseOrderDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", purchaseOrder.Id,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_productid", "gsc_vehiclecolorid", "gsc_basemodelid", "gsc_modelcode",
                "gsc_optioncode", "gsc_modelyear"});

                if (purchaseOrderDetailsCollection.Entities.Count > 0)
                {
                    InventoryMovementHandler handler = new InventoryMovementHandler(_organizationService, _tracingService);
                    handler.LogInventoryQuantityOnorder(purchaseOrder, purchaseOrderDetailsCollection.Entities[0], productQuantity, true, 100000000);
                }
            }

            //Deactivate Receiving Transaction record
            SetStateRequest setStateRequest = new SetStateRequest();
            setStateRequest.EntityMoniker = new EntityReference("gsc_cmn_receivingtransaction", receivingTransactionEntity.Id);
            setStateRequest.State = new OptionSetValue(1);
            setStateRequest.Status = new OptionSetValue(2);

            _organizationService.Execute(setStateRequest);

            _tracingService.Trace("Ended CancelReceivingTransaction Method...");
            return receivingTransactionEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 7/12/2016
        /*Purpose: Replicate Purchase Order field values
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: Purchase Order = gsc_purchaseorderid
         *      Post/Update: 
         *      Post/Create: 
         * Primary Entity: Receiving Transaction
         */
        public Entity ReplicatePurchaseOrderFields(Entity receivingTransactionEntity)
        {
            _tracingService.Trace("Started ReplicatePurchaseOrderFields Method...");

            //Return if record is created from integration
            if (receivingTransactionEntity.GetAttributeValue<Boolean>("gsc_isintegrated") == true) { return null; }

            var purchaseOrderId = receivingTransactionEntity.GetAttributeValue<EntityReference>("gsc_purchaseorderid") != null
                ? receivingTransactionEntity.GetAttributeValue<EntityReference>("gsc_purchaseorderid").Id
                : Guid.Empty;

            EntityCollection purchaseOrderRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_vpostatus", "gsc_mmpcstatus", "gsc_vendorid", "gsc_vendorname", "gsc_siteid", "gsc_tobranchid", "gsc_todealerid", "gsc_totalamount", "gsc_vatamount",
                "gsc_wtaxamount", "gsc_totalvpoamount", "gsc_netofwtaxamount"});

            _tracingService.Trace("PO records retrieved: " + purchaseOrderRecords.Entities.Count);
            if (purchaseOrderRecords != null && purchaseOrderRecords.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderRecords.Entities[0];

                receivingTransactionEntity["gsc_vpostatus"] = purchaseOrder.Contains("gsc_vpostatus")
                    ? purchaseOrder.FormattedValues["gsc_vpostatus"]
                    : String.Empty;
                receivingTransactionEntity["gsc_mmpcstatus"] = purchaseOrder.Contains("gsc_mmpcstatus")
                    ? purchaseOrder.FormattedValues["gsc_mmpcstatus"]
                    : String.Empty;
                receivingTransactionEntity["gsc_vendorid"] = purchaseOrder.Contains("gsc_vendorid")
                    ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_vendorid")
                    : null;
                receivingTransactionEntity["gsc_vendorname"] = purchaseOrder.Contains("gsc_vendorname")
                    ? purchaseOrder.GetAttributeValue<String>("gsc_vendorname")
                    : String.Empty;
                receivingTransactionEntity["gsc_vendorbranchid"] = purchaseOrder.Contains("gsc_tobranchid")
                    ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_tobranchid")
                    : null;
                receivingTransactionEntity["gsc_vendordealerid"] = purchaseOrder.Contains("gsc_todealerid")
                    ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_todealerid")
                    : null;
                receivingTransactionEntity["gsc_totalamount"] = purchaseOrder.Contains("gsc_totalamount")
                    ? new Money(purchaseOrder.GetAttributeValue<Money>("gsc_totalamount").Value)
                    : new Money(0);
                receivingTransactionEntity["gsc_vatamount"] = purchaseOrder.Contains("gsc_vatamount")
                    ? new Money(purchaseOrder.GetAttributeValue<Money>("gsc_vatamount").Value)
                    : new Money(0);
                receivingTransactionEntity["gsc_wtaxamount"] = purchaseOrder.Contains("gsc_wtaxamount")
                    ? new Money(purchaseOrder.GetAttributeValue<Money>("gsc_wtaxamount").Value)
                    : new Money(0);
                receivingTransactionEntity["gsc_totalvpoamount"] = purchaseOrder.Contains("gsc_totalvpoamount")
                    ? new Money(purchaseOrder.GetAttributeValue<Money>("gsc_totalvpoamount").Value)
                    : new Money(0);
                receivingTransactionEntity["gsc_netofwtaxamount"] = purchaseOrder.Contains("gsc_netofwtaxamount")
                    ? new Money(purchaseOrder.GetAttributeValue<Money>("gsc_netofwtaxamount").Value)
                    : new Money(0);

                if (receivingTransactionEntity.GetAttributeValue<EntityReference>("gsc_siteid") == null)
                {
                    receivingTransactionEntity["gsc_siteid"] = purchaseOrder.Contains("gsc_siteid")
                        ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_siteid")
                        : null;
                }
            }

            EntityCollection purchaseOrderDetails = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", purchaseOrderId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_basemodelid" });

            _tracingService.Trace("PO Detail records retrieved: " + purchaseOrderDetails.Entities.Count);
            if (purchaseOrderDetails.Entities.Count > 0)
            {
                Entity purchaseOrderDetailsEntity = purchaseOrderDetails.Entities[0];
                var baseModelId = purchaseOrderDetailsEntity.Contains("gsc_basemodelid") ? purchaseOrderDetailsEntity.GetAttributeValue<EntityReference>("gsc_basemodelid").Id
                    : Guid.Empty;
                receivingTransactionEntity["gsc_vehiclebasemodelid"] = purchaseOrderDetailsEntity.Contains("gsc_basemodelid") ? new EntityReference("gsc_iv_vehiclebasemodel", baseModelId)
                    : null;
            }

            _tracingService.Trace("Ended ReplicatePurchaseOrderFields Method...");
            return receivingTransactionEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 7/14/2016
        /*Purpose: Replicate Purchase Order Detail field values
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create: Purchase Order = gsc_purchaseorderid 
         * Primary Entity: Receiving Transaction
         */
        public Entity ReplicatePurchaseOrderDetailFields(Entity receivingTransactionEntity)
        {
            _tracingService.Trace("Started ReplicatePurchaseOrderDetailFields Method...");

            //Return if record is created from integration
            if (receivingTransactionEntity.GetAttributeValue<Boolean>("gsc_isintegrated") == true) { return null; }

            var purchaseOrderId = receivingTransactionEntity.GetAttributeValue<EntityReference>("gsc_purchaseorderid") != null
                ? receivingTransactionEntity.GetAttributeValue<EntityReference>("gsc_purchaseorderid").Id
                : Guid.Empty;

            //Replicate fields from Purchase Order Detail to Receiving Transaction Detail
            EntityCollection purchaseOrderDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", purchaseOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_vehiclecolorid", "gsc_modelcode", "gsc_optioncode", "gsc_modelyear", "gsc_basemodelid", "gsc_productid", "gsc_dnpamount" });

            if (purchaseOrderDetailRecords != null && purchaseOrderDetailRecords.Entities.Count > 0)
            {
                foreach (Entity purchaseOrderDetail in purchaseOrderDetailRecords.Entities)
                {
                    Entity receivingTransactionDetail = new Entity("gsc_cmn_receivingtransactiondetail");

                    receivingTransactionDetail["gsc_receivingtransactionid"] = new EntityReference("gsc_cmn_receivingtransaction", receivingTransactionEntity.Id);
                    receivingTransactionDetail["gsc_receivingtransactiondetailpn"] = receivingTransactionEntity.Contains("gsc_receivingtransactionpn")
                        ? receivingTransactionEntity.GetAttributeValue<String>("gsc_receivingtransactionpn")
                        : String.Empty;
                    receivingTransactionDetail["gsc_basemodelid"] = purchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_basemodelid") != null
                        ? purchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_basemodelid")
                        : null;
                    receivingTransactionDetail["gsc_productid"] = purchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                        ? purchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_productid")
                        : null;
                    receivingTransactionDetail["gsc_modelcode"] = purchaseOrderDetail.Contains("gsc_modelcode")
                        ? purchaseOrderDetail.GetAttributeValue<String>("gsc_modelcode")
                        : String.Empty;
                    receivingTransactionDetail["gsc_optioncode"] = purchaseOrderDetail.Contains("gsc_optioncode")
                        ? purchaseOrderDetail.GetAttributeValue<String>("gsc_optioncode")
                        : String.Empty;
                    receivingTransactionDetail["gsc_modelyear"] = purchaseOrderDetail.Contains("gsc_modelyear")
                        ? purchaseOrderDetail.GetAttributeValue<String>("gsc_modelyear")
                        : String.Empty;
                    receivingTransactionDetail["gsc_vehiclecolorid"] = purchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                        ? purchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                        : null;
                    receivingTransactionDetail["gsc_dnpamount"] = purchaseOrderDetail.GetAttributeValue<Money>("gsc_dnpamount") != null
                        ? purchaseOrderDetail.GetAttributeValue<Money>("gsc_dnpamount")
                        : null;


                    _organizationService.Create(receivingTransactionDetail);
                }
            }

            _tracingService.Trace("Ended ReplicatePurchaseOrderDetailFields Method...");
            return receivingTransactionEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 7/20/2016
        /*Purpose: Update inventory count
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create: 
         * Primary Entity: Receiving Transaction
         */
        public Entity PopulateVehicleComponentChecklist(Entity receivingTransactionEntity)
        {
            _tracingService.Trace("Started PopulateVehicleComponentChecklist Method...");

            var pullOutDate = receivingTransactionEntity.GetAttributeValue<DateTime?>("gsc_pulloutdate") != null
                ? receivingTransactionEntity.GetAttributeValue<DateTime?>("gsc_pulloutdate")
                : (DateTime?)null;

            //if (!pullOutDate.HasValue) { return null; }

            //Check if Vehicle Component Checklist records exist
            EntityCollection vehicleComponentChecklistRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_vehiclecomponentchecklist", "gsc_receivingtransactionid", receivingTransactionEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_vehiclecomponentchecklistpn" });

            if (vehicleComponentChecklistRecords != null && vehicleComponentChecklistRecords.Entities.Count > 0)
            {
                //Exit if records exist
                _tracingService.Trace("Exiting...");
                return null;
            }
            else
            {

                //Create custom filter for Vehicle Component entity
                var vehicleComponentConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_vehiclecomponentpn", ConditionOperator.NotNull),
                    new ConditionExpression("gsc_default", ConditionOperator.Equal, true)
                };

                //Retrieve all standard Vehicle Components
                EntityCollection vehicleComponentRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_vehiclecomponent", vehicleComponentConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_vehiclecomponentpn" });

                List<Guid> componentIds = new List<Guid>();

                _tracingService.Trace("Vehicle Component Records Retrieved: " + vehicleComponentRecords.Entities.Count);
                if (vehicleComponentRecords != null && vehicleComponentRecords.Entities.Count > 0)
                {
                    foreach (Entity vehicleComponent in vehicleComponentRecords.Entities)
                    {
                        Entity vehicleComponentChecklist = new Entity("gsc_cmn_receivingtransactionchecklist");
                        vehicleComponentChecklist["gsc_receivingtransactionchecklistpn"] = vehicleComponent.GetAttributeValue<String>("gsc_vehiclecomponentpn");
                        vehicleComponentChecklist["gsc_receivingtransactionid"] = new EntityReference("gsc_cmn_receivingtransaction", receivingTransactionEntity.Id);
                        vehicleComponentChecklist["gsc_vehiclecomponentid"] = new EntityReference("gsc_sls_vehiclecomponent", vehicleComponent.Id);

                        _organizationService.Create(vehicleComponentChecklist);

                        componentIds.Add(vehicleComponent.Id);
                    }
                    _tracingService.Trace("Created Standard components...");
                    
                }

                var baseModelId = receivingTransactionEntity.Contains("gsc_vehiclebasemodelid") ? receivingTransactionEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id
                    : Guid.Empty;
                //Retrieve Components Associated with Base Model
                EntityCollection componentChecklistCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_vehiclecomponentchecklist", "gsc_vehiclebasemodelid", baseModelId,
                    _organizationService, null, OrderType.Ascending, new[] { "gsc_vehiclecomponentid", "gsc_vehiclecomponentchecklistpn" });

                _tracingService.Trace("Base Model Component checklist records retrieved: " + componentChecklistCollection.Entities.Count);
                if (componentChecklistCollection.Entities.Count > 0)
                {
                    foreach (Entity componentCollectionEntity in componentChecklistCollection.Entities)
                    {
                        _tracingService.Trace("Base Model Id: " + baseModelId);
                        var componentId = componentCollectionEntity.Contains("gsc_vehiclecomponentid") ? componentCollectionEntity.GetAttributeValue<EntityReference>("gsc_vehiclecomponentid").Id
                            : Guid.Empty;
                        if (componentId != Guid.Empty && (!componentIds.Contains(componentId)))//component does not exist yet in standard vehicle components
                        {
                            _tracingService.Trace("Base Model Component found...");
                            Entity vehicleComponentChecklist = new Entity("gsc_cmn_receivingtransactionchecklist");
                            vehicleComponentChecklist["gsc_receivingtransactionchecklistpn"] = componentCollectionEntity.GetAttributeValue<String>("gsc_vehiclecomponentchecklistpn");
                            vehicleComponentChecklist["gsc_receivingtransactionid"] = new EntityReference("gsc_cmn_receivingtransaction", receivingTransactionEntity.Id);
                            vehicleComponentChecklist["gsc_vehiclecomponentid"] = new EntityReference("gsc_sls_vehiclecomponent", componentId);

                            _organizationService.Create(vehicleComponentChecklist);
                        }
                    }
                }
            }
            _tracingService.Trace("Ended PopulateVehicleComponentChecklist Method...");

            _organizationService.Update(receivingTransactionEntity);
            return receivingTransactionEntity;
        }

        //Created By: Leslie G. Baliguat, Created On: 02/01/2017
        //Update VPO Status in Receiving
        public void UpdateVPOSatus(Entity vehicleReceiving)
        {
            _tracingService.Trace("Update Receiving Status Copy...");

            Entity statusUpdate = _organizationService.Retrieve(vehicleReceiving.LogicalName, vehicleReceiving.Id,
                new ColumnSet("gsc_vpostatus", "gsc_receivingstatuscopy"));

            var status = vehicleReceiving.GetAttributeValue<OptionSetValue>("gsc_receivingstatus").Value;

            if (status == 100000003 || status == 100000004)
                statusUpdate["gsc_vpostatus"] = vehicleReceiving.FormattedValues["gsc_receivingstatus"];
            else if (status == 100000000)
                statusUpdate["gsc_vpostatus"] = "Ordered";

            statusUpdate["gsc_receivingstatuscopy"] = vehicleReceiving.GetAttributeValue<OptionSetValue>("gsc_receivingstatus");
            _tracingService.Trace(String.Concat(vehicleReceiving.GetAttributeValue<OptionSetValue>("gsc_receivingstatus").Value));

            _organizationService.Update(statusUpdate);
        }

        //Created By: Leslie G. Baliguat, Created On: 02/01/2017
        //Update VPO Status in Purchase Order
        public void UpdateVPOSatusinVPO(Entity vehicleReceiving)
        {
            var purchaseOrderId = CommonHandler.GetEntityReferenceIdSafe(vehicleReceiving, "gsc_purchaseorderid");

            EntityCollection purchaseOrderCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_vpostatus" });

            if (purchaseOrderCollection != null && purchaseOrderCollection.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderCollection.Entities[0];

                purchaseOrder["gsc_vpostatus"] = vehicleReceiving.Contains("gsc_receivingstatus")
                    ? new OptionSetValue(vehicleReceiving.GetAttributeValue<OptionSetValue>("gsc_receivingstatus").Value)
                    : null;

                _organizationService.Update(purchaseOrder);
            }
        }

        //Created By: Leslie G. Baliguat, Created On: 02/02/17
        /*Purpose:Descrease in Unserved PO in Destination Site upon creation
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_receivingstatus
         * Primary Entity: Receiving Transaction
         */
        public Entity SubtractinUnservedPO(Entity vehicleReceiving)
        {
            if (!vehicleReceiving.FormattedValues["gsc_receivingstatus"].Equals("In-Transit")) { return null; }

            _tracingService.Trace("Started SubtractinUnservedPO Method...");

            var purchaseOrderId = vehicleReceiving.GetAttributeValue<EntityReference>("gsc_purchaseorderid") != null
            ? vehicleReceiving.GetAttributeValue<EntityReference>("gsc_purchaseorderid").Id
            : Guid.Empty;

            EntityCollection poRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_productquantityid", "gsc_vpostatus", "gsc_vpodate", "gsc_vpotype", "gsc_purchaseorderpn" });

            if (poRecords != null && poRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Purchase Order...");

                Entity purchaseOrder = poRecords.Entities[0];

                EntityCollection purchaseOrderDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", purchaseOrder.Id,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_productid", "gsc_vehiclecolorid", "gsc_basemodelid", "gsc_modelcode",
                "gsc_optioncode", "gsc_modelyear"});

                var productQuantityId = purchaseOrder.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                : Guid.Empty;

                EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_onhand", "gsc_available", "gsc_allocated", "gsc_onorder", "gsc_sold", "gsc_intransit", "gsc_damaged", "gsc_backorder" });

                if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Product Quantity...");

                    InventoryMovementHandler handler = new InventoryMovementHandler(_organizationService, _tracingService);
                    handler.UpdateProductQuantityDirectly(productQuantityRecords.Entities[0], 0, 0, 0, -1, 0, 0, 0, 0);
                    handler.LogInventoryQuantityOnorder(purchaseOrder, purchaseOrderDetailsCollection.Entities[0], productQuantityRecords.Entities[0], false, 100000006);
                }
            }
            _tracingService.Trace("Ended SubtractinUnservedPO Method...");

            return null;
        }


        //Created By: Leslie G. Baliguat, Created On: 02/03/17
        /*Purpose:Decrease in Onhand and Available in In-Transit Site
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_receivingstatus
         * Primary Entity: Receiving Transaction
         */
        public Entity InventoryMovementUponReceiving(Entity vehicleReceiving)
        {
            if (!vehicleReceiving.FormattedValues["gsc_receivingstatus"].Equals("Received")) { return null; }
            DateTime transactionDate = vehicleReceiving.Contains("gsc_actualreceiptdate") ? vehicleReceiving.GetAttributeValue<DateTime>("gsc_actualreceiptdate") : DateTime.MinValue;
            _tracingService.Trace("Started InventoryMovementUponReceiving Method...");

            Guid intransitSite = vehicleReceiving.Contains("gsc_intransitsiteid") ? vehicleReceiving.GetAttributeValue<EntityReference>("gsc_intransitsiteid").Id : Guid.Empty;
            Guid destinationSite = vehicleReceiving.Contains("gsc_siteid") ? vehicleReceiving.GetAttributeValue<EntityReference>("gsc_siteid").Id : Guid.Empty;
            //Retrieve Receiving Transaction Detail
            EntityCollection receivingTransactionDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransactiondetail", "gsc_receivingtransactionid", vehicleReceiving.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid", "gsc_productid", "gsc_vehiclecolorid", "gsc_basemodelid" });

            if (receivingTransactionDetailRecords != null && receivingTransactionDetailRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Receiving Item...");

                foreach (Entity vehicleReceivingItem in receivingTransactionDetailRecords.Entities)
                {
                    var inventoryId = vehicleReceivingItem.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                        ? vehicleReceivingItem.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                        : Guid.Empty;

                    EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_productquantityid", "gsc_mmpcinvoicedate"});

                    if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                    {
                        _tracingService.Trace("Retrieve Inventory..");

                        Entity inventory = inventoryRecords.Entities[0];
                        InventoryMovementHandler handler = new InventoryMovementHandler(_organizationService, _tracingService);
                        //--Log Inventory History
                        var quantityId = inventory.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                                        ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                                        : Guid.Empty;

                            EntityCollection quantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", quantityId, _organizationService, null, OrderType.Ascending,
                           new[] { "gsc_onhand"});

                            if (quantityRecords != null && quantityRecords.Entities.Count > 0)
                            {
                                _tracingService.Trace("Retrieved Product Quantity Details");
                                Entity quantityEntity = quantityRecords.Entities[0];  
                                Int32 onHand = quantityEntity.Contains("gsc_onhand") ? quantityEntity.GetAttributeValue<Int32>("gsc_onhand"):0;
                                CreateReceivingInventoryHistory(vehicleReceiving, transactionDate, 1, 1, destinationSite, intransitSite, intransitSite, onHand - 1,false);
                            }
                        //--

                        //Decrease in Onhand and Available in In-Transit Site
                        handler.UpdateProductQuantity(inventory, -1, -1, 0, 0, 0, 0, 0, 0);


                        //Change Site from in-transit to destination site
                        Guid productQuantityId = GetProductQuantity(vehicleReceiving, vehicleReceivingItem);

                        if (productQuantityId != Guid.Empty)
                        {
                            inventory["gsc_productquantityid"] = new EntityReference("gsc_iv_productquantity", productQuantityId);
                            inventory["gsc_mmpcinvoicedate"] = vehicleReceiving.GetAttributeValue<DateTime>("gsc_actualreceiptdate"); 
                            _organizationService.Update(inventory);

                            EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_onhand", "gsc_available", "gsc_allocated", "gsc_onorder", "gsc_sold", "gsc_intransit", "gsc_damaged", "gsc_backorder"});

                            if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                            {
                                Entity productQuantity = productQuantityRecords.Entities[0];
                                Int32 onHand = productQuantity.Contains("gsc_onhand") ? productQuantity.GetAttributeValue<Int32>("gsc_onhand") : 0;
                                //Increase in Onhand and Available in Destination Site
                                handler.UpdateProductQuantityDirectly(productQuantityRecords.Entities[0], 1, 1, 0, 0, 0, 0, 0, 0);
                                CreateReceivingInventoryHistory(vehicleReceiving,transactionDate, 1, 1, destinationSite, intransitSite, destinationSite, onHand + 1,true);
                                
                            }
                        }
                    }
                }
            }

            _tracingService.Trace("Ended InventoryMovementUponReceiving Method...");
            return null;
        }

        //Update Site of Inventory from in-transit to destination site
        private Guid GetProductQuantity(Entity vehicleReceiving, Entity receivingItem)
        {
            _tracingService.Trace("Started GetProductQuantity Method..");

            //"gsc_intransitsiteid", "gsc_siteid", "", "gsc_branchid" 
            var siteId = CommonHandler.GetEntityReferenceIdSafe(vehicleReceiving, "gsc_siteid");
            var productId = CommonHandler.GetEntityReferenceIdSafe(receivingItem, "gsc_productid");
            var colorId = CommonHandler.GetEntityReferenceIdSafe(receivingItem, "gsc_vehiclecolorid");
            var dealerId = CommonHandler.GetEntityReferenceIdSafe(vehicleReceiving, "gsc_dealerid");
            var branchId = CommonHandler.GetEntityReferenceIdSafe(vehicleReceiving, "gsc_branchid");
            var baseModelId = CommonHandler.GetEntityReferenceIdSafe(receivingItem, "gsc_basemodelid");

            var productQuantityConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_siteid", ConditionOperator.Equal, siteId),
                    new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                    new ConditionExpression("gsc_vehiclecolorid", ConditionOperator.Equal, colorId),
                    //new ConditionExpression("gsc_isonorder", ConditionOperator.Equal, false)
                };

            EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_onhand", "gsc_available", "gsc_allocated", "gsc_onorder", "gsc_sold", "gsc_intransit", "gsc_damaged", "gsc_backorder" });

            if (productQuantityCollection != null && productQuantityCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Product Quantity Retrieved..");

                return productQuantityCollection.Entities[0].Id;
            }
            else
            {
                _tracingService.Trace("Create Quantity Retrieved..");

                String productName = receivingItem.Contains("gsc_productid")
                      ? receivingItem.GetAttributeValue<EntityReference>("gsc_productid").Name
                      : String.Empty;
                String siteName = vehicleReceiving.GetAttributeValue<EntityReference>("gsc_siteid") != null
                    ? vehicleReceiving.GetAttributeValue<EntityReference>("gsc_siteid").Name
                    : String.Empty;

                Entity productQuantityEntity = new Entity("gsc_iv_productquantity");

                productQuantityEntity["gsc_productquantitypn"] = productName;
                productQuantityEntity["gsc_siteid"] = siteId != Guid.Empty
                    ? new EntityReference("gsc_iv_site", siteId)
                    : null;
                productQuantityEntity["gsc_productid"] = productId != Guid.Empty
                    ? new EntityReference("product", productId)
                    : null;
                productQuantityEntity["gsc_vehiclecolorid"] = colorId != Guid.Empty
                    ? new EntityReference("gsc_cmn_vehiclecolor", colorId)
                    : null;
                productQuantityEntity["gsc_vehiclemodelid"] = baseModelId != Guid.Empty
                    ? new EntityReference("gsc_iv_vehiclebasemodel", baseModelId)
                    : null;
                productQuantityEntity["gsc_onorder"] = 0;
                productQuantityEntity["gsc_onhand"] = 0;
                productQuantityEntity["gsc_available"] = 0;
                productQuantityEntity["gsc_allocated"] = 0;
                productQuantityEntity["gsc_sold"] = 0;
                productQuantityEntity["gsc_dealerid"] = dealerId != Guid.Empty
                    ? new EntityReference("account", dealerId)
                    : null;
                productQuantityEntity["gsc_branchid"] = branchId != Guid.Empty
                    ? new EntityReference("account", branchId)
                    : null;
                productQuantityEntity["gsc_recordownerid"] = vehicleReceiving.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                    ? vehicleReceiving.GetAttributeValue<EntityReference>("gsc_recordownerid")
                    : null;

                _tracingService.Trace("Created New Product QUantity");
                _tracingService.Trace("Quantity Retrieved Created...");
                return _organizationService.Create(productQuantityEntity);
            }

            _tracingService.Trace("Edned GetProductQuantity Method..");

            return Guid.Empty;
        }

        //Created By : Jessica Casupanan, Created On : 02/03/2017
        /*Purpose: Validate Delete 
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create:
         * Primary Entity: Receiving Transaction
         */
        public bool IsAllocatedVehicle(Entity ReceivingTransaction)
        {
            _tracingService.Trace("ValidateDelete Method Started ..");

            EntityCollection ReceivingTransactionDetailEC = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransactiondetail", "gsc_receivingtransactionid", ReceivingTransaction.Id, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_inventoryid" });

            if (ReceivingTransactionDetailEC != null && ReceivingTransactionDetailEC.Entities.Count > 0)
            {
                Entity receivingTransactionDetail = ReceivingTransactionDetailEC.Entities[0];

                Guid inventoryId = receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_inventoryid") != null 
                    ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_inventoryid").Id : Guid.Empty;

                EntityCollection inventoryEC = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_status" });
                if (inventoryEC != null && inventoryEC.Entities.Count > 0)
                {
                    Entity inventoryEntity = inventoryEC.Entities[0];
                    var inventoryStatus = inventoryEntity.Contains("gsc_status") ? inventoryEntity.GetAttributeValue<OptionSetValue>("gsc_status").Value
                    : 0;
                    if (inventoryStatus == 100000001)
                        return true;
                }
                else
                {
                    throw new InvalidPluginExecutionException("No Inventory");
                }
            }
            _tracingService.Trace("ValidateDelete Method Ended ..");
            return false;
        }

        //Created By: Leslie G. Baliguat, Created On: 02/06/2017
        /*Purpose: Modify Purhcase Order Item and Computation
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_receivingstatus
         * Primary Entity: Receiving Transaction
         */
        public void UpdatePOItem(Entity vehicleReceiving)
        {
            if (!vehicleReceiving.FormattedValues["gsc_receivingstatus"].Equals("Received")) { return; }

            var purchaseOrderId = vehicleReceiving.GetAttributeValue<EntityReference>("gsc_purchaseorderid") != null
            ? vehicleReceiving.GetAttributeValue<EntityReference>("gsc_purchaseorderid").Id
            : Guid.Empty;

            EntityCollection poItemRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", purchaseOrderId, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_productid" });

            if (poItemRecords != null && poItemRecords.Entities.Count > 0)
            {
                Entity poItemEntity = poItemRecords.Entities[0];

                var poItemId = CommonHandler.GetEntityReferenceIdSafe(poItemEntity, "gsc_productid");
                EntityCollection receivingTransactionDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransactiondetail", "gsc_receivingtransactionid", vehicleReceiving.Id, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_productid" });

                if (receivingTransactionDetailRecords != null && receivingTransactionDetailRecords.Entities.Count > 0)
                {
                    Entity receivingItem = receivingTransactionDetailRecords.Entities[0];

                    var receivingItemId = CommonHandler.GetEntityReferenceIdSafe(receivingItem, "gsc_productid");

                    if (poItemId != receivingItemId)
                    {
                        poItemEntity["gsc_productid"] = new EntityReference("product", receivingItemId);
                        _organizationService.Update(poItemEntity);
                    }
                }
            }
        }

        //Created By: Raphael Herrera, Created On: 02/06/2017
        /*Purpose: Update product quantity and PO records 
         * Registration Details: 
         * Event/Message:
         *      Pre/Delete: Receiving Transaction
         * Primary Entity: Receiving Transaction
         */
        public Entity DeleteReceivingTransaction(Entity receivingTransactionEntity)
        {
            _tracingService.Trace("Starting DeleteReceivingTransaction Method...");

            EntityCollection receivingDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransactiondetail", "gsc_receivingtransactionid", receivingTransactionEntity.Id,
                            _organizationService, null, OrderType.Ascending, new[] { "gsc_receivingtransactiondetailpn", "gsc_inventoryid" });

            _tracingService.Trace("Receiving Details records retrieved: " + receivingDetailsCollection.Entities.Count);
            if (receivingDetailsCollection.Entities.Count > 0)
            {
                Entity receivingDetail = receivingDetailsCollection.Entities[0];

                var inventoryId = CommonHandler.GetEntityReferenceIdSafe(receivingDetail, "gsc_inventoryid");

                EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_productquantityid" });

                _tracingService.Trace("Inventory Records retrieved: " + inventoryCollection.Entities.Count);
                if (inventoryCollection.Entities.Count > 0)
                {
                    Entity inventoryEntity = inventoryCollection.Entities[0];
                    var productQuantityId = CommonHandler.GetEntityReferenceIdSafe(inventoryEntity, "gsc_productquantityid");

                    EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId,
                        _organizationService, null, OrderType.Ascending, new[] { "gsc_available", "gsc_onhand" });//update necessary fields as needed

                    if (productQuantityCollection.Entities.Count > 0)
                    {
                        Entity productQuantityEntity = productQuantityCollection.Entities[0];
                        Int32 onHand = productQuantityEntity.Contains("gsc_onhand") ? productQuantityEntity.GetAttributeValue<Int32>("gsc_onhand")
                            : 0;
                        Int32 available = productQuantityEntity.Contains("gsc_available") ? productQuantityEntity.GetAttributeValue<Int32>("gsc_available")
                            : 0;
                        //Update product quantity
                        productQuantityEntity["gsc_onhand"] = onHand - 1;
                        productQuantityEntity["gsc_available"] = available - 1;
                                               
                        _organizationService.Update(productQuantityEntity);
                        _tracingService.Trace("Updated product quantity count");
                        _organizationService.Delete(inventoryEntity.LogicalName, inventoryEntity.Id);
                        _tracingService.Trace("Deleted Associated Inventory record...");
                    }
                    else
                        _tracingService.Trace("Inventory has no associated product quantity record..");
                }  
                _organizationService.Delete(receivingDetail.LogicalName, receivingDetail.Id);
                _tracingService.Trace("Deleted receiving details record.");
            }

            var purchaseOrderId = CommonHandler.GetEntityReferenceIdSafe(receivingTransactionEntity, "gsc_purchaseorderid");

            EntityCollection purchaseOrderCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_vpostatus", "gsc_isreceivedrecordcreated" });

            _tracingService.Trace("PO records retrieved: " + purchaseOrderCollection.Entities.Count);
            if (purchaseOrderCollection.Entities.Count > 0)
            {
                Entity purchaseOrderEntity = purchaseOrderCollection.Entities[0];
                purchaseOrderEntity["gsc_vpostatus"] = new OptionSetValue(100000002);//revert status to ordered
                purchaseOrderEntity["gsc_isreceivedrecordcreated"] = false;

                _organizationService.Update(purchaseOrderEntity);
                _tracingService.Trace("Reverted VPO Status to ordered");
            }

            _tracingService.Trace("Ending DeleteReceivingTransaction Method");
            return receivingTransactionEntity;
        }

        public Entity DeleteReceivingComponents(Entity receivingTransactionEntity)
        {
            _tracingService.Trace("Starting DeleteReceivingComponents Method");
            EntityCollection receivingComponentCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransactionchecklist", "gsc_receivingtransactionid", receivingTransactionEntity.Id,
                            _organizationService, null, OrderType.Ascending, new[] { "gsc_receivingtransactionchecklistpn" });

            _tracingService.Trace("Receiving Details records retrieved: " + receivingComponentCollection.Entities.Count);
            if (receivingComponentCollection.Entities.Count > 0)
            {
                foreach (Entity receivingComponent in receivingComponentCollection.Entities)
                {
                    _organizationService.Delete(receivingComponent.LogicalName, receivingComponent.Id);
                }

            }
            _tracingService.Trace("Ending DeleteReceivingComponents Method...");
            return receivingTransactionEntity;
        }

        public Entity TagVPOReceivingStatusCreated(Entity vehicleReceiving)
        {
            var purchaseOrderId = CommonHandler.GetEntityReferenceIdSafe(vehicleReceiving, "gsc_purchaseorderid");

            EntityCollection purchaseOrderCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_isreceivedrecordcreated" });

            if (purchaseOrderCollection != null && purchaseOrderCollection.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderCollection.Entities[0];

                purchaseOrder["gsc_isreceivedrecordcreated"] = true;

                _organizationService.Update(purchaseOrder);
            }

            return vehicleReceiving;
        }

        public void CreateReceivingInventoryHistory(Entity vehicleReceiving, DateTime transactionDate, Int32 Qin, Int32 QOut, Guid toSite, Guid fromSite, Guid site, Int32 onHand, bool displayAllSite)
        {
            _tracingService.Trace("Started CreateReceivingInventoryHistory Method...");
            //Retrieved Receiving details
            String transactionNumber = vehicleReceiving.Contains("gsc_receivingtransactionpn") ? vehicleReceiving.GetAttributeValue<String>("gsc_receivingtransactionpn") : String.Empty;
            String vendorId = vehicleReceiving.Contains("gsc_vendorid") ? vehicleReceiving.GetAttributeValue<EntityReference>("gsc_vendorid").Name : String.Empty;
            String vendorName = vehicleReceiving.Contains("gsc_vendorname") ? vehicleReceiving.GetAttributeValue<String>("gsc_vendorname") : String.Empty;

            EntityCollection vehicleReceivingDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransactiondetail", "gsc_receivingtransactionid", vehicleReceiving.Id, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_inventoryid" });

            if (vehicleReceivingDetailRecords != null && vehicleReceivingDetailRecords.Entities.Count > 0)
            {
                _tracingService.Trace("vehicle Receiving Detail Records Retrieved...");
                Entity receivingTransactionDetail = vehicleReceivingDetailRecords.Entities[0];

                var inventoryId = CommonHandler.GetEntityReferenceIdSafe(receivingTransactionDetail, "gsc_inventoryid");

                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_productquantityid", "gsc_modelcode", "gsc_optioncode" });

                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Inventory Records retrieved...");
                    Entity inventory = inventoryRecords.Entities[0];

                    var productQuantityId = CommonHandler.GetEntityReferenceIdSafe(inventory, "gsc_productquantityid");

                    EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_onhand", "gsc_siteid", "gsc_vehiclecolorid", "gsc_vehiclemodelid", "gsc_productid" });

                    _tracingService.Trace("Product Quantity record count : " + productQuantityRecords.Entities.Count.ToString());

                    if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                    {
                        _tracingService.Trace("Product Quantity Records Retrieved...");
                        Entity productQuantity = productQuantityRecords.Entities[0];
                        //Log Inventory History  
                        InventoryMovementHandler inventoryMovementHandler = new InventoryMovementHandler(_organizationService, _tracingService);
                        inventoryMovementHandler.CreateInventoryHistory("Vehicle Receiving", vendorId, vendorName, transactionNumber, transactionDate, QOut, Qin, onHand, toSite,fromSite,site, inventory, productQuantity, true,displayAllSite);
                  
                    }
                }
            }
            //throw new InvalidPluginExecutionException("test");
            _tracingService.Trace("Ended CreateReceivingInventoryHistory Method...");
            
        }
    }
}
