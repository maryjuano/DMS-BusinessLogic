using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.QuoteCharge;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace QuoteChargeUnitTests
{
    [TestClass]
    public class QuoteChargeHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 3/11/2016
        #region Quote Charge - Set Quote Total Charges Amount

        #region Test Scenario : Set 'Total Charges Amount' field value in Quote Entity

        [TestMethod]
        public void SetQuoteTotalChargesAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote Charge EntityCollection
            var QuoteChargeCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_quotecharge",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_quotecharge",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", Guid.NewGuid())
                            { Name = "Sample Quote"}},
                            {"gsc_chargeamount", new Money((Decimal)320000.00)},
                            {"gsc_free", false}
                        }
                    }
                }
            };
            #endregion


            #region Quote EntityCollection
            var QuoteCollection = new EntityCollection
            {
                EntityName = "quote",
                Entities =
                {
                    new Entity
                    {
                        Id = QuoteChargeCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_quoteid").Id,
                        LogicalName = "quote",
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
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteChargeCollection.EntityName)
                ))).Returns(QuoteChargeCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            #endregion

            #region 2. Call/Action

            var QuoteChargeHandler = new QuoteChargeHandler(orgService, orgTracing);
            Entity quote = QuoteChargeHandler.SetQuoteTotalChargesAmount(QuoteChargeCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(QuoteChargeCollection.Entities[0].GetAttributeValue<Money>("gsc_chargeamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_totalchargesamount").Value);
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

            #region Quote Charge EntityCollection
            var QuoteChargeCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_quotecharge",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_quotecharge",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", Guid.NewGuid())
                            { Name = "Sample Quote"}},
                            {"gsc_chargeamount", new Money((Decimal)320000.00)},
                            {"gsc_free", false}
                        }
                    },

                    //Applied Charges records with charge amount value but with 'Free' checked
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_quotecharge",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", Guid.NewGuid())
                            { Name = "Sample Quote"}},
                            {"gsc_chargeamount", new Money((Decimal)80000.00)},
                            {"gsc_free", true}
                        }
                    },

                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_appliedcharges",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", Guid.NewGuid())
                            { Name = "Sample Quote"}},
                            {"gsc_chargeamount", new Money((Decimal)80000.00)},
                            {"gsc_free", true}
                        }
                    }
                }
            };
            #endregion

            #region Quote EntityCollection
            var QuoteCollection = new EntityCollection
            {
                EntityName = "quote",
                Entities =
                {
                    new Entity
                    {
                        Id = QuoteChargeCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_quoteid").Id,
                        LogicalName = "quote",
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
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteChargeCollection.EntityName)
                ))).Returns(QuoteChargeCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            #endregion

            #region 2. Call/Action

            var QuoteChargeHandler = new QuoteChargeHandler(orgService, orgTracing);
            Entity quote = QuoteChargeHandler.SetQuoteTotalChargesAmount(QuoteChargeCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(QuoteChargeCollection.Entities[0].GetAttributeValue<Money>("gsc_chargeamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_totalchargesamount").Value);
            #endregion

        }
        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On : 3/28/2016
        #region ReplicateChargeAmount

        #region Test Scenario: On Create
        [TestMethod]

        public void ReplicateChargeAmountOnCreate()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Charge EntityCollection
            var ChargeCollection = new EntityCollection
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
                            {"gsc_description", "Description"},
                            {"gsc_chargeamount", new Money(20000)},
                            {"gsc_chargetype", new OptionSetValue(1)}
                        }
                    }
                }
            };
            #endregion

            #region QuoteCharge Entity
            var QuoteEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                }
            };
            #endregion

            #region QuoteCharge Entity
            var QuoteChargeEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_cmn_quotecharge",
                Attributes =
                {
                    {"gsc_quoteid", new EntityReference(QuoteEntity.LogicalName, QuoteEntity.Id)},
                    {"gsc_chargesid", new EntityReference(ChargeCollection.EntityName, ChargeCollection.Entities[0].Id)},
                    {"gsc_description", ""},
                    {"gsc_chargeamount", new Money()},
                    {"gsc_chargetype", new OptionSetValue()}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ChargeCollection.EntityName)
                  ))).Returns(ChargeCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(QuoteChargeEntity);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == QuoteChargeEntity.LogicalName)))).Callback<Entity>(s => QuoteChargeEntity = s);

            #endregion

            #region 2. Call/Action

            var QuoteChargeHandler = new QuoteChargeHandler(orgService, orgTracing);
            Entity quote = QuoteChargeHandler.ReplicateChargeAmount(QuoteChargeEntity, "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(ChargeCollection.Entities[0]["gsc_description"], quote["gsc_description"]);
            Assert.AreEqual(ChargeCollection.Entities[0].GetAttributeValue<Money>("gsc_chargeamount").Value, quote.GetAttributeValue<Money>("gsc_chargeamount").Value);
            Assert.AreEqual(ChargeCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_chargetype").Value, quote.GetAttributeValue<OptionSetValue>("gsc_chargetype").Value);
            #endregion
        }
        #endregion

        #region Test Scenario: On Save
        [TestMethod]

        public void ReplicateChargeAmountOnSave()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Charge EntityCollection
            var ChargeCollection = new EntityCollection
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
                            {"gsc_description", "Description"},
                            {"gsc_chargeamount", new Money(20000)},
                            {"gsc_chargetype", new OptionSetValue(1)}
                        }
                    }
                }
            };
            #endregion

            #region QuoteCharge Entity
            var QuoteEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                }
            };
            #endregion

            #region QuoteCharge Entity
            var QuoteChargeEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_cmn_quotecharge",
                Attributes =
                {
                    {"gsc_quoteid", new EntityReference(QuoteEntity.LogicalName, QuoteEntity.Id)},
                    {"gsc_chargesid", new EntityReference(ChargeCollection.EntityName, ChargeCollection.Entities[0].Id)},
                    {"gsc_description", ""},
                    {"gsc_chargeamount", new Money()},
                    {"gsc_chargetype", new OptionSetValue()}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ChargeCollection.EntityName)
                  ))).Returns(ChargeCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(QuoteChargeEntity);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == QuoteChargeEntity.LogicalName)))).Callback<Entity>(s => QuoteChargeEntity = s);

            #endregion

            #region 2. Call/Action

            var QuoteChargeHandler = new QuoteChargeHandler(orgService, orgTracing);
            Entity quote = QuoteChargeHandler.ReplicateChargeAmount(QuoteChargeEntity, "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(ChargeCollection.Entities[0]["gsc_description"], QuoteChargeEntity["gsc_description"]);
            Assert.AreEqual(ChargeCollection.Entities[0].GetAttributeValue<Money>("gsc_chargeamount").Value, QuoteChargeEntity.GetAttributeValue<Money>("gsc_chargeamount").Value);
            Assert.AreEqual(ChargeCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_chargetype").Value, QuoteChargeEntity.GetAttributeValue<OptionSetValue>("gsc_chargetype").Value);
            #endregion
        }
        #endregion

        #endregion
    }
}
