using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.VehicleColor
{
    public class VehicleColorHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehicleColorHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Artum M. Ramos, Created On: 2/10/2017
        /*Purpose: import Vehicle Color
          * Registration Details: 
          * Event/Message:
          *      Pre/Create: Color Code
          *      Post/Update: 
          * Primary Entity: Vehicle Color
          */
        public Entity OnImportVehicleColor(Entity vehicleColor)
        {
            if (vehicleColor.Contains("gsc_colorid"))
            {
                if (vehicleColor.GetAttributeValue<EntityReference>("gsc_colorid") != null)
                    return null;
            }
            _tracingService.Trace("Started OnImportVehicleColor method..");

            var colorCode = vehicleColor.Contains("gsc_colorcode")
                ? vehicleColor.GetAttributeValue<String>("gsc_colorcode")
                : String.Empty;

            EntityCollection colorCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_color", "gsc_colorcode", colorCode, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_colorpn" });

            _tracingService.Trace("Check if color Collection is Null");
            if (colorCollection != null && colorCollection.Entities.Count > 0)
            {
                Entity color = colorCollection.Entities[0];
                var colorName = color.Contains("gsc_colorpn")
                ? color.GetAttributeValue<String>("gsc_colorpn")
                : String.Empty;

                vehicleColor["gsc_vehiclecolorpn"] = colorName;
                vehicleColor["gsc_colorid"] = new EntityReference("gsc_iv_color", color.Id);
                    
                _tracingService.Trace("Ended OnImportVehicleColor Method...");
                return vehicleColor;
            }
            else
            {
                throw new InvalidPluginExecutionException("The Color Code doesn't exist.");
            }
            
        }

        public Entity PopulateColorCode(Entity vehicleColor)
        {
            var colorCode = vehicleColor.Contains("gsc_colorcode")
                ? vehicleColor.GetAttributeValue<String>("gsc_colorcode")
                : String.Empty;

            if (colorCode != String.Empty)
            {
                    return null;
            }
            
            var colorId = vehicleColor.GetAttributeValue<EntityReference>("gsc_colorid") != null
                ? vehicleColor.GetAttributeValue<EntityReference>("gsc_colorid").Id
                : Guid.Empty;

            EntityCollection colorCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_color", "gsc_iv_colorid", colorId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_colorcode" });

            if (colorCollection != null && colorCollection.Entities.Count > 0)
            {
                Entity color = colorCollection.Entities[0];

                var colorName = color.Contains("gsc_colorcode")
                ? color.GetAttributeValue<String>("gsc_colorcode")
                : String.Empty;

                vehicleColor["gsc_colorcode"] = colorName;

            }

            return vehicleColor;
        }

        //Create By: Leslie Baliguat, Created On: 4/17/2017 /*
        /* Purpose:  Restrict adding of color in vehicle model that is already been added.
         * Registration Details:
         * Event/Message: 
         *      Pre-Create:
         * Primary Entity:  Vehicle Color
         */
        public bool IsColorDuplicate(Entity color)
        {
            var productId = CommonHandler.GetEntityReferenceIdSafe(color, "gsc_productid");
            var itemId = CommonHandler.GetEntityReferenceIdSafe(color, "gsc_colorid");

            var productConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                                new ConditionExpression("gsc_colorid", ConditionOperator.Equal, itemId)
                            };

            EntityCollection colorCollection = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_vehiclecolor", productConditionList, _organizationService, null, OrderType.Ascending,
                     new[] { "gsc_productid" });

            if (colorCollection != null && colorCollection.Entities.Count > 0)
            {
                return true;
            }

            return false;
        }
    }
}
