using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.ReceivingTransaction;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace ReceivingTransactionUnitTests
{
    [TestClass]
    public class ReceivingTransactionHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 7/12/2016
        #region Cancel Receiving Transaction
        
        #region Test Scenario : MMPC Status is 'Inactive'
        [TestMethod]
        public void CancelReceivingTransaction()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Purchase Order EntityCollection
            var PurchaseOrderCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_purchaseorder",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_purchaseorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vpostatus", new OptionSetValue(100000002)}
                        }
                    }
                }
            };
            #endregion

            #region Receiving Transaction EntityCollection
            var ReceivingTransactionCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_receivingtransaction",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_receivingtransaction",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_mmpcstatus", "Inactive"},
                            {"gsc_vpostatus", "In-Transit"},
                            {"gsc_purchaseorderid", PurchaseOrderCollection.Entities[0].Id}
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
                        LogicalName = "gsc_iv_productquantiy",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_onhand", (Int32)2},
                            {"gsc_available", (Int32)2},
                            {"gsc_onorder", (Int32)11}
                        }
                    }
                }
            };
            #endregion

            #region Inventory EntityCollection
            var InventoryCollection = new EntityCollection
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
                            {"gsc_productquantityid", Guid.NewGuid()}
                        }
                    }
                }
            };
            #endregion

            #region Receiving Transaction Detail EntityCollection
            var ReceivingTransactionDetailCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_receivingtransactiondetail",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_receivingtransactiondetail",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_receivingtransactionid", ReceivingTransactionCollection.Entities[0].Id},
                            {"gsc_inventoryid", InventoryCollection.Entities[0].Id}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProductQuantityCollection.EntityName)
                ))).Returns(ProductQuantityCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ReceivingTransactionCollection.EntityName)
                ))).Returns(ReceivingTransactionCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == InventoryCollection.EntityName)
                ))).Returns(InventoryCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ReceivingTransactionDetailCollection.EntityName)
                ))).Returns(ReceivingTransactionDetailCollection);

            #endregion

            #region 2. Call / Action
            var ReceivingTransactionHandler = new ReceivingTransactionHandler(orgService, orgTracing);
            Entity receivingTransaction = ReceivingTransactionHandler.CancelReceivingTransaction(ReceivingTransactionCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual("","");
            #endregion
        }
	    #endregion

        #endregion

        //Created By : Jerome Anthony Gerero, Created On : 7/13/2016
        #region Replicate Purchase Order to Receiving Transaction
        
        #region Test Scenario : Purchase Order ID field contains data
        [TestMethod]
        public void ReplicatePurchaseOrderFields()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Purchase Order EntityCollection
            var PurchaseOrderCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_purchaseorder",
                Entities = 
                { 
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_purchaseorder",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {                            
                            {"gsc_vendorid", new EntityReference("gsc_cmn_vendor", Guid.NewGuid())},
                            {"gsc_vendorname", "PO1234"},
                            {"gsc_siteid", new EntityReference("gsc_iv_site", Guid.NewGuid())},
                            {"gsc_vpostatus", new OptionSetValue(0)},
                            {"gsc_mmpcstatus", new OptionSetValue(0)}
                        },
                        FormattedValues =
                        {
                            {"gsc_vpostatus", "Open"},
                            {"gsc_mmpcstatus", "Invoice Not Pulled Out"}   
                        }
                    }
                }
            };
            #endregion

            #region Receiving Transaction EntityCollection
            var ReceivingTransactionCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_receivingtransaction",
                Entities = 
                { 
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_cmn_receivingtransaction",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_purchaseorderid", new EntityReference("gsc_cmn_purchaseorder", PurchaseOrderCollection.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == PurchaseOrderCollection.EntityName)
                ))).Returns(PurchaseOrderCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ReceivingTransactionCollection.EntityName)
                ))).Returns(ReceivingTransactionCollection);

            #endregion

            #region 2. Call / Action
            var ReceivingTransactionHandler = new ReceivingTransactionHandler(orgService, orgTracing);
            Entity receivingTransaction = ReceivingTransactionHandler.ReplicatePurchaseOrderFields(ReceivingTransactionCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(PurchaseOrderCollection.Entities[0].FormattedValues["gsc_vpostatus"], receivingTransaction.GetAttributeValue<String>("gsc_vpostatus"));
            Assert.AreEqual(PurchaseOrderCollection.Entities[0].FormattedValues["gsc_mmpcstatus"], receivingTransaction.GetAttributeValue<String>("gsc_mmpcstatus"));
            Assert.AreEqual(PurchaseOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_vendorid").Id, receivingTransaction.GetAttributeValue<EntityReference>("gsc_vendorid").Id);
            Assert.AreEqual(PurchaseOrderCollection.Entities[0].GetAttributeValue<String>("gsc_vendorname"), receivingTransaction.GetAttributeValue<String>("gsc_vendorname"));
            Assert.AreEqual(PurchaseOrderCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_siteid").Id, receivingTransaction.GetAttributeValue<EntityReference>("gsc_siteid").Id);
            #endregion
        }
        #endregion
        
        #endregion
    }
}
