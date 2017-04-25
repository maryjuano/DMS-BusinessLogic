using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.SalesOrder;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace SalesOrderUnitTests
{
    [TestClass]
    public class SalesOrderHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 3/3/2016
        #region Replicate Quote to Sales Order

        #region Test Scenario : Create Order from Quote form
        [TestMethod]
        public void ReplicateQuoteInfoUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"customerid", new EntityReference("contact", Guid.NewGuid())},
                            {"quoteid", new EntityReference("quote", Guid.NewGuid())}
                        }
                    }
                }
            };
            #endregion

            #region Quote EntityCollection
            var QuoteCollection = new EntityCollection
            {
                EntityName = "quote",
                Entities = 
                {
                    new Entity
                    {
                        Id = SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("quoteid").Id,
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_dealerid", new EntityReference("account", Guid.NewGuid())
                            { Name = "Union Motors"}},
                            {"gsc_branchsiteid", new EntityReference("account", Guid.NewGuid())
                            { Name = "Otis"}},
                            {"gsc_salesexecutiveid", new EntityReference("contact", Guid.NewGuid())
                            { Name = "Mr. Watanabe"}},
                            {"gsc_paymentmode", new OptionSetValue(1)
                            { Value = 1}},
                            {"gsc_productid", new EntityReference("product", Guid.NewGuid())
                            { Name = "Lancer Evolution X"}},
                            {"gsc_color1id", new EntityReference("gsc_iv_color", Guid.NewGuid())
                            { Name = "Periwinkle"}},
                            {"gsc_color2id", new EntityReference("gsc_iv_color", Guid.NewGuid())
                            { Name = "Hot Pink"}},
                            {"gsc_color3id", new EntityReference("gsc_iv_color", Guid.NewGuid())
                            { Name = "Light Blue"}},
                            {"gsc_remarks", "Test One Two"},
                            {"gsc_vehicleunitprice", new Money(((Decimal)120000.46))},
                            {"gsc_vehicledetails", "The Lancer Evolution X is the last of the Evolution series"},
                            {"customerid", new EntityReference("contact", Guid.NewGuid())
                            { Name = "Wiz Khalifa"}},
                            {"gsc_address", "Kolkata, India"}
                        }
                    }
                }
            };
            #endregion

            #region Contact EntityCollection
            var ContactCollection = new EntityCollection
            {
                EntityName = "contact",
                Entities = 
                {
                    new Entity
                    {
                        Id = SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("customerid").Id,
                        LogicalName = "contact",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_tin", "237 770 068 000"}
                        }
                    }
                }
            };
            #endregion            

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ContactCollection.EntityName)
                ))).Returns(ContactCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrder = SalesOrderHandler.ReplicateQuoteInfo(SalesOrderCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_dealerid").Id, salesOrder.GetAttributeValue<EntityReference>("gsc_dealerid").Id);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_branchsiteid").Id, salesOrder.GetAttributeValue<EntityReference>("gsc_branchsiteid").Id);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id, salesOrder.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value, salesOrder.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_productid").Id, salesOrder.GetAttributeValue<EntityReference>("gsc_productid").Id);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_color1id").Id, salesOrder.GetAttributeValue<EntityReference>("gsc_color1id").Id);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_color2id").Id, salesOrder.GetAttributeValue<EntityReference>("gsc_color2id").Id);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_color3id").Id, salesOrder.GetAttributeValue<EntityReference>("gsc_color3id").Id);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_remarks"), salesOrder.GetAttributeValue<String>("gsc_remarks"));
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_vehicleunitprice").Value, salesOrder.GetAttributeValue<Money>("gsc_vehicleunitprice").Value);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_vehicledetails"), salesOrder.GetAttributeValue<String>("gsc_vehicledetails"));
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("customerid").Id, salesOrder.GetAttributeValue<EntityReference>("customerid").Id);
            Assert.AreEqual(QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_address"), salesOrder.GetAttributeValue<String>("gsc_address"));
            #endregion
        }
        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 3/8/2016
        #region Replicate vehicle details from Product entity
        
        #region Test Scenario : Product ID is provided
        [TestMethod]
        public void ProductIdIsNotNull()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

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
                            {"gsc_enginetype", "V8"},
                            {"gsc_transmission", new OptionSetValue(0)
                            { Value = 0 }},
                            {"gsc_grossvehicleweight", "1,420–1,600 kg"},                            
                            {"gsc_pistondisplacement", "2.0L / 1998 cc"},
                            {"gsc_fueltype", new OptionSetValue(1)
                            { Value = 1}},
                            {"gsc_status", new OptionSetValue(0)
                            { Value = 0}},
                            {"gsc_sellprice", new Money(((Decimal)2500000))},
                            {"gsc_warrantyexpirydays", "365"},
                            {"gsc_warrantymileage", "2000"},
                            {"gsc_othervehicledetails", "The Lancer Evolution X is the last of the Evolution series"},
                        },
                        FormattedValues = 
                        {
                            {"gsc_transmission","M/T"},
                            {"gsc_fueltype", "Gasoline"},
                            {"gsc_status", "New"}
                        }
                    }
                }
            };
            #endregion
            
            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_productid", new EntityReference("product", ProductCollection.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
                ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrder = SalesOrderHandler.ReplicateVehicleDetails(SalesOrderCollection.Entities[0], "Update");
            #endregion

            #region 3. Verify

            Entity product = ProductCollection.Entities[0];

            var vehicleDetails = product["gsc_enginetype"].ToString() + ", " + product.FormattedValues["gsc_transmission"].ToString() + ", " + product["gsc_grossvehicleweight"].ToString()
                + ", " + product["gsc_pistondisplacement"].ToString() + ", " + product.FormattedValues["gsc_fueltype"].ToString() + ", " + product.FormattedValues["gsc_status"].ToString()
                + ", " + product["gsc_warrantyexpirydays"].ToString() + ", " + product["gsc_warrantymileage"].ToString() + ", " + product["gsc_othervehicledetails"].ToString() + ", ";
            vehicleDetails = vehicleDetails.Remove(vehicleDetails.Length - 2, 2);

            Assert.AreEqual(vehicleDetails, salesOrder.GetAttributeValue<String>("gsc_vehicledetails"));
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<Money>("gsc_sellprice").Value, salesOrder.GetAttributeValue<Money>("gsc_vehicleunitprice").Value);
            Assert.AreEqual(ProductCollection.Entities[0].GetAttributeValue<Money>("gsc_sellprice").Value, salesOrder.GetAttributeValue<Money>("gsc_unitprice").Value);
            #endregion
        }
        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 3/9/2016
        #region Create free order products

        #region Test Scenario : Product ID is provided
        [TestMethod]
        public void CreateFreeOrderProducts()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Relationship EntityCollection
            var ProductRelationshipCollection = new EntityCollection
            {
                EntityName = "productsubstitute",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "productsubstitute",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"productid", new EntityReference("product", Guid.NewGuid())
                            { Name = "Montero"}},
                            {"substitutedproductid", new EntityReference("product", Guid.NewGuid())
                            { Name = "Wheels"}}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_productid", new EntityReference("product", ProductRelationshipCollection.Entities[0].GetAttributeValue<EntityReference>("productid").Id)}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Detail EntityCollection
            var SalesOrderDetailCollection = new EntityCollection
            {
                EntityName = "salesorderdetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorderdetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesorderid", ""}
                        }
                    }
                }
            };
            #endregion

            #region Unit EntitiyCollection
            var UnitCollection = new EntityCollection
            {
                EntityName = "uom",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "uom",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"name", "Primary Unit"}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProductRelationshipCollection.EntityName)
                ))).Returns(ProductRelationshipCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderDetailCollection.EntityName)
                ))).Returns(SalesOrderDetailCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == UnitCollection.EntityName)
                ))).Returns(UnitCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrderProduct = SalesOrderHandler.GenerateAccessoriesforVehicleModel(SalesOrderCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(ProductRelationshipCollection.Entities[0].GetAttributeValue<EntityReference>("substitutedproductid").Id, salesOrderProduct.GetAttributeValue<EntityReference>("productid").Id);
            #endregion
        }
        #endregion

        #region Test Scenario : Product ID is provided but with no free items
        [TestMethod]
        public void DoNotCreateFreeOrderProducts()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Relationship EntityCollection
            var ProductRelationshipCollection = new EntityCollection
            {
                EntityName = "productsubstitute",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "productsubstitute",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"productid", new EntityReference("product", Guid.NewGuid())
                            { Name = "Montero"}},
                            {"substitutedproductid", new EntityReference("product", Guid.NewGuid())
                            { Name = "Wheels"}}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_productid", new EntityReference("product", Guid.NewGuid())}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Detail EntityCollection
            var SalesOrderDetailCollection = new EntityCollection
            {
                EntityName = "salesorderdetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorderdetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesorderid", ""}
                        }
                    }
                }
            };
            #endregion

            #region Unit EntitiyCollection
            var UnitCollection = new EntityCollection
            {
                EntityName = "uom",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "uom",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"name", "Primary Unit"}
                        }
                    }
                }
            };
            #endregion

            //orgServiceMock.Setup((service => service.RetrieveMultiple(
            //    It.Is<QueryExpression>(expression => expression.EntityName == ProductRelationshipCollection.EntityName)
            //    ))).Returns(ProductRelationshipCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderDetailCollection.EntityName)
                ))).Returns(SalesOrderDetailCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == UnitCollection.EntityName)
                ))).Returns(UnitCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrderProduct = SalesOrderHandler.GenerateAccessoriesforVehicleModel(SalesOrderCollection.Entities[0]);
            #endregion

            #region 3. Verify
            //BL should return a Sales Order record instead of a Sales Order Detail record if no new Sales Order Detail record is created.
            Assert.AreEqual(salesOrderProduct.LogicalName, "salesorder");
            #endregion
        }
        #endregion

        #endregion
    
        //Created By : Jerome Anthony Gerero, Created On : 3/15/2016
        #region Set Chattel Fee value

        #region Test Scenario : Bank ID is provided and Unit Price value is between 50,000.00 and 100,000.00
        [TestMethod]
        public void BankIdIsNotNull()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", Guid.NewGuid())
                            { Name = "Swiss Bank"}},
                            {"gsc_unitprice", new Money((Decimal)99999.99)},
                            {"gsc_freechattelfee", false},
                            {"gsc_chattelfee", new Money(Decimal.Zero)}
                        }
                    }
                }
            };
            #endregion

            #region Chattel Fee EntityCollection
            var ChattelFeeCollection = new EntityCollection
            {
                EntityName = "gsc_sls_chattelfee",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_chattelfee",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
                            {"gsc_loanamount", new Money((Decimal)50000.00)},
                            {"gsc_chattelfeeamount", new Money((Decimal)21263.00)}
                        }
                    },
                    
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_chattelfee",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
                            {"gsc_loanamount", new Money((Decimal)100000.00)},
                            {"gsc_chattelfeeamount", new Money((Decimal)22275.00)}
                        }
                    },

                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_chattelfee",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
                            {"gsc_loanamount", new Money((Decimal)150000.00)},
                            {"gsc_chattelfeeamount", new Money((Decimal)23288.00)}
                        }
                    }
                }
            };
            #endregion            
            
            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ChattelFeeCollection.EntityName)
                ))).Returns(ChattelFeeCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrderProduct = SalesOrderHandler.SetChattelFeeAmount(SalesOrderCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_chattelfee").Value, ChattelFeeCollection.Entities[0].GetAttributeValue<Money>("gsc_chattelfeeamount").Value);
            #endregion
        }
        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 3/17/2016
        #region Set less discount fields value

        #region Test Scenario : Apply amount fields are not null, set less discount fields value
        [TestMethod]
        public void SetLessDiscountFieldValues()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;


            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_downpaymentdiscount", new Money(Decimal.Zero)},
                            {"gsc_discountamountfinanced", new Money(Decimal.Zero)},
                            {"gsc_discount", new Money(Decimal.Zero)},
                            {"gsc_applytodpamount", new Money(20000)},
                            {"gsc_applytoafamount", new Money(25000)},
                            {"gsc_applytoupamount", new Money(35000)},
                            {"statecode" , new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            //Entity salesOrder = SalesOrderHandler.SetLessDiscountAmount(SalesOrderCollection.Entities[0], "Update");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_downpaymentdiscount").Value, SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_applytodpamount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_discountamountfinanced").Value, SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_applytoafamount").Value);
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_discount").Value, SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_applytoupamount").Value);
            #endregion
        }
        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 3/28/2016
        #region SetDates

        #region Test Scenario:
        [TestMethod]

        public void SetDates()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote Entity
            var QuoteEntityCollection = new EntityCollection
            {
                EntityName = "quote",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"createdon", new DateTime(2016, 2, 18)},
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Entity
            var SalesOrderEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "salesorder",
                Attributes =
                {
                    {"createdon", DateTime.Now},
                    {"id", new EntityReference(QuoteEntityCollection.EntityName, QuoteEntityCollection.Entities[0].Id)},
                    {"gsc_quotedate", new DateTime()},
                    {"gsc_orderdate", new DateTime()}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteEntityCollection.EntityName)
                ))).Returns(QuoteEntityCollection);

            #endregion 
            
            #region 2. Call/Action
            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrder = SalesOrderHandler.SetDates(SalesOrderEntity);
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderEntity.GetAttributeValue<DateTime>("createdon"), SalesOrderEntity.GetAttributeValue<DateTime>("gsc_orderdate"));
            Assert.AreEqual(QuoteEntityCollection.Entities[0].GetAttributeValue<DateTime>("createdon"), SalesOrderEntity.GetAttributeValue<DateTime>("gsc_quotedate"));
            #endregion
        }
        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 3/30/2016
        #region ReplicateInsuranceDetails

        #region Test Scenario:
        [TestMethod]

        public void ReplicateInsuranceDetails()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Vehicle Type Entity
            var VehicleTypeEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_iv_vehicletype",
                Attributes =
                {
                }
            };
            #endregion

            #region  Insurance Entity Collection
            var InsuranceEntityCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_insurance",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_insurance",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehicletypeid", new EntityReference(VehicleTypeEntity.LogicalName, VehicleTypeEntity.Id)},
                            {"gsc_vehicleuse", new OptionSetValue(1)},
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Entity
            var SalesOrderEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "salesorder",
                Attributes =
                {
                     {"gsc_insuranceid", new EntityReference(InsuranceEntityCollection.EntityName, InsuranceEntityCollection.Entities[0].Id)},
                     {"gsc_vehicletypeid", new EntityReference()},
                     {"gsc_vehicleuse", new OptionSetValue()},
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == InsuranceEntityCollection.EntityName)
                ))).Returns(InsuranceEntityCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == SalesOrderEntity.LogicalName)))).Callback<Entity>(s => SalesOrderEntity = s);

            #endregion

            #region 2. Call/Action
            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrder = SalesOrderHandler.ReplicateInsuranceDetails(SalesOrderEntity, "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(InsuranceEntityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehicletypeid").Id, salesOrder.GetAttributeValue<EntityReference>("gsc_vehicletypeid").Id);
            Assert.AreEqual(InsuranceEntityCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_vehicleuse"), salesOrder.GetAttributeValue<OptionSetValue>("gsc_vehicleuse"));
            #endregion
        }
        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 3/31/2016
        #region CreateCoverageAvailable

        #region Test Scenario:
        [TestMethod]

        public void CreateCoverageAvailable()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Insurance Entity
            var InsuranceEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_cmn_insurance",
                Attributes =
                {
                }
            };
            #endregion

            #region  Insurance Coverage Available Entity Collection
            var CoverageAvailableEntityCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_insurancecoverage",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_insurancecoverage",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_insuranceid", new EntityReference(InsuranceEntity.LogicalName, InsuranceEntity.Id)},
                            {"gsc_insurancecoveragepn", "Sample"},
                            {"gsc_suminsured", new Money(3000)},
                            {"gsc_premium", new Money(4000)}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Entity
            var SalesOrderEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "salesorder",
                Attributes =
                {
                     {"gsc_insuranceid", new EntityReference(InsuranceEntity.LogicalName, InsuranceEntity.Id)},
                     {"gsc_totalpremium", new Money(4000)}
                }
            };
            #endregion

            #region  Order Coverage Available Entity Collection
            var OrderCoverageAvailableEntityCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_insurancecoverageavailable",
                Entities =
                {
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName ==CoverageAvailableEntityCollection.EntityName)
                ))).Returns(CoverageAvailableEntityCollection);

            orgServiceMock.Setup(service => service.Create(It.Is<Entity>(entity => entity.LogicalName == OrderCoverageAvailableEntityCollection.EntityName)));

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(SalesOrderEntity);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == SalesOrderEntity.LogicalName)))).Callback<Entity>(s => SalesOrderEntity = s);

            #endregion

            #region 2. Call/Action
            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrder = SalesOrderHandler.CreateCoverageAvailable(SalesOrderEntity, "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderEntity.Id, salesOrder.GetAttributeValue<EntityReference>("gsc_orderid").Id);
            Assert.AreEqual(CoverageAvailableEntityCollection.Entities[0].GetAttributeValue<String>("gsc_insurancecoveragepn"), salesOrder.GetAttributeValue<String>("gsc_ordercoverageavailablepn"));
            Assert.AreEqual(CoverageAvailableEntityCollection.Entities[0].GetAttributeValue<Money>("gsc_suminsured").Value, salesOrder.GetAttributeValue<Money>("gsc_suminsured").Value);
            Assert.AreEqual(CoverageAvailableEntityCollection.Entities[0].GetAttributeValue<Money>("gsc_suminsured").Value, salesOrder.GetAttributeValue<Money>("gsc_suminsured").Value);
            #endregion
        }
        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 4/1/2016
        #region Set Additional Accessories Amount field value

        #region Test Scenario : Order contains additional accessories
        [TestMethod]
        public void SetAddAccessoriesAmountFieldValue()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;


            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_accessories", new Money(Decimal.Zero)}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Detail EntityCollection
            var SalesOrderDetailCollection = new EntityCollection
            {
                EntityName = "salesorderdetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorderdetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"salesorderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)},
                            {"priceperunit", new Money((Decimal)100000.00)}
                        }
                    },
                    
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorderdetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"salesorderid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].Id)},
                            {"priceperunit", new Money((Decimal)200000.00)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderDetailCollection.EntityName)
                ))).Returns(SalesOrderDetailCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
           // Entity salesOrder = SalesOrderHandler.(SalesOrderCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_accessories").Value, 300000);
            #endregion
        }
        #endregion

        #endregion
    
        //Created By : Jerome Anthony Gerero, Created On : 4/1/2016
        #region Set Color Amount field value

        #region Test Scenario : Preferred Color field contains data
        [TestMethod]
        public void SetColorAmountFieldValue()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order EntityCollection
            var SalesOrderCollection = new EntityCollection
            {
                EntityName = "salesorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "salesorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_colorprice", new Money(Decimal.Zero)},
                            {"gsc_vehiclecolorid1", new EntityReference("gsc_cmn_vehiclecolor", Guid.NewGuid())}
                        }
                    }
                }
            };
            #endregion

            #region Vehicle Color EntityCollection
            var VehicleColorCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_vehiclecolor",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_vehiclecolor",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_cmn_vehiclecolorid", new EntityReference("salesorder", SalesOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclecolorid1").Id)},
                            {"gsc_additionalprice", new Money((Decimal)100000.00)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VehicleColorCollection.EntityName)
                ))).Returns(VehicleColorCollection);

            #endregion

            #region 2. Call/Action

            var SalesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrder = SalesOrderHandler.SetVehicleColorAmount(SalesOrderCollection.Entities[0], "Update");
            #endregion

            #region 3. Verify
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_colorprice").Value, VehicleColorCollection.Entities[0].GetAttributeValue<Money>("gsc_additionalprice").Value);
            #endregion
        }
        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 4/11/2016
        #region Create Monthly Amortization Record

        #region Test Scenario : Financing Scheme ID contains data
        [TestMethod]
        public void DeleteAndCreateMonthlyAmortizationRecord()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Color EntityCollection
            var ColorCollection = new EntityCollection()
            {
                EntityName = "gsc_cmn_vehiclecolor",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_vehiclecolor",
                        Attributes =
                        {
                            {"gsc_additionalprice", new Money(20000)}
                        }
                    }
                }
            };
            #endregion

            #region Product Entity
            var Product = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "product",
                Attributes =
                {
                }
            };
            #endregion

            #region Financing Term EntityCollection
            var FinancingTermCollection = new EntityCollection
            {
                EntityName = "gsc_sls_financingterm",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_financingterm",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_financingtermpn", "60"}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_financingterm",
                        Attributes =
                        {
                            {"gsc_financingtermpn", "48"}
                        }
                    }
                }
            };
            #endregion

            #region Financing Scheme EntityCollection
            var FinancingSchemeCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_financingscheme",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_financingscheme",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_financingschemepn", "Scheme 1"}
                        }
                    }
                }
            };
            #endregion

            #region Financing Scheme Details Entity
            double addonrate60 = 36.57;
            double addonrate48 = 28.40;

            var FinancingSchemeDetailsCollection = new EntityCollection()
            {
                EntityName = "gsc_cmn_financingschemedetails",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_financingschemedetails",
                        Attributes =
                        {
                            {"gsc_financingschemeid", new EntityReference(FinancingSchemeCollection.Entities[0].LogicalName, FinancingSchemeCollection.Entities[0].Id)},
                            {"gsc_financingtermid", new EntityReference(FinancingTermCollection.EntityName, FinancingTermCollection.Entities[0].Id)},
                            {"gsc_addonrate", addonrate60}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_financingschemedetails",
                        Attributes =
                        {
                            {"gsc_financingschemeid", new EntityReference(FinancingSchemeCollection.Entities[0].LogicalName, FinancingSchemeCollection.Entities[0].Id)},
                            {"gsc_financingtermid", new EntityReference(FinancingTermCollection.EntityName, FinancingTermCollection.Entities[1].Id)},
                            {"gsc_addonrate", addonrate48}
                        }
                    }
                }
            };
            #endregion

            #region Sales Order Entity
            var salesOrder = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "",
                Attributes = 
                { 
                    {"gsc_productid", new EntityReference(Product.LogicalName, Product.Id)},
                    {"gsc_vehiclecolorid1", new EntityReference(ColorCollection.EntityName, ColorCollection.Entities[0].Id)},
                    {"gsc_financingschemeid", new EntityReference(FinancingSchemeCollection.Entities[0].LogicalName, FinancingSchemeCollection.Entities[0].Id)},
                    {"gsc_vehicleunitprice", new Money(1000000)},
                    {"gsc_amountfinanced", new Money(25000)}
                }
            };
            #endregion

            #region Monthly Amortization Entity
            var MonthlyAmortization = new EntityCollection()
            {
                EntityName = "gsc_sls_ordermonthlyamortization",
                Entities =
                {
                }
            };
            #endregion

            #region Created Monthly Amortization Entity
            var CreatedMonthlyAmortization = new EntityCollection()
            {
                EntityName = "gsc_sls_ordermonthlyamortization",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_ordermonthlyamortization",
                        Attributes =
                        {
                            {"gsc_orderid", new EntityReference(salesOrder.LogicalName, salesOrder.Id)},
                            {"gsc_financingschemeid", new EntityReference(FinancingSchemeCollection.Entities[0].LogicalName, FinancingSchemeCollection.Entities[0].Id)},
                            {"gsc_financingtermid", new EntityReference(FinancingTermCollection.EntityName, FinancingTermCollection.Entities[0].Id)},
                            {"gsc_ordermonthlyamortizationpn", "535.00"}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ColorCollection.EntityName)
                ))).Returns(ColorCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == FinancingSchemeDetailsCollection.EntityName)
                  ))).Returns(FinancingSchemeDetailsCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == FinancingTermCollection.EntityName)
                  ))).Returns(FinancingTermCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == MonthlyAmortization.EntityName)
                ))).Returns(MonthlyAmortization);

            orgServiceMock.Setup(service => service.Create(It.Is<Entity>(entity => entity.LogicalName == MonthlyAmortization.EntityName)));

            #endregion

            #region 2. Call/Action
            var salesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity createdMonthlyAmortization = salesOrderHandler.DeleteExistingMonthlyAmortizationRecords(salesOrder);
            #endregion

            #region 3. Verify
            Assert.AreEqual(CreatedMonthlyAmortization.Entities[0].GetAttributeValue<EntityReference>("gsc_orderid").Id, createdMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_orderid").Id);
            Assert.AreEqual(CreatedMonthlyAmortization.Entities[0].GetAttributeValue<EntityReference>("gsc_financingtermid").Id, createdMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_financingtermid").Id);
            Assert.AreEqual(CreatedMonthlyAmortization.Entities[0]["gsc_ordermonthlyamortizationpn"], createdMonthlyAmortization["gsc_ordermonthlyamortizationpn"]);
            #endregion
        }
        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 4/12/2016
        #region Calculate Net Price Amount

        #region Test Scenario : Unit Price, Color Price, and Discount Amount fields contains data
        [TestMethod]
        public void SetNetPriceAmount()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order EntityCollection

            Decimal unitPrice = 1800000;
            Decimal colorPrice = 25000;
            Decimal discount = 20000;

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
                            {"gsc_unitprice", new Money(unitPrice)},
                            {"gsc_colorprice", new Money(colorPrice)},
                            {"gsc_discount", new Money(discount)},
                            {"gsc_netprice", new Money(Decimal.Zero)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action
            var salesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrder = salesOrderHandler.SetNetPriceAmount(SalesOrderCollection.Entities[0], "Update");
            #endregion

            Decimal totalPrice = unitPrice + colorPrice;
            Decimal netPrice = totalPrice - discount;

            #region 3. Verify
            Assert.AreEqual(SalesOrderCollection.Entities[0].GetAttributeValue<Money>("gsc_netprice").Value, netPrice);
            #endregion
        }
        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 4/12/2016
        #region Calculate Total Cash Outlay Amount

        #region Test Scenario : Net Down Payment Amount, Chattel Fee Amount, and Insurance Amount fields contains data
        [TestMethod]
        public void SetTotalCashOutlayAmount()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Sales Order EntityCollection

            Decimal netDownPayment = 50000;
            Decimal chattelFee = 25000;
            Decimal insurance = 3000;

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
                            {"gsc_netdownpayment", new Money(netDownPayment)},
                            {"gsc_chattelfee", new Money(chattelFee)},
                            {"gsc_insurance", new Money(insurance)},
                            {"gsc_totalcashoutlay", new Money(Decimal.Zero)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == SalesOrderCollection.EntityName)
                ))).Returns(SalesOrderCollection);

            #endregion

            #region 2. Call/Action
            var salesOrderHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity salesOrder = salesOrderHandler.SetTotalCashOutlayAmount(SalesOrderCollection.Entities[0], "Update");
            #endregion

            Decimal totalCashOutlay = netDownPayment + chattelFee + insurance;

            #region 3. Verify
            Assert.AreEqual(salesOrder.GetAttributeValue<Money>("gsc_totalcashoutlay").Value, totalCashOutlay);
            #endregion
        }
        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 4/26/2016
        #region CreateRequirementChecklist

        #region Test Scenario: Will Create Requirement Checklist
        [TestMethod]

        public void CreateRequirementChecklist()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Document Entity Collection
            var Document = new EntityCollection()
            {
                EntityName = "gsc_sls_document",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_document",
                        Attributes = 
                        {
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_document",
                        Attributes = 
                        {
                        }
                    }
                }
            };
            #endregion

            #region Bank Entity Collection
            var Bank = new EntityCollection()
            {
                EntityName = "gsc_sls_bank",
                Entities =
                {
                    new Entity 
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_bank",
                        Attributes =
                        {
                        }
                    }
                }
            };
            #endregion

            #region Document Checklist Entity Collection
            var DocumentChecklist = new EntityCollection()
            {
                EntityName = "gsc_sls_documentchecklist",
                Entities =
                {
                    new Entity 
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_documentchecklist",
                        Attributes = 
                        {
                            {"gcs_bankid", Bank.Entities[0].Id},
                            {"gsc_documentid", Document.Entities[0].Id},
                            {"gsc_documentchecklistpn", "Sample Document"},
                            {"gsc_documenttype", true},
                            {"gsc_customertype", new OptionSetValue(1)},
                            {"gsc_mandatory", true }
                        },
                        FormattedValues = 
                        {
                            {"gsc_customertype", "Individual"}
                        }
                    }
                }
            };
            #endregion

            #region Order Entity
            var OrderEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "order",
                Attributes =
                {
                    {"gsc_bankid", new EntityReference(Bank.EntityName, Bank.Entities[0].Id)}
                }
            };
            #endregion

            #region Requirement Checklist Entity Collection
            var RequirementChecklist = new EntityCollection()
            {
                EntityName = "gsc_sls_requirementchecklist",
                Entities =
                {
                }
            };
            #endregion

            #region Created Requirement Checklist Entity Collection
            var CreatedRequirementChecklist = new EntityCollection()
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
                            {"gsc_orderid", OrderEntity.Id},
                            {"gsc_bankid", Bank.Entities[0].Id},
                            {"gsc_documentchecklistid", DocumentChecklist.Entities[0].Id},
                            {"gsc_requirementchecklistpn", "Sample Document"},
                            {"gsc_mandatory", true},
                            {"gsc_documenttype", true},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
               It.Is<QueryExpression>(expression => expression.EntityName == RequirementChecklist.EntityName)
               ))).Returns(RequirementChecklist);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
             It.Is<QueryExpression>(expression => expression.EntityName == DocumentChecklist.EntityName)
             ))).Returns(DocumentChecklist);

            orgServiceMock.Setup(service => service.Create(It.Is<Entity>(entity => entity.LogicalName == RequirementChecklist.EntityName)));

            #endregion

            #region 2. Call / Action
            var QuoteHandler = new SalesOrderHandler(orgService, orgTracing);
           // Entity RequirementCreated = QuoteHandler.CreateRequirementChecklist(OrderEntity);
            #endregion

            #region 3. Verify
           // Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_orderid"], RequirementCreated.GetAttributeValue<EntityReference>("gsc_orderid").Id);
           // Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_bankid"], RequirementCreated.GetAttributeValue<EntityReference>("gsc_bankid").Id);
           // Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_documentchecklistid"], RequirementCreated.GetAttributeValue<EntityReference>("gsc_documentchecklistid").Id);
           // Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_requirementchecklistpn"], RequirementCreated["gsc_requirementchecklistpn"]);
           // Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_mandatory"], RequirementCreated.GetAttributeValue<Boolean>("gsc_mandatory"));
           // Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_documenttype"], RequirementCreated.GetAttributeValue<Boolean>("gsc_documenttype"));
            #endregion
        }

        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 5/17/2016
        #region AllocateVehicle

        #region Test Scenario: AllocateVehicle
        [TestMethod]

        public void AllocateVehicle()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Order Entity
            var OrderEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "order",
                Attributes =
                {
                    {"gsc_inventoryidtoallocate", "1a0effa2-2d1b-e611-80d8-00155d010e2c"}
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
                            {"gsc_available",3},
                            {"gsc_allocated",0},
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
                            {"gsc_status",new OptionSetValue(100000000)},
                            {"gsc_productquantityid", new EntityReference(ProductQuantity.EntityName, ProductQuantity.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            #region Allocated Vehicle Entity Collection
            var AllocatedVehicle = new EntityCollection()
            {
                EntityName = "gsc_iv_allocatedvehicle",
                Entities =
                {
                    /*new Entity 
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_allocatedvehicle",
                        Attributes = 
                        {
                            {"gsc_color",""},
                            {"gsc_csno",""},
                            {"gsc_engineno",""},
                            {"gsc_modelcode",""},
                            {"gsc_optioncode",""},
                            {"gsc_productionno",""},
                            {"gsc_vin",""},
                            {"gsc_vehicleallocateddate",""},
                            {"gsc_inventoryid",""}
                        }
                    }*/
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == Inventory.EntityName)
              ))).Returns(Inventory);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
             It.Is<QueryExpression>(expression => expression.EntityName == AllocatedVehicle.EntityName)
             ))).Returns(AllocatedVehicle);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
             It.Is<QueryExpression>(expression => expression.EntityName == ProductQuantity.EntityName)
             ))).Returns(ProductQuantity);

            orgServiceMock.Setup(service => service.Create(It.Is<Entity>(entity => entity.LogicalName == AllocatedVehicle.EntityName)));

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Inventory.Entities[0].LogicalName)))).Callback<Entity>(s => Inventory.Entities[0] = s);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == ProductQuantity.Entities[0].LogicalName)))).Callback<Entity>(s => ProductQuantity.Entities[0] = s);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(Inventory.Entities[0]);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(ProductQuantity.Entities[0]);

            #endregion

            #region 2. Call / Action
            var QuoteHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity CreatedAllocated = QuoteHandler.AllocateVehicle(OrderEntity);
            #endregion

            #region 3. Verify
            Assert.AreEqual(Inventory.Entities[0].GetAttributeValue<String>("gsc_color"), CreatedAllocated.GetAttributeValue<String>("gsc_color"));
            Assert.AreEqual(Inventory.Entities[0].GetAttributeValue<String>("gsc_csno"), CreatedAllocated.GetAttributeValue<String>("gsc_csno"));
            Assert.AreEqual(Inventory.Entities[0].GetAttributeValue<String>("gsc_engineno"), CreatedAllocated.GetAttributeValue<String>("gsc_engineno"));
            Assert.AreEqual(Inventory.Entities[0].GetAttributeValue<String>("gsc_modelcode"), CreatedAllocated.GetAttributeValue<String>("gsc_modelcode"));
            Assert.AreEqual(Inventory.Entities[0].GetAttributeValue<String>("gsc_optioncode"), CreatedAllocated.GetAttributeValue<String>("gsc_optioncode"));
            Assert.AreEqual(Inventory.Entities[0].GetAttributeValue<String>("gsc_productionno"), CreatedAllocated.GetAttributeValue<String>("gsc_productionno"));
            Assert.AreEqual(Inventory.Entities[0].GetAttributeValue<String>("gsc_vin"), CreatedAllocated.GetAttributeValue<String>("gsc_vin"));
            Assert.AreEqual(DateTime.Today.ToString("MM-dd-yyyy"), CreatedAllocated.GetAttributeValue<DateTime>("gsc_vehicleallocateddate").ToString("MM-dd-yyyy"));
            Assert.AreEqual(2, ProductQuantity.Entities[0].GetAttributeValue<Int32>("gsc_available"));
            Assert.AreEqual(1, ProductQuantity.Entities[0].GetAttributeValue<Int32>("gsc_allocated"));
            #endregion
        }
        #endregion

        #region Test Scenario: Vehicle Aready Allocated
        [TestMethod]

        public void VehicleAllocated()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Order Entity
            var OrderEntity = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "order",
                Attributes =
                {
                    {"gsc_inventoryidtoallocate", "1a0effa2-2d1b-e611-80d8-00155d010e2c"}
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
                            {"gsc_status",new OptionSetValue(100000000)}
                        }
                    }
                }
            };
            #endregion

            #region Allocated Vehicle Entity Collection
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
                            {"gsc_color","Black"},
                            {"gsc_csno","1"},
                            {"gsc_engineno","2"},
                            {"gsc_modelcode","3"},
                            {"gsc_optioncode","4"},
                            {"gsc_productionno","5"},
                            {"gsc_vin","6"},
                            {"gsc_vehicleallocateddate",""},
                            {"gsc_inventoryid", new EntityReference(Inventory.EntityName, Inventory.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == Inventory.EntityName)
              ))).Returns(Inventory);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
             It.Is<QueryExpression>(expression => expression.EntityName == AllocatedVehicle.EntityName)
             ))).Returns(AllocatedVehicle);

            orgServiceMock.Setup(service => service.Create(It.Is<Entity>(entity => entity.LogicalName == AllocatedVehicle.EntityName)));

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Inventory.Entities[0].LogicalName)))).Callback<Entity>(s => Inventory.Entities[0] = s);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(Inventory.Entities[0]);

            #endregion

            #region 2. Call / Action
            var QuoteHandler = new SalesOrderHandler(orgService, orgTracing);
            Entity CreatedAllocated = QuoteHandler.AllocateVehicle(OrderEntity);
            #endregion

            #region 3. Verify
            Assert.AreEqual(null, CreatedAllocated);
            #endregion
        }
        #endregion

        #endregion
    }
}
