using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Moq;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.VehicleCountEntryDetail;

namespace VehicleCountEntryDetailUnitTests
{
    [TestClass]
    public class VehicleCountEntryDetailHandlerUnitTest
    {
        #region ComputeVariance and UpdateStatus
        [TestMethod]
        public void ComputeVarianceAndUpdateStatus()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;


            #region VehicleCountSchedule EntityCollection
            var VehicleCountSchedule = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountschedule",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountschedule",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_description", "Put desc here."},
                            {"gsc_status", new OptionSetValue(100000000)},
                            {"gsc_documentdate", DateTime.Today}
                        }
                    }
                }
            };
            #endregion

            #region VehicleCountEntry EntityCollection
            var VehicleCountEntry = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountentry",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountentry",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountscheduleid", new EntityReference(VehicleCountSchedule.EntityName, VehicleCountSchedule.Entities[0].Id)},
                            {"gsc_status", new OptionSetValue(100000000)}
                        }
                    }
                }
            };
            #endregion

            #region VehicleCountEntryDetail EntityCollection
            var VehicleCountEntryDetail = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountentrydetails",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountentrydetails",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountentryid", new EntityReference(VehicleCountEntry.EntityName, VehicleCountEntry.Entities[0].Id)},
                            {"gsc_onhandqty", 50},
                            {"gsc_countedqty", 39},
                            {"gsc_varianceqty", 0}
                        }
                    }
                }
            };
            #endregion


            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(VehicleCountEntryDetail.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == VehicleCountEntryDetail.EntityName)))).Callback<Entity>(s => VehicleCountEntryDetail.Entities[0] = s);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountEntry.EntityName)
                ))).Returns(VehicleCountEntry);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == VehicleCountEntry.EntityName)))).Callback<Entity>(s => VehicleCountEntry.Entities[0] = s);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountSchedule.EntityName)
                ))).Returns(VehicleCountSchedule);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == VehicleCountSchedule.EntityName)))).Callback<Entity>(s => VehicleCountSchedule.Entities[0] = s);

            #endregion

            #region 2. Call/Action

            var VehicleCountEntryDetailHandler = new VehicleCountEntryDetailHandler(orgService, orgTracing);
            VehicleCountEntryDetailHandler.ComputeVariance(VehicleCountEntryDetail.Entities[0]);
            VehicleCountEntryDetailHandler.UpdateStatus(VehicleCountEntryDetail.Entities[0]);
           
            #endregion

            #region 3. Verify
            Assert.AreEqual(11, VehicleCountEntryDetail.Entities[0].GetAttributeValue<Int32>("gsc_varianceqty"));
            Assert.AreEqual(100000001, VehicleCountEntry.Entities[0].GetAttributeValue<OptionSetValue>("gsc_status").Value);
            Assert.AreEqual(100000002, VehicleCountSchedule.Entities[0].GetAttributeValue<OptionSetValue>("gsc_status").Value);


            #endregion

        }
        #endregion

        #region ComputeVerifiedCountedQty

        #region Scenario 1: Verified < Counted
        [TestMethod]
        public void VerifiedLessThanCounted()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region VehicleCountEntryDetail EntityCollection
            var VehicleCountEntryDetail = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountentrydetails",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountentrydetails",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_countedqty", 3},
                            {"gsc_verified", false}
                        }
                    }
                }
            };
            #endregion

            #region Vehicle Count Breakdown EntityCollection
            var VehicleCounBreakdown = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountbreakdown",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountbreakdown",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountentrydetailid", new EntityReference(VehicleCountEntryDetail.EntityName, VehicleCountEntryDetail.Entities[0].Id)},
                            {"gsc_verified", true}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountbreakdown",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountentrydetailid", new EntityReference(VehicleCountEntryDetail.EntityName, VehicleCountEntryDetail.Entities[0].Id)},
                            {"gsc_verified", true}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCounBreakdown.EntityName)
                ))).Returns(VehicleCounBreakdown);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(VehicleCountEntryDetail.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == VehicleCountEntryDetail.EntityName)))).Callback<Entity>(s => VehicleCountEntryDetail.Entities[0] = s);

            #endregion

            #region 2. Call/Action

            var VehicleCountEntryDetailHandler = new VehicleCountEntryDetailHandler(orgService, orgTracing);

            VehicleCountEntryDetailHandler.ComputeVerifiedCountedQty(VehicleCountEntryDetail.Entities[0]);

            #endregion

            #region 3. Verify
            Assert.AreEqual(false, VehicleCountEntryDetail.Entities[0].GetAttributeValue<Boolean>("gsc_verified"));
            #endregion

        }
        #endregion

        #region Scenario 2: Verified = Counted
        [TestMethod]
        public void VerifiedEqualsCounted()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region VehicleCountEntryDetail EntityCollection
            var VehicleCountEntryDetail = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountentrydetails",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountentrydetails",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_countedqty", 2},
                            {"gsc_verified", false}
                        }
                    }
                }
            };
            #endregion

            #region Vehicle Count Breakdown EntityCollection
            var VehicleCounBreakdown = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountbreakdown",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountbreakdown",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountentrydetailid", new EntityReference(VehicleCountEntryDetail.EntityName, VehicleCountEntryDetail.Entities[0].Id)},
                            {"gsc_verified", true}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountbreakdown",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountentrydetailid", new EntityReference(VehicleCountEntryDetail.EntityName, VehicleCountEntryDetail.Entities[0].Id)},
                            {"gsc_verified", true}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCounBreakdown.EntityName)
                ))).Returns(VehicleCounBreakdown);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(VehicleCountEntryDetail.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == VehicleCountEntryDetail.EntityName)))).Callback<Entity>(s => VehicleCountEntryDetail.Entities[0] = s);

            #endregion

            #region 2. Call/Action

            var VehicleCountEntryDetailHandler = new VehicleCountEntryDetailHandler(orgService, orgTracing);

            VehicleCountEntryDetailHandler.ComputeVerifiedCountedQty(VehicleCountEntryDetail.Entities[0]);

            #endregion

            #region 3. Verify
            Assert.AreEqual(true, VehicleCountEntryDetail.Entities[0].GetAttributeValue<Boolean>("gsc_verified"));
            #endregion

        }
        #endregion

        #region Scenario 3: Verified > Counted
        [TestMethod]
        public void VerifiedGreaterThanCounted()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region VehicleCountEntryDetail EntityCollection
            var VehicleCountEntryDetail = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountentrydetails",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountentrydetails",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_countedqty", 1},
                            {"gsc_verified", false}
                        }
                    }
                }
            };
            #endregion

            #region Vehicle Count Breakdown EntityCollection
            var VehicleCounBreakdown = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountbreakdown",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountbreakdown",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountentrydetailid", new EntityReference(VehicleCountEntryDetail.EntityName, VehicleCountEntryDetail.Entities[0].Id)},
                            {"gsc_verified", true}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountbreakdown",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountentrydetailid", new EntityReference(VehicleCountEntryDetail.EntityName, VehicleCountEntryDetail.Entities[0].Id)},
                            {"gsc_verified", true}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCounBreakdown.EntityName)
                ))).Returns(VehicleCounBreakdown);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(VehicleCountEntryDetail.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == VehicleCountEntryDetail.EntityName)))).Callback<Entity>(s => VehicleCountEntryDetail.Entities[0] = s);

            #endregion

            #region 2. Call/Action

            var VehicleCountEntryDetailHandler = new VehicleCountEntryDetailHandler(orgService, orgTracing);

            VehicleCountEntryDetailHandler.ComputeVerifiedCountedQty(VehicleCountEntryDetail.Entities[0]);

            #endregion

            #region 3. Verify
            Assert.AreEqual(true, VehicleCountEntryDetail.Entities[0].GetAttributeValue<Boolean>("gsc_verified"));
            #endregion

        }
        #endregion

        #endregion
    }
}
