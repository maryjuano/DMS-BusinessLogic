using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GSC.Rover.DMS.BusinessLogic.IDRequest;
using Microsoft.Xrm.Sdk;

namespace IDRequestUnitTests
{
    [TestClass]
    public class IDRequestHandlerUnitTests
    {
        //Created By: Leslie Baliguat, Created On: 8/2/2016
        #region GenerateName
        [TestMethod]

        public void TestMethod1()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Id Request Entity
            var Request = new Entity()
            {
                Id = Guid.NewGuid(),
                LogicalName = "gsc_cmn_idrequest",
                Attributes =
                {
                    {"gsc_branch", new EntityReference("account", new Guid("b360c58f-c7f8-4b37-af9c-7752d3e4740d"))
                        { Name = "Citimotors"}},
                    {"gsc_originatingrecordtype", "Quote"},
                    {"gsc_originatingrecordid", "Quote123"},
                    {"gsc_idrequestpn", ""}
                }
            };
            #endregion

            #endregion

            #region 2. Call/Action

            var IDRequestHandler = new IDRequestHandler(orgService, orgTracing);
            IDRequestHandler.GenerateName(Request);

            #endregion

            #region 3. Verify

            var branch = Request.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? Request.GetAttributeValue<EntityReference>("gsc_branchid").Name
                : String.Empty;
            var recordId = Request.Contains("gsc_originatingrecordid")
                ? Request.GetAttributeValue<String>("gsc_originatingrecordid")
                : String.Empty;
            var recordType = Request.Contains("gsc_originatingrecordtype")
                ? Request.GetAttributeValue<String>("gsc_originatingrecordtype")
                : String.Empty;

            var name = branch + "-" + recordId + "-" + recordType;

            Assert.AreEqual(name, Request.GetAttributeValue<String>("gsc_idrequestpn"));
            #endregion

        }

        #endregion
    }
}
