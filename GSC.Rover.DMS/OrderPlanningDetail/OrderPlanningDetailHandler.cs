using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace GSC.Rover.DMS.BusinessLogic.OrderPlanningDetail
{
    public class OrderPlanningDetailHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public OrderPlanningDetailHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On:6/07/2016
        /*Purpose: Compute Stock Month = Ending Inventory / RetailAverage
         *         Update Stock Month field
         * Registration Details: 
         * Event/Message: 
         *      Post/Update: gsc_endinginventory
         * Primary Entity: Order Planning Detail
         */
        public void ComputeStockMonth(Entity detailEntity)
        {
            _tracingService.Trace("Started ComputeStockMonth Method");

            var endingInventory = detailEntity.Contains("gsc_endinginventory")
                ? detailEntity.GetAttributeValue<Double>("gsc_endinginventory")
                : 0;
            var retailAverage = detailEntity.Contains("gsc_retailaveragesales")
                ? detailEntity.GetAttributeValue<Double>("gsc_retailaveragesales")
                : 0;

            var stockMonth = 0.0;

            if(retailAverage != 0)
                stockMonth = endingInventory / retailAverage;

            _tracingService.Trace("Stock Month: " + stockMonth);

            Entity detailToUpdate = _organizationService.Retrieve(detailEntity.LogicalName, detailEntity.Id, new ColumnSet("gsc_stockmonth"));
            detailToUpdate["gsc_stockmonth"] = stockMonth;

            _organizationService.Update(detailToUpdate);
            _tracingService.Trace("Stock Month Updated");

            _tracingService.Trace("Ended ComputeStockMonth Method");
        }

        //Created By: Leslie Baliguat, Created On: 6/07/2016
        /*Purpose: Create Order Planning Detail for the next month
         *          On Create, Retrieve Ending Inventory from previous month and set this as a value of Beginning Inventory
         *          This runs when ending inventory was updated (through console app)
         * Registration Details: 
         * Event/Message: 
         *      Post/Update: gsc_endinginventory
         * Primary Entity: Order Planning Detail
         */
        public Entity CreatDetailsForNextMonth(Entity detailEntity)
        {
            _tracingService.Trace("Started CreatDetailsForNextMonth Method");

            var oderPlanningId = detailEntity.GetAttributeValue<EntityReference>("gsc_orderplanningid") != null
                ? detailEntity.GetAttributeValue<EntityReference>("gsc_orderplanningid").Id
                : Guid.Empty;

            EntityCollection selectedOrderPlanning = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_orderplanning", "gsc_sls_orderplanningid", oderPlanningId, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_dealerid", "gsc_branchid", "gsc_productid", "gsc_siteid" });

            if (selectedOrderPlanning != null && selectedOrderPlanning.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Order Planning Details");

                Entity orderPlanning = selectedOrderPlanning.Entities[0];

                    _tracingService.Trace("Strated CreateDetails Method");

                    var dateToday = DateTime.Now;
                    var year = dateToday.Year;
                    var month = dateToday.Month;
                    var lastDayLastMonth = DateTime.DaysInMonth(year, month);

                    if (dateToday.Day == 1 || dateToday.Day != lastDayLastMonth)
                    { //Create Order Planing Detail for this Month
                        dateToday = DateTime.Now;
                    }
                    else
                    { //Create Order Planing Detail for Next Month, plugin run on time
                        dateToday = DateTime.Now.AddMonths(1);
                    }

                    if (!CheckIfCreated(orderPlanning, dateToday))
                    {
                        var endingInventory = RetrieveEndingInventory(orderPlanning, dateToday.AddMonths(-1));

                        Entity detail = new Entity("gsc_sls_orderplanningdetail");
                        detail["gsc_orderplanningid"] = new EntityReference(orderPlanning.LogicalName, orderPlanning.Id); ;
                        detail["gsc_year"] = dateToday.Year.ToString();
                        detail["gsc_month"] = dateToday.Month.ToString("d2");
                        detail["gsc_beginninginventory"] = endingInventory;
                        _organizationService.Create(detail);

                        _tracingService.Trace("Ended CreateDetails Method");

                        _tracingService.Trace("Ended CreatDetailsForNextMonth Method");
                        return detail;
                    }
            }

            _tracingService.Trace("Ended CreatDetailsForNextMonth Method");
            return null;
        }

        //Check if Order Planning Detail is already created
        public Boolean CheckIfCreated(Entity orderPlanning, DateTime dateToday)
        {
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
                return true;
            }
            return false;
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

    }
}
