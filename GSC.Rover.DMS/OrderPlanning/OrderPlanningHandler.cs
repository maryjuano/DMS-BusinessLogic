using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace GSC.Rover.DMS.BusinessLogic.OrderPlanning
{
    public class OrderPlanningHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public OrderPlanningHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        ///Created By: Leslie Baliguat, Created On: 5/30/2016
        /*Purpose: Create Order Planning Detail for the current month
         *          On Create, Compute Retail Average Sales and Ending Inventory
         *          This runs when gsc_createdetailforcurrentmonth checkbox is equals to true
         * Registration Details: 
         * Event/Message: 
         *      Post/Update: gsc_createdetailforcurrentmonth
         * Primary Entity: Order Planning
         */
        public Entity CreateDetailsForThisMonth(Entity orderPlanningEntity)
        {

            EntityCollection selectedOrderPlanning = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_orderplanning", "gsc_sls_orderplanningid", orderPlanningEntity.Id, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_createdetailforcurrentmonth", "gsc_retailperiodcoverage", "gsc_dealerid", 
                "gsc_branchid", "gsc_productid", "gsc_siteid" });

            if (selectedOrderPlanning != null && selectedOrderPlanning.Entities.Count > 0)
            {
                Entity orderPlanning = selectedOrderPlanning.Entities[0];

                if (orderPlanning.GetAttributeValue<Boolean>("gsc_createdetailforcurrentmonth"))
                {
                    _tracingService.Trace("Strated CreateDetails Method");

                    var dateToday = DateTime.Now;

                    var retailAverage = ComputeRetailAverageSales(orderPlanning);
                    var endingInventory = RetrieveEndingInventory(orderPlanning, dateToday.AddMonths(-1));

                    Entity detail = new Entity("gsc_sls_orderplanningdetail");
                    detail["gsc_orderplanningid"] = new EntityReference(orderPlanning.LogicalName, orderPlanning.Id); ;
                    detail["gsc_year"] = dateToday.Year.ToString();
                    detail["gsc_month"] = dateToday.Month.ToString("d2");
                    detail["gsc_retailaveragesales"] = retailAverage;
                    detail["gsc_beginninginventory"] = endingInventory;
                    detail["gsc_generatedpostatus"] = new OptionSetValue(100000000);
                    _organizationService.Create(detail);

                    _tracingService.Trace("Ended CreateDetails Method");

                    return detail;
                }
            }

            return null;
        }

        /* Retail Period Coverage: Field in Order Planning
         * Total Net Sales: See ComputeTotalNetSales Methos
         * Retail Average Sales = Total Net Sales / Retail Period Coverage
         */
        public Double ComputeRetailAverageSales(Entity orderPlanning)
        {
            _tracingService.Trace("Compute Retail Average Sales");

            var average = 0.0;

            var coverage = orderPlanning.Contains("gsc_retailperiodcoverage")
                ? orderPlanning.FormattedValues["gsc_retailperiodcoverage"]
                : null;

            if (coverage != null)
            {
                var totalNetSales = ComputeTotalNetSales(orderPlanning);
                average = Convert.ToDouble(totalNetSales) / Convert.ToDouble(coverage);

                _tracingService.Trace("Retail Average Sales Computed");
                _tracingService.Trace("Retail Average Sales" + average);
            }

            return average;
        }

        /* Posted VSI: Count of Vehicle Sales Invoices of the Model Description with status is 'released' (posted)
         * Posted VSR: Count of Released Vehicle Sales Invoices that were returned 
         * Total Net Sales = Posted VSI - Posted VSR
         */
        public Int32 ComputeTotalNetSales(Entity orderPlanning)
        {
            _tracingService.Trace("Compute Total Net Sales");

            var totalNetSales = 0;
            var dealerId = orderPlanning.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                ? orderPlanning.GetAttributeValue<EntityReference>("gsc_dealerid").Id
                : Guid.Empty;
            var branchId = orderPlanning.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? orderPlanning.GetAttributeValue<EntityReference>("gsc_branchid").Id
                : Guid.Empty;
            var productId = orderPlanning.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? orderPlanning.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;

            var invoiceConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                    new ConditionExpression("gsc_salesinvoicestatus", ConditionOperator.Equal, 100000004),
                    new ConditionExpression("gsc_dealerid", ConditionOperator.Equal, dealerId),
                    new ConditionExpression("gsc_branchsiteid", ConditionOperator.Equal, branchId),
                };

            EntityCollection invoiceRecords = CommonHandler.RetrieveRecordsByConditions("invoice", invoiceConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "invoiceid" });

            var postedVSR = 0;
            var postedVSI = invoiceRecords != null ? invoiceRecords.Entities.Count : 0;
            _tracingService.Trace("Posted VSI:" + postedVSI);

            if (invoiceRecords != null && invoiceRecords.Entities.Count > 0)
            {
                foreach (var invoice in invoiceRecords.Entities)
                {
                    _tracingService.Trace("Checking Vehicle Sales Invoice if Returned.");

                    var returnConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_dealerid", ConditionOperator.Equal, dealerId),
                        new ConditionExpression("gsc_branchsiteid", ConditionOperator.Equal, branchId),
                        new ConditionExpression("gsc_invoiceid", ConditionOperator.Equal, invoice.Id),
                        new ConditionExpression("gsc_vehiclesalesreturnstatus", ConditionOperator.Equal, 100000001)
                    };

                    EntityCollection returnRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_vehiclesalesreturn", returnConditionList, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_sls_vehiclesalesreturnid" });

                    postedVSR += returnRecords != null ? returnRecords.Entities.Count : 0;
                }
            }

            _tracingService.Trace("Posted VSR:" + postedVSR);

            totalNetSales = postedVSI - postedVSR;

            _tracingService.Trace("Total Net Sales" + totalNetSales);

            return totalNetSales;
        }

        //Ending Inventory = Beginning Inventory of Last Month
        public Double RetrieveEndingInventory(Entity orderPlanning, DateTime dateToday)
        {
            _tracingService.Trace("Retrieve Ending Inventory");

            var endingInventory = 0.0;

            var siteid = orderPlanning.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? orderPlanning.GetAttributeValue<EntityReference>("gsc_siteid").Id
                : Guid.Empty;
            var productId = orderPlanning.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? orderPlanning.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;

            var detailConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_orderplanningid", ConditionOperator.Equal, orderPlanning.Id),
                    new ConditionExpression("gsc_year", ConditionOperator.Equal, dateToday.Year.ToString()),
                    new ConditionExpression("gsc_month", ConditionOperator.Equal, dateToday.Month.ToString("d2"))
                };

            EntityCollection detailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_orderplanningdetail", detailConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_endinginventory" });

            if (detailRecords != null && detailRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Order Planning Detail Record");

                endingInventory = detailRecords.Entities[0].GetAttributeValue<Double>("gsc_endinginventory");
                _tracingService.Trace("Ending Inventory" + endingInventory);
            }
            else
            {
                throw new InvalidPluginExecutionException(String.Concat("ERROR: Cannot create detail. This record doesn't have previous month order planning detail."));
            }


            return endingInventory;
        }

        //Created By: Leslie Baliguat, Created On: 6/03/2016
        /*Purpose: Generate Primary Name of Order Planning Record
         * Registration Details: 
         * Event/Message: 
         *      Pre/Create: 
         * Primary Entity: Order Planning
         */
        public Entity GeneratePrimaryName(Entity orderPlanning)
        {
            Entity orderPlanningToUpdate = _organizationService.Retrieve(orderPlanning.LogicalName, orderPlanning.Id,
                new ColumnSet("gsc_orderplanningpn", "gsc_siteid", "gsc_productid"));

            var siteName = orderPlanningToUpdate.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? orderPlanningToUpdate.GetAttributeValue<EntityReference>("gsc_siteid").Name
                : String.Empty;
            var productName = orderPlanningToUpdate.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? orderPlanningToUpdate.GetAttributeValue<EntityReference>("gsc_productid").Name
                : String.Empty;


            orderPlanningToUpdate["gsc_orderplanningpn"] = productName + "-" + siteName;

            _organizationService.Update(orderPlanningToUpdate);

            return orderPlanning;
        }

        
    }
}
