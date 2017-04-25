using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.Quote;
using GSC.Rover.DMS.BusinessLogic.QuoteDiscount;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace QuoteUnitTests
{
    [TestClass]
    public class QuoteHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 2/10/2016
        #region Replicate Opportunity to Quote
        [TestMethod]

        #region Test Scenario : Opportunity ID is provided
        public void ReplicateOpportunityInfoUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            var City = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_syscity",
                Attributes =
                {
                    {"name", "Manila"}
                }
            };

            #region Opportunity EntityCollection
            var OpportunityCollection = new EntityCollection
            {
                EntityName = "opportunity",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "opportunity",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            //{"gsc_salesexecutiveid", new EntityReference("contact", Guid.NewGuid())
                            //{ Name = "Citimotors"}},
                            {"gsc_vehiclebasemodelid", new EntityReference("gsc_iv_vehiclebasemodel", Guid.NewGuid())
                            { Name = "Montero"}},
                            {"gsc_colorid", new EntityReference("gsc_iv_color", Guid.NewGuid())
                            { Name = "Jet Black"}},
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
                        Id = Guid.NewGuid(),
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            //{"gsc_salesexecutiveid", ""},
                            {"gsc_vehiclebasemodelid", ""},
                            {"gsc_color1id", ""},
                            {"opportunityid", new EntityReference("opportunity", OpportunityCollection.Entities[0].Id)},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == OpportunityCollection.EntityName)
                ))).Returns(OpportunityCollection);

            #endregion

            #region 2. Call/Action

            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity quote = QuoteHandler.ReplicateOpportunityInfo(QuoteCollection.Entities[0]);
            #endregion

            #region 3. Verify
            //Assert.AreEqual(OpportunityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id, quote.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id);
            //Assert.AreEqual(OpportunityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id, quote.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id);
            Assert.AreEqual(OpportunityCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_colorid").Id, quote.GetAttributeValue<EntityReference>("gsc_color1id").Id);
            #endregion

        }
        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 2/10/16
        #region ConcatenateVehicleDescription

        #region Test Scenario on Create: Vehicle Information are provided
        [TestMethod]

        public void ConcatenatedVehicleDescriptionOnCreate()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Entity
            var ProductCollection = new EntityCollection()
            {
                EntityName = "product",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "product",
                        Attributes =
                        {
                            {"gsc_sellprice", new Money(1000000)},
                            {"gsc_enginetype", "Type A"},
                            {"gsc_transmission", new OptionSetValue(1)},
                            {"gsc_grossvehicleweight", "1"}, 
                            {"gsc_pistondisplacement", "1"},
                            {"gsc_fueltype", new OptionSetValue(1)},
                            {"gsc_status", new OptionSetValue(1)},
                            {"gsc_warrantyexpirydays", "15"},
                            {"gsc_warrantymileage", "1"},
                            {"gsc_othervehicledetails","Other Details"}
                        },
                        FormattedValues = 
                        {
                            {"gsc_transmission","Manual"},
                            {"gsc_fueltype", "Gasoline"},
                            {"gsc_status", "New"}
                        }
                    }
                }
            };
            #endregion

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_productid", new EntityReference("product", ProductCollection.Entities[0].Id)},
                    {"gsc_vehicledetails", ""},
                    {"gsc_vehicleunitprice", new Money()}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
                  ))).Returns(ProductCollection);

            //orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Quote.LogicalName)))).Callback<Entity>(s => Quote = s);

            #endregion

            #region 2. Call/Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity updatedQuote = QuoteHandler.ConcatenateVehicleDescription(Quote, "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(Quote.GetAttributeValue<Money>("gsc_vehicleunitprice").Value, updatedQuote.GetAttributeValue<Money>("gsc_vehicleunitprice").Value);
            Assert.AreEqual(Quote["gsc_vehicledetails"], updatedQuote["gsc_vehicledetails"]);
            #endregion

        }

        #endregion

        #region Test Scenario on Create: Vehicle Information are NULL
        [TestMethod]

        public void ConcatenatedVehicleDescriptionOnCreatewithNullValues()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Entity
            var ProductCollection = new EntityCollection()
            {
                EntityName = "product",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "product",
                        Attributes =
                        {
                            {"gsc_sellprice", new Money()},
                            {"gsc_enginetype", ""},
                            {"gsc_transmission", new OptionSetValue()},
                            {"gsc_grossvehicleweight", ""}, 
                            {"gsc_pistondisplacement", ""},
                            {"gsc_fueltype", new OptionSetValue()},
                            {"gsc_status", new OptionSetValue()},
                            {"gsc_warrantyexpirydays", ""},
                            {"gsc_warrantymileage", ""},
                            {"gsc_othervehicledetails",""}
                        },
                        FormattedValues = 
                        {
                            {"gsc_transmission",""},
                            {"gsc_fueltype", ""},
                            {"gsc_status", ""}
                        }
                    }
                }
            };
            #endregion

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_productid", new EntityReference("product", ProductCollection.Entities[0].Id)},
                    {"gsc_vehicledetails", ""},
                    {"gsc_vehicleunitprice", new Money()},
                    {"gsc_unitprice", new Money()},
                    {"gsc_colorprice", new Money()},
                    {"gsc_totaldiscount", new Money(2000)},
                    {"gsc_netprice", new Money()}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
                  ))).Returns(ProductCollection);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Quote.LogicalName)))).Callback<Entity>(s => Quote = s);

            #endregion

            #region 2. Call/Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity updatedQuote = QuoteHandler.ConcatenateVehicleDescription(Quote, "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(Quote.GetAttributeValue<Money>("gsc_vehicleunitprice").Value, updatedQuote.GetAttributeValue<Money>("gsc_vehicleunitprice").Value);
            Assert.AreEqual(Quote["gsc_vehicledetails"], updatedQuote["gsc_vehicledetails"]);
            Assert.AreEqual(Quote["gsc_vehicleunitprice"], Quote["gsc_unitprice"]);
            Assert.AreEqual(0, Quote.GetAttributeValue<Money>("gsc_netprice").Value);
            #endregion

        }

        #endregion

        #region Test Scenario on Update: Vehicle Information are provided
        [TestMethod]

        public void ConcatenatedVehicleDescriptionOnUpdate()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Product Entity
            var ProductCollection = new EntityCollection()
            {
                EntityName = "product",
                Entities =
                {
                    new Entity
                    {
                        Id = new Guid("60BA7F89-C338-E411-9417-08002731E536"),
                        LogicalName = "product",
                        Attributes =
                        {
                            {"gsc_sellprice", new Money(1000000)},
                            {"gsc_enginetype", "Type A"},
                            {"gsc_transmission", new OptionSetValue(1)},
                            {"gsc_grossvehicleweight", "1"}, 
                            {"gsc_pistondisplacement", "1"},
                            {"gsc_fueltype", new OptionSetValue(1)},
                            {"gsc_status", new OptionSetValue(1)},
                            {"gsc_warrantyexpirydays", "15"},
                            {"gsc_warrantymileage", "1"},
                            {"gsc_othervehicledetails","Other Details"}
                        },
                        FormattedValues = 
                        {
                            {"gsc_transmission","Manual"},
                            {"gsc_fueltype", "Gasoline"},
                            {"gsc_status", "New"}
                        }
                    },
                    new Entity
                    {
                        Id = new Guid("60BA7F89-C338-E411-9417-08002731E535"),
                        LogicalName = "product",
                        Attributes =
                        {
                            {"gsc_sellprice", "2000000"},
                            {"gsc_enginetype", "Type B"},
                            {"gsc_transmission", new OptionSetValue(1)},
                            {"gsc_grossvehicleweight", "1"}, 
                            {"gsc_pistondisplacement", "1"},
                            {"gsc_fueltype", new OptionSetValue(1)},
                            {"gsc_status", new OptionSetValue(1)},
                            {"gsc_warrantyexpirydays", "15"},
                            {"gsc_warrantymileage", "1"},
                            {"gsc_othervehicledetails","Other Details"}
                        },
                        FormattedValues = 
                        {
                            {"gsc_transmission","Automatic"},
                            {"gsc_fueltype", "Diesel"},
                            {"gsc_status", "Old"}
                        }
                    }
                }
            };
            #endregion

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_productid", new EntityReference("product", ProductCollection.Entities[1].Id)},
                    {"gsc_vehicledetails", "Type A, Manual, 1, 1, Gasoline, New, 15, 1, Other Details"},
                    {"gsc_vehicleunitprice", new Money(10000)},
                    {"gsc_unitprice", new Money()},
                    {"gsc_colorprice", new Money()},
                    {"gsc_totaldiscount", new Money(2000)},
                    {"gsc_netprice", new Money()}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ProductCollection.EntityName)
                  ))).Returns(ProductCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(Quote);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Quote.LogicalName)))).Callback<Entity>(s => Quote = s);

            #endregion

            #region 2. Call/Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity updatedQuote = QuoteHandler.ConcatenateVehicleDescription(Quote, "Update");
            #endregion

            #region 3. Verify
            Assert.AreEqual(Quote.GetAttributeValue<Money>("gsc_vehicleunitprice").Value, updatedQuote.GetAttributeValue<Money>("gsc_vehicleunitprice").Value);
            Assert.AreEqual(Quote["gsc_vehicledetails"], updatedQuote["gsc_vehicledetails"]);
            Assert.AreEqual(Quote["gsc_vehicleunitprice"], Quote["gsc_unitprice"]);
            Assert.AreEqual(998000, Quote.GetAttributeValue<Money>("gsc_netprice").Value);
            #endregion

        }

        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 2/16/2016
        #region PopulateInsuranceCoverage

        #region Test Scenario: Insurance not Free
        [TestMethod]

        public void PopulateInsuranceCoverage_InsuranceNotFree()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_free", false},
                    {"gsc_totalpremium", new Money(2000)},
                    {"gsc_insurance", new Money()}
                }
            };
            #endregion

            #endregion

            #region 2. Call/Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            //Money totalpremium = QuoteHandler.PopulateInsuranceCoverage(Quote, "Create");
            #endregion

            #region 3. Verify
            //Assert.AreEqual(Quote.GetAttributeValue<Money>("gsc_totalpremium"), totalpremium);
            #endregion

        }

        #endregion

        #region Test Scenario: Insurance is Free
        [TestMethod]

        public void PopulateInsuranceCoverage_InsuranceFree()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_free", true},
                    {"gsc_totalpremium", new Money(2000)},
                    {"gsc_insurance", new Money()}
                }
            };
            #endregion

            #endregion

            #region 2. Call/Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            //Money totalpremium = QuoteHandler.PopulateInsuranceCoverage(Quote, "Create");
            #endregion

            #region 3. Verify
            //Assert.AreEqual(new Money(0), totalpremium);
            #endregion

        }

        #endregion

        #region Test Scenario: On Update - Insurance is Free
        [TestMethod]

        public void PopulateInsuranceCoverageOnUpdate_InsuranceFree()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_free", false},
                    {"gsc_totalpremium", new Money(2000)},
                    {"gsc_insurance", new Money(1000)}
                }
            };
            #endregion


            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(Quote);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Quote.LogicalName)))).Callback<Entity>(s => Quote = s);

            #endregion

            #region 2. Call/Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            //Money totalpremium = QuoteHandler.PopulateInsuranceCoverage(Quote, "Update");
            #endregion

            #region 3. Verify
            //Assert.AreEqual(Quote.GetAttributeValue<Money>("gsc_insurance"), totalpremium);
            #endregion

        }

        #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 2/15/2016
        #region Create Free Quote Products
        [TestMethod]

        #region Test Scenario : Opportunity ID is provided
        public void CreateFreeItemsInfoUnitTest()
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
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"productid", new EntityReference("product", new Guid("b360c58f-c7f8-4b37-af9c-7752d3e4740d"))
                            { Name = "Montero"}},
                            {"substitutedproductid", new EntityReference("product", Guid.NewGuid())
                            { Name = "Wheels"}}
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
                        Id = Guid.NewGuid(),
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_productid", new EntityReference("product", new Guid("B360C58F-C7F8-4B37-AF9C-7752D3E4740D"))}
                        }
                    }
                }
            };
            #endregion

            #region Quote Product EntityCollection
            var QuoteProductCollection = new EntityCollection
            {
                EntityName = "quotedetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "quotedetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", QuoteCollection.Entities[0].Id},
                            {"gsc_productid", Guid.NewGuid()}
                        }
                    },

                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "quotedetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", QuoteCollection.Entities[0].Id},
                            {"gsc_productid", Guid.NewGuid()}
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
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteProductCollection.EntityName)
                ))).Returns(QuoteProductCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == UnitCollection.EntityName)
                ))).Returns(UnitCollection);

            #endregion

            #region 2. Call/Action

            string message = "Create";

            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity quoteProduct = QuoteHandler.RetrieveAndCreateVehicleFreebies(QuoteCollection.Entities[0], message);
            #endregion

            #region 3. Verify
            Assert.AreEqual(ProductRelationshipCollection.Entities[0].GetAttributeValue<EntityReference>("substitutedproductid").Id, quoteProduct.GetAttributeValue<EntityReference>("productid").Id);
            #endregion
        }
        #endregion

        #endregion

        //Created By: Leslie Baligauat, Created On: 2/16/2016
        #region CreateMonthlyAmortization

        #region Test Scenario: Create Monthly Amortization Record
        [TestMethod]

        public void CreateMonthlyAmortization()
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
            var FinancingTermCollection = new EntityCollection()
            {
                EntityName = "gsc_sls_financingterm",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_financingterm",
                        Attributes =
                        {
                            {"gsc_financingtermpn", "60"},
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_financingterm",
                        Attributes =
                        {
                            {"gsc_financingtermpn", "48"},
                        }
                    }
                }
            };
            #endregion

            #region Financing Scheme Entity
            var FinancingScheme = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_cmn_financingscheme",
                Attributes =
                {
                    {"gsc_financingschemepn", "Scheme 1"},
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
                             {"gsc_financingschemeid", new EntityReference(FinancingScheme.LogicalName, FinancingScheme.Id)},
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
                            {"gsc_financingschemeid", new EntityReference(FinancingScheme.LogicalName, FinancingScheme.Id)},
                            {"gsc_financingtermid", new EntityReference(FinancingTermCollection.EntityName, FinancingTermCollection.Entities[1].Id)},
                            {"gsc_addonrate", addonrate48}
                        }
                    }
                }
            };
            #endregion

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_productid", new EntityReference(Product.LogicalName, Product.Id)},
                    {"gsc_vehiclecolorid1", new EntityReference(ColorCollection.EntityName, ColorCollection.Entities[0].Id)},
                    {"gsc_financingschemeid", new EntityReference(FinancingScheme.LogicalName, FinancingScheme.Id)},
                    {"gsc_vehicleunitprice", new Money(1000000)},
                    {"gsc_amountfinanced", new Money(25000)}
                }
            };
            #endregion

            #region Monthly Amortization Entity
            var MonthlyAmortization = new EntityCollection()
            {
                EntityName = "gsc_sls_quotemonthlyamortization",
                Entities =
                {
                }
            };
            #endregion

            #region Created Monthly Amortization Entity
            var CreatedMonthlyAmortization = new EntityCollection()
            {
                EntityName = "gsc_sls_quotemonthlyamortization",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_quotemonthlyamortization",
                        Attributes =
                        {
                            {"gsc_quoteid", new EntityReference(Quote.LogicalName,Quote.Id)},
                            {"gsc_financingschemeid", new EntityReference(FinancingScheme.LogicalName, FinancingScheme.Id)},
                            {"gsc_financingtermid", new EntityReference(FinancingTermCollection.EntityName, FinancingTermCollection.Entities[0].Id)},
                            {"gsc_quotemonthlyamortizationpn", "535.00"}
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
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity createdMonthlyAmortization = QuoteHandler.CheckMonthlyAmortizationRecord(Quote);
            #endregion

            #region 3. Verify
            Assert.AreEqual(CreatedMonthlyAmortization.Entities[0].GetAttributeValue<EntityReference>("gsc_quoteid").Id, createdMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_quoteid").Id);
            Assert.AreEqual(CreatedMonthlyAmortization.Entities[0].GetAttributeValue<EntityReference>("gsc_financingtermid").Id, createdMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_financingtermid").Id);
            Assert.AreEqual(CreatedMonthlyAmortization.Entities[0]["gsc_quotemonthlyamortizationpn"], createdMonthlyAmortization["gsc_quotemonthlyamortizationpn"]);
            #endregion

        }

        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 3/3/2016
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

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
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
                            {"gsc_quoteid", Quote.Id},
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
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity RequirementCreated = QuoteHandler.CreateRequirementChecklist(Quote);
            #endregion

            #region 3. Verify
            Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_quoteid"], RequirementCreated.GetAttributeValue<EntityReference>("gsc_quoteid").Id);
            Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_bankid"], RequirementCreated.GetAttributeValue<EntityReference>("gsc_bankid").Id);
            Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_documentchecklistid"], RequirementCreated.GetAttributeValue<EntityReference>("gsc_documentchecklistid").Id);
            Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_requirementchecklistpn"], RequirementCreated["gsc_requirementchecklistpn"]);
            Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_mandatory"], RequirementCreated.GetAttributeValue<Boolean>("gsc_mandatory"));
            Assert.AreEqual(CreatedRequirementChecklist.Entities[0]["gsc_documenttype"], RequirementCreated.GetAttributeValue<Boolean>("gsc_documenttype"));
            #endregion
        }

        #endregion

        #endregion

        //Created By: Leslie Baliguat, Cretaed On: 3/16/2016
        #region PopulateColorPrice

        #region Test Scenario: Update Color Price, Preferred Color 1 is given
        [TestMethod]

        public void UpdateColorPrice()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Vehicle Color EntityCollection
            var VehicleColor = new EntityCollection()
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
                            {"gsc_additionalprice", new Money(20000)},
                        }
                    }
                }
            };
            #endregion

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_vehiclecolorid1", new EntityReference(VehicleColor.EntityName, VehicleColor.Entities[0].Id)},
                    {"gsc_colorprice", new Money(0)},
                    {"gsc_unitprice", new Money(1000700)},
                    {"gsc_totaldiscount", new Money(2000)},
                    {"gsc_netprice", new Money()}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
             It.Is<QueryExpression>(expression => expression.EntityName == VehicleColor.EntityName)
             ))).Returns(VehicleColor);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(Quote);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Quote.LogicalName)))).Callback<Entity>(s => Quote = s);

            #endregion

            #region 2. Call / Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity UpdatedQuote = QuoteHandler.PopulateColorPrice(Quote, "Update");
            #endregion

            #region 3. Verify #region 3. Verify
            Assert.AreEqual(VehicleColor.Entities[0].GetAttributeValue<Money>("gsc_additionalprice").Value, UpdatedQuote.GetAttributeValue<Money>("gsc_colorprice").Value);
            Assert.AreEqual(1018700, Quote.GetAttributeValue<Money>("gsc_netprice").Value);
            #endregion
        }
        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 3/17/2016]
        #region ReplicateNetDownPayment

        #region Test Scenario: Replicate Net Downpayment
        [TestMethod]

        public void ReplicateNetDownPayment()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Quote Entity
            var Quote = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes =
                {
                    {"gsc_netdownpayment", new Money(150000)},
                    {"gsc_downpayment", new Money(0)},
                    {"gsc_chattelfee", new Money(1500)},
                    {"gsc_insurance", new Money(3000)},
                    {"gsc_totalcashoutlay", new Money(0)}
                }
            };
            #endregion

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(Quote);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == Quote.LogicalName)))).Callback<Entity>(s => Quote = s);

            #endregion

            #region 2. Call / Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            QuoteHandler.ReplicateNetDownPaymentAndNetAmountFinanced(Quote);
            #endregion

            #region 3. Verify
            Assert.AreEqual(Quote.GetAttributeValue<Money>("gsc_netdownpayment").Value, Quote.GetAttributeValue<Money>("gsc_downpayment").Value);
            Assert.AreEqual(154500, Quote.GetAttributeValue<Money>("gsc_totalcashoutlay").Value);
            #endregion
        }
        #endregion

        #endregion

        //Created By : Leslie Baliguat, Created On : 3/28/2016
        #region SetChattelFeeAmount

        #region Test Scenario : Bank ID is provided and Unit Price value is between 50,000.00 and 100,000.00
        [TestMethod]
        public void BankIdIsNotNull()
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
                        LogicalName = "quote",
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
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
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
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
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
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
                            {"gsc_loanamount", new Money((Decimal)150000.00)},
                            {"gsc_chattelfeeamount", new Money((Decimal)23288.00)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ChattelFeeCollection.EntityName)
                ))).Returns(ChattelFeeCollection);

            #endregion

            #region 2. Call/Action

            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity quote = QuoteHandler.SetChattelFeeAmount(QuoteCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(ChattelFeeCollection.Entities[0].GetAttributeValue<Money>("gsc_chattelfeeamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_chattelfee").Value);
            #endregion
        }
        #endregion

        #region Test Scenario : Free Chattel Fee is ticked
        [TestMethod]
        public void SetChattelFeeAmountFree()
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
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", Guid.NewGuid())
                            { Name = "Swiss Bank"}},
                            {"gsc_unitprice", new Money((Decimal)99999.99)},
                            {"gsc_freechattelfee", true},
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
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
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
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
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
                            {"gsc_bankid", new EntityReference("gsc_sls_bank", QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_bankid").Id)},
                            {"gsc_loanamount", new Money((Decimal)150000.00)},
                            {"gsc_chattelfeeamount", new Money((Decimal)23288.00)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ChattelFeeCollection.EntityName)
                ))).Returns(ChattelFeeCollection);

            #endregion

            #region 2. Call/Action

            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity quote = QuoteHandler.SetChattelFeeAmount(QuoteCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            Assert.AreEqual(Decimal.Zero, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_chattelfee").Value);
            #endregion
        }
        #endregion

        #endregion

        //Created By:Leslie Baliguat, Created On: 4/7/2016
        #region SetLessDiscount

        #region Test Scenario :

        [TestMethod]
        public void ComputeDeductedTotalDiscountAmountUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Color EntityCollection
            var ColorEntity = new Entity
            {
                Id = Guid.NewGuid(),
                LogicalName = "quote",
                Attributes = new AttributeCollection
                {
                     {"gsc_additionalprice", new Money(0)},
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
                        Id = Guid.NewGuid(),
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehicleunitprice", new Money(18000)},
                            {"gsc_vehiclecolorid1", new EntityReference()},
                            {"gsc_applytodpamount", new Money(10000)},
                            {"gsc_applytoafamount", new Money()},
                            {"gsc_applytoupamount", new Money()},
                            {"gsc_lessdiscount", new Money()},
                            {"gsc_lessdiscountaf", new Money()},
                            {"gsc_totaldiscount", new Money()},
                            {"gsc_netdownpayment", new Money()},
                            {"gsc_amountfinanced", new Money()},
                            {"gsc_netamountfinanced", new Money()},
                            {"gsc_netprice", ""},
                            {"statecode" , new OptionSetValue(0)}
                        }
                    }
                }
            };
            #endregion

            #region Quote Discount EntityCollection
            var QuoteDiscountCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_quotediscount",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_quotediscount",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)
                            { Name = "Sample Quote 1"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)}
                        }
                    },
                    
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_appliedpricelist",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_quoteid", new EntityReference("quote", QuoteCollection.Entities[0].Id)
                            { Name = "Sample Quote 2"}},
                            {"gsc_discountamount", new Money((Decimal)100000.00)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteDiscountCollection.EntityName)
                ))).Returns(QuoteDiscountCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == QuoteCollection.EntityName)
                ))).Returns(QuoteCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(QuoteCollection.Entities[0]);

            #endregion

            #region 2. Call/Action

            var QuoteDiscountHandler = new QuoteDiscountHandler(orgService, orgTracing);
            Entity quote = QuoteDiscountHandler.SetQuoteTotalDiscountAmount(QuoteDiscountCollection.Entities[1], "Delete");
            #endregion

            #region 3. Verify
            Assert.AreEqual(QuoteDiscountCollection.Entities[1].GetAttributeValue<Money>("gsc_discountamount").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("totaldiscountamount").Value);
            #endregion

        }
        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 4/25/16
        #region PopulateCustomerInformation

        #region Test Scenario: Customer is Contact Record
        [TestMethod]

        public void ContactInformation()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Contact EntityCollection
            var ContactCollection = new EntityCollection
            {
                EntityName = "contact",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "contact",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_countryid", new EntityReference("country",new Guid("60BA7F89-C338-E411-9417-08002731E536"))
                             { Name = "Philippines"}},
                            {"gsc_provinceid", new EntityReference("province",new Guid("60BA7F89-C338-E411-9417-08002731E536"))
                             { Name = "Manila"}},
                            {"gsc_cityid", new EntityReference("city",new Guid("60BA7F89-C338-E411-9417-08002731E536"))
                             { Name = "Metro Manila"}},
                            {"address1_line1", "Street 1"},
                            {"address1_postalcode", "1234"},
                            {"mobilephone", "0912"},
                            {"telephone1", "0905"},
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
                        Id = Guid.NewGuid(),
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"customerid", new EntityReference(ContactCollection.EntityName, ContactCollection.Entities[0].Id)},
                            {"gsc_address", ""},
                            {"gsc_contactno", ""},
                            {"gsc_alternatecontactno", ""},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ContactCollection.EntityName)
                ))).Returns(ContactCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(QuoteCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == QuoteCollection.EntityName)))).Callback<Entity>(s => QuoteCollection.Entities[0] = s);

            #endregion

            #region 2. Call/Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity updatedQuote = QuoteHandler.PopulateCustomerInformation(QuoteCollection.Entities[0], "Update");
            #endregion

            #region 3. Verify

            var country = ContactCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_countryid").Name;

            var province = ContactCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_provinceid").Name;

            var city = ContactCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cityid").Name;

            var street = ContactCollection.Entities[0].GetAttributeValue<String>("address1_line1");

            var zipcode = ContactCollection.Entities[0].GetAttributeValue<String>("address1_postalcode");

            var address = street + " " + city + " " + province + " " + country + " " + zipcode;

            Assert.AreEqual(address, QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_address"));
            Assert.AreEqual(ContactCollection.Entities[0].GetAttributeValue<String>("mobilephone"), QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_contactno"));
            Assert.AreEqual(ContactCollection.Entities[0].GetAttributeValue<String>("telephone1"), QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_alternatecontactno"));
            #endregion
        }

        #endregion

        #region Test Scenario: Customer is Account Record
        [TestMethod]

        public void AccountInformation()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Account EntityCollection
            var accountCollection = new EntityCollection
            {
                EntityName = "account",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "account",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_countryid", new EntityReference("country",new Guid("60BA7F89-C338-E411-9417-08002731E536"))
                             { Name = "Philippines"}},
                            {"gsc_provinceid", new EntityReference("province",new Guid("60BA7F89-C338-E411-9417-08002731E536"))
                             { Name = "Manila"}},
                            {"gsc_cityid", new EntityReference("city",new Guid("60BA7F89-C338-E411-9417-08002731E536"))
                             { Name = "Metro Manila"}},
                            {"address1_line1", "Street 1"},
                            {"address1_postalcode", "1234"},
                            {"telephone1", "0905"},
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
                        Id = Guid.NewGuid(),
                        LogicalName = "quote",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"customerid", new EntityReference(accountCollection.EntityName, accountCollection.Entities[0].Id)},
                            {"gsc_address", ""},
                            {"gsc_contactno", ""},
                            {"gsc_alternatecontactno", ""},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == accountCollection.EntityName)
                ))).Returns(accountCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(QuoteCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == QuoteCollection.EntityName)))).Callback<Entity>(s => QuoteCollection.Entities[0] = s);

            #endregion

            #region 2. Call/Action
            var QuoteHandler = new QuoteHandler(orgService, orgTracing);
            Entity updatedQuote = QuoteHandler.PopulateCustomerInformation(QuoteCollection.Entities[0], "Update");
            #endregion

            #region 3. Verify

            var country = accountCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_countryid").Name;

            var province = accountCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_provinceid").Name;

            var city = accountCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cityid").Name;

            var street = accountCollection.Entities[0].GetAttributeValue<String>("address1_line1");

            var zipcode = accountCollection.Entities[0].GetAttributeValue<String>("address1_postalcode");

            var address = street + " " + city + " " + province + " " + country + " " + zipcode;

            Assert.AreEqual(address, QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_address"));
            Assert.AreEqual(accountCollection.Entities[0].GetAttributeValue<String>("telephone1"), QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_contactno"));
            Assert.AreEqual("", QuoteCollection.Entities[0].GetAttributeValue<String>("gsc_alternatecontactno"));
            #endregion
        }

        #endregion

        #endregion

        //Created By:Raphael Herrera, Created On: 4/26/2016
        #region Replicate Insurance Information
        #region Test Scenario: Replicate insurance information to quote

        [TestMethod]
        public void ReplicateInsuranceInformationUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;


            #region Vehicle Type Entity Collection
            var VehicleTypeCollection = new EntityCollection
            {
                EntityName = "vehicletype",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "vehicletype"
                    }
                }
            };
            #endregion

            #region Quote Entity Collection
            var QuoteCollection = new EntityCollection
            {
                EntityName = "quote",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "quote",
                      
                        Attributes = new AttributeCollection
                        {
                              {"gsc_vehicleuse", null},
                              {"gsc_vehicletype", null},
                              {"gsc_totalpremium", null},
                              {"gsc_originaltotalpremium", null},
                              {"gsc_insuranceid", new EntityReference("gsc_cmn_insurance", Guid.NewGuid())}  
                        }
                    }
                }
            };
            #endregion

            #region Insurance Entity Collection
            var InsuranceCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_insurance",
                Entities =
                {
                    new Entity
                    {
                        Id =QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_insuranceid").Id,
                        LogicalName = "quote",
                      
                        Attributes = new AttributeCollection
                        {
                              {"gsc_vehicleuse", new OptionSetValue(10000)},
                              {"gsc_vehicletypeid",  new EntityReference("vehicletype", VehicleTypeCollection.Entities[0].Id)},
                              {"gsc_totalpremium",  new Money(10000)},
                        }
                    }
                }
            };
            #endregion

            #region Insurance Covereage Entity Collection
            var InsuranceCoverageCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_insurancecoverage",
                Entities =
                {
                     new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_insurancecoverage",
                      
                        Attributes = new AttributeCollection
                        {
                              {"gsc_insurancecoveragepn", "test"},
                              {"gsc_suminsured",  new Money(50000)},
                              {"gsc_premium",  new Money(100000)},
                              {"gsc_insuranceid",  new EntityReference("gsc_cmn_insurance", InsuranceCollection.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            #region Quote Coverage Entity Collection
            var QuoteCoverageCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_quotecoverageavailable",
                Entities =
                {
                  
                }
            };
            #endregion


            orgServiceMock.Setup((service => service.RetrieveMultiple(
               It.Is<QueryExpression>(expression => expression.EntityName == InsuranceCollection.EntityName)
               ))).Returns(InsuranceCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == InsuranceCoverageCollection.EntityName)
              ))).Returns(InsuranceCoverageCollection);

            orgServiceMock.Setup(service => service.Create(It.Is<Entity>(entity => entity.LogicalName == QuoteCoverageCollection.EntityName)));
            #endregion

            #region 2. Call/Action
            QuoteHandler qh = new QuoteHandler(orgService, orgTracing);
            EntityCollection unitTestEC = qh.ReplicateInsuranceInformation(QuoteCollection.Entities[0]);
            #endregion

            #region 3. Verify
            //Verification for replicate of insurance fields
            Assert.AreEqual(InsuranceCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_vehicleuse"), QuoteCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_vehicleuse"));
            Assert.AreEqual(InsuranceCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehicletypeid").Id, QuoteCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehicletype").Id);
            Assert.AreEqual(InsuranceCollection.Entities[0].GetAttributeValue<Money>("gsc_totalpremium").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_totalpremium").Value);
            Assert.AreEqual(InsuranceCollection.Entities[0].GetAttributeValue<Money>("gsc_totalpremium").Value, QuoteCollection.Entities[0].GetAttributeValue<Money>("gsc_originaltotalpremium").Value);
            
            //Verification for creation of Quote Coverage
            Assert.AreEqual(InsuranceCoverageCollection.Entities[0].GetAttributeValue<string>("gsc_insurancecoveragepn"), unitTestEC.Entities[0].GetAttributeValue<string>("gsc_quotecoverageavailablepn"));
            Assert.AreEqual(InsuranceCoverageCollection.Entities[0].GetAttributeValue<Money>("gsc_suminsured").Value, unitTestEC.Entities[0].GetAttributeValue<Money>("gsc_suminsured").Value);
            Assert.AreEqual(InsuranceCoverageCollection.Entities[0].GetAttributeValue<Money>("gsc_premium").Value, unitTestEC.Entities[0].GetAttributeValue<Money>("gsc_premium").Value);
            Assert.AreEqual(QuoteCollection.Entities[0].Id, unitTestEC.Entities[0].GetAttributeValue<EntityReference>("gsc_quoteid").Id);
            #endregion
        }

        #endregion
      
        #endregion

    }
}
