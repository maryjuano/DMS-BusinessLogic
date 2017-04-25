using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.Quote;
using GSC.Rover.DMS.BusinessLogic.PriceList;

namespace GSC.Rover.DMS.BusinessLogic.QuoteCabChassis
{
    public class QuoteCabChassisHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public QuoteCabChassisHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 9/27/2016
        /*Purpose: Populate Item Details
         * Registration Details:
         * Event/Message: 
         *      Pre/Create:
         *      Post/Update: gsc_vehiclecabchassis
         * Primary Entity: gsc_sls_quotecabchassis
         */
        public void PopulateDetails(Entity quoteCabChassis, String message)
        {
            _tracingService.Trace("Started PopulateDetails Method...");

            Entity quoteEntity = RetrieveQuoteDetails(quoteCabChassis);
            Entity itemEntity = RetrieveCabChasssis(quoteCabChassis);

            if (itemEntity != null && quoteEntity != null)
            {
                quoteCabChassis["gsc_financing"] = quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value == 100000001
                    ? true
                    : false;
                quoteCabChassis["gsc_itemnumber"] = itemEntity.Contains("gsc_itemnumber")
                    ? itemEntity.GetAttributeValue<String>("gsc_itemnumber")
                    : String.Empty;
                var itemId = itemEntity.GetAttributeValue<EntityReference>("gsc_itemid") != null
                     ? itemEntity.GetAttributeValue<EntityReference>("gsc_itemid").Id
                     : Guid.Empty;

                quoteCabChassis["gsc_amount"] = RetrivePrice(itemEntity);

                if (message.Equals("Update"))
                {
                    Entity CCToUpdate = _organizationService.Retrieve(quoteCabChassis.LogicalName, quoteCabChassis.Id,
                        new ColumnSet("gsc_itemnumber", "gsc_amount"));
                    CCToUpdate["gsc_itemnumber"] = quoteCabChassis["gsc_itemnumber"];
                    CCToUpdate["gsc_amount"] = quoteCabChassis["gsc_amount"];
                    _organizationService.Update(CCToUpdate);
                }
            }
            _tracingService.Trace("Ended PopulateDetails Method...");
        }


        //Created By: Raphael Herrera, Created On: 9/19/2016
        /*Purpose: Set CC Add-Ons Amount in Quote Entity
         * Registration Details:
         * Event/Message: 
         *      Post/Create: QuoteCabChassis
         *      Pre/Delete: QuoteCabChassis
         *      Post/Update: QuoteCabChassis
         *      Post/Update: quote gsc_paymentmode
         * Primary Entity: gsc_sls_quotecabchassis
         */
        public void SetCCAddOnAmount(Entity quoteCabChassis, String message)
        {
            _tracingService.Trace("Started SetCCAddOnAmount Method...");
            var quoteId = quoteCabChassis.Contains("gsc_quoteid") ? quoteCabChassis.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;
            Decimal ccAddOns = 0;

            EntityCollection quoteCollection = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_ccaddons", "gsc_netprice", "gsc_totaldiscount", "gsc_unitprice", "gsc_colorprice", "gsc_freightandhandling", "gsc_paymentmode", "gsc_branchid",
                "gsc_vatablesales", "gsc_vatexemptsales", "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount", "gsc_totalamountdue", "gsc_insurance", "gsc_othercharges", "customerid"
                , "gsc_accessories", "gsc_productid"});

            _tracingService.Trace("Quote Records retrieved: " + quoteCollection.Entities.Count);
            if (quoteCollection.Entities.Count > 0)
            {
                Entity quoteEntity = quoteCollection.Entities[0];

                Decimal paymentMode = quoteEntity.Contains("gsc_paymentmode") ? quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                    : 0;

                //Retrieve all quote cab chassis related to quote
                EntityCollection quoteCabChassisCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_quotecabchassis", "gsc_quoteid", quoteId, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_amount", "gsc_financing" });

                _tracingService.Trace("Quote Cab Chassis Records Retrieved: " + quoteCabChassisCollection.Entities.Count);
                if (quoteCabChassisCollection.Entities.Count > 0)
                {
                    //Get total cc add on price that are for financing...
                    foreach (Entity quoteCabChassisEntity in quoteCabChassisCollection.Entities)
                    {
                        if (quoteCabChassisEntity.Contains("gsc_amount"))
                        {
                            ccAddOns += quoteCabChassisEntity.GetAttributeValue<Money>("gsc_amount").Value;
                            _tracingService.Trace("CC Add Ons Amount: " + ccAddOns);
                        }

                    }

                    //Subtract sellprice of deleted.
                    if (quoteCabChassis.Contains("gsc_amount") && message.Equals("Delete"))
                    {
                        _tracingService.Trace("Message is Delete...");
                        ccAddOns = ccAddOns - (Decimal)quoteCabChassis.GetAttributeValue<Money>("gsc_amount").Value;
                    }

                    quoteEntity["gsc_ccaddons"] = new Money(ccAddOns);

                    #region Recalculate net price

                    QuoteHandler quoteHandler = new QuoteHandler(_organizationService, _tracingService);
                    quoteEntity["gsc_netprice"] = new Money(quoteHandler.ComputeNetPrice(quoteEntity));
                    quoteEntity = quoteHandler.ComputeVAT(quoteEntity);

                    var paymentmode = quoteEntity.Contains("gsc_paymentmode")
                        ? quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                        : Decimal.Zero;
                    var amountfinanced = Decimal.Zero;

                    //Financing
                    if (paymentmode == 100000001 || paymentmode == 100000001)
                    {
                        amountfinanced = quoteHandler.ComputeAmountFinanced(quoteEntity);
                        quoteEntity["gsc_amountfinanced"] = new Money(amountfinanced);
                        quoteEntity["gsc_totalamountfinanced"] = new Money(amountfinanced);
                    }

                    #endregion

                    if (ccAddOns >= 0)
                    {
                        _organizationService.Update(quoteEntity);
                        _tracingService.Trace("CC Add Ons Amount:" + ccAddOns + ". Updated quote entity...");

                    }
                    else
                        _tracingService.Trace("CC add on is negative...");

                }
                
            }
            _tracingService.Trace("ending SetCCAddOnAmount Entity");
        }

        //Retrieve Dealer and Branch from Parent Entity (Quote)
        private Entity RetrieveQuoteDetails(Entity quoteCabChassis)
        {
            var quoteId = quoteCabChassis.Contains("gsc_quoteid")
                ? quoteCabChassis.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;

            EntityCollection quoteCollection = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                 new[] { "gsc_branchid", "gsc_dealerid", "gsc_paymentmode" });

            if (quoteCollection != null && quoteCollection.Entities.Count > 0)
            {
                return quoteCollection.Entities[0];
            }
            return null;
        }

        //Retrieve Vehicle Cab Chassis Details 
        private Entity RetrieveCabChasssis(Entity quoteCabChassis)
        {
            var cabChassisId = quoteCabChassis.GetAttributeValue<EntityReference>("gsc_vehiclecabchassisid") != null
                ? quoteCabChassis.GetAttributeValue<EntityReference>("gsc_vehiclecabchassisid").Id
                : Guid.Empty;

            EntityCollection itemCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_vehiclecabchassis", "gsc_sls_vehiclecabchassisid", cabChassisId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_itemnumber", "gsc_itemid" });

            if (itemCollection != null && itemCollection.Entities.Count > 0)
            {
                return itemCollection.Entities[0];
            }
            return null;
        }

        //Retrieve Price from the Latest Price List
        private Money RetrivePrice(Entity quoteCabChassis)
        {
            /* Legend: 
             * itemType = 0 : Accessory
             * itemType = 1 : Cab Chassis*/

            PriceListHandler priceListHandler = new PriceListHandler(_organizationService, _tracingService);
            priceListHandler.itemType = 2;
            priceListHandler.productFieldName = "gsc_itemid";
            List<Entity> latestPriceList = priceListHandler.RetrievePriceList(quoteCabChassis, 100000000, 100000002);
            if (latestPriceList.Count > 0)
            {
                Entity priceListItem = latestPriceList[0];
                Entity priceList = latestPriceList[1];

                return priceListItem.GetAttributeValue<Money>("amount");
            }
            else
            {
                throw new InvalidPluginExecutionException("There is no effecive Price List for the selected Vehicle.");
            }
        }

    }
}
