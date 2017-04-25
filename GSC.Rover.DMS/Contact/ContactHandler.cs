using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.City;

namespace GSC.Rover.DMS.BusinessLogic.Contact
{
    public class ContactHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ContactHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        public void ChangeThemeUrl(Entity currentEntity)
        {
            _tracingService.Trace("Started Changing Theme Url");

            Guid theme = currentEntity.GetAttributeValue<EntityReference>("gsc_themes") != null ? currentEntity.GetAttributeValue<EntityReference>("gsc_themes").Id : Guid.Empty;
            //_tracingService.Trace("Guid of theme is " + theme.ToString());
            //if (theme == Guid.Empty) return;

            _tracingService.Trace("Modify Contact Record's theme url and perform Update");

            currentEntity["gsc_themeurl"] = _organizationService.Retrieve("adx_webfile", theme, new ColumnSet("adx_partialurl"))["adx_partialurl"].ToString();
            //currentEntity["gsc_themeurl"] = "turtletheme";

            _organizationService.Update(currentEntity);
            _tracingService.Trace("Ended Changing Theme Url");
        }

        //Created By: Leslie Baliguat, Created on: 9/28/2016
        public void PopulateTaxRate(Entity customerEntity)
        {
            _tracingService.Trace("Started PopulateTaxRate Method...");

            Entity customerToUpdate = _organizationService.Retrieve(customerEntity.LogicalName, customerEntity.Id, new ColumnSet("gsc_taxrate"));

            var taxId = customerEntity.GetAttributeValue<EntityReference>("gsc_taxid") != null
                ? customerEntity.GetAttributeValue<EntityReference>("gsc_taxid").Id
                : Guid.Empty;

            EntityCollection taxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_taxmaintenance", "gsc_cmn_taxmaintenanceid", taxId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_rate" });

            if (taxCollection != null && taxCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Tax Rate");

                var taxEntity = taxCollection.Entities[0];

                var rate = taxEntity.Contains("gsc_rate")
                    ? taxEntity.GetAttributeValue<Double>("gsc_rate")
                    : 0;

                customerToUpdate["gsc_taxrate"] = rate;

                _organizationService.Update(customerToUpdate);

                _tracingService.Trace("Rate Updated.");
            }

            _tracingService.Trace("Ended PopulateTaxRate Method...");
        }

        //Created By: Leslie Baliguat, Created On: 10/04/2016
        public void ReplicateAddresstoBillingAddress(Entity customerEntity)
        {
            var sameAddress = customerEntity.GetAttributeValue<Boolean>("gsc_sametopermanentaddress");

            if (sameAddress)
            {
                Entity customerToUpdate = _organizationService.Retrieve(customerEntity.LogicalName, customerEntity.Id, new ColumnSet("address1_postalcode",
                    "address1_line1", "gsc_countryid", "gsc_provinceid", "gsc_cityid"));

                customerToUpdate["address2_postalcode"] = customerEntity.Contains("address1_postalcode")
                    ? customerEntity.GetAttributeValue<String>("address1_postalcode")
                    : String.Empty;
                customerToUpdate["address2_line1"] = customerEntity.Contains("address1_line1")
                    ? customerEntity.GetAttributeValue<String>("address1_line1")
                    : String.Empty;
                customerToUpdate["gsc_countrybillingid"] = customerEntity.GetAttributeValue<EntityReference>("gsc_countryid") != null
                    ? customerEntity.GetAttributeValue<EntityReference>("gsc_countryid")
                    : null;
                customerToUpdate["gsc_provincebillingid"] = customerEntity.GetAttributeValue<EntityReference>("gsc_provinceid") != null
                    ? customerEntity.GetAttributeValue<EntityReference>("gsc_provinceid")
                    : null;
                customerToUpdate["gsc_citybillingid"] = customerEntity.GetAttributeValue<EntityReference>("gsc_cityid") != null
                    ? customerEntity.GetAttributeValue<EntityReference>("gsc_cityid")
                    : null;

                _organizationService.Update(customerToUpdate);
            }
        }

        //Created By : Jerome Anthony Gerero, Created On : 10/20/2016
        /*Purpose: Generate employee credentials on new employee record.
         * Registration Details: 
         * Event/Message:
         *      Post-operation/Create
         * Primary Entity: Contact
         */
        public Entity GenerateEmployeeCredentials(Entity contactEntity)
        {
            _tracingService.Trace("Started GenerateCredentials method..");

            if (!contactEntity.Contains("gsc_recordtype")) { return null; }

            if (!contactEntity.FormattedValues["gsc_recordtype"].Equals("Employee")) { return null; }

            CreateUsernamePassword(contactEntity);

            _tracingService.Trace("Ended GenerateCredentials method..");
            return contactEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 10/14/2016
        //Purpose : Generate username and random 8-character password
        private Entity CreateUsernamePassword(Entity contactEntity)
        {
            _tracingService.Trace("Started CreateUsernamePassword method..");

            String lastname = contactEntity.Contains("lastname")
                ? contactEntity.GetAttributeValue<String>("lastname").ToLower().Replace(" ", String.Empty)
                : String.Empty;
            String firstname = contactEntity.Contains("firstname")
                ? contactEntity.GetAttributeValue<String>("firstname").ToLower()
                : String.Empty;
            Int32 counter = 1;
            String username = String.Empty;
            String password = RandomString(8);

            username = firstname[0] + lastname + "-" + String.Concat(counter).PadLeft(3, '0');

            //Retrieve contacts with duplicate usernames then increment counter value
            EntityCollection contactRecords = CommonHandler.RetrieveRecordsByOneValue("contact", "adx_identity_username", username, _organizationService, "createdon", OrderType.Descending,
                new[] { "adx_identity_username" });

            if (contactRecords != null && contactRecords.Entities.Count > 0)
            {
                Entity contact = contactRecords.Entities[0];

                String incrementUsername = contact.Contains("adx_identity_username")
                    ? contact.GetAttributeValue<String>("adx_identity_username")
                    : String.Empty;

                counter = Convert.ToInt32(incrementUsername.Substring(incrementUsername.Length - 3)) + 1;

                username = firstname[0] + lastname + "-" + String.Concat(counter).PadLeft(3, '0');
            }

            contactEntity["adx_logonenabled"] = true;
            contactEntity["adx_identity_lockoutenabled"] = true;
            contactEntity["adx_identity_username"] = username;
            contactEntity["adx_password"] = password;

            _tracingService.Trace("Ended CreateUsernamePassword method..");
            return contactEntity;
        }

        private static Random random = new Random();

        private static string RandomString(Int32 length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //Create By: Raphael Herrera, Created On: 4/18/2016 /*
        //Modified By: Leslie Baliguat, Modified On: 11/08/2016
        /* Purpose:  Check if a prospect has an existing record that is fraudulent.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Contact
         */
        public Entity CheckForExistingRecords(Entity ProspectEntity)
        {
            _tracingService.Trace("Started CheckForExistingRecords Method");
            var recordtype = ProspectEntity.Contains("gsc_recordtype")
                ? ProspectEntity.GetAttributeValue<OptionSetValue>("gsc_recordtype").Value
                : 0;

            //if (recordtype != 100000001)
            //{
            //    _tracingService.Trace("Not individual...");
            //    return ProspectEntity;
            //}

            _tracingService.Trace("Checking for existing records..");

            String firstName = ProspectEntity.Contains("firstname")
                 ? ProspectEntity.GetAttributeValue<string>("firstname")
                 : String.Empty;
            String lastName = ProspectEntity.Contains("lastname")
                ? ProspectEntity.GetAttributeValue<string>("lastname")
                : String.Empty;
            String birthDate = ProspectEntity.Contains("birthdate")
             ? ProspectEntity.GetAttributeValue<DateTime>("birthdate").AddHours(8).ToShortDateString()
             : String.Empty;

            String mobileNo = ProspectEntity.Contains("mobilephone")
               ? ProspectEntity.GetAttributeValue<string>("mobilephone")
               : String.Empty;

            //check for contact with similar record as prospect
            var contactConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("firstname", ConditionOperator.Equal, firstName),
                                new ConditionExpression("lastname", ConditionOperator.Equal, lastName),
                                new ConditionExpression("mobilephone", ConditionOperator.Equal, mobileNo),
                                new ConditionExpression("birthdate", ConditionOperator.Equal, birthDate),
                                new ConditionExpression("gsc_recordtype", ConditionOperator.Equal, recordtype),
                                new ConditionExpression("contactid", ConditionOperator.NotEqual, ProspectEntity.Id)
                            };
            _tracingService.Trace("fn: " + firstName + " >LN: " + lastName + " >BD: " + birthDate + " >M#: " + mobileNo);
            EntityCollection contactEC = CommonHandler.RetrieveRecordsByConditions("contact", contactConditionList, _organizationService, null, OrderType.Ascending,
                 new[] { "contactid", "firstname", "lastname", "birthdate", "mobilephone", "gsc_fraud", "gsc_ispotential" });

            _tracingService.Trace("contact records found: " + contactEC.Entities.Count);
            if (contactEC.Entities.Count > 0)
            {
                foreach (Entity contactEntity in contactEC.Entities)
                {
                    _tracingService.Trace("Existing record found Lastname: " + contactEntity.GetAttributeValue<string>("lastname").ToString() + contactEntity.GetAttributeValue<DateTime>("birthdate").ToString() + contactEntity.GetAttributeValue<bool>("gsc_fraud").ToString());
                   
                    //check if record is fraudulent
                    if (contactEntity.GetAttributeValue<bool>("gsc_fraud").Equals(true))
                    {
                        throw new InvalidPluginExecutionException("This record has been identified as a fraud account. Please ask the customer to provide further information.");
                    }
                    else
                    {
                        if (contactEC.Entities[0].GetAttributeValue<bool>("gsc_ispotential").Equals(true))
                            throw new InvalidPluginExecutionException("A duplicate error was found. There is an already existing prospect record with the same information.");
                        else

                            throw new InvalidPluginExecutionException("A duplicate error was found. There is an already existing customer record with the same information.");
                    }

                    _tracingService.Trace("Self record retrieved...");
                }

            }

            _tracingService.Trace("Ending CheckForExistingRecords method...");
            return ProspectEntity;

        }

        //Created By: Leslie Baliguat, Created On: 12/07/2016
        /* Purpose:  Update Primary Contact Details in Corporate Customer Form 
         * on update of Contact Person details in Contact Person Form.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: mobilephone, emailaddress1, gsc_customerrelation
         * Primary Entity: Contact
         */
        public void UpdatePrimaryContactDetails(Entity contactPerson)
        {
            Entity primaryContact = _organizationService.Retrieve(contactPerson.LogicalName, contactPerson.Id, new ColumnSet("parentcustomerid"));
            var primaryContactId = CommonHandler.GetEntityReferenceIdSafe(primaryContact, "parentcustomerid");

            EntityCollection accountCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", primaryContactId, _organizationService, null, OrderType.Ascending,
                new[] { "telephone2", "emailaddress1", "gsc_contactrelation", "primarycontactid" });

            if (accountCollection != null && accountCollection.Entities.Count > 0)
            {
                Entity companyEntity = accountCollection.Entities[0];


                if (contactPerson.Id == CommonHandler.GetEntityReferenceIdSafe(companyEntity, "primarycontactid"))
                {
                    companyEntity["telephone2"] = contactPerson.Contains("mobilephone")
                        ? contactPerson.GetAttributeValue<String>("mobilephone")
                        : String.Empty;
                    companyEntity["emailaddress1"] = contactPerson.Contains("emailaddress1")
                        ? contactPerson.GetAttributeValue<String>("emailaddress1")
                        : String.Empty;
                    companyEntity["gsc_contactrelation"] = contactPerson.Contains("gsc_contactrelation")
                        ? contactPerson.GetAttributeValue<String>("gsc_contactrelation")
                        : String.Empty;

                    _organizationService.Update(companyEntity);
                }
            }
        }

        //Created By: Leslie Baliguat, Created On: 12/07/2016
        /* Purpose:  Clear Primary Contact Details in Corporate Customer Form 
         * on delete of Contact Person details in Contact Person Form.
         * Registration Details:
         * Event/Message: 
         *      PreValidation/Delete: 
         * Primary Entity: Contact
         */
        public void ClearPrimaryContactDetails(Entity contactPerson)
        {
            Entity primaryContact = _organizationService.Retrieve(contactPerson.LogicalName, contactPerson.Id, new ColumnSet("parentcustomerid"));
            var primaryContactId = CommonHandler.GetEntityReferenceIdSafe(primaryContact, "parentcustomerid");

            EntityCollection accountCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", primaryContactId, _organizationService, null, OrderType.Ascending,
                new[] { "telephone2", "emailaddress1", "gsc_contactrelation", "primarycontactid" });

            if (accountCollection != null && accountCollection.Entities.Count > 0)
            {
                Entity companyEntity = accountCollection.Entities[0];

                if (contactPerson.Id == CommonHandler.GetEntityReferenceIdSafe(companyEntity, "primarycontactid"))
                {
                    companyEntity["telephone2"] = String.Empty;
                    companyEntity["emailaddress1"] = String.Empty;
                    companyEntity["gsc_contactrelation"] = String.Empty;

                    _organizationService.Update(companyEntity);
                }
            }
        }

        public void ConcatenateAddress(Entity contactPerson)
        {
            if (!contactPerson.Contains("gsc_recordtype")) { return; }

            if (!contactPerson.FormattedValues["gsc_recordtype"].Equals("Contact Person")) { return; }

            var street = contactPerson.Contains("address1_line1")
                ? contactPerson.GetAttributeValue<String>("address1_line1")
                : string.Empty;
            var country = contactPerson.Contains("gsc_countryid")
                ? contactPerson.GetAttributeValue<EntityReference>("gsc_countryid").Name
                : null;
            var province = contactPerson.Contains("gsc_provinceid")
               ? contactPerson.GetAttributeValue<EntityReference>("gsc_provinceid").Name
               : null;
            var city = contactPerson.Contains("gsc_cityid")
               ? contactPerson.GetAttributeValue<EntityReference>("gsc_cityid").Name
               : null;

            _tracingService.Trace("Account Complete Address");

            Entity accountToUpdate = _organizationService.Retrieve(contactPerson.LogicalName, contactPerson.Id,
                new ColumnSet("address1_composite"));

            accountToUpdate["address1_composite"] = string.Concat(street, ", ", city, ", ", province, ", ", country);
            _organizationService.Update(accountToUpdate);
        }

        public Boolean IsUsedInTransaction(Entity contactEntity)
        {
            _tracingService.Trace("IsUsedInTransaction Method Started");

            EntityCollection opportunityEC = CommonHandler.RetrieveRecordsByOneValue("opportunity", "customerid", contactEntity.Id, _organizationService, null, OrderType.Ascending,
                 new[] { "customerid" });

            EntityCollection quoteEC = CommonHandler.RetrieveRecordsByOneValue("quote", "customerid", contactEntity.Id, _organizationService, null, OrderType.Ascending,
                 new[] { "customerid" });

            EntityCollection salesOrderEC = CommonHandler.RetrieveRecordsByOneValue("salesorder", "customerid", contactEntity.Id, _organizationService, null, OrderType.Ascending,
                 new[] { "customerid" });

            if (opportunityEC.Entities.Count > 0 || quoteEC.Entities.Count > 0 || salesOrderEC.Entities.Count > 0)
            {
                return true;
            }
            _tracingService.Trace("IsUsedInTransaction Method Ended");
            return false;
        }

        public Entity SetCity(Entity contactEntity)
        {
            CityHandler cityHandler = new CityHandler(_organizationService, _tracingService);
            return cityHandler.SetCity(contactEntity);
        }

        public Entity SetAccessLevel(Entity contactEntity)
        {
            _tracingService.Trace("Started SetAccessLevel Method...");
            contactEntity["parentcustomerid"] = contactEntity["gsc_branchid"];
            _tracingService.Trace("Ending SetAccessLevel Method...");

            return contactEntity;
        }
    }
}
