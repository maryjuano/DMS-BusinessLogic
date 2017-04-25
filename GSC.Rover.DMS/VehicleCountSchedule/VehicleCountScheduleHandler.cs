using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.VehicleCountSchedule
{
    public class VehicleCountScheduleHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleCountScheduleHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie G. Baliguat, Created On: 8/23/2016
        /*Purpose: Filter Inventory with the filter criteria, then create Filter results (copy of these inventories).
         *          No Duplicates.
         * Registration Details: 
         * Event/Message:
         *      Post/Update: Site, Base Model, Model Description, Model Code,
         *                   Option Code, Color
         * Primary Entity: Vehicle Count Schedule
         */
        public Entity ApplyFilter(Entity countScheduleEntity)
        {
            _tracingService.Trace("Apply Filter Method Started");

            var siteId = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                : Guid.Empty;
            var baseModelId = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id
                : Guid.Empty;
            var productId = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;
            var modelCode = countScheduleEntity.Contains("gsc_modelcode")
                ? countScheduleEntity.GetAttributeValue<String>("gsc_modelcode")
                : String.Empty;
            var optionCode = countScheduleEntity.Contains("gsc_optioncode")
                ? countScheduleEntity.GetAttributeValue<String>("gsc_optioncode")
                : String.Empty;
            var color = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id
                : Guid.Empty;

            _tracingService.Trace("Filter Criteria Retrieved.");

            //If Conditions in Criteria fields are need to filter inventories only by those criteria which are not null/empty
            //Null/Empty Criteria feild means retrieve all. 
            QueryExpression retrievePQ = new QueryExpression("gsc_iv_productquantity");
            retrievePQ.ColumnSet.AddColumns("gsc_vehiclemodelid", "gsc_siteid", "gsc_productid", "gsc_vehiclecolorid");

            if (productId != Guid.Empty)
                retrievePQ.Criteria.AddCondition(new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId));

            if (siteId != Guid.Empty)
                retrievePQ.Criteria.AddCondition(new ConditionExpression("gsc_siteid", ConditionOperator.Equal, siteId));

            if (baseModelId != Guid.Empty)
                retrievePQ.Criteria.AddCondition(new ConditionExpression("gsc_vehiclemodelid", ConditionOperator.Equal, baseModelId));

            if (color != Guid.Empty)
                retrievePQ.Criteria.AddCondition(new ConditionExpression("gsc_vehiclecolorid", ConditionOperator.Equal, color));

            retrievePQ.LinkEntities.Add(new LinkEntity("gsc_iv_productquantity", "product", "gsc_productid", "productid", JoinOperator.Inner));
            retrievePQ.LinkEntities[0].Columns.AddColumns("gsc_modelcode", "gsc_optioncode");
            retrievePQ.LinkEntities[0].EntityAlias = "Inventory";

            if (modelCode != String.Empty)
                retrievePQ.LinkEntities[0].LinkCriteria.AddCondition("gsc_modelcode", ConditionOperator.Equal, modelCode);

            if (optionCode != String.Empty)
                retrievePQ.LinkEntities[0].LinkCriteria.AddCondition("gsc_optioncode", ConditionOperator.Equal, optionCode);

            var PQList = _organizationService.RetrieveMultiple(retrievePQ);

            if (PQList != null && PQList.Entities.Count > 0)
            {
                Entity newFilterResult = new Entity("gsc_iv_vehiclecountschedulefilter");

                foreach (var prodQuan in PQList.Entities)
                {
                    _tracingService.Trace("Retrieve Filtered Inventories.");

                    var filterConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_vehiclecountscheduleid", ConditionOperator.Equal, countScheduleEntity.Id),
                        new ConditionExpression("gsc_productquantityid", ConditionOperator.Equal, prodQuan.Id)
                    };

                    EntityCollection filterResult = CommonHandler.RetrieveRecordsByConditions("gsc_iv_vehiclecountschedulefilter", filterConditionList, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_vehiclecountschedulefilterpn" });

                    //No Duplication in Filter Result.
                    //If Inventory Record is not existing in Filter Result grid, Create new Result Record. Hence, do nothing.
                    if (filterResult == null || filterResult.Entities.Count == 0)
                    {
                        _tracingService.Trace("Inventory doesn't exist in Current Result.");

                        newFilterResult["gsc_vehiclecountscheduleid"] = new EntityReference(countScheduleEntity.LogicalName, countScheduleEntity.Id);
                        newFilterResult["gsc_productquantityid"] = new EntityReference(prodQuan.LogicalName, prodQuan.Id);
                        newFilterResult["gsc_siteid"] = prodQuan.GetAttributeValue<EntityReference>("gsc_siteid") != null
                            ? prodQuan.GetAttributeValue<EntityReference>("gsc_siteid")
                            : null;
                        newFilterResult["gsc_vehiclebasemodelid"] = prodQuan.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                            ? prodQuan.GetAttributeValue<EntityReference>("gsc_vehiclemodelid")
                            : null;
                        newFilterResult["gsc_productid"] = prodQuan.GetAttributeValue<EntityReference>("gsc_productid") != null
                            ? prodQuan.GetAttributeValue<EntityReference>("gsc_productid")
                            : null;
                        newFilterResult["gsc_modelcode"] = (prodQuan.GetAttributeValue<AliasedValue>("Inventory.gsc_modelcode")) != null
                            ? prodQuan.GetAttributeValue<AliasedValue>("Inventory.gsc_modelcode").Value
                            : String.Empty;
                        newFilterResult["gsc_optioncode"] = (prodQuan.GetAttributeValue<AliasedValue>("Inventory.gsc_optioncode")) != null
                            ? prodQuan.GetAttributeValue<AliasedValue>("Inventory.gsc_optioncode").Value
                            : String.Empty;
                        newFilterResult["gsc_vehiclecolorid"] = prodQuan.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                            ? prodQuan.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                            : null;
                        newFilterResult["gsc_recordownerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                            : null;
                        newFilterResult["gsc_dealerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                            : null;
                        newFilterResult["gsc_branchid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                            : null;

                        _organizationService.Create(newFilterResult);

                        _tracingService.Trace("New Filter Result Record Created.");
                    }
                }
                _tracingService.Trace("Apply Filter Method Ended");
                return newFilterResult;
            }

            _tracingService.Trace("Apply Filter Method Ended");
            return null;
        }

        //Created By: Leslie G. Baliguat, Created On: 8/24/2016
        /*Purpose: Replicate Selected Filter Results grid to Vehicle for Counting Grid
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_copyfilterresult
         * Primary Entity: Vehicle Count Schedule
         */
        public List<Entity> ReplicateFilterResult(Entity countScheduleEntity)
        {
            List<Entity> vehicleForCounting = new List<Entity>();

            if (countScheduleEntity.Contains("gsc_copyids") && countScheduleEntity.GetAttributeValue<String>("gsc_copyids") != String.Empty)
            {
                _tracingService.Trace("ReplicateFilterResult Method Started");

                //CopyIds is a textarea in CRM Form, this contains all the ids that are selected.
                var CopyIds = countScheduleEntity.GetAttributeValue<String>("gsc_copyids");
                string[] filterResultIds = CopyIds.Split(',');

                foreach (var ids in filterResultIds)
                {
                    EntityCollection filterResults = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountschedulefilter", "gsc_iv_vehiclecountschedulefilterid", new Guid(ids), _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_vehiclecountscheduleid", "gsc_productquantityid", "gsc_vehiclebasemodelid", "gsc_productid", "gsc_siteid", "gsc_vehiclecolorid", "gsc_optioncode", "gsc_modelcode" });

                    if (filterResults != null && filterResults.Entities.Count > 0)
                    {
                        _tracingService.Trace("Retrieve Filter Results.");

                        var filteredEntity = filterResults.Entities[0];

                        var inventoryId = filteredEntity.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                            ? filteredEntity.GetAttributeValue<EntityReference>("gsc_productquantityid")
                            : null;

                        var detailConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_vehiclecountscheduleid", ConditionOperator.Equal, countScheduleEntity.Id),
                                new ConditionExpression("gsc_productquantityid", ConditionOperator.Equal, inventoryId.Id)
                            };

                        EntityCollection existingCount = CommonHandler.RetrieveRecordsByConditions("gsc_iv_vehiclecountscheduledetail", detailConditionList, _organizationService,
                            null, OrderType.Ascending, new[] { "gsc_vehiclecountscheduledetailpn" });

                        //No Duplication in Vehicle For Counting
                        //If Inventory Record doesn't exist in Vehicle for Countinf grid, Create new Record. Hence, do nothing.
                        if (existingCount == null || existingCount.Entities.Count == 0)
                        {
                            _tracingService.Trace("Set up Vehicle for Counting Record.");

                            Entity vehicleDetail = new Entity("gsc_iv_vehiclecountscheduledetail");

                            vehicleDetail["gsc_vehiclecountscheduleid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclecountscheduleid") != null
                                ? filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclecountscheduleid")
                                : null;
                            vehicleDetail["gsc_productquantityid"] = inventoryId;
                            vehicleDetail["gsc_vehiclebasemodelid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                                ? filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                                : null;
                            vehicleDetail["gsc_productid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                                ? filteredEntity.GetAttributeValue<EntityReference>("gsc_productid")
                                : null;
                            vehicleDetail["gsc_siteid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                                ? filteredEntity.GetAttributeValue<EntityReference>("gsc_siteid")
                                : null;
                            vehicleDetail["gsc_vehiclecolorid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                                ? filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                                : null;
                            vehicleDetail["gsc_optioncode"] = filteredEntity.Contains("gsc_optioncode")
                                ? filteredEntity.GetAttributeValue<String>("gsc_optioncode")
                                : String.Empty;
                            vehicleDetail["gsc_modelcode"] = filteredEntity.Contains("gsc_modelcode")
                                ? filteredEntity.GetAttributeValue<String>("gsc_modelcode")
                                : String.Empty;
                            vehicleDetail["gsc_recordownerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                                : null;
                            vehicleDetail["gsc_dealerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                                : null;
                            vehicleDetail["gsc_branchid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                                : null;

                            _organizationService.Create(vehicleDetail);
                            vehicleForCounting.Add(vehicleDetail);

                            _tracingService.Trace("Vehicle for Counting Created.");
                        }
                        else
                        {
                            _tracingService.Trace("Inventory already exists in Vehicle for Counting.");
                        }

                    }
                    _tracingService.Trace("ReplicateFilterResult Method Ended");
                }
            }
            return vehicleForCounting;
        }

        //Created By: Leslie G. Baliguat, Created On: 8/24/2016
        /*Purpose: Replicate All Filter Results grid to Vehicle for Counting Grid
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_copyallfilterresult
         * Primary Entity: Vehicle Count Schedule
         */
        public List<Entity> ReplicateAllFilterResult(Entity countScheduleEntity)
        {
            List<Entity> vehicleForCounting = new List<Entity>();

            _tracingService.Trace("ReplicateFilterResult Method Started");

            EntityCollection filterResults = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountschedulefilter", "gsc_vehiclecountscheduleid", countScheduleEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_vehiclecountscheduleid", "gsc_productquantityid", "gsc_vehiclebasemodelid", "gsc_productid", "gsc_siteid", "gsc_vehiclecolorid", "gsc_optioncode", "gsc_modelcode" });

            if (filterResults != null && filterResults.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Filter Results.");

                foreach (var filteredEntity in filterResults.Entities)
                {
                    var inventoryId = filteredEntity.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                        ? filteredEntity.GetAttributeValue<EntityReference>("gsc_productquantityid")
                        : null;

                    var detailConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_vehiclecountscheduleid", ConditionOperator.Equal, countScheduleEntity.Id),
                                new ConditionExpression("gsc_productquantityid", ConditionOperator.Equal, inventoryId.Id)
                            };

                    EntityCollection existingCount = CommonHandler.RetrieveRecordsByConditions("gsc_iv_vehiclecountscheduledetail", detailConditionList, _organizationService,
                        null, OrderType.Ascending, new[] { "gsc_vehiclecountscheduledetailpn" });

                    //No Duplication in Vehicle For Counting
                    //If Inventory Record doesn't exist in Vehicle for Countinf grid, Create new Record. Hence, do nothing.
                    if (existingCount == null || existingCount.Entities.Count == 0)
                    {
                        _tracingService.Trace("Set up Vehicle for Counting Record.");

                        Entity vehicleDetail = new Entity("gsc_iv_vehiclecountscheduledetail");

                        vehicleDetail["gsc_vehiclecountscheduleid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclecountscheduleid") != null
                            ? filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclecountscheduleid")
                            : null;
                        vehicleDetail["gsc_productquantityid"] = inventoryId;
                        vehicleDetail["gsc_vehiclebasemodelid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                            ? filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                            : null;
                        vehicleDetail["gsc_productid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                            ? filteredEntity.GetAttributeValue<EntityReference>("gsc_productid")
                            : null;
                        vehicleDetail["gsc_siteid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                            ? filteredEntity.GetAttributeValue<EntityReference>("gsc_siteid")
                            : null;
                        vehicleDetail["gsc_vehiclecolorid"] = filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                            ? filteredEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                            : null;
                        vehicleDetail["gsc_optioncode"] = filteredEntity.Contains("gsc_optioncode")
                            ? filteredEntity.GetAttributeValue<String>("gsc_optioncode")
                            : String.Empty;
                        vehicleDetail["gsc_modelcode"] = filteredEntity.Contains("gsc_modelcode")
                            ? filteredEntity.GetAttributeValue<String>("gsc_modelcode")
                            : String.Empty;
                        vehicleDetail["gsc_recordownerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                            : null;
                        vehicleDetail["gsc_dealerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                            : null;
                        vehicleDetail["gsc_branchid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                            : null;

                        _organizationService.Create(vehicleDetail);
                        vehicleForCounting.Add(vehicleDetail);

                        _tracingService.Trace("Vehicle for Counting Created.");
                    }
                    else
                    {
                        _tracingService.Trace("Inventory already exists in Vehicle for Counting.");
                    }

                }
                _tracingService.Trace("ReplicateFilterResult Method Ended");
            }

            return vehicleForCounting;
        }

        //Created By: Leslie G. Baliguat, Created On: 8/25/2016
        /*Purpose: Delete all records in Filter Result.
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_deletefilterresult
         * Primary Entity: Vehicle Count Schedule
         */
        public void ClearFilterResults(Entity countScheduleEntity)
        {
            if (countScheduleEntity.Contains("gsc_copyids") && countScheduleEntity.GetAttributeValue<String>("gsc_copyids") != String.Empty)
            {
                _tracingService.Trace("ClearFilterResults Method Started");

                //CopyIds is a textarea in CRM Form, this contains all the ids that are selected.
                var CopyIds = countScheduleEntity.GetAttributeValue<String>("gsc_copyids");
                string[] filterResultIds = CopyIds.Split(',');

                foreach (var ids in filterResultIds)
                {
                    _tracingService.Trace("Deleting ...");

                    EntityCollection filterResult = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountschedulefilter", "gsc_iv_vehiclecountschedulefilterid", new Guid(ids), _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_vehiclecountschedulefilterpn" });

                    if (filterResult != null && filterResult.Entities.Count > 0)
                    {
                        var result = filterResult.Entities[0];

                        _organizationService.Delete(result.LogicalName, result.Id);

                        _tracingService.Trace("Deleted.");
                    }
                }

                _tracingService.Trace("ClearFilterResults Method Ended");
            }
        }

        //Created By: Leslie G. Baliguat, Created On: 8/25/2016
        /*Purpose: Delete all records in Vehicle for Counting.
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_deletedetail
         * Primary Entity: Vehicle Count Schedule
         */
        public void ClearVehicleForCounting(Entity countScheduleEntity)
        {
            if (countScheduleEntity.Contains("gsc_copyids") && countScheduleEntity.GetAttributeValue<String>("gsc_copyids") != String.Empty)
            {
                _tracingService.Trace("ClearVehicleForCounting Method Started");

                //CopyIds is a textarea in CRM Form, this contains all the ids that are selected.
                var CopyIds = countScheduleEntity.GetAttributeValue<String>("gsc_copyids");
                string[] filterResultIds = CopyIds.Split(',');

                foreach (var ids in filterResultIds)
                {
                    _tracingService.Trace("Deleting ...");

                    EntityCollection filterResult = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountscheduledetail", "gsc_iv_vehiclecountscheduledetailid", new Guid(ids), _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_vehiclecountscheduledetailpn" });

                    if (filterResult != null && filterResult.Entities.Count > 0)
                    {
                        var result = filterResult.Entities[0];
                        _organizationService.Delete(result.LogicalName, result.Id);

                        _tracingService.Trace("Deleted.");
                    }
                }

                _tracingService.Trace("ClearVehicleForCounting Method Ended");
            }

        }

        //Created By: Leslie G. Baliguat, Created On: 8/31/2016
        /*Purpose: Check if there is already Srated Vehicle Count Schedule Detail that has exact the same site-vehicle combination 
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_status
         * Primary Entity: Vehicle Count Schedule
         */
        public void CheckDuplicateStartedSchedule(Entity countScheduleEntity)
        {
            _tracingService.Trace("CheckDuplicateStartedSchedule Method Started.");

            EntityCollection countSchedDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountscheduledetail", "gsc_vehiclecountscheduleid", countScheduleEntity.Id, _organizationService, null, OrderType.Ascending,
                   new[] { "gsc_productquantityid" });

            if (countSchedDetailRecords != null && countSchedDetailRecords.Entities.Count > 0)
            {
                foreach (var countSchedDetail in countSchedDetailRecords.Entities)
                {
                    _tracingService.Trace("Retrieve Vehicle Count Schedule Detail.");

                    var inventory = countSchedDetail.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                        ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_productquantityid")
                        : null;

                    QueryExpression query = new QueryExpression("gsc_iv_vehiclecountschedule");
                    query.ColumnSet = new ColumnSet(new string[] { "gsc_vehiclecountschedulepn" });
                    query.Distinct = true;
                    query.Criteria.AddCondition("gsc_status", ConditionOperator.Equal, "100000001");
                    query.Criteria.AddCondition("gsc_iv_vehiclecountscheduleid", ConditionOperator.NotEqual, countScheduleEntity.Id);
                    query.AddLink("gsc_iv_vehiclecountscheduledetail", "gsc_iv_vehiclecountscheduleid", "gsc_vehiclecountscheduleid").
                            LinkCriteria.AddCondition("gsc_productquantityid", ConditionOperator.Equal, inventory.Id);

                    var inventoryRecords = _organizationService.RetrieveMultiple(query);

                    if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                    {
                        throw new InvalidPluginExecutionException("ERROR: Atleast one of the Vehicles of this Schedule has already been started from another Schedule Record.");
                    }
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("ERROR: To start this schedule, please add at least one record in Vehicle for Counting.");
            }
            _tracingService.Trace("CheckDuplicateStartedSchedule Method Ended.");

            //Create Replicate of count Schedule
            var countEntryId = CreateVehicleCountEntry(countScheduleEntity);
            CreateVehicleCountEntryDetail(countScheduleEntity, countEntryId);
            CreateVehicleCountBreakdown(countScheduleEntity, countEntryId);

            //Update Status to Started
            UpdateStatus(countScheduleEntity);
        }

        //Created By: Leslie G. Baliguat, Created On: 8/26/2016
        /*Purpose: Create Inventory Count Entry when Vehicle Count Schedule Started
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_status
         * Primary Entity: Vehicle Count Schedule
         */
        public Guid CreateVehicleCountEntry(Entity countScheduleEntity)
        {
            _tracingService.Trace("Create Vehicle Count Entry.");

            Entity countEntry = new Entity("gsc_iv_vehiclecountentry");
            countEntry["gsc_vehiclecountscheduleid"] = new EntityReference(countScheduleEntity.LogicalName, countScheduleEntity.Id);
            countEntry["gsc_vehiclecountentrypn"] = countScheduleEntity.Contains("gsc_vehiclecountschedulepn")
                ? countScheduleEntity.GetAttributeValue<String>("gsc_vehiclecountschedulepn")
                : String.Empty;
            countEntry["gsc_description"] = countScheduleEntity.Contains("gsc_description")
                ? countScheduleEntity.GetAttributeValue<String>("gsc_description")
                : String.Empty;
            countEntry["gsc_status"] = new OptionSetValue(100000000);
            countEntry["gsc_documentdate"] = countScheduleEntity.GetAttributeValue<DateTime>("createdon");
            countEntry["gsc_recordownerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                : null;
            countEntry["gsc_dealerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;
            countEntry["gsc_branchid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;

            _tracingService.Trace("Vehicle Count Entry Created.");

            return _organizationService.Create(countEntry);
        }

        //Created By: Leslie G. Baliguat, Created On: 8/26/2016
        /*Purpose: Create Inventory Count Entry Detail when Vehicle Count Schedule Started
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_status
         * Primary Entity: Vehicle Count Schedule
         */
        public List<Entity> CreateVehicleCountEntryDetail(Entity countScheduleEntity, Guid countEntryEntityId)
        {
            List<Entity> returnEntities = new List<Entity>();

            if (countEntryEntityId != Guid.Empty && countEntryEntityId != null)
            {
                _tracingService.Trace("CreateVehicleCountEntryDetail Method Started.");

                EntityCollection countSchedDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountscheduledetail", "gsc_vehiclecountscheduleid", countScheduleEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_vehiclebasemodelid", "gsc_productid", "gsc_modelcode", "gsc_optioncode", "gsc_vehiclecolorid", "gsc_siteid", "gsc_productquantityid", "gsc_productid" });

                if (countSchedDetailRecords != null && countSchedDetailRecords.Entities.Count > 0)
                {
                    Entity countEntryDetail = new Entity("gsc_iv_vehiclecountentrydetails");

                    foreach (var countSchedDetail in countSchedDetailRecords.Entities)
                    {
                        _tracingService.Trace("Retrieve Vehicle Count Schedule Detail.");

                        var inventory = countSchedDetail.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_productquantityid")
                            : null;

                        countEntryDetail["gsc_vehiclecountentryid"] = new EntityReference("gsc_iv_vehiclecountentry", countEntryEntityId);
                        countEntryDetail["gsc_vehiclebasemodelid"] = countSchedDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                            : null;
                        countEntryDetail["gsc_productid"] = countSchedDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_productid")
                            : null;
                        countEntryDetail["gsc_modelcode"] = countSchedDetail.Contains("gsc_modelcode")
                            ? countSchedDetail.GetAttributeValue<String>("gsc_modelcode")
                            : String.Empty;
                        countEntryDetail["gsc_optioncode"] = countSchedDetail.Contains("gsc_optioncode")
                            ? countSchedDetail.GetAttributeValue<String>("gsc_optioncode")
                            : String.Empty;
                        countEntryDetail["gsc_vehiclecolorid"] = countSchedDetail.Contains("gsc_vehiclecolorid")
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                            : null;
                        countEntryDetail["gsc_siteid"] = countSchedDetail.GetAttributeValue<EntityReference>("gsc_siteid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_siteid")
                            : null;
                        countEntryDetail["gsc_description"] = countScheduleEntity.Contains("gsc_description")
                            ? countScheduleEntity.GetAttributeValue<String>("gsc_description")
                            : String.Empty;
                        countEntryDetail["gsc_recordownerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                            : null;
                        countEntryDetail["gsc_dealerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                            : null;
                        countEntryDetail["gsc_branchid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                            : null;

                        EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", inventory.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_available", "gsc_allocated", "gsc_onhand" });

                        if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                        {
                            _tracingService.Trace("Retrieve Inventory Info.");

                            var inventoryEntity = inventoryRecords.Entities[0];

                            countEntryDetail["gsc_productquantityid"] = new EntityReference(inventoryEntity.LogicalName, inventoryEntity.Id);
                            countEntryDetail["gsc_availableqty"] = inventoryEntity.Contains("gsc_available")
                                ? inventoryEntity.GetAttributeValue<Int32>("gsc_available")
                                : 0;
                            countEntryDetail["gsc_allocatedqty"] = inventoryEntity.Contains("gsc_allocated")
                                ? inventoryEntity.GetAttributeValue<Int32>("gsc_allocated")
                                : 0;
                            countEntryDetail["gsc_onhandqty"] = inventoryEntity.Contains("gsc_onhand")
                                ? inventoryEntity.GetAttributeValue<Int32>("gsc_onhand")
                                : 0;
                            countEntryDetail["gsc_varianceqty"] = inventoryEntity.Contains("gsc_onhand")
                                ? 0 - inventoryEntity.GetAttributeValue<Int32>("gsc_onhand")
                                : 0;
                        }

                        _organizationService.Create(countEntryDetail);

                        returnEntities.Add(countEntryDetail);

                        _tracingService.Trace("Vehicle Count Entry Detail Created.");
                    }
                }
                _tracingService.Trace("CreateVehicleCountEntryDetail Method Ended.");
            }

            return returnEntities;
        }

        //Created By: Leslie G. Baliguat, Created On: 8/26/2016
        /*Purpose: Create Inventory Count Breakdown when Vehicle Count Schedule Started
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_status
         * Primary Entity: Vehicle Count Schedule
         */
        public List<Entity> CreateVehicleCountBreakdown(Entity countScheduleEntity, Guid countEntryEntityId)
        {
            List<Entity> returnEntities = new List<Entity>();

            if (countEntryEntityId != Guid.Empty && countEntryEntityId != null)
            {
                _tracingService.Trace("CreateVehicleCountBreakdown Method Started.");

                EntityCollection countEntryDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountentrydetails", "gsc_vehiclecountentryid", countEntryEntityId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_modelcode", "gsc_optioncode", "gsc_siteid", "gsc_productquantityid", "gsc_vehiclecolorid" });

                if (countEntryDetailRecords != null && countEntryDetailRecords.Entities.Count > 0)
                {
                    foreach (var countEntryDetail in countEntryDetailRecords.Entities)
                    {
                        _tracingService.Trace("Retrieve Vehicle Count Entry Detail.");

                        var inventory = countEntryDetail.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                            ? countEntryDetail.GetAttributeValue<EntityReference>("gsc_productquantityid")
                            : null;

                        var filterConditionList = new List<ConditionExpression>
                        {
                            new ConditionExpression("gsc_productquantityid", ConditionOperator.Equal, inventory.Id),
                            new ConditionExpression("gsc_status", ConditionOperator.NotEqual, 100000002)
                        };

                        EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_inventory", filterConditionList, _organizationService, null, OrderType.Ascending,
                            new[] { "gsc_productionno", "gsc_csno", "gsc_engineno", "gsc_vin", "gsc_modelyear" });

                        if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                        {
                            foreach (var inventoryEntity in inventoryRecords.Entities)
                            {
                                Entity countBreakdown = new Entity("gsc_iv_vehiclecountbreakdown");

                                //From Vehicle Count Entry Details
                                countBreakdown["gsc_vehiclecountentrydetailid"] = new EntityReference(countEntryDetail.LogicalName, countEntryDetail.Id);
                                countBreakdown["gsc_inventoryid"] = new EntityReference(inventoryEntity.LogicalName, inventoryEntity.Id);
                                countBreakdown["gsc_vehiclecountbreakdownpn"] = countEntryDetail.Contains("gsc_modelcode")
                                    ? countEntryDetail.GetAttributeValue<String>("gsc_modelcode")
                                    : String.Empty;
                                countBreakdown["gsc_optioncode"] = countEntryDetail.Contains("gsc_optioncode")
                                    ? countEntryDetail.GetAttributeValue<String>("gsc_optioncode")
                                    : String.Empty;
                                countBreakdown["gsc_site"] = countEntryDetail.Contains("gsc_site")
                                    ? countEntryDetail.GetAttributeValue<String>("gsc_site")
                                    : String.Empty;
                                countBreakdown["gsc_color"] = countEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                                     ? countEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Name
                                     : null;

                                //From Vehicle Count Entry
                                countBreakdown["gsc_recordownerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                                    ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                                    : null;
                                countBreakdown["gsc_dealerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                                    ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                                    : null;
                                countBreakdown["gsc_branchid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                                    ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                                    : null;

                                //From inventory
                                countBreakdown["gsc_productionno"] = inventoryEntity.Contains("gsc_productionno")
                                    ? inventoryEntity.GetAttributeValue<String>("gsc_productionno")
                                    : String.Empty;
                                countBreakdown["gsc_csno"] = inventoryEntity.Contains("gsc_csno")
                                    ? inventoryEntity.GetAttributeValue<String>("gsc_csno")
                                    : String.Empty;
                                countBreakdown["gsc_engineno"] = inventoryEntity.Contains("gsc_engineno")
                                    ? inventoryEntity.GetAttributeValue<String>("gsc_engineno")
                                    : String.Empty;
                                countBreakdown["gsc_vin"] = inventoryEntity.Contains("gsc_vin")
                                    ? inventoryEntity.GetAttributeValue<String>("gsc_vin")
                                    : String.Empty;
                                countBreakdown["gsc_modelyear"] = inventoryEntity.Contains("gsc_modelyear")
                                    ? inventoryEntity.GetAttributeValue<String>("gsc_modelyear")
                                    : String.Empty;

                                _organizationService.Create(countBreakdown);

                                returnEntities.Add(countBreakdown);

                                _tracingService.Trace("Vehicle Count Breakdown Created.");
                            }
                        }
                    }
                }
                _tracingService.Trace("CreateVehicleCountBreakdown Method Ended.");
            }
            return returnEntities;
        }

        //Created By: Leslie G. Baliguat, Created On: 8/31/2016
        /*Purpose: Copy Vehice Count Schedule and Details
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_lastuseddate
         * Primary Entity: Vehicle Count Schedule
         */
        public void ReuseCountSchedule(Entity countScheduleEntity)
        {
            var newCountSchedId = CopyVehicleCountSchedule(countScheduleEntity);
            CopyVehicleCountScheduleDetail(countScheduleEntity, newCountSchedId);

            //Update last count to Date Today
            UpdateLastCount(countScheduleEntity, "gsc_lastuseddate");
        }

        public Guid CopyVehicleCountSchedule(Entity countScheduleEntity)
        {
            _tracingService.Trace("CopyVehicleCountSchedule Method Started.");

            Entity reuseSchedEntity = new Entity("gsc_iv_vehiclecountschedule");

            //General Information
            reuseSchedEntity["gsc_vehiclecountschedulepn"] = countScheduleEntity.Contains("gsc_vehiclecountschedulepn")
                ? countScheduleEntity.GetAttributeValue<String>("gsc_vehiclecountschedulepn")
                : String.Empty;
            reuseSchedEntity["gsc_description"] = countScheduleEntity.Contains("gsc_description")
                ? countScheduleEntity.GetAttributeValue<String>("gsc_description")
                : String.Empty;
            reuseSchedEntity["gsc_status"] = new OptionSetValue(100000000);

            //Record Information
            reuseSchedEntity["gsc_recordownerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                : null;
            reuseSchedEntity["gsc_dealerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;
            reuseSchedEntity["gsc_branchid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;

            _tracingService.Trace("Vehicle Count Schedule Copied.");

            _tracingService.Trace("CopyVehicleCountSchedule Method Ended.");

            return _organizationService.Create(reuseSchedEntity);
        }

        public Entity CopyVehicleCountScheduleDetail(Entity countScheduleEntity, Guid newCountSchedId)
        {
            List<Entity> returnEntities = new List<Entity>();

            if (newCountSchedId != Guid.Empty && newCountSchedId != null)
            {
                _tracingService.Trace("CopyVehicleCountScheduleDetail Method Started.");

                EntityCollection countSchedDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountscheduledetail", "gsc_vehiclecountscheduleid", countScheduleEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_vehiclebasemodelid", "gsc_productid", "gsc_modelcode", "gsc_optioncode", "gsc_vehiclecolorid", "gsc_siteid", "gsc_productquantityid", "gsc_lastcount" });

                if (countSchedDetailRecords != null && countSchedDetailRecords.Entities.Count > 0)
                {
                    Entity newCountSchedDetail = new Entity("gsc_iv_vehiclecountscheduledetail");

                    foreach (var countSchedDetail in countSchedDetailRecords.Entities)
                    {
                        _tracingService.Trace("Retrieve Vehicle Count Schedule Detail.");

                        newCountSchedDetail["gsc_vehiclecountscheduleid"] = new EntityReference("gsc_iv_vehiclecountschedule", newCountSchedId);
                        newCountSchedDetail["gsc_vehiclebasemodelid"] = countSchedDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                            : null;
                        newCountSchedDetail["gsc_productid"] = countSchedDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_productid")
                            : null;
                        newCountSchedDetail["gsc_modelcode"] = countSchedDetail.Contains("gsc_modelcode")
                            ? countSchedDetail.GetAttributeValue<String>("gsc_modelcode")
                            : String.Empty;
                        newCountSchedDetail["gsc_optioncode"] = countSchedDetail.Contains("gsc_optioncode")
                            ? countSchedDetail.GetAttributeValue<String>("gsc_optioncode")
                            : String.Empty;
                        newCountSchedDetail["gsc_vehiclecolorid"] = countSchedDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                            : null;
                        newCountSchedDetail["gsc_siteid"] = countSchedDetail.GetAttributeValue<EntityReference>("gsc_siteid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_siteid")
                            : null;
                        newCountSchedDetail["gsc_productquantityid"] = countSchedDetail.GetAttributeValue<EntityReference>("gsc_productquantityid") != null
                            ? countSchedDetail.GetAttributeValue<EntityReference>("gsc_productquantityid")
                            : null;

                        //Record Information
                        newCountSchedDetail["gsc_recordownerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                            : null;
                        newCountSchedDetail["gsc_dealerid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                            : null;
                        newCountSchedDetail["gsc_branchid"] = countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                            ? countScheduleEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                            : null;

                        _organizationService.Create(newCountSchedDetail);

                        _tracingService.Trace("Vehicle Count Schedule Detail Copied.");

                        //Update last count to Date Today
                        UpdateLastCount(countSchedDetail, "gsc_lastcount");
                    }
                    _tracingService.Trace("CopyVehicleCountScheduleDetail Method Ended.");

                    return newCountSchedDetail;
                }
            }
            _tracingService.Trace("CopyVehicleCountScheduleDetail Method Ended.");

            return null;
        }

        private void UpdateLastCount(Entity entity, String lastCountDate)
        {
            _tracingService.Trace("Update LastCount.");

            String today = DateTime.Today.ToString("MM-dd-yyyy");

            Entity countSchedDetailtoUpdate = _organizationService.Retrieve(entity.LogicalName, entity.Id,
                new ColumnSet(lastCountDate));

            countSchedDetailtoUpdate[lastCountDate] = Convert.ToDateTime(today);

            _organizationService.Update(countSchedDetailtoUpdate);

            _tracingService.Trace("LastCount Updated.");
        }

        //Created By: Leslie G. Baliguat, Created On: 9/1/2016
        /*Purpose: 
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_status
         * Primary Entity: Vehicle Count Schedule
         */
        public void CancelCountSchedule(Entity countScheduleEntity)
        {
            _tracingService.Trace("CancelCountSchedule Method Started.");

            #region Delete Vehicle Count Entry

            EntityCollection countEntryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountentry", "gsc_vehiclecountscheduleid", countScheduleEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "createdon" });

            if (countEntryRecords != null && countEntryRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Related Vehicle Count Entry.");

                var countEntry = countEntryRecords.Entities[0];

                #region Delete Vehicle Count Entry Details

                EntityCollection countEntryDetailsRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountentrydetails", "gsc_vehiclecountentryid", countEntry.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "createdon" });

                if (countEntryDetailsRecords != null && countEntryDetailsRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Entry Details.");

                    foreach (var countEntryDetail in countEntryDetailsRecords.Entities)
                    {
                        #region Delete Count Breakdown

                        EntityCollection countBreakdownRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountbreakdown", "gsc_vehiclecountentrydetailid", countEntryDetail.Id, _organizationService, null, OrderType.Ascending,
                            new[] { "createdon" });

                        if (countBreakdownRecords != null && countEntryDetailsRecords.Entities.Count > 0)
                        {
                            _tracingService.Trace("Retrieve Count Breakdown.");

                            foreach (var countBreakdown in countBreakdownRecords.Entities)
                            {
                                _organizationService.Delete(countBreakdown.LogicalName, countBreakdown.Id);

                                _tracingService.Trace("Associated Count Breakdown Deleted.");
                            }
                        }

                        #endregion

                        _organizationService.Delete(countEntryDetail.LogicalName, countEntryDetail.Id);

                        _tracingService.Trace("Associated Detail Deleted.");
                    }
                }

                #endregion

                _organizationService.Delete(countEntry.LogicalName, countEntry.Id);

                _tracingService.Trace("Related Vehicle Count Entry Deleted.");

            }

            #endregion

            _tracingService.Trace("CancelCountSchedule Method Ended.");

            //Update Status Back to Available
            UpdateStatus(countScheduleEntity);
        }

        private void UpdateStatus(Entity countScheduleEntity)
        {
            _tracingService.Trace("Update Status.");

            Entity countSchedtoUpdate = _organizationService.Retrieve(countScheduleEntity.LogicalName, countScheduleEntity.Id,
                new ColumnSet("gsc_changestatus"));

            countSchedtoUpdate["gsc_status"] = countScheduleEntity.Contains("gsc_changestatus")
                ? countScheduleEntity.GetAttributeValue<OptionSetValue>("gsc_changestatus")
                : null;

            _organizationService.Update(countSchedtoUpdate);

            _tracingService.Trace("Status Updated.");
        }
    }
}