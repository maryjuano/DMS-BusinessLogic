using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.City;

namespace GSC.Rover.DMS.BusinessLogic.ProspectInquiry
{
    public class ProspectInquiryHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public ProspectInquiryHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //TO BE DELETED
        // Created By: Leslie Baliguat, Created On: 1/28/2016 
        /*Purpose: Replicate Prospect Information to Prospect Inquiry 
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         * Primary Entity: Prospect Inquiry
         */
        public Entity ReplicateProspectInfo(Entity prospectInquiryEntity)
        {
            _tracingService.Trace("Started ReplicateProspectInfo method...");

            if (prospectInquiryEntity.Contains("gsc_prospectid") && prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_prospectid") != null)
            {
                var prospectId = prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_prospectid").Id;

                // Retrieve Prospect Record based on ProspectId
                EntityCollection prospectRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_prospect", "gsc_sls_prospectid",
                    prospectId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_firstname", "gsc_middlename", "gsc_lastname", "gsc_mobileno", "gsc_emailaddress", 
                    "gsc_street", "gsc_cityid", "gsc_provinceid", "gsc_countryid"});

                if (prospectRecords != null && prospectRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved {" + prospectRecords.Entities.Count + "}: " + "Retrieving Prospect...");

                    Entity prospect = prospectRecords.Entities[0];

                    var firstname = prospect.Contains("gsc_firstname")
                        ? prospect.GetAttributeValue<string>("gsc_firstname")
                        : string.Empty;
                    var lastname = prospect.Contains("gsc_lastname")
                        ? prospect.GetAttributeValue<string>("gsc_lastname")
                        : string.Empty;

                    prospectInquiryEntity["firstname"] = firstname;
                    prospectInquiryEntity["lastname"] = lastname;
                    prospectInquiryEntity["fullname"] = firstname + " " + lastname;
                    prospectInquiryEntity["middlename"] = prospect.Contains("gsc_middlename")
                        ? prospect.GetAttributeValue<string>("gsc_middlename")
                        : string.Empty;
                    prospectInquiryEntity["mobilephone"] = prospect.Contains("gsc_mobileno")
                        ? prospect.GetAttributeValue<string>("gsc_mobileno")
                        : string.Empty;
                    prospectInquiryEntity["emailaddress1"] = prospect.Contains("gsc_emailaddress")
                        ? prospect.GetAttributeValue<string>("gsc_emailaddress")
                        : string.Empty;
                    prospectInquiryEntity["address1_line1"] = prospect.Contains("gsc_street")
                        ? prospect.GetAttributeValue<string>("gsc_street")
                        : string.Empty;
                    prospectInquiryEntity["address1_city"] = prospect.Contains("gsc_cityid")
                        ? prospect.GetAttributeValue<EntityReference>("gsc_cityid").Name
                        : string.Empty;
                    prospectInquiryEntity["address1_stateorprovince"] = prospect.Contains("gsc_provinceid")
                        ? prospect.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                        : string.Empty;
                    prospectInquiryEntity["address1_country"] = prospect.Contains("gsc_countryid")
                        ? prospect.GetAttributeValue<EntityReference>("gsc_countryid").Name
                        : string.Empty;
                }
            }
            _tracingService.Trace("Ended ReplicateProspectInfo method...");

            return prospectInquiryEntity;
        }

        // Created By: Leslie Baliguat, Created On: 1/28/2016
        /*Purpose: Concatenate Full Name and Base Model as value for Subject
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update: Base Model Id
         *                   FullName (firstname, lastname)   
         * Primary Entity: Prospect Inquiry
         */
        public String ConcatenateVehicleInfo(Entity prospectInquiryEntity, string message)
        {
            _tracingService.Trace("Started ConcatenateVehicleInfo method...");

            //retrieve Base Model Name in Vehicle Base Model Entity
            if (prospectInquiryEntity.Contains("gsc_vehiclebasemodelid"))
            {
                var parentProductId = prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                    ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id
                    : Guid.Empty;

                EntityCollection productRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_vehiclebasemodel", "gsc_iv_vehiclebasemodelid",
                parentProductId, _organizationService, null, OrderType.Ascending, new[] { "gsc_basemodelpn" });

                if (productRecords != null && productRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved {" + productRecords.Entities.Count + "}: " + "Retrieving Prospect...");

                    Entity product = productRecords.Entities[0];

                    var basemodel = product.Contains("gsc_basemodelpn")
                        ? product["gsc_basemodelpn"]
                        : string.Empty;
                    var fullname = prospectInquiryEntity.Contains("fullname")
                        ? prospectInquiryEntity["fullname"]
                        : string.Empty;

                    prospectInquiryEntity["subject"] = fullname + " - " + basemodel;

                    if (message == "Update")
                    {
                        _tracingService.Trace("Updating Record ...");

                        Entity prospectInquirytoUpdate = _organizationService.Retrieve(prospectInquiryEntity.LogicalName, prospectInquiryEntity.Id, new ColumnSet("subject"));
                        prospectInquirytoUpdate["subject"] = fullname + " - " + basemodel;

                        _organizationService.Update(prospectInquirytoUpdate);

                        return prospectInquirytoUpdate["subject"].ToString();
                    }
                }
            }

            _tracingService.Trace("Ended ConcatenateVehicleInfo method...");

            return prospectInquiryEntity["subject"].ToString();
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/31/2016
        /*Purpose: Set Prospect Inquiry Disqualified Status Reason
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update: Disqualified (Two Options)
         *                   Disqualified Status Reason (Optionset)
         * Primary Entity: Prospect Inquiry
         */
        public Entity DisqualifyProspectInquiry(Entity prospectInquiryEntity)
        {
            _tracingService.Trace("Started DisqualifyProspectInquiry method...");

            if (prospectInquiryEntity.GetAttributeValue<Int32>("gsc_disqualifiedstatusreason") == 0 || prospectInquiryEntity.GetAttributeValue<Boolean>("gsc_disqualified") == false)
            {
                return null;
            }

            SetStateRequest request = new SetStateRequest
            {
                EntityMoniker = new EntityReference("lead", prospectInquiryEntity.Id),
                State = new OptionSetValue(2),
                Status = new OptionSetValue(prospectInquiryEntity.GetAttributeValue<Int32>("gsc_disqualifiedstatusreason"))
            };

            _organizationService.Execute(request);

            _tracingService.Trace("Ended DisqualifyProspectInquiry method...");
            return prospectInquiryEntity;
        }


        //Created By : Leslie Baliguat, Created On : 4/15/2016
        /*Purpose: Set Prospect Inquiry StateCode Qualified
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update: Qualified (Two Options)
         * Primary Entity: Prospect Inquiry
         */
        public Entity QualifyProspectInquiry(Entity prospectInquiryEntity)
        {
            _tracingService.Trace("Started QualifyProspectInquiry method...");

            if (prospectInquiryEntity.GetAttributeValue<Boolean>("gsc_qualified") == true)
            {
                SetStateRequest request = new SetStateRequest
                {
                    EntityMoniker = new EntityReference("lead", prospectInquiryEntity.Id),
                    State = new OptionSetValue(1),
                    Status = new OptionSetValue(3)
                };

                _organizationService.Execute(request);

                _tracingService.Trace("Ended QualifyProspectInquiry method...");
            }

            return prospectInquiryEntity;
        }

        //Created y: Leslie G. Baliguat, Created On: 4/18/2016
        //Modified By : Jerome Anthony Gerero, Modified on : 11/9/2016
        /*Purpose: Create Contact/Account once qualified base on customertype when contact/account doesn't exist yet
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update: Qualified (Two Options)
         * Primary Entity: Prospect Inquiry
         */
        public Entity CreateCustomer(Entity prospectInquiryEntity)
        {
            _tracingService.Trace("Started CreateCustomer method...");

            //check if lead is already qualified
            if (prospectInquiryEntity.GetAttributeValue<Boolean>("gsc_qualified") == true)
            {
                _tracingService.Trace("Qualify Prospect Inquiry");

                var prospectType = prospectInquiryEntity.Contains("gsc_prospecttype") ?
                    prospectInquiryEntity.GetAttributeValue<OptionSetValue>("gsc_prospecttype").Value
                    : 0;

                if (prospectType == 100000000) //individual
                {
                    //Create Contact Record
                    var contactEntityId = CreateCustomerIndividual(prospectInquiryEntity);

                    //Relate created contact to lead through parentcontactid field
                    prospectInquiryEntity["parentcontactid"] = new EntityReference("contact", contactEntityId);
                    prospectInquiryEntity["customerid"] = new EntityReference("contact", contactEntityId);

                    _organizationService.Update(prospectInquiryEntity);

                    _tracingService.Trace("parentcontactid Updated...");

                    _tracingService.Trace("Ended CreateCustomer method...");

                    return prospectInquiryEntity;
                }
                else if (prospectType == 100000001 || prospectType == 100000002) //corporate or government
                {
                    //Create Account Record
                    var accountEntityId = CreateCorporateCustomer(prospectInquiryEntity);

                    //Relate created account to lead through parentaccountid field
                    prospectInquiryEntity["parentaccountid"] = new EntityReference("account", accountEntityId);
                    prospectInquiryEntity["customerid"] = new EntityReference("account", accountEntityId);

                    _organizationService.Update(prospectInquiryEntity);

                    _tracingService.Trace("parentaccountid Updated...");

                    _tracingService.Trace("Ended CreateCustomer method...");

                    return prospectInquiryEntity;
                }
            }

            _tracingService.Trace("Ended CreateCustomer method...");
            return null;
        }

        private Guid CreateCustomerIndividual(Entity prospectInquiryEntity)
        {
            _tracingService.Trace("Started CreateCustomerIndividual method...");

            Entity contact = new Entity("contact");

            contact["firstname"] = prospectInquiryEntity.Contains("firstname")
                ? prospectInquiryEntity.GetAttributeValue<String>("firstname")
                : null;
            contact["middlename"] = prospectInquiryEntity.Contains("middlename")
                ? prospectInquiryEntity.GetAttributeValue<String>("middlename")
                : null;
            contact["lastname"] = prospectInquiryEntity.Contains("lastname")
                ? prospectInquiryEntity.GetAttributeValue<String>("lastname")
                : null;
            contact["mobilephone"] = prospectInquiryEntity.Contains("mobilephone")
                ? prospectInquiryEntity.GetAttributeValue<String>("mobilephone")
                : null;
            contact["telephone1"] = prospectInquiryEntity.Contains("gsc_alternatecontactno")
                ? prospectInquiryEntity.GetAttributeValue<String>("gsc_alternatecontactno")
                : null;
            contact["fax"] = prospectInquiryEntity.Contains("fax")
                ? prospectInquiryEntity.GetAttributeValue<String>("fax")
                : null;
            contact["emailaddress1"] = prospectInquiryEntity.Contains("emailaddress1")
                ? prospectInquiryEntity.GetAttributeValue<String>("emailaddress1")
                : null;
            contact["gendercode"] = prospectInquiryEntity.Contains("gsc_gender")
                ? prospectInquiryEntity.GetAttributeValue<OptionSetValue>("gsc_gender")
                : null;
            contact["familystatuscode"] = prospectInquiryEntity.Contains("gsc_maritalstatus")
                ? prospectInquiryEntity.GetAttributeValue<OptionSetValue>("gsc_maritalstatus")
                : null;
            contact["birthdate"] = prospectInquiryEntity.Contains("gsc_birthday")
                ? prospectInquiryEntity.GetAttributeValue<DateTime>("gsc_birthday")
                : (DateTime?)null;
            contact["gsc_age"] = prospectInquiryEntity.Contains("gsc_age")
                ? Convert.ToInt32(prospectInquiryEntity.GetAttributeValue<String>("gsc_age"))
                : 0;
            contact["gsc_countryid"] = prospectInquiryEntity.Contains("gsc_countryid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_countryid")
                : null;
            contact["gsc_regionid"] = prospectInquiryEntity.Contains("gsc_regionid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_regionid")
                : null;
            contact["gsc_provinceid"] = prospectInquiryEntity.Contains("gsc_provinceid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_provinceid")
                : null;
            contact["gsc_cityid"] = prospectInquiryEntity.Contains("gsc_cityid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_cityid")
                : null;
            contact["address1_line1"] = prospectInquiryEntity.Contains("address1_line1")
                ? prospectInquiryEntity.GetAttributeValue<String>("address1_line1")
                : null;
            contact["address1_postalcode"] = prospectInquiryEntity.Contains("address1_postalcode")
                ? prospectInquiryEntity.GetAttributeValue<String>("address1_postalcode")
                : null;
            contact["gsc_salesexecutiveid"] = prospectInquiryEntity.Contains("gsc_salesexecutiveid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid")
                : null;
            contact["gsc_dealerid"] = prospectInquiryEntity.Contains("gsc_dealerid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;
            contact["gsc_branchid"] = prospectInquiryEntity.Contains("gsc_branchid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;
            contact["gsc_recordownerid"] = prospectInquiryEntity.Contains("gsc_recordownerid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                : null;
            contact["gsc_recordtype"] = new OptionSetValue(100000001);
            contact["gsc_ispotential"] = true;
            contact["gsc_prospect"] = true;
            Entity DefaultCustomerTax = GetDefaultTax();
            contact["gsc_taxid"] = new EntityReference(DefaultCustomerTax.LogicalName, DefaultCustomerTax.Id);

            _tracingService.Trace("Customer Record Created...");
            _tracingService.Trace("Ended CreateCustomerIndividual method...");

            var contactid = _organizationService.Create(contact);
            return contactid;
        }

        private Guid CreateCorporateCustomer(Entity prospectInquiryEntity)
        {
            _tracingService.Trace("Started CreateCorporateCustomer method...");

            _tracingService.Trace("Creating Corporate Customer ...");
            Entity account = new Entity("account");

            account["gsc_customertype"] = prospectInquiryEntity.Contains("gsc_prospecttype")
                ? new OptionSetValue(prospectInquiryEntity.GetAttributeValue<OptionSetValue>("gsc_prospecttype").Value - 1)
                : null;
            account["gsc_firstname"] = prospectInquiryEntity.Contains("firstname")
                 ? prospectInquiryEntity.GetAttributeValue<String>("firstname")
                 : null;
            account["gsc_middlename"] = prospectInquiryEntity.Contains("middlename")
                ? prospectInquiryEntity.GetAttributeValue<String>("middlename")
                : null;
            account["gsc_lastname"] = prospectInquiryEntity.Contains("lastname")
                ? prospectInquiryEntity.GetAttributeValue<String>("lastname")
                : null;
            account["telephone2"] = prospectInquiryEntity.Contains("mobilephone")
                ? prospectInquiryEntity.GetAttributeValue<String>("mobilephone")
                : null;
            account["telephone3"] = prospectInquiryEntity.Contains("gsc_alternatecontactno")
                ? prospectInquiryEntity.GetAttributeValue<String>("gsc_alternatecontactno")
                : null;
            account["fax"] = prospectInquiryEntity.Contains("fax")
                ? prospectInquiryEntity.GetAttributeValue<String>("fax")
                : null;
            account["emailaddress1"] = prospectInquiryEntity.Contains("emailaddress1")
                ? prospectInquiryEntity.GetAttributeValue<String>("emailaddress1")
                : null;
            account["name"] = prospectInquiryEntity.Contains("companyname")
                ? prospectInquiryEntity.GetAttributeValue<String>("companyname")
                : null;
            account["telephone1"] = prospectInquiryEntity.Contains("telephone1")
                ? prospectInquiryEntity.GetAttributeValue<String>("telephone1")
                : null;
            account["fax"] = prospectInquiryEntity.Contains("fax")
                ? prospectInquiryEntity.GetAttributeValue<String>("fax")
                : null;
            account["websiteurl"] = prospectInquiryEntity.Contains("websiteurl")
                ? prospectInquiryEntity.GetAttributeValue<String>("websiteurl")
                : null;
            account["gsc_countryid"] = prospectInquiryEntity.Contains("gsc_countryid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_countryid")
                : null;
            account["gsc_regionid"] = prospectInquiryEntity.Contains("gsc_regionid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_regionid")
                : null;
            account["gsc_provinceid"] = prospectInquiryEntity.Contains("gsc_provinceid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_provinceid")
                : null;
            account["gsc_cityid"] = prospectInquiryEntity.Contains("gsc_cityid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_cityid")
                : null;
            account["address1_line1"] = prospectInquiryEntity.Contains("address1_line1")
                ? prospectInquiryEntity.GetAttributeValue<String>("address1_line1")
                : null;
            account["address1_postalcode"] = prospectInquiryEntity.Contains("address1_postalcode")
                ? prospectInquiryEntity.GetAttributeValue<String>("address1_postalcode")
                : null;
            account["gsc_salesexecutiveid"] = prospectInquiryEntity.Contains("gsc_salesexecutiveid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid")
                : null;
            account["gsc_dealerid"] = prospectInquiryEntity.Contains("gsc_dealerid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;
            account["gsc_branchid"] = prospectInquiryEntity.Contains("gsc_branchid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;
            account["gsc_recordownerid"] = prospectInquiryEntity.Contains("gsc_recordownerid")
                ? prospectInquiryEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                : null;
            account["gsc_recordtype"] = new OptionSetValue(100000003);
            account["gsc_ispotential"] = true;
            account["gsc_prospect"] = true;
            Entity DefaultCustomerTax = GetDefaultTax();
            account["gsc_taxid"] = new EntityReference(DefaultCustomerTax.LogicalName, DefaultCustomerTax.Id);

            var accountid = _organizationService.Create(account);

            _tracingService.Trace("Corporate Customer Created ...");
            _tracingService.Trace("Ended CreateCorporateCustomer method...");

            return accountid;
        }

        //Created By: Leslie Baliguat, Created On: 4/19/16
        /*Purpose: Create Opportunity Record once Prospect Inquiry is Qualified
        * Registration Details:
        * Event/Message: 
        *      Pre/Create: 
        *      Post/Update: Qualified (Two Options)
        * Primary Entity: Prospect Inquiry
        */
        public Entity CreateOpportunity(Entity prospectinquiryEntity)
        {
            _tracingService.Trace("Started CreateOpportunity method...");

            //check if lead is already qualified
            if (prospectinquiryEntity.GetAttributeValue<Boolean>("gsc_qualified") == true)
            {
                //Relate lead to opportunity through originatingleadid field
                Entity opportunityEntity = new Entity("opportunity");

                opportunityEntity["originatingleadid"] = new EntityReference(prospectinquiryEntity.LogicalName, prospectinquiryEntity.Id);

                var qualifyingOpportunityId = _organizationService.Create(opportunityEntity);

                _tracingService.Trace("Ended CreateOpportunity method...");

                //Relate created opportunity to lead through qualifyingopportunityid field
                Entity inquiryToUpdate = _organizationService.Retrieve(prospectinquiryEntity.LogicalName, prospectinquiryEntity.Id, new ColumnSet("qualifyingopportunityid"));

                inquiryToUpdate["qualifyingopportunityid"] = new EntityReference("opportunity", qualifyingOpportunityId);

                _organizationService.Update(inquiryToUpdate);

                _tracingService.Trace("qualifyingopportunityid Updated...");

                return opportunityEntity;
            }

            _tracingService.Trace("Ended CreateOpportunity method...");

            return null;
        }

        //Create By: Jessica Casupanan, Created On: 12/06/2016 /*
        /* Purpose:  Check if the customer information has an existing record that is fraudulent.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Prospect Inquiry
         */
        public bool CheckForExistingRecords(Entity ProspectInquiryEntity)
        {
            _tracingService.Trace("Checking for existing records..");

            String firstName = ProspectInquiryEntity.Contains("firstname")
                 ? ProspectInquiryEntity.GetAttributeValue<string>("firstname")
                 : String.Empty;
            String lastName = ProspectInquiryEntity.Contains("lastname")
                ? ProspectInquiryEntity.GetAttributeValue<string>("lastname")
                : String.Empty;
            String birthDate = ProspectInquiryEntity.Contains("gsc_birthday")
             ? ProspectInquiryEntity.GetAttributeValue<DateTime>("gsc_birthday").AddHours(8).ToShortDateString()
             : String.Empty;
            String mobileNo = ProspectInquiryEntity.Contains("mobilephone")
               ? ProspectInquiryEntity.GetAttributeValue<string>("mobilephone")
               : String.Empty;
            String companyName = ProspectInquiryEntity.Contains("companyname")
               ? ProspectInquiryEntity.GetAttributeValue<string>("companyname")
               : String.Empty;

            if (ProspectInquiryEntity.FormattedValues["gsc_prospecttype"].Equals("Individual"))
            {
                _tracingService.Trace("Individual");

                //check for contact with similar record as prospect inquiry
                var contactConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("firstname", ConditionOperator.Equal, firstName),
                                new ConditionExpression("lastname", ConditionOperator.Equal, lastName),
                                new ConditionExpression("birthdate", ConditionOperator.Equal, birthDate),
                                new ConditionExpression("mobilephone", ConditionOperator.Equal, mobileNo),
                                new ConditionExpression("gsc_recordtype", ConditionOperator.Equal, 100000001)
                            };

                EntityCollection contactEC = CommonHandler.RetrieveRecordsByConditions("contact", contactConditionList, _organizationService, null, OrderType.Ascending,
                     new[] { "contactid", "firstname", "lastname", "birthdate", "mobilephone", "gsc_fraud", "gsc_ispotential" });

                _tracingService.Trace(String.Concat(contactEC.Entities.Count));

                if (contactEC.Entities.Count > 0)
                {
                    _tracingService.Trace("Existing record found..." + contactEC.Entities.Count.ToString() + "records. Lastname: " + contactEC.Entities[0].GetAttributeValue<string>("lastname").ToString() + contactEC.Entities[0].GetAttributeValue<DateTime>("birthdate").ToString() + contactEC.Entities[0].GetAttributeValue<bool>("gsc_fraud").ToString());
                    //check if record is fraudulent
                    if (contactEC.Entities[0].GetAttributeValue<bool>("gsc_fraud").Equals(true))
                    {
                        _tracingService.Trace("Existing record fraudulent...");
                        return true;
                    }
                    else
                    {
                        if (contactEC.Entities[0].GetAttributeValue<bool>("gsc_ispotential").Equals(true))
                            throw new InvalidPluginExecutionException("A duplicate error was found. There is an already existing prospect record with the same information.");
                        else

                            throw new InvalidPluginExecutionException("A duplicate error was found. There is an already existing customer record with the same information.");
                    }
                }
            }
            else
            {
                _tracingService.Trace("Account");

                EntityCollection accountCollection = CommonHandler.RetrieveRecordsByOneValue("account", "name", companyName, _organizationService, null, OrderType.Ascending,
             new[] { "gsc_ispotential", "gsc_fraud" });

                if (accountCollection != null && accountCollection.Entities.Count > 0)
                {
                    if (accountCollection.Entities[0].GetAttributeValue<bool>("gsc_fraud").Equals(true))
                    {
                        _tracingService.Trace("Existing record fraudulent...");
                        return true;
                    }
                    else
                    {
                        if (accountCollection.Entities[0].GetAttributeValue<bool>("gsc_ispotential").Equals(true))
                            throw new InvalidPluginExecutionException("A duplicate error was found. There is an already existing prospect account with the same company name.");
                        else
                            throw new InvalidPluginExecutionException("A duplicate error was found. There is an already existing corporate account with the same company name.");
                    }
                }
            }

            _tracingService.Trace("Ending CheckForExistingRecords method...");
            // throw new InvalidPluginExecutionException("test");
            return false;
        }

        public Entity GetDefaultTax()
        {
            _tracingService.Trace("Started GetDefaultTax Method...");

            EntityCollection taxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_taxmaintenance", "gsc_isdefault", true, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_isdefault", "gsc_cmn_taxmaintenanceid", });

            _tracingService.Trace("Tax Collection Records Retrieved: " + taxCollection.Entities.Count);
            if (taxCollection.Entities.Count > 0)
            {
                return taxCollection.Entities[0];
            }

            else
                throw new InvalidPluginExecutionException("No Default Tax Maintenance Set.");

        }
    
        //Created By: Jerome Anthony Gerero, Created On: 3/31/2017
        /* Purpose: Restrict delete of prospect inquiry records with related opportunities.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Prospect Inquiry
         */
        public Entity RestrictProspectInquiryDelete(Entity prospectInquiryEntity)
        {
            _tracingService.Trace("Started RestrictProspectInquiryDelete Method...");            

            if (prospectInquiryEntity.GetAttributeValue<OptionSetValue>("statecode").Value == 1)
            {
                throw new InvalidPluginExecutionException("Cannot delete qualified prospect inquiries.");
            }

            _tracingService.Trace("Ended RestrictProspectInquiryDelete Method...");
            return prospectInquiryEntity;
        }

        public Entity SetCity(Entity prospectInquiryEntity)
        {
            CityHandler cityHandler = new CityHandler(_organizationService, _tracingService);
            return cityHandler.SetCity(prospectInquiryEntity);
        }
    }
}
