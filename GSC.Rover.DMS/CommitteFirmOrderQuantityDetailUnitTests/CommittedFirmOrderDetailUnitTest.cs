using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.CommittedFirmOrderQuantityDetail;
using System.Collections.Generic;

namespace CommitteFirmOrderQuantityDetailUnitTests
{
    [TestClass]
    public class CommittedFirmOrderDetailUnitTest
    {
        //Created By: Leslie Baliguat, Created On: 6/24/2016
        #region ReplicateAllocatedQuantity
        [TestMethod]
        public void TestMethod1()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Order Planning Entity Collection
            var OrderPlanningCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_orderplanning",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_orderplanning",
                        Attributes = new AttributeCollection
                        {
                        }
                    }
                }
            };
            #endregion

            #region Order Planning Details Entity Collection
            var OrderPlanningDetailCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_orderplanningdetail",
                Entities =
                {
                    new Entity
                    {
                        Id =  Guid.NewGuid(),
                        LogicalName = "gsc_sls_orderplanningdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_sls_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[0].Id)},
                            {"gsc_cfoallocation", null}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Entity Collection
            var CFOQuantityCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantity",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantity",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_cfomonth", new OptionSetValue(100000008)},
                            {"gsc_year", new OptionSetValue(100000000)}
                        },
                        FormattedValues = 
                        {
                            {"gsc_cfomonth", "09"},
                            {"gsc_year", "2016"}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Details Entity Collection
            var CFOQuantityDetailsCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantitydetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantitydetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderquantityid", new EntityReference(CFOQuantityCollection.EntityName, CFOQuantityCollection.Entities[0].Id)},
                            {"gsc_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[0].Id)},
                            {"gsc_allocatedquantity", 10},
                            {"gsc_remainingallocatedquantity", 10}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == CFOQuantityCollection.EntityName)
                ))).Returns(CFOQuantityCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningCollection.EntityName)
              ))).Returns(OrderPlanningCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningDetailCollection.EntityName)
              ))).Returns(OrderPlanningDetailCollection);

            orgServiceMock.Setup(service => service.Retrieve(
           It.IsAny<string>(),
           It.IsAny<Guid>(),
           It.IsAny<ColumnSet>())).Returns(OrderPlanningDetailCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == OrderPlanningDetailCollection.Entities[0].LogicalName)))).Callback<Entity>(s => OrderPlanningDetailCollection.Entities[0] = s);


            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(CFOQuantityDetailsCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == CFOQuantityDetailsCollection.Entities[0].LogicalName)))).Callback<Entity>(s => CFOQuantityDetailsCollection.Entities[0] = s);

            #endregion

            #region 2. Call/Action

            var CFOQuantityHandler = new CommittedFirmOrderQuantityDetailHandler(orgService, orgTracing);
            CFOQuantityHandler.ReplicateAllocatedQuantity(CFOQuantityDetailsCollection.Entities[0]);
            
            #endregion

            #region 3. Verify
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<Int32>("gsc_allocatedquantity"),
                CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<Int32>("gsc_remainingallocatedquantity"));
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<Int32>("gsc_allocatedquantity"),
               OrderPlanningDetailCollection.Entities[0].GetAttributeValue<Int32>("gsc_cfoquantity"));
            #endregion
        }
        #endregion

        //Created By: Leslie Baliguat, Created O: 6/27/2016
        #region GeneratePO

        #region Scenario 1: Happy Flow, Generate PO, Change Remaining Allocated, Change CFO Status to Completed
        [TestMethod]
        public void GeneratePO_HappyFlow()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Entity Collection
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
                            {"gsc_modelyear", "2016"}
                        }
                    }
                }
            };
            #endregion

            #region Vendor Entity Collection
            var VendorCollection = new EntityCollection()
            {
                EntityName = "gsc_cmn_vendor",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_vendor",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vendornamepn", "MMPC"}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Entity Collection
            var CFOQuantityCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantity",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantity",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderquantitypn", "CFO06272016"},
                            {"gsc_cfodealerid", new EntityReference("dealer", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Citimotors"}},
                            {"gsc_cfobranchid", new EntityReference("branch", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Branch 1"}},
                            {"gsc_cfostatus", new OptionSetValue(100000000)}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Details Entity Collection
            var CFOQuantityDetailsCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantitydetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantitydetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_selected", true},
                            {"gsc_committedfirmorderquantityid", new EntityReference(CFOQuantityCollection.EntityName, CFOQuantityCollection.Entities[0].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid",  new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_modelcode", "123"},
                            {"gsc_optioncode", "456"},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_orderquantity", 6},
                            {"gsc_remainingallocatedquantity", 6}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantitydetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_selected", true},
                            {"gsc_committedfirmorderquantityid", new EntityReference(CFOQuantityCollection.EntityName, CFOQuantityCollection.Entities[0].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid",  new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_modelcode", "123"},
                            {"gsc_optioncode", "456"},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_orderquantity", 0},
                            {"gsc_remainingallocatedquantity", 0}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == CFOQuantityCollection.EntityName)
                ))).Returns(CFOQuantityCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
              ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == VendorCollection.EntityName)
              ))).Returns(VendorCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
            It.Is<QueryExpression>(expression => expression.EntityName == CFOQuantityCollection.EntityName)
            ))).Returns(CFOQuantityCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == CFOQuantityCollection.Entities[0].LogicalName)))).Callback<Entity>(s => CFOQuantityCollection.Entities[0] = s);

            orgServiceMock.Setup(service => service.Retrieve(
           It.IsAny<string>(),
           It.IsAny<Guid>(),
           It.IsAny<ColumnSet>())).Returns(CFOQuantityDetailsCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == CFOQuantityDetailsCollection.Entities[0].LogicalName)))).Callback<Entity>(s => CFOQuantityDetailsCollection.Entities[0] = s);


            #endregion

            #region 2. Call/Action

            var CFOQuantityHandler = new CommittedFirmOrderQuantityDetailHandler(orgService, orgTracing);
            List<Entity> cfoQuantityEntities = CFOQuantityHandler.GeneratePO(CFOQuantityDetailsCollection.Entities[0]);

            #endregion

            #region 3. Verify
            //Vehicle Purchase Order
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<String>("gsc_committedfirmorderquantitypn"),
                cfoQuantityEntities[0].GetAttributeValue<String>("gsc_cfonumber"));
            Assert.AreEqual(VendorCollection.Entities[0].Id,
               cfoQuantityEntities[0].GetAttributeValue<EntityReference>("gsc_vendorid").Id);
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cfodealerid").Id,
                cfoQuantityEntities[0].GetAttributeValue<EntityReference>("gsc_todealerid").Id);
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cfobranchid").Id,
                cfoQuantityEntities[0].GetAttributeValue<EntityReference>("gsc_tobranchid").Id);

            //Vehicle Purchase Order Item
            Assert.AreEqual(cfoQuantityEntities[0].Id,
                cfoQuantityEntities[1].GetAttributeValue<EntityReference>("gsc_purchaseorderid").Id);
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id,
               cfoQuantityEntities[1].GetAttributeValue<EntityReference>("gsc_basemodelid").Id);
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_productid").Id,
               cfoQuantityEntities[1].GetAttributeValue<EntityReference>("gsc_productid").Id);
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<String>("gsc_modelcode"),
               cfoQuantityEntities[1].GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<String>("gsc_optioncode"),
               cfoQuantityEntities[1].GetAttributeValue<String>("gsc_optioncode"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<String>("gsc_modelyear"),
               cfoQuantityEntities[1].GetAttributeValue<String>("gsc_modelyear"));
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id,
               cfoQuantityEntities[1].GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id);

            //CFO Quantity Detail
            Assert.AreEqual(false, CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<Boolean>("gsc_selected"));
            Assert.AreEqual(0, CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<Int32>("gsc_remainingallocatedquantity"));

            //CFO Status
            Assert.AreEqual(100000002, CFOQuantityCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_cfostatus").Value);

            #endregion
        }
        #endregion

        #region Scenario 2: Generate PO, Change Remaining Allocated, CFO Status not completed
        [TestMethod]
        public void GeneratePO_CFOStatusNotUpdated()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Entity Collection
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
                            {"gsc_modelyear", "2016"}
                        }
                    }
                }
            };
            #endregion

            #region Vendor Entity Collection
            var VendorCollection = new EntityCollection()
            {
                EntityName = "gsc_cmn_vendor",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_vendor",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vendornamepn", "MMPC"}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Entity Collection
            var CFOQuantityCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantity",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantity",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderquantitypn", "CFO06272016"},
                            {"gsc_cfodealerid", new EntityReference("dealer", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Citimotors"}},
                            {"gsc_cfobranchid", new EntityReference("branch", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Branch 1"}},
                            {"gsc_cfostatus", new OptionSetValue(100000000)}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Details Entity Collection
            var CFOQuantityDetailsCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantitydetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantitydetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_selected", true},
                            {"gsc_committedfirmorderquantityid", new EntityReference(CFOQuantityCollection.EntityName, CFOQuantityCollection.Entities[0].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid",  new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_modelcode", "123"},
                            {"gsc_optioncode", "456"},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_orderquantity", 6},
                            {"gsc_remainingallocatedquantity", 6}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantitydetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_selected", true},
                            {"gsc_committedfirmorderquantityid", new EntityReference(CFOQuantityCollection.EntityName, CFOQuantityCollection.Entities[0].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid",  new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_modelcode", "123"},
                            {"gsc_optioncode", "456"},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_orderquantity", 0},
                            {"gsc_remainingallocatedquantity", 3}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == CFOQuantityCollection.EntityName)
                ))).Returns(CFOQuantityCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
              ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == VendorCollection.EntityName)
              ))).Returns(VendorCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
            It.Is<QueryExpression>(expression => expression.EntityName == CFOQuantityDetailsCollection.EntityName)
            ))).Returns(CFOQuantityDetailsCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == CFOQuantityCollection.Entities[0].LogicalName)))).Callback<Entity>(s => CFOQuantityCollection.Entities[0] = s);

            orgServiceMock.Setup(service => service.Retrieve(
           It.IsAny<string>(),
           It.IsAny<Guid>(),
           It.IsAny<ColumnSet>())).Returns(CFOQuantityDetailsCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == CFOQuantityDetailsCollection.Entities[0].LogicalName)))).Callback<Entity>(s => CFOQuantityDetailsCollection.Entities[0] = s);


            #endregion

            #region 2. Call/Action

            var CFOQuantityHandler = new CommittedFirmOrderQuantityDetailHandler(orgService, orgTracing);
            List<Entity> cfoQuantityEntities = CFOQuantityHandler.GeneratePO(CFOQuantityDetailsCollection.Entities[0]);

            #endregion

            #region 3. Verify
            //Vehicle Purchase Order
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<String>("gsc_committedfirmorderquantitypn"),
                cfoQuantityEntities[0].GetAttributeValue<String>("gsc_cfonumber"));
            Assert.AreEqual(VendorCollection.Entities[0].Id,
               cfoQuantityEntities[0].GetAttributeValue<EntityReference>("gsc_vendorid").Id);
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cfodealerid").Id,
                cfoQuantityEntities[0].GetAttributeValue<EntityReference>("gsc_todealerid").Id);
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cfobranchid").Id,
                cfoQuantityEntities[0].GetAttributeValue<EntityReference>("gsc_tobranchid").Id);

            //Vehicle Purchase Order Item
            Assert.AreEqual(cfoQuantityEntities[0].Id,
                cfoQuantityEntities[1].GetAttributeValue<EntityReference>("gsc_purchaseorderid").Id);
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id,
               cfoQuantityEntities[1].GetAttributeValue<EntityReference>("gsc_basemodelid").Id);
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_productid").Id,
               cfoQuantityEntities[1].GetAttributeValue<EntityReference>("gsc_productid").Id);
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<String>("gsc_modelcode"),
               cfoQuantityEntities[1].GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<String>("gsc_optioncode"),
               cfoQuantityEntities[1].GetAttributeValue<String>("gsc_optioncode"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<String>("gsc_modelyear"),
               cfoQuantityEntities[1].GetAttributeValue<String>("gsc_modelyear"));
            Assert.AreEqual(CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id,
               cfoQuantityEntities[1].GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id);

            //CFO Quantity Detail
            Assert.AreEqual(false, CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<Boolean>("gsc_selected"));
            Assert.AreEqual(0, CFOQuantityDetailsCollection.Entities[0].GetAttributeValue<Int32>("gsc_remainingallocatedquantity"));

            //CFO Status
            Assert.AreEqual(100000000, CFOQuantityCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_cfostatus").Value);

            #endregion
        }
        #endregion

        #region Scenario 3: Order Quantity is Greater than Remaining
        [TestMethod]
        public void GeneratePO_InvalidOrerQuantity()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Entity Collection
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
                            {"gsc_modelyear", "2016"}
                        }
                    }
                }
            };
            #endregion

            #region Vendor Entity Collection
            var VendorCollection = new EntityCollection()
            {
                EntityName = "gsc_cmn_vendor",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_vendor",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vendornamepn", "MMPC"}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Entity Collection
            var CFOQuantityCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantity",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantity",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderquantitypn", "CFO06272016"},
                            {"gsc_cfodealerid", new EntityReference("dealer", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Citimotors"}},
                            {"gsc_cfobranchid", new EntityReference("branch", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Branch 1"}}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Details Entity Collection
            var CFOQuantityDetailsCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantitydetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantitydetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_selected", true},
                            {"gsc_committedfirmorderquantityid", new EntityReference(CFOQuantityCollection.EntityName, CFOQuantityCollection.Entities[0].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid",  new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_modelcode", "123"},
                            {"gsc_optioncode", "456"},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_orderquantity", 7},
                            {"gsc_remainingallocatedquantity", 6}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == CFOQuantityCollection.EntityName)
                ))).Returns(CFOQuantityCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
              ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == VendorCollection.EntityName)
              ))).Returns(VendorCollection);
            #endregion

            #region 2. Call/Action

            var CFOQuantityHandler = new CommittedFirmOrderQuantityDetailHandler(orgService, orgTracing);
            List<Entity> cfoQuantityEntities = CFOQuantityHandler.GeneratePO(CFOQuantityDetailsCollection.Entities[0]);

            #endregion

            #region 3. Verify
            //Vehicle Purchase Order
            Assert.AreEqual(null, cfoQuantityEntities);
            #endregion
        }
        #endregion

        #region Scenario 4: MMPC not Exisiting, will throw an error
        [TestMethod]
        public void GeneratePO_Error()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Entity Collection
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
                            {"gsc_modelyear", "2016"}
                        }
                    }
                }
            };
            #endregion

            #region Vendor Entity Collection
            var VendorCollection = new EntityCollection()
            {
                EntityName = "gsc_cmn_vendor",
                Entities =
                {
                }
            };
            #endregion

            #region CFO Quantity Entity Collection
            var CFOQuantityCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantity",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantity",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderquantitypn", "CFO06272016"},
                            {"gsc_cfodealerid", new EntityReference("dealer", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Citimotors"}},
                            {"gsc_cfobranchid", new EntityReference("branch", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Branch 1"}}
                        }
                    }
                }
            };
            #endregion

            #region CFO Quantity Details Entity Collection
            var CFOQuantityDetailsCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderquantitydetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_committedfirmorderquantitydetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_selected", true},
                            {"gsc_committedfirmorderquantityid", new EntityReference(CFOQuantityCollection.EntityName, CFOQuantityCollection.Entities[0].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid",  new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_modelcode", "123"},
                            {"gsc_optioncode", "456"},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_orderquantity", 2},
                            {"gsc_remainingallocatedquantity", 6}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == CFOQuantityCollection.EntityName)
                ))).Returns(CFOQuantityCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
              ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == VendorCollection.EntityName)
              ))).Returns(VendorCollection);
            #endregion

            #region 2. Call/Action

            var CFOQuantityHandler = new CommittedFirmOrderQuantityDetailHandler(orgService, orgTracing);
            List<Entity> cfoQuantityEntities = CFOQuantityHandler.GeneratePO(CFOQuantityDetailsCollection.Entities[0]);

            #endregion

            #region 3. Verify
            //Vehicle Purchase Order
            Assert.AreEqual(null, cfoQuantityEntities);
            #endregion
        }
        #endregion

        #endregion
      
    }
}
