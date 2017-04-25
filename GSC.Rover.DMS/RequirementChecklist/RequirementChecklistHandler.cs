using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.RequirementChecklist
{
    public class RequirementChecklistHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public RequirementChecklistHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Raphael Herrera, Cretaed On: 4/29/2016
        //Modified By: Artum M. Ramos, Cretaed On: 12/29/2016
        /*Purpose: Check if all requirement checklist have been submitted.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_submitted
         * Primary Entity: Requirement Checklist
         */
        public void SetForAllocation(Entity requirementChecklist)
        {
            _tracingService.Trace("Starting checkForCompleteRequirements method...");

            
            var orderId = requirementChecklist.Contains("gsc_orderid")
                ? requirementChecklist.GetAttributeValue<EntityReference>("gsc_orderid").Id
                : Guid.Empty;

            _tracingService.Trace("Retrieve orderCollectionToCheck...");
            EntityCollection orderCollectionToCheck = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", orderId, _organizationService, null,
                 OrderType.Ascending, new[] { "gsc_paymentmode", "gsc_bankid" });

            _tracingService.Trace("Check ortherCollection if not null...");
            if (orderCollectionToCheck.Entities.Count > 0)
            {
                Entity orderEntity = orderCollectionToCheck.Entities[0];
                _tracingService.Trace("Retrieve paymentmode and bankid...");
                var paymentmode = orderEntity.Contains("gsc_paymentmode")
                    ? orderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                    : 0;

                var bankid = orderEntity.Contains("gsc_bankid")
                    ? orderEntity.GetAttributeValue<EntityReference>("gsc_bankid").Id
                    : Guid.Empty;

                _tracingService.Trace("Check if financing...");
                if (paymentmode == 100000001)
                {
                    _tracingService.Trace("Check if bankid is null...");
                    if (bankid == null)
                    {
                        _tracingService.Trace("No Bank Record...");
                        throw new InvalidPluginExecutionException("No Bank Record in Order Details!");
                    }
                    else
                    {
                        _tracingService.Trace("Call SetForAllocationToUpdate 1...");
                        SetForAllocationToUpdate(requirementChecklist, orderId);
                    }
                }
                    _tracingService.Trace("Call SetForAllocationToUpdate 2...");
                    SetForAllocationToUpdate(requirementChecklist, orderId);

            }

            _tracingService.Trace("Ending SetForAllocation method...");

            //throw new InvalidPluginExecutionException("TEST");
        }

        //Created By: Artum M. Ramos, Cretaed On: 12/29/2016
        public void SetForAllocationToUpdate(Entity requirementChecklist, Guid orderId)
        {
            #region check for complete requirements
            var requirementChecklistConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_orderid", ConditionOperator.Equal, orderId),
                    new ConditionExpression("gsc_mandatory", ConditionOperator.Equal, true)
                };

            EntityCollection requirementChecklistCollection = CommonHandler.RetrieveRecordsByConditions("gsc_sls_requirementchecklist", requirementChecklistConditionList,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_submitted", "gsc_mandatory" });

            bool isComplete = true;
            _tracingService.Trace("Retrieved requirement checklist..." + requirementChecklistCollection.Entities.Count + " records found...");

            foreach (Entity requirementChecklistEntity in requirementChecklistCollection.Entities)
            {
                _tracingService.Trace("Checking document " + requirementChecklistEntity.GetAttributeValue<bool>("gsc_submitted") + "...");
                if (requirementChecklistEntity.GetAttributeValue<bool>("gsc_submitted") == false && requirementChecklistEntity.GetAttributeValue<bool>("gsc_mandatory") == true)
                {
                    isComplete = false;
                    _tracingService.Trace("unsubmitted mandatory document found...");
                }
            }


            var documentstatus = 0;
            var status = 0;
            if (isComplete == true)
            {
                documentstatus = 100000001;
                status = 100000002;

            }
            else if (isComplete == false)
            {
                documentstatus = 100000000;
                status = 100000000;
            }


            EntityCollection orderCollectionToUpdate = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", orderId, _organizationService, null,
                 OrderType.Ascending, new[] { "gsc_status", "gsc_documentstatus", "gsc_recordownerid" });
            _tracingService.Trace("Sales Order Records Retrieved: " + orderCollectionToUpdate.Entities.Count);
            if (orderCollectionToUpdate.Entities.Count > 0)
            {
                Entity orderToUpdate = orderCollectionToUpdate.Entities[0];



                orderToUpdate["gsc_documentstatus"] = new OptionSetValue(documentstatus);//Document Status Completed
                orderToUpdate["gsc_status"] = new OptionSetValue(status);//For Allocation
                _organizationService.Update(orderToUpdate);

                #region set date submitted and submitted by
                requirementChecklist["gsc_datesubmitted"] = DateTime.UtcNow;
                requirementChecklist["gsc_submittedby"] = orderToUpdate.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                ? orderToUpdate.GetAttributeValue<EntityReference>("gsc_recordownerid")
                : null;
                _organizationService.Update(requirementChecklist);
                _tracingService.Trace("Updated date submitted...");
                #endregion

            }
            #endregion

        }


        //Created By: Raphael Herrera, Cretaed On: 5/4/2016
        /*Purpose: Check if Required Document is Pre-defined from bank
          * Registration Details:
          * Event/Message: 
          *      Pre/Delete: Requirement Checklist
          * Primary Entity: Requirement Checklist
          */
        public void ValidateDelete(Entity requirementChecklistEntity, string message)
        {
            _tracingService.Trace("Starting ValidateDelete method...");

            bool preDefined = requirementChecklistEntity.Contains("gsc_predefined")
                ? requirementChecklistEntity.GetAttributeValue<bool>("gsc_predefined")
                : false;

            

            if (message == "Update")
            {
                _organizationService.Delete(requirementChecklistEntity.LogicalName, requirementChecklistEntity.Id);
                _tracingService.Trace("Deleted Requirement Checklist record...");
            }

            else //Delete message
            {
                if (preDefined == true)
                    throw new InvalidPluginExecutionException("Unable to delete required document.");
                else
                    return;
            }


            _tracingService.Trace("Ending ValidateDelete method...");

        }

        public void ValidateEdit(Entity requirementChecklistEntity)
        {
            _tracingService.Trace("Started ValidateEdit Method...");

            bool preDefined = requirementChecklistEntity.Contains("gsc_predefined")
               ? requirementChecklistEntity.GetAttributeValue<bool>("gsc_predefined")
               : false;

            if (preDefined == true)
                throw new InvalidPluginExecutionException("Unable to modify documents from bank.");

            _tracingService.Trace("Ending ValidateEdit Method...");
        }

    }
}
