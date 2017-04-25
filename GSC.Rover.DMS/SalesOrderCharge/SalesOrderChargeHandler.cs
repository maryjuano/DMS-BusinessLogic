using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.SalesOrder;

namespace GSC.Rover.DMS.BusinessLogic.SalesOrderCharge
{
    public class SalesOrderChargeHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public SalesOrderChargeHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 7/28/2016
        /*Purpose: Check is Charge selected was already applied in the order associated to it.
         * Registration Details:
         * Event/Message: 
         *      Create: 
         * Primary Entity: Order Charge
         */
        public void CheckifChargeExists(Entity orderDiscountEntity)
        {
            if (orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_chargesid") != null)
            {
                _tracingService.Trace("Started CheckifChargeExists Method");

                var quoteConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_orderid", ConditionOperator.Equal, orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_orderid") != null
                                                                                                        ? orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_orderid").Id
                                                                                                        : Guid.Empty),
                                new ConditionExpression("gsc_chargesid", ConditionOperator.Equal, orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_chargesid")!= null
                                                                                                        ? orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_chargesid").Id
                                                                                                        : Guid.Empty)
                            };

                EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_ordercharge", quoteConditionList, _organizationService,
                     null, OrderType.Ascending, new[] { "gsc_chargesid" });

                if (quoteRecords != null && quoteRecords.Entities.Count > 0)
                {
                    throw new InvalidPluginExecutionException("Selected Charge was already applied to this Order.");
                }

                _tracingService.Trace("Ended CheckifChargeExists Method");
            }
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/11/2016
        public Entity SetOrderTotalChargesAmount(Entity salesOrderChargeEntity, String message)
        {
            _tracingService.Trace("Started SetOrderTotalChargesAmount method..");

            var orderId = salesOrderChargeEntity.GetAttributeValue<EntityReference>("gsc_orderid") != null
                ? salesOrderChargeEntity.GetAttributeValue<EntityReference>("gsc_orderid").Id
                : Guid.Empty;
            var chargetype = salesOrderChargeEntity.Contains("gsc_chargetype")
                        ? salesOrderChargeEntity.GetAttributeValue<OptionSetValue>("gsc_chargetype").Value
                        : 0;

            Decimal totalOtherCharges = 0;
            Decimal totalFreight = 0;

            //Retrieve Applied Charges records with the same Sales Order
            EntityCollection salesOrderChargeRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_ordercharge", "gsc_orderid", orderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_actualcost", "gsc_free", "gsc_chargetype" });

            //Retrieve Sales Order record from Sales Order field value
            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", orderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_totalchargesamount", "statecode", "gsc_othercharges", "gsc_totalcashoutlay", "gsc_downpayment", "gsc_chattelfee", "gsc_netdownpayment",
                "gsc_insurance", "gsc_freightandhandling", "gsc_discount", "gsc_unitprice", "gsc_colorprice", "gsc_netprice", "gsc_reservation", "gsc_paymentmode",
                "gsc_vatablesales", "gsc_vatexemptsales", "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount", "gsc_totalamountdue", "customerid", "gsc_productid",
                "gsc_totalchargesamount", "gsc_totalpremium", "gsc_amountfinanced", "gsc_downpaymentamount", "gsc_totalamountfinanced"});


            if (salesOrderChargeRecords != null && salesOrderChargeRecords.Entities.Count > 0)
            {
                foreach (var salesOrderCharge in salesOrderChargeRecords.Entities)
                {
                    chargetype = salesOrderCharge.Contains("gsc_chargetype")
                        ? salesOrderCharge.GetAttributeValue<OptionSetValue>("gsc_chargetype").Value
                        : 0;

                    if (salesOrderCharge.Contains("gsc_actualcost"))
                    {
                        if (chargetype == 100000000) //freight
                        {
                            _tracingService.Trace("Add Freight.");

                            totalFreight += salesOrderCharge.GetAttributeValue<Boolean>("gsc_free")
                                ? Decimal.Zero
                                : salesOrderCharge.GetAttributeValue<Money>("gsc_actualcost").Value;

                            _tracingService.Trace(totalFreight.ToString());
                        }
                        else
                        {
                            _tracingService.Trace("Add Other Charges.");

                            totalOtherCharges += salesOrderCharge.GetAttributeValue<Boolean>("gsc_free")
                                ? Decimal.Zero
                                : salesOrderCharge.GetAttributeValue<Money>("gsc_actualcost").Value;

                            _tracingService.Trace(totalOtherCharges.ToString());
                        }
                    }
                }

                if (salesOrderChargeEntity.Contains("gsc_actualcost") && message.Equals("Delete"))
                {
                    if (chargetype == 100000000) //freight
                    {
                        _tracingService.Trace("Delete Freight.");
                        totalFreight = totalFreight - (Decimal)salesOrderChargeEntity.GetAttributeValue<Money>("gsc_actualcost").Value;
                        _tracingService.Trace(totalFreight.ToString());
                    }
                    else
                    {
                        _tracingService.Trace("Delete Other Charges.");
                        totalOtherCharges = totalOtherCharges - (Decimal)salesOrderChargeEntity.GetAttributeValue<Money>("gsc_actualcost").Value;
                        _tracingService.Trace(totalOtherCharges.ToString());
                    }
                }
            }

            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0 && salesOrderRecords.Entities[0].GetAttributeValue<OptionSetValue>("statecode").Value == 0)
            {
                SalesOrderHandler SOHandler = new SalesOrderHandler(_organizationService, _tracingService);

                Entity salesOrder = salesOrderRecords.Entities[0];

                salesOrder["gsc_freightandhandling"] = new Money(totalFreight);
                salesOrder["gsc_othercharges"] = new Money(totalOtherCharges);
                salesOrder["gsc_totalchargesamount"] = new Money(totalFreight + totalOtherCharges);

                var netprice = SOHandler.ComputeNetPrice(salesOrder);
                salesOrder["gsc_netprice"] = new Money(netprice);

                //Compute VAT
                salesOrder = SOHandler.ComputeVAT(salesOrder);

                var paymentmode = salesOrder.Contains("gsc_paymentmode")
                  ? salesOrder.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                  : Decimal.Zero;

                //Financing
                if (paymentmode == 100000001 || paymentmode == 100000002)
                {
                    var amountfinanced = SOHandler.ComputeAmountFinanced(salesOrder);
                    salesOrder["gsc_amountfinanced"] = new Money(amountfinanced);
                    salesOrder["gsc_totalamountfinanced"] = new Money(amountfinanced);

                    if (paymentmode == 100000001)
                        salesOrder = SOHandler.SetTotalCashOutlayAmount(salesOrder, "Create");
                }

                _organizationService.Update(salesOrder);

                return salesOrder;
            }
            _tracingService.Trace("Ended SetOrderTotalChargesAmount method..");
            return salesOrderChargeEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/4/2016
        public Entity SetChargeDetails(Entity salesOrderChargeEntity, String message)
        {
            _tracingService.Trace("Started SetChargeAmount method..");

            var chargesId = salesOrderChargeEntity.GetAttributeValue<EntityReference>("gsc_chargesid") != null
                ? salesOrderChargeEntity.GetAttributeValue<EntityReference>("gsc_chargesid").Id
                : Guid.Empty;

            //Retrieve Charge Record from Charge ID field
            EntityCollection chargeRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_charges", "gsc_cmn_chargesid", chargesId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_description", "gsc_chargeamount", "gsc_chargetype" });

            if (chargeRecords != null && chargeRecords.Entities.Count > 0)
            {
                Entity charge = chargeRecords.Entities[0];

                var chargeAmount = charge.Contains("gsc_chargeamount")
                    ? charge.GetAttributeValue<Money>("gsc_chargeamount")
                    : new Money(0);

                salesOrderChargeEntity["gsc_description"] = charge.Contains("gsc_description")
                    ? charge.GetAttributeValue<String>("gsc_description")
                    : String.Empty;
                salesOrderChargeEntity["gsc_amount"] = chargeAmount;
                salesOrderChargeEntity["gsc_actualcost"] = salesOrderChargeEntity.GetAttributeValue<Boolean>("gsc_free")
                            ? new Money(0)
                            : chargeAmount;
                salesOrderChargeEntity["gsc_chargetype"] = charge.Contains("gsc_chargetype")
                    ? charge.GetAttributeValue<OptionSetValue>("gsc_chargetype")
                    : null;

                if (message.Equals("Update"))
                {
                    _organizationService.Update(salesOrderChargeEntity);
                }
            }
            else
            {
                salesOrderChargeEntity["gsc_description"] = String.Empty;
                salesOrderChargeEntity["gsc_amount"] = new Money(Decimal.Zero);
                salesOrderChargeEntity["gsc_chargetype"] = String.Empty;

                if (message.Equals("Update"))
                {
                    _organizationService.Update(salesOrderChargeEntity);
                }
            }
            _tracingService.Trace("Ended SetChargeAmount method..");
            return salesOrderChargeEntity;
        }


        //Created By: Leslie Baliguat
        public void FreeCharges(Entity orderChargeEntity)
        {
            var isfree = orderChargeEntity.GetAttributeValue<Boolean>("gsc_free");

            var actualcost = orderChargeEntity.Contains("gsc_actualcost")
                ? orderChargeEntity.GetAttributeValue<Money>("gsc_actualcost")
                : new Money(0);

            var amount = orderChargeEntity.Contains("gsc_amount")
                ? orderChargeEntity.GetAttributeValue<Money>("gsc_amount")
                : new Money(0);

            Entity orderChargeToUpdate = _organizationService.Retrieve(orderChargeEntity.LogicalName, orderChargeEntity.Id,
                new ColumnSet("gsc_actualcost"));

            if (isfree)
            {
                orderChargeToUpdate["gsc_actualcost"] = new Money(0);
            }
            else
            {
                orderChargeToUpdate["gsc_actualcost"] = amount;
            }

            _organizationService.Update(orderChargeToUpdate);

        }
    }
}
