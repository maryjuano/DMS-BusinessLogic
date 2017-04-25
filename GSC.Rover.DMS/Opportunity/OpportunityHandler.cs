using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Portal.Configuration;

namespace GSC.Rover.DMS.BusinessLogic.Opportunity
{
    public class OpportunityHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public OpportunityHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On: 2/1/2016
        public Entity ReplicateInquiryInfo(Entity opportunityEntity)
        {
            _tracingService.Trace("Started ReplicateInquiryInfo method..");

            if (opportunityEntity.Contains("originatingleadid"))
            {
                var prospectInquiryId = opportunityEntity.GetAttributeValue<EntityReference>("originatingleadid").Id;

                //Retrieve Prospect Inquiry record from Originating Lead field value
                EntityCollection prospectInquiryRecords = CommonHandler.RetrieveRecordsByOneValue("lead", "leadid", prospectInquiryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_salesexecutiveid", "gsc_vehiclebasemodelid", "gsc_colorid", "gsc_leadsourceid", "subject", "gsc_dealerid", "gsc_branchid",
                    "parentaccountid", "parentcontactid", "gsc_dealerid", "gsc_branchid", "gsc_portaluserid", "gsc_paymentmode", "gsc_financingtermid", "gsc_inquiryno" });

                if (prospectInquiryRecords != null && prospectInquiryRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved {" + prospectInquiryRecords.Entities.Count + "}: " + "Retrieving Prospect...");

                    Entity prospectInquiry = prospectInquiryRecords.Entities[0];

                    //Leslie Baliguat added dealer id and branch id, 03/23/2016
                    opportunityEntity["gsc_dealerid"] = prospectInquiry.Contains("gsc_dealerid") 
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("gsc_dealerid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                        : null;
                    opportunityEntity["gsc_branchid"] = prospectInquiry.Contains("gsc_branchid") 
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("gsc_branchid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("gsc_branchid").Id) 
                        : null;
                    opportunityEntity["gsc_recordownerid"] = prospectInquiry.Contains("gsc_portaluserid")
                        ? new EntityReference("contact", new Guid(prospectInquiry.GetAttributeValue<String>("gsc_portaluserid")))
                        : null;
                    opportunityEntity["gsc_salesexecutiveid"] = prospectInquiry.Contains("gsc_salesexecutiveid")
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id) 
                        : null;
                    opportunityEntity["gsc_vehiclebasemodelid"] = prospectInquiry.Contains("gsc_vehiclebasemodelid")
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id) 
                        : null;
                    opportunityEntity["gsc_colorid"] = prospectInquiry.Contains("gsc_colorid") 
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("gsc_colorid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("gsc_colorid").Id) 
                        : null;
                    opportunityEntity["gsc_leadsourceid"] = prospectInquiry.Contains("gsc_leadsourceid") 
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("gsc_leadsourceid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("gsc_leadsourceid").Id) 
                        : null;
                    opportunityEntity["parentcontactid"] = prospectInquiry.Contains("parentcontactid")
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("parentcontactid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("parentcontactid").Id)
                        : null;
                    opportunityEntity["parentaccountid"] = prospectInquiry.Contains("parentaccountid")
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("parentaccountid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("parentaccountid").Id) 
                        : null;
                    opportunityEntity["gsc_topic"] = prospectInquiry.Contains("subject") 
                        ? prospectInquiry.GetAttributeValue<string>("subject") 
                        : string.Empty;
                    opportunityEntity["customerid"] = prospectInquiry.Contains("parentcontactid")
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("parentcontactid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("parentcontactid").Id)
                        : prospectInquiry.Contains("parentaccountid")
                        ? new EntityReference(prospectInquiry.GetAttributeValue<EntityReference>("parentaccountid").LogicalName, prospectInquiry.GetAttributeValue<EntityReference>("parentaccountid").Id)
                        : null;
                    opportunityEntity["gsc_paymentmode"] = prospectInquiry.Contains("gsc_paymentmode")
                        ? prospectInquiry.GetAttributeValue<OptionSetValue>("gsc_paymentmode")
                        : null;
                    opportunityEntity["gsc_financingtermid"] = prospectInquiry.Contains("gsc_financingtermid")
                        ? new EntityReference("gsc_sls_financingterm", prospectInquiry.GetAttributeValue<EntityReference>("gsc_financingtermid").Id)
                        : null;
                    opportunityEntity["gsc_inquiryno"] = prospectInquiry.Contains("gsc_inquiryno")
                        ? prospectInquiry.GetAttributeValue<String>("gsc_inquiryno")
                        : String.Empty;
                }
            }

            _tracingService.Trace("Ended ReplicateInquiryInfo method..");

            return opportunityEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 12/6/2016
        /*Purpose: Replicate prospect inquiry activity on opportunity create.
        * Registration Details:
        * Event/Message: 
        *      Post/Create: Opportunity
        * Primary Entity: Opportunity
        */
        public Entity ReplicateProspectInquiryActivities(Entity opportunityEntity)
        {
            _tracingService.Trace("Started ReplicateProspectInquiryActivities method...");

            Guid prospectInquiryId = opportunityEntity.Contains("originatingleadid")
                ? opportunityEntity.GetAttributeValue<EntityReference>("originatingleadid").Id
                : Guid.Empty;

            EntityCollection prospectActivityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_prospectingactivity", "gsc_prospectinquiryid", prospectInquiryId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_activitytype", "gsc_description", "gsc_recordownerid", "gsc_dealerid", "gsc_branchid", "gsc_date" });

            if (prospectActivityRecords != null && prospectActivityRecords.Entities.Count > 0)
            {
                foreach (Entity prospectActivity in prospectActivityRecords.Entities)
                {
                    Entity opportunityActivity = new Entity("gsc_sls_opportunityactivity");
                    opportunityActivity["gsc_opportunityid"] = new EntityReference("opportunity", opportunityEntity.Id);
                    opportunityActivity["gsc_activitytype"] = prospectActivity.GetAttributeValue<OptionSetValue>("gsc_activitytype") != null
                        ? new OptionSetValue(prospectActivity.GetAttributeValue<OptionSetValue>("gsc_activitytype").Value)
                        : null;
                    opportunityActivity["gsc_date"] = prospectActivity.Contains("gsc_date")
                        ? prospectActivity.GetAttributeValue<DateTime>("gsc_date")
                        : (DateTime?) null;
                    opportunityActivity["gsc_activitydescription"] = prospectActivity.Contains("gsc_description")
                        ? prospectActivity.GetAttributeValue<String>("gsc_description")
                        : String.Empty;
                    opportunityActivity["gsc_recordownerid"] = prospectActivity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                        ? new EntityReference("contact", prospectActivity.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                        : null;
                    opportunityActivity["gsc_dealerid"] = prospectActivity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                        ? new EntityReference("account", prospectActivity.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                        : null;
                    opportunityActivity["gsc_branchid"] = prospectActivity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                        ? new EntityReference("account", prospectActivity.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                        : null;

                    _organizationService.Create(opportunityActivity);
                }
            }

            _tracingService.Trace("Ended ReplicateProspectInquiryActivities method...");
            return opportunityEntity;
        }

        //Created By : Raphael Herrera, Created On: 4/19/2016
        /*Purpose: Set Potential to false
        * Registration Details:
        * Event/Message: 
        *      Post/Win: Opportunity
        * Primary Entity: Opportunity
        */
        public void updateCustomer(Entity opportunityEntity, IOrganizationService service, ITracingService trace)
        {
            trace.Trace("Started update customer method...");

            var opportunityId  = (Object) null;
            
            //handles individual/contacts customers
            if (opportunityEntity.Contains("parentcontactid"))
            {
                trace.Trace("Customer is contact...");

                var parentContactId = opportunityEntity.GetAttributeValue<EntityReference>("parentcontactid") != null
                    ? opportunityEntity.GetAttributeValue<EntityReference>("parentcontactid").Id
                    : Guid.Empty;

                EntityCollection contactEC = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", parentContactId, service, null, OrderType.Ascending,
                    new[] { "contactid", "gsc_ispotential" });

                trace.Trace("Contact records retrieved: " + contactEC.Entities.Count);
                if (contactEC.Entities.Count > 0)
                {
                    Entity contact = contactEC[0];
                    contact["gsc_ispotential"] = false;
                    service.Update(contact);
                    trace.Trace("Record updated...");
                }
            }
            //handles account customers
            else if (opportunityEntity.Contains("parentaccountid"))
            {
                trace.Trace("Customer is account...");
                var parentAccountId = opportunityEntity.GetAttributeValue<EntityReference>("parentaccountid") != null
                    ? opportunityEntity.GetAttributeValue<EntityReference>("parentaccountid").Id
                    : Guid.Empty;

                EntityCollection accountEC = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", parentAccountId , service, null, OrderType.Ascending,
                    new[] { "accountid", "gsc_ispotential" });

                if (accountEC.Entities.Count > 0)
                {
                    Entity account = accountEC[0];
                    account["gsc_ispotential"] = false;
                    service.Update(account);
                    trace.Trace("Record updated...");
                }
            }
            trace.Trace("Ended update customer method...");
        }
         
        //Created By : Raphael Herrera, Created On: 4/20/2016
        public void linkCustomer(Entity opportunityEntity, IOrganizationService service, ITracingService trace)
        {
            trace.Trace("Started update customer method...");

            bool isAccount = false;
            var customerId = (Object) null;

            var leadId = opportunityEntity.GetAttributeValue<EntityReference>("originatingleadid") != null
                ? opportunityEntity.GetAttributeValue<EntityReference>("originatingleadid").Id
                : Guid.Empty;


            if (opportunityEntity.GetAttributeValue<EntityReference>("parentaccountid") != null)
            {
                customerId = opportunityEntity.GetAttributeValue<EntityReference>("parentaccountid").Id;
                isAccount = true;
            }
            else if (opportunityEntity.GetAttributeValue<EntityReference>("parentcontactid") != null)
            {
                customerId = opportunityEntity.GetAttributeValue<EntityReference>("parentcontactid").Id;
            }
            trace.Trace("Getting lead fields...");

             var leadConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("leadid", ConditionOperator.Equal, leadId)
                            };

             EntityCollection leadEC = CommonHandler.RetrieveRecordsByConditions("contact", leadConditionList, service, null, OrderType.Ascending,
                      new[] { "leadid", "parentcontactid", "parentaccountid" });



             if (leadEC.Entities.Count > 0)
             {
                 Entity leadEntity = leadEC[0];
                 if (leadEC.Entities[0].GetAttributeValue<EntityReference>("parentaccountid") == null && isAccount)
                 {
                    leadEntity["parentcontactid"] = customerId;
                    leadEntity["customerid"] = customerId;
                 }
                 else if (leadEC.Entities[0].GetAttributeValue<EntityReference>("parentcontactid") == null && !isAccount)
                 {
                     leadEntity["parentcontactid"] = customerId;
                     leadEntity["customerid"] = customerId;
                 }
                 trace.Trace("Updating lead...");

                 service.Update(leadEntity);
                 
             }

            trace.Trace("Ended update customer method...");
        }

        //Created By: Leslie Baliguat, Created On: 06/15/2016
        /*Purpose: Create Quote Record
         *          Set gsc_isgeneratequote field back to false
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_isgeneratequote
         * Primary Entity: Opportunity
         */
        public void generateQuote(Entity opportunityEntity)
        {
            if (opportunityEntity.GetAttributeValue<int>("gsc_quotecount") > 0)
            {
                _tracingService.Trace("Started generateQuote method.");

                Entity quoteEntity = new Entity("quote");
                quoteEntity["opportunityid"] = new EntityReference(opportunityEntity.LogicalName, opportunityEntity.Id);
                quoteEntity["customerid"] = opportunityEntity.Contains("parentcontactid")
                    ? opportunityEntity.GetAttributeValue<EntityReference>("parentcontactid")
                    : opportunityEntity.Contains("parentaccountid")
                    ? opportunityEntity.GetAttributeValue<EntityReference>("parentaccountid")
                    : null;
                var a = _organizationService.Create(quoteEntity);

                _tracingService.Trace("Quote Created.");
            }
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 9/13/2016
        /*Purpose: Restrict closing of opportunity records with won quotations
         * Registration Details:
         * Event/Message: 
         *      Post/Lost
         * Primary Entity: Opportunity
         */
        public Entity RestrictOpportunityCloseAsLost(Entity opportunityEntity)
        {
            _tracingService.Trace("Started RestrictOpportunityCloseAsLost method..");

            //Return if opportunity status is not lost
            if (opportunityEntity.GetAttributeValue<OptionSetValue>("statecode").Value != 2) { return null; }

            //Retrieve Quote records
            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "opportunityid", opportunityEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "statecode" });

            if (quoteRecords != null && quoteRecords.Entities.Count > 0)
            {
                foreach (Entity quote in quoteRecords.Entities)
                {
                    if (quote.GetAttributeValue<OptionSetValue>("statecode").Value == 2)
                    {
                        throw new InvalidPluginExecutionException("Cannot close Opportunity records as 'Lost' with 'Won' Quotations.");
                    }
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("Record cannot be updated as LOST, there is no associated Quote in the record.");
            }

            _tracingService.Trace("Ended RestrictOpportunityCloseAsLost method..");
            return opportunityEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 9/13/2016
        /*Purpose: Restrict closing of opportunity records with lost quotations
         * Registration Details:
         * Event/Message: 
         *      Post/Lost
         * Primary Entity: Opportunity
         */
        public Entity RestrictOpportunityCloseAsWon(Entity opportunityEntity)
        {
            _tracingService.Trace("Started RestrictOpportunityCloseAsWon method..");

            //Return if opportunity status is not won
            if (opportunityEntity.GetAttributeValue<OptionSetValue>("statecode").Value != 1) { return null; }

            //Retrieve Quote records
            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "opportunityid", opportunityEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "statecode" });

            if (quoteRecords != null && quoteRecords.Entities.Count > 0)
            {
                Boolean isWon = true;

                foreach (Entity quote in quoteRecords.Entities)
                {
                    if (quote.GetAttributeValue<OptionSetValue>("statecode").Value != 3)
                    {
                        isWon = false;
                    }
                }

                if (isWon == true)
                {
                    throw new InvalidPluginExecutionException("Cannot close Opportunity records as 'Won' with only 'Lost' Quotations.");
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("Record cannot be updated as WON, there is no associated Quote in the record.");
            }

            _tracingService.Trace("Ended RestrictOpportunityCloseAsWon method..");
            return opportunityEntity;
        }

        //Created By : Artum M. Ramos, Created On : 12/5/2016
        //Modified By : Jerome Anthony Gerero, Modified On : 12/9/2016
        /*Purpose: Replicate Prospect Information and Create Opportunity form
         * Registration Details:
         * Event/Message: 
         *     Pre-Create
         * Primary Entity: Opportunity
         */
        public Entity ReplicateProspectInfo(Entity opportunityEntity)
        { 
            _tracingService.Trace("Started ReplicateProspectInfo method..");
            string mobileno = null;
            string completeaddress = null;

            String customerLogicalName = opportunityEntity.GetAttributeValue<EntityReference>("customerid") != null
                ? opportunityEntity.GetAttributeValue<EntityReference>("customerid").LogicalName
                : String.Empty;

            var customerId = opportunityEntity.GetAttributeValue<EntityReference>("customerid") != null
                ? opportunityEntity.GetAttributeValue<EntityReference>("customerid").Id
                : Guid.Empty;

            _tracingService.Trace("Check if Customer is contact/Account...");

            //handles contact customer
            if (customerLogicalName.Equals("contact"))
            {
                _tracingService.Trace("Customer is contact...");

                _tracingService.Trace("Retrieve Contact Data");
                EntityCollection contactEC = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", customerId, _organizationService, null, OrderType.Ascending,
                   new[] { "mobilephone", "address1_line1", "gsc_countryid", "gsc_regionid", "gsc_provinceid", "gsc_cityid" });

                _tracingService.Trace("Contact records retrieved...");

                if (contactEC != null && contactEC.Entities.Count > 0)
                {
                    _tracingService.Trace("Contact Data");
                    Entity contact = contactEC.Entities[0];

                    _tracingService.Trace("Contact Mobile");
                    mobileno = contact.Contains("mobilephone")
                        ? contact.GetAttributeValue<String>("mobilephone")
                        : string.Empty;
                    var street = contact.Contains("address1_line1")
                        ? contact.GetAttributeValue<String>("address1_line1")
                        : string.Empty;
                    var country = contact.Contains("gsc_countryid")
                        ? contact.GetAttributeValue<EntityReference>("gsc_countryid").Name
                        : null;
                    var region = contact.Contains("gsc_regionid")
                       ? contact.GetAttributeValue<EntityReference>("gsc_regionid").Name
                       : null;
                    var province = contact.Contains("gsc_provinceid")
                       ? contact.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                       : null;
                    var city = contact.Contains("gsc_cityid")
                       ? contact.GetAttributeValue<EntityReference>("gsc_cityid").Name
                       : null;

                    _tracingService.Trace("Contact Complete Address");
                    completeaddress = string.Concat(street, ", ", city, ", ", province, ", ", country);
                }
            }

            //handles account customers
            else if (customerLogicalName.Equals("account"))
            {
                _tracingService.Trace("Customer is account...");

                _tracingService.Trace("Retrieve Account Data");
                EntityCollection accountEC = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", customerId, _organizationService, null, OrderType.Ascending,
                    new[] { "telephone2", "address1_line1", "gsc_countryid", "gsc_regionid", "gsc_provinceid", "gsc_cityid" });

                _tracingService.Trace("Account records retrieved...");

                if (accountEC != null && accountEC.Entities.Count > 0)
                {
                    _tracingService.Trace("Account Data");

                    Entity account = accountEC.Entities[0];

                    _tracingService.Trace("Account Mobile");
                    mobileno = account.Contains("telephone2")
                        ? account.GetAttributeValue<String>("telephone2")
                        : string.Empty;
                    var street = account.Contains("address1_line1")
                        ? account.GetAttributeValue<String>("address1_line1")
                        : string.Empty;
                    var country = account.Contains("gsc_countryid")
                        ? account.GetAttributeValue<EntityReference>("gsc_countryid").Name
                        : null;
                    var region = account.Contains("gsc_regionid")
                       ? account.GetAttributeValue<EntityReference>("gsc_regionid").Name
                       : null;
                    var province = account.Contains("gsc_provinceid")
                       ? account.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                       : null;
                    var city = account.Contains("gsc_cityid")
                       ? account.GetAttributeValue<EntityReference>("gsc_cityid").Name
                       : null;

                    _tracingService.Trace("Account Complete Address");
                    completeaddress = string.Concat(street, ", ", city, ", ", province, ", ", country);

                }
            }

            Entity opportunityToUpdate = _organizationService.Retrieve(opportunityEntity.LogicalName, opportunityEntity.Id, new ColumnSet("gsc_mobileno", "gsc_completeaddress"));
                    opportunityToUpdate["gsc_mobileno"] = mobileno;
                    opportunityToUpdate["gsc_completeaddress"] = completeaddress;
                    _organizationService.Update(opportunityToUpdate);
                    _tracingService.Trace("Account Record Created...");
                    return opportunityToUpdate;

            //throw new InvalidPluginExecutionException("Test");
            _tracingService.Trace("Ended ReplicateProspectInfo method..");
            return opportunityEntity;
        }

        


        //Created By: Leslie G. Baliguat, Created On: 01/23/14
        /*Purpose: Do not allow created when there is associated quote
         * Registration Details:
         * Event/Message: 
         *      Pre-validate/Delete: 
         * Primary Entity: Order Charge
         */
        public void ValidateDelete(Entity Opportunity)
        {
            var quoteConditionList = new List<ConditionExpression>
               {
                                new ConditionExpression("opportunityid", ConditionOperator.Equal, Opportunity.Id)
                };

            EntityCollection opportunityCollection = CommonHandler.RetrieveRecordsByConditions("quote", quoteConditionList, _organizationService,
                 null, OrderType.Ascending, new[] { "opportunityid" });

            if (opportunityCollection != null && opportunityCollection.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("Cannot delete opportunity record. There is/are associated quote(s).");
            }
        }

    }
}
