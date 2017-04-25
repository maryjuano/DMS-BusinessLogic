using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.CommittedFirmOrder
{
    public class CommittedFirmOrderHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public CommittedFirmOrderHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 6/08/2016
        /*Purpose: Create Committed Firm Order Details derived from Order Planning Records 
         *              that statisfies the filters from Committed Firm Order 
         * Registration Details: 
         * Event/Message: 
         *      Post/Create: 
         *      Post/Update:
         * Primary Entity: Committed Firm Order
         */
        public Entity SuggestCFOQuantity(Entity cfoEntity)
        {
            _tracingService.Trace("Started SuggestCFOQuantity Method.");

            _tracingService.Trace("Retrieve Filters.");

            var baseModelId = cfoEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                ? cfoEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id
                : Guid.Empty;
            var prodId = cfoEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? cfoEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;
            var colorId = cfoEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                ? cfoEntity.GetAttributeValue<EntityReference>("gsc_colorid").Id
                : Guid.Empty;

            _tracingService.Trace("Base Model: " + baseModelId);
            _tracingService.Trace("Product ID: " + prodId);
            _tracingService.Trace("Color Id: " + colorId);
            //Setup Condition List from Filters
            List<ConditionExpression> planningConditionList = new List<ConditionExpression>();
            planningConditionList.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));

            if (baseModelId != Guid.Empty)
                planningConditionList.Add(new ConditionExpression("gsc_vehiclebasemodelid", ConditionOperator.Equal, baseModelId));
            if (prodId != Guid.Empty)
                planningConditionList.Add(new ConditionExpression("gsc_productid", ConditionOperator.Equal, prodId));
            if (colorId != Guid.Empty)
                planningConditionList.Add(new ConditionExpression("gsc_colorid", ConditionOperator.Equal, colorId));

            EntityCollection planningRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_orderplanning", planningConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_productid", "gsc_vehiclebasemodelid", "gsc_dealerid", "gsc_branchid", "gsc_siteid", "gsc_ordercycle", "gsc_colorid" });

            _tracingService.Trace("Order Planning Records retrieved: " + planningRecords.Entities.Count);
            if (planningRecords != null && planningRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Order Planning Base on Filters.");

                //Entity to be created
                Entity cfoDetail = new Entity("gsc_sls_committedfirmorderdetail");

                foreach (var planning in planningRecords.Entities)
                {
                    _tracingService.Trace("Create Suggested CFO.");

                    cfoDetail["gsc_committedfirmorderid"] = new EntityReference(cfoEntity.LogicalName, cfoEntity.Id);

                    Entity planningDetailEntity = RetrieveOrderPlanningDetailDetails(planning);
                    cfoDetail["gsc_suggestedcfo"] = ComputeSuggestedCFO(planningDetailEntity);


                    cfoDetail["gsc_vehiclebasemodelid"] = planning.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                        ? planning.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                        : null;
                    cfoDetail["gsc_productid"] = planning.GetAttributeValue<EntityReference>("gsc_productid") != null
                        ? planning.GetAttributeValue<EntityReference>("gsc_productid")
                        : null;
                    cfoDetail["gsc_colorid"] = planning.GetAttributeValue<EntityReference>("gsc_colorid") != null
                        ? planning.GetAttributeValue<EntityReference>("gsc_colorid")
                        : null;
                    cfoDetail["gsc_cfoquantity"] = 0.00;


                    cfoDetail["gsc_orderplanningid"] = new EntityReference(planning.LogicalName, planning.Id);
                    cfoDetail["gsc_recordownerid"] = cfoEntity.Contains("gsc_recordownerid")
                        ? cfoEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                        : null;
                    cfoDetail["gsc_cfodealerid"] = cfoEntity.Contains("gsc_dealerid")
                        ? cfoEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                        : null;
                    cfoDetail["gsc_cfobranchid"] = cfoEntity.Contains("gsc_branchid")
                        ? cfoEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                        : null;

                    _organizationService.Create(cfoDetail);

                    _tracingService.Trace("Suggested CFO Created.");
                }
                _tracingService.Trace("Ended SuggestCFOQuantity Method.");

                return cfoDetail;
            }
            _tracingService.Trace("Ended SuggestCFOQuantity Method.");

            return null;
        }

        private double ComputeSuggestedCFO(Entity orderPlanningDetails)
        {
            _tracingService.Trace("Started ComputeSuggestedCFO Method...");

            double suggestedCFO = 0.0;

            var retailAveSales = orderPlanningDetails.Contains("gsc_retailaveragesales")
                ? orderPlanningDetails.GetAttributeValue<Double>("gsc_retailaveragesales") : 0;
            var beginningInventory = orderPlanningDetails.Contains("gsc_beginninginventory")
                ? orderPlanningDetails.GetAttributeValue<Double>("gsc_beginninginventory") : 0;
            var stockMonth = orderPlanningDetails.Contains("gsc_stockmonth")
                ? orderPlanningDetails.GetAttributeValue<Double>("gsc_stockmonth") : 0;

            suggestedCFO = (retailAveSales - beginningInventory) + (retailAveSales * stockMonth);
            _tracingService.Trace("Retail Ave Sales: " + retailAveSales);
            _tracingService.Trace("Beginning Inventory: " + beginningInventory);
            _tracingService.Trace("Stock Month: " + stockMonth);
            _tracingService.Trace("Suggested CFO: " + suggestedCFO);
            if (suggestedCFO < 0)
                suggestedCFO = 0.0;

            _tracingService.Trace("Ending ComputeSuggestedCFO Method...");
            return suggestedCFO;
        }


        //Retrieve Order Planning Details: Suggested CFO and Status
        private Entity RetrieveOrderPlanningDetailDetails(Entity planning)
        {
            _tracingService.Trace("RetrieveOrderPlanningDetailDetails Started.");

            String orderCycle = planning.FormattedValues["gsc_ordercycle"].ToString();

            DateTime cycleEnd = DateTime.Now.AddMonths(Convert.ToInt16(orderCycle));
            Entity orderPlanningDetails = new Entity("gsc_sls_orderplanningdetail");

            var detailConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_orderplanningid", ConditionOperator.Equal, planning.Id),
                    new ConditionExpression("gsc_month", ConditionOperator.Equal, cycleEnd.ToString("MMMM")),
                    new ConditionExpression("gsc_year", ConditionOperator.Equal, cycleEnd.Year.ToString())
                };
            _tracingService.Trace(cycleEnd.ToString("MMMM"));
            _tracingService.Trace(cycleEnd.Year.ToString());
            EntityCollection detailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_orderplanningdetail", detailConditionList, _organizationService, "createdon", OrderType.Descending,
                new[] { "gsc_retailaveragesales", "gsc_beginninginventory", "gsc_stockmonth", "gsc_generatedpostatus" });

            _tracingService.Trace("Order Planning detail records retrieved: " + detailRecords.Entities.Count);
            if (detailRecords.Entities.Count > 0)
            {
                orderPlanningDetails = detailRecords.Entities[0];
            }

            _tracingService.Trace("RetrieveOrderPlanningDetailDetails Ended.");
            return orderPlanningDetails;
        }

        //Created By: Leslie Baliguat, Created On:6/09/2016
        /*Purpose: Delete Suggested Committed Firm Order Details
         * Registration Details: 
         * Event/Message: 
         *      Post/Update: All fields (filters)
         * Primary Entity: Committed Firm Order
         */
        public void DeleteSuggestedCFODetails(Entity cfoEntity)
        {
            _tracingService.Trace("Started DeleteSuggestedCFODetails method..");

            var detailConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_committedfirmorderid", ConditionOperator.Equal, cfoEntity.Id)
                    };

            EntityCollection detailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_committedfirmorderdetail", detailConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_committedfirmorderdetailpn" });

            if (detailRecords != null && detailRecords.Entities.Count > 0)
            {
                foreach (var detail in detailRecords.Entities)
                {
                    _tracingService.Trace("Delete");
                    _organizationService.Delete(detail.LogicalName, detail.Id);
                }
            }

            _tracingService.Trace("Ended DeleteSuggestedCFODetails method..");

        }

        //Created by: Leslie Baliguat, Created On: 06/20/2016
        /*Purpose: Generate CFO Quantity Record and CFO Quantity Details Record from selected Suggested CFO
         *         Status of the selected Suggested CFO Record should be updated to "Generated CFO"
         *         Status of the latest Order Planning Detail of the Order Planning Record associated to the selected CFO Quantity Record
         *              should be updated to "Generated CFO"
         * Registration Details: 
         * Event/Message: 
         *      Post/Update: gsc_generatecfoquantity
         * Primary Entity: Committed Firm Order
         */
        public Entity GenerateCFOQuantity(Entity cfoEntity)
        {
            //if generate cfo button was clicked
            if (cfoEntity.Contains("gsc_generatecfoquantity") && cfoEntity.GetAttributeValue<Boolean>("gsc_generatecfoquantity"))
            {
                _tracingService.Trace("Started GenerateCFOQuantity Method.");

                Entity cfoQuantityDetail = new Entity("gsc_sls_committedfirmorderquantitydetail"); //entity to be created

                //Retrieve Selected Suggested CFO who's status is still Active
                var detialsConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_committedfirmorderid", ConditionOperator.Equal, cfoEntity.Id),
                        new ConditionExpression("gsc_statusreason", ConditionOperator.Equal, 100000000),
                        new ConditionExpression("gsc_cfoquantitygenerated", ConditionOperator.Equal, true)
                    };

                EntityCollection cfoDetailsRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_committedfirmorderdetail", detialsConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_orderplanningid", "gsc_statusreason", "gsc_vehiclebasemodelid", "gsc_productid",
                    "gsc_modelcode", "gsc_optioncode", "gsc_vehiclecolorid", "gsc_cfoquantity", "gsc_siteid",
                    "gsc_unitcost", "gsc_orderplanningid", "gsc_recordownerid", "gsc_dealerid", "gsc_branchid", "gsc_cfoquantitygenerated"});

                if (cfoDetailsRecords != null && cfoDetailsRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Suggested CFO Details");

                    var odercycle = CheckOrderCycle(cfoDetailsRecords); //if all is good then proceed to the process

                    var cfoQuantityId = CreateCFOQuantity(cfoDetailsRecords, odercycle); //Create CFO Quantity Record (Parent Record)first

                    foreach (var cfoDetails in cfoDetailsRecords.Entities)
                    {
                        _tracingService.Trace("Create CFO Quantity Details");

                        cfoQuantityDetail["gsc_vehiclebasemodelid"] = cfoDetails.Contains("gsc_vehiclebasemodelid")
                            ? cfoDetails.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                            : null;
                        cfoQuantityDetail["gsc_productid"] = cfoDetails.Contains("gsc_productid")
                            ? cfoDetails.GetAttributeValue<EntityReference>("gsc_productid")
                            : null;
                        cfoQuantityDetail["gsc_modelcode"] = cfoDetails.Contains("gsc_modelcode")
                            ? cfoDetails.GetAttributeValue<String>("gsc_modelcode")
                            : String.Empty;
                        cfoQuantityDetail["gsc_optioncode"] = cfoDetails.Contains("gsc_optioncode")
                            ? cfoDetails.GetAttributeValue<String>("gsc_optioncode")
                            : String.Empty;
                        cfoQuantityDetail["gsc_vehiclecolorid"] = cfoDetails.Contains("gsc_vehiclecolorid")
                            ? cfoDetails.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                            : null;
                        cfoQuantityDetail["gsc_cfoquantity"] = cfoDetails.Contains("gsc_cfoquantity")
                            ? cfoDetails.GetAttributeValue<Int32>("gsc_cfoquantity")
                            : 0;
                        cfoQuantityDetail["gsc_orderquantity"] = 0;
                        cfoQuantityDetail["gsc_remainingallocatedquantity"] = 0;
                        cfoQuantityDetail["gsc_siteid"] = cfoDetails.Contains("gsc_siteid")
                            ? cfoDetails.GetAttributeValue<EntityReference>("gsc_siteid")
                            : null;

                        // replace dnp amount for unit cost
                        /*cfoQuantityDetail["gsc_unitcost"] = cfoDetails.Contains("gsc_unitcost")
                            ? cfoDetails.GetAttributeValue<Money>("gsc_unitcost")
                            : new Money(0);*/

                        cfoQuantityDetail["gsc_totalcost"] = new Money(0);
                        cfoQuantityDetail["gsc_remarks"] = "";
                        cfoQuantityDetail["gsc_orderplanningid"] = cfoDetails.Contains("gsc_orderplanningid")
                            ? cfoDetails.GetAttributeValue<EntityReference>("gsc_orderplanningid")
                            : null;
                        cfoQuantityDetail["gsc_committedfirmorderquantityid"] = new EntityReference("gsc_sls_committedfirmorderquantity", cfoQuantityId);
                        cfoQuantityDetail["gsc_recordownerid"] = cfoDetails.Contains("gsc_recordownerid")
                            ? cfoEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                            : null;
                        cfoQuantityDetail["gsc_dealerid"] = cfoDetails.Contains("gsc_dealerid")
                            ? cfoEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                            : null;
                        cfoQuantityDetail["gsc_branchid"] = cfoDetails.Contains("gsc_branchid")
                            ? cfoEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                            : null;

                        _organizationService.Create(cfoQuantityDetail);

                        _tracingService.Trace("CFO Quantity Details Created");

                        //Update CFO Detail Status to Generated CFO
                        cfoDetails["gsc_statusreason"] = new OptionSetValue(100000001);
                        cfoDetails["gsc_cfoquantitygenerated"] = false;
                        _organizationService.Update(cfoDetails);

                        _tracingService.Trace("CFO Details Status Updated");

                        //call UpdateOrderPlanningDetailStatus method
                        UpdateOrderPlanningDetailStatus(cfoDetails);
                    }
                }

                //Update CFO trigger for generate cfo quantity, gsc_generatecfoquantity to false
                Entity cfoToUpdate = _organizationService.Retrieve(cfoEntity.LogicalName, cfoEntity.Id,
                    new ColumnSet("gsc_generatecfoquantity"));
                cfoToUpdate["gsc_generatecfoquantity"] = false;
                _organizationService.Update(cfoToUpdate);

                _tracingService.Trace("CFO gsc_generatecfoquantity Updated.");

                _tracingService.Trace("Ended GenerateCFOQuantity Method.");

                return (cfoQuantityDetail);
            }
            _tracingService.Trace("Ended GenerateCFOQuantity Method.");
            return null;
        }

        //Check if Order Planning Record related to CFO Details shares same order cycle
        private String CheckOrderCycle(EntityCollection cfoDetailsRecords)
        {
            _tracingService.Trace("Check Order Cycle of Order Planning Record");

            var orderCycle = 0;
            var months = "";

            foreach (var cfoDetails in cfoDetailsRecords.Entities)
            {
                _tracingService.Trace("Retrieve CFO Details");

                var oderPlanningId = cfoDetails.Contains("gsc_orderplanningid")
                    ? cfoDetails.GetAttributeValue<EntityReference>("gsc_orderplanningid").Id
                    : Guid.Empty;

                EntityCollection planningRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_orderplanning", "gsc_sls_orderplanningid", oderPlanningId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_ordercycle" });

                if (planningRecords != null && planningRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Order Planning");

                    if (orderCycle == 0)
                    {
                        orderCycle = planningRecords.Entities[0].GetAttributeValue<OptionSetValue>("gsc_ordercycle").Value;
                        months = planningRecords.Entities[0].FormattedValues["gsc_ordercycle"];
                    }

                    else
                    {
                        //check if previous retrieved order cycle is the same with newly retrieved order cycle
                        //if not, throw an error. the process won't continue
                        if (orderCycle != planningRecords.Entities[0].GetAttributeValue<OptionSetValue>("gsc_ordercycle").Value)
                        {
                            _tracingService.Trace("Found different order cycle.");
                            throw new InvalidPluginExecutionException("Transaction cannot be completed. Please contact the administrator.");
                        }
                    }

                }
            }

            _tracingService.Trace("Order Cycle Checking Done.");
            return months;
        }
        
        //Ceate CFO Quantity Record
        private Guid CreateCFOQuantity(EntityCollection cfoDetailsRecords, String odercycle)
        {
            _tracingService.Trace("Create CFO Quantity Record");

            Entity cfoDetailsEntity = cfoDetailsRecords.Entities[0];

            var dateToday = DateTime.Now;
            var cfoDate = dateToday.AddMonths(Convert.ToInt32(odercycle));
            var cfoMonth = 0;
            var cfoYear = 0;
            var cfoMonthString = "";

            #region Set OptionSetValue of cfoMonth
            switch (cfoDate.Month)
            {
                case 1: cfoMonth = 100000000;
                    cfoMonthString = "January";
                    break;
                case 2: cfoMonth = 100000001;
                    cfoMonthString = "February";
                    break;
                case 3: cfoMonth = 100000002;
                    cfoMonthString = "March";
                    break;
                case 4: cfoMonth = 100000003;
                    cfoMonthString = "April";
                    break;
                case 5: cfoMonth = 100000004;
                    cfoMonthString = "May";
                    break;
                case 6: cfoMonth = 100000005;
                    cfoMonthString = "June";
                    break;
                case 7: cfoMonth = 100000006;
                    cfoMonthString = "July";
                    break;
                case 8: cfoMonth = 100000007;
                    cfoMonthString = "August";
                    break;
                case 9: cfoMonth = 100000008;
                    cfoMonthString = "September";
                    break;
                case 10: cfoMonth = 100000009;
                    cfoMonthString = "October";
                    break;
                case 11: cfoMonth = 100000010;
                    cfoMonthString = "November";
                    break;
                case 12: cfoMonth = 100000011;
                    cfoMonthString = "December";
                    break;
                default: cfoMonth = 0;
                    break;
            }
            #endregion

            #region Set OptionSetValue of cfoYear
            switch (cfoDate.Year)
            {
                case 2016: cfoYear = 100000000;
                    break;
                case 2017: cfoYear = 100000001;
                    break;
                case 2018: cfoYear = 100000002;
                    break;
                case 2019: cfoYear = 100000003;
                    break;
                case 2020: cfoYear = 100000004;
                    break;
                case 2021: cfoYear = 100000005;
                    break;
                case 2022: cfoYear = 100000006;
                    break;
                case 2023: cfoYear = 100000007;
                    break;
                case 2024: cfoYear = 100000008;
                    break;
                case 2025: cfoYear = 100000009;
                    break;
                case 2026: cfoYear = 100000010;
                    break;
                case 2027: cfoYear = 100000011;
                    break;
                case 2028: cfoYear = 100000012;
                    break;
                case 2029: cfoYear = 100000013;
                    break;
                case 2030: cfoYear = 100000014;
                    break;
                case 2031: cfoYear = 100000015;
                    break;
                case 2032: cfoYear = 100000016;
                    break;
                case 2033: cfoYear = 100000017;
                    break;
                case 2034: cfoYear = 100000018;
                    break;
                case 2035: cfoYear = 100000019;
                    break;
                case 2036: cfoYear = 100000020;
                    break;
                case 2037: cfoYear = 100000021;
                    break;
                case 2038: cfoYear = 100000022;
                    break;
                case 2039: cfoYear = 100000023;
                    break;
                case 2040: cfoYear = 100000024;
                    break;
                default: cfoYear = 0;
                    break;
            }
            #endregion

            Entity cfoQuantity = new Entity("gsc_sls_committedfirmorderquantity");
            cfoQuantity["gsc_cfodescription"] = String.Concat("CFO for the Month of ", cfoMonthString);
            cfoQuantity["gsc_cfomonth"] = new OptionSetValue(cfoMonth);
            cfoQuantity["gsc_year"] = new OptionSetValue(cfoYear);
            cfoQuantity["gsc_cfostatus"] = new OptionSetValue(100000000);
            cfoQuantity["gsc_cfodealerid"] = cfoDetailsEntity.Contains("gsc_dealerid")
                ? cfoDetailsEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;
            cfoQuantity["gsc_cfobranchid"] = cfoDetailsEntity.Contains("gsc_branchid")
                ? cfoDetailsEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;
            cfoQuantity["gsc_dealerid"] = cfoDetailsEntity.Contains("gsc_dealerid")
                ? cfoDetailsEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;
            cfoQuantity["gsc_branchid"] = cfoDetailsEntity.Contains("gsc_branchid")
                ? cfoDetailsEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;
            cfoQuantity["gsc_recordownerid"] = cfoDetailsEntity.Contains("gsc_recordownerid")
                ? cfoDetailsEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                : null;
            Guid cfoOrderQuantityId = _organizationService.Create(cfoQuantity);

            _tracingService.Trace("CFO Quantity Created");

            return cfoOrderQuantityId;
        }

        //Update Order Planning Detail Status to "Generated CFO"
        private void UpdateOrderPlanningDetailStatus(Entity cfoDetails)
        {
            _tracingService.Trace("UpdateOrderPlanningDetailStatus Method Started.");

            var planningId = cfoDetails.Contains("gsc_orderplanningid")
                    ? cfoDetails.GetAttributeValue<EntityReference>("gsc_orderplanningid").Id
                    : Guid.Empty;

            EntityCollection planningRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_orderplanning", "gsc_sls_orderplanningid", planningId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_orderplanningpn"});

            if (planningRecords != null && planningRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Order Planning.");

                Entity planningEntity = planningRecords.Entities[0];

                 var detailConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_orderplanningid", ConditionOperator.Equal, planningEntity.Id)
                };

                EntityCollection detailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_orderplanningdetail", detailConditionList, _organizationService, "createdon", OrderType.Descending,
                    new[] { "gsc_generatedpostatus" });

                if (detailRecords != null && detailRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Order Planning Detail.");

                    Entity detailEntity = detailRecords.Entities[0];
                    detailEntity["gsc_generatedpostatus"] = new OptionSetValue(100000001);
                    _organizationService.Update(detailEntity);

                    _tracingService.Trace("Status Updated.");
                }
            }

            _tracingService.Trace("UpdateOrderPlanningDetailStatus Method Ended.");
        }
    }
}
