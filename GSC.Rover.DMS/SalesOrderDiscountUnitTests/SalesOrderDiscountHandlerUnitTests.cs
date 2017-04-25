using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.SalesOrderDiscount;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace SalesOrderDiscountUnitTests
{
    [TestClass]
    public class SalesOrderDiscountHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 3/11/2016
        #region Order Discount - Set Total Discount Amount

        #region Test Scenario : Set 'Total Discount Amount' value of Order entity

        [TestMethod]
        public void SetOrderTotalDiscountAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order Discount EntityCollection
            var SalesOrderDiscountCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_salesorderdiscount",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_salesorderdiscount",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_orderid", new EntityReference("salesorder", Guid.NewGuid())
                            { Name = "Sample Order"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = SalesOrderDiscountCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_orderid").Id,
                        LogicalName = "salesorer",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_totaldiscountamount", ""},
                            {"statecode", new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderDiscountCollection.EntityName)
                ))).Returns(SalesOrderDiscountCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderDiscountHandler = new SalesOrderDiscountHandler(orgService, orgTracing);
            Entity salesorder = SalesOrderDiscountHandler.SetOrderTotalDiscountAmount(SalesOrderDiscountCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderDiscountCollection.Entities[0].GetAttributeValue<Money>("gsc_discountamount").Value, SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totaldiscountamount").Value);
            #endregion
        }
        #endregion

        #region Test Scenario : Delete a Sales Order Discount record then update 'Total Discount Amount' value in Order record

        [TestMethod]
        public void ComputeDeductedOrderTotalDiscountAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;


            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_totaldiscountamount", ""},
                            {"statecode" , new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Discount EntityCollection
            var SalesOrderDiscountCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_salesorderdiscount",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_salesorderdiscount",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesorderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)
                            { Name = "Sample Quote 1"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)}
                        }
                    },
                    
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_salesorderdiscount",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesorderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)
                            { Name = "Sample Quote 2"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderDiscountCollection.EntityName)
                ))).Returns(SalesOrderDiscountCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderDiscountHandler = new SalesOrderDiscountHandler(orgService, orgTracing);
            Entity quote = SalesOrderDiscountHandler.SetOrderTotalDiscountAmount(SalesOrderDiscountCollection.Entities[1], "Delete");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderDiscountCollection.Entities[1].GetAttributeValue<Money>("gsc_discountamount").Value, SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totaldiscountamount").Value);
            #endregion
        }
        #endregion

        #region Test Scenario : Apply fields are null, update the 3 Less Discount fields

        [TestMethod]
        public void SetLessDiscountFieldValues()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;


            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_downpaymentdiscount", new Money(Decimal.Zero)},
                            {"gsc_discountamountfinanced", new Money(Decimal.Zero)},
                            {"gsc_discount", new Money(Decimal.Zero)},
                            {"gsc_applytodpamount", new Money(Decimal.Zero)},
                            {"gsc_applytoafamount", new Money(Decimal.Zero)},
                            {"gsc_applytoupamount", new Money(Decimal.Zero)},
                            {"statecode" , new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Discount EntityCollection
            var SalesOrderDiscountCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_salesorderdiscount",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_salesorderdiscount",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesorderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)
                            { Name = "Sample Order Discount 1"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)},
                            {"gsc_applyamounttodp", new Money((Decimal)100000.00)},
                            {"gsc_applyamounttoaf", new Money((Decimal)200000.00)},
                            {"gsc_applyamounttoup", new Money((Decimal)300000.00)}
                        }
                    },
                    
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_salesorderdiscount",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesorderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)
                            { Name = "Sample Order 2 Discount"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)},
                            {"gsc_applyamounttodp", new Money((Decimal)300000.00)},
                            {"gsc_applyamounttoaf", new Money((Decimal)200000.00)},
                            {"gsc_applyamounttoup", new Money((Decimal)100000.00)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderDiscountCollection.EntityName)
                ))).Returns(SalesOrderDiscountCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderDiscountHandler = new SalesOrderDiscountHandler(orgService, orgTracing);
            //Entity salesOrder = SalesOrderDiscountHandler.SetLessDiscountValues(SalesOrderDiscountCollection.Entities[0], "Update");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_downpaymentdiscount").Value, 400000);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_discountamountfinanced").Value, 400000);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_discount").Value, 400000);
            #endregion
        }
        #endregion

        #endregion
    }
}
