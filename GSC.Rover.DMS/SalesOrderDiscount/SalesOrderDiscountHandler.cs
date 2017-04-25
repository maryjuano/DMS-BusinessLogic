using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.SalesOrderDiscount
{
    public class SalesOrderDiscountHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public SalesOrderDiscountHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 7/28/2016
        /*Purpose: Check is Discount selected was already applied in the order associated to it.
         * Registration Details:
         * Event/Message: 
         *      Create: 
         * Primary Entity: Order Discount
         */
        public void CheckifDiscountExists(Entity orderDiscountEntity)
        {
            if (orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid") != null)
            {
                _tracingService.Trace("Started CheckifDiscountExists Method");

                var quoteConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_salesorderid", ConditionOperator.Equal, orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_salesorderid") != null
                                                                                                        ? orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_salesorderid").Id
                                                                                                        : Guid.Empty),
                                new ConditionExpression("gsc_pricelistid", ConditionOperator.Equal, orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid")!= null
                                                                                                        ? orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid").Id
                                                                                                        : Guid.Empty)
                            };

                EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_salesorderdiscount", quoteConditionList, _organizationService,
                     null, OrderType.Ascending, new[] { "gsc_pricelistid" });

                if (quoteRecords != null && quoteRecords.Entities.Count > 0)
                {
                    throw new InvalidPluginExecutionException("Selected Discount was already applied to this Order.");
                }

                _tracingService.Trace("Ended CheckifDiscountExists Method");
            }
        }

        //Created By: Leslie Baliguat, Created On: 7/28/2016
        /*Purpose: Replicate Discount Information to Order Discount Form.
         * Registration Details:
         * Event/Message: 
         *      Create: 
         * Primary Entity: Order Discount
         */
        public void ReplicateDiscountInformation(Entity orderDiscountEntity)
        {
            if (orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid") != null)
            {
                _tracingService.Trace("Started ReplicateDiscountInformation method..");

                var orderId = orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_salesorderid") != null
                    ? orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_salesorderid").Id
                    : Guid.Empty;
                var discountId = orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid")!= null
                    ? orderDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid").Id
                    : Guid.Empty;
                var product = new EntityReference();

                //Retrieve Porduct Associated in Quote 
                EntityCollection salesOrderCollection = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", orderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_productid" });

                if (salesOrderCollection != null && salesOrderCollection.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Product Id..");

                    product = salesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_productid");
                }

                //Check if Quote Record has Product associated in it. If none, throw an error.
                if (product == null)
                    throw new InvalidPluginExecutionException("Cannot associate discount. Vehicle is missing in Order Record.");

                //Retrieve Price List Selected
                EntityCollection priceListRecords = CommonHandler.RetrieveRecordsByOneValue("pricelevel", "pricelevelid", discountId, _organizationService, null, OrderType.Ascending,
                new[] { "description" });

                if (priceListRecords != null && priceListRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Price List Details..");

                    var priceListEntity = priceListRecords.Entities[0];

                    var description = priceListEntity.Contains("description")
                        ? priceListEntity.GetAttributeValue<String>("description")
                        : String.Empty;

                    //Check if price list is applicable to Product, if not, throw an error.
                    var priceListItemConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("pricelevelid", ConditionOperator.Equal, priceListEntity.Id),
                                new ConditionExpression("productid", ConditionOperator.Equal, product.Id)
                            };

                    EntityCollection priceListItemRecords = CommonHandler.RetrieveRecordsByConditions("productpricelevel", priceListItemConditionList, _organizationService,
                         null, OrderType.Ascending, new[] { "productid", "amount"});


                    if (priceListItemRecords != null && priceListItemRecords.Entities.Count > 0)
                    {
                        _tracingService.Trace("Retrieve Price List Item Details..");

                        var priceListItemEntity = priceListItemRecords.Entities[0];

                        var discount = priceListItemEntity.Contains("amount")
                                ? priceListItemEntity.GetAttributeValue<Money>("amount")
                                : new Money(0);

                        Entity quoteDiscountToUpdate = _organizationService.Retrieve(orderDiscountEntity.LogicalName, orderDiscountEntity.Id, new ColumnSet("gsc_discountamount", "gsc_description"));
                        quoteDiscountToUpdate["gsc_discountamount"] = discount;
                        quoteDiscountToUpdate["gsc_description"] = description;
                        _organizationService.Update(quoteDiscountToUpdate);

                        _tracingService.Trace("Quote Information Updated..");

                    }
                    else
                    {
                        throw new InvalidPluginExecutionException("The Promo selected is not applicable for the product of this Order.");
                    }

                }

                _tracingService.Trace("Ended ReplicateDiscountInformation method..");
            }
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/11/2016
        public Entity SetOrderTotalDiscountAmount(Entity salesOrderDiscountEntity, String message)
        {
            _tracingService.Trace("Started SetOrderTotalDiscountAmount method..");

            var salesOrderId = salesOrderDiscountEntity.GetAttributeValue<EntityReference>("gsc_salesorderid") != null
                ? salesOrderDiscountEntity.GetAttributeValue<EntityReference>("gsc_salesorderid").Id
                : Guid.Empty;

            Decimal totalDiscountAmount = 0;
            Decimal totalDPAmount = 0;
            Decimal totalAFAmount = 0;
            Decimal totalUPAmount = 0;

            //Retrieve Sales Order records with the same Order
            EntityCollection salesOrderDiscountRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_salesorderdiscount", "gsc_salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_discountamount", "gsc_applypercentagetodp", "gsc_applypercentagetoaf", "gsc_applypercentagetoup",
                "gsc_applyamounttodp", "gsc_applyamounttoaf", "gsc_applyamounttoup"});

            if (salesOrderDiscountRecords != null && salesOrderDiscountRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Order Discounts");

                foreach (var salesOrderDiscount in salesOrderDiscountRecords.Entities)
                {
                    if (salesOrderDiscount.Contains("gsc_discountamount"))
                    {
                        totalDiscountAmount += salesOrderDiscount.GetAttributeValue<Money>("gsc_discountamount").Value;

                        totalDPAmount += salesOrderDiscount.Contains("gsc_applyamounttodp")
                            ? salesOrderDiscount.GetAttributeValue<Money>("gsc_applyamounttodp").Value
                            : Decimal.Zero;

                        totalAFAmount += salesOrderDiscount.Contains("gsc_applyamounttoaf")
                            ? salesOrderDiscount.GetAttributeValue<Money>("gsc_applyamounttoaf").Value
                            : Decimal.Zero;

                        totalUPAmount += salesOrderDiscount.Contains("gsc_applyamounttoup")
                            ? salesOrderDiscount.GetAttributeValue<Money>("gsc_applyamounttoup").Value
                            : Decimal.Zero;
                    }
                }

                if (salesOrderDiscountEntity.Contains("gsc_discountamount") && message.Equals("Delete"))
                {
                    _tracingService.Trace("Subtract on Total");

                    totalDiscountAmount = totalDiscountAmount - (Decimal)salesOrderDiscountEntity.GetAttributeValue<Money>("gsc_discountamount").Value;

                    totalDPAmount = totalDPAmount - (salesOrderDiscountEntity.Contains("gsc_applyamounttodp") 
                    ? salesOrderDiscountEntity.GetAttributeValue<Money>("gsc_applyamounttodp").Value
                    : 0);

                    totalAFAmount = totalAFAmount -  (salesOrderDiscountEntity.Contains("gsc_applyamounttoaf") 
                    ? salesOrderDiscountEntity.GetAttributeValue<Money>("gsc_applyamounttoaf").Value
                    : 0);

                    totalUPAmount = totalUPAmount - (salesOrderDiscountEntity.Contains("gsc_applyamounttoup") 
                    ? salesOrderDiscountEntity.GetAttributeValue<Money>("gsc_applyamounttoup").Value
                    : 0);
                }
            }

            //Retrieve Order record from Order field value
            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_totaldiscountamount", "statecode", "gsc_applytodppercentage", "gsc_applytoafpercentage", "gsc_applytouppercentage", "gsc_applytodpamount", "gsc_applytoafamount", "gsc_applytoupamount" });

            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0 && salesOrderRecords.Entities[0].GetAttributeValue<OptionSetValue>("statecode").Value == 0)
            {
                _tracingService.Trace("Retrieve Sales Order");

                Entity salesOrder = salesOrderRecords.Entities[0];

                salesOrder["gsc_totaldiscountamount"] = new Money(totalDiscountAmount);
                salesOrder["gsc_applytodpamount"] = new Money(totalDPAmount);
                salesOrder["gsc_applytoafamount"] = new Money(totalAFAmount);
                salesOrder["gsc_applytoupamount"] = new Money(totalUPAmount);
                salesOrder["gsc_applytodppercentage"] = ComputePercentage(totalDPAmount, totalDiscountAmount);
                salesOrder["gsc_applytoafpercentage"] = ComputePercentage(totalAFAmount, totalDiscountAmount);
                salesOrder["gsc_applytouppercentage"] = ComputePercentage(totalUPAmount, totalDiscountAmount);

                _tracingService.Trace("Updating Discount Amounts...");
                _organizationService.Update(salesOrder);

                return salesOrder;
            }

            _tracingService.Trace("Ended SetOrderTotalDiscountAmount method..");
            return salesOrderDiscountEntity;
        }

        private double ComputePercentage(Decimal amount, Decimal totaldiscount)
        {
            _tracingService.Trace("ComputePercentage method..");

            if (totaldiscount == 0) { return 0; };

            return Convert.ToDouble((amount / totaldiscount) * 100);
        }

        public void ApplytoUPWhenCash(Entity orderDiscount)
        {
            var orderId = orderDiscount.GetAttributeValue<EntityReference>("gsc_salesorderid") != null
                ? orderDiscount.GetAttributeValue<EntityReference>("gsc_salesorderid").Id
                : Guid.Empty;

            EntityCollection orderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", orderId, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_paymentmode" });

            if (orderRecords != null && orderRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Product Id..");

                var paymentMode = orderRecords.Entities[0].GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value;

                if (paymentMode == 100000000 || paymentMode == 100000002 || paymentMode == 100000003)
                {
                    orderDiscount["gsc_applypercentagetoup"] = Convert.ToDouble(100);
                    orderDiscount["gsc_applyamounttoup"] = orderDiscount.GetAttributeValue<Money>("gsc_discountamount");
                }
            }
        }
    }
}
