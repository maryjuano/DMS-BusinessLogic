using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.QuoteDiscount
{
    public class QuoteDiscountHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public QuoteDiscountHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 7/26/2016
        /*Purpose: Check is Discount selected was already applied in the quote associated to it.
         * Registration Details:
         * Event/Message: 
         *      Create: 
         * Primary Entity: Quote Discount
         */
        public void CheckifDiscountExists(Entity quoteDiscountEntity)
        {
            if (quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid") != null)
            {
                _tracingService.Trace("Started CheckifDiscountExists Method");

                var quoteConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_quoteid", ConditionOperator.Equal, quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_quoteid")!= null
                                                                                                ? quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                                                                                                : Guid.Empty),
                                new ConditionExpression("gsc_pricelistid", ConditionOperator.Equal, quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid") != null
                                                                                                ? quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid").Id
                                                                                                : Guid.Empty) 
                            };

                EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_quotediscount", quoteConditionList, _organizationService,
                     null, OrderType.Ascending, new[] { "gsc_pricelistid" });

                if (quoteRecords != null && quoteRecords.Entities.Count > 0)
                {
                    throw new InvalidPluginExecutionException("Selected Discount was already applied to this Quote.");
                }

                _tracingService.Trace("Ended CheckifDiscountExists Method");
            }
        }

        //Created By: Leslie Baliguat, Created On: 7/7/2016
        /*Purpose: Replicate Disocunt Information to Quote Discount Form.
         * Registration Details:
         * Event/Message: 
         *      Create: 
         * Primary Entity: Quote Discount
         */
        public void ReplicateDiscountInformation(Entity quoteDiscountEntity, String message)
        {
            if (quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid") != null)
            {
                _tracingService.Trace("Started ReplicateDiscountInformation method..");

                var quoteId = quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_quoteid")!= null
                    ? quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                    : Guid.Empty;
                var discountId = quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid") != null
                    ? quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_pricelistid").Id
                    : Guid.Empty;
                var product = new EntityReference();

                //Retrieve Porduct Associated in Quote 
                EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_productid" });

                if (quoteRecords != null && quoteRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Product Id..");

                    product = quoteRecords.Entities[0].GetAttributeValue<EntityReference>("gsc_productid");
                }

                //Check if Quote Record has Product associated in it. If none, throw an error.
                if (product == null)
                    throw new InvalidPluginExecutionException("Cannot associate discount. Vehicle is missing in Quotation Record.");

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

                        quoteDiscountEntity["gsc_discountamount"] = discount;
                        quoteDiscountEntity["gsc_description"] = description;

                        if (message.Equals("Update"))
                        {
                            Entity quoteDiscountToUpdate = _organizationService.Retrieve(quoteDiscountEntity.LogicalName, quoteDiscountEntity.Id,
                                new ColumnSet("gsc_discountamount", "gsc_description", "gsc_quotediscountpn"));
                            quoteDiscountToUpdate["gsc_discountamount"] = discount;
                            quoteDiscountToUpdate["gsc_description"] = description;
                            _organizationService.Update(quoteDiscountToUpdate);
                        }

                        _tracingService.Trace("Quote Information Updated..");

                    }
                    else
                    {
                        throw new InvalidPluginExecutionException("The promo selected is not applicable for the vehicle of this quote.");
                    }

                }

                _tracingService.Trace("Ended ReplicateDiscountInformation method..");
            }
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/11/2016
        public Entity SetQuoteTotalDiscountAmount(Entity quoteDiscountEntity, String message)
        {
            _tracingService.Trace("Started SetQuoteTotalDiscountAmount method..");

            Decimal totalDiscountAmount = 0;
            Decimal totalDPAmount = 0;
            Decimal totalAFAmount = 0;
            Decimal totalUPAmount = 0;

            var quoteId = quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_quoteid") != null
                ? quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;

            //Retrieve Applied Price List records with the same Quote
            EntityCollection quoteDiscountRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_quotediscount", "gsc_quoteid", quoteId, _organizationService, null, 
                OrderType.Ascending, new[] { "gsc_discountamount", "gsc_applypercentagetodp", "gsc_applyamounttodp", "gsc_applypercentagetoaf", "gsc_applyamounttoaf", "gsc_applypercentagetoup", "gsc_applyamounttoup" });

            _tracingService.Trace("Quote Discount Records Retrieved: " + quoteDiscountRecords.Entities.Count);

            if (quoteDiscountRecords != null && quoteDiscountRecords.Entities.Count > 0)
            {

                //Compute for Total Discount Amount from all retrieved Quote records
                foreach (var appliedPriceList in quoteDiscountRecords.Entities)
                {
                    _tracingService.Trace("Add Created/Updated Discount Amount in Quote..");

                    totalDiscountAmount += appliedPriceList.Contains("gsc_discountamount")
                        ? appliedPriceList.GetAttributeValue<Money>("gsc_discountamount").Value
                        : Decimal.Zero;

                    totalDPAmount += appliedPriceList.Contains("gsc_applyamounttodp")
                        ? appliedPriceList.GetAttributeValue<Money>("gsc_applyamounttodp").Value
                        : Decimal.Zero;

                    totalAFAmount += appliedPriceList.Contains("gsc_applyamounttoaf")
                        ? appliedPriceList.GetAttributeValue<Money>("gsc_applyamounttoaf").Value
                        : Decimal.Zero;

                    totalUPAmount += appliedPriceList.Contains("gsc_applyamounttoup")
                        ? appliedPriceList.GetAttributeValue<Money>("gsc_applyamounttoup").Value
                        : Decimal.Zero;
                }

                if (quoteDiscountEntity.Contains("gsc_discountamount") && message.Equals("Delete"))
                {
                    _tracingService.Trace("Subtract Deleted Discount from Total Discount...");

                    totalDiscountAmount = totalDiscountAmount - (Decimal)quoteDiscountEntity.GetAttributeValue<Money>("gsc_discountamount").Value;

                    totalDPAmount = totalDPAmount - (quoteDiscountEntity.Contains("gsc_applyamounttodp") 
                    ? quoteDiscountEntity.GetAttributeValue<Money>("gsc_applyamounttodp").Value
                    : 0);

                    totalAFAmount = totalAFAmount -  (quoteDiscountEntity.Contains("gsc_applyamounttoaf") 
                    ? quoteDiscountEntity.GetAttributeValue<Money>("gsc_applyamounttoaf").Value
                    : 0);

                    totalUPAmount = totalUPAmount - (quoteDiscountEntity.Contains("gsc_applyamounttoaf") 
                    ? quoteDiscountEntity.GetAttributeValue<Money>("gsc_applyamounttoup").Value
                    : 0);
                }
            }


            //Retrieve Quote record from Quote field value
            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "totaldiscountamount", "gsc_applytodppercentage", "gsc_applytodpamount", "gsc_applytoafpercentage", "gsc_applytoafamount", 
                    "gsc_applytouppercentage", "gsc_applytoupamount", "statecode" });

            if (quoteRecords != null && quoteRecords.Entities.Count > 0 && quoteRecords.Entities[0].GetAttributeValue<OptionSetValue>("statecode").Value == 0)
            {
                _tracingService.Trace("Setting Up Quote to be Updated . . . ");

                Entity quotetoUpdate = quoteRecords.Entities[0];

                quotetoUpdate["totaldiscountamount"] = new Money(totalDiscountAmount);
                quotetoUpdate["gsc_applytodpamount"] = new Money(totalDPAmount);
                quotetoUpdate["gsc_applytoafamount"] = new Money(totalAFAmount);
                quotetoUpdate["gsc_applytoupamount"] = new Money(totalUPAmount);
                quotetoUpdate["gsc_applytodppercentage"] = ComputePercentage(totalDPAmount, totalDiscountAmount);
                quotetoUpdate["gsc_applytoafpercentage"] = ComputePercentage(totalAFAmount, totalDiscountAmount);
                quotetoUpdate["gsc_applytouppercentage"] = ComputePercentage(totalUPAmount, totalDiscountAmount);

                _organizationService.Update(quotetoUpdate);

                _tracingService.Trace("Updated Discounts Amount in Quote..");
                return quotetoUpdate;
            }

            _tracingService.Trace("Ended SetQuoteTotalDiscountAmount method..");

            return quoteDiscountEntity;
        }

        private double ComputePercentage(Decimal amount, Decimal totaldiscount)
        {
            _tracingService.Trace("ComputePercentage method..");

            if (totaldiscount == 0) { return 0; };

            return Convert.ToDouble((amount / totaldiscount) * 100);
        }

        //To be deleted.
        public void ApplytoUPWhenCash(Entity quoteDiscount)
        {
            var quoteId = quoteDiscount.GetAttributeValue<EntityReference>("gsc_quoteid") != null
                ? quoteDiscount.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;

            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
               new[] { "gsc_paymentmode" });

            if (quoteRecords != null && quoteRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Product Id..");
                
                var paymentMode = quoteRecords.Entities[0].GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value;

                if(paymentMode == 100000000 || paymentMode == 100000002 || paymentMode == 100000003)
                {
                    quoteDiscount["gsc_applypercentagetoup"] = Convert.ToDouble(100);
                    quoteDiscount["gsc_applyamounttoup"] = quoteDiscount.GetAttributeValue<Money>("gsc_discountamount");
                }
            }
        }
    }
}
