using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.CommittedFirmOrder;
using Moq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;


namespace CommittedFirmOrderUnitTests
{
    [TestClass]
    public class CommittedFirmOrderUnitTests
    {
        //Created By: Leslie Baliguat, Created On: 7/8/2016
        #region SuggestCFOQuantity

        #region Test Scenario: With Filter
        [TestMethod]
        public void WithFilter()
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
                            {"gsc_sellprice", new Money(((Decimal)2500000))},
                            {"gsc_modelcode", "123"},
                            {"gsc_optioncode", "456"}
                        }
                    }
                }
            };
            #endregion

            #region Order CFO Quanity Entity Collection
            var CFOQuantityCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorder",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_committedfirmorder",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid", new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_dealerfilterid", new EntityReference("dealer", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Citimotors"}},
                            {"gsc_branchfilterid", new EntityReference("branch", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Branch 1"}},
                            {"gsc_siteid", new EntityReference("site", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Site 1"}},
                        }
                    }
                }
            };
            #endregion

            #region Order Planning Entity Collection
            var OrderPlanningCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_orderplanning",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_orderplanning",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid",  new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_dealerid", new EntityReference("dealer", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Citimotors"}},
                            {"gsc_branchid", new EntityReference("branch", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Branch 1"}},
                            {"gsc_siteid", new EntityReference("site", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Site 1"}},
                            {"gsc_orderpolicy", new OptionSetValue(100000001)}
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
                        Id = new Guid(),
                        LogicalName = "gsc_sls_orderplanningdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_sls_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[0].Id)},
                            {"gsc_beginninginventory", 5.00},
                            {"gsc_retailaveragesales", 10.00},
                            {"gsc_stockmonth", 0.5}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningCollection.EntityName)
                ))).Returns(OrderPlanningCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
                ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningDetailCollection.EntityName)
                ))).Returns(OrderPlanningDetailCollection);

            #endregion

            #region 2. Call/Action
            var CFOHandler = new CommittedFirmOrderHandler(orgService, orgTracing);
            Entity cfoDetail = CFOHandler.SuggestCFOQuantity(CFOQuantityCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_basemodel"));
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_productid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_modeldescription"));
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_color"));
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_dealerfilterid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_dealer"));
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_branchfilterid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_branch"));
            Assert.AreEqual(CFOQuantityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_siteid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_site"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<String>("gsc_optioncode"),
                cfoDetail.GetAttributeValue<String>("gsc_optioncode"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<String>("gsc_modelcode"),
                cfoDetail.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<Money>("gsc_sellprice"),
                cfoDetail.GetAttributeValue<Money>("gsc_unitprice"));
            Assert.AreEqual(0,
                cfoDetail.GetAttributeValue<Double>("gsc_cfoquantity"));
            Assert.AreEqual(10,
                cfoDetail.GetAttributeValue<Double>("gsc_suggestedcfo"));
            #endregion

        }
        #endregion

        #region Test Scenario: No Filter
        [TestMethod]
        public void NoFilter()
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
                            {"gsc_sellprice", new Money(((Decimal)2500000))},
                            {"gsc_modelcode", "123"},
                            {"gsc_optioncode", "456"}
                        }
                    }
                }
            };
            #endregion

            #region Order CFO Quanity Entity Collection
            var CFOQuantityCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorder",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_committedfirmorder",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclebasemodelid", null},
                            {"gsc_productid", null},
                            {"gsc_vehiclecolorid", null},
                            {"gsc_dealerfilterid",null},
                            {"gsc_branchfilterid",null},
                            {"gsc_siteid", null},
                        }
                    }
                }
            };
            #endregion

            #region Order Planning Entity Collection
            var OrderPlanningCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_orderplanning",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_orderplanning",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid",  new EntityReference(ProductCollection.EntityName, ProductCollection.Entities[0].Id)
                            { Name = "Mirage GLX"}},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_dealerid", new EntityReference("dealer", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Citimotors"}},
                            {"gsc_branchid", new EntityReference("branch", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Branch 1"}},
                            {"gsc_siteid", new EntityReference("site", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Site 1"}},
                            {"gsc_orderpolicy", new OptionSetValue(100000001)}
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
                        Id = new Guid(),
                        LogicalName = "gsc_sls_orderplanningdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_sls_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[0].Id)},
                            {"gsc_beginninginventory", 5.00},
                            {"gsc_retailaveragesales", 10.00},
                            {"gsc_stockmonth", 0.5}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningCollection.EntityName)
                ))).Returns(OrderPlanningCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
                ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningDetailCollection.EntityName)
                ))).Returns(OrderPlanningDetailCollection);

            #endregion

            #region 2. Call/Action
            var CFOHandler = new CommittedFirmOrderHandler(orgService, orgTracing);
            Entity cfoDetail = CFOHandler.SuggestCFOQuantity(CFOQuantityCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(OrderPlanningCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_basemodel"));
            Assert.AreEqual(OrderPlanningCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_productid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_modeldescription"));
            Assert.AreEqual(OrderPlanningCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_color"));
            Assert.AreEqual(OrderPlanningCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_dealerid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_dealer"));
            Assert.AreEqual(OrderPlanningCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_branchid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_branch"));
            Assert.AreEqual(OrderPlanningCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_siteid").Name,
                cfoDetail.GetAttributeValue<String>("gsc_site"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<String>("gsc_optioncode"),
                cfoDetail.GetAttributeValue<String>("gsc_optioncode"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<String>("gsc_modelcode"),
                cfoDetail.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<Money>("gsc_sellprice"),
                cfoDetail.GetAttributeValue<Money>("gsc_unitprice"));
            Assert.AreEqual(0,
                cfoDetail.GetAttributeValue<Double>("gsc_cfoquantity"));
            Assert.AreEqual(10,
                cfoDetail.GetAttributeValue<Double>("gsc_suggestedcfo"));
            #endregion

        }
        #endregion

        #endregion

        //Created By:Leslie Baliguat, Created On: 7/21/2016
        #region GenerateCFOQuantity

        #region Scenario 1: Happy Flow
        [TestMethod]

        public void GenerateCFOQuantity_HappyFlow()
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
                        Attributes = 
                        {
                            {"gsc_ordercycle", new OptionSetValue(100000002)}
                        }, 
                        FormattedValues = 
                        {
                            {"gsc_ordercycle", "3"}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_orderplanning",
                        Attributes =
                        {
                            {"gsc_ordercycle", new OptionSetValue(100000002)}
                        }, 
                        FormattedValues = 
                        {
                            {"gsc_ordercycle", "3"}
                        }
                    }
                }
            };
            #endregion

            #region Suggested CFO Entity Collection
            var SuggestedCFOCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorder",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_committedfirmorder",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_generatecfoquantity", true},
                        }
                    }
                }
            };
            #endregion

            #region Suggested CFO Details Entity Collection
            var SuggestedCFODetailsCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderdetail",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_committedfirmorderdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderid", new EntityReference(SuggestedCFOCollection.EntityName, SuggestedCFOCollection.Entities[0].Id)},
                            {"gsc_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[0].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid", new EntityReference("product", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Montero"}},
                            {"gsc_modelcode","Model Code"},
                            {"gsc_optioncode","Option Code"},
                            {"gsc_unitcost",new Money(180000)},
                            {"gsc_cfoquantity", 10},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_dealer", "Citimotors"},
                            {"gsc_branch", "Branch 1"},
                            {"gsc_siteid", new EntityReference("site", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Site 1"}},
                            {"gsc_remakrs", "Remarks"},
                            {"gsc_statusreason", 100000000},
                        }
                    },
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_committedfirmorderdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderid", new EntityReference(SuggestedCFOCollection.EntityName, SuggestedCFOCollection.Entities[0].Id)},
                            {"gsc_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[1].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid", new EntityReference("product", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Montero"}},
                            {"gsc_modelcode","Model Code"},
                            {"gsc_optioncode","Option Code"},
                            {"gsc_unitcost",new Money(250000)},
                            {"gsc_cfoquantity", 10},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_dealer", "Citimotors"},
                            {"gsc_branch", "Branch 1"},
                            {"gsc_siteid", new EntityReference("site", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Site 1"}},
                            {"gsc_remakrs", "Remarks"},
                            {"gsc_statusreason", 100000000},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SuggestedCFODetailsCollection.EntityName)
                ))).Returns(SuggestedCFODetailsCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningCollection.EntityName)
                ))).Returns(OrderPlanningCollection);

            #endregion

            #region 2. Call/Action
            var CFOHandler = new CommittedFirmOrderHandler(orgService, orgTracing);
            Entity cfoQuantityDetails = CFOHandler.GenerateCFOQuantity(SuggestedCFOCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<EntityReference>("gsc_orderplanningid").Id,
                cfoQuantityDetails.GetAttributeValue<EntityReference>("gsc_orderplanningid").Id);
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id,
                cfoQuantityDetails.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id);
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<EntityReference>("gsc_productid").Id,
                cfoQuantityDetails.GetAttributeValue<EntityReference>("gsc_productid").Id);
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id,
                cfoQuantityDetails.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id);
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<String>("gsc_modelcode"),
                cfoQuantityDetails.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<String>("gsc_optioncode"),
                cfoQuantityDetails.GetAttributeValue<String>("gsc_optioncode"));
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<Money>("gsc_unitcost").Value,
                cfoQuantityDetails.GetAttributeValue<Money>("gsc_unitcost").Value);
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<Int32>("gsc_cfoquantity"),
                cfoQuantityDetails.GetAttributeValue<Int32>("gsc_cfoquantity"));
            Assert.AreEqual(0, cfoQuantityDetails.GetAttributeValue<Int32>("gsc_orderquantity"));
            Assert.AreEqual(0, cfoQuantityDetails.GetAttributeValue<Int32>("gsc_remainingallocatedquantity"));
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<Money>("gsc_unitcost").Value, cfoQuantityDetails.GetAttributeValue<Money>("gsc_unitcost").Value);
            Assert.AreEqual(0, cfoQuantityDetails.GetAttributeValue<Money>("gsc_totalcost").Value);
            Assert.AreEqual(SuggestedCFODetailsCollection.Entities[1].GetAttributeValue<EntityReference>("gsc_siteid").Id,
                cfoQuantityDetails.GetAttributeValue<EntityReference>("gsc_siteid").Id);
            #endregion
        }
        #endregion

        #region Scenario 2: Suggested CFO Updated but not for Generating CFO Quantity; gsc_generatecfoquantity is false
        [TestMethod]

        public void GenerateCFOQuantity_Null()
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
                        Attributes = 
                        {
                            {"gsc_ordercycle", new OptionSetValue(100000002)}
                        }, 
                        FormattedValues = 
                        {
                            {"gsc_ordercycle", "3"}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_orderplanning",
                        Attributes =
                        {
                            {"gsc_ordercycle", new OptionSetValue(100000002)}
                        }, 
                        FormattedValues = 
                        {
                            {"gsc_ordercycle", "3"}
                        }
                    }
                }
            };
            #endregion

            #region Suggested CFO Entity Collection
            var SuggestedCFOCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorder",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_committedfirmorder",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_generatecfoquantity", false},
                        }
                    }
                }
            };
            #endregion

            #region Suggested CFO Details Entity Collection
            var SuggestedCFODetailsCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_committedfirmorderdetail",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_committedfirmorderdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderid", new EntityReference(SuggestedCFOCollection.EntityName, SuggestedCFOCollection.Entities[0].Id)},
                            {"gsc_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[0].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid", new EntityReference("product", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Montero"}},
                            {"gsc_modelcode","Model Code"},
                            {"gsc_optioncode","Option Code"},
                            {"gsc_unitcost",new Money(180000)},
                            {"gsc_cfoquantity", 10},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_dealer", "Citimotors"},
                            {"gsc_branch", "Branch 1"},
                            {"gsc_siteid", new EntityReference("site", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Site 1"}},
                            {"gsc_remakrs", "Remarks"},
                            {"gsc_statusreason", 100000000},
                        }
                    },
                    new Entity
                    {
                        Id = new Guid(),
                        LogicalName = "gsc_sls_committedfirmorderdetail",
                        Attributes = new AttributeCollection
                        {
                            {"gsc_committedfirmorderid", new EntityReference(SuggestedCFOCollection.EntityName, SuggestedCFOCollection.Entities[0].Id)},
                            {"gsc_orderplanningid", new EntityReference(OrderPlanningCollection.EntityName, OrderPlanningCollection.Entities[1].Id)},
                            {"gsc_vehiclebasemodelid", new EntityReference("basemodel", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Mirage"}},
                            {"gsc_productid", new EntityReference("product", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Montero"}},
                            {"gsc_modelcode","Model Code"},
                            {"gsc_optioncode","Option Code"},
                            {"gsc_unitcost",new Money(250000)},
                            {"gsc_cfoquantity", 10},
                            {"gsc_vehiclecolorid", new EntityReference("color", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Black"}},
                            {"gsc_dealer", "Citimotors"},
                            {"gsc_branch", "Branch 1"},
                            {"gsc_siteid", new EntityReference("site", new Guid("5ebda07e-0a26-e611-80d8-00155d010e2c"))
                            { Name = "Site 1"}},
                            {"gsc_remakrs", "Remarks"},
                            {"gsc_statusreason", 100000000},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SuggestedCFODetailsCollection.EntityName)
                ))).Returns(SuggestedCFODetailsCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == OrderPlanningCollection.EntityName)
                ))).Returns(OrderPlanningCollection);

            #endregion

            #region 2. Call/Action
            var CFOHandler = new CommittedFirmOrderHandler(orgService, orgTracing);
            Entity cfoQuantityDetails = CFOHandler.GenerateCFOQuantity(SuggestedCFOCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(null, cfoQuantityDetails);
            #endregion
        }
        #endregion

        #endregion
    }
}
