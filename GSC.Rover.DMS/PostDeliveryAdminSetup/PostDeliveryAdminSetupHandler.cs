using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.PostDeliveryAdminSetup
{
    public class PostDeliveryAdminSetupHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public PostDeliveryAdminSetupHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 10/04/2016
        /*Purpose: Restrict Create New Record when there is already existing record. One record per branch
        * Registration Details:
        * Event/Message: 
        *      Pre/Create: 
        * Primary Entity: Post-Delivery Administration Setup
        */
        public void RetrictCreateNewRecord(Entity adminSetup)
        {
            var branchId = adminSetup.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? adminSetup.GetAttributeValue<EntityReference>("gsc_branchid").Id
                : Guid.Empty;

            //Retrieve Prospect Inquiry record from Originating Lead field value
            EntityCollection setupCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_postdeliveryadministration", "gsc_branchid", branchId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_postdeliveryadministrationpn" });

            if (setupCollection != null && setupCollection.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("You cannot create this record. Post-Delivery Administration Setup record already exists.");
            }
        }
    }
}
