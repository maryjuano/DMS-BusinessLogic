using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Crm.Sdk.Messages;
using System.Globalization;
using System.Data.SqlClient;
using System.Threading;
using GSC.Rover.DMS.BusinessLogic.InventoryMovement;

namespace GSC.Rover.DMS.BusinessLogic.Invoice
{
    public class InvoiceHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public InvoiceHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/10/2016
        //Modified By : Artum M. Ramos, Created On : 1/21/2017
        /*Purpose: Replicate Order fields into newly created Invoice record
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: Order ID = salesorderid
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Invoice
         */
        public Entity ReplicateOrderInfo(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateOrderInfo method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            _tracingService.Trace("a"); 

            //Retrieve Order records
            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                 new[] {  "gsc_dealerid", "gsc_branchid", "gsc_recordownerid",
                          "gsc_leadsourceid", "gsc_salesexecutiveid", "gsc_paymentmode",
                          "gsc_customerid", "gsc_customertype", "gsc_address", "gsc_tin", 
                          "gsc_productid", "gsc_vehiclecolorid1", "gsc_vehiclecolorid2", "gsc_vehiclecolorid3", "gsc_vehicleunitprice", "gsc_vehicledetails", "gsc_remarks",
                          "gsc_unitprice", "gsc_colorprice", "gsc_discount", "gsc_downpayment", "gsc_accessories", "gsc_insurance", "gsc_chattelfee", "gsc_othercharges", 
                                "gsc_reservation", "gsc_netmonthlyamortization", "gsc_totalcashoutlay", "gsc_totalamountfinanced", "gsc_netmonthlyamortization", 
                                "gsc_vatablesales", "gsc_vatexemptsales", "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount", "gsc_totalamountdue",
                          "gsc_downpaymentamount", "gsc_downpaymentpercentage", "gsc_downpaymentdiscount", "gsc_netdownpayment", "gsc_amountfinanced", "gsc_discountamountfinanced", 
                                "gsc_netamountfinanced", "gsc_bankid", "gsc_financingschemeid", "gsc_freechattelfee", "gsc_chattelfeeeditable",
                           "gsc_totaldiscountamount", "gsc_applytodppercentage", "gsc_applytoafpercentage", "gsc_applytouppercentage", "gsc_applytodpamount", "gsc_applytoafamount", "gsc_applytoupamount",
                           "gsc_insuranceid", "gsc_vehicletypeid", "gsc_vehicleuse", "gsc_free", "gsc_cost", "gsc_totalpremium", "gsc_originaltotalpremuim", "gsc_providercompanyid",
                           "gsc_totalchargesamount", "gsc_ccaddons", "gsc_netprice", "gsc_freightandhandling",
                           "gsc_documentstatus",
                           "gsc_requesteddeliverydate", "gsc_promiseddeliverydate", "gsc_placeofrelease", "gsc_deliverytermsremarks", "gsc_quotedate", "gsc_orderdate", "gsc_requestedallocationdate", "gsc_vehicleallocateddate", "gsc_transferreddateforinvoicing",
                "gsc_ordercancelleddate", "gsc_createddate", "gsc_invoicedate", "gsc_printeddrdate", "gsc_releaseddate", "gsc_invoicecancelleddate" });

            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                _tracingService.Trace("0"); 
                Entity salesOrder = salesOrderRecords.Entities[0];

                String today = DateTime.Today.ToString("MM-dd-yyyy");

                #region Record Information
                invoiceEntity["gsc_recordownerid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                    ? new EntityReference("contact", salesOrder.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                    : null;
                invoiceEntity["gsc_dealerid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                    ? new EntityReference("account", salesOrder.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                    : null;
                _tracingService.Trace("1");
                invoiceEntity["gsc_branchid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_branchid") != null
                    ? new EntityReference("account", salesOrder.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                    : null;
                _tracingService.Trace("2"); 
                #endregion

                #region Invoice Information
                invoiceEntity["gsc_leadsourceid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_leadsourceid") != null
                    ? new EntityReference("gsc_sls_leadsource", salesOrder.GetAttributeValue<EntityReference>("gsc_leadsourceid").Id)
                    : null;
                _tracingService.Trace("3");
                invoiceEntity["gsc_salesexecutiveid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_salesexecutiveid") != null
                    ? new EntityReference("contact", salesOrder.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id)
                    : null;
                _tracingService.Trace("4");
                invoiceEntity["gsc_paymentmode"] = salesOrder.GetAttributeValue<OptionSetValue>("gsc_paymentmode") != null
                    ? new OptionSetValue(salesOrder.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value)
                    : null;
                _tracingService.Trace("5");
                invoiceEntity["gsc_salesinvoicestatus"] = new OptionSetValue(100000000);
                #endregion

                #region Customer Information
                invoiceEntity["gsc_customer"] = salesOrder.GetAttributeValue<String>("gsc_customerid") != null
                    ? salesOrder.GetAttributeValue<String>("gsc_customerid")
                    : null;
                invoiceEntity["gsc_customertype"] = salesOrder.GetAttributeValue<OptionSetValue>("gsc_customertype") != null
                    ? new OptionSetValue(salesOrder.GetAttributeValue<OptionSetValue>("gsc_customertype").Value)
                    : null;
                _tracingService.Trace("6");
                invoiceEntity["shipto_composite"] = salesOrder.GetAttributeValue<String>("gsc_address") != null
                    ? salesOrder.GetAttributeValue<String>("gsc_address")
                    : null;
                _tracingService.Trace("8");
                invoiceEntity["gsc_tin"] = salesOrder.GetAttributeValue<String>("gsc_tin") != null
                    ? salesOrder.GetAttributeValue<String>("gsc_tin")
                    : null;
                _tracingService.Trace("9");
                #endregion

                #region Vehicle Information
                invoiceEntity["gsc_productid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? new EntityReference("product", salesOrder.GetAttributeValue<EntityReference>("gsc_productid").Id)
                    : null;
                _tracingService.Trace("10");
                invoiceEntity["gsc_vehicleunitprice"] = salesOrder.GetAttributeValue<Money>("gsc_vehicleunitprice") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_vehicleunitprice").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("11");
                invoiceEntity["gsc_vehiclecolorid1"] = salesOrder.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1") != null
                    ? new EntityReference("gsc_cmn_vehiclecolor", salesOrder.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1").Id)
                    : null;
                _tracingService.Trace("12");
                invoiceEntity["gsc_vehiclecolorid2"] = salesOrder.GetAttributeValue<EntityReference>("gsc_vehiclecolorid2") != null
                    ? new EntityReference("gsc_cmn_vehiclecolor", salesOrder.GetAttributeValue<EntityReference>("gsc_vehiclecolorid2").Id)
                    : null;
                _tracingService.Trace("13");
                invoiceEntity["gsc_vehiclecolorid3"] = salesOrder.GetAttributeValue<EntityReference>("gsc_vehiclecolorid3") != null
                    ? new EntityReference("gsc_cmn_vehiclecolor", salesOrder.GetAttributeValue<EntityReference>("gsc_vehiclecolorid3").Id)
                    : null;
                _tracingService.Trace("14");
                invoiceEntity["gsc_vehicledetails"] = salesOrder.GetAttributeValue<String>("gsc_vehicledetails") != null
                    ? salesOrder.GetAttributeValue<String>("gsc_vehicledetails")
                    : null;
                _tracingService.Trace("15");
                invoiceEntity["gsc_remarks"] = salesOrder.GetAttributeValue<String>("gsc_remarks") != null
                    ? salesOrder.GetAttributeValue<String>("gsc_remarks")
                    : null;
                _tracingService.Trace("16");
                #endregion

                #region Payment Summary
                invoiceEntity["gsc_unitprice"] = salesOrder.GetAttributeValue<Money>("gsc_unitprice") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_unitprice").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_ccaddons"] = salesOrder.Contains("gsc_ccaddons")
                    ? salesOrder.GetAttributeValue<Money>("gsc_ccaddons")
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_colorprice"] = salesOrder.GetAttributeValue<Money>("gsc_colorprice") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_colorprice").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_freightandhandling"] = salesOrder.GetAttributeValue<Money>("gsc_freightandhandling") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_freightandhandling").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_discount"] = salesOrder.GetAttributeValue<Money>("gsc_discount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_discount").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_netprice"] = salesOrder.GetAttributeValue<Money>("gsc_netprice") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_netprice").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_accessories"] = salesOrder.GetAttributeValue<Money>("gsc_accessories") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_accessories").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_insurance"] = salesOrder.GetAttributeValue<Money>("gsc_insurance") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_insurance").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_chattelfee"] = salesOrder.GetAttributeValue<Money>("gsc_chattelfee") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_chattelfee").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_othercharges"] = salesOrder.GetAttributeValue<Money>("gsc_othercharges") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_othercharges").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_reservation"] = salesOrder.GetAttributeValue<Money>("gsc_reservation") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_reservation").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_downpayment"] = salesOrder.GetAttributeValue<Money>("gsc_downpayment") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_downpayment").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_vatablesales"] = salesOrder.GetAttributeValue<Money>("gsc_vatablesales") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_vatablesales").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_vatexemptsales"] = salesOrder.GetAttributeValue<Money>("gsc_vatexemptsales") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_vatexemptsales").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_zeroratedsales"] = salesOrder.GetAttributeValue<Money>("gsc_zeroratedsales") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_zeroratedsales").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_totalsales"] = salesOrder.GetAttributeValue<Money>("gsc_totalsales") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_totalsales").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_vatamount"] = salesOrder.GetAttributeValue<Money>("gsc_vatamount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_vatamount").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["totalamount"] = salesOrder.GetAttributeValue<Money>("gsc_totalamountdue") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_totalamountdue").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_totalcashoutlay"] = salesOrder.GetAttributeValue<Money>("gsc_totalcashoutlay") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_totalcashoutlay").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_totalamountfinanced"] = salesOrder.GetAttributeValue<Money>("gsc_totalamountfinanced") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_totalamountfinanced").Value)
                    : new Money(Decimal.Zero);
                invoiceEntity["gsc_netmonthlyamortization"] = salesOrder.GetAttributeValue<Money>("gsc_netmonthlyamortization") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_netmonthlyamortization").Value)
                    : new Money(Decimal.Zero);
                #endregion

                #region Financing 
                invoiceEntity["gsc_downpaymentamount"] = salesOrder.GetAttributeValue<Money>("gsc_downpaymentamount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_downpaymentamount").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("30");
                invoiceEntity["gsc_downpaymentpercentage"] = salesOrder.GetAttributeValue<Double>("gsc_downpaymentpercentage") != null
                    ? salesOrder.GetAttributeValue<Double>("gsc_downpaymentpercentage")
                    : 0;
                _tracingService.Trace("31");
                invoiceEntity["gsc_downpaymentdiscount"] = salesOrder.GetAttributeValue<Money>("gsc_downpaymentdiscount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_downpaymentdiscount").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("32");
                invoiceEntity["gsc_netdownpayment"] = salesOrder.GetAttributeValue<Money>("gsc_netdownpayment") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_netdownpayment").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("33");
                invoiceEntity["gsc_amountfinanced"] = salesOrder.GetAttributeValue<Money>("gsc_amountfinanced") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_amountfinanced").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("34");
                invoiceEntity["gsc_discountamountfinanced"] = salesOrder.GetAttributeValue<Money>("gsc_discountamountfinanced") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_discountamountfinanced").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("33");
                invoiceEntity["gsc_netamountfinanced"] = salesOrder.GetAttributeValue<Money>("gsc_netamountfinanced") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_netamountfinanced").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("34");
                invoiceEntity["gsc_bankid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_bankid") != null
                    ? new EntityReference("gsc_sls_bank", salesOrder.GetAttributeValue<EntityReference>("gsc_bankid").Id)
                    : null;
                _tracingService.Trace("35");
                invoiceEntity["gsc_financingschemeid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_financingschemeid") != null
                    ? new EntityReference("gsc_cmn_financingscheme", salesOrder.GetAttributeValue<EntityReference>("gsc_financingschemeid").Id)
                    : null;
                _tracingService.Trace("36");
                invoiceEntity["gsc_freechattelfee"] = salesOrder.GetAttributeValue<Boolean>("gsc_freechattelfee");
                _tracingService.Trace("37");
                invoiceEntity["gsc_chattelfeeeditable"] = salesOrder.GetAttributeValue<Money>("gsc_chattelfeeeditable") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_chattelfeeeditable").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("34");
                #endregion

                #region Discounts
                invoiceEntity["gsc_totaldiscountamount"] = salesOrder.GetAttributeValue<Money>("gsc_totaldiscountamount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_totaldiscountamount").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("38");
                invoiceEntity["gsc_applytodppercentage"] = salesOrder.GetAttributeValue<Double>("gsc_applytodppercentage") != null
                    ? salesOrder.GetAttributeValue<Double>("gsc_applytodppercentage")
                    : 0;
                _tracingService.Trace("39");
                invoiceEntity["gsc_applytoafpercentage"] = salesOrder.GetAttributeValue<Double>("gsc_applytoafpercentage") != null
                    ? salesOrder.GetAttributeValue<Double>("gsc_applytoafpercentage")
                    : 0;
                _tracingService.Trace("40");
                invoiceEntity["gsc_applytouppercentage"] = salesOrder.GetAttributeValue<Double>("gsc_applytouppercentage") != null
                    ? salesOrder.GetAttributeValue<Double>("gsc_applytouppercentage")
                    : 0;
                _tracingService.Trace("41");
                invoiceEntity["gsc_applytodpamount"] = salesOrder.GetAttributeValue<Money>("gsc_applytodpamount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_applytodpamount").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("42");
                invoiceEntity["gsc_applytoafamount"] = salesOrder.GetAttributeValue<Money>("gsc_applytoafamount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_applytoafamount").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("43");
                invoiceEntity["gsc_applytoupamount"] = salesOrder.GetAttributeValue<Money>("gsc_applytoupamount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_applytoupamount").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("44");
                #endregion

                #region Insurance
                invoiceEntity["gsc_insuranceid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_insuranceid") != null
                    ? new EntityReference("gsc_cmn_insurance", salesOrder.GetAttributeValue<EntityReference>("gsc_insuranceid").Id)
                    : null;
                _tracingService.Trace("45");
                invoiceEntity["gsc_vehicletypeid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_vehicletypeid") != null
                    ? new EntityReference("gsc_iv_vehicletype", salesOrder.GetAttributeValue<EntityReference>("gsc_vehicletypeid").Id)
                    : null;
                _tracingService.Trace("46");
                invoiceEntity["gsc_providerid"] = salesOrder.GetAttributeValue<EntityReference>("gsc_providercompanyid") != null
                    ? salesOrder.GetAttributeValue<EntityReference>("gsc_providercompanyid")
                    : null;
                _tracingService.Trace("45");
                invoiceEntity["gsc_vehicleuse"] = salesOrder.GetAttributeValue<OptionSetValue>("gsc_vehicleuse") != null
                    ? new OptionSetValue(salesOrder.GetAttributeValue<OptionSetValue>("gsc_vehicleuse").Value)
                    : null;
                _tracingService.Trace("47");
                invoiceEntity["gsc_free"] = salesOrder.GetAttributeValue<Boolean>("gsc_free");
                _tracingService.Trace("48");
                invoiceEntity["gsc_rate"] = salesOrder.Contains("gsc_rate")
                    ? salesOrder.GetAttributeValue<Double>("gsc_rate")
                    : (Double)0;
                _tracingService.Trace("49");
                invoiceEntity["gsc_cost"] = salesOrder.GetAttributeValue<Money>("gsc_cost") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_cost").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("50");
                invoiceEntity["gsc_totalpremium"] = salesOrder.GetAttributeValue<Money>("gsc_totalpremium") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_totalpremium").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("51");
                invoiceEntity["gsc_originaltotalpremium"] = salesOrder.GetAttributeValue<Money>("gsc_originaltotalpremuim") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_originaltotalpremuim").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("52");
                #endregion

                #region Charges
                invoiceEntity["gsc_totalchargesamount"] = salesOrder.GetAttributeValue<Money>("gsc_totalchargesamount") != null
                    ? new Money(salesOrder.GetAttributeValue<Money>("gsc_totalchargesamount").Value)
                    : new Money(Decimal.Zero);
                _tracingService.Trace("53");
                #endregion

                #region Document Checklist
                invoiceEntity["gsc_documentstatus"] = salesOrder.Contains("gsc_documentstatus") != null
                    ? salesOrder.GetAttributeValue<OptionSetValue>("gsc_documentstatus")
                    : null;
                _tracingService.Trace("53");
                #endregion

                #region Sales Date Entries
                invoiceEntity["gsc_requesteddeliverydate"] = salesOrder.Contains("gsc_requesteddeliverydate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_requesteddeliverydate")
                    : (DateTime?)null;
                invoiceEntity["gsc_promiseddeliverydate"] = salesOrder.Contains("gsc_promiseddeliverydate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_promiseddeliverydate")
                    : (DateTime?)null;
                _tracingService.Trace("70");
                invoiceEntity["gsc_placeofrelease"] = salesOrder.GetAttributeValue<String>("gsc_placeofrelease") != null
                    ? salesOrder.GetAttributeValue<String>("gsc_placeofrelease")
                    : String.Empty;
                invoiceEntity["gsc_deliverytermsremarks"] = salesOrder.GetAttributeValue<String>("gsc_deliverytermsremarks") != null
                    ? salesOrder.GetAttributeValue<String>("gsc_deliverytermsremarks")
                    : String.Empty;
                _tracingService.Trace("71");
                invoiceEntity["gsc_quotedate"] = salesOrder.Contains("gsc_quotedate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_quotedate")
                    : (DateTime?)null;
                _tracingService.Trace("72");
                invoiceEntity["gsc_orderdate"] = salesOrder.Contains("gsc_orderdate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_orderdate")
                    : (DateTime?)null;
                _tracingService.Trace("73");
                invoiceEntity["gsc_requestedallocationdate"] = salesOrder.Contains("gsc_requestedallocationdate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_requestedallocationdate")
                    : (DateTime?)null;
                _tracingService.Trace("74");
                invoiceEntity["gsc_vehicleallocateddate"] = salesOrder.Contains("gsc_vehicleallocateddate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_vehicleallocateddate")
                    : (DateTime?)null;
                _tracingService.Trace("75");
                invoiceEntity["gsc_transferreddateforinvoicing"] = salesOrder.Contains("gsc_transferreddateforinvoicing")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_transferreddateforinvoicing")
                    : (DateTime?)null;
                _tracingService.Trace("76");
                invoiceEntity["gsc_ordercancelleddate"] = salesOrder.Contains("gsc_ordercancelleddate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_ordercancelleddate")
                    : (DateTime?)null;
                _tracingService.Trace("77");
                invoiceEntity["gsc_createddate"] = Convert.ToDateTime(today);
                invoiceEntity["gsc_invoicedate"] = salesOrder.Contains("gsc_invoicedate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_invoicedate")
                    : (DateTime?)null;
                _tracingService.Trace("78");
                invoiceEntity["gsc_printeddrdate"] = salesOrder.Contains("gsc_printeddrdate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_printeddrdate")
                    : (DateTime?)null;
                _tracingService.Trace("79");
                invoiceEntity["gsc_releaseddate"] = salesOrder.Contains("gsc_releaseddate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_releaseddate")
                    : (DateTime?)null;
                invoiceEntity["gsc_invoicecancelleddate"] = salesOrder.Contains("gsc_invoicecancelleddate")
                    ? salesOrder.GetAttributeValue<DateTime?>("gsc_invoicecancelleddate")
                    : (DateTime?)null;
                _tracingService.Trace("80");
                _tracingService.Trace("81");
                #endregion

            }

            _tracingService.Trace("Ended ReplicateOrderInfo method..");
            return invoiceEntity;
        }
        
        //Created By : Jerome Anthony Gerero, Created On : 5/11/2016
        /*Purpose: Replicate Allocated Vehicle records into Invoiced Vehicle entity
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create: Order ID = salesorderid
         * Primary Entity: Invoice
         */
        public Entity ReplicateAllocatedVehicle(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateAllocatedVehicle method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection allocatedVehicleRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_allocatedvehicle", "gsc_orderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_allocatedvehiclepn", "gsc_inventoryid", "gsc_modelcode", "gsc_optioncode", "gsc_productionno", "gsc_csno", "gsc_engineno", "gsc_vin", "gsc_color", "gsc_vehicleallocateddate" });

            if (allocatedVehicleRecords != null && allocatedVehicleRecords.Entities.Count > 0)
            {
                Entity allocatedVehicle = allocatedVehicleRecords.Entities[0];

                Entity invoicedVehicle = new Entity("gsc_iv_invoicedvehicle");
                invoicedVehicle["gsc_invoicedvehiclepn"] = allocatedVehicle.GetAttributeValue<String>("gsc_allocatedvehiclepn") != null
                    ? allocatedVehicle.GetAttributeValue<String>("gsc_allocatedvehiclepn")
                    : String.Empty;
                invoicedVehicle["gsc_inventoryid"] = allocatedVehicle.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                    ? new EntityReference("gsc_iv_inventory", allocatedVehicle.GetAttributeValue<EntityReference>("gsc_inventoryid").Id)
                    : null;
                invoicedVehicle["gsc_invoiceid"] = new EntityReference("invoice", invoiceEntity.Id);
                invoicedVehicle["gsc_modelcode"] = allocatedVehicle.GetAttributeValue<String>("gsc_modelcode") != null
                    ? allocatedVehicle.GetAttributeValue<String>("gsc_modelcode")
                    : null;
                invoicedVehicle["gsc_optioncode"] = allocatedVehicle.GetAttributeValue<String>("gsc_optioncode") != null
                    ? allocatedVehicle.GetAttributeValue<String>("gsc_optioncode")
                    : null;
                invoicedVehicle["gsc_color"] = allocatedVehicle.GetAttributeValue<String>("gsc_color") != null
                    ? allocatedVehicle.GetAttributeValue<String>("gsc_color")
                    : null;

                var prodNo = allocatedVehicle.GetAttributeValue<String>("gsc_productionno") != null
                    ? allocatedVehicle.GetAttributeValue<String>("gsc_productionno")
                    : null;
                var csNo = allocatedVehicle.GetAttributeValue<String>("gsc_csno") != null
                    ? allocatedVehicle.GetAttributeValue<String>("gsc_csno")
                    : null;
                var engineNo = allocatedVehicle.GetAttributeValue<String>("gsc_engineno") != null
                    ? allocatedVehicle.GetAttributeValue<String>("gsc_engineno")
                    : null;
                var vin = allocatedVehicle.GetAttributeValue<String>("gsc_vin") != null
                    ? allocatedVehicle.GetAttributeValue<String>("gsc_vin")
                    : null;

                invoicedVehicle["gsc_productionno"] = prodNo;
                invoicedVehicle["gsc_csno"] = csNo;
                invoicedVehicle["gsc_engineno"] = engineNo;
                invoicedVehicle["gsc_vin"] = vin;

                _organizationService.Create(invoicedVehicle);

                //Update Invoice Vehiclee Unique Details
                Entity invoiceToUpdate = _organizationService.Retrieve(invoiceEntity.LogicalName, invoiceEntity.Id,
                    new ColumnSet("gsc_productionno", "gsc_csno", "gsc_engineno", "gsc_vin"));

                invoiceToUpdate["gsc_productionno"] = prodNo;
                invoiceToUpdate["gsc_csno"] = csNo;
                invoiceToUpdate["gsc_engineno"] = engineNo;
                invoiceToUpdate["gsc_vin"] = vin;

                _organizationService.Update(invoiceToUpdate);
            }

            _tracingService.Trace("Ended ReplicateAllocatedVehicle method..");
            return invoiceEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/17/2016
        /*Purpose: Replicate Order Monthly Amortization into Invoice Monthly Amortization
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create: Order ID = salesorderid
         * Primary Entity: Invoice
         */
        public Entity ReplicateMonthlyAmortizations(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateMonthlyAmortizations method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection orderMonthlyAmortizationRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_ordermonthlyamortization", "gsc_orderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_branchid", "gsc_dealerid", "gsc_recordownerid", "gsc_selected", "gsc_orderid", "gsc_financingtermid", "gsc_ordermonthlyamortizationpn" });

            if (orderMonthlyAmortizationRecords != null && orderMonthlyAmortizationRecords.Entities.Count > 0)
            {
                foreach (Entity orderMonthlyAmortization in orderMonthlyAmortizationRecords.Entities)
                {
                    Entity invoiceMonthlyAmortization = new Entity("gsc_sls_invoicemonthlyamortization");
                    invoiceMonthlyAmortization["gsc_invoiceid"] = new EntityReference("invoice", invoiceEntity.Id);
                    invoiceMonthlyAmortization["gsc_branchid"] = orderMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_branchid") != null
                        ? new EntityReference("account", orderMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                        : null;
                    invoiceMonthlyAmortization["gsc_dealerid"] = orderMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                        ? new EntityReference("account", orderMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                        : null;
                    invoiceMonthlyAmortization["gsc_recordownerid"] = orderMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                        ? new EntityReference("contact", orderMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                        : null;
                    invoiceMonthlyAmortization["gsc_selected"] = orderMonthlyAmortization.GetAttributeValue<Boolean>("gsc_selected");
                    invoiceMonthlyAmortization["gsc_financingtermid"] = orderMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_financingtermid") != null
                        ? new EntityReference("gsc_sls_financingterm", orderMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_financingtermid").Id)
                        : null;
                    invoiceMonthlyAmortization["gsc_invoicemonthlyamortizationpn"] = orderMonthlyAmortization.GetAttributeValue<String>("gsc_ordermonthlyamortizationpn") != null
                        ? orderMonthlyAmortization.GetAttributeValue<String>("gsc_ordermonthlyamortizationpn")
                        : String.Empty;

                    _organizationService.Create(invoiceMonthlyAmortization);
                }                
            }

            _tracingService.Trace("Ended ReplicateMonthlyAmortizations method..");
            return invoiceEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/17/2016
        /*Purpose: Replicate Order Discount records into Invoice Discount entity
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create: Order ID = salesorderid
         * Primary Entity: Invoice
         */
        public Entity ReplicateDiscount(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateDiscount method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection orderDiscountRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_salesorderdiscount", "gsc_salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_branchid", "gsc_dealerid", "gsc_recordownerid", "gsc_salesorderdiscountpn", "gsc_pricelistid", "gsc_promo", "gsc_begindate", "gsc_discountamount",
                "gsc_applypercentagetodp", "gsc_applypercentagetoaf", "gsc_applypercentagetoup", "gsc_enddate", "gsc_discountpercentage", "gsc_description",
                "gsc_applyamounttodp", "gsc_applyamounttoaf", "gsc_applyamounttoup", "gsc_forclaims" });

            if (orderDiscountRecords != null && orderDiscountRecords.Entities.Count > 0)
            {
                foreach (Entity orderDiscount in orderDiscountRecords.Entities)
                {
                    Entity invoiceDiscount = new Entity("gsc_cmn_invoicediscount");
                    invoiceDiscount["gsc_invoiceid"] = new EntityReference("invoice", invoiceEntity.Id);
                    invoiceDiscount["gsc_branchid"] = orderDiscount.GetAttributeValue<EntityReference>("gsc_branchid") != null
                        ? new EntityReference("account", orderDiscount.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                        : null;
                    invoiceDiscount["gsc_dealerid"] = orderDiscount.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                        ? new EntityReference("account", orderDiscount.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                        : null;
                    invoiceDiscount["gsc_recordownerid"] = orderDiscount.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                        ? new EntityReference("contact", orderDiscount.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                        : null;
                    invoiceDiscount["gsc_invoicediscountpn"] = orderDiscount.GetAttributeValue<String>("gsc_orderdiscountpn") != null
                        ? orderDiscount.GetAttributeValue<String>("gsc_orderdiscountpn")
                        : null;
                    invoiceDiscount["gsc_pricelistid"] = orderDiscount.GetAttributeValue<EntityReference>("gsc_pricelistid") != null
                        ? new EntityReference("pricelevel", orderDiscount.GetAttributeValue<EntityReference>("gsc_pricelistid").Id)
                        : null;
                    invoiceDiscount["gsc_promo"] = orderDiscount.GetAttributeValue<Boolean>("gsc_promo");
                    invoiceDiscount["gsc_begindate"] = orderDiscount.Contains("gsc_begindate")
                        ? orderDiscount.GetAttributeValue<DateTime?>("gsc_begindate")
                        : (DateTime?)null;
                    invoiceDiscount["gsc_discountamount"] = orderDiscount.GetAttributeValue<Money>("gsc_discountamount") != null
                        ? new Money(orderDiscount.GetAttributeValue<Money>("gsc_discountamount").Value)
                        : new Money(Decimal.Zero);
                    invoiceDiscount["gsc_applypercentagetodp"] = orderDiscount.GetAttributeValue<Double>("gsc_applypercentagetodp") != null
                        ? orderDiscount.GetAttributeValue<Double>("gsc_applypercentagetodp")
                        : 0;
                    invoiceDiscount["gsc_applypercentagetoaf"] = orderDiscount.GetAttributeValue<Double>("gsc_applypercentagetoaf") != null
                        ? orderDiscount.GetAttributeValue<Double>("gsc_applypercentagetoaf")
                        : 0;
                    invoiceDiscount["gsc_applypercentagetoup"] = orderDiscount.GetAttributeValue<Double>("gsc_applypercentagetoup") != null
                        ? orderDiscount.GetAttributeValue<Double>("gsc_applypercentagetoup")
                        : 0;
                    invoiceDiscount["gsc_enddate"] = orderDiscount.Contains("gsc_enddate")
                        ? orderDiscount.GetAttributeValue<DateTime?>("gsc_enddate")
                        : (DateTime?)null;
                    invoiceDiscount["gsc_discountpercentage"] = orderDiscount.GetAttributeValue<Double>("gsc_discountpercentage") != null
                        ? orderDiscount.GetAttributeValue<Double>("gsc_discountpercentage")
                        : 0;
                    invoiceDiscount["gsc_description"] = orderDiscount.GetAttributeValue<String>("gsc_description") != null
                        ? orderDiscount.GetAttributeValue<String>("gsc_description")
                        : null;
                    invoiceDiscount["gsc_applyamounttodp"] = orderDiscount.GetAttributeValue<Money>("gsc_applyamounttodp") != null
                        ? new Money(orderDiscount.GetAttributeValue<Money>("gsc_applyamounttodp").Value)
                        : new Money(Decimal.Zero);
                    invoiceDiscount["gsc_applyamounttoaf"] = orderDiscount.GetAttributeValue<Money>("gsc_applyamounttoaf") != null
                        ? new Money(orderDiscount.GetAttributeValue<Money>("gsc_applyamounttoaf").Value)
                        : new Money(Decimal.Zero);
                    invoiceDiscount["gsc_applyamounttoup"] = orderDiscount.GetAttributeValue<Money>("gsc_applyamounttoup") != null
                        ? new Money(orderDiscount.GetAttributeValue<Money>("gsc_applyamounttoup").Value)
                        : new Money(Decimal.Zero);
                    invoiceDiscount["gsc_forclaims"] = orderDiscount.GetAttributeValue<Boolean>("gsc_forclaims");
                    _organizationService.Create(invoiceDiscount);
                }
            }

            _tracingService.Trace("Ended ReplicateDiscount method..");
            return invoiceEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/17/2016
        /*Purpose: Replicate Order Coverage Available records into Invoice Coverage Available entity
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create: Order ID = salesorderid
         * Primary Entity: Invoice
         */
        public Entity ReplicateCoverageAvailable(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateCoverageAvailable method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection orderCoverageAvailableRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_ordercoverageavailable", "gsc_orderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_branchid", "gsc_dealerid", "gsc_recordownerid", "gsc_ordercoverageavailablepn", "gsc_suminsured", "gsc_premium" });

            if (orderCoverageAvailableRecords != null && orderCoverageAvailableRecords.Entities.Count > 0)
            {
                foreach (Entity orderCoverageAvailable in orderCoverageAvailableRecords.Entities)
                {
                    Entity invoiceCoverageAvailable = new Entity("gsc_cmn_invoicecoverageavailable");
                    invoiceCoverageAvailable["gsc_invoiceid"] = new EntityReference("invoice", invoiceEntity.Id);
                    invoiceCoverageAvailable["gsc_branchid"] = orderCoverageAvailable.GetAttributeValue<EntityReference>("gsc_branchid") != null
                        ? new EntityReference("account", orderCoverageAvailable.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                        : null;
                    invoiceCoverageAvailable["gsc_dealerid"] = orderCoverageAvailable.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                        ? new EntityReference("account", orderCoverageAvailable.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                        : null;
                    invoiceCoverageAvailable["gsc_recordownerid"] = orderCoverageAvailable.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                        ? new EntityReference("contact", orderCoverageAvailable.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                        : null;
                    invoiceCoverageAvailable["gsc_invoicecoverageavailablepn"] = orderCoverageAvailable.GetAttributeValue<String>("gsc_ordercoverageavailablepn") != null
                        ? orderCoverageAvailable.GetAttributeValue<String>("gsc_ordercoverageavailablepn")
                        : null;
                    invoiceCoverageAvailable["gsc_suminsured"] = orderCoverageAvailable.GetAttributeValue<Money>("gsc_suminsured") != null
                        ? new Money(orderCoverageAvailable.GetAttributeValue<Money>("gsc_suminsured").Value)
                        : new Money(Decimal.Zero);
                    invoiceCoverageAvailable["gsc_premium"] = orderCoverageAvailable.GetAttributeValue<Money>("gsc_premium") != null
                        ? new Money(orderCoverageAvailable.GetAttributeValue<Money>("gsc_premium").Value)
                        : new Money(Decimal.Zero);
                    _organizationService.Create(invoiceCoverageAvailable);
                }
            }

            _tracingService.Trace("Ended ReplicateCoverageAvailable method..");
            return invoiceEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/17/2016
        /*Purpose: Replicate Order Charges records into Invoice Charges entity
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create: Order ID = salesorderid
         * Primary Entity: Invoice
         */
        public Entity ReplicateCharge(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateCharge method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection orderChargeRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_ordercharge", "gsc_orderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_branchid", "gsc_dealerid", "gsc_recordownerid", "gsc_orderchargepn", "gsc_free", "gsc_chargesid", "gsc_chargetype", "gsc_description", "gsc_amount", "gsc_actualcost" });

            if (orderChargeRecords != null && orderChargeRecords.Entities.Count > 0)
            {
                foreach (Entity orderCharge in orderChargeRecords.Entities)
                {
                    Entity invoiceCharge = new Entity("gsc_cmn_invoicecharge");
                    invoiceCharge["gsc_invoiceid"] = new EntityReference("invoice", invoiceEntity.Id);
                    invoiceCharge["gsc_branchid"] = orderCharge.GetAttributeValue<EntityReference>("gsc_branchid") != null
                        ? new EntityReference("account", orderCharge.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                        : null;
                    invoiceCharge["gsc_dealerid"] = orderCharge.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                        ? new EntityReference("account", orderCharge.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                        : null;
                    invoiceCharge["gsc_recordownerid"] = orderCharge.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                        ? new EntityReference("contact", orderCharge.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                        : null;
                    invoiceCharge["gsc_invoicechargepn"] = orderCharge.GetAttributeValue<String>("gsc_orderchargepn") != null
                        ? orderCharge.GetAttributeValue<String>("gsc_orderchargepn")
                        : null;
                    invoiceCharge["gsc_free"] = orderCharge.GetAttributeValue<Boolean>("gsc_free");
                    invoiceCharge["gsc_chargesid"] = orderCharge.GetAttributeValue<EntityReference>("gsc_chargesid") != null
                        ? new EntityReference("gsc_cmn_charges", orderCharge.GetAttributeValue<EntityReference>("gsc_chargesid").Id)
                        : null;
                    _tracingService.Trace("chargesid...");
                    invoiceCharge["gsc_chargetype"] = orderCharge.Contains("gsc_chargetype")
                        ? orderCharge.FormattedValues["gsc_chargetype"]
                        : String.Empty;
                    _tracingService.Trace("chargetype...");
                    invoiceCharge["gsc_description"] = orderCharge.GetAttributeValue<String>("gsc_description") != null
                        ? orderCharge.GetAttributeValue<String>("gsc_description")
                        : null;
                    invoiceCharge["gsc_amount"] = orderCharge.GetAttributeValue<Money>("gsc_amount") != null
                        ? new Money(orderCharge.GetAttributeValue<Money>("gsc_amount").Value)
                        : new Money(Decimal.Zero);
                    invoiceCharge["gsc_actualcost"] = orderCharge.GetAttributeValue<Money>("gsc_actualcost") != null
                        ? new Money(orderCharge.GetAttributeValue<Money>("gsc_actualcost").Value)
                        : new Money(Decimal.Zero);
                    _organizationService.Create(invoiceCharge);
                }
            }

            _tracingService.Trace("Ended ReplicateCharge method..");
            return invoiceEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/17/2016
        /*Purpose: Replicate Order Requirement Checklist records into Invoice Requirement Checklist entity
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create: Order ID = salesorderid
         * Primary Entity: Invoice
         */
        public Entity ReplicateRequirementChecklist(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateRequirementChecklist method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection orderRequirementChecklistRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_requirementchecklist", "gsc_orderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_branchid", "gsc_dealerid", "gsc_recordownerid", "gsc_requirementchecklistpn", "gsc_bankid", "gsc_documentchecklistid", "gsc_documenttype", "gsc_mandatory", 
                    "gsc_submitted", "gsc_datesubmitted", "gsc_submittedby" });

            if (orderRequirementChecklistRecords != null && orderRequirementChecklistRecords.Entities.Count > 0)
            {
                foreach (Entity orderRequirementChecklist in orderRequirementChecklistRecords.Entities)
                {
                    Entity invoiceRequirementChecklist = new Entity("gsc_sls_invoicerequirementchecklist");
                    invoiceRequirementChecklist["gsc_invoiceid"] = new EntityReference("invoice", invoiceEntity.Id);
                    invoiceRequirementChecklist["gsc_branchid"] = orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_branchid") != null
                        ? new EntityReference("account", orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                        : null;
                    invoiceRequirementChecklist["gsc_dealerid"] = orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                        ? new EntityReference("account", orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                        : null;
                    invoiceRequirementChecklist["gsc_recordownerid"] = orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                        ? new EntityReference("contact", orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                        : null;
                    invoiceRequirementChecklist["gsc_requirementchecklistpn"] = orderRequirementChecklist.GetAttributeValue<String>("gsc_requirementchecklistpn") != null
                        ? orderRequirementChecklist.GetAttributeValue<String>("gsc_requirementchecklistpn")
                        : null;
                    invoiceRequirementChecklist["gsc_bankid"] = orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_bankid") != null
                        ? new EntityReference("gsc_sls_bank", orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_bankid").Id)
                        : null;
                    invoiceRequirementChecklist["gsc_documentchecklistid"] = orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_documentchecklistid") != null
                        ? new EntityReference("gsc_sls_documentchecklist", orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_documentchecklistid").Id)
                        : null;
                    invoiceRequirementChecklist["gsc_documenttype"] = orderRequirementChecklist.GetAttributeValue<Boolean>("gsc_documenttype");
                    invoiceRequirementChecklist["gsc_mandatory"] = orderRequirementChecklist.GetAttributeValue<Boolean>("gsc_mandatory");
                    invoiceRequirementChecklist["gsc_submitted"] = orderRequirementChecklist.GetAttributeValue<Boolean>("gsc_submitted");
                    invoiceRequirementChecklist["gsc_datesubmitted"] = orderRequirementChecklist.Contains("gsc_datesubmitted")
                        ? orderRequirementChecklist.GetAttributeValue<DateTime?>("gsc_datesubmitted")
                        : (DateTime?)null;
                    invoiceRequirementChecklist["gsc_submittedbyid"] = orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_submittedby") != null
                        ? new EntityReference("contact", orderRequirementChecklist.GetAttributeValue<EntityReference>("gsc_submittedby").Id)
                        : null;
                    _organizationService.Create(invoiceRequirementChecklist);
                }
            }

            _tracingService.Trace("Ended ReplicateRequirementChecklist method..");
            return invoiceEntity;
        }

        //Created By: Leslie Baliguat, Created On: 10/06/2016
        public void ReplicateAccessories(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateAccessories method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection accessoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_orderaccessory", "gsc_orderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_itemnumber", "gsc_free", "gsc_productid", "gsc_amount" });

            if (accessoryCollection != null && accessoryCollection.Entities.Count > 0)
            {
                foreach (Entity accessory in accessoryCollection.Entities)
                {
                    Entity invoiceAccessory = new Entity("gsc_sls_invoiceaccessory");
                    invoiceAccessory["gsc_invoiceid"] = new EntityReference(invoiceEntity.LogicalName, invoiceEntity.Id);
                    invoiceAccessory["gsc_itemnumber"] = accessory.Contains("gsc_itemnumber")
                        ? accessory.GetAttributeValue<String>("gsc_itemnumber")
                        : String.Empty;
                    invoiceAccessory["gsc_free"] = accessory.GetAttributeValue<Boolean>("gsc_free");
                    invoiceAccessory["gsc_productid"] = accessory.GetAttributeValue<EntityReference>("gsc_productid") != null
                        ? accessory.GetAttributeValue<EntityReference>("gsc_productid")
                        : null;
                    invoiceAccessory["gsc_amount"] = accessory.Contains("gsc_amount")
                        ? accessory.GetAttributeValue<Money>("gsc_amount")
                        : new Money(0);
                    _organizationService.Create(invoiceAccessory);
                }
            }
        }

        //Created By: Leslie Baliguat, Created On: 10/06/2016
        public void ReplicateCabChassis(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateAccessories method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection cabChassisCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_ordercabchassis", "gsc_orderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_itemnumber", "gsc_financing", "gsc_vehiclecabchassisid", "gsc_amount" });

            if (cabChassisCollection != null && cabChassisCollection.Entities.Count > 0)
            {
                foreach (Entity cabChassis in cabChassisCollection.Entities)
                {
                    Entity invoiceCabChassis = new Entity("gsc_invoicecabchassis");
                    invoiceCabChassis["gsc_invoiceid"] = new EntityReference(invoiceEntity.LogicalName, invoiceEntity.Id);
                    invoiceCabChassis["gsc_itemnumber"] = cabChassis.Contains("gsc_itemnumber")
                        ? cabChassis.GetAttributeValue<String>("gsc_itemnumber")
                        : String.Empty;
                    invoiceCabChassis["gsc_financing"] = cabChassis.GetAttributeValue<Boolean>("gsc_financing");
                    invoiceCabChassis["gsc_vehiclecabchassisid"] = cabChassis.GetAttributeValue<EntityReference>("gsc_vehiclecabchassisid") != null
                        ? cabChassis.GetAttributeValue<EntityReference>("gsc_vehiclecabchassisid")
                        : null;
                    invoiceCabChassis["gsc_amount"] = cabChassis.Contains("gsc_amount")
                        ? cabChassis.GetAttributeValue<Money>("gsc_amount")
                        : new Money(0);
                    _organizationService.Create(invoiceCabChassis);
                }
            }
        }

        //Created By: Leslie Baliguat, Created On: 10/07/2016
        public void ChangeSOInvoiceCreatedDate(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ChangeSOInvoiceCreatedDate method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_createddate" });

            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];

                salesOrder["gsc_createddate"] = DateTime.UtcNow;
                _organizationService.Update(salesOrder);
            }
            _tracingService.Trace("Ended ChangeSOInvoiceCreatedDate method..");
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/18/2016
        //Modify By : Artum Ramos, Created On : 1/16/2017
        public Entity SetOrderCancelledStatus(Entity invoiceEntity)
        {
            _tracingService.Trace("Started SetOrderCancelledStatus method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            _tracingService.Trace("retrieve sales order record.");
            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_status", "gsc_invoicecancelleddate" });

            _tracingService.Trace("check if record is not equal to null.");
            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];

                salesOrder["gsc_status"] = new OptionSetValue(100000006);
                salesOrder["gsc_invoicecancelleddate"] = DateTime.UtcNow;
                _organizationService.Update(salesOrder);
            }
            _tracingService.Trace("Call SetInvoiceCancelledAfterOrderCancel method..");
            SetInvoiceCancelledAfterOrderCancel(invoiceEntity);
            _tracingService.Trace("Ended SetOrderCancelledStatus method..");
            return invoiceEntity;
            //throw new InvalidPluginExecutionException("Test SetOrderCancelledStatus");
        }
        //Create By : Artum Ramos, Created On : 1/16/2017
        //Method to update invoice status to Cancelled
        public Entity SetInvoiceCancelledAfterOrderCancel(Entity invoiceEntity)
        {
            _tracingService.Trace("Started SetInvoiceCancelledAfterOrderCancel method..");

            Entity invoiceToUpdate = _organizationService.Retrieve(invoiceEntity.LogicalName, invoiceEntity.Id,
                new ColumnSet("gsc_invoicestatuscopy", "gsc_cancelled"));

            _tracingService.Trace("update cancelled and invoicestatuscopy..");
            invoiceToUpdate["gsc_cancelled"] = true;
            invoiceToUpdate["gsc_invoicestatuscopy"] = new OptionSetValue(100000005);

            _tracingService.Trace("update invoicettoupdate..");
            _organizationService.Update(invoiceToUpdate);

            return invoiceEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/18/2016
        /*Purpose: Set Invoice status to "Cancelled"
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Cancelled = gsc_cancelled
         *      Post/Create:
         * Primary Entity: Invoice
         */
        public Entity SetInvoiceCancelledStatus(Entity invoiceEntity)
        {
            if (!invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Cancelled")) { return null; }

            _tracingService.Trace("Started SetInvoiceCancelledStatus method..");

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_status", "gsc_iscreateinvoice" });

            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];

                //Entity orderToUpdate = _organizationService.Retrieve(salesOrder.LogicalName, salesOrder.Id,
                 //   new ColumnSet("gsc_status"));

                salesOrder["gsc_status"] = new OptionSetValue(100000004);
                salesOrder["gsc_iscreateinvoice"] = false;
                _organizationService.Update(salesOrder);
            }


            SetStateRequest setStateRequest = new SetStateRequest()
            {
                EntityMoniker = new EntityReference
                {
                    Id = invoiceEntity.Id,
                    LogicalName = invoiceEntity.LogicalName
                },
                State = new OptionSetValue(3),
                Status = new OptionSetValue(100003)
            };
            SetStateResponse setStateReponse = (SetStateResponse)_organizationService.Execute(setStateRequest);

            _tracingService.Trace("Ended SetInvoiceCancelledStatus method..");
            return invoiceEntity;
            //throw new InvalidPluginExecutionException("Test SetInvoiceCancelledStatus");
        }

        //Created By : Jerome Anthony Gerero, Created On : 1/21/2017
        /*Purpose: - Set related SO field 'isCreateInvoice' to false on delete.
         *         - Delete related detail records.
         * Registration Details:
         * Event/Message: 
         *      Pre-Validation/Delete: invoiceid
         * Primary Entity: Invoice
         */
        public Entity DeleteOpenInvoice(Entity invoiceEntity)
        {
            if (!invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Open")) { return null; }

            _tracingService.Trace("Started DeleteOpenInvoice method..");

            Guid salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            //Retrieve related Sales Order
            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_status", "gsc_iscreateinvoice" });

            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];

                salesOrder["gsc_status"] = new OptionSetValue(100000004);
                salesOrder["gsc_iscreateinvoice"] = false;

                _organizationService.Update(salesOrder);
            }

            //Retrieve related Invoiced Vehicle
            EntityCollection invoicedVehicleCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_invoicedvehicle", "gsc_invoiceid", invoiceEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_invoiceid" });

            if (invoicedVehicleCollection != null && invoicedVehicleCollection.Entities.Count > 0)
            {
                foreach (var invoicedVehicle in invoicedVehicleCollection.Entities)
                {
                    _organizationService.Delete(invoicedVehicle.LogicalName, invoicedVehicle.Id);
                }
            }

            _tracingService.Trace("Ended DeleteOpenInvoice method..");
            return invoiceEntity;
        }

        //Create By: Leslie Baliguat, Created On: 10/06/2016
        public void DeleteInvoicedVehicle(Entity invoiceEntity)
        {
            if (invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Cancelled"))
            {
                //Retrieve Contact record from Customer ID field value
                EntityCollection invoicedVehicleCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_invoicedvehicle", "gsc_invoiceid", invoiceEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_invoiceid" });

                if (invoicedVehicleCollection != null && invoicedVehicleCollection.Entities.Count > 0)
                {
                    foreach (var invoicedVehicle in invoicedVehicleCollection.Entities)
                    {
                        _organizationService.Delete(invoicedVehicle.LogicalName, invoicedVehicle.Id);
                    }
                }
            }
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/19/2016
        /*Purpose: Create Vehicle Claim record
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Sales Invoice Status = gsc_salesinvoicestatus
         *      Post/Create:
         * Primary Entity: Invoice
         */
        public Entity CreateVehicleClaimAndDiscounts(Entity invoiceEntity)
        {
            _tracingService.Trace("Started CreateVehicleClaimAndDiscounts method..");

            if (!invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Invoiced")) { return null; }

            //Custom filter for Invoice Discount
            var invoiceDiscountConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_invoiceid", ConditionOperator.Equal, invoiceEntity.Id),
                    new ConditionExpression("gsc_forclaims", ConditionOperator.Equal, true)
                };

            EntityCollection invoiceDiscountRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_invoicediscount", invoiceDiscountConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_description", "gsc_discountamount" });

            if (invoiceDiscountRecords != null && invoiceDiscountRecords.Entities.Count > 0)
            {
                Entity vehicleClaim = new Entity("gsc_sls_vehicleclaim");

                vehicleClaim["gsc_vehicleclaimpn"] = invoiceEntity.GetAttributeValue<String>("name") != null
                    ? invoiceEntity.GetAttributeValue<String>("name")
                    : String.Empty;
                vehicleClaim["gsc_vsistatus"] = invoiceEntity.FormattedValues["gsc_salesinvoicestatus"];
                vehicleClaim["gsc_invoiceid"] = new EntityReference("invoice", invoiceEntity.Id);
                vehicleClaim["gsc_invoicedate"] = invoiceEntity.Contains("createdon")
                    ? invoiceEntity.GetAttributeValue<DateTime?>("createdon")
                    : (DateTime?)null;
                vehicleClaim["gsc_customerid"] = invoiceEntity.GetAttributeValue<EntityReference>("customerid");
                vehicleClaim["gsc_salesexecutiveid"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid") != null
                    ? new EntityReference("contact", invoiceEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id)
                    : null;
                vehicleClaim["gsc_modelcode"] = invoiceEntity.GetAttributeValue<String>("gsc_modelcode") != null
                    ? invoiceEntity.GetAttributeValue<String>("gsc_modelcode")
                    : null;
                vehicleClaim["gsc_productid"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? new EntityReference("product", invoiceEntity.GetAttributeValue<EntityReference>("gsc_productid").Id)
                    : null;
                vehicleClaim["gsc_colorid"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_colorid") != null
                    ? new EntityReference("gsc_iv_color", invoiceEntity.GetAttributeValue<EntityReference>("gsc_colorid").Id)
                    : null;
                vehicleClaim["gsc_productionno"] = invoiceEntity.GetAttributeValue<String>("gsc_productionno") != null
                    ? invoiceEntity.GetAttributeValue<String>("gsc_productionno")
                    : null;
                vehicleClaim["gsc_csno"] = invoiceEntity.GetAttributeValue<String>("gsc_csno") != null
                    ? invoiceEntity.GetAttributeValue<String>("gsc_csno")
                    : null;
                vehicleClaim["gsc_engineno"] = invoiceEntity.GetAttributeValue<String>("gsc_engineno") != null
                    ? invoiceEntity.GetAttributeValue<String>("gsc_engineno")
                    : null;
                vehicleClaim["gsc_vin"] = invoiceEntity.GetAttributeValue<String>("gsc_vin") != null
                    ? invoiceEntity.GetAttributeValue<String>("gsc_vin")
                    : null;
                vehicleClaim["gsc_status"] = new OptionSetValue(100000000);
                vehicleClaim["gsc_statuscopy"] = new OptionSetValue(100000000);

                var vehicleClaimId = _organizationService.Create(vehicleClaim);

                foreach (Entity invoiceDiscount in invoiceDiscountRecords.Entities)
                {
                    Entity vehicleClaimDiscount = new Entity("gsc_sls_vehicleclaimdiscount");

                    vehicleClaimDiscount["gsc_vehicleclaimid"] = new EntityReference("gsc_sls_vehicleclaim", vehicleClaimId);
                    vehicleClaimDiscount["gsc_vehicleclaimdiscountpn"] = invoiceDiscount.GetAttributeValue<String>("gsc_description") != null
                        ? invoiceDiscount.GetAttributeValue<String>("gsc_description")
                        : String.Empty;
                    vehicleClaimDiscount["gsc_amount"] = invoiceDiscount.GetAttributeValue<Money>("gsc_discountamount") != null
                        ? new Money(invoiceDiscount.GetAttributeValue<Money>("gsc_discountamount").Value)
                        : new Money(Decimal.Zero);

                    _organizationService.Create(vehicleClaimDiscount);
                }         
            }
            _tracingService.Trace("Ended CreateVehicleClaimAndDiscounts method..");
            return invoiceEntity;
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 5/23/2016
        /*Purpose: Updated SI and SO fields and create VPD record on status change
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Sales Invoice Status = gsc_salesinvoicestatus
         *      Post/Create:
         * Primary Entity: Invoice
         */
        public Entity PostInvoice(Entity invoiceEntity)
        {
            _tracingService.Trace("Started PostInvoice method..");

            if (!invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Released")) { return null; }

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_releaseddate" });
          
            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];
                salesOrder["gsc_releaseddate"] = DateTime.UtcNow;

                _organizationService.Update(salesOrder);
            }

            invoiceEntity["gsc_releaseddate"] = DateTime.UtcNow;
            _organizationService.Update(invoiceEntity);

            //Set all fields to read only by changing standard invoice status to "Paid"
            SetStateRequest setStateRequest = new SetStateRequest()
            {
                EntityMoniker = new EntityReference
                {
                    Id = invoiceEntity.Id,
                    LogicalName = invoiceEntity.LogicalName
                },
                State = new OptionSetValue(2),
                Status = new OptionSetValue(100001)
            };
            _organizationService.Execute(setStateRequest);

            //Create Vehicle Post-Delivery Monitoring record
            Entity vehiclePostDeliveryMonitoring = new Entity("gsc_sls_vehiclepostdeliverymonitoring");
            vehiclePostDeliveryMonitoring["gsc_dealerid"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                ? new EntityReference("account", invoiceEntity.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                : null;
            vehiclePostDeliveryMonitoring["gsc_branchid"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? new EntityReference("account", invoiceEntity.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                : null;
            vehiclePostDeliveryMonitoring["gsc_recordownerid"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                ? new EntityReference("contact", invoiceEntity.GetAttributeValue<EntityReference>("gsc_recordownerid").Id)
                : null;
            vehiclePostDeliveryMonitoring["gsc_customer"] = invoiceEntity.GetAttributeValue<String>("gsc_customer") != null
                ? invoiceEntity.GetAttributeValue<String>("gsc_customer")
                : String.Empty;
            vehiclePostDeliveryMonitoring["gsc_customername"] = invoiceEntity.GetAttributeValue<EntityReference>("customerid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("customerid").Name
                : String.Empty;
            vehiclePostDeliveryMonitoring["gsc_postdeliverystatus"] = new OptionSetValue(100000000);
            
            String mobileNo, emailAddress;
            Entity customer = _organizationService.Retrieve(invoiceEntity.GetAttributeValue<EntityReference>("customerid").LogicalName, invoiceEntity.GetAttributeValue<EntityReference>("customerid").Id, new ColumnSet(true));

            mobileNo = customer.GetAttributeValue<String>("mobilephone") != null
                ? customer.GetAttributeValue<String>("mobilephone")
                : customer.GetAttributeValue<String>("telephone1") != null
                    ? customer.GetAttributeValue<String>("telephone1")
                    : String.Empty;
            emailAddress = customer.GetAttributeValue<String>("emailaddress1") != null
                ? customer.GetAttributeValue<String>("emailaddress1")
                : String.Empty;

            vehiclePostDeliveryMonitoring["gsc_mobileno"] = mobileNo;
            vehiclePostDeliveryMonitoring["gsc_emailaddress"] = emailAddress;
            vehiclePostDeliveryMonitoring["gsc_address"] = invoiceEntity.GetAttributeValue<String>("gsc_address") != null
                ? invoiceEntity.GetAttributeValue<String>("gsc_address")
                : null;
            vehiclePostDeliveryMonitoring["gsc_invoiceid"] = new EntityReference(invoiceEntity.LogicalName, invoiceEntity.Id);
            vehiclePostDeliveryMonitoring["gsc_modeldescription"] = invoiceEntity.GetAttributeValue<String>("gsc_modeldescription") != null
                ? invoiceEntity.GetAttributeValue<String>("gsc_modeldescription")
                : null;
            vehiclePostDeliveryMonitoring["gsc_csno"] = invoiceEntity.GetAttributeValue<String>("gsc_csno") != null
                ? invoiceEntity.GetAttributeValue<String>("gsc_csno")
                : null;
            vehiclePostDeliveryMonitoring["gsc_releaseddate"] = DateTime.UtcNow;
            vehiclePostDeliveryMonitoring["gsc_salesexecutiveid"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid") != null
                ? new EntityReference("contact", invoiceEntity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id)
                : null;
            //Modified By : Raphael Herrera, Modified On : 6/7/2016
            /*Purpose: Included gsc_expectedcalldated to fields created
             */
            var branchId = invoiceEntity.Contains("gsc_branchid") ? invoiceEntity.GetAttributeValue<EntityReference>("gsc_branchid").Id
                    : Guid.Empty;

            EntityCollection postDeliveryMonitoringAdminsitration = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_postdeliveryadministration", "gsc_branchid", branchId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_daysaftersales" });

            if (postDeliveryMonitoringAdminsitration.Entities.Count > 0)
            {
                int afterSalesDays = postDeliveryMonitoringAdminsitration.Entities[0].GetAttributeValue<int>("gsc_daysaftersales");
                DateTime expectedCallDate = DateTime.UtcNow.AddDays(afterSalesDays);

                _tracingService.Trace("Expected Call Date - " + afterSalesDays.ToString() + " " + expectedCallDate.Date.ToString());

                vehiclePostDeliveryMonitoring["gsc_expectedcalldate"] = expectedCallDate.Date;
            }
            //************* End of inserted code *****************//

            _organizationService.Create(vehiclePostDeliveryMonitoring);

            _tracingService.Trace("Ended PostInvoice method..");
            return invoiceEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 6/1/2016
        /*Purpose: Updated SI and SO fields
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Sales Invoice Status = gsc_salesinvoicestatus
         *      Post/Create:
         * Primary Entity: Invoice
         */
        public Entity UpdateInvoicedStatus(Entity invoiceEntity)
        {
            _tracingService.Trace("Started UpdateInvoice method..");

            if (!invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Invoiced")) { return null; }

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new [] { "gsc_status", "gsc_invoicedate" });

            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];
                salesOrder["gsc_invoicedate"] = DateTime.UtcNow;
                salesOrder["gsc_status"] = new OptionSetValue(100000005);
                _organizationService.Update(salesOrder);
            }

            invoiceEntity["gsc_invoicedate"] = DateTime.UtcNow;
            _organizationService.Update(invoiceEntity);

            _tracingService.Trace("Started UpdateInvoice method..");
            return invoiceEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 6/6/2016
        /*Purpose: Updated SI and SO fields
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Sales Invoice Status = gsc_salesinvoicestatus
         *      Post/Create:
         * Primary Entity: Invoice
         */
        public Entity UpdatedPrintedStatus(Entity invoiceEntity)
        {
            _tracingService.Trace("Started UpdatedPrintedStatus method..");

            if (!invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Printed")) { return null; }

            var salesOrderId = invoiceEntity.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection salesOrderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", salesOrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_printeddrdate" });

            if (salesOrderRecords != null && salesOrderRecords.Entities.Count > 0)
            {
                Entity salesOrder = salesOrderRecords.Entities[0];
                salesOrder["gsc_printeddrdate"] = DateTime.UtcNow;
                _organizationService.Update(salesOrder);
            }

            invoiceEntity["gsc_printeddrdate"] = DateTime.UtcNow;
            _organizationService.Update(invoiceEntity);

            _tracingService.Trace("Ended UpdatedPrintedStatus method..");
            return invoiceEntity;
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 6/8/2016
        /*Purpose: Adjust allocated product quantity
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Sales Invoice Status = gsc_salesinvoicestatus
         *      Post/Create:
         * Primary Entity: Invoice
         */
        public Entity AdjustProductQuantity(Entity invoiceEntity)
        {
            _tracingService.Trace("Started AdjustProductQuantity method..");

            if (!invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Released")) { return null; }

            EntityCollection invoicedVehicleRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_invoicedvehicle", "gsc_invoiceid", invoiceEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_invoicedvehiclepn", "gsc_inventoryid" });

            String customerId = invoiceEntity.Contains("gsc_customer") ? invoiceEntity.GetAttributeValue<String>("gsc_customer") : String.Empty;
            String customerName = invoiceEntity.GetAttributeValue<EntityReference>("customerid") != null
                ? invoiceEntity.GetAttributeValue<EntityReference>("customerid").Name
                : String.Empty;
            String transactionNumber = invoiceEntity.Contains("name") 
                ? invoiceEntity.GetAttributeValue<String>("name") : String.Empty;
            DateTime transactionDate = invoiceEntity.Contains("gsc_releaseddate")
                ? invoiceEntity.GetAttributeValue<DateTime>("gsc_releaseddate").Date : DateTime.MinValue;

            if (invoicedVehicleRecords != null && invoicedVehicleRecords.Entities.Count > 0)
            {
                Entity invoicedVehicle = invoicedVehicleRecords.Entities[0];
                Guid inventoryId = invoicedVehicle.GetAttributeValue<EntityReference>("gsc_inventoryid") != null
                    ? invoicedVehicle.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                    : Guid.Empty;

                //Retrieve Allocated Vehicle's Inventory record
                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_productquantityid", "gsc_status", "gsc_modelcode", "gsc_optioncode", "gsc_color", "gsc_csno", "gsc_engineno", "gsc_productionno", "gsc_vin", "gsc_modelyear", "gsc_siteid", "gsc_productid", "gsc_basemodelid" });
                
                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    Entity inventory = inventoryRecords.Entities[0];
                     Guid productQuantityId = inventory.Contains("gsc_productquantityid")
                    ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid").Id
                    : Guid.Empty;

                    EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
                    new[] {"gsc_onhand", "gsc_siteid", "gsc_vehiclecolorid", "gsc_vehiclemodelid", "gsc_productid" });

                    InventoryMovementHandler inventoryMovementHandler = new InventoryMovementHandler(_organizationService, _tracingService);
                    inventoryMovementHandler.UpdateInventoryStatus(inventory, 100000002);
                    inventoryMovementHandler.UpdateProductQuantity(inventory, -1, 0, -1, 0, 1, 0, 0, 0);

                    if(productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                    {
                        Entity productQuantity = productQuantityRecords.Entities[0];
                        Int32 balance = productQuantity.Contains("gsc_onhand") ? productQuantity.GetAttributeValue<Int32>("gsc_onhand") - 1 : 0;
                        Guid fromSite = productQuantity.Contains("gsc_siteid") ? productQuantity.GetAttributeValue<EntityReference>("gsc_siteid").Id : Guid.Empty;
                        //create inventory history log
                        inventoryMovementHandler.CreateInventoryHistory("Vehicle Sales Invoice",customerId,customerName,transactionNumber,transactionDate,1,0,balance,Guid.Empty,fromSite,fromSite,inventory,productQuantity,true,true);

                       
                        inventoryMovementHandler.CreateInventoryQuantityAllocated(invoiceEntity, inventory, productQuantity, invoiceEntity.GetAttributeValue<string>("name"),
                        DateTime.UtcNow, "Released", Guid.Empty, 100000009);
                    }
                }
            }

            _tracingService.Trace("Ended AdjustProductQuantity method..");
            return invoiceEntity;
        }

        //Created By:  Leslie Baliguat, Created On: 10/06/2016
        public void UpdateStatus(Entity invoiceEntity)
        {
            _tracingService.Trace("Started UpdateStatus Method...");

            Entity invoicceToUpdate = _organizationService.Retrieve(invoiceEntity.LogicalName, invoiceEntity.Id,
                new ColumnSet("gsc_salesinvoicestatus"));

            invoicceToUpdate["gsc_salesinvoicestatus"] = invoiceEntity.Contains("gsc_invoicestatuscopy")
                ? invoiceEntity.GetAttributeValue<OptionSetValue>("gsc_invoicestatuscopy")
                : null;

            _organizationService.Update(invoicceToUpdate);



            _tracingService.Trace("Ending UpdateStatus Method");
        }

        //Created By : Jessica Casupanan, Created On : 11/23/2016
        /*Purpose: Validate if the record can be deleted based on status
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create: 
         * Primary Entity: Invoice.
         */
        public bool ValidateDelete(Entity Invoice)
        {
            _tracingService.Trace("Started ValidateDelete Method...");
            var invoicestatus = Invoice.Contains("gsc_salesinvoicestatus") ? Invoice.GetAttributeValue<OptionSetValue>("gsc_salesinvoicestatus").Value
                   : 0;
            return (invoicestatus != 100000000);
        }

        //Created By : Jessica Casupanan, Created On : 12/07/2016
        //Modified By: Artum M. Ramos, Modified On : 1/21/2017  --- add Conduction No
        /*Purpose: Create purchased/service vehicle (transacted vehicle) record in customer form 
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Sales Invoice Status = gsc_salesinvoicestatus
         *      Post/Create:
         * Primary Entity: Invoice
         */
        public void CreateTransactedVehicle(Entity invoiceEntity)
        {
            _tracingService.Trace("Started CreateTransactedVehicle method..");
            Entity transactedVehicle = new Entity("gsc_cmn_transactedvehicle");

            #region Retrieve invoiced vehicle record
            var invoicedVehicleCondition = new List<ConditionExpression>
            {
                new ConditionExpression("gsc_invoiceid", ConditionOperator.Equal, invoiceEntity.Id)
            };

            EntityCollection invoicedVehicleCollection = CommonHandler.RetrieveRecordsByConditions("gsc_iv_invoicedvehicle", invoicedVehicleCondition, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_engineno", "gsc_vin", "gsc_color", "gsc_csno" });
            if (invoicedVehicleCollection != null && invoicedVehicleCollection.Entities.Count > 0)
            {
                Entity invoicedVehicle = invoicedVehicleCollection.Entities[0];
                transactedVehicle["gsc_engineserialno"] = invoicedVehicle.Contains("gsc_engineno") ? invoicedVehicle.GetAttributeValue<string>("gsc_engineno") : string.Empty;
                transactedVehicle["gsc_vin"] = invoicedVehicle.Contains("gsc_vin") ? invoicedVehicle.GetAttributeValue<string>("gsc_vin") : string.Empty;
                transactedVehicle["gsc_color"] = invoicedVehicle.Contains("gsc_color") ? invoicedVehicle.GetAttributeValue<string>("gsc_color") : string.Empty;
                transactedVehicle["gsc_conductionno"] = invoicedVehicle.Contains("gsc_csno") ? invoicedVehicle.GetAttributeValue<string>("gsc_csno") : string.Empty;
                
                _tracingService.Trace("invoicedcollection");
            }
            else
            {
                _tracingService.Trace("No invoiced vehicle...");
            }
            #endregion

            #region Retrieve product record
            var productId = invoiceEntity.Contains("gsc_productid") ? invoiceEntity.GetAttributeValue<EntityReference>("gsc_productid").Id : Guid.Empty;
            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product","productid", productId, _organizationService, null, OrderType.Ascending,
            new[] { "productnumber", "gsc_vehiclemakeid", "gsc_vehiclemodelid", "name","gsc_warrantyexpirydays", "gsc_warrantymileage"});
            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                Entity product = productCollection.Entities[0];
                transactedVehicle["gsc_vehicleid"] = product.Contains("productnumber") ? product.GetAttributeValue<string>("productnumber") : string.Empty;
                transactedVehicle["gsc_vehiclemakeid"] = product.GetAttributeValue<EntityReference>("gsc_vehiclemakeid") != null ? new EntityReference("gsc_iv_vehiclemake", product.GetAttributeValue<EntityReference>("gsc_vehiclemakeid").Id) : null;
                transactedVehicle["gsc_vehiclebasemodelid"] = product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null ? new EntityReference("gsc_iv_vehiclebasemodel", product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id) : null;
                transactedVehicle["gsc_modeldescriptionpn"] = product.Contains("name") ? product.GetAttributeValue<string>("name") : string.Empty;
                transactedVehicle["gsc_warrantymileage"] =  product.Contains("gsc_warrantymileage") ? Int32.Parse(product.GetAttributeValue<string>("gsc_warrantymileage")) : 0;
                _tracingService.Trace("Product collection...");
            }
            else
            {
                _tracingService.Trace("No vehicle setup...");
            }
            #endregion

            #region Retrieve customer's classId record
            var customer = invoiceEntity.GetAttributeValue<EntityReference>("customerid") != null
                   ? invoiceEntity.GetAttributeValue<EntityReference>("customerid")
                   : null;
            EntityCollection customerRecords = null;
            if (customer != null)
            {
                customerRecords = CommonHandler.RetrieveRecordsByOneValue(customer.LogicalName, customer.LogicalName + "id", customer.Id, _organizationService, null, OrderType.Ascending,
                   new[] { "gsc_isfleet", customer.LogicalName + "id" });
            }

            if (customerRecords != null && customerRecords.Entities.Count > 0)
            {
                Entity customerEntity = customerRecords.Entities[0];
                if (customerEntity.Contains("accountid"))
                {
                    transactedVehicle["gsc_corporateid"] = customer.Id != null ? new EntityReference("account", customer.Id) : null;
                }
                else if (customerEntity.Contains("contactid"))
                {
                    transactedVehicle["gsc_customerid"] = customer.Id != null ? new EntityReference("contact", customer.Id) : null;
                }
                if (customerEntity.Contains("gsc_isfleet"))
                {
                    transactedVehicle["gsc_fleet"] = customerEntity.GetAttributeValue<Boolean>("gsc_isfleet") == true ? new OptionSetValue(100000000) : new OptionSetValue(100000001);
                    _tracingService.Trace("Fleet..." + customerEntity.GetAttributeValue<Boolean>("gsc_isfleet"));
                }
                else
                {
                    transactedVehicle["gsc_fleet"] = new OptionSetValue(100000001);
                    _tracingService.Trace("No Fleet...");
                }
            }
              #endregion

            transactedVehicle["gsc_releaseddate"] = invoiceEntity.Contains("gsc_releaseddate") ? invoiceEntity.GetAttributeValue<DateTime?>("gsc_releaseddate") : (DateTime?)null ;
            transactedVehicle["gsc_accountid"] = invoiceEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null 
                ? new EntityReference("account", invoiceEntity.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                : null;
            _organizationService.Create(transactedVehicle);

            _tracingService.Trace("Ended CreateTransactedVehicle method..");
        }
    
        //Created By : Jerome Anthony Gerero, Created On : 1/3/2017
        /*Purpose: Replicate generated sales invoice id to transaction/pro-forma number field
         * Registration Details: 
         * Event/Message:
         *      Post/Create: Sales Invoice ID = name
         * Primary Entity: Invoice
         */
        public Entity ReplicateSalesInvoiceId(Entity invoiceEntity)
        {
            _tracingService.Trace("Started ReplicateSalesInvoiceId method..");

            String salesInvoiceIdName = invoiceEntity.Contains("name")
                ? invoiceEntity.GetAttributeValue<String>("name")
                : String.Empty;

            invoiceEntity["gsc_transactionproformanumber"] = salesInvoiceIdName;

            _organizationService.Update(invoiceEntity);

            _tracingService.Trace("Ended ReplicateSalesInvoiceId method..");
            return invoiceEntity;
        }

        //Created By : Artum Ramos, Created On : 1/09/2017
        /*Purpose: Generate Delivery Receipt and Gate Pass NO\=
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update: Sales Invoice Status = gsc_isDeliveryReceiptandGatePass
         *      Post/Create:
         * Primary Entity: Invoice
         */

        #region Variable and Other Metho for Delivery Reciept and Gate Pass Method
        private static Random random = new Random();
        private System.Object lockThis = new System.Object();
        private const int MAX_RETRY = 2;
        private const double LONG_WAIT_SECONDS = 5;
        private const double SHORT_WAIT_SECONDS = 0.5;
        private static readonly TimeSpan longWait = TimeSpan.FromSeconds(LONG_WAIT_SECONDS);
        private static readonly TimeSpan shortWait = TimeSpan.FromSeconds(SHORT_WAIT_SECONDS);

        private enum RetryableSqlErrors
        {
            Timeout = -2,
            NoLock = 1204,
            Deadlock = 1205,
            WordbreakerTimeout = 30053,
        }
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        #endregion

        public void DeliveryReceiptandGatePassIdSequenceGen(Entity invoiceEntity)
        {

            _tracingService.Trace("Started DeliveryReceiptandGatePassIdSequenceGen Method...");
            int retryCount = 0;

            _tracingService.Trace("Retrive gsc_dealerid, gsc_branchid...");

            EntityCollection invoiceRecords = CommonHandler.RetrieveRecordsByOneValue("invoice", "invoiceid", invoiceEntity.Id, _organizationService, null, OrderType.Ascending,
                   new[] { "gsc_dealerid", "gsc_branchid", "invoiceid" });

            //query user based on context.initiatinguser (user logged on)
            //get dealer and branch
            if (invoiceRecords != null && invoiceRecords.Entities.Count > 0)
            {
                Entity invoice = invoiceRecords.Entities[0];
                _tracingService.Trace("InvoiceId " + invoice.Id.ToString());
                var dealerid = invoice.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                    ? invoice.GetAttributeValue<EntityReference>("gsc_dealerid").Id
                    : Guid.Empty;
                var branchid = invoice.GetAttributeValue<EntityReference>("gsc_branchid") != null
                    ? invoice.GetAttributeValue<EntityReference>("gsc_branchid").Id
                    : Guid.Empty;

                _tracingService.Trace("DealerId "+dealerid.ToString());
                _tracingService.Trace("BranchId " + branchid.ToString());
            _tracingService.Trace("Retrieve only the Guid of the ID Sequence Record of a specific Entity...");
            //Retrieve only the Guid of the ID Sequence Record of a specific Entity
            var sequenceConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_entityname", ConditionOperator.Equal, "drandgp"),
                                new ConditionExpression("gsc_dealerid", ConditionOperator.Equal, dealerid),
                                new ConditionExpression("gsc_branchid", ConditionOperator.Equal, branchid),
                            };

            EntityCollection sequenceRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_idsequence", sequenceConditionList, _organizationService, null, OrderType.Ascending, 
                new[] { "gsc_cmn_idsequenceid" });


            _tracingService.Trace("Check if sequenceRecord is not null...");
            if (sequenceRecords != null && sequenceRecords.Entities.Count > 0)
                {
                    var sequenceId = sequenceRecords.Entities[0].Id;

                    //Created an Infinite loop with no condition expression
                    //The loop will be terminated once the record successfully given an ID.   
                    //Hence, It will continually retry until it reaches the max retry count.
                    //Unless, there is an unexpected error occurs (errors that are not listed in "RetryableSqlErrors"), plugin will throw an error.
                    _tracingService.Trace("1");
                    for (; ; )
                    {
                        try
                        {
                            _tracingService.Trace("Calling all the Method...");
                            //Lock the Record
                            RecordLock(sequenceId, _organizationService, _tracingService);

                            //Retrieve ID Seqeunce Record Details using the Id we retrieved in above code
                            Entity sequenceEntity = _organizationService.Retrieve("gsc_cmn_idsequence", sequenceId, new ColumnSet("gsc_targetfield", "gsc_sequencenumber", "gsc_numberpadding", "gsc_prefix", "gsc_suffix", "gsc_lastsequencenumber",
                                "gsc_suffixlength", "gsc_delimiter", "gsc_customdelimiter", "gsc_lock", "gsc_entityname", "gsc_cmn_idsequenceid"));

                            // Assemble Id Sequence based on the setup in ID Sequence Record
                            var generatedId = AssembleIdSequence(sequenceEntity, _organizationService, _tracingService);

                            //Assign the Assembled ID in the target field indicated in IS Sequence Setup
                            PopulateGeneratedID(invoice, generatedId, _organizationService, _tracingService);

                            //Release the Record
                            RecordUnlock(sequenceEntity, sequenceId, generatedId, _organizationService, _tracingService);

                        
                            break;
                        }

                        catch (SqlException ex)
                        {
                            if (!Enum.IsDefined(typeof(RetryableSqlErrors), ex.Number))
                                throw;

                            retryCount++;
                            if (retryCount > MAX_RETRY) throw;

                            Thread.Sleep(ex.Number == (int)RetryableSqlErrors.Timeout ?
                                                                    longWait : shortWait);
                        }
                    }
                }

                else
                {
                    throw new InvalidPluginExecutionException("No Auto Numbering Setup");
                }
            }
        }

        public static void RecordLock(Guid sequenceId, IOrganizationService service, ITracingService trace)
        {
            trace.Trace("Started RecordLock Mehod.");

            var recordtoLock = new Entity("gsc_cmn_idsequence");
            recordtoLock.Id = sequenceId;
            recordtoLock["gsc_lock"] = true;
            service.Update(recordtoLock);

            trace.Trace("Ended RecordLock Mehod.");
        }

        public static String AssembleIdSequence(Entity sequenceEntity, IOrganizationService service, ITracingService trace)
        {
            trace.Trace("Started AssembleIdSequence Method.");

            var generatedID = String.Empty;

            if (sequenceEntity != null)
            {
                trace.Trace("Retrieved Id Sequence Details.");

                var prefix = sequenceEntity.Contains("gsc_prefix")
                   ? sequenceEntity.GetAttributeValue<String>("gsc_prefix")
                   : String.Empty;

                var sequenceNo = sequenceEntity.Contains("gsc_sequencenumber")
                    ? sequenceEntity.GetAttributeValue<Int32>("gsc_sequencenumber")
                    : 0;
                var padding = sequenceEntity.Contains("gsc_numberpadding")
                    ? sequenceEntity.FormattedValues["gsc_numberpadding"]
                    : String.Empty;

                var sequenceNoString = sequenceNo.ToString();
                sequenceNoString = sequenceNoString.PadLeft(Convert.ToInt32(padding), '0');

                trace.Trace("Sequence No Assembled");

                var suffix = sequenceEntity.Contains("gsc_suffix")
                    ? sequenceEntity.GetAttributeValue<OptionSetValue>("gsc_suffix").Value
                    : 0;
                var suffixLength = sequenceEntity.Contains("gsc_suffixlength")
                    ? sequenceEntity.FormattedValues["gsc_suffixlength"]
                    : String.Empty;
                var suffixString = String.Empty;

                if (suffix == 100000001) // Suffix is CurrentDate
                    suffixString = DateTime.Now.ToString("yyyyMMdd");
                else if (suffix == 100000002) // Suffix is Random AlphaNumeric
                    suffixString = RandomString(Convert.ToInt32(suffixLength));

                trace.Trace("Suffix Assembled");

                var delimiter = sequenceEntity.Contains("gsc_delimiter")
                    ? sequenceEntity.GetAttributeValue<OptionSetValue>("gsc_delimiter").Value
                    : 0;
                var delimiterString = String.Empty;

                if (delimiter == 100000001)//Delimiter is Dot (.)
                    delimiterString = ".";
                else if (delimiter == 100000002)//Delimiter is Hyphen (-)
                    delimiterString = "-";
                else if (delimiter == 100000003)//Delimiter is Underscore (_)
                    delimiterString = "_";
                else if (delimiter == 100000004)//Delimiter is a Custom Delimiter
                {
                    delimiterString = sequenceEntity.Contains("gsc_customdelimiter")
                    ? sequenceEntity.GetAttributeValue<String>("gsc_customdelimiter")
                    : String.Empty;
                }

                trace.Trace("Delimiter Assembled");

                generatedID = prefix + delimiterString + sequenceNoString + delimiterString + suffixString;

                trace.Trace("Sequence Id Assembled");
            }

            trace.Trace("Ended AssembleIdSequence Method.");

            return generatedID;
        }

        public static void PopulateGeneratedID(Entity invoice, String generatedId, IOrganizationService service, ITracingService trace)
        {
            trace.Trace("Started PopulateGeneratedID Method.");

            
            var invoiceId = invoice.Contains("invoiceid")
                ? invoice.GetAttributeValue<Guid>("invoiceid")
                : Guid.Empty;

            trace.Trace("Retrieved invoicerecord to update.");
           EntityCollection invoiceRecordstoUpdate = CommonHandler.RetrieveRecordsByOneValue("invoice", "invoiceid", invoiceId, service, null, OrderType.Ascending,
                   new[] { "gsc_drno", "gsc_drdate", "gsc_invoicestatuscopy" });

           trace.Trace("Check if not equal to null");
            if (invoiceRecordstoUpdate != null && invoiceRecordstoUpdate .Entities.Count > 0)
            {
                Entity invoicetoUpdate  = invoiceRecordstoUpdate .Entities[0];

        
           
                trace.Trace("Update DRno and DRdate.");

                invoicetoUpdate["gsc_drno"] = generatedId;
                invoicetoUpdate["gsc_drdate"] = DateTime.UtcNow;
                invoicetoUpdate["gsc_invoicestatuscopy"] = new OptionSetValue(100000002);
                service.Update(invoicetoUpdate);

                trace.Trace("Invoice Record DRno and DRdate Updated.");
            }

            trace.Trace("Ended PopulateGeneratedID Method.");
        }
        
        public static void RecordUnlock(Entity sequenceEntity, Guid sequenceId, String generatedID, IOrganizationService service, ITracingService trace)
        {
            trace.Trace("Started RecordUnlock Method.");

            if (sequenceEntity != null)
            {
                var sequenceNo = sequenceEntity.Contains("gsc_sequencenumber")
                    ? sequenceEntity.GetAttributeValue<Int32>("gsc_sequencenumber")
                    : 0;

                sequenceEntity.Id = sequenceId;
                sequenceEntity["gsc_sequencenumber"] = sequenceNo + 1;
                sequenceEntity["gsc_lock"] = false;
                sequenceEntity["gsc_lastsequencenumber"] = generatedID;

                service.Update(sequenceEntity);
            }
            trace.Trace("Ended RecordUnlock Method.");
        }

        public Entity CheckInvoiceIdIfDuplicate(Entity invoiceEntity)
        {
            _tracingService.Trace("Started CheckInvoiceIdIfDuplicate method...");

            //Retrieve Invoice record
            EntityCollection invoiceRecords = CommonHandler.RetrieveRecordsByOneValue(invoiceEntity.LogicalName, "invoiceid", invoiceEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "name", "gsc_branchid" });

            if (invoiceRecords != null || invoiceRecords.Entities.Count > 0)
            {
                _tracingService.Trace("purchase order entity...");
                Entity invoice = invoiceRecords.Entities[0];


                var invoiceId = invoice.Contains("name")
                    ? invoice.GetAttributeValue<String>("name")
                    : String.Empty;
                var branchId = invoice.GetAttributeValue<EntityReference>("gsc_branchid") != null
                    ? invoice.GetAttributeValue<EntityReference>("gsc_branchid").Id
                    : Guid.Empty;

                _tracingService.Trace("Create Condition List for invoice...");
                var invoiceConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_branchid", ConditionOperator.Equal, branchId),
                                new ConditionExpression("name", ConditionOperator.Equal, invoiceId),
                            };

                EntityCollection invoiceNameRecords = CommonHandler.RetrieveRecordsByConditions("invoice", invoiceConditionList, _organizationService, null, OrderType.Ascending);

                _tracingService.Trace("Invoice Detail Records Count : " + invoiceRecords.Entities.Count.ToString());

                if (invoiceNameRecords != null && invoiceNameRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Check name/invoiceid if Duplicated...");
                    if (invoiceNameRecords.Entities.Count >= 2)
                    {
                        throw new InvalidPluginExecutionException("Duplicated Invoiced Number");
                    }
                    //gsc_isNotDuplicate -- need to add field
                    
                    
                }
            }
            return invoiceEntity;
        }

        public Boolean ValidateSubmitDRandGatePass(Entity invoice)
        {
            var OrderId = invoice.GetAttributeValue<EntityReference>("salesorderid") != null
                ? invoice.GetAttributeValue<EntityReference>("salesorderid").Id
                : Guid.Empty;

            EntityCollection orderRecords = CommonHandler.RetrieveRecordsByOneValue("salesorder", "salesorderid", OrderId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_completed"});

            if (orderRecords != null || orderRecords.Entities.Count > 0)
            {
                if (orderRecords.Entities[0].GetAttributeValue<Boolean>("gsc_completed"))
                    return true;
            }

            return false;
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/20/2017
        /*Purpose: Create inventory history record on invoice release.
         * Registration Details: 
         * Event/Message:
         *      Post/Update: Sales Invoice Status
         * Primary Entity: Invoice
         */
        public Entity CreateSoldInventoryHistory(Entity invoiceEntity)
        {
            _tracingService.Trace("Started CreateSoldInventoryHistory method..");

            if (!invoiceEntity.FormattedValues["gsc_salesinvoicestatus"].Equals("Released")) { return null; }

            Entity inventoryHistory = new Entity("gsc_iv_inventoryhistory");
            
            inventoryHistory["gsc_customername"] = invoiceEntity.Contains("customerid")
                ? invoiceEntity.GetAttributeValue<EntityReference>("customerid").Name
                : String.Empty;
            inventoryHistory["gsc_customerid"] = invoiceEntity.Contains("gsc_customer")
                ? invoiceEntity.GetAttributeValue<String>("gsc_customer")
                : String.Empty;
            inventoryHistory["gsc_vehiclecolorid"] = invoiceEntity.Contains("gsc_vehiclecolorid1")
                ? invoiceEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1")
                : null;
            inventoryHistory["gsc_productid"] = invoiceEntity.Contains("gsc_productid")
                ? invoiceEntity.GetAttributeValue<EntityReference>("gsc_productid")
                : null;
            inventoryHistory["gsc_invoiceid"] = new EntityReference(invoiceEntity.LogicalName, invoiceEntity.Id);
            inventoryHistory["gsc_vsidate"] = invoiceEntity.Contains("gsc_invoicedate")
                ? invoiceEntity.GetAttributeValue<DateTime?>("gsc_invoicedate")
                : (DateTime?)null;
            inventoryHistory["gsc_releaseddate"] = invoiceEntity.Contains("gsc_releaseddate")
                ? invoiceEntity.GetAttributeValue<DateTime?>("gsc_releaseddate")
                : (DateTime?)null;
            inventoryHistory["gsc_quantitytype"] = new OptionSetValue(100000002);
            inventoryHistory["gsc_latest"] = true;
            inventoryHistory["gsc_transactionnumber"] = invoiceEntity.Contains("name")
                ? invoiceEntity.GetAttributeValue<String>("name")
                : String.Empty;

            EntityCollection invoicedVehicleRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_invoicedvehicle", "gsc_invoiceid", invoiceEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid" });

            if (invoicedVehicleRecords != null && invoicedVehicleRecords.Entities.Count > 0)
            {
                Entity invoicedVehicle = invoicedVehicleRecords.Entities[0];

                Guid inventoryId = invoicedVehicle.Contains("gsc_inventoryid")
                    ? invoicedVehicle.GetAttributeValue<EntityReference>("gsc_inventoryid").Id
                    : Guid.Empty;

                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_siteid", "gsc_modelyear", "gsc_productquantityid" });

                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    Entity inventory = inventoryRecords.Entities[0];
                    inventoryHistory["gsc_modelcode"] = inventory.Contains("gsc_modelcode")
                        ? inventory.GetAttributeValue<String>("gsc_modelcode")
                        : null;
                    inventoryHistory["gsc_optioncode"] = inventory.Contains("gsc_optioncode")
                        ? inventory.GetAttributeValue<String>("gsc_optioncode")
                        : null;
                    inventoryHistory["gsc_modelyear"] = inventory.Contains("gsc_modelyear")
                        ? inventory.GetAttributeValue<String>("gsc_modelyear")
                        : null;
                    inventoryHistory["gsc_siteid"] = inventory.Contains("gsc_siteid")
                        ? inventory.GetAttributeValue<EntityReference>("gsc_siteid")
                        : null;
                    inventoryHistory["gsc_vin"] = inventory.Contains("gsc_vin")
                        ? inventory.GetAttributeValue<String>("gsc_vin")
                        : String.Empty;
                    inventoryHistory["gsc_csno"] = inventory.Contains("gsc_csno")
                        ? inventory.GetAttributeValue<String>("gsc_csno")
                        : String.Empty;
                    inventoryHistory["gsc_productionno"] = inventory.Contains("gsc_productionno")
                        ? inventory.GetAttributeValue<String>("gsc_productionno")
                        : String.Empty;
                    inventoryHistory["gsc_engineno"] = inventory.Contains("gsc_engineno")
                        ? inventory.GetAttributeValue<String>("gsc_engineno")
                        : String.Empty;
                    inventoryHistory["gsc_productquantityid"] = inventory.Contains("gsc_productquantityid")
                        ? inventory.GetAttributeValue<EntityReference>("gsc_productquantityid")
                        : null;
                }

                _organizationService.Create(inventoryHistory);
            }            

            _tracingService.Trace("Ended CreateSoldInventoryHistory method..");
            return invoiceEntity;
        }
    }
}
