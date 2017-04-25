using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace GSC.Rover.DMS.BusinessLogic.VehicleClaim
{
    public class VehicleClaimHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleClaimHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/23/2016
        /*Purpose: Deactivate Claimed Vehicle Claims
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Claimed? = gsc_claimed
         *      Post/Create:
         * Primary Entity: Vehicle Claim
         */
        public Entity DeactivateVehicleClaim(Entity vehicleClaimEntity)
        {
            _tracingService.Trace("Started DeactivateVehicleClaim method..");

            if (vehicleClaimEntity != null && vehicleClaimEntity.GetAttributeValue<OptionSetValue>("statecode").Value == 0 && vehicleClaimEntity.GetAttributeValue<Boolean>("gsc_claimed") == true)
            {
                SetStateRequest setStateRequest = new SetStateRequest()
                {
                    EntityMoniker = new EntityReference
                    {
                        Id = vehicleClaimEntity.Id,
                        LogicalName = vehicleClaimEntity.LogicalName
                    },
                    State = new OptionSetValue(1),
                    Status = new OptionSetValue(2)
                };
                _organizationService.Execute(setStateRequest);
            }
            _tracingService.Trace("Ended DeactivateVehicleClaim method..");
            return vehicleClaimEntity;
        }

        //Created By: Leslie Baliguat, Created On: 9/22/2016
        public void UpdateStatus(Entity vehicleClaimEntity)
        {
            _tracingService.Trace("Update Status.");

            Entity claimtoUpdate = _organizationService.Retrieve(vehicleClaimEntity.LogicalName, vehicleClaimEntity.Id,
                new ColumnSet("gsc_statuscopy"));

            claimtoUpdate["gsc_status"] = vehicleClaimEntity.Contains("gsc_statuscopy")
                ? vehicleClaimEntity.GetAttributeValue<OptionSetValue>("gsc_statuscopy")
                : null;

            _organizationService.Update(claimtoUpdate);

            _tracingService.Trace("Status Updated.");
        }
    }
}
