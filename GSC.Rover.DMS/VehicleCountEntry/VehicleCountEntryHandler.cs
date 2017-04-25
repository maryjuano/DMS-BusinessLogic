using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.VehicleCountEntry
{
    public class VehicleCountEntryHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleCountEntryHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie G. Baliguat
        public void ProcessCountEntry(Entity countEntry)
        {
            _tracingService.Trace("ProcessCountEntry Method Started.");

            var NullCountedCondition = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_vehiclecountentryid", ConditionOperator.Equal, countEntry.Id),
                        new ConditionExpression("gsc_countedqty", ConditionOperator.Null)
                    };

            EntityCollection nullDetailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_vehiclecountentrydetails", NullCountedCondition, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_verified" });

            if (nullDetailRecords == null || nullDetailRecords.Entities.Count == 0)
            {
                var filterConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_vehiclecountentryid", ConditionOperator.Equal, countEntry.Id),
                        new ConditionExpression("gsc_countedqty", ConditionOperator.NotNull),
                        new ConditionExpression("gsc_verified", ConditionOperator.Equal, false),
                    };

                EntityCollection invalidDetailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_vehiclecountentrydetails", filterConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_verified" });

                if (invalidDetailRecords == null || invalidDetailRecords.Entities.Count == 0)
                {
                    var filterConditionList2 = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_vehiclecountentryid", ConditionOperator.Equal, countEntry.Id),
                        new ConditionExpression("gsc_verified", ConditionOperator.Equal, true)
                    };

                    EntityCollection detailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_vehiclecountentrydetails", filterConditionList2, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_varianceqty", "gsc_description", "gsc_vehiclebasemodelid", "gsc_productid", "gsc_siteid", "gsc_vehiclecolorid", "gsc_optioncode", "gsc_modelcode" });

                    if (detailRecords != null && detailRecords.Entities.Count > 0)
                    {
                        foreach (var entryDetail in detailRecords.Entities)
                        {
                            _tracingService.Trace("Retrieve Entry Details.");

                            var variance = entryDetail.Contains("gsc_varianceqty")
                                ? entryDetail.GetAttributeValue<Int32>("gsc_varianceqty")
                                : 0;

                            if (variance < 0)
                            {
                                _tracingService.Trace("Variance Less Than 0");

                                Guid vehicleAdjustmentId = CreateAdjustmentRecord(entryDetail); // Subtract
                                SubtractVehicleAdjustment(entryDetail, vehicleAdjustmentId);
                            }

                            else if (variance > 0)
                            {
                                _tracingService.Trace("Variance Greater Than 0");

                                while (variance > 0)
                                {
                                    Guid vehicleAdjustmentId = CreateAdjustmentRecord(entryDetail); // Add
                                    AddVehicleAdjustment(entryDetail, vehicleAdjustmentId);
                                    variance--;
                                }
                            }

                        }
                    }
                }

                else
                {
                    //Throw an error
                    throw new InvalidPluginExecutionException("ERROR: Cannot proceed in processing this record. Atleast one of the Details is not yet verified.");
                }
            }

            else
            {
                //Throw an error
                throw new InvalidPluginExecutionException("ERROR: Cannot proceed in processing this record. Please provide counted quantity to all details.");
            }
            _tracingService.Trace("ProcessCountEntry Method Ended.");
        }

        public Guid CreateAdjustmentRecord(Entity entryDetail)
        {
            _tracingService.Trace("CreateAdjustmentRecord Method Started.");

            String today = DateTime.Today.ToString("MM-dd-yyyy");

            Entity adjustmentEntity = new Entity("gsc_sls_vehicleadjustmentvarianceentry");

            adjustmentEntity["gsc_documenttype"] = new OptionSetValue(100000001);
            adjustmentEntity["gsc_description"] =entryDetail.Contains("gsc_description")
                ? entryDetail.GetAttributeValue<String>("gsc_description")
                : String.Empty;
            adjustmentEntity["gsc_adjustmentvariancedate"] = Convert.ToDateTime(today);
            adjustmentEntity["gsc_adjustmentvariancestatus"] = new OptionSetValue(100000000);

            _tracingService.Trace("CreateAdjustmentRecord Method Ended.");

            return _organizationService.Create(adjustmentEntity);
        }

        public void AddVehicleAdjustment(Entity entryDetail, Guid vehicleAdjustmentId)
        {
            _tracingService.Trace("AddVehicleAdjustment Method Started.");

            Entity vehicleAdjusted = new Entity("gsc_sls_adjustmentvariancedetail");

            vehicleAdjusted["gsc_vehicleadjustmentvarianceentryid"] = new EntityReference("gsc_sls_vehicleadjustmentvarianceentry", vehicleAdjustmentId);
            /*vehicleAdjusted["gsc_modelyear"] = entryDetail.Contains("gsc_modelyear")
                ? entryDetail.GetAttributeValue<String>("gsc_modelyear")
                : String.Empty;*/
            vehicleAdjusted["gsc_vehiclebasemodelid"] = entryDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                ? entryDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                : null;
            vehicleAdjusted["gsc_productid"] = entryDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? entryDetail.GetAttributeValue<EntityReference>("gsc_productid")
                : null;
            vehicleAdjusted["gsc_modelcode"] = entryDetail.Contains("gsc_modelcode")
                ? entryDetail.GetAttributeValue<String>("gsc_modelcode")
                : String.Empty;
            vehicleAdjusted["gsc_optioncode"] = entryDetail.Contains("gsc_optioncode")
                ? entryDetail.GetAttributeValue<String>("gsc_optioncode")
                : String.Empty;
            vehicleAdjusted["gsc_vehiclecolorid"] =  entryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                ? entryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                : null;
            vehicleAdjusted["gsc_siteid"] = entryDetail.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? entryDetail.GetAttributeValue<EntityReference>("gsc_siteid")
                : null;
            vehicleAdjusted["gsc_quantity"] = 1;
            vehicleAdjusted["gsc_operation"] = new OptionSetValue(100000000);

            _organizationService.Create(vehicleAdjusted);

            _tracingService.Trace("AddVehicleAdjustment Method Ended.");
        }

        public List<Entity> SubtractVehicleAdjustment(Entity entryDetail, Guid vehicleAdjustmentId)
        {
            _tracingService.Trace("SubtractVehicleAdjustment Method Started.");

            List<Entity> returnEntities = new List<Entity>();

            var filterConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_vehiclecountentrydetailid", ConditionOperator.Equal, entryDetail.Id),
                    new ConditionExpression("gsc_verified", ConditionOperator.Equal, false)
                };

            EntityCollection countBreakdownRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_vehiclecountbreakdown", filterConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_verified", "gsc_productionno", "gsc_csno", "gsc_engineno", "gsc_vin", "gsc_inventoryid", "gsc_modelyear" });

            if (countBreakdownRecords != null && countBreakdownRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Breakdown.");

                foreach (var countBreakdown in countBreakdownRecords.Entities)
                {
                    _tracingService.Trace("Setup Adjustment Variance Detail.");

                    Entity vehicleAdjusted = new Entity("gsc_sls_adjustmentvariancedetail");

                    vehicleAdjusted["gsc_vehicleadjustmentvarianceentryid"] = new EntityReference("gsc_sls_vehicleadjustmentvarianceentry", vehicleAdjustmentId);
                    vehicleAdjusted["gsc_vehiclebasemodelid"] = entryDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                        ? entryDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                        : null;
                    vehicleAdjusted["gsc_productid"] = entryDetail.GetAttributeValue<EntityReference>("gsc_productid") != null
                        ? entryDetail.GetAttributeValue<EntityReference>("gsc_productid")
                        : null;
                    vehicleAdjusted["gsc_modelcode"] = entryDetail.Contains("gsc_modelcode")
                        ? entryDetail.GetAttributeValue<String>("gsc_modelcode")
                        : String.Empty;
                    vehicleAdjusted["gsc_optioncode"] = entryDetail.Contains("gsc_optioncode")
                        ? entryDetail.GetAttributeValue<String>("gsc_optioncode")
                        : String.Empty;
                    vehicleAdjusted["gsc_vehiclecolorid"] = entryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid") != null
                        ? entryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                        : null;
                    vehicleAdjusted["gsc_siteid"] = entryDetail.GetAttributeValue<EntityReference>("gsc_siteid") != null
                        ? entryDetail.GetAttributeValue<EntityReference>("gsc_siteid")
                        : null;
                    vehicleAdjusted["gsc_quantity"] = 1;
                    vehicleAdjusted["gsc_operation"] = new OptionSetValue(100000001);

                    vehicleAdjusted["gsc_csno"] = countBreakdown.Contains("gsc_csno")
                        ? countBreakdown.GetAttributeValue<String>("gsc_csno")
                        : String.Empty;
                    vehicleAdjusted["gsc_vin"] = countBreakdown.Contains("gsc_vin")
                        ? countBreakdown.GetAttributeValue<String>("gsc_vin")
                        : String.Empty;
                    vehicleAdjusted["gsc_productionno"] = countBreakdown.Contains("gsc_productionno")
                        ? countBreakdown.GetAttributeValue<String>("gsc_productionno")
                        : String.Empty;
                    vehicleAdjusted["gsc_engineno"] = countBreakdown.Contains("gsc_engineno")
                        ? countBreakdown.GetAttributeValue<String>("gsc_engineno")
                        : String.Empty;
                    vehicleAdjusted["gsc_modelyear"] = countBreakdown.Contains("gsc_modelyear")
                         ? countBreakdown.GetAttributeValue<String>("gsc_modelyear")
                         : String.Empty;
                    vehicleAdjusted["gsc_inventoryid"] = countBreakdown.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                        ? countBreakdown.GetAttributeValue<EntityReference>("gsc_inventoryid")
                        : null;

                    _organizationService.Create(vehicleAdjusted);

                    returnEntities.Add(vehicleAdjusted);

                    _tracingService.Trace("Adjustment Variance Created.");
                }
            }

            _tracingService.Trace("SubtractVehicleAdjustment Method Ended.");

            return returnEntities;
        }
    
    }
}
