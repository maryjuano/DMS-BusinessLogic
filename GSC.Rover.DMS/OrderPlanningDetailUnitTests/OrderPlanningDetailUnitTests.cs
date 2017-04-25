using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.OrderPlanningDetail;
using Microsoft.Xrm.Sdk;
using Moq;
using Microsoft.Xrm.Sdk.Query;


namespace OrderPlanningDetailUnitTests
{
    [TestClass]
    public class OrderPlanningDetailUnitTests
    {
        //Created By: Leslie G. Baliguat, Created On: 05/31/2016
        #region CreatDetailsForNextMonth

        #region Test Scenario: CreateDetailForCurrentMonth
        [TestMethod]
        public void CreatDetailsForNextMonth()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Invoice Entity Collection
            var InvoiceCollection = new EntityCollection()
            {
                EntityName = "invoice",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "invoice",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesinvoicestatus", new OptionSetValue(100000004)},
                            {"gsc_productid", new EntityReference("product", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))}
                        }
                    },
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "invoice",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesinvoicestatus", new OptionSetValue(100000004)},
                            {"gsc_productid", new EntityReference("product", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))}
                        }
                    }
                }
            };
            #endregion

            #region Sales Return Entity Collection
            var ReturnCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_vehiclesalesreturn",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_vehiclesalesreturn",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclesalesreturnstatus", new OptionSetValue(100000002)},
                            {"gsc_productid", new EntityReference("product", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))},
                            {"gsc_invoiceid", new EntityReference(InvoiceCollection.EntityName, InvoiceCollection.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            #region Order Planning Entity Collection
            var OrderPlanningCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_orderplanning",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_orderplanning",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_productid", new EntityReference("product", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))},
                            {"gsc_siteid", new EntityReference("site", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))},
                            {"gsc_createdetailforcurrentmonth", true},
                            {"gsc_retailperiodcoverage", 2}
                        },
                        FormattedValues = 
                        {
                            {"gsc_retailperiodcoverage", "2"}
                        }
                    }
                }
            };
            #endregion

            #region Order Planning Details Entity Collection
            var OrderPlanningDetailCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_orderplanningdetail",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_orderplanningdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_sls_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[0].Id)},
                            {"gsc_year", 2016},
                            {"gsc_month", 06},
                            {"gsc_endinginventory", 10.00},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == InvoiceCollection.EntityName)
                ))).Returns(InvoiceCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ReturnCollection.EntityName)
                ))).Returns(ReturnCollection);


            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningCollection.EntityName)
              ))).Returns(OrderPlanningCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningDetailCollection.EntityName)
                ))).Returns(OrderPlanningDetailCollection);

            #endregion

            #region 2. Call/Action
            var OrderPlanningHandler = new OrderPlanningDetailHandler(orgService, orgTracing);
            Entity Detail = OrderPlanningHandler.CreatDetailsForNextMonth(OrderPlanningCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(OrderPlanningCollection.Entities[0].Id, Detail.GetAttributeValue<EntityReference>("gsc_orderplanningid").Id);
            Assert.AreEqual("2016", Detail.GetAttributeValue<String>("gsc_year"));
            Assert.AreEqual("07", Detail.GetAttributeValue<String>("gsc_month"));
            Assert.AreEqual(0.0, Detail.GetAttributeValue<Double>("gsc_retailaveragesales"));
            Assert.AreEqual(OrderPlanningDetailCollection.Entities[0].GetAttributeValue<Double>("gsc_endinginventory"), Detail.GetAttributeValue<Double>("gsc_beginninginventory"));
            #endregion
        }
        #endregion

        #endregion

        //Created By: Leslie G. Baliguat, Created On: 06/07/2016
        #region ComputeStockMonth

        #region Test Scenario: Happy Flow
        [TestMethod]
        public void ComputeStockMonth()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Order Planning Details Entity Collection
            var OrderPlanningDetailCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_orderplanningdetail",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_orderplanningdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_endinginventory", 10.00},
                            {"gsc_retailaveragesales", 5.0},
                            {"gsc_stockmonth", 0.0}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(OrderPlanningDetailCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == OrderPlanningDetailCollection.Entities[0].LogicalName)))).Callback<Entity>(s => OrderPlanningDetailCollection.Entities[0] = s);

            #endregion

            #region 2. Call/Action
            var OrderPlanningHandler = new OrderPlanningDetailHandler(orgService, orgTracing);
            OrderPlanningHandler.ComputeStockMonth(OrderPlanningDetailCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(2, OrderPlanningDetailCollection.Entities[0].GetAttributeValue<Double>("gsc_stockmonth"));
            #endregion
        }
        #endregion

        #region Test Scenario: Retail Average Zero
        [TestMethod]
        public void ComputeStockMonthZero()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Order Planning Details Entity Collection
            var OrderPlanningDetailCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_orderplanningdetail",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_orderplanningdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_endinginventory", 10.00},
                            {"gsc_retailaveragesales", 0.0},
                            {"gsc_stockmonth", 0.0}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(OrderPlanningDetailCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == OrderPlanningDetailCollection.Entities[0].LogicalName)))).Callback<Entity>(s => OrderPlanningDetailCollection.Entities[0] = s);

            #endregion

            #region 2. Call/Action
            var OrderPlanningHandler = new OrderPlanningDetailHandler(orgService, orgTracing);
            OrderPlanningHandler.ComputeStockMonth(OrderPlanningDetailCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(0, OrderPlanningDetailCollection.Entities[0].GetAttributeValue<Double>("gsc_stockmonth"));
            #endregion
        }
        #endregion

        #endregion
    }
}
