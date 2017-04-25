using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace GSC.Rover.DMS.BusinessLogic.QuoteProduct
{
    public class QuoteProductHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public QuoteProductHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Raphael Herrera, Cretaed On: 4/28/2016
        /*Purpose: Set quote product amount to 0 if accessory set to free
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Quote Product
         */
        public void setActualCost(Entity EntityQuoteProduct)
        {
            _tracingService.Trace("Started set actual cost method...");

            bool isFree = EntityQuoteProduct.GetAttributeValue<bool>("gsc_free");

            if (isFree)
            {
                _tracingService.Trace("Accessory set to free...");
                EntityQuoteProduct["gsc_amount"] = new Money(0);
                _tracingService.Trace("Updating accessory amount...");
                _organizationService.Update(EntityQuoteProduct);
            }

            else
            {
                _tracingService.Trace("Accessory not free...");
                var unitPrice = EntityQuoteProduct.GetAttributeValue<Money>("priceperunit");
                EntityQuoteProduct["gsc_amount"] = unitPrice;
                
                _tracingService.Trace("Updating accessory amount...");
                try
                {
                    _organizationService.Update(EntityQuoteProduct);
                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException(ex.Message);
                }
            }
            _tracingService.Trace("Ending set actual cost method...");
        }
    }
}
