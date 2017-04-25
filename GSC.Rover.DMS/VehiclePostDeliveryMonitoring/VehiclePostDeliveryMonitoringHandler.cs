using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;
using System.Globalization;


namespace GSC.Rover.DMS.BusinessLogic.VehiclePostDeliveryMonitoring
{
    public class VehiclePostDeliveryMonitoringHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehiclePostDeliveryMonitoringHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 6/3/2016
        /*Purpose: Delete survey questions on Survey Setup field change
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: Survey = gsc_surveysetupid
         *      Post/Create:
         * Primary Entity: Vehicle Post-Delivery Monitoring
         */
        public Entity DeleteExistingSurveyQuestions(Entity vehiclePostDeliveryMonitoringEntity)
        {
            _tracingService.Trace("Started DeleteExistingSurveyQuestions method..");

            EntityCollection surveyTransactionRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_surveytransactions", "gsc_postdeliverymonitoringid", vehiclePostDeliveryMonitoringEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_cmn_surveytransactionsid" });

            if (surveyTransactionRecords != null && surveyTransactionRecords.Entities.Count > 0)
            {
                foreach (Entity surveyTransaction in surveyTransactionRecords.Entities)
                {
                    _organizationService.Delete(surveyTransaction.LogicalName, surveyTransaction.Id);
                }
            }

            //Call ReplicateSurveyQuestions method..
            _tracingService.Trace("Calling ReplicateSurveyQuestions method..");
            ReplicateSurveyQuestions(vehiclePostDeliveryMonitoringEntity);

            _tracingService.Trace("Ended DeleteExistingSurveyQuestions method..");
            return null;
        }

        //Created By : Jerome Anthony Gerero, Created On : 6/3/2016
        /*Purpose: Create survey questions taken from Survey field
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: Survey = gsc_surveysetupid
         *      Post/Create: Survey = gsc_surveysetupid
         * Primary Entity: Vehicle Post-Delivery Monitoring
         */
        public Entity ReplicateSurveyQuestions(Entity vehiclePostDeliveryMonitoringEntity)
        {
            _tracingService.Trace("Started ReplicateSurveyQuestions method..");

            var surveySetupId = vehiclePostDeliveryMonitoringEntity.GetAttributeValue<EntityReference>("gsc_surveysetupid") != null
                ? vehiclePostDeliveryMonitoringEntity.GetAttributeValue<EntityReference>("gsc_surveysetupid").Id
                : Guid.Empty;

            EntityCollection vehicleSurveyQuestionsSetupRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_surveyquestionssetup", "gsc_surveysetupid", surveySetupId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_surveyquestionssetuppn", "gsc_question", "gsc_answertype" });

            if (vehicleSurveyQuestionsSetupRecords != null && vehicleSurveyQuestionsSetupRecords.Entities.Count > 0)
            {
                foreach (Entity vehicleSurveyQuestionsSetup in vehicleSurveyQuestionsSetupRecords.Entities)
                {
                    Entity surveyTransaction = new Entity("gsc_cmn_surveytransactions");
                    surveyTransaction["gsc_surveytransactionpn"] = vehicleSurveyQuestionsSetup.GetAttributeValue<String>("gsc_surveyquestionssetuppn") != null
                        ? vehicleSurveyQuestionsSetup.GetAttributeValue<String>("gsc_surveyquestionssetuppn")
                        : String.Empty;
                    surveyTransaction["gsc_postdeliverymonitoringid"] = new EntityReference(vehiclePostDeliveryMonitoringEntity.LogicalName, vehiclePostDeliveryMonitoringEntity.Id);
                    surveyTransaction["gsc_question"] = vehicleSurveyQuestionsSetup.GetAttributeValue<String>("gsc_question") != null
                        ? vehicleSurveyQuestionsSetup.GetAttributeValue<String>("gsc_question")
                        : String.Empty;
                    surveyTransaction["gsc_answertype"] = vehicleSurveyQuestionsSetup.GetAttributeValue<OptionSetValue>("gsc_answertype") != null
                        ? new OptionSetValue(vehicleSurveyQuestionsSetup.GetAttributeValue<OptionSetValue>("gsc_answertype").Value)
                        : null;
                    _organizationService.Create(surveyTransaction);
                }
            }

            _tracingService.Trace("Ended ReplicateSurveyQuestions method..");
            return vehiclePostDeliveryMonitoringEntity;
        }


        //Created By : Raphael Herrera, Created On : 6/6/2016
        /*Purpose: Create Post Delivery Monitoring Record Given Sales Invoice
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create: gsc_invoiceid = invoiceid
         * Primary Entity: Vehicle Post-Delivery Monitoring
         */
        public Entity CreatePostDeliveryMonitoringRecord(Entity salesInvoiceEntity)
        {

                _tracingService.Trace("Started CreatePostDeliveryMonitoringRecord method..");
                Entity vehiclePostDeliveryMonitoringEntity = new Entity("gsc_sls_vehiclepostdeliverymonitoring");

                var branchId = salesInvoiceEntity.Contains("gsc_branchsiteid") ? salesInvoiceEntity.GetAttributeValue<EntityReference>("gsc_branchsiteid").Id
                    : Guid.Empty;

                EntityCollection postDeliveryMonitoringAdminsitration = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_postdeliveryadministration", "gsc_branchsiteid", branchId, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_daysaftersales" });
                _tracingService.Trace("Retrieved Post Delivery Monitoring Administration Setup..");

                vehiclePostDeliveryMonitoringEntity["gsc_customer"] = salesInvoiceEntity.Contains("gsc_customer") ? salesInvoiceEntity.GetAttributeValue<EntityReference>("gsc_customer")
                    : null;
                vehiclePostDeliveryMonitoringEntity["gsc_invoiceid"] = salesInvoiceEntity.Contains("invoiceid") ? salesInvoiceEntity.Id
                    : Guid.Empty;
                vehiclePostDeliveryMonitoringEntity["gsc_modeldescription"] = salesInvoiceEntity.Contains("gsc_modeldescription") ? salesInvoiceEntity.GetAttributeValue<string>("gsc_modeldescription")
                    : String.Empty;
                vehiclePostDeliveryMonitoringEntity["gsc_csno"] = salesInvoiceEntity.Contains("gsc_csno") ? salesInvoiceEntity.GetAttributeValue<string>("gsc_csno")
                    : String.Empty;
                vehiclePostDeliveryMonitoringEntity["gsc_branchsiteid"] = salesInvoiceEntity.Contains("gsc_branchsiteid") ? salesInvoiceEntity.GetAttributeValue<EntityReference>("gsc_branchsiteid")
                    : null;
                vehiclePostDeliveryMonitoringEntity["gsc_salesexecutiveid"] = salesInvoiceEntity.Contains("gsc_salesexecutiveid") ? salesInvoiceEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid")
                    : null;
                vehiclePostDeliveryMonitoringEntity["gsc_dealerid"] = salesInvoiceEntity.Contains("gsc_dealerid") ? salesInvoiceEntity.GetAttributeValue<EntityReference>("gsc_dealerid")
                    : null;
                vehiclePostDeliveryMonitoringEntity["gsc_recordownerid"] = salesInvoiceEntity.Contains("gsc_recordownerid") ? salesInvoiceEntity.GetAttributeValue<EntityReference>("gsc_recordownerid")
                    : null;
                vehiclePostDeliveryMonitoringEntity["gsc_releaseddate"] = salesInvoiceEntity.Contains("gsc_releaseddate") ? salesInvoiceEntity.GetAttributeValue<DateTime>("gsc_releaseddate")
                    : (DateTime?)null;

                if (postDeliveryMonitoringAdminsitration.Entities.Count > 0)
                {
                    float afterSalesDays = float.Parse(postDeliveryMonitoringAdminsitration.Entities[0].GetAttributeValue<string>("gsc_daysaftersales"), CultureInfo.InvariantCulture.NumberFormat);
                    DateTime expectedCallDate = DateTime.UtcNow.AddDays(afterSalesDays);

                    _tracingService.Trace("Expected Call Date - " + afterSalesDays.ToString() + " " + expectedCallDate.Date.ToString());

                    vehiclePostDeliveryMonitoringEntity["gsc_expectedcalldate"] = expectedCallDate.Date;
                }
                _tracingService.Trace("Completed field assignments... Creating Record..");
                _organizationService.Create(vehiclePostDeliveryMonitoringEntity);
                
                _tracingService.Trace("Ended CreatePostDeliveryMonitoringRecord method..");
                return vehiclePostDeliveryMonitoringEntity;

        }

        //Created By : Raphael Herrera, Created On : 6/7/2016
        /*Purpose: Create Survey Transaction Records based on Survey Setup 
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create: 
         * Primary Entity: Survey Transaction
         */
        public void CreateSurveyTransaction(Entity vehiclePostDeliveryMonitoring)
        {
            _tracingService.Trace("Started CreateSurveyTransaction Method...");
            var branchId = vehiclePostDeliveryMonitoring.Contains("gsc_branchsiteid") ? vehiclePostDeliveryMonitoring.GetAttributeValue<EntityReference>("gsc_branchsiteid").Id
                : Guid.Empty;

            EntityCollection surveySetupCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_surveysetup", "gsc_branchsiteid", branchId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_cmn_surveysetupid" });
            _tracingService.Trace("Retrieved SurveySetup...");

            if (surveySetupCollection.Entities.Count < 1)
                _tracingService.Trace("No survey setup found for branch of sales invoice...");
            
            else
            {
                _tracingService.Trace("Survey Setup found...");

                var surveySetupId = surveySetupCollection.Entities[0].Id;
                EntityCollection surveyQuestionSetupCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_surveyquestionssetup", "gsc_surveysetupid", surveySetupId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_question", "gsc_answertype", "gsc_cmn_surveyquestionssetupid", "gsc_surveyquestionssetuppn" });

                Entity surveyTransaction = new Entity("gsc_cmn_surveytransactions");
                _tracingService.Trace("Retrieved Survey Questions Setup...");

                foreach (Entity surveyQuestionSetup in surveyQuestionSetupCollection.Entities)
                {
                    _tracingService.Trace("Assigning Survey Transaction Fields...");

                    surveyTransaction["gsc_postdeliverymonitoringid"] = vehiclePostDeliveryMonitoring.Id;
                    surveyTransaction["gsc_question"] = surveyQuestionSetup.GetAttributeValue<string>("gsc_question");
                    surveyTransaction["gsc_answertype"] = surveyQuestionSetup.GetAttributeValue<OptionSetValue>("gsc_answertype").Value;
                    surveyTransaction["gsc_surveytransactionpn"] = surveyQuestionSetup.GetAttributeValue<string>("gsc_surveyquestionssetuppn");

                    _tracingService.Trace("Creating Survey Transaction Method...");
                    _organizationService.Create(surveyTransaction);
                }
            }
            _tracingService.Trace("Ended CreateSurveyTransaction Method...");
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 6/10/2016
        /*Purpose: Validate answered survey questions on Completed field = 'Yes' 
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: Completed? = gsc_completed
         *      Post/Create: 
         * Primary Entity: Survey Transaction
         */
        public Entity ValidateSurveyQuestions(Entity vehiclePostDeliveryMonitoring)
        {
            _tracingService.Trace("Started ValidateSurveyQuestions Method...");

            if (vehiclePostDeliveryMonitoring.GetAttributeValue<Boolean>("gsc_completed") == false) { return null; }

            //Retrieve Survey Transaction records
            EntityCollection surveyTransactionRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_surveytransactions", "gsc_postdeliverymonitoringid", vehiclePostDeliveryMonitoring.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_yesornoanswer", "gsc_textanswer" });

            if (surveyTransactionRecords != null && surveyTransactionRecords.Entities.Count > 0)
            {
                foreach (Entity surveyTransaction in surveyTransactionRecords.Entities)
                {
                    if (!surveyTransaction.Contains("gsc_yesornoanswer") && !surveyTransaction.Contains("gsc_textanswer"))
                    {
                        throw new InvalidPluginExecutionException("Survey has no answer.");
                    }
                }
            }

            _tracingService.Trace("Ended ValidateSurveyQuestions Method...");
            return vehiclePostDeliveryMonitoring;        
        }

        //Created By : Jessica Casupanan, Created On : 11/16/2016
        /*Purpose: Auto Populate Call Date and Update Status
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: Completed? = gsc_completed
         *      Post/Create: 
         * Primary Entity: vehicle Post Delivery Monitoring
         */

        public void UpdateStatusAndCallDate(Entity vehiclePostDeliveryMonitoring)
        {
            _tracingService.Trace("Started updateStatusAndCallDate Method...");

            if (vehiclePostDeliveryMonitoring.GetAttributeValue<Boolean>("gsc_completed") == true)
            {
                // update Call date = today
                String today = DateTime.Today.ToString("MM-dd-yyyy");
                vehiclePostDeliveryMonitoring["gsc_calldate"] = Convert.ToDateTime(today);
                // update Status = Completed
                vehiclePostDeliveryMonitoring["gsc_postdeliverystatus"] = new OptionSetValue(100000001);

                _organizationService.Update(vehiclePostDeliveryMonitoring);
            }

            _tracingService.Trace("Ended ValidateSurveyQuestions Method...");
        }

    }
}
