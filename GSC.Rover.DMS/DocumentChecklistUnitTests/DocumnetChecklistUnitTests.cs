using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.DocumentChecklist;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace DocumentChecklistUnitTests
{
    [TestClass]
    public class DocumnetChecklistUnitTests
    {
        //Created By: Leslie G. Baliguat, Created On: 3/2/2016
        #region ReplicateDocumentInfo

        #region Test Scenario: Replicate Document Info to Document Checklist
        [TestMethod]

        public void ReplicateDocumentInfo()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            
            #region Document EntityCollection
            var Document = new EntityCollection()
            {
                EntityName = "gsc_sls_document",
                Entities =
                {
                    new Entity()
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_document",
                        Attributes = 
                        {
                            {"gsc_documentpn", "Sample Document"},
                            {"gsc_documenttype", true}
                        },
                        FormattedValues = 
                        {
                            {"gsc_documenttype", "Financing"}
                        }
                    }
                }
            };
            #endregion

            #region Document Checklist Entity
            var DocumentChecklist = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_sls_documentchecklist",
                Attributes = 
                {
                    {"gsc_documentid", new EntityReference(Document.EntityName, Document.Entities[0].Id)},
                    {"gsc_documentchecklistpn", ""},
                    {"gsc_documenttype", ""}
                }
            };
            #endregion
            


            orgServiceMock.Setup((service => service.RetrieveMultiple(
               It.Is<QueryExpression>(expression => expression.EntityName == Document.EntityName)
               ))).Returns(Document);

            #endregion

            #region 2. Call / Action
            var DocumentChecklistHandler = new DocumentChecklistHandler(orgService, orgTracing);
            Entity UpdatedDocumentChecklist = DocumentChecklistHandler.ReplicateDocumentInfo(DocumentChecklist, "Create");
            #endregion

            #region 3. Verfiy
            Assert.AreEqual(Document.Entities[0]["gsc_documentpn"], UpdatedDocumentChecklist["gsc_documentchecklistpn"]);
            Assert.AreEqual(Document.Entities[0].GetAttributeValue<Boolean>("gsc_documenttype"), UpdatedDocumentChecklist.GetAttributeValue<Boolean>("gsc_documenttype"));
            #endregion
        }
        #endregion

        #region Test Scenario: Replicate Document Info to Document Checklist on Update
        [TestMethod]

        public void ReplicateDocumentInfoonUpdate()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Document EntityCollection
            var Document = new EntityCollection()
            {
                EntityName = "gsc_sls_document",
                Entities =
                {
                    new Entity()
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_document",
                        Attributes = 
                        {
                            {"gsc_documentpn", "Sample Document"},
                            {"gsc_documenttype", true}
                        }
                    },
                    new Entity()
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_document",
                        Attributes = 
                        {
                            {"gsc_documentpn", "Financing Document"},
                            {"gsc_documenttype", true}
                        }
                    }
                }
            };
            #endregion

            #region Document Checklist Entity
            var DocumentChecklist = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_sls_documentchecklist",
                Attributes = 
                {
                    {"gsc_documentid", new EntityReference(Document.EntityName, Document.Entities[0].Id)},
                    {"gsc_documentchecklistpn", ""},
                    {"gsc_documenttype", ""}
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
               It.Is<QueryExpression>(expression => expression.EntityName == Document.EntityName)
               ))).Returns(Document);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(DocumentChecklist);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == DocumentChecklist.LogicalName)))).Callback<Entity>(s => DocumentChecklist = s);


            #endregion

            #region 2. Call / Action
            var DocumentChecklistHandler = new DocumentChecklistHandler(orgService, orgTracing);
            Entity UpdatedDocumentChecklist = DocumentChecklistHandler.ReplicateDocumentInfo(DocumentChecklist, "Update");
            #endregion

            #region 3. Verfiy
            Assert.AreEqual(DocumentChecklist["gsc_documentchecklistpn"], UpdatedDocumentChecklist["gsc_documentchecklistpn"]);
            Assert.AreEqual(DocumentChecklist.GetAttributeValue<Boolean>("gsc_documenttype"), UpdatedDocumentChecklist.GetAttributeValue<Boolean>("gsc_documenttype"));
            #endregion
        }
        #endregion

        #endregion
    }
}
