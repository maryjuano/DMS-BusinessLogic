using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.Account
{
    public class AccountHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public AccountHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created on: 9/28/2016
        public void PopulateTaxRate(Entity branchEntity, string taxIdField, string rateField)
        {
            var taxId = branchEntity.GetAttributeValue<EntityReference>(taxIdField) != null
                ? branchEntity.GetAttributeValue<EntityReference>(taxIdField).Id
                : Guid.Empty;

            EntityCollection taxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_taxmaintenance", "gsc_cmn_taxmaintenanceid", taxId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_rate" });

            if (taxCollection != null && taxCollection.Entities.Count > 0)
             {
                var taxEntity = taxCollection.Entities[0];

                if(taxEntity.Contains("gsc_rate"))
                {
                     branchEntity[rateField] = taxEntity.GetAttributeValue<Decimal>("gsc_rate");
                }
             }
        }

        //Created By: Leslie Baliguat, Created On: 9/28/2106
        public void PopulateRegion(Entity branchEntity)
        {
            var provinceId = branchEntity.GetAttributeValue<EntityReference>("gsc_provinceId") != null
                ? branchEntity.GetAttributeValue<EntityReference>("gsc_provinceId").Id
                : Guid.Empty;

            EntityCollection regionCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_province", "gsc_cmn_provinceid", provinceId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_regionid" });

            if (regionCollection != null && regionCollection.Entities.Count > 0)
            {
                var regionEntity = regionCollection.Entities[0];

                if (regionEntity.Contains("gsc_regionid"))
                {
                    branchEntity["gsc_regionid"] = regionEntity.GetAttributeValue<Decimal>("gsc_regionid");
                }
            }
        }

        //Created By: Leslie Baliguat, Created On: 9/28/2106
        public void PopulateDealerCode(Entity branchEntity)
        {
            var dealerId = branchEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                ? branchEntity.GetAttributeValue<EntityReference>("gsc_dealerid").Id
                : Guid.Empty;

            EntityCollection regionCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", dealerId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_regionid" });

            if (regionCollection != null && regionCollection.Entities.Count > 0)
            {
                var regionEntity = regionCollection.Entities[0];

                if (regionEntity.Contains("gsc_regionid"))
                {
                    branchEntity["gsc_regionid"] = regionEntity.GetAttributeValue<Decimal>("gsc_regionid");
                }
            }
        }
    }
}
