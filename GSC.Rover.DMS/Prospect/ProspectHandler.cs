using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.Prospect
{
    public class ProspectHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;
      


        public ProspectHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }
       //Create By: Raphael Herrera, Created On: 4/18/2016 /*
       /* Purpose:  Check if a prospect has an existing record that is fraudulent.
        * Registration Details:
        * Event/Message: 
        *      Post/Update: 
        * Primary Entity: Prospect
        */
        public bool CheckForExistingRecords(Entity ProspectEntity)
        {
                _tracingService.Trace("Checking for existing records..");

                if (!ProspectEntity.FormattedValues["gsc_customertype"].Equals("Individual")) { return false; }

               String firstName = ProspectEntity.Contains("gsc_firstname")
                    ? ProspectEntity.GetAttributeValue<string>("gsc_firstname")
                    : String.Empty;
               String lastName = ProspectEntity.Contains("gsc_lastname")
                   ? ProspectEntity.GetAttributeValue<string>("gsc_lastname")
                   : String.Empty;
               DateTime birthDate = ProspectEntity.Contains("gsc_birthday")
                ? ProspectEntity.GetAttributeValue<DateTime>("gsc_birthday")
                : DateTime.MinValue;
               String mobileNo = ProspectEntity.Contains("gsc_mobileno")
                  ? ProspectEntity.GetAttributeValue<string>("gsc_mobileno")
                  : String.Empty;
               String alternateContact = ProspectEntity.Contains("gsc_alternatecontactno")
                 ? ProspectEntity.GetAttributeValue<string>("gsc_alternatecontactno")
                 : String.Empty;

            //check for contact with similar record as prospect
                  var contactConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("firstname", ConditionOperator.Equal, firstName),
                                new ConditionExpression("lastname", ConditionOperator.Equal, lastName),
                                new ConditionExpression("birthdate", ConditionOperator.On, birthDate),
                                new ConditionExpression("mobilephone", ConditionOperator.Equal, mobileNo)
                            };
                 
                      EntityCollection contactEC = CommonHandler.RetrieveRecordsByConditions("contact", contactConditionList, _organizationService, null, OrderType.Ascending,
                           new[] { "contactid", "firstname", "lastname", "birthdate", "mobilephone", "gsc_fraud" });

                      if (contactEC.Entities.Count > 0)
                      {
                          _tracingService.Trace("Existing record found..." + contactEC.Entities.Count.ToString() + "records. Lastname: " + contactEC.Entities[0].GetAttributeValue<string>("lastname").ToString() + contactEC.Entities[0].GetAttributeValue<DateTime>("birthdate").ToString() + contactEC.Entities[0].GetAttributeValue<bool>("gsc_fraud").ToString());
                          //check if record is fraudulent
                          if (contactEC.Entities[0].GetAttributeValue<bool>("gsc_fraud").Equals(true))
                          {
                              _tracingService.Trace("Existing record fraudulent...");
                              return true;
                          }
                      }
                
                 _tracingService.Trace("Ending CheckForExistingRecords method...");
                 return false;
            }
        }

}
