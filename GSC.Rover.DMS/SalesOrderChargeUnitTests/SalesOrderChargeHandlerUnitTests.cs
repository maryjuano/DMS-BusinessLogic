using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.SalesOrderCharge;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace SalesOrderChargeUnitTests
{
    [TestClass]
    public class SalesOrderChargeHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 3/11/2016
        #region Order Charge - Set Order Total Charges Amount

        #region Test Scenario : Set 'Total Charges Amount' field value in Order Entity

        [TestMethod]
        public void SetOrderTotalChargesAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order Charge EntityCollection
            var SalesOrderChargeCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_ordercharge",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_ordercharge",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_orderid", new EntityReference("salesorder", Guid.NewGuid())
                            { Name = "Sample Order"}},
                            {"gsc_amount", new Money((Decimal)320000.00)},
                            {"gsc_free", false}
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
                        Id = SalesOrderChargeCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_orderid").Id,
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_totalchargesamount", ""},
                            {"statecode", new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderChargeCollection.EntityName)
                ))).Returns(SalesOrderChargeCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderChargeHandler = new SalesOrderChargeHandler(orgService, orgTracing);
            Entity quote = SalesOrderChargeHandler.SetOrderTotalChargesAmount(SalesOrderChargeCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderChargeCollection.Entities[0].GetAttributeValue<Money>("gsc_amount").Value, SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totalchargesamount").Value);
            #endregion
        }
        #endregion

        #region Test Scenario : Some Applied Charges record has 'Free' field checked

        [TestMethod]
        public void FreeFieldCheckedUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order Charge EntityCollection
            var SalesOrderChargeCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_ordercharge",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_ordercharge",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_orderid", new EntityReference("salesorder", Guid.NewGuid())
                            { Name = "Sample Order"}},
                            {"gsc_amount", new Money((Decimal)320000.00)},
                            {"gsc_free", false}
                        }
                    },

                    //Sales Order Charge records with charge amount value but with 'Free' checked
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_ordercharge",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_orderid", new EntityReference("salesorder", Guid.NewGuid())
                            { Name = "Sample Order"}},
                            {"gsc_amount", new Money((Decimal)80000.00)},
                            {"gsc_free", true}
                        }
                    },

                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_ordercharge",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_orderid", new EntityReference("salesorder", Guid.NewGuid())
                            { Name = "Sample Order"}},
                            {"gsc_amount", new Money((Decimal)80000.00)},
                            {"gsc_free", true}
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
                        Id = SalesOrderChargeCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_orderid").Id,
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_totalchargesamount", ""},
                            {"statecode", new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderChargeCollection.EntityName)
                ))).Returns(SalesOrderChargeCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderChargeHandler = new SalesOrderChargeHandler(orgService, orgTracing);
            Entity salesorder = SalesOrderChargeHandler.SetOrderTotalChargesAmount(SalesOrderChargeCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderChargeCollection.Entities[0].GetAttributeValue<Money>("gsc_amount").Value, SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_totalchargesamount").Value);
            #endregion

        }
        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 4/11/2016
        #region Order Charge - Set Order Charge Amount field

        #region Test Scenario : Charges Code field contains data

        [TestMethod]
        public void SetChargeAmount()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order Charge EntityCollection
            var SalesOrderChargeCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_ordercharge",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_ordercharge",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {                            
                            {"gsc_description", String.Empty},
                            {"gsc_chargesid", new EntityReference("gsc_cmn_charges", Guid.NewGuid())
                            { Name = "Sample Order"}},
                            {"gsc_chargetype", String.Empty},
                            {"gsc_amount", new Money(Decimal.Zero)},
                            {"gsc_free", false}
                        }
                    }
                }
            };
            #endregion

            #region Charges EntityCollection
            var ChargesCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_charges",
                Entities =
                {
                    new Entity
                    {
                        Id = SalesOrderChargeCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_chargesid").Id,
                        LogicalName = "gsc_cmn_charges",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_description", "Delivery Charge"},
                            {"gsc_chargeamount", new Money((Decimal)800.00)},
                            {"gsc_chargetype", new OptionSetValue(0)}
                        },
                        FormattedValues = 
                        {
                            {"gsc_chargetype", "Delivery Charge"}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderChargeCollection.EntityName)
                ))).Returns(SalesOrderChargeCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ChargesCollection.EntityName)
                ))).Returns(ChargesCollection);

            #endregion

            #region 2. Call/Action
            
            var SalesOrderChargeHandler = new SalesOrderChargeHandler(orgService, orgTracing);
            Entity salesOrderCharge = SalesOrderChargeHandler.SetChargeDetails(SalesOrderChargeCollection.Entities[0], "Create");
            
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderChargeCollection.Entities[0].GetAttributeValue<Money>("gsc_amount").Value, ChargesCollection.Entities[0].GetAttributeValue<Money>("gsc_chargeamount").Value);
            Assert.AreEqual(SalesOrderChargeCollection.Entities[0].GetAttributeValue<String>("gsc_description"), ChargesCollection.Entities[0].GetAttributeValue<String>("gsc_description"));
            Assert.AreEqual(SalesOrderChargeCollection.Entities[0].GetAttributeValue<String>("gsc_chargetype"), ChargesCollection.Entities[0].FormattedValues["gsc_chargetype"]);
            #endregion
        }
        #endregion

        #region Test Scenario : Charges Code field does not contain data

        [TestMethod]
        public void SetNullChargeAmount()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order Charge EntityCollection
            var SalesOrderChargeCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_ordercharge",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_ordercharge",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {                            
                            {"gsc_description", String.Empty},
                            {"gsc_chargesid", new EntityReference("gsc_cmn_charges", Guid.Empty)
                            { Name = "Sample Order"}},
                            {"gsc_chargetype", String.Empty},
                            {"gsc_amount", new Money(Decimal.Zero)},
                            {"gsc_free", false}
                        }
                    }
                }
            };
            #endregion

            #region Charges EntityCollection
            var ChargesCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_charges",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_charges",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_description", "Delivery Charge"},
                            {"gsc_chargeamount", new Money((Decimal)800.00)},
                            {"gsc_chargetype", new OptionSetValue(0)}
                        },
                        FormattedValues = 
                        {
                            {"gsc_chargetype", "Delivery Charge"}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderChargeCollection.EntityName)
                ))).Returns(SalesOrderChargeCollection);

            //orgServiceMock.Setup((service => service.RetrieveMultiple(
            //    It.Is<QueryExpression>(expression => expression.EntityName == ChargesCollection.EntityName)
            //    ))).Returns(ChargesCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderChargeHandler = new SalesOrderChargeHandler(orgService, orgTracing);
            Entity salesOrderCharge = SalesOrderChargeHandler.SetChargeDetails(SalesOrderChargeCollection.Entities[0], "Create");

            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderChargeCollection.Entities[0].GetAttributeValue<Money>("gsc_amount").Value, Decimal.Zero);
            Assert.AreEqual(SalesOrderChargeCollection.Entities[0].GetAttributeValue<String>("gsc_description"), String.Empty);
            Assert.AreEqual(SalesOrderChargeCollection.Entities[0].GetAttributeValue<String>("gsc_chargetype"), String.Empty);
            #endregion
        }
        #endregion


        #endregion
    }
}
