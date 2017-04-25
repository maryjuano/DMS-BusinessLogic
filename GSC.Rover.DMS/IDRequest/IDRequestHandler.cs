using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.IDRequest
{
    public class IDRequestHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public IDRequestHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 8/2/2016
        public void GenerateName(Entity requesEntity)
        {
            _tracingService.Trace("Started GenerateName Method");
           
            var recordId = requesEntity.Contains("gsc_originatingrecordid")
                ? requesEntity.GetAttributeValue<String>("gsc_originatingrecordid")
                : String.Empty;
            var recordType = requesEntity.Contains("gsc_originatingrecordtype")
                ? requesEntity.GetAttributeValue<String>("gsc_originatingrecordtype")
                : String.Empty;

            _tracingService.Trace("Retrieved Request Details");

            Entity requestToUpdate = _organizationService.Retrieve(requesEntity.LogicalName, requesEntity.Id, new ColumnSet("gsc_idrequestpn", "gsc_branchid"));

            var branch = requestToUpdate.GetAttributeValue<EntityReference>("gsc_branchid") != null
               ? requestToUpdate.GetAttributeValue<EntityReference>("gsc_branchid").Name
               : String.Empty;

            requestToUpdate["gsc_idrequestpn"] = branch + "-" + recordId + "-" + recordType;
            _organizationService.Update(requestToUpdate);

            _tracingService.Trace("Name Updated");

            _tracingService.Trace("Ended GenerateName Method");
        }
    }
}
