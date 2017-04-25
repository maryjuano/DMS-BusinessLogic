//using GSC.Rover.DMS.BusinessLogic.CommittedFirmOrderQuantity;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.CommittedFirmOrderQuantityDetail
{
    public class CommittedFirmOrderQuantityDetailHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public CommittedFirmOrderQuantityDetailHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace; 
        }

        //Created By: Leslie Baliguat, Created On: 06/23/2016
        /*Purpose: Once MMPC Import the updated CFO Quantity Detail Records, 
         *          Allocated Quantity being filled out, Allocated Quantity value 
         *          will be replicated to remaing allocated quantity of CFO Quantity Detail
         *          and to the CFO Quantity of Order Planning Detail to where it is associated.
         * Registration Details: 
         * Event/Message: 
         *      Post/Update: gsc_allocatedquantity
         * Primary Entity: Committed Firm Order Quantity Detail
         */
        public void ReplicateAllocatedQuantity(Entity cfoQuantityDetailsEntity)
        {
            _tracingService.Trace("Started ReplicateAllocatedQuantity Method.");

            var allocated = cfoQuantityDetailsEntity.Contains("gsc_allocatedquantity")
                ? cfoQuantityDetailsEntity.GetAttributeValue<Int32>("gsc_allocatedquantity")
                : 0;
            var cfoQuantity = cfoQuantityDetailsEntity.Contains("gsc_cfoquantity")
                ? cfoQuantityDetailsEntity.GetAttributeValue<Int32>("gsc_cfoquantity")
                : 0;

            //Replicate Allocated Quantity to CFO Allocation
            var cfoQuantityId = cfoQuantityDetailsEntity.Contains("gsc_committedfirmorderquantityid")
                ? cfoQuantityDetailsEntity.GetAttributeValue<EntityReference>("gsc_committedfirmorderquantityid").Id
                : Guid.Empty;
            var orderPlanningId = cfoQuantityDetailsEntity.Contains("gsc_orderplanningid")
                ? cfoQuantityDetailsEntity.GetAttributeValue<EntityReference>("gsc_orderplanningid").Id
                : Guid.Empty;
            var month = String.Empty;
            var year = String.Empty;

            EntityCollection cfoQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantity", "gsc_sls_committedfirmorderquantityid", cfoQuantityId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_cfomonth", "gsc_year" });

            if (cfoQuantityCollection != null && cfoQuantityCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve month and year from parent cfo quantity record");

                Entity cfoQuantityEntity = cfoQuantityCollection.Entities[0];
                month = cfoQuantityEntity.Contains("gsc_cfomonth")
                    ? cfoQuantityEntity.FormattedValues["gsc_cfomonth"]
                    : String.Empty;
                year = cfoQuantityEntity.Contains("gsc_year")
                    ? cfoQuantityEntity.FormattedValues["gsc_year"]
                    : String.Empty;
            }
            _tracingService.Trace("Month: " + Convert.ToInt16(month) + " Year: " + Convert.ToInt16(year));
            EntityCollection orderPlanningCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_orderplanning", "gsc_sls_orderplanningid", orderPlanningId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_orderplanningpn" });
            _tracingService.Trace("Order Planning records retrieved: " + orderPlanningCollection.Entities.Count);
            if (orderPlanningCollection != null && orderPlanningCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Order Planning");

                var detailConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_orderplanningid", ConditionOperator.Equal, orderPlanningId),
                    new ConditionExpression("gsc_month", ConditionOperator.Equal, DateTimeFormatInfo.CurrentInfo.GetMonthName(Convert.ToInt16(month))),
                    new ConditionExpression("gsc_year", ConditionOperator.Equal, year)
                };
                _tracingService.Trace("Month: " + DateTimeFormatInfo.CurrentInfo.GetMonthName(Convert.ToInt16(month)) + " Year: " + year);
                EntityCollection planningDetailsCollection = CommonHandler.RetrieveRecordsByConditions("gsc_sls_orderplanningdetail", detailConditionList, _organizationService, "createdon", OrderType.Descending,
                    new[] { "gsc_cfoallocation" });

                _tracingService.Trace("Order Planning Detail records retrieved: " + planningDetailsCollection.Entities.Count);
                if (planningDetailsCollection != null && planningDetailsCollection.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Order Planning Detail.");

                    Entity orderPlanningDetails = planningDetailsCollection.Entities[0];

                    orderPlanningDetails["gsc_cfoallocation"] = allocated != 0 ? allocated : cfoQuantity;
                    _organizationService.Update(orderPlanningDetails);

                    _tracingService.Trace("Order Planning CFO Allocation Updated.");
                }

            }

            _tracingService.Trace("Ended ReplicateAllocatedQuantity Method.");
        }

        public Entity SubmitVPO(Entity cfoQuantityDetailEntity, String message)
        {
            _tracingService.Trace("Update Submitted VPO.");

            var forSubmission = cfoQuantityDetailEntity.Contains("gsc_vpoquantityforsubmission")
                ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_vpoquantityforsubmission")
                : 0;
            var submittedQty = cfoQuantityDetailEntity.Contains("gsc_submittedvpo")
                ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_submittedvpo")
                : 0;

            cfoQuantityDetailEntity["gsc_submittedvpo"] = submittedQty + forSubmission;

            _tracingService.Trace("Submitted VPO Computed.");

            cfoQuantityDetailEntity = ComputeVPOBalance(cfoQuantityDetailEntity);

            if (message.Equals("Update"))
            {
                _tracingService.Trace("Update Computation.");

                //Replicated Allocated Quantity to Remaing Allocated Quantity
                Entity cfoQuantityDetailToUpdate = _organizationService.Retrieve(cfoQuantityDetailEntity.LogicalName, cfoQuantityDetailEntity.Id,
                               new ColumnSet("gsc_submittedvpo", "gsc_vpoquantityforsubmission", "gsc_vpobalance"));
                cfoQuantityDetailToUpdate["gsc_submittedvpo"] = cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_submittedvpo");
                cfoQuantityDetailToUpdate["gsc_vpobalance"] = cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_vpobalance");
                cfoQuantityDetailToUpdate["gsc_vpoquantityforsubmission"] = 0;

                _organizationService.Update(cfoQuantityDetailToUpdate);

                _tracingService.Trace("Computation Updated.");
            }

            _tracingService.Trace("Update CFO Quantity Details.");

            return cfoQuantityDetailEntity;

        }

        //Remaing Allocated Quantity = Remaining Allocated Quantity - Order Quantity
        public Entity ComputeVPOBalance(Entity cfoQuantityDetailEntity)
        {
            _tracingService.Trace("Compute VPO Balance.");

            var allocatedQty = cfoQuantityDetailEntity.Contains("gsc_allocatedquantity")
                ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_allocatedquantity")
                : 0;
            var submitted = cfoQuantityDetailEntity.Contains("gsc_submittedvpo")
                ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_submittedvpo")
                : 0; ;
            var balance = cfoQuantityDetailEntity.Contains("gsc_vpobalance")
                ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_vpobalance")
                : 0;

            cfoQuantityDetailEntity = GetVPOBalfromPrevMonth(cfoQuantityDetailEntity);

            var prevBalance = cfoQuantityDetailEntity.Contains("gsc_vpobalfromprevmonth")
                ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_vpobalfromprevmonth")
                : 0;

            cfoQuantityDetailEntity["gsc_vpobalance"] = allocatedQty + prevBalance - submitted;

            _tracingService.Trace("VPO Balance Computed.");

            return cfoQuantityDetailEntity;
        }

        public Entity UpdateVPOBalance(Entity cfoQuantityEntity)
        {
            cfoQuantityEntity = ComputeVPOBalance(cfoQuantityEntity);

            Entity cfoQuantityDetailToUpdate = _organizationService.Retrieve(cfoQuantityEntity.LogicalName, cfoQuantityEntity.Id,
                               new ColumnSet("gsc_vpobalance"));
            cfoQuantityDetailToUpdate["gsc_vpobalance"] = cfoQuantityEntity["gsc_vpobalance"];

            _organizationService.Update(cfoQuantityDetailToUpdate);

            return cfoQuantityEntity;
        }

        public Entity GetVPOBalfromPrevMonth(Entity cfoQuantityDetailEntity)
        {
            _tracingService.Trace("GetVPOBalfromPrevMonth Started.");

            cfoQuantityDetailEntity["gsc_vpobalfromprevmonth"] = 0;

            var modelDesc = cfoQuantityDetailEntity.Contains("gsc_productid")
                ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;
            var site = cfoQuantityDetailEntity.Contains("gsc_siteid")
                ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                : Guid.Empty;
            var year = 0;
            var month = 0;
            
            var cfoId = cfoQuantityDetailEntity.Contains("gsc_committedfirmorderquantityid")
                ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_committedfirmorderquantityid").Id
                : Guid.Empty;

            EntityCollection cfoRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantity", "gsc_sls_committedfirmorderquantityid", cfoId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_cfomonth", "gsc_year" });

            if (cfoRecords != null && cfoRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Parent CFO.");

                Entity cfoEntity = cfoRecords.Entities[0];

                month = cfoEntity.Contains("gsc_cfomonth")
                    ? cfoEntity.GetAttributeValue<OptionSetValue>("gsc_cfomonth").Value
                    : 0;
                year = cfoEntity.Contains("gsc_year")
                    ? cfoEntity.GetAttributeValue<OptionSetValue>("gsc_year").Value
                    : 0;
            }

            var detailConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_cfomonth", ConditionOperator.Equal, month - 1),
                    new ConditionExpression("gsc_year", ConditionOperator.Equal, year)
                };

            EntityCollection detailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_committedfirmorderquantity", detailConditionList, _organizationService, "createdon", OrderType.Descending,
                    new[] { "gsc_cfomonth" });

                if (detailRecords != null && detailRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Prev CFO.");

                    var cfodetailConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_committedfirmorderquantityid", ConditionOperator.Equal, detailRecords.Entities[0].Id),
                        new ConditionExpression("gsc_productid", ConditionOperator.Equal, modelDesc),
                        new ConditionExpression("gsc_siteid", ConditionOperator.Equal, site)
                    };

                    EntityCollection detailRecords2 = CommonHandler.RetrieveRecordsByConditions("gsc_sls_committedfirmorderquantitydetail", cfodetailConditionList, _organizationService, "createdon", OrderType.Descending,
                            new[] { "gsc_vpobalance" });

                    if (detailRecords2 != null && detailRecords2.Entities.Count > 0)
                    {
                        _tracingService.Trace("Retrieve Prev CFO Detail.");

                        cfoQuantityDetailEntity["gsc_vpobalfromprevmonth"] = detailRecords2.Entities[0].GetAttributeValue<Int32>("gsc_vpobalance");
                    }
                }

            return cfoQuantityDetailEntity;
        }

        //Created By: Jerome Anthony Gerero, Created On: 03/02/2017
        /*Purpose: Populate site field on record create
         * Registration Details: 
         * Event/Message: 
         *      Pre/Create: Site / gsc_site
         * Primary Entity: Committed Firm Order Quantity Detail
         */
        public Entity PopulateSiteField(Entity cfoQuantityDetailEntity)
        {
            _tracingService.Trace("Started PopulateSiteField method..");

            Guid cfoQuantityId = cfoQuantityDetailEntity.Contains("gsc_committedfirmorderquantityid")
                ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_committedfirmorderquantityid").Id
                : Guid.Empty;

            EntityCollection cfoQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantity", "gsc_sls_committedfirmorderquantityid", cfoQuantityId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_siteid" });

            if (cfoQuantityRecords != null && cfoQuantityRecords.Entities.Count > 0)
            {
                Entity cfoQuantity = cfoQuantityRecords.Entities[0];

                cfoQuantityDetailEntity["gsc_siteid"] = cfoQuantity.Contains("gsc_siteid")
                    ? cfoQuantity.GetAttributeValue<EntityReference>("gsc_siteid")
                    : null;
            }

            _tracingService.Trace("Ended PopulateSiteField method..");
            return cfoQuantityDetailEntity;
        }
        public Boolean CheckifCFOStatusisSubmitted(Entity cfoQuantityDetailEntity)
        {
            var cfoId = cfoQuantityDetailEntity.Contains("gsc_committedfirmorderquantityid")
                ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_committedfirmorderquantityid").Id
                : Guid.Empty;

            EntityCollection cfoRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantity", "gsc_sls_committedfirmorderquantityid", cfoId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_cfostatus"});

            if (cfoRecords != null && cfoRecords.Entities.Count > 0)
            {
                if (cfoRecords.Entities[0].GetAttributeValue<OptionSetValue>("gsc_cfostatus").Value == 100000001)
                    return true;
            }

            return false;
        }

        public Entity UpdateStatustoCompleted(Entity cfoQuantityDetailEntity)
        {
            _tracingService.Trace("Started UpdateStatustoCompleted Method.");

            var cfoId = cfoQuantityDetailEntity.Contains("gsc_committedfirmorderquantityid")
                ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_committedfirmorderquantityid").Id
                : Guid.Empty;
            var isCompleted = true;

            EntityCollection cfoDetailsRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantitydetail", "gsc_committedfirmorderquantityid", cfoId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_vpobalance" });

            if (cfoDetailsRecords != null && cfoDetailsRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve CFO Details.");

                foreach (Entity cfoDetail in cfoDetailsRecords.Entities)
                {
                    if (cfoDetail.GetAttributeValue<Int32>("gsc_vpobalance") != 0)
                    {
                        isCompleted = false;
                        _tracingService.Trace("Not all VPO Balance is zero(0).");
                        break;
                    }

                }
            }

            if (isCompleted)
            {
                _tracingService.Trace("Not all VPO Balance is zero(0).");

                EntityCollection cfoRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantity", "gsc_sls_committedfirmorderquantityid", cfoId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_cfostatus" });

                if (cfoRecords != null && cfoRecords.Entities.Count > 0)
                {
                    Entity cfoEntity = cfoRecords.Entities[0];

                    cfoEntity["gsc_cfostatus"] = new OptionSetValue(100000002);

                    _organizationService.Update(cfoEntity);

                    _tracingService.Trace("Status Completed.");
                }
            }

            return cfoQuantityDetailEntity;
        }

        public Boolean RestrictDelete(Entity cfoQuantityDetailEntity)
        {
            var allocatedQty = cfoQuantityDetailEntity.Contains("gsc_allocatedquantity")
                ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_allocatedquantity")
                : 0;

            if(allocatedQty != 0)
            {
                return false;
            }

            return true;
        }

        public Boolean RestrictDeleteCreate(Entity cfoQuantityDetailEntity)
        {
            var cfoId = cfoQuantityDetailEntity.Contains("gsc_committedfirmorderquantityid")
                ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_committedfirmorderquantityid").Id
                : Guid.Empty;

            EntityCollection cfoRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantity", "gsc_sls_committedfirmorderquantityid", cfoId, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_cfostatus"});

            if (cfoRecords != null && cfoRecords.Entities.Count > 0)
            {
                if (cfoRecords.Entities[0].GetAttributeValue<OptionSetValue>("gsc_cfostatus").Value != 100000000)
                    return false;
            }

            return true;

        }

        //Created By: Raphael Herrera, Created On: 03/08/2017
        /*Purpose: Relate corresponding order planning record to cfo quantity details
         * Registration Details: 
         * Event/Message: 
         *      Post/Create: CFO Quantity Details / gsc_sls_committedfirmorderquantitydetail
         *      Post/Update: gsc_productid
         * Primary Entity: Committed Firm Order Quantity Detail
         */
        public Entity RelateOrderPlanningRecord(Entity cfoQuantityDetailEntity)
        {
            _tracingService.Trace("Started RelateOrderPlanningRecord Method...");

            var productId = cfoQuantityDetailEntity.Contains("gsc_productid") ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;

            EntityCollection orderPlanningCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_orderplanning", "gsc_productid", productId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_orderplanningpn" });

            _tracingService.Trace("Order Planning records retrieved: " + orderPlanningCollection.Entities.Count);
            if (orderPlanningCollection.Entities.Count > 0)
            {
                cfoQuantityDetailEntity["gsc_orderplanningid"] = new EntityReference("gsc_sls_orderplanning", orderPlanningCollection.Entities[0].Id);
                _organizationService.Update(cfoQuantityDetailEntity);
                _tracingService.Trace("Updated cfo quantity detail..");
            }


            ReplicateAllocatedQuantity(cfoQuantityDetailEntity);
            _tracingService.Trace("Ending RelateOrderPlanningRecord Method...");
            return cfoQuantityDetailEntity;
        }
    }
}
