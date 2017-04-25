using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.Opportunity;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace OpportunityUnitTests
{
    [TestClass]
    public class OpportunityHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 2/2/2016
        #region Opportunity - Replicate Prospect Inquiry to Opportunity
        
        #region Test Scenario: Prospect Inquiry ID is provided
        
        [TestMethod]
        public void ReplicateProspectInquiryInfoUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesexecutiveid", new EntityReference("contact", Guid.NewGuid())
                            { Name = "Citimotors"}},
                            {"gsc_vehiclebasemodelid", new EntityReference("gsc_iv_vehiclebasemodel", Guid.NewGuid())
                            { Name = "Montero"}},
                            {"gsc_colorid", new EntityReference("gsc_iv_color", Guid.NewGuid())
                            { Name = "Jet Black"}},
                            {"gsc_leadsourceid", new EntityReference("gsc_sls_leadsource", Guid.NewGuid())
                            { Name = "Mall Display"}},
                            {"subject", "Montero Sport SUA Terrorista"},
                        }
                    }
                }
            };
            #endregion

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
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesexecutiveid", ""},
                            {"gsc_vehiclebasemodelid", ""},
                            {"gsc_colorid", ""},
                            {"gsc_leadsourceid", ""},
                            {"gsc_topic", ""},
                            {"originatingleadid", new EntityReference("lead", ProspectInquiryCollection.Entities[0].Id)},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProspectInquiryCollection.EntityName)
                ))).Returns(ProspectInquiryCollection);

            #endregion

            #region 2. Call/Action

            var OpportunityHandler = new OpportunityHandler(orgService, orgTracing);
            Entity opportunity = OpportunityHandler.ReplicateInquiryInfo(OpportunityCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id, opportunity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id);
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id, opportunity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id);
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_colorid").Id, opportunity.GetAttributeValue<EntityReference>("gsc_colorid").Id);
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_leadsourceid").Id, opportunity.GetAttributeValue<EntityReference>("gsc_leadsourceid").Id);
            Assert.AreEqual(ProspectInquiryCollection.Entities[0]["subject"], opportunity["gsc_topic"]);
            #endregion
        }
        
        #endregion

        #region Test Scenario: Prospect Inquiry ID is not provided
        
        [TestMethod]
        public void ReplicateNullProspectInquiryInfoUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesexecutiveid", new EntityReference("contact", Guid.NewGuid())
                            { Name = "Citimotors"}},
                            {"gsc_vehiclebasemodelid", new EntityReference("gsc_iv_vehiclebasemodel", Guid.NewGuid())
                            { Name = "Montero"}},
                            {"gsc_colorid", new EntityReference("gsc_iv_color", Guid.NewGuid())
                            { Name = "Jet Black"}},
                            {"gsc_leadsourceid", new EntityReference("gsc_sls_leadsource", Guid.NewGuid())
                            { Name = "Mall Display"}},
                            {"subject", "Montero Sport SUA Terrorista"},
                        }
                    }
                }
            };
            #endregion

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
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_salesexecutiveid", ""},
                            {"gsc_vehiclebasemodelid", ""},
                            {"gsc_colorid", ""},
                            {"gsc_leadsourceid", ""},
                            {"gsc_topic", ""},
                            {"originatingleadid", new EntityReference("lead", Guid.Empty)},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProspectInquiryCollection.EntityName)
                ))).Returns(ProspectInquiryCollection);

            #endregion

            #region 2. Call/Action

            var OpportunityHandler = new OpportunityHandler(orgService, orgTracing);
            Entity opportunity = OpportunityHandler.ReplicateInquiryInfo(OpportunityCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id, opportunity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id);
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id, opportunity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id);
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_colorid").Id, opportunity.GetAttributeValue<EntityReference>("gsc_colorid").Id);
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_leadsourceid").Id, opportunity.GetAttributeValue<EntityReference>("gsc_leadsourceid").Id);
            Assert.AreEqual(ProspectInquiryCollection.Entities[0]["subject"], opportunity["gsc_topic"]);
            #endregion
        }

        #endregion

        #endregion


        //Created By : Raphael Herrera, Created On : 4/19/2016
        #region Opportunity - Update customer once set as won
        #region Test Scenario: Update ispotential field
        [TestMethod]
        public void updateCustomer()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

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
                    
                        Attributes = new AttributeCollection
                        {
                            {"parentcontactid", new EntityReference("contact", Guid.NewGuid())},
                            {"parentaccountid", ""}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "opportunity",
                      
                        Attributes = new AttributeCollection
                        {
                            {"parentcontactid", new EntityReference("contact", Guid.NewGuid())},
                            {"parentaccountid", ""}
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
                        Id =  OpportunityCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid").Id,
                        LogicalName = "contact",
                        
                        Attributes = new AttributeCollection
                        {
                            {"gsc_ispotential", true},
                         
                        }
                    },
                     new Entity
                    {
                        Id =  OpportunityCollection.Entities[1].GetAttributeValue<EntityReference>("parentcontactid").Id,
                        LogicalName = "contact",
                        
                        Attributes = new AttributeCollection
                        {
                            {"gsc_ispotential", true},
                         
                        }
                    }
                }
            };
            #endregion
            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ContactCollection.EntityName)
                ))).Returns(ContactCollection);
            #endregion

            #region 2. Call/Action

            OpportunityHandler OpportunityHandler = new OpportunityHandler(orgService, orgTracing);
            OpportunityHandler.updateCustomer(OpportunityCollection.Entities[0], orgService, orgTracing);
            #endregion

            #region 3. Verify
            Assert.AreEqual(false, ContactCollection.Entities[0].GetAttributeValue<bool>("gsc_ispotential"));
            Assert.AreEqual(true, ContactCollection.Entities[1].GetAttributeValue<bool>("gsc_ispotential"));
            #endregion

        }

        #endregion
        #endregion

        //Created By : Raphael Herrera, Created On : 4/20/2016
        #region Opportunity - Link Customer to Lead
        #region Test Case - Update contact customer
        [TestMethod]
        public void linkContact()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

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
                            {"parentcontactid", new EntityReference("contact", Guid.NewGuid())},
                            {"parentaccountid",  new EntityReference("account", Guid.NewGuid())},
                            {"originatingleadid", new EntityReference("lead", Guid.NewGuid())},
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "opportunity",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"parentcontactid", new EntityReference("contact", Guid.NewGuid())},
                            {"parentaccountid", new EntityReference("account", Guid.NewGuid())},
                            {"originatingleadid", new EntityReference("lead", Guid.NewGuid())},

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
                        Id =  OpportunityCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid").Id,
                        LogicalName = "contact",
                    },
                     new Entity
                    {
                        Id =  OpportunityCollection.Entities[1].GetAttributeValue<EntityReference>("parentcontactid").Id,
                        LogicalName = "contact",
                    }
                }
            };
            #endregion

            #region Lead EntityCollection
            var LeadCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id =  OpportunityCollection.Entities[0].GetAttributeValue<EntityReference>("originatingleadid").Id,
                        LogicalName = "lead",
                        Attributes = new AttributeCollection
                        {
                            {"parentcontactid", new EntityReference("contact", Guid.NewGuid())},
                            {"parentaccountid", new EntityReference("contact", Guid.NewGuid())},
                            {"customerid", new EntityReference("customer", Guid.NewGuid())}
                        }
                    },
                     new Entity
                    {
                        Id =  OpportunityCollection.Entities[1].GetAttributeValue<EntityReference>("originatingleadid").Id,
                        LogicalName = "lead",
                        Attributes = new AttributeCollection
                        {
                            {"parentcontactid", new EntityReference("contact", Guid.NewGuid())},
                            {"parentaccountid", new EntityReference("contact", Guid.NewGuid())},
                            {"customerid", new EntityReference("customer", Guid.NewGuid())}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
              It.Is<QueryExpression>(expression => expression.EntityName == ContactCollection.EntityName)
              ))).Returns(ContactCollection);
            #endregion

            #region 2. Call/Action
            OpportunityHandler OpportunityHandler = new OpportunityHandler(orgService, orgTracing);
            OpportunityHandler.linkCustomer(OpportunityCollection.Entities[0], orgService, orgTracing);
            #endregion

            #region 3. Verify
            Assert.AreEqual(OpportunityCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid").Id, LeadCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid").Id);
            Assert.AreEqual(OpportunityCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid").Id, LeadCollection.Entities[0].GetAttributeValue<EntityReference>("customerid").Id);
          //  Assert.AreEqual(ContactCollection.Entities[0].Id, LeadCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid").Id);
            #endregion
        }
        #endregion
        #endregion
    }
}
