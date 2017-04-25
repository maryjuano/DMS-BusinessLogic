using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.AppliedPriceList
{
    public class AppliedPriceListHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public AppliedPriceListHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 2/19/2016
        public Entity SetTotalDiscountAmountQuote(Entity appliedPriceListEntity, String message)
        {
            _tracingService.Trace("Started SetTotalDiscountAmount method..");
            
            Decimal totalDiscountAmount = 0;
            
            var quoteId = appliedPriceListEntity.GetAttributeValue<EntityReference>("gsc_quoteid") != null
                ? appliedPriceListEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;


            //Retrieve Applied Price List records with the same Quote
            EntityCollection appliedPriceListQuoteRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_appliedpricelist", "gsc_quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_discountamount" });

            //Retrieve Quote record from Quote field value
            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "totaldiscountamount", "statecode" });

            if (appliedPriceListQuoteRecords != null && appliedPriceListQuoteRecords.Entities.Count > 0)
            {
                //Compute for Total Discount Amount from all retrieved Quote records
                foreach (var appliedPriceList in appliedPriceListQuoteRecords.Entities)
                {
                    totalDiscountAmount += appliedPriceList.GetAttributeValue<Money>("gsc_discountamount").Value;
                }

                if (appliedPriceListEntity.Contains("gsc_discountamount") && message.Equals("Delete"))
                {
                    totalDiscountAmount = totalDiscountAmount - (Decimal)appliedPriceListEntity.GetAttributeValue<Money>("gsc_discountamount").Value;
                }
            }            

            if (quoteRecords != null && quoteRecords.Entities.Count > 0 && quoteRecords.Entities[0].GetAttributeValue<OptionSetValue>("statecode").Value == 0)
            {
                Entity quote = quoteRecords.Entities[0];
                quote["totaldiscountamount"] = new Money(totalDiscountAmount);
                _organizationService.Update(quote);

                return quote;
            }
            
            _tracingService.Trace("Ended SetTotalDiscountAmount method..");
            return appliedPriceListEntity;
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 3/10/2016
        public Entity SetTotalDiscountAmountOrder(Entity appliedPriceListEntity, String message)
        {
            _tracingService.Trace("Started SetTotalDiscountAmountOrder method..");
            
            Decimal totalDiscountAmount = 0;
            
            var salesOrderId = appliedPriceListEntity.GetAttributeValue<EntityReference>("gsc_orderid") != null
                ? appliedPriceListEntity.GetAttributeValue<EntityReference>("gsc_orderid").Id
                : Guid.Empty;
            
            //Retrieve Applied Price List records with the same Order
            EntityCollection appliedPriceListOrderRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_appliedpricelist", "gsc_orderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_discountamount" });
            
            //Retrieve Quote record from Quote field value
            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "totaldiscountamount" });

            if (appliedPriceListOrderRecords != null && appliedPriceListOrderRecords.Entities.Count > 0)
            {
                //Compute for Total Discount Amount from all retrieved Order records
                foreach (var appliedPriceList in appliedPriceListOrderRecords.Entities)
                {
                    totalDiscountAmount += appliedPriceList.GetAttributeValue<Money>("gsc_discountamount").Value;
                }

                if (appliedPriceListEntity.Contains("gsc_discountamount") && message.Equals("Delete"))
                {
                    totalDiscountAmount = totalDiscountAmount - (Decimal)appliedPriceListEntity.GetAttributeValue<Money>("gsc_discountamount").Value;
                }
            }
            
            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];
                salesOrder["totaldiscountamount"] = new Money(totalDiscountAmount);
                _organizationService.Update(salesOrder);

                return salesOrder;
            }
            _tracingService.Trace("Ended SetTotalDiscountAmountOrder method..");
            return appliedPriceListEntity;
        }
    }
}
