using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace GSC.Rover.DMS.BusinessLogic.SalesDocument
{
    public class SalesDocumentHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public SalesDocumentHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }
        //Created By : Raphael Herrera, Created On : 06/09/2016
        //Modified By: Raphael Herrera, Modified On: 10/04/2016
        /*Purpose: Apply Cash Receipt To Sales Document
         * Event/Message:
         *      Post/Update: gsc_apply
         * Primary Entity: Sales Document
         */
        public void ApplyCashReceipt(Entity salesDocument)
        {
            _tracingService.Trace("Started ApplyCashReceipt Method...");

            var cashReceiptId = salesDocument.Contains("gsc_cashreceiptid") ? salesDocument.GetAttributeValue<EntityReference>("gsc_cashreceiptid").Id
                : Guid.Empty;
            var salesOrderId = salesDocument.Contains("gsc_salesorderid") ?  salesDocument.GetAttributeValue<EntityReference>("gsc_salesorderid").Id
                : Guid.Empty;
            _tracingService.Trace("Retrieved id fields...");

            EntityCollection cashReceiptCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_cashreceipt", "gsc_sls_cashreceiptid", cashReceiptId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_status", "gsc_unappliedamount", "gsc_amount" });

            _tracingService.Trace("Cash Receipt Records Retrieved: " + cashReceiptCollection.Entities.Count);

            #region Update Cash Receipt
            if (cashReceiptCollection.Entities.Count > 0)
            {
                Entity cashReceiptEntity = cashReceiptCollection.Entities[0];

                Decimal amount = cashReceiptEntity.GetAttributeValue<Money>("gsc_amount").Value;
                Decimal unappliedAmount = cashReceiptEntity.GetAttributeValue<Money>("gsc_unappliedamount").Value;
                Decimal balance = salesDocument.GetAttributeValue<Money>("gsc_balance").Value;
                Decimal appliedAmount = salesDocument.GetAttributeValue<Money>("gsc_appliedamount").Value;
                Decimal amountRemaining = balance - appliedAmount;
                Decimal totalAppliedAmount = 0;

                unappliedAmount = amount - appliedAmount;

                if (appliedAmount > amount)
                    throw new InvalidPluginExecutionException("Applied amount cannot be greater than amount in cash receipt.");

                cashReceiptEntity["gsc_unappliedamount"] = new Money(unappliedAmount);
                //Set to Applied Cash Receipt
                if (unappliedAmount == 0)
                    cashReceiptEntity["gsc_status"] = new OptionSetValue(100000000);
                else//Set to Partial Cash Receipt
                    cashReceiptEntity["gsc_status"] = new OptionSetValue(100000003);

                _organizationService.Update(cashReceiptEntity);
                _tracingService.Trace("Updated Cash Receipt...");

                #region Reserve Sales Order
                //Update SO status to Reserved
                if (!(salesDocument.Contains("gsc_invoiceid") && salesDocument.Contains("gsc_invoicedate")))
                {
                    _tracingService.Trace("Setting SO Status to Reserved...");
                    EntityCollection salesOrderCollection = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_status" });
                    salesOrderCollection.Entities[0]["gsc_status"] = new OptionSetValue(100000001);

                    _organizationService.Update(salesOrderCollection.Entities[0]);
                }
                #endregion

                #region Update Sales Document

                EntityCollection salesDocumentCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_salesdocument", "gsc_salesorderid", salesOrderId, _organizationService,
                    null, OrderType.Ascending, new[] {"gsc_appliedamount"});

                _tracingService.Trace("Sales Document Records Retrieved: " + salesDocumentCollection.Entities.Count);

                //Set summation of applied amounts for all sales documents related to SO
                foreach (Entity allSalesDocument in salesDocumentCollection.Entities)
                {
                    totalAppliedAmount = allSalesDocument.Contains("gsc_appliedamount") ? totalAppliedAmount + allSalesDocument.GetAttributeValue<Money>("gsc_appliedamount").Value
                        : totalAppliedAmount + 0;
                }

                salesDocument["gsc_amountremaining"] = new Money(amountRemaining);
                salesDocument["gsc_totalappliedamount"] = new Money(totalAppliedAmount);

                _tracingService.Trace("Updating current sales document...");
                _organizationService.Update(salesDocument);

                #endregion
            }
            else
                _tracingService.Trace("No Cash Receipt record found...");

            #endregion

            _tracingService.Trace("Ended ApplyCashReceipt Method...");
        
        }


        //Created By : Raphael Herrera, Created On : 06/22/2016
        //Modified By: Raphael Herrera, Modified On: 10/05/2016
        /*Purpose: Unapply Sales Document from cash receipt
         * Event/Message:
         *      Post/Update: gsc_apply
         * Primary Entity: Sales Document
         */
        public void UnapplyCashReceipt(Entity salesDocument)
        {
            _tracingService.Trace("Started UnapplyCashReceipt Method...");

            var cashReceiptId = salesDocument.Contains("gsc_cashreceiptid") ? salesDocument.GetAttributeValue<EntityReference>("gsc_cashreceiptid").Id
                : Guid.Empty;
            var salesOrderId = salesDocument.Contains("gsc_salesorderid") ? salesDocument.GetAttributeValue<EntityReference>("gsc_salesorderid").Id
                : Guid.Empty;

            //Retrieve cash receipt of Sales Document
            EntityCollection cashReceiptCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_cashreceipt", "gsc_sls_cashreceiptid", cashReceiptId, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_sls_cashreceiptid", "gsc_unappliedamount"});

            _tracingService.Trace("Cash Receipt Records Retrieved: " + cashReceiptCollection.Entities.Count);

            if (cashReceiptCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Cash Receipt record found...");

                Entity cashReceiptEntity = cashReceiptCollection.Entities[0];

                Decimal unappliedAmount = cashReceiptEntity.GetAttributeValue<Money>("gsc_unappliedamount").Value;
                Decimal appliedAmount = salesDocument.GetAttributeValue<Money>("gsc_appliedamount").Value;
                Decimal amountRemaining = salesDocument.GetAttributeValue<Money>("gsc_amountremaining").Value;

                cashReceiptEntity["gsc_unappliedamount"] = new Money(unappliedAmount + appliedAmount);

                _organizationService.Update(cashReceiptCollection.Entities[0]);
                _tracingService.Trace("Updated CashReceipt unapplied amount..");

                EntityCollection salesDocumentCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_salesdocument", "gsc_salesorderid", salesOrderId, _organizationService, "createdon", OrderType.Ascending,
                        new[] { "gsc_sls_salesdocumentid", "gsc_amountremaining", "gsc_balance", "gsc_appliedamount" });

                int ctr = 0;
                bool foundUnapplied = false;

                _tracingService.Trace("Running through all associated cash receipts. " + salesDocumentCollection.Entities.Count + " records found...");

                //Parse through all sales document in collection from oldest to newest until unapplied record found. 
                while (salesDocumentCollection.Entities.Count > ctr)
                {
                    _tracingService.Trace("Checking record " + (ctr + 1));

                    //Records affected by unapplied record. 
                    if (foundUnapplied)
                    {
                        //record for rollup
                        Decimal rollupBalance = salesDocumentCollection.Entities[ctr].GetAttributeValue<Money>("gsc_balance").Value;
                        Decimal rollupAmountRemaining = salesDocumentCollection.Entities[ctr].GetAttributeValue<Money>("gsc_amountremaining").Value;

                        if (ctr == (salesDocumentCollection.Entities.Count - 1))
                        {
                            _tracingService.Trace("Latest Record reached..." );
                            //reached newest record
                            salesDocumentCollection.Entities[ctr]["gsc_balance"] = new Money(rollupBalance + appliedAmount);
                            salesDocumentCollection.Entities[ctr]["gsc_amountremaining"] = new Money(rollupBalance + appliedAmount);
                        }
                        else
                        {
                            _tracingService.Trace("Not latest record...");
                            //not latest record for rollup
                            salesDocumentCollection.Entities[ctr]["gsc_balance"] = new Money(rollupBalance + appliedAmount);
                            salesDocumentCollection.Entities[ctr]["gsc_amountremaining"] = new Money(rollupAmountRemaining + appliedAmount);
                        }
                        _organizationService.Update(salesDocumentCollection.Entities[ctr]);
                        _tracingService.Trace("Applied adjustments...");
                    }
                        
                    else if (salesDocumentCollection.Entities[ctr].Id == salesDocument.Id)
                    {
                            _tracingService.Trace("Unapplied record detected...");
                            foundUnapplied = true;
                    }
                        

                    ctr++;
                }

            }
                
            else
                _tracingService.Trace("No Cash Receipts associated...");

            _tracingService.Trace("Ending UnapplyCashReceipt Method...");  
        }
    }
}
