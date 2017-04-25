using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;
using System;
using System.Collections.Generic;


namespace GSC.Rover.DMS.BusinessLogic.QuoteMonthlyAmortization
{
    public class QuoteMonthlyAmortizationHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public QuoteMonthlyAmortizationHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Create By: Leslie Baliguat, Created On: 3/4/2016 /*Purpose: Replicate Prospect Information to Prospect Inquiry 
       /* Purpose: Once a monthly amortization record was tagged "selected", 
        * it's monthly amortization will be replicated to it's quotes's net monthly amortization field
        * and other monthly amortization with the same quote id, selected field will be unchecked
         * Registration Details:
        * Event/Message: 
        *      Post/Update: Selected 
        * Primary Entity: Monthly Amortization
        */
        public void ReplicateMonthlyAmortization(Entity monthlyAmortizationEntity)
        {
            _tracingService.Trace("Started ReplicateMonthlyAmortization method ...");

            if (monthlyAmortizationEntity.Contains("gsc_quoteid") || monthlyAmortizationEntity.GetAttributeValue<EntityReference>("gsc_quoteid") != null)
            {
                var quoteid = monthlyAmortizationEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id;
                var isSelected = monthlyAmortizationEntity.GetAttributeValue<Boolean>("gsc_isselected");

                if (isSelected == true)
                {
                    _tracingService.Trace("isSelected is true ...");

                    //update net monthly amortization  in quote
                    _tracingService.Trace("Retrieve Quote Record...");

                    EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteid, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_netmonthlyamortization" });

                    if (quoteRecords != null && quoteRecords.Entities.Count > 0)
                    {
                        _tracingService.Trace("Update Net Monthly Amortization in Quote ...");

                        Entity quoteEntity = quoteRecords.Entities[0];

                        var monthlyDecimal = monthlyAmortizationEntity["gsc_quotemonthlyamortizationpn"].ToString().Trim(',');
                        quoteEntity["gsc_netmonthlyamortization"] = new Money(Decimal.Parse(monthlyDecimal));

                        _organizationService.Update(quoteEntity);
                    }

                    //check if there is other monthly amortization record which gsc_isselected is checked
                    _tracingService.Trace("Retrieve Monthly Amortization Records associated with the same Quote...");

                    var monthlyAmotizationConditionList = new List<ConditionExpression>
                        {
                            new ConditionExpression("gsc_quoteid", ConditionOperator.Equal, quoteid),
                            new ConditionExpression("gsc_sls_quotemonthlyamortizationid", ConditionOperator.NotEqual, monthlyAmortizationEntity.Id)
                        };

                    EntityCollection monthlyAmortizationRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_quotemonthlyamortization", monthlyAmotizationConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_isselected" });

                    if (monthlyAmortizationRecords != null || monthlyAmortizationRecords.Entities.Count > 0)
                    {
                        foreach (Entity monthlyAmortization in monthlyAmortizationRecords.Entities)
                        {
                            var monthlyAmortization_isSelected = monthlyAmortization.GetAttributeValue<Boolean>("gsc_isselected");

                            if (monthlyAmortization_isSelected == true)
                            {
                                _tracingService.Trace("Update isSelected field which value is true");

                                monthlyAmortization["gsc_isselected"] = false;

                                _organizationService.Update(monthlyAmortization);

                                break;
                            }
                        }
                    }
                }
                else
                {
                    CheckMonthlyAmortizationRecords(monthlyAmortizationEntity);
                }
            }

            _tracingService.Trace("Ended ReplicateMonthlyAmortization method ...");
        }

        //Created By: Leslie Baliguat, Created On: 3/4/2016 /* Purpose: Once a monthly amortization record was tagged "selected", 
        /* If there is no monthly amortization record in the same quote id
         * is selected, net monthly amortization field in quote will be set to null
        */
        private void CheckMonthlyAmortizationRecords(Entity monthlyAmortizationEntity)
        {
            _tracingService.Trace("Started CheckMonthlyAmortizationRecords method ...");

            var quoteid = monthlyAmortizationEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id;

            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteid, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_netmonthlyamortization" });

            if (quoteRecords != null && quoteRecords.Entities.Count > 0)
            {
                Entity quoteEntity = quoteRecords.Entities[0];

                var monthlyAmotizationConditionList = new List<ConditionExpression>
                {
                     new ConditionExpression("gsc_quoteid", ConditionOperator.Equal, quoteid),
                     new ConditionExpression("gsc_sls_quotemonthlyamortizationid", ConditionOperator.NotEqual, monthlyAmortizationEntity.Id),
                     new ConditionExpression("gsc_isselected", ConditionOperator.Equal, true)
                };

                EntityCollection monthlyAmortizationRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_quotemonthlyamortization", monthlyAmotizationConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_isselected" });

                if (monthlyAmortizationRecords == null || monthlyAmortizationRecords.Entities.Count == 0)
                {
                    quoteEntity["gsc_netmonthlyamortization"] = new Money(0);

                    _organizationService.Update(quoteEntity);
                }
            }
        }
    }
}
