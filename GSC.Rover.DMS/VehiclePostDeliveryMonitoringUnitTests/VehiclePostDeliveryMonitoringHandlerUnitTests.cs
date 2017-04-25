using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using GSC.Rover.DMS.BusinessLogic.VehiclePostDeliveryMonitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VehiclePostDeliveryMonitoringUnitTests
{
    [TestClass]
    public class VehiclePostDeliveryMonitoringHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 6/10/2016
        #region Validate Survey Transactions

        #region Test Scenario : Survey Transactions record has no given answer
        [TestMethod]
        public void ValidateSurveyTransactions()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Vehicle Post-Delivery Monitoring EntityCollection
            var VehiclePostDeliveryMonitoring = new EntityCollection
            {
                EntityName = "gsc_sls_vehiclepostdeliverymonitoring",
                Entities = 
                { 
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_vehiclepostdeliverymonitoring",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_surveytransactionsid", new EntityReference("gsc_cmn_surveytransactions", Guid.NewGuid())},
                            {"gsc_completed",  true}
                        }
                    }
                }
            };
            #endregion

            #region Survey Transactions EntityCollections
            var SurveyTransactions = new EntityCollection
            {
                EntityName = "gsc_cmn_surveytransactions",
                Entities =
                {
                    new Entity
                    {
                        Id = VehiclePostDeliveryMonitoring.Entities[0].Id,
                        LogicalName = "gsc_cmn_surveytransactions",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            //{"gsc_yesornoanswer", new OptionSetValue()},
                            //{"gsc_textanswer", String.Empty}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehiclePostDeliveryMonitoring.EntityName)
                ))).Returns(VehiclePostDeliveryMonitoring);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SurveyTransactions.EntityName)
                ))).Returns(SurveyTransactions);
            #endregion

            #region 2. Call / Action
            var VehiclePostDeliveryMonitoringHandler = new VehiclePostDeliveryMonitoringHandler(orgService, orgTracing);
            //Entity vehiclePostDeliveryMonitoring = VehiclePostDeliveryMonitoringHandler.ValidateSurveyQuestions(VehiclePostDeliveryMonitoring.Entities[0]);
            #endregion

            #region 3. Verify
            //Assert.AreEqual();
            #endregion
        }
        #endregion

        #endregion
    }
}
