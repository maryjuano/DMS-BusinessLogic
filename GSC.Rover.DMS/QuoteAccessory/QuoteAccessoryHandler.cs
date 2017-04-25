using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.PriceList;
using GSC.Rover.DMS.BusinessLogic.Quote;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.QuoteAccessory
{
    public class QuoteAccessoryHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public QuoteAccessoryHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 9/27/2016
        /*Purpose: Populate Item Details
         * Registration Details:
         * Event/Message: 
         *      Pre/Create:
         *      Post/Update: gsc_productid
         * Primary Entity: gsc_sls_quoteaccessorry
         */
        public Entity PopulateDetails(Entity quoteAccessory, String message)
        {
            _tracingService.Trace("Started PopulateDetails Method...");

            Entity quoteEntity = RetrieveBranchDealer(quoteAccessory);
            Entity itemEntity = RetrieveAccessory(quoteAccessory);

            if (itemEntity != null && quoteEntity != null)
             {
                 quoteAccessory["gsc_itemnumber"] = itemEntity.Contains("productnumber")
                     ? itemEntity.GetAttributeValue<String>("productnumber")
                     : String.Empty;
                 quoteAccessory["gsc_branchid"] = quoteEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                     ? quoteEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                     : null;
                 quoteAccessory["gsc_dealerid"] = quoteEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                     ? quoteEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                     : null;

                 Money sellingPrice = RetrivePrice(quoteAccessory);
                 var isFree = quoteAccessory.Contains("gsc_free")
                     ? quoteAccessory.GetAttributeValue<Boolean>("gsc_free")
                     : false;

                 quoteAccessory["gsc_amount"] = sellingPrice;

                 if (isFree)
                 {
                     _tracingService.Trace("Free");

                     quoteAccessory["gsc_actualcost"] = new Money(0);
                 }
                 else
                 {
                     _tracingService.Trace("Not Free");
                     quoteAccessory["gsc_actualcost"] = sellingPrice;
                 }

                 if (message.Equals("Update"))
                 {
                     Entity accessoryToUpdate = _organizationService.Retrieve(quoteAccessory.LogicalName, quoteAccessory.Id,
                         new ColumnSet("gsc_itemnumber", "gsc_amount", "gsc_actualcost"));
                     accessoryToUpdate["gsc_itemnumber"] = quoteAccessory["gsc_itemnumber"];
                     accessoryToUpdate["gsc_amount"] = quoteAccessory["gsc_amount"];
                     accessoryToUpdate["gsc_actualcost"] = quoteAccessory["gsc_actualcost"];
                     _organizationService.Update(accessoryToUpdate);
                 }
             }
            _tracingService.Trace("Ended PopulateDetails Method...");
            return quoteAccessory;
        }

        //Created By: Leslie Baliguat, Created On: 9/27/2016
        /*Purpose: Set Accessories in Quote
         * Registration Details:
         * Event/Message: 
         *      Post/update: gsc_free
         * Primary Entity: gsc_sls_quoteaccessorry
         */
        public Entity UpdateActualCost(Entity quoteAccessory)
        {
            _tracingService.Trace("Started UpdateActualCost Method...");

            Entity quoteAccessoryToUpdate = _organizationService.Retrieve(quoteAccessory.LogicalName, quoteAccessory.Id,
                new ColumnSet("gsc_actualcost", "gsc_quoteid"));
            
            var amount = quoteAccessory.Contains("gsc_amount")
                ? quoteAccessory.GetAttributeValue<Money>("gsc_amount")
                : new Money(0);

            var isFree = quoteAccessory.Contains("gsc_free")
                ? quoteAccessory.GetAttributeValue<Boolean>("gsc_free")
                : false;

            if (isFree)
            {
                _tracingService.Trace("Free");

                quoteAccessoryToUpdate["gsc_actualcost"] = new Money(0);
            }
            else
            {
                _tracingService.Trace("Not Free");

                quoteAccessoryToUpdate["gsc_actualcost"] = amount;
            }

            _organizationService.Update(quoteAccessoryToUpdate);

            _tracingService.Trace("Ended UpdateActualCost Method...");

            return quoteAccessoryToUpdate;
        }

        //Created By: Leslie Baliguat, Created On: 9/27/2016
        /*Purpose: Set Accessories in Quote
         * Registration Details:
         * Event/Message: 
         *      Post/Create: 
         *      Pre/Delete: 
         *      Post/Update: gsc_free, gsc_productid
         * Primary Entity: gsc_sls_quoteaccessorry
         */
        public void SetTotalAccessories(Entity quoteAccessory, string message)
        {
            _tracingService.Trace("Started SetTotalAccessories Method...");

            var quoteId = quoteAccessory.Contains("gsc_quoteid") ? quoteAccessory.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;

            Decimal totalAccessories = 0;

            EntityCollection quoteCollection = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_downpayment", "gsc_chattelfee", "gsc_insurance", "gsc_othercharges", "gsc_accessories", "gsc_netprice", "gsc_paymentmode", "gsc_branchid",
                "gsc_vatablesales", "gsc_vatexemptsales", "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount", "gsc_totalamountdue", "customerid", "gsc_productid"});

            if (quoteCollection != null && quoteCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Quote");

                var quoteEntity = quoteCollection.Entities[0];

                //Retrieve all quote cab chassis related to quote
                EntityCollection quoteAccessoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_quoteaccessory", "gsc_quoteid", quoteEntity.Id, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_actualcost"});

                if (quoteAccessoryCollection.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Quote Accessories");

                    //Get total cc add on price that are for financing...
                    foreach (Entity quoteAccessoryEntity in quoteAccessoryCollection.Entities)
                    {
                        if (quoteAccessoryEntity.Contains("gsc_actualcost"))
                        {
                            totalAccessories += quoteAccessoryEntity.GetAttributeValue<Money>("gsc_actualcost").Value;
                            _tracingService.Trace("Accessories:" + totalAccessories);
                        }
                        
                    }

                    _tracingService.Trace("total Accessories: " + totalAccessories);
                }

                //Add Current record to total Accessories
                if (message == "Create")
                   totalAccessories += quoteAccessory.Contains("gsc_actualcost") ? quoteAccessory.GetAttributeValue<Money>("gsc_actualcost").Value : 0;

                //Subtract sellprice of deleted.
                if (quoteAccessory.Contains("gsc_actualcost") && message.Equals("Delete"))
                {
                    _tracingService.Trace("Message is Delete...");
                    _tracingService.Trace("To be subtract: " + (Decimal)quoteAccessory.GetAttributeValue<Money>("gsc_actualcost").Value);
                    totalAccessories = totalAccessories - (Decimal)quoteAccessory.GetAttributeValue<Money>("gsc_actualcost").Value;
                    totalAccessories = totalAccessories < 0 ? 0 : totalAccessories;
                }

                _tracingService.Trace("total Accessories: " + totalAccessories);
                quoteEntity["gsc_accessories"] = new Money(totalAccessories);

                #region Recalculate total cash outlay

                QuoteHandler quoteHandler = new QuoteHandler(_organizationService, _tracingService);
                var paymentmode = quoteEntity.Contains("gsc_paymentmode")
                  ? quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                  : Decimal.Zero;

                //Financing
                if (paymentmode == 100000001)
                {
                    quoteEntity["gsc_totalcashoutlay"] = new Money(quoteHandler.ComputeCashLayout(quoteEntity));
                }

                quoteEntity = quoteHandler.ComputeVAT(quoteEntity);

                _organizationService.Update(quoteEntity);

                _tracingService.Trace("Quote Computation Updated");
                #endregion

            }

            _tracingService.Trace("Ended SetTotalAccessories Method...");

        }

        //Created By: Leslie Baliguat, Created On: 4/17/2017
        /*Purpose: Cannot delete acccessory that are default by vehicle model
         * Registration Details:
         * Event/Message: 
         *      PreValidate/Delete: 
         * Primary Entity: gsc_sls_quoteaccessorry
         */
        public Boolean IsAccessoryStandard(Entity quoteAccessory)
        {
            if (quoteAccessory.GetAttributeValue<Boolean>("gsc_standard"))
                return true;

            return false;
        }

        //Retrieve Price from the Latest Price List
        private Money RetrivePrice(Entity quoteAccessory)
        {
            /* Legend: 
             * itemType = 0 : Accessory
             * itemType = 1 : Cab Chassis*/

            PriceListHandler priceListHandler = new PriceListHandler(_organizationService, _tracingService);
            priceListHandler.itemType = 1;
            priceListHandler.productFieldName = "gsc_productid";
            List<Entity> latestPriceList = priceListHandler.RetrievePriceList(quoteAccessory, 100000000, 100000002);
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

        //Retrieve Dealer and Branch from Parent Entity (Quote)
        private Entity RetrieveBranchDealer(Entity quoteAccessory)
        {
            var quoteId = quoteAccessory.Contains("gsc_quoteid") 
                ? quoteAccessory.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;

            EntityCollection quoteCollection = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                 new[] { "gsc_branchid", "gsc_dealerid" });

            if (quoteCollection != null && quoteCollection.Entities.Count > 0)
             {
                 return quoteCollection.Entities[0];
             }
            return null;
        }

        //Retrieve Product No. 
        private Entity RetrieveAccessory(Entity quoteAccessory)
        {
             var itemId = quoteAccessory.Contains("gsc_productid") ? quoteAccessory.GetAttributeValue<EntityReference>("gsc_productid").Id
                 : Guid.Empty;

             EntityCollection itemCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", itemId, _organizationService, null, OrderType.Ascending,
                 new[] { "productnumber"});

             if (itemCollection != null && itemCollection.Entities.Count > 0)
             {
                 return itemCollection.Entities[0];
             }
             return null;
        }
    }
}
