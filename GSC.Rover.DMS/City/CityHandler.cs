using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.City
{
    public class CityHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;



        public CityHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Raphael Herrera
        /* Purpose:  Common function used to handle duplicate detection when importing entities with city field
         * Registration Details:
         * Event/Message: 
         *      Post/Create: account, contact
         * Primary Entity: Account, Contact
         */
        public Entity SetCity(Entity parentEntity)
        {
            _tracingService.Trace("Started SetCity Method...");

            Entity parentEntityToUpdate = _organizationService.Retrieve(parentEntity.LogicalName, parentEntity.Id, new ColumnSet("gsc_provinceid", "gsc_cityname", "gsc_cityid"));
            var provinceId = parentEntityToUpdate.Contains("gsc_provinceid") ? parentEntity.GetAttributeValue<EntityReference>("gsc_provinceid").Id : Guid.Empty;
            var cityName = parentEntityToUpdate.Contains("gsc_cityname") ? parentEntityToUpdate.GetAttributeValue<String>("gsc_cityname") : String.Empty;

            var contactConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_provinceid", ConditionOperator.Equal, provinceId),
                                new ConditionExpression("gsc_citypn", ConditionOperator.Equal, cityName)
                            };
            EntityCollection cityCollection = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_city", contactConditionList, _organizationService, null, OrderType.Ascending, new[] { "gsc_citypn" });

            _tracingService.Trace("City records retrieved: " + cityCollection.Entities.Count);
            if (cityCollection != null && cityCollection.Entities.Count > 0)
            {
                Entity cityEntity = cityCollection.Entities[0];

                parentEntityToUpdate["gsc_cityid"] = new EntityReference("gsc_cmn_city", cityEntity.Id);

                _organizationService.Update(parentEntityToUpdate);
                _tracingService.Trace("Updated contact entity...");
            }
            _tracingService.Trace("Ending SetCity Method...");
            return parentEntityToUpdate;
        }
    }
}
