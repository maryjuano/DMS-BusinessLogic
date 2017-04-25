using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.ChargesItem
{
    public class ChargesItemHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ChargesItemHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        public void PopulateBaseModelDetails(Entity chargesItem)
        {
            if (chargesItem.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null)
            {
                _tracingService.Trace("Started PopulateBaseModelDetails method..");

                var baseModelId = chargesItem.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                    ? chargesItem.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id
                    : Guid.Empty;

                EntityCollection baseModelRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclebasemodel", "gsc_iv_vehiclebasemodelid", baseModelId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_vehicletypeid","gsc_bodytypeid" });

                if (baseModelRecords != null && baseModelRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Base Model Details");

                    var baseModel = baseModelRecords.Entities[0];

                    Entity itemToUpdate = _organizationService.Retrieve(chargesItem.LogicalName, chargesItem.Id, new ColumnSet("gsc_vehicletypeid", "gsc_bodytypeid"));
                    itemToUpdate["gsc_vehicletypeid"] = baseModel.GetAttributeValue<EntityReference>("gsc_vehicletypeid");
                    itemToUpdate["gsc_bodytypeid"] = baseModel.GetAttributeValue<EntityReference>("gsc_bodytypeid");
                    _organizationService.Update(itemToUpdate);
                }

                _tracingService.Trace("Ended PopulateBaseModelDetails method..");

            }
        }

    }
}
