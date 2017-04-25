using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.TransactedVehicle
{
    public class TransactedVehicleHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public TransactedVehicleHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 12/8/2016
        /* Purpose:  Change Owner of the Transacted Vehicle
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_transfertocorporate, gsc_transfertoindividual
         * Primary Entity: Transacted Vehicle
         */
        public void TransferVehicleOwnership(Entity vehicleEntity, string customerType)
        {
            Entity vehicleToUpdate = _organizationService.Retrieve(vehicleEntity.LogicalName, vehicleEntity.Id,
                new ColumnSet("gsc_corporateid", "gsc_customerid"));

            if (customerType == "account")
            {
                vehicleToUpdate["gsc_corporateid"] = vehicleEntity.GetAttributeValue<EntityReference>("gsc_transfertocorporate") != null
                                                     ? vehicleEntity.GetAttributeValue<EntityReference>("gsc_transfertocorporate")
                                                     : null;
            }
            else //else contact
            {
                vehicleToUpdate["gsc_customerid"] = vehicleEntity.GetAttributeValue<EntityReference>("gsc_transfertoindividual") != null
                                                    ? vehicleEntity.GetAttributeValue<EntityReference>("gsc_transfertoindividual")
                                                    : null;
            }

            _organizationService.Update(vehicleToUpdate);
        }

        //Created By: Leslie Baliguat, Created On: 12/8/2016
        /* Purpose:  Upon Changing Vehicle Ownership, Create Transfer History
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_transfertocorporate, gsc_transfertoindividual
         * Primary Entity: Transacted Vehicle
         */
        public void CreateTransferHistory(Entity vehicleEntity, string customerType)
        {
            Entity transferHistory = new Entity("gsc_vehicleownershiptransfer");

            transferHistory["gsc_transferreddate"] = Convert.ToDateTime(DateTime.Today.ToString("MM-dd-yyyy"));
            transferHistory["gsc_transactedvehicleid"] = new EntityReference(vehicleEntity.LogicalName, vehicleEntity.Id);
            
            if (customerType  == "account")
            {
                transferHistory["gsc_name"] = vehicleEntity.GetAttributeValue<EntityReference>("gsc_corporateid") != null
                    ? vehicleEntity.GetAttributeValue<EntityReference>("gsc_corporateid").Name
                    : String.Empty;
            }
            else  //else contact
            {
                transferHistory["gsc_name"] = vehicleEntity.GetAttributeValue<EntityReference>("gsc_customerid") != null
                    ? vehicleEntity.GetAttributeValue<EntityReference>("gsc_customerid").Name
                    : String.Empty;
            }

            _organizationService.Create(transferHistory);
        }
    }
}
