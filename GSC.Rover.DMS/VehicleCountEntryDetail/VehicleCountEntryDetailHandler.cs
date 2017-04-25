using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace GSC.Rover.DMS.BusinessLogic.VehicleCountEntryDetail
{
    public class VehicleCountEntryDetailHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleCountEntryDetailHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 9/2/2016
        /*Purpose: Compute for Variance Quantity = OnHand Quantity - Counted Quantity
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_countedqty
         * Primary Entity: Vehicle Count Entry Detail
         */
        public void ComputeVariance(Entity countEntryDetail)
        {
            _tracingService.Trace("ComputeVariance Method Started.");
            var onHandQty = countEntryDetail.Contains("gsc_onhandqty")
                ? countEntryDetail.GetAttributeValue<Int32>("gsc_onhandqty")
                : 0;
            var countedQty = countEntryDetail.Contains("gsc_countedqty")
                ? countEntryDetail.GetAttributeValue<Int32>("gsc_countedqty")
                : 0;

            var varianceQty = countedQty - onHandQty;

            Entity countEntryDetailtoUpdate = _organizationService.Retrieve(countEntryDetail.LogicalName, countEntryDetail.Id,
                new ColumnSet("gsc_varianceqty"));

            countEntryDetailtoUpdate["gsc_varianceqty"] = varianceQty;

            _organizationService.Update(countEntryDetailtoUpdate);

            _tracingService.Trace("ComputeVariance Method Ended.");
        }

        //Created By: Leslie Baliguat, Created On: 9/2/2016
        /*Purpose: Update the Status of Related Vehicle Count Entry and Vehicle Count Schedule Status to 'Entered'
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_countedqty
         * Primary Entity: Vehicle Count Entry Detail
         */
        public void UpdateStatus(Entity countEntryDetail)
        {
            _tracingService.Trace("UpdateStatus Method Started.");

            var countEntryId = countEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecountentryid") != null
                ? countEntryDetail.GetAttributeValue<EntityReference>("gsc_vehiclecountentryid").Id
                : Guid.Empty;

            EntityCollection countEntryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountentry", "gsc_iv_vehiclecountentryid", countEntryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_status", "gsc_vehiclecountscheduleid" });

            if (countEntryRecords != null && countEntryRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Related VehicleCountEntry.");

                var countEntry = countEntryRecords.Entities[0];

                var countScheduleId = countEntry.GetAttributeValue<EntityReference>("gsc_vehiclecountscheduleid") != null
                    ? countEntry.GetAttributeValue<EntityReference>("gsc_vehiclecountscheduleid").Id
                    : Guid.Empty;

                EntityCollection countSchedRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclecountschedule", "gsc_iv_vehiclecountscheduleid", countScheduleId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_status" });

                if (countSchedRecords != null && countSchedRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Related VehicleCountSchedule.");

                    var countSched = countSchedRecords.Entities[0];

                    countSched["gsc_status"] = new OptionSetValue(100000002);

                    _organizationService.Update(countSched);

                    _tracingService.Trace("VehicleCountSchedule Status Updated.");
                }

                countEntry["gsc_status"] = new OptionSetValue(100000001);

                _organizationService.Update(countEntry);

                _tracingService.Trace("VehicleCountEntry Status Updated.");
            }

            _tracingService.Trace("UpdateStatus Method Ended.");
        }

        //Created By: Leslie Baliguat, Created On: 9/2/2016
        /*Purpose: Compute for Verified Counted Quantity. 
         *         Update Vehicle Count Entry Detail's "Verified" Field to True if "Verified" Count Breakdown
         *          is greater than or equal to "Counted Qty"
         * Registration Details: 
         * Event/Message:
         *      Post/Update: gsc_countedqty
         * Primary Entity: Vehicle Count Entry Detail
         */
        public void ComputeVerifiedCountedQty(Entity countEntryDetail)
        {
            _tracingService.Trace("ComputeVerifiedCountedQty Method Started.");

            var filterConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_vehiclecountentrydetailid", ConditionOperator.Equal, countEntryDetail.Id),
                        new ConditionExpression("gsc_verified", ConditionOperator.Equal, true)
                    };

            EntityCollection countBreakdownRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_vehiclecountbreakdown", filterConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_verified" });

            var verifiedBreakdwon = countBreakdownRecords != null
                    ? countBreakdownRecords.Entities.Count
                    : 0;

            var onhandQty = countEntryDetail.Contains("gsc_onhandqty")
                ? countEntryDetail.GetAttributeValue<Int32>("gsc_onhandqty")
                : 0;

            var countedQty = countEntryDetail.Contains("gsc_countedqty")
                ? countEntryDetail.GetAttributeValue<Int32>("gsc_countedqty")
                : 0;

            var varianceQty = countEntryDetail.Contains("gsc_varianceqty")
                ? countEntryDetail.GetAttributeValue<Int32>("gsc_varianceqtyd")
                : 0;

            Entity countEntryDetailtoUpdate = _organizationService.Retrieve(countEntryDetail.LogicalName, countEntryDetail.Id,
                new ColumnSet("gsc_verified", "gsc_verifiedcountedqty"));


            if (varianceQty > 0) //If Variance's operation is Add
            {
                if (verifiedBreakdwon == onhandQty) //verified Breakdown should equal to on hand quantity
                {
                    _tracingService.Trace("Verified Breakdown is Equal to OnHand Quantity.");

                    countEntryDetailtoUpdate["gsc_verified"] = true;
                }

                else
                {
                    throw new InvalidPluginExecutionException("Verified Stock Count Entry should equal to on hand quantity.");
                }
            }

            else //If Variance's operaion is Subtract or Variance is 0
            {
                if (verifiedBreakdwon == countedQty)//Verified Breakdown should equal to counted qty.
                {
                    _tracingService.Trace("Verified Breakdown is Equal to Counted Quantity.");

                    countEntryDetailtoUpdate["gsc_verified"] = true;
                }

                else
                {
                    throw new InvalidPluginExecutionException("Verified Stock Count Entry should equal to counted quantity.");
                }
            }

            countEntryDetailtoUpdate["gsc_verifiedcountedqty"] = verifiedBreakdwon;

            _organizationService.Update(countEntryDetailtoUpdate);

            _tracingService.Trace("Verified Field was Updated.");

            _tracingService.Trace("ComputeVerifiedCountedQty Method Ended.");
        }
    }
}
