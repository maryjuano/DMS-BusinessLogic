using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.Invoice;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace InvoiceUnitTests
{
    [TestClass]
    public class InvoiceHandlerUnitTests
    {
        // Created By : Jerome Anthony Gerero, Created On : 5/11/2016
        #region Replicate Order fields to created Invoice record

        #region Test Scenario : User creates new Invoice from Order form
        [TestMethod]
        public void ReplicateOrderToInvoice()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order entity
            var SalesOrderCollection = new EntityCollection()
            {
                EntityName = "salesorder",
                Entities = 
                {
                    new Entity                    
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        Attributes = 
                        {
                            {"gsc_dealerid", new EntityReference("account", Guid.NewGuid())},
                            {"gsc_branchsiteid", new EntityReference("account", Guid.NewGuid())},
                            {"gsc_salesexecutiveid", new EntityReference("contact", Guid.NewGuid())},
                            {"gsc_leadsourceid", new EntityReference("gsc_sls_leadsource", Guid.NewGuid())},
                            {"gsc_paymentmode", new OptionSetValue(1000000)},
                            {"gsc_customertype", new OptionSetValue(1000001)},
                            {"gsc_customerid", new EntityReference("contact", Guid.NewGuid())},
                            {"gsc_address", "Calle Industria cor. Economia"},
                            {"gsc_tin", "000809208002"},
                            {"gsc_productid", new EntityReference("product", Guid.NewGuid())},
                            {"gsc_vehicleunitprice", new Money((Decimal)1800000)},
                            {"gsc_vehiclecolorid1", new EntityReference("gsc_cmn_vehiclecolor", Guid.NewGuid())},
                            {"gsc_vehiclecolorid2", new EntityReference("gsc_cmn_vehiclecolor", Guid.NewGuid())},
                            {"gsc_vehiclecolorid3", new EntityReference("gsc_cmn_vehiclecolor", Guid.NewGuid())},
                            {"gsc_vehicledetails", "Car is made purely from carbon fiber"},
                            {"gsc_remarks", "Uneasy hearts weigh the most"},
                            {"gsc_unitprice", new Money((Decimal)1800000)},
                            {"gsc_colorprice", new Money((Decimal)30000)},
                            {"gsc_discount", new Money((Decimal)40000)},
                            //{"gsc_downpaymentdiscount", new Money((Decimal)20000)},
                            {"gsc_netprice", new Money((Decimal)1700000)},
                            {"gsc_downpayment", new Money((Decimal)50000)},
                            {"gsc_accessories", new Money((Decimal)80000)},
                            {"gsc_insurance", new Money((Decimal)3000)},
                            {"gsc_chattelfee", new Money((Decimal)54000)},
                            {"gsc_othercharges", new Money((Decimal)25000)},
                            {"gsc_reservation", new Money((Decimal)5000)},
                            {"gsc_totalcashoutlay", new Money((Decimal)35000)},
                            {"gsc_totalamountfinanced", new Money((Decimal)8000)},
                            {"gsc_netmonthlyamortization", new Money((Decimal)30000)},
                            {"gsc_downpaymentamount", new Money((Decimal)50000)},
                            {"gsc_downpaymentpercentage", (Double)25.00},
                            {"gsc_downpaymentdiscount", new Money((Decimal)50000)},
                            {"gsc_netdownpayment", new Money((Decimal)60000)},
                            {"gsc_amountfinanced", new Money((Decimal)180000)},
                            {"gsc_discountamountfinanced", new Money((Decimal)180000)},
                            {"gsc_netamountfinanced", new Money((Decimal)180000)},
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", Guid.NewGuid())},
                            {"gsc_financingschemeid", new EntityReference("gsc_cmn_financingscheme", Guid.NewGuid())},
                            {"gsc_freechattelfee", false},
                            {"gsc_totaldiscountamount", new Money((Decimal)60000)},
                            {"gsc_applytodppercentage", (Double)25.00},
                            {"gsc_applytoafpercentage", (Double)25.00},
                            {"gsc_applytouppercentage", (Double)50.00},
                            {"gsc_applytodpamount", new Money((Decimal)25000)},
                            {"gsc_applytoafamount", new Money((Decimal)25000)},
                            {"gsc_applytoupamount", new Money((Decimal)50000)},
                            {"gsc_insuranceid", new EntityReference("gsc_cmn_insurance", Guid.NewGuid())},
                            {"gsc_vehicletype", new EntityReference("gsc_iv_vehicletype", Guid.NewGuid())},
                            {"gsc_free", false},
                            {"gsc_rate", (Double)2.00},
                            {"gsc_cost", new Money((Decimal)40000)},
                            {"gsc_totalpremium", new Money((Decimal)9000)},
                            {"gsc_originaltotalpremium", new Money((Decimal)7000)},
                            {"gsc_totalchargesamount", new Money((Decimal)5000)},
                            {"gsc_modeldescription", "Montero"},
                            {"gsc_modelyear", "2016"},
                            {"gsc_siteid", new EntityReference("account", Guid.NewGuid())},
                            {"gsc_colorid", new EntityReference("gsc_iv_color", Guid.NewGuid())},
                            {"gsc_csnocriteria", new OptionSetValue(10000001)},
                            {"gsc_enginenocriteria", new OptionSetValue(100000001)},
                            {"gsc_vincriteria", new OptionSetValue(10000001)},
                            {"gsc_productionnocriteria", new OptionSetValue(10000001)},
                            {"gsc_modelcode", "MNT"},
                            {"gsc_color1", "Hot Pink"},
                            {"gsc_color2", "Baby Blue"},
                            {"gsc_color3", ""},
                            {"gsc_csno", "59483726"},
                            {"gsc_engineno", "GAT-X105"},
                            {"gsc_vin", "GAT-X201"},
                            {"gsc_productionno", "X101"},
                            {"gsc_expecteddateofrelease", DateTime.UtcNow.AddHours(256)},
                            {"gsc_placeofrelease", "Eastwood"},
                            {"gsc_quotedate", DateTime.UtcNow},
                            {"gsc_orderdate", DateTime.UtcNow},
                            {"gsc_requestallocationdate", DateTime.UtcNow},
                            {"gsc_vehicleallocationdate", DateTime.UtcNow.AddYears(1)},
                            {"gsc_transferreddateforinvoicing", DateTime.UtcNow},
                            {"gsc_ordercancelleddate", DateTime.UtcNow},
                            {"gsc_invoicedate", DateTime.UtcNow},
                            {"gsc_drdate", DateTime.UtcNow},
                            {"gsc_posteddate", DateTime.UtcNow},
                            {"gsc_recordownerid", new EntityReference("contact", Guid.NewGuid())},
                            {"gsc_salesinvoicestatus", new OptionSetValue(10000001)}
                        }                    
                    }                
                }
            };
            #endregion

            #region Invoice entity
            var invoiceEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "salesorder",
                Attributes = 
                {
                    {"salesorderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);
            
            #endregion

            #region 2. Call / Action
            var invoiceHandler = new InvoiceHandler(orgService, orgTracing);
            Entity invoice = invoiceHandler.ReplicateOrderInfo(invoiceEntity);
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_dealerid").Id, invoice.GetAttributeValue<EntityReference>("gsc_dealerid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_branchsiteid").Id, invoice.GetAttributeValue<EntityReference>("gsc_branchsiteid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id, invoice.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_leadsourceid").Id, invoice.GetAttributeValue<EntityReference>("gsc_leadsourceid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value, invoice.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_customertype").Value, invoice.GetAttributeValue<OptionSetValue>("gsc_customertype").Value);
            //Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_customerid").Id, invoice.GetAttributeValue<EntityReference>("gsc_customerid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_address"), invoice.GetAttributeValue<String>("shipto_composite"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_tin"), invoice.GetAttributeValue<String>("gsc_tin"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_productid").Id, invoice.GetAttributeValue<EntityReference>("gsc_productid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_vehicleunitprice").Value, invoice.GetAttributeValue<Money>("gsc_vehicleunitprice").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclecolorid1").Id, invoice.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclecolorid2").Id, invoice.GetAttributeValue<EntityReference>("gsc_vehiclecolorid2").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclecolorid3").Id, invoice.GetAttributeValue<EntityReference>("gsc_vehiclecolorid3").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_vehicledetails"), invoice.GetAttributeValue<String>("gsc_vehicledetails"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_remarks"), invoice.GetAttributeValue<String>("gsc_remarks"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_unitprice").Value, invoice.GetAttributeValue<Money>("gsc_unitprice").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_colorprice").Value, invoice.GetAttributeValue<Money>("gsc_colorprice").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_discount").Value, invoice.GetAttributeValue<Money>("gsc_discount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_netprice").Value, invoice.GetAttributeValue<Money>("gsc_netprice").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_downpayment").Value, invoice.GetAttributeValue<Money>("gsc_downpayment").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_accessories").Value, invoice.GetAttributeValue<Money>("gsc_accessories").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_insurance").Value, invoice.GetAttributeValue<Money>("gsc_insurance").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_chattelfee").Value, invoice.GetAttributeValue<Money>("gsc_chattelfee").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_othercharges").Value, invoice.GetAttributeValue<Money>("gsc_othercharges").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_reservation").Value, invoice.GetAttributeValue<Money>("gsc_reservation").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totalcashoutlay").Value, invoice.GetAttributeValue<Money>("gsc_totalcashoutlay").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totalamountfinanced").Value, invoice.GetAttributeValue<Money>("gsc_totalamountfinanced").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_netmonthlyamortization").Value, invoice.GetAttributeValue<Money>("gsc_netmonthlyamortization").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_downpaymentamount").Value, invoice.GetAttributeValue<Money>("gsc_downpaymentamount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Double>("gsc_downpaymentpercentage"), invoice.GetAttributeValue<Double>("gsc_downpaymentpercentage"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_downpaymentdiscount").Value, invoice.GetAttributeValue<Money>("gsc_downpaymentdiscount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_netdownpayment").Value, invoice.GetAttributeValue<Money>("gsc_netdownpayment").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_amountfinanced").Value, invoice.GetAttributeValue<Money>("gsc_amountfinanced").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_discountamountfinanced").Value, invoice.GetAttributeValue<Money>("gsc_discountamountfinanced").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_netamountfinanced").Value, invoice.GetAttributeValue<Money>("gsc_netamountfinanced").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_discountamountfinanced").Value, invoice.GetAttributeValue<Money>("gsc_discountamountfinanced").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_netamountfinanced").Value, invoice.GetAttributeValue<Money>("gsc_netamountfinanced").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id, invoice.GetAttributeValue<EntityReference>("gsc_bankid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_financingschemeid").Id, invoice.GetAttributeValue<EntityReference>("gsc_financingschemeid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Boolean>("gsc_freechattelfee"), invoice.GetAttributeValue<Boolean>("gsc_freechattelfee"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totaldiscountamount").Value, invoice.GetAttributeValue<Money>("gsc_totaldiscountamount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Double>("gsc_applytodppercentage"), invoice.GetAttributeValue<Double>("gsc_applytoddppercentage"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Double>("gsc_applytoafpercentage"), invoice.GetAttributeValue<Double>("gsc_applytoafpercentage"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Double>("gsc_applytouppercentage"), invoice.GetAttributeValue<Double>("gsc_applytouppercentage"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_applytodpamount").Value, invoice.GetAttributeValue<Money>("gsc_applytodpamount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_applytoafamount").Value, invoice.GetAttributeValue<Money>("gsc_applytoafamount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_applytoupamount").Value, invoice.GetAttributeValue<Money>("gsc_applytoupamount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_insuranceid").Id, invoice.GetAttributeValue<EntityReference>("gsc_insuranceid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehicletypeid").Id, invoice.GetAttributeValue<EntityReference>("gsc_vehicletypeid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_vehicleuse").Value, invoice.GetAttributeValue<OptionSetValue>("gsc_vehicleuse").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Boolean>("gsc_free"), invoice.GetAttributeValue<Boolean>("gsc_free"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Double>("gsc_rate"), invoice.GetAttributeValue<Double>("gsc_rage"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_cost").Value, invoice.GetAttributeValue<Money>("gsc_cost").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totalpremium").Value, invoice.GetAttributeValue<Money>("gsc_totalpremium").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_originaltotalpremium").Value, invoice.GetAttributeValue<Money>("gsc_originaltotalpremium").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totalchargesamount").Value, invoice.GetAttributeValue<Money>("gsc_totalchargesamount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_modeldescription").Id, invoice.GetAttributeValue<EntityReference>("gsc_modeldescription").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_modelyear"), invoice.GetAttributeValue<String>("gsc_modelyear"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_siteid").Id, invoice.GetAttributeValue<EntityReference>("gsc_siteid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_colorid").Id, invoice.GetAttributeValue<EntityReference>("gsc_colorid").Id);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_csnocriteria").Value, invoice.GetAttributeValue<OptionSetValue>("gsc_csnocriteria").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_enginenocriteria").Value, invoice.GetAttributeValue<OptionSetValue>("gsc_enginenocriteria").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_vincriteria").Value, invoice.GetAttributeValue<OptionSetValue>("gsc_vincriteria").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_productionnocriteria").Value, invoice.GetAttributeValue<OptionSetValue>("gsc_productioncriteria").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_modelcode"), invoice.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_color1"), invoice.GetAttributeValue<String>("gsc_color1"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_color2"), invoice.GetAttributeValue<String>("gsc_color2"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_color3"), invoice.GetAttributeValue<String>("gsc_color3"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_csno"), invoice.GetAttributeValue<String>("gsc_csno"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_engineno"), invoice.GetAttributeValue<String>("gsc_engineno"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_vin"), invoice.GetAttributeValue<String>("gsc_vin"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_productionno"), invoice.GetAttributeValue<String>("gsc_productionno"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_expecteddateofrelease"), invoice.GetAttributeValue<DateTime>("gsc_expecteddateofrelease"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<String>("gsc_placeofrelease"), invoice.GetAttributeValue<String>("gsc_placeofrelease"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_quotedate"), invoice.GetAttributeValue<DateTime>("gsc_quotedate"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_orderdate"), invoice.GetAttributeValue<DateTime>("gsc_orderdate"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_requestallocationdate"), invoice.GetAttributeValue<DateTime>("gsc_requestallocationdate"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_vehicleallocationdate"), invoice.GetAttributeValue<DateTime>("gsc_vehicleallocationdate"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_transferreddateforinvoicing"), invoice.GetAttributeValue<DateTime>("gsc_transferreddateforinvoicing"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_ordercancelleddate"), invoice.GetAttributeValue<DateTime>("gsc_ordercancelleddate"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_invoicedate"), invoice.GetAttributeValue<DateTime>("gsc_invoicedate"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_drdate"), invoice.GetAttributeValue<DateTime>("gsc_drdate"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<DateTime>("gsc_posteddate"), invoice.GetAttributeValue<DateTime>("gsc_posteddate"));
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_recordownerid").Id, invoice.GetAttributeValue<EntityReference>("gsc_recordownerid").Id);
            #endregion
        }
        #endregion

        #endregion
    }
}
