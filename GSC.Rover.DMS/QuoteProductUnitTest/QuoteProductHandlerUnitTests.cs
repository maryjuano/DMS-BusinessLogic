using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.QuoteProduct;
using Moq;

namespace QuoteProductUnitTest
{
    [TestClass]
    public class QuoteProductHandlerUnitTests
    {
        //Created By:Raphael Herrera, Created On: 4/28/2016
        #region Set acutal cost of quote product
        #region Test Scenario: Quote product set to free
        [TestMethod]
        public void SetActualCost()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote Product Entity Collection
            var QuoteProductCollection = new EntityCollection
            {
                EntityName = "quotedetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "quotedetail",
                        Attributes = new AttributeCollection
                        {
                              {"gsc_free",  (bool)true},
                              {"gsc_amount",  new Money(10000)},
                        }
                    }
                }
            };
            #endregion


            #endregion

            #region 2. Call/Action
            QuoteProductHandler quoteproducthandler = new QuoteProductHandler(orgService, orgTracing);
            quoteproducthandler.setActualCost(QuoteProductCollection.Entities[0]);

            #endregion

            #region 3. Verify
            Assert.AreEqual(0, QuoteProductCollection[0].GetAttributeValue<Money>("gsc_amount").Value);
            #endregion

        }
        #endregion
        #endregion
    }
}
