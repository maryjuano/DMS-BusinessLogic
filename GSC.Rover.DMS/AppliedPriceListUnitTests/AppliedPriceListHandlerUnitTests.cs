using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.AppliedPriceList;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace AppliedPriceListUnitTests
{
    [TestClass]
    public class AppliedPriceListHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 2/19/2016
        #region Price List Item - Set Total Discount Amount

        #region Test Scenario : Set 'Total Discount Amount' value from Applied Price List entity
        
        [TestMethod]
        public void SetTotalDiscountAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Applied Price List EntityCollection
            var AppliedPriceListCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_appliedpricelist",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_appliedpricelist",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", Guid.NewGuid())
                            { Name = "Sample Quote"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)}
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
                        Id = AppliedPriceListCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_quoteid").Id,
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"totaldiscountamount", ""},
                            {"statecode", new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == AppliedPriceListCollection.EntityName)
                ))).Returns(AppliedPriceListCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            #endregion

            #region 2. Call/Action

            var AppliedPriceListHandler = new AppliedPriceListHandler(orgService, orgTracing);
            Entity quote = AppliedPriceListHandler.SetTotalDiscountAmountQuote(AppliedPriceListCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(AppliedPriceListCollection.Entities[0].GetAttributeValue<Money>("gsc_discountamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("totaldiscountamount").Value);
            #endregion

        }
        #endregion

        #region Test Scenario : Delete an Applied Price List record then update 'Total Discount Amount' value in Quote record

        [TestMethod]
        public void ComputeDeductedTotalDiscountAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote EntityCollection
            var QuoteCollection = new EntityCollection
            {
                EntityName = "quote",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"totaldiscountamount", ""},
                            {"statecode" , new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            #region Applied Price List EntityCollection
            var AppliedPriceListCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_appliedpricelist",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_appliedpricelist",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)
                            { Name = "Sample Quote 1"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)}
                        }
                    },
                    
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_appliedpricelist",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)
                            { Name = "Sample Quote 2"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)}
                        }
                    }
                }
            };
            #endregion
                       
            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == AppliedPriceListCollection.EntityName)
                ))).Returns(AppliedPriceListCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            #endregion

            #region 2. Call/Action

            var AppliedPriceListHandler = new AppliedPriceListHandler(orgService, orgTracing);
            Entity quote = AppliedPriceListHandler.SetTotalDiscountAmountQuote(AppliedPriceListCollection.Entities[1], "Delete");
            #endregion

            #region 3. Verify
            Assert.AreEqual(AppliedPriceListCollection.Entities[1].GetAttributeValue<Money>("gsc_discountamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("totaldiscountamount").Value);
            #endregion

        }
        #endregion

        #endregion
    }
}
