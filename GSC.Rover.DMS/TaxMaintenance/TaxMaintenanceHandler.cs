using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.TaxMaintenance
{
    public class TaxMaintenanceHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public TaxMaintenanceHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jessica Casupanan, Created On : 01/14/2017
        /*Purpose: Restrict to one tax default only
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: gsc_cmn_taxmaintenanceid
         *      Post/Update: gsc_cmn_taxmaintenanceid
         *      Post/Create:
         * Primary Entity: Tax Maintenance
         */

        public Entity TaxDefaultUpdate(Entity taxMaintenance)
        {
            _tracingService.Trace("TaxDefaultUpdate Method Started ");
            var taxCondition = new List<ConditionExpression>
                        {
                            new ConditionExpression("gsc_cmn_taxmaintenanceid", ConditionOperator.NotEqual, taxMaintenance.Id),
                            new ConditionExpression("gsc_isdefault", ConditionOperator.Equal, true)
                        };

            EntityCollection taxCollection = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_taxmaintenance", taxCondition, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_isdefault", "gsc_cmn_taxmaintenanceid" });

            _tracingService.Trace("Tax Collection Records Retrieved: ");
            if (taxCollection.Entities.Count > 0)
            {
                //
                foreach (Entity tax in taxCollection.Entities)
                {

                    tax["gsc_isdefault"] = false;
                    _organizationService.Update(tax);
                }
            }
            _tracingService.Trace("TaxDefaultUpdate Method Ende ");
            return taxMaintenance;
        }


        //Created By : Raphael Herrera, Created On : 01/20/2017
        /*Purpose: Update Vehicle Tax Rates with updated tax rate
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: gsc_rate
         *      Post/Create:
         * Primary Entity: Tax Maintenance
         */
        public Entity UpdateVehicleTaxRate(Entity taxMaintenance)
        {
            _tracingService.Trace("Starting UpdateVehicleTaxRate Method");
            var rate = taxMaintenance.Contains("gsc_rate") ? taxMaintenance.GetAttributeValue<Double>("gsc_rate") : 0;

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "gsc_taxid", taxMaintenance.Id, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_taxrate" });

            _tracingService.Trace("Product Records Retrieved: " + productCollection.Entities.Count);
            if (productCollection.Entities.Count > 0)
            {
                foreach (Entity productEntity in productCollection.Entities)
                {
                    productEntity["gsc_taxrate"] = rate;
                    _organizationService.Update(productEntity);
                    _tracingService.Trace("Updated Vehicle Tax Rate...");                    
                }
            
            }
            _tracingService.Trace("Ending UpdateVehicleTaxRate Method");
            return taxMaintenance;
        }
    }
}
