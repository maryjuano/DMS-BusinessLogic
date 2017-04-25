using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Moq;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.VehicleCountSchedule;
using System.Collections.Generic;

namespace VehicleCountScheduleUnitTests
{
    [TestClass]
    public class VehicleCountScheduleUnitTest
    {
        #region ApplyFilter - Link Entities not working

        //Link Entities not working
        [TestMethod]
        public void FilterResultEmpty()
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
                        {/*
                            {"gsc_siteid", new EntityReference("site", new Guid("e0968b43-af44-e611-80da-00155d010e2c"))},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2a"))},
                            {"gsc_productid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2b"))},
                            {"gsc_vehiclecolorid", new EntityReference("color", Guid.NewGuid()) { Name = "Blue"}},
                            {"gsc_modelcode", "0001"},
                            {"gsc_optioncode", "002"}*/
                            {"gsc_siteid", new EntityReference("site", new Guid("e0968b43-af44-e611-80da-00155d010e2c"))},
                            {"gsc_vehiclebasemodelid", null},
                            {"gsc_productid", null},
                            {"gsc_colo", ""},
                            {"gsc_modelcode", ""},
                            {"gsc_optioncode", ""}
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
                        Id = Guid.NewGuid(),
                        LogicalName = "product",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_modelcode", "0001"},
                            {"gsc_optioncode", "0002"}
                        }
                    }
                }
            };
            #endregion

            #region Product Quantity EntityCollection
            var ProductQuantityCollection = new EntityCollection
            {
                EntityName = "gsc_iv_productquantity",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_productquantity",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_siteid", new EntityReference("site", new Guid("e0968b43-af44-e611-80da-00155d010e2c"))},
                            {"gsc_vehiclemodelid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2a"))},
                            {"gsc_vehiclecolorid", new EntityReference("vehiclecolor", new Guid("e0968b43-af44-e611-80da-00155d010e1a"))},
                            {"gsc_productid", new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProductQuantityCollection.EntityName)
                ))).Returns(ProductQuantityCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
                ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountSchedule.EntityName)
                ))).Returns(VehicleCountSchedule);

            #endregion

            #region 2. Call/Action
            var VehicleCountScheduleHandler = new VehicleCountScheduleHandler(orgService, orgTracing);
            Entity filterResult = VehicleCountScheduleHandler.ApplyFilter(VehicleCountSchedule.Entities[0]);
            #endregion

            #region 3. Verify
            var ProductQuantityEntity = ProductQuantityCollection.Entities[0];
            var InventorySchedulEntity = ProductCollection.Entities[0];
            Assert.AreEqual(ProductQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclemodelid").Id, filterResult.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id);
            Assert.AreEqual(ProductQuantityEntity.GetAttributeValue<EntityReference>("gsc_productid").Id, filterResult.GetAttributeValue<EntityReference>("gsc_productid").Id);
            Assert.AreEqual(ProductQuantityEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id, filterResult.GetAttributeValue<EntityReference>("gsc_siteid").Id);
            Assert.AreEqual(ProductQuantityEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id, filterResult.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id);
            Assert.AreEqual(InventorySchedulEntity.GetAttributeValue<String>("gsc_modelcode"), filterResult.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(InventorySchedulEntity.GetAttributeValue<String>("gsc_optioncode"), filterResult.GetAttributeValue<String>("gsc_optioncode"));
            #endregion
        }

        #endregion

        #region ReplicateFilterResult

        #region Test Scenario 1: Filter Result Record doesn't exist in Vehicle for Counting
        [TestMethod]
        public void FilterResultNoDuplicate()
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
                        }
                    }
                }
            };
            #endregion

            #region VehicleCountScheduleFilter EntityCollection
            var VehicleCountScheduleFilter = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountschedulefilter",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountschedulefilter",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountscheduleid", new EntityReference(VehicleCountSchedule.EntityName, VehicleCountSchedule.Entities[0].Id)},
                            {"gsc_inventoryid", new EntityReference("inventory", new Guid("e0968b43-af44-e611-80da-00155d010e2d"))},
                            {"gsc_siteid", new EntityReference("site", new Guid("e0968b43-af44-e611-80da-00155d010e2c"))},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2a"))},
                            {"gsc_productid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2b"))},
                            {"gsc_color", "Black"},
                            {"gsc_modelcode", "0001"},
                            {"gsc_optioncode", "002"}
                        }
                    }
                }
            };
            #endregion

            #region VehicleCountScheduleDetail EntityCollection
            var VehicleCountScheduleDetail = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountscheduledetail",
                Entities =
                {
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountScheduleFilter.EntityName)
                ))).Returns(VehicleCountScheduleFilter);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountScheduleDetail.EntityName)
                ))).Returns(VehicleCountScheduleDetail);

            #endregion

            #region 2. Call/Action
            var VehicleCountScheduleHandler = new VehicleCountScheduleHandler(orgService, orgTracing);
            List<Entity> records = VehicleCountScheduleHandler.ReplicateFilterResult(VehicleCountSchedule.Entities[0]);
            #endregion

            #region 3. Verify
            var filter = VehicleCountScheduleFilter.Entities[0];

            foreach (var vehicle in records)
            {
                Assert.AreEqual(filter.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id, vehicle.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id);
                Assert.AreEqual(filter.GetAttributeValue<EntityReference>("gsc_productid").Id, vehicle.GetAttributeValue<EntityReference>("gsc_productid").Id);
                Assert.AreEqual(filter.GetAttributeValue<EntityReference>("gsc_siteid").Id, vehicle.GetAttributeValue<EntityReference>("gsc_siteid").Id);
                Assert.AreEqual(filter.GetAttributeValue<String>("gsc_color"), vehicle.GetAttributeValue<String>("gsc_color"));
                Assert.AreEqual(filter.GetAttributeValue<String>("gsc_modelcode"), vehicle.GetAttributeValue<String>("gsc_modelcode"));
                Assert.AreEqual(filter.GetAttributeValue<String>("gsc_optioncode"), vehicle.GetAttributeValue<String>("gsc_optioncode"));
            }
            #endregion
        }
        #endregion

        #region Test Scenario 2: Filter Result Record already exists in Vehicle for Counting
        [TestMethod]
        public void FilterResultDuplicate()
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
                        }
                    }
                }
            };
            #endregion

            #region VehicleCountScheduleFilter EntityCollection
            var VehicleCountScheduleFilter = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountschedulefilter",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountschedulefilter",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountscheduleid", new EntityReference(VehicleCountSchedule.EntityName, VehicleCountSchedule.Entities[0].Id)},
                            {"gsc_inventoryid", new EntityReference("inventory", new Guid("e0968b43-af44-e611-80da-00155d010e2d"))},
                            {"gsc_siteid", new EntityReference("site", new Guid("e0968b43-af44-e611-80da-00155d010e2c"))},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2a"))},
                            {"gsc_productid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2b"))},
                            {"gsc_color", "Black"},
                            {"gsc_modelcode", "0001"},
                            {"gsc_optioncode", "002"}
                        }
                    }
                }
            };
            #endregion

            #region VehicleCountScheduleDetail EntityCollection
            var VehicleCountScheduleDetail = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountscheduledetail",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountscheduledetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountscheduleid", new EntityReference(VehicleCountSchedule.EntityName, VehicleCountSchedule.Entities[0].Id)},
                            {"gsc_inventoryid", new EntityReference("inventory", new Guid("e0968b43-af44-e611-80da-00155d010e2d"))},
                            {"gsc_siteid", new EntityReference("site", new Guid("e0968b43-af44-e611-80da-00155d010e2c"))},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2a"))},
                            {"gsc_productid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2b"))},
                            {"gsc_color", "Black"},
                            {"gsc_modelcode", "0001"},
                            {"gsc_optioncode", "002"}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountScheduleFilter.EntityName)
                ))).Returns(VehicleCountScheduleFilter);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountScheduleDetail.EntityName)
                ))).Returns(VehicleCountScheduleDetail);

            #endregion

            #region 2. Call/Action
            var VehicleCountScheduleHandler = new VehicleCountScheduleHandler(orgService, orgTracing);
            List<Entity> records = VehicleCountScheduleHandler.ReplicateFilterResult(VehicleCountSchedule.Entities[0]);
            #endregion

            #region 3. Verify
            var filter = VehicleCountScheduleFilter.Entities[0];

            Assert.AreEqual(0, records.Count);

            #endregion
        }
        #endregion

        #endregion

        #region CreateVehicleCountEntry

        [TestMethod]
        public void CreateVehicleCountEntry()
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

            #endregion

            #region 2. Call/Action
            var VehicleCountScheduleHandler = new VehicleCountScheduleHandler(orgService, orgTracing);
            VehicleCountScheduleHandler.CreateVehicleCountEntry(VehicleCountSchedule.Entities[0]);
            #endregion

            #region 3. Verify
            #endregion
        }

        #endregion

        #region CreateVehicleCountEntryDetail

        [TestMethod]
        public void CreateVehicleCountEntryDetail()
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

            #region VehicleCountScheduleDetail EntityCollection
            var VehicleCountScheduleDetail = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountscheduledetail",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountscheduledetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountscheduleid", new EntityReference(VehicleCountSchedule.EntityName, VehicleCountSchedule.Entities[0].Id)},
                            {"gsc_inventoryid", new EntityReference("inventory", new Guid("e0968b43-af44-e611-80da-00155d010e2d"))},
                            {"gsc_siteid", new EntityReference("site", new Guid("e0968b43-af44-e611-80da-00155d010e2c"))},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2a"))},
                            {"gsc_productid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2b"))},
                            {"gsc_color", "Black"},
                            {"gsc_modelcode", "0001"},
                            {"gsc_optioncode", "002"}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountScheduleDetail.EntityName)
                ))).Returns(VehicleCountScheduleDetail);

            #endregion

            #region 2. Call/Action
            var VehicleCountScheduleHandler = new VehicleCountScheduleHandler(orgService, orgTracing);
            List<Entity> records = VehicleCountScheduleHandler.CreateVehicleCountEntryDetail(VehicleCountSchedule.Entities[0], new Guid("e0968b43-af44-e611-80da-00155d010e2d"));
            #endregion

            #region 3. Verify
            var schedDetail = VehicleCountScheduleDetail.Entities[0];
            var returnEntity = records[0];

            Assert.AreEqual(schedDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Name, returnEntity.GetAttributeValue<String>("gsc_basemodel"));
            Assert.AreEqual(schedDetail.GetAttributeValue<EntityReference>("gsc_productid").Name, returnEntity.GetAttributeValue<String>("gsc_vehiclecountentrydetailspn"));
            Assert.AreEqual(schedDetail.GetAttributeValue<EntityReference>("gsc_siteid").Name, returnEntity.GetAttributeValue<String>("gsc_site"));
            Assert.AreEqual(schedDetail.GetAttributeValue<String>("gsc_color"), returnEntity.GetAttributeValue<String>("gsc_color"));
            Assert.AreEqual(schedDetail.GetAttributeValue<String>("gsc_modelcode"), returnEntity.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(schedDetail.GetAttributeValue<String>("gsc_optioncode"), returnEntity.GetAttributeValue<String>("gsc_optioncode"));

            #endregion
        }

        #endregion

        #region CreateVehicleCountBreakdown

        [TestMethod]
        public void CreateVehicleCountBreakdown()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Inventory EntityCollection
            var Inventory = new EntityCollection
            {
                EntityName = "gsc_iv_inventory",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_inventory",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_productionno", "Prod1"},
                            {"gsc_csno", "CS1"},
                            {"gsc_engineno", "Engine1"},
                            {"gsc_vinno", "VIN1"},
                            {"gsc_color", "Black"}
                        }
                    }
                }
            };
            #endregion

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
                            {"gsc_inventoryid", new EntityReference(Inventory.EntityName, Inventory.Entities[0].Id)},
                            {"gsc_site", "Site 1"},
                            {"gsc_modelcode", "0001"},
                            {"gsc_optioncode", "002"}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountEntryDetail.EntityName)
                ))).Returns(VehicleCountEntryDetail);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == Inventory.EntityName)
                ))).Returns(Inventory);

            #endregion

            #region 2. Call/Action
            var VehicleCountScheduleHandler = new VehicleCountScheduleHandler(orgService, orgTracing);
            List<Entity> records = VehicleCountScheduleHandler.CreateVehicleCountBreakdown(VehicleCountSchedule.Entities[0], VehicleCountEntry.Entities[0].Id);
            #endregion

            #region 3. Verify
            var schedDetail = VehicleCountEntryDetail.Entities[0];
            var inventory = Inventory.Entities[0];
            var returnEntity = records[0];

            Assert.AreEqual(schedDetail.GetAttributeValue<String>("site"), returnEntity.GetAttributeValue<String>("site"));
            Assert.AreEqual(schedDetail.GetAttributeValue<String>("gsc_modelcode"), returnEntity.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(schedDetail.GetAttributeValue<String>("gsc_optioncode"), returnEntity.GetAttributeValue<String>("gsc_optioncode"));

            Assert.AreEqual(inventory.GetAttributeValue<String>("gsc_productionno"), returnEntity.GetAttributeValue<String>("gsc_productionno"));
            Assert.AreEqual(inventory.GetAttributeValue<String>("gsc_csno"), returnEntity.GetAttributeValue<String>("gsc_csno"));
            Assert.AreEqual(inventory.GetAttributeValue<String>("gsc_engineno"), returnEntity.GetAttributeValue<String>("gsc_engineno"));
            Assert.AreEqual(inventory.GetAttributeValue<String>("gsc_vinno"), returnEntity.GetAttributeValue<String>("gsc_vinno"));
            Assert.AreEqual(inventory.GetAttributeValue<String>("gsc_color"), returnEntity.GetAttributeValue<String>("gsc_color"));

            #endregion
        }

        #endregion

        #region CopyVehicleCountScheduleDetail - Encountered an error

        [TestMethod]
        public void CopyVehicleCountScheduleDetail()
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

            #region VehicleCountScheduleDetail EntityCollection
            var VehicleCountScheduleDetail = new EntityCollection
            {
                EntityName = "gsc_iv_vehiclecountscheduledetail",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclecountscheduledetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclecountscheduleid", new EntityReference(VehicleCountSchedule.EntityName, VehicleCountSchedule.Entities[0].Id)},
                            {"gsc_inventoryid", new EntityReference("inventory", new Guid("e0968b43-af44-e611-80da-00155d010e2d"))},
                            {"gsc_siteid", new EntityReference("site", new Guid("e0968b43-af44-e611-80da-00155d010e2c"))},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2a"))},
                            {"gsc_productid", new EntityReference("basemodel", new Guid("e0968b43-af44-e611-80da-00155d010e2b"))},
                            {"gsc_color", "Black"},
                            {"gsc_modelcode", "0001"},
                            {"gsc_optioncode", "002"},
                            {"gsc_lastcount", null}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleCountScheduleDetail.EntityName)
                ))).Returns(VehicleCountScheduleDetail);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(VehicleCountScheduleDetail.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == VehicleCountScheduleDetail.EntityName)))).Callback<Entity>(s => VehicleCountScheduleDetail.Entities[0] = s);

            #endregion

            #region 2. Call/Action
            var VehicleCountScheduleHandler = new VehicleCountScheduleHandler(orgService, orgTracing);
            Entity returnEntity = VehicleCountScheduleHandler.CopyVehicleCountScheduleDetail(VehicleCountSchedule.Entities[0], new Guid("e0968b43-af44-e611-80da-00155d010e2d"));
            #endregion

            #region 3. Verify
            var schedDetail = VehicleCountScheduleDetail.Entities[0];

            Assert.AreEqual(schedDetail.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Name, returnEntity.GetAttributeValue<String>("gsc_basemodel"));
            Assert.AreEqual(schedDetail.GetAttributeValue<EntityReference>("gsc_productid").Name, returnEntity.GetAttributeValue<String>("gsc_vehiclecountentrydetailspn"));
            Assert.AreEqual(schedDetail.GetAttributeValue<EntityReference>("gsc_siteid").Name, returnEntity.GetAttributeValue<String>("gsc_site"));
            Assert.AreEqual(schedDetail.GetAttributeValue<String>("gsc_color"), returnEntity.GetAttributeValue<String>("gsc_color"));
            Assert.AreEqual(schedDetail.GetAttributeValue<String>("gsc_modelcode"), returnEntity.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(schedDetail.GetAttributeValue<String>("gsc_optioncode"), returnEntity.GetAttributeValue<String>("gsc_optioncode"));
            Assert.AreEqual(DateTime.Today.ToString("MM-dd-yyyy"), schedDetail.GetAttributeValue<DateTime>("gsc_lastcount"));

            #endregion
        }

        #endregion

    }
}
