using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.QuoteDiscount;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace QuoteDiscountUnitTests
{
    [TestClass]
    public class QuoteDiscountHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 3/11/2016
        //Mofified By: Leslie Baliguat, Modified On: 3/16/2016 --> Add unit test of apply amounts
        #region Quote Discount - Set Total Discount Amount

        #region Test Scenario : Set 'Total Discount Amount' value of Quote entity

        [TestMethod]
        public void SetTotalDiscountAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote Discount EntityCollection
            var QuoteDiscountCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_quotediscount",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_quotediscount",
                        EntityState = EntityState.Created,
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
            double dppercentage = 40;
            double afpercentage = 40;
            double uppercentage = 20;
            var QuoteCollection = new EntityCollection
            {
                EntityName = "quote",
                Entities =
                {
                    new Entity
                    {
                        Id = QuoteDiscountCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_quoteid").Id,
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"totaldiscountamount", new Money(0)},
                            {"gsc_applytodppercentage", dppercentage},
                            {"gsc_applytodpamount",  new Money()},
                            {"gsc_applytoafpercentage",  afpercentage},
                            {"gsc_applytoafamount",  new Money()},
                            {"gsc_applytouppercentage",  uppercentage},
                            {"gsc_applytoupamount",  new Money()},
                            {"statecode", new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteDiscountCollection.EntityName)
                ))).Returns(QuoteDiscountCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(QuoteCollection.Entities[0]);

            #endregion

            #region 2. Call/Action

            var QuoteDiscountHandler = new QuoteDiscountHandler(orgService, orgTracing);
            Entity quote = QuoteDiscountHandler.SetQuoteTotalDiscountAmount(QuoteDiscountCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(QuoteDiscountCollection.Entities[0].GetAttributeValue<Money>("gsc_discountamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("totaldiscountamount").Value);
            Assert.AreEqual(40000, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_applytodpamount").Value);
            Assert.AreEqual(40000, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_applytoafamount").Value);
            Assert.AreEqual(20000, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_applytoupamount").Value);
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

            #region Quote Discount EntityCollection
            var QuoteDiscountCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_quotediscount",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_quotediscount",
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
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteDiscountCollection.EntityName)
                ))).Returns(QuoteDiscountCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(QuoteCollection.Entities[0]);

            #endregion

            #region 2. Call/Action

            var QuoteDiscountHandler = new QuoteDiscountHandler(orgService, orgTracing);
            Entity quote = QuoteDiscountHandler.SetQuoteTotalDiscountAmount(QuoteDiscountCollection.Entities[1], "Delete");
            #endregion

            #region 3. Verify
            Assert.AreEqual(QuoteDiscountCollection.Entities[1].GetAttributeValue<Money>("gsc_discountamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("totaldiscountamount").Value);
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
                            {"gsc_lessdiscount", new Money(Decimal.Zero)},
                            {"gsc_lessdiscountaf", new Money(Decimal.Zero)},
                            {"gsc_totaldiscount", new Money(Decimal.Zero)},
                            {"gsc_applytodpamount", new Money(Decimal.Zero)},
                            {"gsc_applytoafamount", new Money(Decimal.Zero)},
                            {"gsc_applytoupamount", new Money(Decimal.Zero)},
                            {"statecode" , new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            #region Quote Discount EntityCollection
            var QuoteDiscountCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_quotediscount",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_quotediscount",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)
                            { Name = "Sample Quote Discount 1"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)},
                            {"gsc_applyamounttodp", new Money((Decimal)100000.00)},
                            {"gsc_applyamounttoaf", new Money((Decimal)200000.00)},
                            {"gsc_applyamounttoup", new Money((Decimal)300000.00)}
                        }
                    },
                    
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_quotediscount",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesorderid", new EntityReference("quote", QuoteCollection.Entities[0].Id)
                            { Name = "Sample Quote 2 Discount"}},
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
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteDiscountCollection.EntityName)
                ))).Returns(QuoteDiscountCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            #endregion

            #region 2. Call/Action

            var QuoteDiscountHandler = new QuoteDiscountHandler(orgService, orgTracing);
            //Entity quote = QuoteDiscountHandler.SetLessDiscountValues(QuoteDiscountCollection.Entities[0], "Update");
            #endregion

            #region 3. Verify
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_lessdiscount").Value, 400000);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_lessdiscountaf").Value, 400000);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_totaldiscount").Value, 400000);
            #endregion
        }
        #endregion


        #endregion
    }
}
