using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.InventoryMovement;
using GSC.Rover.DMS.BusinessLogic.PriceList;
using GSC.Rover.DMS.BusinessLogic.ReceivingTransaction;
using System.Text.RegularExpressions;

namespace GSC.Rover.DMS.BusinessLogic.ReceivingTransactionDetail
{
    public class ReceivingTransactionDetailHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ReceivingTransactionDetailHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 6/29/2016
        /*Purpose: Set newly created Receiving Transaction Detail fields
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: Invoice = gsc_invoiceid
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Receiving Transaction Detail_
         */
        public Entity SetReceivingTransactionDetailFields(Entity receivingTransactionDetailEntity)
        {
            _tracingService.Trace("Started SetReceivingTransactionDetailName Method...");

            var inventoryId = receivingTransactionDetailEntity.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                ? receivingTransactionDetailEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                : Guid.Empty;

            EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventorypn" });

            if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
            {
                Entity inventory = inventoryRecords.Entities[0];
                receivingTransactionDetailEntity["gsc_receivingtransactiondetailpn"] = inventory.GetAttributeValue<String>("gsc_inventorypn");
            }

            _tracingService.Trace("Ended SetReceivingTransactionDetailName Method...");
            return receivingTransactionDetailEntity;
        }


        //Created By: Leslie G. Baliguat, Created On: 02/02/17
        //Increase in on-hand and available in In-transit Site Upon creation
        public void AddOnHandAvailable(Entity vehicleReceivingItem)
        {
            var inventoryId = vehicleReceivingItem.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                ? vehicleReceivingItem.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                : Guid.Empty;

            EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventorypn" });

            if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
            {
                Entity inventory = inventoryRecords.Entities[0];
                InventoryMovementHandler handler = new InventoryMovementHandler(_organizationService, _tracingService);
                handler.UpdateProductQuantity(inventory, 1, 1, 0, 0, 0, 0, 0, 0);
            }
        }

        //Descrease in Unserved PO in Destination Site
        public void SubtractinUnservedPO(Entity vehicleReceivingItem)
        {
            var vehicleReceivingId = vehicleReceivingItem.GetAttributeValue<EntityReference>("gsc_receivingtransactionid") != null
                ? vehicleReceivingItem.GetAttributeValue<EntityReference>("gsc_receivingtransactionid").Id
                : Guid.Empty;

            EntityCollection receivingRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransaction", "gsc_cmn_receivingtransactionid", vehicleReceivingId, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_purchaseorderid" });

            if (receivingRecords != null && receivingRecords.Entities.Count > 0)
            {
                Entity vehicleReceiving = receivingRecords.Entities[0];

                var purchaseOrderId = vehicleReceiving.GetAttributeValue<EntityReference>("gsc_purchaseorderid") != null
                ? vehicleReceiving.GetAttributeValue<EntityReference>("gsc_purchaseorderid").Id
                : Guid.Empty;

                EntityCollection poRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId, _organizationService, null, OrderType.Ascending,
                   new[] { "gsc_productquantityid" });

                if (poRecords != null && poRecords.Entities.Count > 0)
                {
                    Entity purchaseOrder = poRecords.Entities[0];

                    var productQuantityId = purchaseOrder.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                    ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                    : Guid.Empty;

                    EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                   new[] { "gsc_onhand", "gsc_available", "gsc_allocated", "gsc_onorder", "gsc_sold", "gsc_intransit", "gsc_damaged", "gsc_backorder" });

                     if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                     {
                         InventoryMovementHandler handler = new InventoryMovementHandler(_organizationService, _tracingService);
                         handler.UpdateProductQuantityDirectly(productQuantityRecords.Entities[0], 0, 0, 0, -1, 0, 0, 0, 0);
                     }
                }
            }
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 2/3/2017
        /*Purpose: Create Inventory record on Receiving Transaction Detail update
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: Model Code, Option Code, CS No, VIN, Production No, Engine No
         *      Post/Create:
         * Primary Entity: Receiving Transaction Detail
         */
        public Entity CreateUpdateInventoryRecord(Entity receivingTransactionDetail)
        {
            _tracingService.Trace("Started CreateUpdateInventoryRecord Method...");
            ReceivingTransactionHandler receivingHandler = new ReceivingTransactionHandler(_organizationService, _tracingService);
            Guid receivingTransactionId = receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_receivingtransactionid").Id;

            //Retrieve In-Transit Site field from Receiving Transaction record
            Entity receivingTransaction = _organizationService.Retrieve("gsc_cmn_receivingtransaction", receivingTransactionId, new ColumnSet("gsc_intransitsiteid", "gsc_receivingstatus", "gsc_receivingtransactionpn", "gsc_vendorid", "gsc_vendorname", "gsc_intransitreceiptdate"));

            if (!receivingTransaction.FormattedValues["gsc_receivingstatus"].Equals("Open"))
            {
                _tracingService.Trace("Status not Open.");
                return null;
            }

            String productName = receivingTransactionDetail.Contains("gsc_productid")
                ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_productid").Name
                : String.Empty;

            String colorName = receivingTransactionDetail.Contains("gsc_vehiclecolorid")
                ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Name
                : String.Empty;

            String inTransitSiteName = receivingTransaction.Contains("gsc_intransitsiteid")
                ? receivingTransaction.GetAttributeValue<EntityReference>("gsc_intransitsiteid").Name
                : String.Empty;

            EntityReference inventoryId = receivingTransactionDetail.Contains("gsc_inventoryid")
                ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_inventoryid")
                : null;

            Guid inTransitSite = receivingTransaction.Contains("gsc_intransitsiteid") ? receivingTransaction.GetAttributeValue<EntityReference>("gsc_intransitsiteid").Id
                : Guid.Empty;

            DateTime transactionDate = receivingTransaction.Contains("gsc_intransitreceiptdate") ? receivingTransaction.GetAttributeValue<DateTime>("gsc_intransitreceiptdate") : DateTime.MinValue;

            if (inventoryId == null)
            {
                _tracingService.Trace("Create Inventory.");

                Int32 onHand = 0;
                Int32 available = 0;
                //Create new inventory record
                Entity inventory = new Entity("gsc_iv_inventory");
                inventory["gsc_inventorypn"] = productName + "-" + colorName + "-" + inTransitSiteName;
                inventory["gsc_color"] = colorName;
                inventory["gsc_vin"] = receivingTransactionDetail.Contains("gsc_vin")
                    ? receivingTransactionDetail.GetAttributeValue<String>("gsc_vin")
                    : String.Empty;
                inventory["gsc_engineno"] = receivingTransactionDetail.Contains("gsc_engineno")
                    ? receivingTransactionDetail.GetAttributeValue<String>("gsc_engineno")
                    : String.Empty;
                inventory["gsc_optioncode"] = receivingTransactionDetail.Contains("gsc_optioncode")
                    ? receivingTransactionDetail.GetAttributeValue<String>("gsc_optioncode")
                    : String.Empty;
                inventory["gsc_csno"] = receivingTransactionDetail.Contains("gsc_csno")
                    ? receivingTransactionDetail.GetAttributeValue<String>("gsc_csno")
                    : String.Empty;
                inventory["gsc_modelcode"] = receivingTransactionDetail.Contains("gsc_modelcode")
                    ? receivingTransactionDetail.GetAttributeValue<String>("gsc_modelcode")
                    : String.Empty;
                inventory["gsc_productionno"] = receivingTransactionDetail.Contains("gsc_productionno")
                    ? receivingTransactionDetail.GetAttributeValue<String>("gsc_productionno")
                    : String.Empty;
                inventory["gsc_modelyear"] = receivingTransactionDetail.Contains("gsc_modelyear")
                    ? receivingTransactionDetail.GetAttributeValue<String>("gsc_modelyear")
                    : String.Empty;
                inventory["gsc_siteid"] = receivingTransaction.Contains("gsc_intransitsiteid")
                    ? receivingTransaction.GetAttributeValue<EntityReference>("gsc_intransitsiteid")
                    : null;

                Guid newInventory = _organizationService.Create(inventory);

                _tracingService.Trace("Inventory Created");

                Entity newInventoryEntity = _organizationService.Retrieve("gsc_iv_inventory", newInventory, new ColumnSet("gsc_productquantityid"));
                
                receivingTransactionDetail["gsc_inventoryid"] = new EntityReference("gsc_iv_inventory", newInventory);

                _organizationService.Update(receivingTransactionDetail);

                _tracingService.Trace("Updated Receiving Item Inventory");

                //Reference inventory to product quantity
                Guid productId = receivingTransactionDetail.Contains("gsc_productid")
                    ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty;

                Guid inTransitSiteId = receivingTransaction.Contains("gsc_intransitsiteid")
                    ? receivingTransaction.GetAttributeValue<EntityReference>("gsc_intransitsiteid").Id
                    : Guid.Empty;

                Guid colorId = receivingTransactionDetail.Contains("gsc_vehiclecolorid")
                    ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id
                    : Guid.Empty;

                var productQuantityFilter = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                    new ConditionExpression("gsc_siteid", ConditionOperator.Equal, inTransitSiteId),
                    new ConditionExpression("gsc_vehiclecolorid", ConditionOperator.Equal, colorId),
                    new ConditionExpression("gsc_isonorder", ConditionOperator.Equal, false)
                };

                EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityFilter, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_iv_productquantityid", "gsc_onhand", "gsc_available", "gsc_allocated", "gsc_onorder", "gsc_sold", "gsc_intransit", "gsc_damaged", "gsc_backorder" });

                _tracingService.Trace("Product Quantity record/s retrieved : " + productQuantityRecords.Entities.Count);
                
                if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Product Quantity");

                    Entity productQuantity = productQuantityRecords.Entities[0];

                    newInventoryEntity["gsc_productquantityid"] = new EntityReference("gsc_iv_productquantity", productQuantity.Id);

                    _organizationService.Update(newInventoryEntity);

                    _tracingService.Trace("Inventory Product Quantity Updated");

                    onHand = productQuantity.GetAttributeValue<Int32>("gsc_onhand") + 1;
                    available = productQuantity.GetAttributeValue<Int32>("gsc_available") + 1;
                    productQuantity["gsc_onhand"] = onHand;
                    productQuantity["gsc_available"] = available;

                    _organizationService.Update(productQuantity);

                    _tracingService.Trace("Product Quantity Updated");
                }
                else
                {
                    _tracingService.Trace("Create Product Quantity");

                    Entity productQuantity = new Entity("gsc_iv_productquantity");
                    productQuantity["gsc_productquantitypn"] = productName;
                    productQuantity["gsc_siteid"] = receivingTransaction.Contains("gsc_intransitsiteid")
                        ? receivingTransaction.GetAttributeValue<EntityReference>("gsc_intransitsiteid")
                        : null;
                    productQuantity["gsc_productid"] = receivingTransactionDetail.Contains("gsc_productid")
                        ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_productid")
                        : null;
                    productQuantity["gsc_vehiclecolorid"] = receivingTransactionDetail.Contains("gsc_vehiclecolorid")
                        ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                        : null;
                    productQuantity["gsc_vehiclemodelid"] = receivingTransactionDetail.Contains("gsc_basemodelid")
                        ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_basemodelid")
                        : null;

                    Guid newProductQuantity = _organizationService.Create(productQuantity);

                    newInventoryEntity["gsc_productquantityid"] = new EntityReference("gsc_iv_productquantity", newProductQuantity);

                    _organizationService.Update(newInventoryEntity);

                    _tracingService.Trace("Inventory Product Quantity Updated");

                    EntityCollection productQuantity2Records = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", newProductQuantity, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_onhand", "gsc_available", "gsc_allocated", "gsc_onorder", "gsc_sold", "gsc_intransit", "gsc_damaged", "gsc_backorder" });

                     if (productQuantity2Records != null && productQuantity2Records.Entities.Count > 0)
                     {
                         Entity productQuantity2 = productQuantity2Records.Entities[0];

                         onHand = productQuantity2.GetAttributeValue<Int32>("gsc_onhand") + 1;
                         available = productQuantity2.GetAttributeValue<Int32>("gsc_available") + 1;
                         productQuantity2["gsc_onhand"] = onHand;
                         productQuantity2["gsc_available"] = available;

                         _organizationService.Update(productQuantity2);

                         _tracingService.Trace("Product Quantity Updated");
                     }
                }
                receivingHandler.CreateReceivingInventoryHistory(receivingTransaction,transactionDate, 1, 0, inTransitSite, Guid.Empty, inTransitSite, onHand,true);

            }
            else
            {
                _tracingService.Trace("Update Inventory");

                //Update existing inventory record
                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_productquantityid" });

                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Inventory");

                    Entity inventory = inventoryRecords.Entities[0];
                    inventory["gsc_inventorypn"] = productName + "-" + colorName + "-" + inTransitSiteName;
                    inventory["gsc_color"] = receivingTransactionDetail.Contains("gsc_vehiclecolorid")
                        ? receivingTransactionDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Name
                        : String.Empty;
                    inventory["gsc_vin"] = receivingTransactionDetail.Contains("gsc_vin")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_vin")
                        : String.Empty;
                    inventory["gsc_engineno"] = receivingTransactionDetail.Contains("gsc_engineno")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_engineno")
                        : String.Empty;
                    inventory["gsc_optioncode"] = receivingTransactionDetail.Contains("gsc_optioncode")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_optioncode")
                        : String.Empty;
                    inventory["gsc_csno"] = receivingTransactionDetail.Contains("gsc_csno")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_csno")
                        : String.Empty;
                    inventory["gsc_modelcode"] = receivingTransactionDetail.Contains("gsc_modelcode")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_modelcode")
                        : String.Empty;
                    inventory["gsc_productionno"] = receivingTransactionDetail.Contains("gsc_productionno")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_productionno")
                        : String.Empty;
                    inventory["gsc_modelyear"] = receivingTransactionDetail.Contains("gsc_modelyear")
                        ? receivingTransactionDetail.GetAttributeValue<String>("gsc_modelyear")
                        : String.Empty;
                    inventory["gsc_siteid"] = receivingTransaction.Contains("gsc_intransitsiteid")
                        ? receivingTransaction.GetAttributeValue<EntityReference>("gsc_intransitsiteid")
                        : null;

                    _organizationService.Update(inventory);
                    _tracingService.Trace("Inventory Updated");
                }

                //receivingHandler.CreateReceivingInventoryHistory(receivingTransaction, 1, 0, inTransitSite, Guid.Empty, inTransitSite, 1);
            }

            _tracingService.Trace("Created Receiving Inventory History...");

            receivingTransaction["gsc_receivingstatus"] = new OptionSetValue(100000003);

            _organizationService.Update(receivingTransaction);

            _tracingService.Trace("Receving Status Updated");

            _tracingService.Trace("Ended CreateUpdateInventoryRecord Method...");
            return receivingTransactionDetail;
        }

        //Created By: Leslie G. Baliguat
        //Create By: Raphael Herrera, Created On: 2/06/2017 /*
        /* Purpose:  Set DNP Amount
         * Registration Details:
         * Event/Message: 
         *      Post/Update: productid
         * Primary Entity: ReceivingDetails
         */
        public Entity SetDNPAmount(Entity receivingDetail)
        {
            _tracingService.Trace("Started SetDNPAmount method...");

            var productId = receivingDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? receivingDetail.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;

            Money amount = new Money();
            var dateToday = DateTime.Now.ToShortDateString();

            PriceListHandler priceListHandler = new PriceListHandler(_organizationService, _tracingService);
            priceListHandler.itemType = 0;
            priceListHandler.productFieldName = "gsc_productid";
            List<Entity> latestPriceList = priceListHandler.RetrievePriceList(receivingDetail, 100000001, 0);

            if (latestPriceList.Count > 0)
            {
                Entity priceListItem = latestPriceList[0];
                Entity priceList = latestPriceList[1];

                amount = new Money(priceListItem.GetAttributeValue<Money>("amount").Value);

                receivingDetail["gsc_dnpamount"] = amount;

                _tracingService.Trace("Updated DNP Amount to " + amount.Value);

                _tracingService.Trace("Ending SetDNPAmount method...");

                ComputeVAT(receivingDetail, latestPriceList);
            }
            else
            {
                throw new InvalidPluginExecutionException("There is no effecive DNP Price List for the selected Vehicle.");
            }

            //Get Price List where begin date is less than or equal to current date and end date is greater than than or equak to current date
            /* var priceLevelConditionList = new List<ConditionExpression>
                 {
                     new ConditionExpression("gsc_transactiontype", ConditionOperator.Equal, 100000001),
                     new ConditionExpression("begindate", ConditionOperator.LessEqual, dateToday),
                     new ConditionExpression("enddate", ConditionOperator.GreaterEqual, dateToday)
                 };

             EntityCollection priceListCollection = CommonHandler.RetrieveRecordsByConditions("pricelevel", priceLevelConditionList, _organizationService, "begindate", OrderType.Descending,
                         new[] { "gsc_transactiontype", "begindate", "enddate", "gsc_taxstatus" });

             _tracingService.Trace(priceListCollection.Entities.Count + " price list records retrieved...");

             if (priceListCollection != null && priceListCollection.Entities.Count > 0)
             {
                 Entity priceListEntity = priceListCollection.Entities[0];
                 var priceListItemConditionList = new List<ConditionExpression>
                 {
                     new ConditionExpression("pricelevelid", ConditionOperator.Equal, priceListEntity.Id),
                     new ConditionExpression("productid", ConditionOperator.Equal, productId)
                 };

                 EntityCollection priceListItemCollection = CommonHandler.RetrieveRecordsByConditions("productpricelevel", priceListItemConditionList, _organizationService, null, OrderType.Ascending,
                       new[] { "amount" });

                 _tracingService.Trace(priceListItemCollection.Entities.Count + " price list item records retrieved...");

                 if (priceListItemCollection.Entities.Count > 0)
                 {
                     amount = new Money(priceListItemCollection.Entities[0].GetAttributeValue<Money>("amount").Value);
                     receivingDetail["gsc_dnpamount"] = amount;
                     _tracingService.Trace("Updated DNP Amount to " + amount.Value);
                     ComputeVAT(receivingDetail, priceListEntity);

                 }
                 else
                     throw new InvalidPluginExecutionException("Vehicle selected is not yet included in the most recent DNP Price List.");
             }
             else
                 throw new InvalidPluginExecutionException("There is no effecive DNP Price List.");*/


            _tracingService.Trace("Ending SetDNPAmount method...");
            return receivingDetail;

        }

        //Create By: Raphael Herrera, Created On: 2/06/2017 /*
        /* Purpose:  Compute for VAT related fields of Receiving Transaction
         * Registration Details:
         * Event/Message: 
         *      Post/Update: productid
         * Primary Entity: ReceivingDetails
         */
        private Entity ComputeVAT(Entity receivingDetailEntity, List<Entity> latestPriceList)
        {
            _tracingService.Trace("Started ComputeVAT method...");

            var receivingId = CommonHandler.GetEntityReferenceIdSafe(receivingDetailEntity, "gsc_receivingtransactionid");

            EntityCollection receivingTransactionCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_receivingtransaction", "gsc_cmn_receivingtransactionid", receivingId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_totalamount", "gsc_vatamount", "gsc_totalvpoamount", "gsc_wtaxamount", "gsc_netofwtaxamount", "gsc_branchid" });

            _tracingService.Trace("Receiving Transaction records retrieved: " + receivingTransactionCollection.Entities.Count);
            if (receivingTransactionCollection.Entities.Count > 0)
            {
                Entity receivingTransaction = receivingTransactionCollection.Entities[0];

                Guid productId = CommonHandler.GetEntityReferenceIdSafe(receivingDetailEntity, "gsc_productid");
                Guid branchId = CommonHandler.GetEntityReferenceIdSafe(receivingTransaction, "gsc_branchid");
                var dateToday = DateTime.Now.ToShortDateString();

                Entity priceListItem = latestPriceList[0];
                Entity priceList = latestPriceList[1];

                //get tax id of purchase order branch
                EntityCollection branchCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", branchId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_taxid", "gsc_taxrate", "gsc_vehiclewithholdingtaxrate", "gsc_vehiclewithholdingtaxid" });
                //get vehicle tax id
                EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_taxid", "gsc_taxrate" });

                _tracingService.Trace("branch records: " + branchCollection.Entities.Count);
                _tracingService.Trace("product records: " + productCollection.Entities.Count);
                if (branchCollection.Entities.Count > 0 && branchCollection != null && productCollection.Entities.Count > 0 && productCollection != null)
                {
                    Entity branchEntity = branchCollection.Entities[0];
                    Entity productEntity = productCollection.Entities[0];

                    decimal branchTaxRate = branchEntity.Contains("gsc_taxrate") ? (decimal)branchEntity.GetAttributeValue<double>("gsc_taxrate")
                        : 0;
                    decimal productTaxRate = productEntity.Contains("gsc_taxrate") ? (decimal)productEntity.GetAttributeValue<double>("gsc_taxrate")
                        : 0;
                    decimal dnpAmount = receivingDetailEntity.Contains("gsc_dnpamount") ? receivingDetailEntity.GetAttributeValue<Money>("gsc_dnpamount").Value
                        : 0;

                    //if inclusive
                    decimal taxStatus = priceList.Contains("gsc_taxstatus") ? priceList.GetAttributeValue<OptionSetValue>("gsc_taxstatus").Value
                        : 0;
                    if (taxStatus == 100000000)
                    {
                        _tracingService.Trace("Pricelist is Tax inclusive...");
                        decimal totalAmount = dnpAmount / (1 + (productTaxRate/100));
                        decimal vatamount = (dnpAmount / (1 + (productTaxRate / 100))) * (branchTaxRate/100);
                        receivingTransaction["gsc_totalamount"] = new Money(totalAmount);
                        receivingTransaction["gsc_vatamount"] = new Money(vatamount);
                        receivingTransaction["gsc_totalvpoamount"] = new Money(totalAmount + vatamount);
                        receivingTransaction = ComputeWithHoldingTax(receivingTransaction, branchEntity);

                        _organizationService.Update(receivingTransaction);
                        _tracingService.Trace("Updated totalamount: " + totalAmount + " vatamount: " + vatamount);
                    }
                    //if exclusive
                    else if (taxStatus == 100000001)
                    {
                        _tracingService.Trace("Pricelist is exclusive of tax...");

                        receivingTransaction["gsc_totalamount"] = new Money(dnpAmount);
                        receivingTransaction["gsc_vatamount"] = new Money(dnpAmount * (branchTaxRate/100));
                        receivingTransaction["gsc_totalvpoamount"] = new Money(dnpAmount + (dnpAmount * (branchTaxRate/100)));
                        receivingTransaction = ComputeWithHoldingTax(receivingTransaction, branchEntity);

                        _organizationService.Update(receivingTransaction);
                        _tracingService.Trace("Updated totalamount: " + dnpAmount);
                    }
                    else
                        _tracingService.Trace("No tax status setup ...");
                }
                _tracingService.Trace("Ending ComputeVAT Method...");
            }
            return receivingDetailEntity;
        }

        private Entity ComputeWithHoldingTax(Entity receivingEntity, Entity branchEntity)
        {
            _tracingService.Trace("Started ComputeWithHoldingTax Method...");
            if (branchEntity.GetAttributeValue<EntityReference>("gsc_vehiclewithholdingtaxid") == null)
                throw new InvalidPluginExecutionException("Please provide withholding tax for vehicle-po in the branch setup.");

            var totalamount = receivingEntity.GetAttributeValue<Money>("gsc_totalamount").Value;
            var totalVPOAmount = receivingEntity.GetAttributeValue<Money>("gsc_totalvpoamount").Value;
            var wTaxRate = branchEntity.Contains("gsc_vehiclewithholdingtaxrate")
                ? (decimal)branchEntity.GetAttributeValue<Double>("gsc_vehiclewithholdingtaxrate")
                : 0;
            var wtaxAmount = totalamount * (wTaxRate / 100);

            receivingEntity["gsc_wtaxamount"] = new Money(wtaxAmount);
            receivingEntity["gsc_netofwtaxamount"] = new Money(totalVPOAmount - wtaxAmount);

            _tracingService.Trace("Ending ComputeWithHoldingTax Method...");
            return receivingEntity;
        }

        //Added By : Jessica Casupanan, Added On : 02/08/2017
        /*Purpose: Validate if new record exists in Inventory entity
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Vehicle Info
         *      Post/Create:
         * Primary Entity: Vehicle Receiving Transaction Entry Detail
         */
        public bool CheckExistingInventoryRecord(Entity receivingTransactionDetailEntity)
        {
            _tracingService.Trace("Started CheckExistingInventoryRecord Method...");

            String productionNo = receivingTransactionDetailEntity.Contains("gsc_productionno")
                ? receivingTransactionDetailEntity.GetAttributeValue<String>("gsc_productionno")
                : String.Empty;
            String csNo = receivingTransactionDetailEntity.Contains("gsc_csno")
                ? receivingTransactionDetailEntity.GetAttributeValue<String>("gsc_csno")
                : String.Empty;
            String engineNo = receivingTransactionDetailEntity.Contains("gsc_engineno")
                ? receivingTransactionDetailEntity.GetAttributeValue<String>("gsc_engineno")
                : String.Empty;
            String vin = receivingTransactionDetailEntity.Contains("gsc_vin")
                ? receivingTransactionDetailEntity.GetAttributeValue<String>("gsc_vin")
                : String.Empty;
            Guid inventoryId = receivingTransactionDetailEntity.Contains("gsc_inventoryid")
              ? receivingTransactionDetailEntity.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
              : Guid.Empty;

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

            //Retrieve Inventory records using ConditionList
            QueryExpression inventoryQuery = new QueryExpression("gsc_iv_inventory");
            inventoryQuery.ColumnSet = new ColumnSet("gsc_status", "gsc_productquantityid", "gsc_productionno", "gsc_csno", "gsc_engineno", "gsc_vin");
            inventoryQuery.Criteria.AddFilter(productFilter);
            EntityCollection inventoryRecords = _organizationService.RetrieveMultiple(inventoryQuery);

            _tracingService.Trace(inventoryRecords.Entities.Count.ToString() + " Inventory record/records retrieved.");
            
            if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
            {
                foreach (Entity inventoryE in inventoryRecords.Entities)
                {
                    if (inventoryId != inventoryE.Id)
                    {
                        _tracingService.Trace("Duplicate Inventory Found.");

                        var prodNoE = inventoryE.Contains("gsc_productionno")
                            ? inventoryE.GetAttributeValue<String>("gsc_productionno")
                            : String.Empty;
                        var csNoE = inventoryE.Contains("gsc_csno")
                            ? inventoryE.GetAttributeValue<String>("gsc_csno")
                            : String.Empty;
                        var engineNoE = inventoryE.Contains("gsc_engineno")
                            ? inventoryE.GetAttributeValue<String>("gsc_engineno")
                            : String.Empty;
                        var vinE = inventoryE.Contains("gsc_vin")
                            ? inventoryE.GetAttributeValue<String>("gsc_vin")
                            : String.Empty;
                        string errorMsg = "";

                        if (csNo == csNoE)
                            errorMsg = errorMsg + "CS No. is already associated to an existing vehicle.\n";

                        if (vin == vinE)
                            errorMsg = errorMsg + "VIN is already associated to an existing vehicle.\n";

                        if (productionNo == prodNoE)
                            errorMsg = errorMsg + "Product No. is already associated to an existing vehicle.\n";

                        if (engineNo == engineNoE)
                            errorMsg = errorMsg + "Engine No. is already associated to an existing vehicle.";

                        if(errorMsg != "")
                        {
                            errorMsg = Regex.Replace(errorMsg, @"\t|\n|\r", "");
                            throw new InvalidPluginExecutionException(errorMsg);
                        }
                    }
                }
            }

            _tracingService.Trace("Ended CheckExistingInventoryRecord Method...");
            return false;
        }

    }
}
