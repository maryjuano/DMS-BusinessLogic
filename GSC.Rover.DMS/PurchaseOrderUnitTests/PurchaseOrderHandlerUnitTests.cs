using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.PurchaseOrder;
using Microsoft.Xrm.Sdk;
using Moq;
using Microsoft.Xrm.Sdk.Query;

namespace PurchaseOrderUnitTests
{
    [TestClass]
    public class PurchaseOrderHandlerUnitTests
    {
        [TestMethod]
        public void PopulateVendorDetailsUnitTest()
        {
            #region 1: Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            var PurchaseOrderCollection = new EntityCollection
            {
                EntityName = "purchaseorder",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_svc_purchaseorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vendorid", new EntityReference("account", Guid.NewGuid())},
                           
                        }
                    }
                }
            };

            var VendorCollection = new EntityCollection
            {
                EntityName = "account",
                Entities = 
                {
                    new Entity
                    {
                        Id = PurchaseOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vendorid").Id,
                        LogicalName = "account",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_cityid", new EntityReference("gsc_syscity", Guid.NewGuid())
                            { Name = "Manila"}},
                            {"gsc_provinceid", new EntityReference("gsc_sysprovince", Guid.NewGuid())
                            { Name = "Metro Manila"}},
                            {"gsc_countryid", new EntityReference("gsc_syscountry", Guid.NewGuid())
                            { Name = "Philippines"}},
                            {"gsc_street", "Masangkay St."},
                            {"gsc_zipcode", "1234"},
                            {"gsc_phone", "1234567"}
                        }
                    }
                }
            };

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == PurchaseOrderCollection.EntityName)
                ))).Returns(PurchaseOrderCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == VendorCollection.EntityName)
                ))).Returns(VendorCollection);


            #endregion  

            #region 2: Act
            var purchaseOrderHandler = new PurchaseOrderHandler(orgService, orgTracing);
            Entity purchaseOrder = purchaseOrderHandler.PopulateVendorDetails(PurchaseOrderCollection.Entities[0]);

            #endregion

            #region 3: Assert
            Assert.AreEqual(VendorCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cityid").Id,
                purchaseOrder.GetAttributeValue<EntityReference>("gsc_cityid").Id);
            Assert.AreEqual(VendorCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_provinceid").Id,
                purchaseOrder.GetAttributeValue<EntityReference>("gsc_provincestateid").Id);
            Assert.AreEqual(VendorCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_countryid").Id,
                purchaseOrder.GetAttributeValue<EntityReference>("gsc_countryid").Id);
            Assert.AreEqual(VendorCollection.Entities[0].GetAttributeValue<String>("gsc_street"),
                purchaseOrder.GetAttributeValue<String>("gsc_street"));
            Assert.AreEqual(VendorCollection.Entities[0].GetAttributeValue<String>("gsc_zipcode"),
                purchaseOrder.GetAttributeValue<String>("gsc_zipcode"));
            Assert.AreEqual(VendorCollection.Entities[0].GetAttributeValue<String>("gsc_phone"),
                purchaseOrder.GetAttributeValue<String>("gsc_contactno"));
            
            #endregion
        }

        [TestMethod]
        public void PopulateShipToDetailsUnitTest()
        {
            #region 1: Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            var PurchaseOrderCollection = new EntityCollection
            {
                EntityName = "purchaseorder",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_svc_purchaseorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_branchcodeid", new EntityReference("account", Guid.NewGuid())},
                           
                        }
                    }
                }
            };

            var BranchCollection = new EntityCollection
            {
                EntityName = "account",
                Entities = 
                {
                    new Entity
                    {
                        Id = PurchaseOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_branchcodeid").Id,
                        LogicalName = "account",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_cityid", new EntityReference("gsc_syscity", Guid.NewGuid())
                            { Name = "Manila"}},
                            {"gsc_provinceid", new EntityReference("gsc_sysprovince", Guid.NewGuid())
                            { Name = "Metro Manila"}},
                            {"gsc_countryid", new EntityReference("gsc_syscountry", Guid.NewGuid())
                            { Name = "Philippines"}},
                            {"address1_line1", "Masangkay St."},
                            {"address1_postalcode", "1234"},
                            {"telephone1", "1234567"},
                            {"name", "Masangkay Branch"}
                        }
                    }
                }
            };

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == PurchaseOrderCollection.EntityName)
                ))).Returns(PurchaseOrderCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == BranchCollection.EntityName)
                ))).Returns(BranchCollection);


            #endregion

            #region 2: Act
            var purchaseOrderHandler = new PurchaseOrderHandler(orgService, orgTracing);
            Entity purchaseOrder = purchaseOrderHandler.PopulateShipDetails(PurchaseOrderCollection.Entities[0]);

            #endregion

            #region 3: Assert
            Assert.AreEqual(BranchCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cityid").Id,
                purchaseOrder.GetAttributeValue<EntityReference>("gsc_shiptocityid").Id);
            Assert.AreEqual(BranchCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_provinceid").Id,
                purchaseOrder.GetAttributeValue<EntityReference>("gsc_shiptoprovincestateid").Id);
            Assert.AreEqual(BranchCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_countryid").Id,
                purchaseOrder.GetAttributeValue<EntityReference>("gsc_shiptocountry").Id);
            Assert.AreEqual(BranchCollection.Entities[0].GetAttributeValue<String>("address1_line1"),
                purchaseOrder.GetAttributeValue<String>("gsc_shiptostreet"));
            Assert.AreEqual(BranchCollection.Entities[0].GetAttributeValue<String>("address1_postalcode"),
                purchaseOrder.GetAttributeValue<String>("gsc_shiptozipcode"));
            Assert.AreEqual(BranchCollection.Entities[0].GetAttributeValue<String>("telephone1"),
                purchaseOrder.GetAttributeValue<String>("gsc_shiptocontactno"));
            Assert.AreEqual(BranchCollection.Entities[0].GetAttributeValue<String>("name"),
                purchaseOrder.GetAttributeValue<String>("gsc_branchname"));

            
            #endregion
        }

        //Created By : Jefferson Cordero, Created On : 5/2/2016
        [TestMethod]
        public void deactivatePOTest()
        {
            #region Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            OptionSetValue val = new OptionSetValue(100000002);
            OptionSetValue status = new OptionSetValue(0);

            var PurchaseOrderCollection = new EntityCollection
            {
                EntityName = "purchaseorder",
                Entities = 
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_svc_purchaseorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_postatus", val},
                            {"statecode", status}
                        }
                    }
                }
            };

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == PurchaseOrderCollection.EntityName)
                ))).Returns(PurchaseOrderCollection);
            #endregion

            #region Act
            var purchaseOrderHandler = new PurchaseOrderHandler(orgService, orgTracing);
            Entity purchaseOrder = purchaseOrderHandler.DeactivatePurchaseOrder(PurchaseOrderCollection.Entities[0]);

            #endregion

            #region Assert
            Assert.AreEqual(purchaseOrder.GetAttributeValue<OptionSetValue>("gsc_postatus").Value, 100000002);
            Assert.AreEqual(purchaseOrder.GetAttributeValue<OptionSetValue>("statecode").Value, 0);
            
            #endregion
        }
    }
}
