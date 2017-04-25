using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.GVDReport
{
    public class GVDReportHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public GVDReportHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        public void DeleteGVDDetials(Entity gvdReport)
        {
            _tracingService.Trace("Started DeleteGVDDetials Method..");

            EntityCollection gvdDetailCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_gvdreportdetail", "gsc_gvdid", gvdReport.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_gvdid" });

            if (gvdDetailCollection != null && gvdDetailCollection.Entities.Count > 0)
            {
                foreach (Entity gvdDetail in gvdDetailCollection.Entities)
                {
                    _tracingService.Trace("Deleting...");
                    _organizationService.Delete(gvdDetail.LogicalName, gvdDetail.Id);
                }
            }

            _tracingService.Trace("Ended DeleteGVDDetials Method..");
        }

        //Created By: Leslie G. Baliguat, Created On: 01/12/2017
        public Entity FilterInvoice(Entity gvdReport)
        {
            _tracingService.Trace("Started FilterInvoice Method..");

            var dateFrom = gvdReport.Contains("gsc_datefrom")
                ? gvdReport.GetAttributeValue<DateTime>("gsc_datefrom").AddHours(8).ToShortDateString()
                : String.Empty;

            var dateTo = gvdReport.Contains("gsc_dateto")
                ? gvdReport.GetAttributeValue<DateTime>("gsc_dateto").AddHours(8).ToShortDateString()
                : String.Empty;

            QueryExpression queryInvoice = new QueryExpression("invoice");
            queryInvoice.ColumnSet = new ColumnSet(new[] { "invoiceid", "gsc_salesinvoicestatus", "gsc_paymentmode", "gsc_downpaymentpercentage", "gsc_salesexecutiveid", "customerid", "gsc_invoicedate" });
            
            FilterExpression filter = new FilterExpression(LogicalOperator.And);
            FilterExpression filter1 = new FilterExpression(LogicalOperator.And);

            filter1.Conditions.Add(new ConditionExpression("gsc_branchid", ConditionOperator.Equal, CommonHandler.GetEntityReferenceIdSafe(gvdReport, "gsc_branchid")));
            filter1.Conditions.Add(new ConditionExpression("gsc_dealerid", ConditionOperator.Equal, CommonHandler.GetEntityReferenceIdSafe(gvdReport, "gsc_dealerid")));
            filter1.Conditions.Add(new ConditionExpression("gsc_isgvdreported", ConditionOperator.Equal, false));

            if (dateFrom != String.Empty)
                filter1.Conditions.Add(new ConditionExpression("createdon", ConditionOperator.GreaterEqual, dateFrom));
            if (dateTo != String.Empty)
                filter1.Conditions.Add(new ConditionExpression("createdon", ConditionOperator.LessEqual, dateTo));

            FilterExpression filter2 = new FilterExpression(LogicalOperator.Or);
            filter2.Conditions.Add(new ConditionExpression("gsc_salesinvoicestatus", ConditionOperator.Equal, 100000002));
            filter2.Conditions.Add(new ConditionExpression("gsc_salesinvoicestatus", ConditionOperator.Equal, 100000004));

            filter.AddFilter(filter1);
            filter.AddFilter(filter2);

            queryInvoice.Criteria.AddFilter(filter);

            EntityCollection invoiceCollection = _organizationService.RetrieveMultiple(queryInvoice);

            if (invoiceCollection != null && invoiceCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Invoice Retrieved..");

                foreach (var invoiceEntity in invoiceCollection.Entities)
                {
                    EntityCollection salesReturnCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_vehiclesalesreturn", "gsc_invoiceid", invoiceEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_posttransaction"});

                    if (salesReturnCollection != null && salesReturnCollection.Entities.Count > 0)
                    {
                        _tracingService.Trace("Invoice has Sales Return");

                        if (!salesReturnCollection.Entities[0].GetAttributeValue<Boolean>("gsc_PostTransaction"))
                        {
                            _tracingService.Trace("Sales Return not yet posted");
                            CreateGVDDetails(invoiceEntity, gvdReport);
                            continue;
                        }
                    }

                    CreateGVDDetails(invoiceEntity, gvdReport);
                }
            }

            _tracingService.Trace("Ended FilterInvoice Method..");
            return null;

        }

        private void CreateGVDDetails(Entity invoiceEntity, Entity gvdReport)
        {
            _tracingService.Trace("Started CreateGVDDetails Method ");

            Entity gvdDetail = new Entity("gsc_sls_gvdreportdetail");
            gvdDetail["gsc_invoiceid"] = new EntityReference(invoiceEntity.LogicalName, invoiceEntity.Id);
            gvdDetail["gsc_gvdid"] = new EntityReference(gvdReport.LogicalName, gvdReport.Id);
            gvdDetail["gsc_dealerid"] = gvdReport.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                ? gvdReport.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;
            gvdDetail["gsc_branchid"] = gvdReport.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? gvdReport.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;
            gvdDetail["gsc_invoicedate"] = invoiceEntity.Contains("gsc_invoicedate")
                ? invoiceEntity.GetAttributeValue<DateTime>("gsc_invoicedate")
                : (DateTime?)null;
            gvdDetail["gsc_downpayment"] = invoiceEntity.Contains("gsc_downpaymentpercentage")
                ? invoiceEntity.GetAttributeValue<Double>("gsc_downpaymentpercentage").ToString() + "%"
                : String.Empty;
            gvdDetail["gsc_paymentmode"] = invoiceEntity.Contains("gsc_paymentmode")
                ? invoiceEntity.FormattedValues["gsc_paymentmode"]
                : String.Empty;
            gvdDetail["gsc_salesexecutive"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Name
                : String.Empty;


            gvdDetail = SetPaymentTerm(gvdDetail, invoiceEntity);
            gvdDetail = SetCustomerDetails(gvdDetail, invoiceEntity);
            gvdDetail = SetInventoryDetails(gvdDetail, invoiceEntity);

            _organizationService.Create(gvdDetail);

            _tracingService.Trace("Ended CreateGVDDetails Method ");
        }

        private Entity SetCustomerDetails(Entity gvdDetail, Entity invoiceEntity)
        {
            _tracingService.Trace("Started SetCustomerDetails Method ");

            var customer = invoiceEntity.GetAttributeValue<EntityReference>("customerid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("customerid")
                : null;

            if( customer != null)
            {
                if (customer.LogicalName == "contact")
                {
                    SetContactDetails(gvdDetail, customer);
                }
                else
                {
                    SetAccountDetails(gvdDetail, customer);
                }
            }

            _tracingService.Trace("Ended SetCustomerDetails Method ");

            return gvdDetail;
        }

        private Entity SetContactDetails(Entity gvdDetail, EntityReference customer)
        {
            _tracingService.Trace("Started SetContactDetails Method ");

            EntityCollection customerCollection = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", customer.Id, _organizationService, null, OrderType.Ascending,
        new[] { "firstname", "middlename", "lastname", "birthdate", "gendercode", "address1_line1", "gsc_cityid", "gsc_provinceid", "address1_postalcode", "mobilephone" });

            if (customerCollection != null && customerCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Setup Customer Details ");

                var customerEntity = customerCollection.Entities[0];

                gvdDetail["gsc_firstname"] = customerEntity.Contains("firstname")
                    ? customerEntity.GetAttributeValue<String>("firstname")
                    : String.Empty;
                gvdDetail["gsc_middlename"] = customerEntity.Contains("middlename")
                    ? customerEntity.GetAttributeValue<String>("middlename")[0].ToString()
                    : String.Empty;
                gvdDetail["gsc_lastname"] = customerEntity.Contains("lastname")
                    ? customerEntity.GetAttributeValue<String>("lastname")
                    : String.Empty;
                gvdDetail["gsc_birthdate"] = customerEntity.Contains("birthdate")
                    ? customerEntity.GetAttributeValue<DateTime>("birthdate")
                    : (DateTime?)null;
                gvdDetail["gsc_gender"] = customerEntity.Contains("gendercode")
                    ? customerEntity.FormattedValues["gendercode"]
                    : String.Empty;
                var street = customerEntity.Contains("address1_line1")
                    ? customerEntity.GetAttributeValue<String>("address1_line1")
                    : String.Empty;
                var city = customerEntity.GetAttributeValue<EntityReference>("gsc_cityid") != null
                    ? customerEntity.GetAttributeValue<EntityReference>("gsc_cityid").Name
                    : null;
                gvdDetail["gsc_address"] = street + ", " + city;
                gvdDetail["gsc_province"] = customerEntity.GetAttributeValue<EntityReference>("gsc_provinceid") != null
                    ? customerEntity.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                    : String.Empty;
                gvdDetail["gsc_zipcode"] = customerEntity.Contains("address1_postalcode")
                    ? customerEntity.GetAttributeValue<String>("address1_postalcode")
                    : String.Empty;
                gvdDetail["gsc_contactno"] = customerEntity.Contains("mobilephone")
                    ? customerEntity.GetAttributeValue<String>("mobilephone")
                    : String.Empty;

            }

            _tracingService.Trace("Ended SetContactDetails Method ");

            return gvdDetail;
        }

        private Entity SetAccountDetails(Entity gvdDetail, EntityReference customer)
        {
            _tracingService.Trace("Started SetAccountDetails Method ");

            EntityCollection customerCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", customer.Id, _organizationService, null, OrderType.Ascending,
            new[] { "address1_line1", "gsc_cityid", "gsc_provinceid", "address1_postalcode", "name", "telephone1"});

            if (customerCollection != null && customerCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Setup Customer Details ");

                var customerEntity = customerCollection.Entities[0];

                gvdDetail["gsc_companyname"] = customerEntity.Contains("name")
                    ? customerEntity.GetAttributeValue<String>("name")
                    : String.Empty;
                gvdDetail["gsc_contactno"] = customerEntity.Contains("telephone1")
                    ? customerEntity.GetAttributeValue<String>("telephone1")
                    : String.Empty;
                var street = customerEntity.Contains("address1_line1")
                    ? customerEntity.GetAttributeValue<String>("address1_line1")
                    : String.Empty;
                var city = customerEntity.GetAttributeValue<EntityReference>("gsc_cityid") != null
                    ? customerEntity.GetAttributeValue<EntityReference>("gsc_cityid").Name
                    : null;
                gvdDetail["gsc_address"] = street + ", " + city;
                gvdDetail["gsc_province"] = customerEntity.GetAttributeValue<EntityReference>("gsc_provinceid") != null
                    ? customerEntity.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                    : String.Empty;
                gvdDetail["gsc_zipcode"] = customerEntity.Contains("address1_postalcode")
                    ? customerEntity.GetAttributeValue<String>("address1_postalcode")
                    : String.Empty;
            }

            _tracingService.Trace("Ended SetAccountDetails Method ");
            return gvdDetail;

        }

        private Entity SetInventoryDetails(Entity gvdDetail, Entity invoiceEntity)
        {
            _tracingService.Trace("Started SetInventoryDetails Method ");

            EntityCollection inventoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_invoicedvehicle", "gsc_invoiceid", invoiceEntity.Id, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_vin", "gsc_csno" });

            if (inventoryCollection != null && inventoryCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Setup Inventory Details ");

                var invoicedVehicle = inventoryCollection.Entities[0];

                gvdDetail["gsc_vin"] = invoicedVehicle.Contains("gsc_vin")
                    ? invoicedVehicle.GetAttributeValue<String>("gsc_vin")
                    : String.Empty;
                gvdDetail["gsc_csno"] = invoicedVehicle.Contains("gsc_csno")
                    ? invoicedVehicle.GetAttributeValue<String>("gsc_csno")
                    : String.Empty;
            }

            _tracingService.Trace("Ended SetInventoryDetails Method ");

            return gvdDetail;
        }

        private Entity SetPaymentTerm(Entity gvdDetail, Entity invoiceEntity)
        {
            _tracingService.Trace("Started SetPaymentTerm Method ");

            var selectedConditionList = new List<ConditionExpression>
                        {
                            new ConditionExpression("gsc_invoiceid", ConditionOperator.Equal, invoiceEntity.Id),
                            new ConditionExpression("gsc_selected", ConditionOperator.Equal, true)
                        };

            EntityCollection paymentTermsCollection = CommonHandler.RetrieveRecordsByConditions("gsc_sls_invoicemonthlyamortization", selectedConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_financingtermid"});

            if (paymentTermsCollection != null && paymentTermsCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Setup Payment Term");

                var paymentTerm = paymentTermsCollection.Entities[0];

                gvdDetail["gsc_paymentterm"] = paymentTerm.GetAttributeValue<EntityReference>("gsc_financingtermid") != null
                    ? paymentTerm.GetAttributeValue<EntityReference>("gsc_financingtermid").Name
                    : String.Empty;
            }

            _tracingService.Trace("Ended SetPaymentTerm Method ");

            return gvdDetail;
        }

        public void GVDGenerated(Entity gvdReport)
        {
            _tracingService.Trace("Started GVDGenerated Method ");

            EntityCollection gvdDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_gvdreportdetail", "gsc_gvdid", gvdReport.Id, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_invoiceid"});

            if (gvdDetailsCollection != null && gvdDetailsCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve GVD Details");

                foreach (var gvdDetail in gvdDetailsCollection.Entities)
                {
                    EntityCollection invoiceCollection = CommonHandler.RetrieveRecordsByOneValue("invoice", "invoiceid", gvdDetail.GetAttributeValue<EntityReference>("gsc_invoiceid").Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_isgvdreported", "gsc_salesinvoicestatus" });

                    if (invoiceCollection != null && invoiceCollection.Entities.Count > 0)
                    {
                        _tracingService.Trace("Retrieve Invoice");

                        foreach (var invoice in invoiceCollection.Entities)
                        {
                            _tracingService.Trace("Update Invoice");

                            if (invoice.GetAttributeValue<OptionSetValue>("gsc_salesinvoicestatus").Value == 100000004)
                            {
                                ChangeInvoiceStatustoOpen(invoice);
                                invoice["gsc_isgvdreported"] = true;
                                _organizationService.Update(invoice);
                                ChangeInvoiceStatustoReleased(invoice);
                            }
                            else
                            {
                                invoice["gsc_isgvdreported"] = true;
                                _organizationService.Update(invoice);
                            }
                        }
                    }
                }
            }

            _tracingService.Trace("Ended GVDGenerated Method ");
            
        }

        private void ChangeInvoiceStatustoOpen(Entity invoice)
        {
            SetStateRequest request = new SetStateRequest
            {
                EntityMoniker = new EntityReference(invoice.LogicalName, invoice.Id),
                State = new OptionSetValue(0),
                Status = new OptionSetValue(1)
            };

            _organizationService.Execute(request);
        }

        private void ChangeInvoiceStatustoReleased(Entity invoice)
        {
            SetStateRequest request = new SetStateRequest
            {
                EntityMoniker = new EntityReference(invoice.LogicalName, invoice.Id),
                State = new OptionSetValue(2),
                Status = new OptionSetValue(100001)
            };

            _organizationService.Execute(request);
        }

    }
}
