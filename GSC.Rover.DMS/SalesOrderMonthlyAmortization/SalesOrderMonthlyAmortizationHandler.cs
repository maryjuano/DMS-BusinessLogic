using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.SalesOrderMonthlyAmortization
{
    public class SalesOrderMonthlyAmortizationHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public SalesOrderMonthlyAmortizationHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/6/2016
        public void SetNetMonthlyAmortization(Entity salesOrderMonthlyAmortizationEntity)
        {
            _tracingService.Trace("Started SetNetMonthlyAmortization method..");

            var salesOrderId = salesOrderMonthlyAmortizationEntity.GetAttributeValue<EntityReference>("gsc_orderid") != null
                ? salesOrderMonthlyAmortizationEntity.GetAttributeValue<EntityReference>("gsc_orderid").Id
                : Guid.Empty;

            Boolean isSelected = salesOrderMonthlyAmortizationEntity.GetAttributeValue<Boolean>("gsc_selected");

            if (isSelected == true)
            {
                //Retrieve Sales Order record
                EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_netmonthlyamortization" });

                if (salesOrderRecords != null || salesOrderRecords.Entities.Count > 0)
                {
                    Entity salesOrder = salesOrderRecords.Entities[0];

                    var monthlyAmortizationAmount = salesOrderMonthlyAmortizationEntity.GetAttributeValue<String>("gsc_ordermonthlyamortizationpn").Trim(',');
                    salesOrder["gsc_netmonthlyamortization"] = new Money(Decimal.Parse(monthlyAmortizationAmount));

                    _organizationService.Update(salesOrder);
                }


                //Uncheck Selected field in every other Sales Order Monthly Amortization record
                var salesOrderMonthlyAmortizationConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_orderid", ConditionOperator.Equal, salesOrderId),
                    new ConditionExpression("gsc_sls_ordermonthlyamortizationid", ConditionOperator.NotEqual, salesOrderMonthlyAmortizationEntity.Id)
                };

                //Retrieve Sales Order Monthly Amortization records
                EntityCollection salesOrderMonthlyAmortizationRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_ordermonthlyamortization", salesOrderMonthlyAmortizationConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_selected" });

                if (salesOrderMonthlyAmortizationRecords != null || salesOrderMonthlyAmortizationRecords.Entities.Count > 0)
                {
                    foreach (Entity salesOrderMonthlyAmortization in salesOrderMonthlyAmortizationRecords.Entities)
                    {
                        if (salesOrderMonthlyAmortization.GetAttributeValue<Boolean>("gsc_selected") == true)
                        {
                            salesOrderMonthlyAmortization["gsc_selected"] = false;

                            _organizationService.Update(salesOrderMonthlyAmortization);

                            break;
                        }
                    }
                }
            }
            else
            {
                SetNullNetMonthlyAmortization(salesOrderMonthlyAmortizationEntity);
            }
            _tracingService.Trace("Ended SetNetMonthlyAmortization method..");
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 4/6/2016
        private void SetNullNetMonthlyAmortization(Entity salesOrderMonthlyAmortizationEntity)
        {
            _tracingService.Trace("Started SetNullNetMonthlyAmortization method..");

            var salesOrderId = salesOrderMonthlyAmortizationEntity.GetAttributeValue<EntityReference>("gsc_orderid") != null
                ? salesOrderMonthlyAmortizationEntity.GetAttributeValue<EntityReference>("gsc_orderid").Id
                : Guid.Empty;

            //Retrieve Sales Order record
            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_netmonthlyamortization" });

            if (salesOrderRecords != null || salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];

                var salesOrderMonthlyAmortizationConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_orderid", ConditionOperator.Equal, salesOrderId),
                    new ConditionExpression("gsc_sls_ordermonthlyamortizationid", ConditionOperator.NotEqual, salesOrderMonthlyAmortizationEntity.Id),
                    new ConditionExpression("gsc_selected", ConditionOperator.Equal, true)
                };

                EntityCollection salesOrderMonthlyAmortizationRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_ordermonthlyamortization", salesOrderMonthlyAmortizationConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_selected" });

                if (salesOrderMonthlyAmortizationRecords == null || salesOrderMonthlyAmortizationRecords.Entities.Count == 0)
                {
                    salesOrder["gsc_netmonthlyamortization"] = null;

                    _organizationService.Update(salesOrder);
                }
            }
            _tracingService.Trace("Ended SetNullNetMonthlyAmortization method..");
        }
    }
}
