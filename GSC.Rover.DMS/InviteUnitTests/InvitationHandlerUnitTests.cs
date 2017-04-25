using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GSC.Rover.DMS.BusinessLogic.Invitation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace InviteUnitTests
{
    [TestClass]
    public class InvitationHandlerUnitTests
    {
        //Created By : Jerome Anthony Gerero, Created On : 10/14/2016
        #region Generate portal user credentials on new invite

        #region Test Scenario : User creates new invite record
        [TestMethod]
        public void GenerateCredentials()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Contact entity
            var ContactCollection = new EntityCollection()
            {
                EntityName = "contact",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "contact",
                        Attributes =
                        {
                            {"adx_identity_username", "tpearson-011"},
                            {"adx_password", ""},
                            {"adx_logonenabled", false},
                            {"lastname", "Pearson"},
                            {"firstname", "Tilian"}
                        }
                    },
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "contact",
                        Attributes =
                        {
                            {"adx_identity_username", "tpearson-012"},
                            {"adx_password", ""},
                            {"adx_logonenabled", false},
                            {"lastname", "Pearson"},
                            {"firstname", "Tilian"}
                        }
                    }
                }
            };
            #endregion

            #region Invitation entity
            var InvitationCollection = new EntityCollection()
            {
                EntityName = "adx_invitation",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "adx_invitation",
                        Attributes =
                        {
                            {"adx_invitecontact", new EntityReference("contact", ContactCollection.Entities[0].Id)}
                        },
                        FormattedValues =
                        {
                            {"adx_type", "Single"}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ContactCollection.EntityName)
                ))).Returns(ContactCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == InvitationCollection.EntityName)
                ))).Returns(InvitationCollection);

            #endregion

            #region 2. Call / Action
            var invitationHandler = new InvitationHandler(orgService, orgTracing);
            //Entity invitationEntity = invitationHandler.GenerateEmployeeCredentials(InvitationCollection.Entities[0]);
            #endregion

            #region 3. Verify
            
            #endregion
        }
        #endregion

        #endregion
    }
}
