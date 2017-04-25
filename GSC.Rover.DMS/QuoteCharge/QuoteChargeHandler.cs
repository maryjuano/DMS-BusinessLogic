using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.Quote;

namespace GSC.Rover.DMS.BusinessLogic.QuoteCharge
{
    public class QuoteChargeHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public QuoteChargeHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 7/26/2016
        /*Purpose: Check is Charge selected was already applied in the quote associated to it.
         * Registration Details:
         * Event/Message: 
         *      Create: 
         * Primary Entity: Quote Charge
         */
        public void CheckifChargeExists(Entity quoteDiscountEntity)
        {
            if (quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_chargesid") != null)
            {
                _tracingService.Trace("Started CheckifChargeExists Method");

                var quoteConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_quoteid", ConditionOperator.Equal, quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id),
                                new ConditionExpression("gsc_chargesid", ConditionOperator.Equal, quoteDiscountEntity.GetAttributeValue<EntityReference>("gsc_chargesid").Id)
                            };

                EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_quotecharge", quoteConditionList, _organizationService,
                     null, OrderType.Ascending, new[] { "gsc_chargesid" });

                if (quoteRecords != null && quoteRecords.Entities.Count > 0)
                {
                    throw new InvalidPluginExecutionException("Selected Charge was already applied to this Quote.");
                }

                _tracingService.Trace("Ended CheckifChargeExists Method");
            }
        }

        //Created By : Jerome Anthony Gerero, Created On: 3/11/2016
        /*Modified By: Raphael Herrera, Modified On: 9/19/2016
         * Modification Purpose: Compute for Freight & Handling Charges
         */
        public Entity SetQuoteTotalChargesAmount(Entity quoteChargeEntity, String message)
        {
            _tracingService.Trace("Started SetQuoteTotalChargesAmount method..");
            
            var quoteId = quoteChargeEntity.GetAttributeValue<EntityReference>("gsc_quoteid") != null
                ? quoteChargeEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;

            var actualcost = quoteChargeEntity.Contains("gsc_actualcost")
                ? quoteChargeEntity.GetAttributeValue<Money>("gsc_actualcost")
                : new Money(0);

            if (actualcost.Value == 0)
            {
                Entity quoteChargeToUpdate = _organizationService.Retrieve(quoteChargeEntity.LogicalName, quoteChargeEntity.Id,
                    new ColumnSet("gsc_actualcost"));

                quoteChargeToUpdate["gsc_actualcost"] = new Money(0);

                _organizationService.Update(quoteChargeToUpdate);
            }

            Decimal totalChargesAmount = 0;
            Decimal totalFreightAmount = 0;

            //Retrieve Applied Charges records with the same Quote
            EntityCollection quoteChargeRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_quotecharge", "gsc_quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_free", "gsc_actualcost", "gsc_chargetype" });

            //Retrieve Quote record from Quote field value
            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_totalchargesamount", "gsc_othercharges", "statecode", "gsc_totalcashoutlay", "gsc_downpayment", "gsc_chattelfee", "gsc_productid",
                "gsc_insurance", "gsc_freightandhandling", "gsc_ccaddons", "gsc_totaldiscount", "gsc_unitprice", "gsc_colorprice", "customerid", "gsc_paymentmode",
                "gsc_netprice", "gsc_accessories", "gsc_vatablesales", "gsc_vatexemptsales", "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount", "gsc_totalamountdue",
                "gsc_amountfinanced", "gsc_totalamountfinanced", "gsc_downpaymentamount", "gsc_lessdiscountaf"});

            _tracingService.Trace("Quote Charge records retrieved: " + quoteChargeRecords.Entities.Count);

            if (quoteChargeRecords != null && quoteChargeRecords.Entities.Count > 0)
            {
                foreach (var quoteCharge in quoteChargeRecords.Entities)
                {
                    if (quoteCharge.Contains("gsc_actualcost"))
                    {
                        // charge type is freight charges
                        if (quoteCharge.GetAttributeValue<OptionSetValue>("gsc_chargetype").Value == 100000000)
                            totalFreightAmount += quoteCharge.GetAttributeValue<Boolean>("gsc_free")
                            ? Decimal.Zero
                            : quoteCharge.GetAttributeValue<Money>("gsc_actualcost").Value;
                        //Other charge type
                        else
                            totalChargesAmount += quoteCharge.GetAttributeValue<Boolean>("gsc_free")
                            ? Decimal.Zero
                            : quoteCharge.GetAttributeValue<Money>("gsc_actualcost").Value;

                        _tracingService.Trace("Toal Freight: " + totalFreightAmount + " Total Charges:" + totalChargesAmount);
                    }
                }

                if (quoteChargeEntity.Contains("gsc_actualcost") && message.Equals("Delete"))
                {
                    _tracingService.Trace("Message is Delete...");
                    // charge type is freight charges
                    if (quoteChargeEntity.GetAttributeValue<OptionSetValue>("gsc_chargetype").Value == 100000000)
                        totalFreightAmount = totalFreightAmount - quoteChargeEntity.GetAttributeValue<Money>("gsc_actualcost").Value;
                    //Other charge type
                    else
                        totalChargesAmount = totalChargesAmount - quoteChargeEntity.GetAttributeValue<Money>("gsc_actualcost").Value;
                }
            }

            if (quoteRecords != null && quoteRecords.Entities.Count > 0 && quoteRecords.Entities[0].GetAttributeValue<OptionSetValue>("statecode").Value == 0)
            {
                QuoteHandler quoteHandler = new QuoteHandler(_organizationService, _tracingService);

                Entity quote = quoteRecords.Entities[0];

                quote["gsc_totalchargesamount"] = new Money(totalChargesAmount + totalFreightAmount);
                quote["gsc_othercharges"] = new Money(totalChargesAmount);
                quote["gsc_freightandhandling"] = new Money(totalFreightAmount);

                var paymentmode = quote.Contains("gsc_paymentmode")
                  ? quote.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                  : Decimal.Zero;

                //Financing
                if (paymentmode == 100000001)
                {
                    //Compute Cash Outlay
                    quote["gsc_totalcashoutlay"] = new Money(quoteHandler.ComputeCashLayout(quote));
                }

                //Compute Net Price
                quote["gsc_netprice"] = new Money(quoteHandler.ComputeNetPrice(quote));

                //Compute VAT
                quote = quoteHandler.ComputeVAT(quote);

                var amountfinanced = Decimal.Zero;

                //Financing
                if (paymentmode == 100000001 || paymentmode == 100000002)
                {
                    amountfinanced = quoteHandler.ComputeAmountFinanced(quote);
                    quote["gsc_amountfinanced"] = new Money(amountfinanced);
                    quote["gsc_totalamountfinanced"] = new Money(amountfinanced);
                }

                _organizationService.Update(quote);
                _tracingService.Trace("Updated quote entity...");

                return quote;
            }
            _tracingService.Trace("Ended SetQuoteTotalChargesAmount method..");
            return quoteChargeEntity;
        }

        //Created By: Leslie Baliguat, Created On: 3/28/16
        /*Purpose: Replicate Charge fields into Quote Charge
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update: gsc_chargesid
         * Primary Entity: Quote Charge
         */
        public Entity ReplicateChargeAmount(Entity quoteChargeEntity, String message)
        {
            _tracingService.Trace("Started ReplicateChargeAmount Method ...");

            if (quoteChargeEntity.Contains("gsc_quoteid") && quoteChargeEntity.Contains("gsc_chargesid"))
            {
                _tracingService.Trace("Contains Quote and Charges");

                var chargesId = quoteChargeEntity.GetAttributeValue<EntityReference>("gsc_chargesid") != null
                    ? quoteChargeEntity.GetAttributeValue<EntityReference>("gsc_chargesid").Id
                    : Guid.Empty;

                //Retrieve Quote Charges record
                EntityCollection chargesRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_charges", "gsc_cmn_chargesid", chargesId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_description", "gsc_chargeamount", "gsc_chargetype"});

                if (chargesRecords != null && chargesRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Charges Info");

                    Entity chargeEntity = chargesRecords.Entities[0];

                    var chargeAmount = chargeEntity.Contains("gsc_chargeamount")
                        ? chargeEntity.GetAttributeValue<Money>("gsc_chargeamount")
                        : new Money(0);

                    quoteChargeEntity["gsc_description"] = chargeEntity.GetAttributeValue<String>("gsc_description");
                    quoteChargeEntity["gsc_chargeamount"] = chargeAmount;
                    quoteChargeEntity["gsc_actualcost"] = quoteChargeEntity.GetAttributeValue<Boolean>("gsc_free")
                        ? new Money(0)
                        : chargeAmount;
                    quoteChargeEntity["gsc_chargetype"] = chargeEntity.GetAttributeValue<OptionSetValue>("gsc_chargetype");
                   
                    if (message == "Update")
                    {
                        _tracingService.Trace("Updating Quote Charge ...");

                        Entity quoteChargeToUpdate = _organizationService.Retrieve(quoteChargeEntity.LogicalName, quoteChargeEntity.Id, new ColumnSet("gsc_description", "gsc_chargeamount", "gsc_chargetype"));
                        quoteChargeToUpdate["gsc_description"] = chargeEntity.GetAttributeValue<String>("gsc_description");
                        quoteChargeToUpdate["gsc_chargeamount"] = chargeEntity.GetAttributeValue<Money>("gsc_chargeamount");
                        quoteChargeToUpdate["gsc_actualcost"] = chargeEntity.GetAttributeValue<Boolean>("gsc_free")
                            ? new Money(0)
                            : chargeAmount;
                        quoteChargeToUpdate["gsc_chargetype"] = chargeEntity.GetAttributeValue<OptionSetValue>("gsc_chargetype");

                        _organizationService.Update(quoteChargeEntity);
                    }

                    return quoteChargeEntity;
                }
            }

            _tracingService.Trace("Ended ReplicateChargeAmount Method ...");

           return quoteChargeEntity;                
        }

        //Created By: Leslie Baliguat
        public void FreeCharges(Entity quoteChargeEntity)
        {
            var isfree = quoteChargeEntity.GetAttributeValue<Boolean>("gsc_free");

            var actualcost = quoteChargeEntity.Contains("gsc_actualcost")
                ? quoteChargeEntity.GetAttributeValue<Money>("gsc_actualcost")
                : new Money(0);

            var amount = quoteChargeEntity.Contains("gsc_chargeamount")
                ? quoteChargeEntity.GetAttributeValue<Money>("gsc_chargeamount")
                : new Money(0);

            Entity quoteChargeToUpdate = _organizationService.Retrieve(quoteChargeEntity.LogicalName, quoteChargeEntity.Id,
                new ColumnSet("gsc_actualcost"));

            if (isfree)
            {
                quoteChargeToUpdate["gsc_actualcost"] = new Money(0);
            }
            else if (actualcost.Value == 0)
            {
                quoteChargeToUpdate["gsc_actualcost"] = amount;
            }

            _organizationService.Update(quoteChargeToUpdate);
        }
    }
}

