using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.Insurance
{
    public class InsuranceCoverageHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public InsuranceCoverageHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie G. Baliguat, Created On: 11/10/2016
        public void ComputeTotalPremium(Entity insuranceCoverage, String message)
        {
            _tracingService.Trace("Started ComputeTotalPremium method..");

            var insurnaceId = insuranceCoverage.GetAttributeValue<EntityReference>("gsc_insuranceid") != null
                ? insuranceCoverage.GetAttributeValue<EntityReference>("gsc_insuranceid").Id
                : Guid.Empty;

            var totalPremium = Decimal.Zero;

            EntityCollection coverageRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_insurancecoverage", "gsc_insuranceid", insurnaceId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_premium" });

            if (coverageRecords != null && coverageRecords.Entities.Count > 0)
            {
                foreach (var coverageEntity in coverageRecords.Entities)
                {
                    totalPremium += coverageEntity.Contains("gsc_premium")
                        ? coverageEntity.GetAttributeValue<Money>("gsc_premium").Value
                        : Decimal.Zero;
                }
            }

            if (insuranceCoverage.Contains("gsc_premium") && message.Equals("Delete"))
            {
                totalPremium = totalPremium - insuranceCoverage.GetAttributeValue<Money>("gsc_premium").Value;
            }

            Entity insurancetoUpdate = _organizationService.Retrieve("gsc_cmn_insurance", insurnaceId, new ColumnSet("gsc_totalpremium"));
            insurancetoUpdate["gsc_totalpremium"] = new Money(Convert.ToDecimal(totalPremium));
            _organizationService.Update(insurancetoUpdate);

            _tracingService.Trace("Ended ComputeTotalPremium method..");

        }
    }
}
