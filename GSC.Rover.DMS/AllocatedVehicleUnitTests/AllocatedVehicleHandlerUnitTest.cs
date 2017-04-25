using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Moq;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.AllocatedVehicle;

namespace AllocatedVehicleUnitTests
{
    [TestClass]
    public class AllocatedVehicleHandlerUniTest
    {
        #region RemoveAllocation

        [TestMethod]

        public void RemoveAllocation()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Order Entity
            var OrderEntity = new EntityCollection()
            {
                EntityName = "order",
                Entities =
                {
                    new Entity 
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "order",
                        Attributes =
                        {
                            {"gsc_inventoryidtoallocate", "1a0effa2-2d1b-e611-80d8-00155d010e2c"},
                            {"gsc_status", new OptionSetValue(100000004)},
                            {"gsc_vehicleallocateddate", DateTime.Today.ToString("MM-dd-yyyy")}
                        }
                    }
                }
            };
            #endregion

            #region Product Quantity Entity Collection
            var ProductQuantity = new EntityCollection()
            {
                EntityName = "gsc_iv_productquantity",
                Entities =
                {
                    new Entity 
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_productquantity",
                        Attributes = 
                        {
                             {"gsc_available",2},
                             {"gsc_allocated",1}
                        }
                    }
                }
            };
            #endregion

            #region Inventory Entity Collection
            var Inventory = new EntityCollection()
            {
                EntityName = "gsc_iv_inventory",
                Entities =
                {
                    new Entity 
                    {
                        Id = new Guid("1a0effa2-2d1b-e611-80d8-00155d010e2c"),
                        LogicalName = "gsc_iv_inventory",
                        Attributes = 
                        {
                                    {"gsc_color","Black"},
                                    {"gsc_csno","1"},
                                    {"gsc_engineno","2"},
                                    {"gsc_modelcode","3"},
                                    {"gsc_optioncode","4"},
                                    {"gsc_productionno","5"},
                                    {"gsc_vin","6"},
                                    {"gsc_status",new OptionSetValue(100000001)},
                                    {"gsc_productquantityid", new EntityReference(ProductQuantity.EntityName, ProductQuantity.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            #region Allocated Vehicle Entity
            var AllocatedVehicle = new EntityCollection()
            {
                EntityName = "gsc_iv_allocatedvehicle",
                Entities =
                {
                    new Entity 
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_allocatedvehicle",
                        Attributes = 
                        {
                            {"gsc_orderid", new EntityReference(OrderEntity.EntityName, OrderEntity.Entities[0].Id)},
                            {"gsc_inventoryid", new EntityReference(Inventory.EntityName, Inventory.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
           It.Is<QueryExpression>(expression => expression.EntityName == OrderEntity.EntityName)
           ))).Returns(OrderEntity);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
          It.Is<QueryExpression>(expression => expression.EntityName == Inventory.EntityName)
          ))).Returns(Inventory);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
          It.Is<QueryExpression>(expression => expression.EntityName == ProductQuantity.EntityName)
          ))).Returns(ProductQuantity);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == OrderEntity.EntityName)))).Callback<Entity>(s => OrderEntity.Entities[0] = s);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Inventory.EntityName)))).Callback<Entity>(s => Inventory.Entities[0] = s);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == ProductQuantity.EntityName)))).Callback<Entity>(s => ProductQuantity.Entities[0] = s);

            #endregion

            #region 2. Call / Action
            var AllocateVehicleHandler = new AllocatedVehicleHandler(orgService, orgTracing);
            AllocateVehicleHandler.RemoveAllocation(AllocatedVehicle.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(100000002, OrderEntity.Entities[0].GetAttributeValue<OptionSetValue>("gsc_status").Value);
            Assert.AreEqual(null, OrderEntity.Entities[0].GetAttributeValue<String>("gsc_inventoryidtoallocate"));
            Assert.AreEqual(null, OrderEntity.Entities[0].GetAttributeValue<DateTime>("gsc_vehicleallocateddate"));
            Assert.AreEqual(100000000, Inventory.Entities[0].GetAttributeValue<OptionSetValue>("gsc_status").Value);
            Assert.AreEqual(3, ProductQuantity.Entities[0].GetAttributeValue<Int32>("gsc_available"));
            Assert.AreEqual(0, ProductQuantity.Entities[0].GetAttributeValue<Int32>("gsc_allocated"));
            #endregion 
        }

        #endregion
    }
}
