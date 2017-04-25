using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.AppliedCharges;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace AppliedChargesUnitTests
{
    [TestClass]
    public class AppliedChargesHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 2/22/2016
        #region Applied Charges - Set Total Charges Amount

        #region Test Scenario : Set 'Total Charges Amount' field value in Quote Entity
        
        [TestMethod]
        public void SetTotalChargesAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Applied Charges EntityCollection
            var AppliedChargesCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_appliedcharges",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_appliedcharges",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", Guid.NewGuid())
                            { Name = "Sample Quote"}},
                            {"gsc_chargeamount", new Money((Decimal)320000.00)},
                            {"gsc_free", false}
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
                        Id = AppliedChargesCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_quoteid").Id,
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_totalchargesamount", ""},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == AppliedChargesCollection.EntityName)
                ))).Returns(AppliedChargesCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            #endregion

            #region 2. Call/Action

            var AppliedChargesHandler = new AppliedChargesHandler();
            Entity quote = AppliedChargesHandler.SetTotalChargesAmount(AppliedChargesCollection.Entities[0], orgService, orgTracing, "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(AppliedChargesCollection.Entities[0].GetAttributeValue<Money>("gsc_chargeamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_totalchargesamount").Value);
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

            #region Applied Charges EntityCollection
            var AppliedChargesCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_appliedcharges",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_appliedcharges",
                        EntityState = EntityState.Changed,
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
                        LogicalName = "gsc_cmn_appliedcharges",
                        EntityState = EntityState.Changed,
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
                        Id = AppliedChargesCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_quoteid").Id,
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_totalchargesamount", ""},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == AppliedChargesCollection.EntityName)
                ))).Returns(AppliedChargesCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            #endregion

            #region 2. Call/Action

            var AppliedChargesHandler = new AppliedChargesHandler();
            Entity quote = AppliedChargesHandler.SetTotalChargesAmount(AppliedChargesCollection.Entities[0], orgService, orgTracing, "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(AppliedChargesCollection.Entities[0].GetAttributeValue<Money>("gsc_chargeamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_totalchargesamount").Value);
            #endregion

        }
        #endregion

        #endregion
    }
}
