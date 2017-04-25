using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.PriceListItem;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace PriceListItemUnitTests
{
    [TestClass]
    public class PriceListItemHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 2/18/2016
        #region Price List Item - Set Standard Sell Price Amount
        [TestMethod]

        #region Test Scenario : Price List lookup field has 'Standard Price List' value
        public void SetStandardSellPriceAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Price List Item EntityCollection
            var PriceListItemCollection = new EntityCollection
            {
                EntityName = "productpricelevel",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "productpricelevel",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"pricelevelid", new EntityReference("pricelevel", Guid.NewGuid())
                            { Name = "Standard Price List"}},
                            {"productid", new EntityReference("product", Guid.NewGuid())
                            { Name = "Montero"}},
                            {"amount", new Money(((Decimal)2000000.00))}
                        }
                    }
                }
            };
            #endregion

            #region Product EntityCollection
            var ProductCollection = new EntityCollection
            {
                EntityName = "product",
                Entities =
                {
                    new Entity
                    {
                        Id = PriceListItemCollection.Entities[0].GetAttributeValue<EntityReference>("productid").Id,
                        LogicalName = "product",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_sellprice", new Money(Decimal.Zero)},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == PriceListItemCollection.EntityName)
                ))).Returns(PriceListItemCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
                ))).Returns(ProductCollection);

            #endregion

            #region 2. Call/Action

            var PriceListItemHandler = new PriceListItemHandler(orgService, orgTracing);
            //Entity product = PriceListItemHandler.SetStandardSellPriceAmount(PriceListItemCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            //Assert.AreEqual(PriceListItemCollection.Entities[0].GetAttributeValue<Money>("amount").Value, ProductCollection.Entities[0].GetAttributeValue<Money>("gsc_sellprice").Value);
            #endregion

        }
        #endregion

        #endregion
    }
}
