using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Moq;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.MonthlyAmortization;
using System.Collections.Generic;

namespace MonthlyAmortizationUnitTests
{
    [TestClass]
    public class MonthlyAmortizationUnitTests
    {
        //Created By: Leslie Baliguat, Created On: 3/4/2016
        #region ReplicateMonthlyAmortization

        #region Update Net Monthly Amortization in Quote
        [TestMethod]

        public void UpdateNetMonthlyAmortization()
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
                        LogicalName = "qoute",
                        Attributes =
                        {
                            {"gsc_netmonthlyamortization", ""}
                        }
                    }
                }
            };
            #endregion

            #region Monthly Amortization Entity Collection
            var MonthlyAmortizationCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_monthlyamortization",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_monthlyamortization",
                        Attributes =
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)},
                            {"gsc_isselected", true},
                            {"gsc_monthlyamortizationpn", "61875.00"}
                        }

                    }
                }
            };
            #endregion

            #region Monthly Amortization Entity Collection
            var MonthlyAmortizationTestCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_monthlyamortization",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_monthlyamortization",
                        Attributes =
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)},
                            {"gsc_isselected", false},
                            {"gsc_monthlyamortizationpn", "46406.25"}
                        }

                    },
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
               It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
               ))).Returns(QuoteCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == QuoteCollection.Entities[0].LogicalName)))).Callback<Entity>(s => QuoteCollection.Entities[0] = s);
            
            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == MonthlyAmortizationTestCollection.EntityName)
              ))).Returns(MonthlyAmortizationTestCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == MonthlyAmortizationTestCollection.Entities[0].LogicalName)))).Callback<Entity>(s => MonthlyAmortizationTestCollection.Entities[0] = s);
            
            #endregion

            #region 2. Call / Action
            var monthlyAmortizationHandler = new MonthlyAmortizationHandler();
            monthlyAmortizationHandler.ReplicateMonthlyAmortization(MonthlyAmortizationCollection.Entities[0], orgService, orgTracing);
            #endregion

            #region 3. Verify
            Assert.AreEqual(MonthlyAmortizationCollection.Entities[0]["gsc_monthlyamortizationpn"], QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_netmonthlyamortization").Value.ToString());
            Assert.AreEqual(false, MonthlyAmortizationTestCollection.Entities[0]["gsc_isselected"]);
            #endregion
        }

        #endregion

        #region Update gsc_isselected in Monthly Amoritzation Records with the same Quote record which has true in gsc_isselected
        [TestMethod]

        public void UpdateisSelected()
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
                        LogicalName = "qoute",
                        Attributes =
                        {
                            {"gsc_netmonthlyamortization", ""}
                        }
                    }
                }
            };
            #endregion

            #region Monthly Amortization Entity Collection
            var MonthlyAmortizationCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_monthlyamortization",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_monthlyamortization",
                        Attributes =
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)},
                            {"gsc_isselected", true},
                            {"gsc_monthlyamortizationpn", "61875.00"}
                        }

                    }
                }
            };
            #endregion

            #region Monthly Amortization Entity Collection
            var MonthlyAmortizationTestCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_monthlyamortization",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_monthlyamortization",
                        Attributes =
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)},
                            {"gsc_isselected", true},
                            {"gsc_monthlyamortizationpn", "46406.25"}
                        }

                    },
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
               It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
               ))).Returns(QuoteCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == QuoteCollection.Entities[0].LogicalName)))).Callback<Entity>(s => QuoteCollection.Entities[0] = s);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == MonthlyAmortizationTestCollection.EntityName)
              ))).Returns(MonthlyAmortizationTestCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == MonthlyAmortizationTestCollection.Entities[0].LogicalName)))).Callback<Entity>(s => MonthlyAmortizationTestCollection.Entities[0] = s);

            #endregion

            #region 2. Call / Action
            var monthlyAmortizationHandler = new MonthlyAmortizationHandler();
            monthlyAmortizationHandler.ReplicateMonthlyAmortization(MonthlyAmortizationCollection.Entities[0], orgService, orgTracing);
            #endregion

            #region 3. Verify
            Assert.AreEqual(false, MonthlyAmortizationTestCollection.Entities[0]["gsc_isselected"]);
            #endregion
        }

        #endregion
       
        #region Update Net Monthly Amortization in Quote where no selected Monthly Amortization
        [TestMethod]

        public void UpdateNetMonthlyAmortizationNull()
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
                        LogicalName = "qoute",
                        Attributes =
                        {
                            {"gsc_netmonthlyamortization", new Money(431)}
                        }
                    }
                }
            };
            #endregion

            #region Monthly Amortization Entity Collection
            var MonthlyAmortizationCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_monthlyamortization",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_monthlyamortization",
                        Attributes =
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)},
                            {"gsc_isselected", false},
                            {"gsc_monthlyamortizationpn", "61875.00"}
                        }

                    }
                }
            };
            #endregion

            #region Monthly Amortization Entity Collection
            var MonthlyAmortizationTestCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_monthlyamortization",
                Entities =
                {
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
               It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
               ))).Returns(QuoteCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == QuoteCollection.Entities[0].LogicalName)))).Callback<Entity>(s => QuoteCollection.Entities[0] = s);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == MonthlyAmortizationTestCollection.EntityName)
              ))).Returns(MonthlyAmortizationTestCollection);

            //orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == MonthlyAmortizationTestCollection.Entities[0].LogicalName)))).Callback<Entity>(s => MonthlyAmortizationTestCollection.Entities[0] = s);

            #endregion

            #region 2. Call / Action
            var monthlyAmortizationHandler = new MonthlyAmortizationHandler();
            monthlyAmortizationHandler.ReplicateMonthlyAmortization(MonthlyAmortizationCollection.Entities[0], orgService, orgTracing);
            #endregion

            #region 3. Verify
            Assert.AreEqual(null, QuoteCollection.Entities[0]["gsc_netmonthlyamortization"]);
            #endregion
        }

        #endregion
        #endregion
    }
}
