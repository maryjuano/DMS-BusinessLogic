using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;


namespace GSC.Rover.DMS.BusinessLogic.RequirementsChecklist
{
    public class RequirementsChecklistHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public RequirementsChecklistHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        
    }
}
