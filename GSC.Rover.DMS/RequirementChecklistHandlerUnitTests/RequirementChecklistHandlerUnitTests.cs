using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
//using GSC.Rover.DMS.BusinessLogic.RequirementsChecklist;
using GSC.Rover.DMS.BusinessLogic.SalesOrder;
using Moq;

namespace RequirementChecklistHandlerUnitTests
{
    [TestClass]
    public class UnitTest1
    {
        //Created By:Raphael Herrera, Created On: 4/29/2016
        #region Check for complete requirements
        [TestMethod]

        #region Test Scenario: Complete requirements
        public void checkForCompleteRequirements()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;
            #region Sales Order Entity Collection
            var SalesOrderCollection = new EntityCollection()
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        Attributes =
                        {
                            {"gsc_status", new OptionSetValue(1)}
                        },
                        FormattedValues = 
                        {
                            {"gsc_status","Test"},
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        Attributes =
                        {
                            {"gsc_status", new OptionSetValue(1)}
                        },
                        FormattedValues = 
                        {
                            {"gsc_status","Test"},
                        }
                    }
                }
            };

            #endregion
            #region Requirement Checklist Entity Collection
            var RequirementChecklistCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_requirementchecklist",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_requirementchecklist",
                        Attributes =
                        {
                            {"gsc_submitted", (bool) true},
                            {"gsc_orderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)}
                        }
                    },
                     new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_requirementchecklist",
                        Attributes =
                        {
                            {"gsc_submitted", (bool) true},
                            {"gsc_orderid", new EntityReference("salesorder", SalesOrderCollection.Entities[1].Id)}
                        }
                    },
                     new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_requirementchecklist",
                        Attributes =
                        {
                            {"gsc_submitted", (bool) true},
                            {"gsc_orderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == RequirementChecklistCollection.EntityName)
                ))).Returns(RequirementChecklistCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(SalesOrderCollection.Entities[0]);

            #endregion

            #region 2. Call/Action
            //RequirementsChecklistHandler handler = new RequirementsChecklistHandler(orgService, orgTracing);
            SalesOrderHandler soHandler = new SalesOrderHandler(orgService, orgTracing);
            //bool res = handler.checkForCompleteRequirements(RequirementChecklistCollection.Entities[0]);
            //if (res)
            //    soHandler.SetStatus(RequirementChecklistCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_orderid").Id);

            #endregion

            #region 3. Verify
            //Assert.AreEqual(true,res);
            Assert.AreEqual(new OptionSetValue(100000002), SalesOrderCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_status"));
            #endregion

        }
        #endregion
        #endregion
    }
}
