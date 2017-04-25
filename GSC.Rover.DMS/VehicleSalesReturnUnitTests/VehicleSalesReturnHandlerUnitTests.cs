using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using GSC.Rover.DMS.BusinessLogic.VehicleSalesReturn;


namespace VehicleSalesReturnUnitTests
{
    [TestClass]
    public class VehicleSalesReturnHandlerUnitTests
    {

        //Created By:Raphael Herrera, Created On: 6/01/2016
        #region Simulate replication of records
        [TestMethod]

        #region Test Scenario: Replicate Records
        public void TestMethod1()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Color Entity Collection
            //var ColorCollection = new EntityCollection()
            //{
            //    EntityName = "VehicleColor",
            //    Entities =
            //    {
            //        new Entity
            //        {
            //            Id = Guid.NewGuid(),
            //            LogicalName = "vehiclecolor",
            //            Attributes =
            //            {
            //                {"gsc_colorpn", String.Empty}
                            
            //            }
                       
            //        }
            //    }
            //};
            
            #endregion

             #region Sales Order Entity Collection
            var SalesOrderCollection = new EntityCollection()
            {
                EntityName = "Order",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "order"   
                    }
                }
            };
            #endregion

            #region VehicleSalesReturn Entity Collection
            var VehicleSalesReturnCollection = new EntityCollection()
            {
                EntityName = "VehicleSalesReturn",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_vehiclesalesreturn",
                        Attributes =
                        {
                            {"gsc_modeldescription", String.Empty},
                            {"gsc_modelcode", String.Empty},
                            {"gsc_modelyear", String.Empty},
                            {"gsc_color", String.Empty},
                            {"gsc_csno", String.Empty},
                            {"gsc_engineno", String.Empty},
                            {"gsc_vin", String.Empty},
                            {"gsc_productionno", String.Empty},
                            {"gsc_returnedquantity", 0},
                            {"gsc_returnedamount", new Money(0)},
                            {"gsc_salesorderid", Guid.Empty},
                            {"gsc_paymentmode", null},
                            {"gsc_customertype", null},
                            {"gsc_salesinvoicestatus", String.Empty}
                        }
                       
                    }
                }
            };
            #endregion

            #region Invoice Entity Collection
            var SalesInvoiceCollection = new EntityCollection()
            {
                EntityName = "Invoice",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "invoice",
                        Attributes =
                        {
                            {"gsc_modeldescription", "description"},
                            {"gsc_modelcode", "model code"},
                            {"gsc_modelyear", "2002"},
                            {"gsc_csno", "1"},
                            {"gsc_engineno", "2"},
                            {"gsc_vin", "3"},
                            {"gsc_productionno", "4"},
                            {"gsc_unitprice", new Money(900000)},
                            {"gsc_salesorderid", new EntityReference("order", SalesOrderCollection.Entities[0].Id)},
                            {"gsc_paymentmode", new OptionSetValue(1)},
                            {"gsc_customertype", new OptionSetValue(1)},
                            {"gsc_salesinvoicestatus", "Released"}
                        },
                        FormattedValues = 
                        {
                            {"gsc_paymentmode","Cash"},
                            {"gsc_customertype", "Individual"},
                        }
                       
                    }
                }
            };
            #endregion

            #endregion


            #region 2. Call/Action
            VehicleSalesReturnHandler handler = new VehicleSalesReturnHandler(orgService, orgTracing);
            handler.ReplicateInvoicedVehicle(VehicleSalesReturnCollection.Entities[0], "Update");
            #endregion

            #region 3. Verify
            Assert.AreEqual("description", VehicleSalesReturnCollection.Entities[0].GetAttributeValue<string>("gsc_modeldescription"));
            Assert.AreEqual("model code", VehicleSalesReturnCollection.Entities[0].GetAttributeValue<string>("gsc_modelcode"));
            Assert.AreEqual("2002", VehicleSalesReturnCollection.Entities[0].GetAttributeValue<string>("gsc_modelyear"));
            Assert.AreEqual( "1", VehicleSalesReturnCollection.Entities[0].GetAttributeValue<string>("gsc_csno"));
            Assert.AreEqual("2", VehicleSalesReturnCollection.Entities[0].GetAttributeValue<string>("gsc_engineno"));
            Assert.AreEqual("3", VehicleSalesReturnCollection.Entities[0].GetAttributeValue<string>("gsc_vin"));
            Assert.AreEqual("4", VehicleSalesReturnCollection.Entities[0].GetAttributeValue<string>("gsc_productionno"));
            Assert.AreEqual(1, VehicleSalesReturnCollection.Entities[0].GetAttributeValue<Int32>("gsc_returnedquantity"));
            Assert.AreEqual(SalesInvoiceCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_salesorderid").Id,VehicleSalesReturnCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_salesorderid").Id);
            Assert.AreEqual(SalesInvoiceCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_paymentmode"),VehicleSalesReturnCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_paymentmode"));
            Assert.AreEqual(SalesInvoiceCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_customertype"),VehicleSalesReturnCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_customertype"));
            Assert.AreEqual("Released",VehicleSalesReturnCollection.Entities[0].GetAttributeValue<string>("gsc_salesinvoicestatus"));
            //Assert.AreEqual(SalesInvoiceCollection.Entities[0].GetAttributeValue<Money>("gsc_unitprice"), (decimal)VehicleSalesReturnCollection.Entities[0].GetAttributeValue<Money>("gsc_returnedamount").Value);

            #endregion
        }
        #endregion

        #endregion
    }
}
