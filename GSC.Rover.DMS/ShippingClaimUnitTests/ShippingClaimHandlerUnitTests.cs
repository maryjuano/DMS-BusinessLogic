using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.ShippingClaim;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace ShippingClaimUnitTests
{
    [TestClass]
    public class ShippingClaimHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 8/10/2016
        #region Replicate Receiving Transaction

        #region Test Scenario : Receipt No field contains data
        [TestMethod]
        public void ReplicateReceivingTransactionFields()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Receiving Transacton EntityCollection
            var ReceivingTransactionCollection = new EntityCollection
            {
                EntityName = "gsc_cmn_receivingtransaction",
                Entities =
                { 
                    
                }
            };
            #endregion

            #endregion
        }
        #endregion

        #endregion
    }
}
