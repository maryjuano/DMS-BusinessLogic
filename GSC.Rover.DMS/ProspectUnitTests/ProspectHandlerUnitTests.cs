using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.Prospect;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;


using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace ProspectUnitTests
{
    [TestClass]
    public class ProspectHandlerUnitTests
    {
        #region Prospect - Check for existing records

        #region Test Scenario : Check if prospect created has a match in contacts that is tagged as a fraudulent accout

        [TestMethod]
        public void CheckForExistingRecords()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Prospect EntityCollection
            var prospectCollection = new EntityCollection
            {
                EntityName = "gsc_sls_prospect",
                Entities =
                {
                    new Entity
                    {
                         Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_prospect",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_firstname", "testfirst"},
                            {"gsc_lastname", "testlast"},
                            {"gsc_mobileno", "09999999999"},
                            {"gsc_birthday", new DateTime(1990, 1, 1)}
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
                         Id = Guid.NewGuid(),
                        LogicalName = "contact",
                        
                        Attributes = new AttributeCollection
                        {
                            {"contactid", "2"},
                            {"firstname", "testfirst"},
                            {"lastname", "testlast"},
                            {"mobileno", "09999999999"},
                            {"birthday", new DateTime(1990, 1, 1)},
                            {"gsc_fraud", true}
                        }
                    },
                     new Entity
                    {
                         Id = Guid.NewGuid(),
                        LogicalName = "contact",
                        
                        Attributes = new AttributeCollection
                        {
                            {"contactid", "3"},
                            {"firstname", ""},
                            {"lastname", ""},
                            {"mobileno", ""},
                            {"birthday", ""},
                            {"gsc_fraud", false}
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

            var prospectHandler = new ProspectHandler(orgService, orgTracing);
            bool res = prospectHandler.CheckForExistingRecords(prospectCollection[0]);
            
            #endregion

            #region 3. Verify
            Assert.AreEqual(true, res);
            #endregion

        }


        #endregion
    }
        #endregion
   
}

       


