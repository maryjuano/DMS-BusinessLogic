using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.SalesOrder;
using GSC.Rover.DMS.BusinessLogic.PriceList;

namespace GSC.Rover.DMS.BusinessLogic.SalesOrderCabChassis
{
    public class SalesOrderCabChassisHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public SalesOrderCabChassisHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Artum Ramos, Created On: 4/5/2017
        /*Purpose: Populate Item Details
         * Registration Details:
         * Event/Message: 
         *      Pre/Create:
         *      Post/Update: gsc_vehiclecabchassis
         * Primary Entity: gsc_sls_salesOrderCabChassis
         */
        public Entity PopulateDetails(Entity salesOrderCabChassis, String message)
        {
            _tracingService.Trace("Started PopulateDetails Method...");

            Entity orderEntity = RetrieveOrderDetails(salesOrderCabChassis);
            Entity itemEntity = RetrieveCabChasssis(salesOrderCabChassis);

            if (itemEntity != null && orderEntity != null)
            {
                salesOrderCabChassis["gsc_financing"] = orderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value == 100000001
                    ? true
                    : false;
                salesOrderCabChassis["gsc_itemnumber"] = itemEntity.Contains("gsc_itemnumber")
                    ? itemEntity.GetAttributeValue<String>("gsc_itemnumber")
                    : String.Empty;
                var itemId = itemEntity.GetAttributeValue<EntityReference>("gsc_itemid") != null
                     ? itemEntity.GetAttributeValue<EntityReference>("gsc_itemid").Id
                     : Guid.Empty;

                salesOrderCabChassis["gsc_amount"] = RetrivePrice(itemEntity);

                if (message.Equals("Update"))
                {
                    Entity CCToUpdate = _organizationService.Retrieve(salesOrderCabChassis.LogicalName, salesOrderCabChassis.Id,
                        new ColumnSet("gsc_itemnumber", "gsc_amount"));
                    CCToUpdate["gsc_itemnumber"] = salesOrderCabChassis["gsc_itemnumber"];
                    CCToUpdate["gsc_amount"] = salesOrderCabChassis["gsc_amount"];
                    _organizationService.Update(CCToUpdate);
                }
            }
            _tracingService.Trace("Ended PopulateDetails Method...");
            return salesOrderCabChassis;
        }

        public void SetCCAddOnAmount(Entity orderCabChassis, String message)
        {
            _tracingService.Trace("Started SetCCAddOnAmount Method...");
            var orderId = orderCabChassis.Contains("gsc_orderid") ? orderCabChassis.GetAttributeValue<EntityReference>("gsc_orderid").Id
                : Guid.Empty;
            Decimal ccAddOns = 0;

            EntityCollection orderCollection = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", orderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_discount", "gsc_ccaddons", "gsc_netprice", "gsc_unitprice", "gsc_colorprice", "gsc_freightandhandling", "gsc_paymentmode", "gsc_branchid",
                "gsc_vatablesales", "gsc_vatexemptsales", "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount", "gsc_totalamountdue", "gsc_insurance", "gsc_othercharges", "customerid"
                , "gsc_accessories", "gsc_productid", "gsc_downpaymentamount", "gsc_discountamountfinanced"});

            if (orderCollection.Entities.Count > 0)
            {
                Entity orderEntity = orderCollection.Entities[0];

                Decimal paymentMode = orderEntity.Contains("gsc_paymentmode") ? orderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                    : 0;

                //Retrieve all order cab chassis related to quote
                EntityCollection orderCabChassisCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_ordercabchassis", "gsc_orderid", orderId, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_amount", "gsc_financing" });

                if (orderCabChassisCollection.Entities.Count > 0)
                {
                    //Get total cc add on price that are for financing...
                    foreach (Entity quoteCabChassisEntity in orderCabChassisCollection.Entities)
                    {
                        if (quoteCabChassisEntity.Contains("gsc_amount"))
                        {
                            ccAddOns += quoteCabChassisEntity.GetAttributeValue<Money>("gsc_amount").Value;
                            _tracingService.Trace("CC Add Ons Amount: " + ccAddOns);
                        }

                    }

                    //Subtract sellprice of deleted.
                    if (orderCabChassis.Contains("gsc_amount") && message.Equals("Delete"))
                    {
                        _tracingService.Trace("Message is Delete...");
                        ccAddOns = ccAddOns - (Decimal)orderCabChassis.GetAttributeValue<Money>("gsc_amount").Value;
                    }

                    orderEntity["gsc_ccaddons"] = new Money(ccAddOns);

                    #region Recalculate net price

                    SalesOrderHandler orderHandler = new SalesOrderHandler(_organizationService, _tracingService);
                    orderEntity["gsc_netprice"] = new Money(orderHandler.ComputeNetPrice(orderEntity));
                    orderEntity = orderHandler.ComputeVAT(orderEntity);

                    var paymentmode = orderEntity.Contains("gsc_paymentmode")
                        ? orderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                        : Decimal.Zero;
                    var amountfinanced = Decimal.Zero;

                    //Financing
                    if (paymentmode == 100000001 || paymentmode == 100000002)
                    {
                        amountfinanced = orderHandler.ComputeAmountFinanced(orderEntity);
                        orderEntity["gsc_amountfinanced"] = new Money(amountfinanced);
                        orderEntity["gsc_totalamountfinanced"] = new Money(amountfinanced);
                    }

                    #endregion

                    if (ccAddOns >= 0)
                    {
                        _organizationService.Update(orderEntity);

                    }
                    else
                        _tracingService.Trace("CC add on is negative...");
                }
            }
            _tracingService.Trace("ending SetCCAddOnAmount Entity");
        }

        //Retrieve Dealer and Branch from Parent Entity (rder)
        private Entity RetrieveOrderDetails(Entity orderCabChassis)
        {
            var orderId = orderCabChassis.Contains("gsc_orderid")
                ? orderCabChassis.GetAttributeValue<EntityReference>("gsc_orderid").Id
                : Guid.Empty;

            EntityCollection quoteCollection = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", orderId, _organizationService, null, OrderType.Ascending,
                 new[] { "gsc_branchid", "gsc_dealerid", "gsc_paymentmode" });

            if (quoteCollection != null && quoteCollection.Entities.Count > 0)
            {
                return quoteCollection.Entities[0];
            }
            return null;
        }

        //Retrieve Vehicle Cab Chassis Details 
        private Entity RetrieveCabChasssis(Entity orderCabChassis)
        {
            var cabChassisId = orderCabChassis.GetAttributeValue<EntityReference>("gsc_vehiclecabchassisid") != null
                ? orderCabChassis.GetAttributeValue<EntityReference>("gsc_vehiclecabchassisid").Id
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
        private Money RetrivePrice(Entity orderCabChassis)
        {
            /* Legend: 
             * itemType = 0 : Accessory
             * itemType = 1 : Cab Chassis*/

            PriceListHandler priceListHandler = new PriceListHandler(_organizationService, _tracingService);
            priceListHandler.itemType = 2;
            priceListHandler.productFieldName = "gsc_itemid";
            List<Entity> latestPriceList = priceListHandler.RetrievePriceList(orderCabChassis, 100000000, 100000002);
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
