using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.ApproverSetup
{
    public class ApproverSetupHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ApproverSetupHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie G. Baliguat, Created On: 01/25/2017
        public void RestrictDuplicateSetup(Entity approver)
        {
            _tracingService.Trace("Started RestrictDuplicateSetup method...");

            var setupConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_branchid", ConditionOperator.Equal, CommonHandler.GetEntityReferenceIdSafe(approver, "gsc_branchid")),
                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                };

            EntityCollection approverSetupCollection = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_approversetup", setupConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_cmn_approversetupid" });

            _tracingService.Trace("Branch: " + CommonHandler.GetEntityReferenceIdSafe(approver, "gsc_branchid").ToString());
            _tracingService.Trace("Retrieve Records: " + approverSetupCollection.Entities.Count.ToString());

            if (approverSetupCollection != null && approverSetupCollection.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("There is already an existing approver setup for this branch.");
            }

            _tracingService.Trace("Ended RestrictDuplicateSetup method...");
        }
    }
}
