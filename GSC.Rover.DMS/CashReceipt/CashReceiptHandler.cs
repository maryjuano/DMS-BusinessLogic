using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.SalesDocument;

namespace GSC.Rover.DMS.BusinessLogic.CashReceipt
{
    public class CashReceiptHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public CashReceiptHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }
        
        //Created By : Jefferson Cordero, Created On : 5/12/2016
        //******* Modified By: Raphael Herrera, Modified On: 10/04/2016 *******//
        // Purpose: Search for other related Sales Document prior to creation

        //**
        public Entity PopulateSalesDocumentSubGrid(Entity cashReceipt)
        {
            _tracingService.Trace("Started PopulateSalesDocumentSubGrid method..");

            var customerId = cashReceipt.Contains("gsc_contactid") ? cashReceipt.GetAttributeValue<EntityReference>("gsc_contactid").Id
                : cashReceipt.GetAttributeValue<EntityReference>("gsc_accountid").Id;

            //EntityCollection salesDocumentsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_salesdocument", "gsc_cashreceiptid", cashReceipt.Id, _organizationService, null, OrderType.Ascending,
            //            new[] { "gsc_sls_salesdocumentid" });

            //foreach (Entity salesDocument in salesDocumentsCollection.Entities)
            //{
            //    _organizationService.Delete(salesDocument.LogicalName, salesDocument.Id);
            //    _tracingService.Trace("Deleted Related Sales Document..");
            //}

            //for sales order records
            EntityCollection SalesOrderCollection = CommonHandler.RetrieveRecordsByOneValue("salesorder", "customerid", customerId, _organizationService, null, OrderType.Ascending,
                        new[] { "createdon", "salesorderid", "gsc_totalamountdue" });

            _tracingService.Trace("Sales Order Records Retrieved: " + SalesOrderCollection.Entities.Count);
            if (SalesOrderCollection != null && SalesOrderCollection.Entities.Count > 0)
            {
                for (int i = 0; i < SalesOrderCollection.Entities.Count; i++)
                {
                    Entity SalesDocumentEntity = new Entity("gsc_sls_salesdocument");

                    Entity salesOrder = SalesOrderCollection.Entities[i];

                    var orderDate = salesOrder.Contains("createdon")
                        ? salesOrder.GetAttributeValue<DateTime>("createdon")
                        : new DateTime(1900, 1, 1);

                    var salesOrderId = salesOrder.Contains("salesorderid")
                        ? salesOrder.GetAttributeValue<Guid>("salesorderid")
                        : Guid.Empty;

                    var totalAmount = salesOrder.Contains("gsc_totalamountdue")
                        ? (Decimal)salesOrder.GetAttributeValue<Money>("gsc_totalamountdue").Value
                        : Decimal.Zero;

                    if (salesOrderId != Guid.Empty)
                        SalesDocumentEntity["gsc_salesorderid"] = new EntityReference("salesorder", salesOrderId);



                    //Retrieve Sales Documents related to same SO
                    EntityCollection salesDocumentCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_salesdocument", "gsc_salesorderid", salesOrder.Id, _organizationService,
                        "createdon", OrderType.Descending, new[] { "gsc_totalappliedamount", "gsc_chargeableamount", "gsc_amountremaining", "gsc_appliedamount" });

                    _tracingService.Trace("Sales Document Records Retrieved: " + salesDocumentCollection.Entities.Count);

                    //Has existing Sales Document records for sales order
                    if (salesDocumentCollection.Entities.Count > 0)
                    {
                        Entity latestSalesDocument = salesDocumentCollection.Entities[0];

                        Decimal totalAppliedAmount = latestSalesDocument.GetAttributeValue<Money>("gsc_totalappliedamount").Value;
                        Decimal amountRemaining = latestSalesDocument.GetAttributeValue<Money>("gsc_amountremaining").Value;

                        SalesDocumentEntity["gsc_balance"] = new Money(amountRemaining);
                        SalesDocumentEntity["gsc_amountremaining"] = new Money(amountRemaining);
                        SalesDocumentEntity["gsc_totalappliedamount"] = new Money(totalAppliedAmount);

                        _tracingService.Trace("Total Appled:" + totalAppliedAmount + "   Balance:" + amountRemaining);
                    }

                    //Sales Document is first for SO
                    else
                    {
                        SalesDocumentEntity["gsc_balance"] = new Money(totalAmount);
                        SalesDocumentEntity["gsc_amountremaining"] = new Money(totalAmount);
                        SalesDocumentEntity["gsc_totalappliedamount"] = new Money(0);
                    }

                    SalesDocumentEntity["gsc_appliedamount"] = new Money(0);
                    SalesDocumentEntity["gsc_orderdate"] = orderDate;
                    SalesDocumentEntity["gsc_chargeableamount"] = new Money(totalAmount);
                    SalesDocumentEntity["gsc_apply"] = false;
                    SalesDocumentEntity["gsc_cashreceiptid"] = new EntityReference(cashReceipt.LogicalName, cashReceipt.Id);
                    
                    

                    _tracingService.Trace("Assigned Sales Order fields..");

                    EntityCollection SalesInvoiceCollection = CommonHandler.RetrieveRecordsByOneValue("invoice", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                    new[] { "invoiceid", "createdon" });

                    if (SalesInvoiceCollection != null && SalesInvoiceCollection.Entities.Count > 0)
                    {
                        _tracingService.Trace("Invoice Records Found..");
                        var salesInvoice = SalesInvoiceCollection.Entities[0];

                        var invoiceId = salesInvoice.Contains("invoiceid")
                        ? salesInvoice.GetAttributeValue<Guid>("invoiceid")
                        : Guid.Empty;

                        var invoiceDate = salesInvoice.Contains("createdon")
                        ? salesInvoice.GetAttributeValue<DateTime>("createdon")
                        : new DateTime(1900, 1, 1);

                        if (invoiceId != Guid.Empty)
                            SalesDocumentEntity["gsc_invoiceid"] = new EntityReference("invoice", invoiceId);
                        SalesDocumentEntity["gsc_invoicedate"] = invoiceDate;
                        _tracingService.Trace("Assigned Invoice fields..");
                    }
                    else
                    {
                        _tracingService.Trace("No Invoice Records found ...");
                    }
                    _tracingService.Trace("Creating Sales Document Record..");
                    Guid id = _organizationService.Create(SalesDocumentEntity);
                }
            }
            _tracingService.Trace("Ending PopulateSalesDocumentSubGrid method..");
            return null;

        }

        //Created By : Raphael Herrera, Created On : 06/23/2016
        /*Purpose: Void Cash receipt. Unapply all connected applied sales document
         * Event/Message:
         *      Post/Update: gsc_void
         * Primary Entity: Sales Document
         */
        public void VoidCashReceipt(Entity cashReceipt)
        {
            _tracingService.Trace("Started VoidCashReceipt Method...");

            var salesDocumentConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_cashreceiptid", ConditionOperator.Equal, cashReceipt.Id)
                };

            EntityCollection salesDocumentCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_salesdocument", "gsc_cashreceiptid", cashReceipt.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_apply", "gsc_cashreceiptid" });

            _tracingService.Trace("Retrieved applied sales document associated. " + salesDocumentCollection.Entities.Count + " records found...");

            SalesDocumentHandler sdHandler = new SalesDocumentHandler(_organizationService, _tracingService);
            foreach(Entity salesDocument in salesDocumentCollection.Entities)
            {
                //Delete connected sales documents
                _tracingService.Trace("Unapplying...");
                _organizationService.Delete(salesDocument.LogicalName, salesDocument.Id);
            }

            _tracingService.Trace("Ending VoidCashReceipt Method...");
        }

        //Created By : Raphael Herrera, Created On : 06/23/2016
        /*Purpose: Fill in other information of cash receipt
         * Event/Message:
         *      Post/Create: cash receipt
         * Primary Entity: Cash Receipt
         */
        public Entity PopulateCashReceipt(Entity cashReceipt)
        {
            _tracingService.Trace("Starting PopulateCashReceipt Method...");

            var customerType = cashReceipt.Contains("gsc_customertype") ? cashReceipt.GetAttributeValue<OptionSetValue>("gsc_customertype").Value
                : 0;
            var amount = cashReceipt.GetAttributeValue<Money>("gsc_amount").Value;
            string customerName = "";
            string customerId = "";

            //Individual Customer
            if (customerType == 100000000)
            {
                _tracingService.Trace("Individual Customer...");

                var contactId = cashReceipt.Contains("gsc_contactid") ? cashReceipt.GetAttributeValue<EntityReference>("gsc_contactid").Id
                : Guid.Empty;

                EntityCollection contactCollection = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", contactId, _organizationService, null,
                    OrderType.Ascending, new[] { "gsc_customerid", "fullname" });

                _tracingService.Trace("Contact Records retrieved: " + contactCollection.Entities.Count);
                if (contactCollection.Entities.Count > 0)
                {
                    Entity contactEntity = contactCollection.Entities[0];
                    customerId = contactEntity.Contains("gsc_customerid") ? contactEntity.GetAttributeValue<string>("gsc_customerid") : string.Empty;
                    customerName = contactEntity.Contains("fullname") ? contactEntity.GetAttributeValue<string>("fullname") : string.Empty;
                }
            }

            //Corporate Customer
            else if (customerType == 100000001)
            {
                _tracingService.Trace("Corporate Customer...");

                var accountId = cashReceipt.Contains("gsc_accountid") ? cashReceipt.GetAttributeValue<EntityReference>("gsc_accountid").Id
                : Guid.Empty;

                EntityCollection accountCollection = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", accountId, _organizationService, null,
                    OrderType.Ascending, new[] { "name", "accountnumber" });

                _tracingService.Trace("Account Records retrieved: " + accountCollection.Entities.Count);
                if (accountCollection.Entities.Count > 0)
                {
                    Entity accountentity = accountCollection.Entities[0];
                    customerId = accountentity.Contains("accountnumber") ? accountentity.GetAttributeValue<string>("accountnumber") : string.Empty;
                    customerName = accountentity.Contains("name") ? accountentity.GetAttributeValue<string>("name") : string.Empty;
                }
            }

            cashReceipt["gsc_customerid"] = customerId;
            cashReceipt["gsc_customername"] = customerName;
            cashReceipt["gsc_unappliedamount"] = new Money(amount);


            _tracingService.Trace("Ending PopulateCashReceipt Method...");
            return cashReceipt;
        }
    }
}
