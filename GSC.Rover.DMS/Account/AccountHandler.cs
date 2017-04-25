using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.City;

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
        //Modified By: Raphael Herrera, Modified On: 11/07/2016
        //Modification Purpose: add taxcategory to replicated fields
        public void PopulateTaxRate(Entity branchEntity)
        {
            _tracingService.Trace("Started PopulateTaxRate Method...");

            Entity branchToUpdate = _organizationService.Retrieve(branchEntity.LogicalName, branchEntity.Id,
                new ColumnSet("gsc_taxrate", "gsc_taxtype"));

            var taxId = branchEntity.GetAttributeValue<EntityReference>("gsc_taxid") != null
                ? branchEntity.GetAttributeValue<EntityReference>("gsc_taxid").Id
                : Guid.Empty;

            EntityCollection taxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_taxmaintenance", "gsc_cmn_taxmaintenanceid", taxId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_rate", "gsc_taxcategory" });

            if (taxCollection != null && taxCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Tax Rate");

                var taxEntity = taxCollection.Entities[0];

                var rate = taxEntity.Contains("gsc_rate")
                    ? taxEntity.GetAttributeValue<Double>("gsc_rate")
                    : 0;

                branchToUpdate["gsc_taxrate"] = rate;

                _organizationService.Update(branchToUpdate);

                _tracingService.Trace("Rate Updated.");
            }

            _tracingService.Trace("Ended PopulateTaxRate Method...");
        }

        //Created By: Leslie Baliguat, Created on: 9/28/2016
        public void PopulateWithholdingTaxRate(Entity branchEntity, string taxIdField, string rateField)
        {
            _tracingService.Trace("Started PopulateWithholdingTaxRate Method...");

            Entity branchToUpdate = _organizationService.Retrieve(branchEntity.LogicalName, branchEntity.Id, new ColumnSet(rateField));

            var taxId = branchEntity.GetAttributeValue<EntityReference>(taxIdField) != null
                ? branchEntity.GetAttributeValue<EntityReference>(taxIdField).Id
                : Guid.Empty;

            EntityCollection taxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_withholdingtax", "gsc_cmn_withholdingtaxid", taxId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_rate" });

            if (taxCollection != null && taxCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Wihtholding Tax Rate");

                var taxEntity = taxCollection.Entities[0];

                var rate = taxEntity.Contains("gsc_rate")
                    ? taxEntity.GetAttributeValue<Double>("gsc_rate")
                    : 0;

                branchToUpdate[rateField] = taxEntity.GetAttributeValue<Double>("gsc_rate");

                _organizationService.Update(branchToUpdate);

                _tracingService.Trace("Rate Updated.");
            }

            else
            {
                branchToUpdate[rateField] = null;
                _organizationService.Update(branchToUpdate);

                _tracingService.Trace("Rate Updated.");
            }

            _tracingService.Trace("Ended PopulateWithholdingTaxRate Method...");
        }

        //Created By: Leslie Baliguat, Created On: 9/28/2106
        public void PopulateRegion(Entity branchEntity)
        {
            _tracingService.Trace("Started PopulateRegion Method...");

            var provinceId = branchEntity.GetAttributeValue<EntityReference>("gsc_provinceId") != null
                ? branchEntity.GetAttributeValue<EntityReference>("gsc_provinceId").Id
                : Guid.Empty;

            EntityCollection regionCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_province", "gsc_cmn_provinceid", provinceId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_regionid" });

            if (regionCollection != null && regionCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Region");

                var regionEntity = regionCollection.Entities[0];

                if (regionEntity.Contains("gsc_regionid"))
                {
                    Entity branchToUpdate = _organizationService.Retrieve(branchEntity.LogicalName, branchEntity.Id, new ColumnSet("gsc_regionid"));

                    branchToUpdate["gsc_regionid"] = regionEntity.GetAttributeValue<EntityReference>("gsc_regionid");

                    _organizationService.Update(branchToUpdate);

                    _tracingService.Trace("Region Updated.");
                }
            }
            _tracingService.Trace("Ended PopulateRegion Method...");
        }

        //Created By: Leslie Baliguat, Created On: 9/28/2106
        public void PopulateDealerCode(Entity branchEntity)
        {
            _tracingService.Trace("Started PopulateDealerCode Method...");

            var dealerId = branchEntity.GetAttributeValue<EntityReference>("parentaccountid") != null
                ? branchEntity.GetAttributeValue<EntityReference>("parentaccountid").Id
                : Guid.Empty;

            EntityCollection accountCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", dealerId, _organizationService, null, OrderType.Ascending,
                new[] { "accountnumber" });

            if (accountCollection != null && accountCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Dealer Code");

                var accountEntity = accountCollection.Entities[0];

                if (accountEntity.Contains("accountnumber"))
                {
                    Entity branchToUpdate = _organizationService.Retrieve(branchEntity.LogicalName, branchEntity.Id, new ColumnSet("gsc_dealercode"));

                    branchToUpdate["gsc_dealercode"] = accountEntity.GetAttributeValue<String>("accountnumber");

                    _organizationService.Update(branchToUpdate);

                    _tracingService.Trace("Dealer Code Updated.");
                }
            }
            _tracingService.Trace("Ended PopulateDealerCode Method...");
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

        //Created By: Raphael Herrera, Created On: 4/18/2016 /*
        //Modified By: Leslie Baliguat, Modified On: 11/08/2016
        /* Purpose:  Check if a prospect has an existing record that is fraudulent.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Account
         */
        public Entity CheckForExistingRecords(Entity ProspectEntity)
        {
            var recordtype = ProspectEntity.Contains("gsc_recordtype")
                ? ProspectEntity.GetAttributeValue<OptionSetValue>("gsc_recordtype").Value
                : 0;

            //if (recordtype != 100000003)
            //    return ProspectEntity;

            _tracingService.Trace("Checking for existing records..");

            String companyName = ProspectEntity.Contains("name")
                 ? ProspectEntity.GetAttributeValue<string>("name")
                 : String.Empty;

            //check for account with similar record as prospect/customer
            var contactConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("name", ConditionOperator.Equal, companyName),
                                new ConditionExpression("gsc_recordtype", ConditionOperator.Equal, recordtype)
                            };

            EntityCollection accountEC = CommonHandler.RetrieveRecordsByConditions("account", contactConditionList, _organizationService, null, OrderType.Ascending,
                 new[] { "name", "gsc_fraud"});

            _tracingService.Trace("account records found: " + accountEC.Entities.Count);
            if (accountEC.Entities.Count > 0)
            {
                foreach (Entity accountEntity in accountEC.Entities)
                {
                    if (ProspectEntity.Id != accountEntity.Id)
                    {
                        _tracingService.Trace("Existing record found..." + accountEC.Entities.Count.ToString() + "records. Company Name: " + accountEC.Entities[0].GetAttributeValue<string>("name").ToString() + accountEC.Entities[0].GetAttributeValue<bool>("gsc_fraud").ToString());
                        //check if record is fraudulent
                        if (accountEntity.GetAttributeValue<bool>("gsc_fraud").Equals(true))
                        {
                            throw new InvalidPluginExecutionException("This record has been identified as a fraud account. Please ask the customer to provide further information.");
                        }
                        else
                            throw new InvalidPluginExecutionException("A duplicate error was found. There is an already existing customer record with the same information.");
                    }
                    _tracingService.Trace("Self record retrieved...");
                }
                
            }
            _tracingService.Trace("Ending CheckForExistingRecords method...");
            return ProspectEntity;

        }

        //Created By: Leslie Baliguat
        /* Purpose:  Populate Primary Contact Details in Corporate Customer Form.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: primarycontactid
         * Primary Entity: Account
         */
        public void PopulatePrimaryContactDetails(Entity customerEntity)
        {
            Guid primaryContactId = CommonHandler.GetEntityReferenceIdSafe(customerEntity, "primarycontactid");

            EntityCollection contactCollection = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", primaryContactId, _organizationService, null, OrderType.Ascending,
                new[] { "emailaddress1", "mobilephone", "gsc_contactrelation" });

            if (contactCollection != null && contactCollection.Entities.Count > 0)
            {
                Entity contactPerson = contactCollection.Entities[0];

                Entity customerToUpdate = _organizationService.Retrieve(customerEntity.LogicalName, customerEntity.Id, 
                    new ColumnSet("telephone2", "emailaddress1", "gsc_contactrelation"));

                customerToUpdate["telephone2"] = contactPerson.Contains("mobilephone")
                    ? contactPerson.GetAttributeValue<String>("mobilephone")
                    : String.Empty;
                customerToUpdate["emailaddress1"] = contactPerson.Contains("emailaddress1")
                    ? contactPerson.GetAttributeValue<String>("emailaddress1")
                    : String.Empty;
                customerToUpdate["gsc_contactrelation"] = contactPerson.Contains("gsc_contactrelation")
                    ? contactPerson.GetAttributeValue<String>("gsc_contactrelation")
                    : String.Empty;

                _organizationService.Update(customerToUpdate);

            }
        }

        public void ValidatePrimaryContact(Entity customerEntity)
        {
            var primaryContactId = customerEntity.GetAttributeValue<EntityReference>("primarycontactid") != null
                ? customerEntity.GetAttributeValue<EntityReference>("primarycontactid").Id
                : Guid.Empty;

            if(primaryContactId != Guid.Empty)
            {
                var contactConditionList = new List<ConditionExpression>
                                {
                                    new ConditionExpression("parentcustomerid", ConditionOperator.Equal, customerEntity.Id),
                                    new ConditionExpression("gsc_recordtype", ConditionOperator.Equal, 100000003)
                                };

                EntityCollection contactCollection = CommonHandler.RetrieveRecordsByConditions("contact", contactConditionList, _organizationService, null, OrderType.Ascending,
                     new[] { "contactid"});


                if (contactCollection != null && contactCollection.Entities.Count > 0)
                {
                }
                else
                {
                    throw new InvalidPluginExecutionException(String.Concat("Primary Contact does not exist as contact person of this account."));
                }
            }
        }

        public void CreateContactPerson(Entity accountEntity)
        {
            Entity contact = new Entity("contact");

            contact["parentcustomerid"] = new EntityReference("account", accountEntity.Id);
            contact["firstname"] = accountEntity.Contains("gsc_firstname")
                ? accountEntity.GetAttributeValue<String>("gsc_firstname")
                : null;
            contact["middlename"] = accountEntity.Contains("gsc_middlename")
                ? accountEntity.GetAttributeValue<String>("gsc_middlename")
                : null;
            contact["lastname"] = accountEntity.Contains("gsc_lastname")
                ? accountEntity.GetAttributeValue<String>("gsc_lastname")
                : null;
            contact["mobilephone"] = accountEntity.Contains("telephone2")
                ? accountEntity.GetAttributeValue<String>("telephone2")
                : null;
            contact["emailaddress1"] = accountEntity.Contains("emailaddress1")
                ? accountEntity.GetAttributeValue<String>("emailaddress1")
                : null;
            contact["gsc_recordownerid"] = accountEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                ? accountEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                : null;
            contact["gsc_dealerid"] = accountEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                ? accountEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;
            contact["gsc_branchid"] = accountEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? accountEntity.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;
            contact["gsc_recordtype"] = new OptionSetValue(100000003);
            contact["gsc_ispotential"] = false;
            contact["gsc_prospect"] = true;

            var contactId = _organizationService.Create(contact);

            _tracingService.Trace("Primary Contact Created");

            Entity accountToUpdate = _organizationService.Retrieve(accountEntity.LogicalName, accountEntity.Id,
                new ColumnSet("primarycontactid"));

            accountToUpdate["primarycontactid"] = new EntityReference("contact", contactId);
            _organizationService.Update(accountToUpdate);
        }

        public Boolean IsUsedInTransaction(Entity accountEntity)
        {
            _tracingService.Trace("IsUsedInTransaction Method Started");

            EntityCollection opportunityEC = CommonHandler.RetrieveRecordsByOneValue("opportunity", "customerid", accountEntity.Id, _organizationService, null, OrderType.Ascending,
                 new[] { "customerid" });

            EntityCollection quoteEC = CommonHandler.RetrieveRecordsByOneValue("quote", "customerid", accountEntity.Id, _organizationService, null, OrderType.Ascending,
                 new[] { "customerid" });

            if (opportunityEC.Entities.Count > 0 || quoteEC.Entities.Count > 0)
            {
                return true;
            }           
            _tracingService.Trace("IsUsedInTransaction Method Ended");
            return false;
        }

        public Entity SetCity(Entity accountEntity)
        {
            CityHandler cityHandler = new CityHandler(_organizationService, _tracingService);
            return cityHandler.SetCity(accountEntity);
        }
    }

}
