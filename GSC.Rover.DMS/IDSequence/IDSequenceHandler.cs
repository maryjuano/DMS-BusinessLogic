using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.IDSequence
{
    public class IDSequenceHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public IDSequenceHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 8/10/2016
        public void SequenceNumberPadding(Entity sequenceEntity)
        {
            Entity sequenceToUpdate = _organizationService.Retrieve(sequenceEntity.LogicalName, sequenceEntity.Id,
                new ColumnSet("gsc_numberpadding", "gsc_sequencenumber"));

            var padding = sequenceToUpdate.Contains("gsc_numberpadding")
                ? sequenceToUpdate.FormattedValues["gsc_numberpadding"]
                : String.Empty;

            var sequenceNoString = "0";
            sequenceNoString = sequenceNoString.PadLeft(Convert.ToInt32(padding), '0');

            sequenceToUpdate["gsc_sequencenumber"] = Convert.ToInt32(sequenceNoString);

            _organizationService.Update(sequenceToUpdate);
        }
        //Created By: Jessica Casupanan, Created On: 01/27/2017
        public Boolean IsUsedInTransaction(Entity IDSequenceEntity)
        {
            _tracingService.Trace("IsUsedInTransaction Method Started");
            if (IDSequenceEntity.Contains("gsc_lastsequencenumber"))
            { 
                return true; 
            }
            _tracingService.Trace("IsUsedInTransaction Method Ended");
                return false;
        }
    }
}
