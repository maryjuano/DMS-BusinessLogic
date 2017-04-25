using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.PriceList;

namespace GSC.Rover.DMS.BusinessLogic.VehiclePurchaseOrderDetails
{
    public class VehiclePurchaseOrderDetailsHandler
    {

        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehiclePurchaseOrderDetailsHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Create By: Raphael Herrera, Created On: 7/12/2016 /*
        //Modified By: Leslie Baliguat, Modified On: 01/25/17
        /* Purpose:  Set DNP Amount
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: VehiclePurchaseOrderDetail
         */
        public decimal SetDNPAmount(Entity vpoDetail)
        {
            _tracingService.Trace("Started SetDNPAmount method...");

            var productId = vpoDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? vpoDetail.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;

            if (productId != Guid.Empty)
            {
                Money amount = new Money();
                var dateToday = DateTime.Now.ToShortDateString();

                PriceListHandler priceListHandler = new PriceListHandler(_organizationService, _tracingService);
                priceListHandler.itemType = 0;
                priceListHandler.productFieldName = "gsc_productid";
                List<Entity> latestPriceList = priceListHandler.RetrievePriceList(vpoDetail, 100000001, 0);

                if (latestPriceList.Count > 0)
                {
                    Entity priceListItem = latestPriceList[0];
                    Entity priceList = latestPriceList[1];

                    amount = new Money(priceListItem.GetAttributeValue<Money>("amount").Value);

                    vpoDetail["gsc_dnpamount"] = amount;

                    _organizationService.Update(vpoDetail);

                    _tracingService.Trace("Updated DNP Amount to " + amount.Value);

                    _tracingService.Trace("Ending SetDNPAmount method...");

                    ComputeVAT(vpoDetail, latestPriceList);

                    return amount.Value;
                }
                else
                {
                    throw new InvalidPluginExecutionException("There is no effecive DNP Price List for the selected Vehicle.");
                }
            }

            return 0;
        }

        //Created By: Leslie Baliguat, Created On: 01/31/17
        //Clear Computation Section in Purchase Order when Purchase Order Item is deleted
        public Entity ClearComputation(Entity vpoDetail)
        {
            var purchaseOrderId = CommonHandler.GetEntityReferenceIdSafe(vpoDetail, "gsc_purchaseorderid");

            EntityCollection purchaseOrderCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_totalamount", "gsc_vatamount", "gsc_totalvpoamount", "gsc_wtaxamount", "gsc_netofwtaxamount" });

            if (purchaseOrderCollection != null && purchaseOrderCollection.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderCollection.Entities[0];

                purchaseOrder["gsc_totalamount"] = null;
                purchaseOrder["gsc_vatamount"] = null;
                purchaseOrder["gsc_wtaxamount"] = null;
                purchaseOrder["gsc_totalvpoamount"] = null;
                purchaseOrder["gsc_netofwtaxamount"] = null;

                _organizationService.Update(purchaseOrder);

                return purchaseOrder;
            }

            return null;
        }

        //Created By: Leslie G. Baliguat, Created On: 01/31/2017
        //Restrict more than 1 VPO Item per PO Record
        public void RestrictMultipleCreate(Entity vpoDetail)
        {
            var purchaseOrderId = CommonHandler.GetEntityReferenceIdSafe(vpoDetail, "gsc_purchaseorderid");

            EntityCollection purchaseOrderCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", purchaseOrderId,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_purchaseorderid" });

            if (purchaseOrderCollection != null && purchaseOrderCollection.Entities.Count > 0)
                throw new InvalidPluginExecutionException("You can only created one (1) Vehicle Purchase Order Item on each VPO record.");
        }

        //Created By: Leslie G. Baliguat, Created On: 02/01/2017
        //Check if the combination of Model Description, Model Code and Option Code exists
        public void CheckifVehicleExists(Entity purchaseOrder)
        {
            var modelDescription = purchaseOrder.Contains("gsc_productid")
                ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;
            var optionCode = purchaseOrder.Contains("gsc_optioncode")
                ? purchaseOrder.GetAttributeValue<String>("gsc_optioncode")
                : String.Empty;
            var modelCode = purchaseOrder.Contains("gsc_modelcode")
                ? purchaseOrder.GetAttributeValue<String>("gsc_modelcode")
                : String.Empty;

            var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("parentproductid", ConditionOperator.Equal, modelDescription),
                        new ConditionExpression("gsc_optioncode", ConditionOperator.Equal, optionCode),
                        new ConditionExpression("gsc_modelcode", ConditionOperator.Equal, modelCode)
                    };

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByConditions("product", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "name", "gsc_optioncode", "gsc_modelcode", "gsc_modelyear", "gsc_vehiclemodelid" });

            if (productCollection.Entities.Count == 0)
                throw new InvalidPluginExecutionException("The combination of Model Description, Model Code and Option Code doesn't exist.");

        }

        //Create By: Raphael Herrera, Created On: 7/13/2016 /*
        /* Purpose:  Compute for VAT related fields of vehicle purchase order
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Purchase Order
         */
        public void ComputeVAT(Entity vehiclePurchaseOrderDetail, List<Entity> latestPriceList)
        {
            _tracingService.Trace("Started ComputeVAT method...");

            var purchaseOrderId = vehiclePurchaseOrderDetail.Contains("gsc_purchaseorderid") ? vehiclePurchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_purchaseorderid").Id
                               : Guid.Empty;

            EntityCollection purchaseOrderCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_totalamount", "gsc_vatamount", "gsc_totalvpoamount", "gsc_wtaxamount", "gsc_netofwtaxamount", "gsc_branchid" });

            if (purchaseOrderCollection.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderCollection.Entities[0];

                Guid productId = vehiclePurchaseOrderDetail.Contains("gsc_productid") ? vehiclePurchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty;
                Guid branchId = purchaseOrder.Contains("gsc_branchid") ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_branchid").Id
                    : Guid.Empty;
                var dateToday = DateTime.Now.ToShortDateString();

                Entity priceListItem = latestPriceList[0];
                Entity priceList = latestPriceList[1];

                EntityCollection branchCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", branchId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_taxid" });
                //get vehicle tax id
                EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_taxid" });

                if (branchCollection.Entities.Count > 0 && branchCollection != null && productCollection.Entities.Count > 0 && productCollection != null)
                {
                    Entity branchEntity = branchCollection.Entities[0];
                    Entity productEntity = productCollection.Entities[0];

                    decimal branchRate = 0;
                    decimal productRate = 0;

                    Guid branchTaxId = branchEntity.Contains("gsc_taxid") ? branchEntity.GetAttributeValue<EntityReference>("gsc_taxid").Id
                        : Guid.Empty;
                    Guid productTaxId = productEntity.Contains("gsc_taxid") ? productEntity.GetAttributeValue<EntityReference>("gsc_taxid").Id
                        : Guid.Empty;
                    //get tax rate of taxid
                    EntityCollection branchTaxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_taxmaintenance", "gsc_cmn_taxmaintenanceid", branchTaxId, _organizationService, null,
                        OrderType.Ascending, new[] { "gsc_rate" });
                    EntityCollection productTaxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_taxmaintenance", "gsc_cmn_taxmaintenanceid", productTaxId, _organizationService, null,
                        OrderType.Ascending, new[] { "gsc_rate" });

                    if (branchTaxCollection.Entities.Count > 0 && branchTaxCollection != null && productTaxCollection.Entities.Count > 0 && productTaxCollection != null)
                    {
                        Entity branchTaxEntity = branchTaxCollection.Entities[0];
                        Entity productTaxEntity = productTaxCollection.Entities[0];
                        decimal dnpAmount = vehiclePurchaseOrderDetail.Contains("gsc_dnpamount") ? vehiclePurchaseOrderDetail.GetAttributeValue<Money>("gsc_dnpamount").Value
                            : 0;

                        branchRate = branchTaxEntity.Contains("gsc_rate") ? (decimal)branchTaxEntity.GetAttributeValue<double>("gsc_rate") : 0;
                        branchRate = branchRate / 100;
                        productRate = productTaxEntity.Contains("gsc_rate") ? (decimal)productTaxEntity.GetAttributeValue<double>("gsc_rate") : 0;
                        productRate = productRate / 100;
                        //if inclusive
                        if (priceList.GetAttributeValue<OptionSetValue>("gsc_taxstatus").Value == 100000000)
                        {
                            _tracingService.Trace("Pricelist is Tax inclusive...");
                            decimal totalAmount = dnpAmount / (1 + productRate);
                            decimal vatamount = (dnpAmount / (1 + productRate)) * branchRate;
                            purchaseOrder["gsc_totalamount"] = new Money(totalAmount);
                            purchaseOrder["gsc_vatamount"] = new Money(vatamount);
                            purchaseOrder["gsc_totalvpoamount"] = new Money(totalAmount + vatamount);
                            purchaseOrder = ComputeWithHoldingTax(purchaseOrder);

                            _organizationService.Update(purchaseOrder);
                            _tracingService.Trace("Updated totalamount: " + totalAmount + " vatamount: " + vatamount);
                        }
                        //if exclusive
                        else if (priceList.GetAttributeValue<OptionSetValue>("gsc_taxstatus").Value == 100000001)
                        {
                            _tracingService.Trace("Pricelist is exclusive of tax...");

                            purchaseOrder["gsc_totalamount"] = new Money(dnpAmount);
                            purchaseOrder["gsc_vatamount"] = new Money(dnpAmount * branchRate);
                            purchaseOrder["gsc_totalvpoamount"] = new Money(dnpAmount + (dnpAmount * branchRate));
                            purchaseOrder = ComputeWithHoldingTax(purchaseOrder);

                            _organizationService.Update(purchaseOrder);
                            _tracingService.Trace("Updated totalamount: " + dnpAmount);
                        }
                        else
                            _tracingService.Trace("No tax status setup ...");
                    }
                    else
                        _tracingService.Trace("No tax record retrieved...");
                }
                else
                    _tracingService.Trace("No setup of tax id in branch and product...");
            }

            _tracingService.Trace("Ending ComputeVAT Method...");
        }

        //Created By: Leslie Baliguat, Created On: 01/25/2107
        /* Purpose:  Compute W/tax Amount and Net of W/Tax Amount
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Purchase Order
         */
        private Entity ComputeWithHoldingTax(Entity purchaseOrder)
        {
            var branchId = CommonHandler.GetEntityReferenceIdSafe(purchaseOrder, "gsc_branchid");

            //get tax rate of taxid
            EntityCollection branchCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", branchId, _organizationService, null,
                OrderType.Ascending, new[] { "gsc_vehiclewithholdingtaxrate", "gsc_vehiclewithholdingtaxid" });

            if (branchCollection != null && branchCollection.Entities.Count > 0)
            {
                Entity branch = branchCollection.Entities[0];

                if (branch.GetAttributeValue<EntityReference>("gsc_vehiclewithholdingtaxid") == null)
                    throw new InvalidPluginExecutionException("Please provide withholding tax for vehicle-po in the branch setup.");

                var totalamount = purchaseOrder.GetAttributeValue<Money>("gsc_totalamount").Value;
                var totalVPOAmount = purchaseOrder.GetAttributeValue<Money>("gsc_totalvpoamount").Value;
                var wTaxRate = branch.Contains("gsc_vehiclewithholdingtaxrate")
                    ? (decimal)branch.GetAttributeValue<Double>("gsc_vehiclewithholdingtaxrate")
                    : 0;
                var wtaxAmount = totalamount * (wTaxRate / 100);

                purchaseOrder["gsc_wtaxamount"] = new Money(wtaxAmount);
                purchaseOrder["gsc_netofwtaxamount"] = new Money(totalVPOAmount - wtaxAmount);
            }

            return purchaseOrder;
        }
    }
}
