using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using GSC.Rover.DMS.BusinessLogic.Common;
using System.Xml;

namespace GSC.Rover.DMS.BusinessLogic.Invitation
{
    public class InvitationHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public InvitationHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 10/13/2016
        /*Purpose: Send employee credentials via email
         * Registration Details: 
         * Event/Message:
         *      Invitation workflow trigger
         * Primary Entity: Invitation
         */
        public Entity SendEmployeeCredentials(Entity invitationEntity)
        {
            _tracingService.Trace("Started SendEmployeeCredentials method..");

            EntityReferenceCollection contactCollection = new EntityReferenceCollection();

            if (invitationEntity.FormattedValues["adx_type"].Equals("Single"))
            {
                Guid contactId = invitationEntity.GetAttributeValue<EntityReference>("adx_invitecontact") != null
                    ? invitationEntity.GetAttributeValue<EntityReference>("adx_invitecontact").Id
                    : Guid.Empty;

                EntityCollection contactRecords = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", contactId, _organizationService, null, OrderType.Ascending,
                    new[] { "adx_identity_username", "adx_password", "adx_logonenabled", "lastname", "firstname", "fullname" });
                
                if (contactRecords != null && contactRecords.Entities.Count > 0)
                {
                    Entity contact = contactRecords.Entities[0];
                    
                    EmailSender(invitationEntity, contact);

                    contact["adx_password"] = String.Empty;

                    _organizationService.Update(contact);

                    contactCollection.Add(new EntityReference("contact", contact.Id));

                    AssignWebRole(contactCollection, invitationEntity);
                }
            }
            else if (invitationEntity.FormattedValues["adx_type"].Equals("Group"))
            {
                //Retrieve invite contacts
                EntityCollection relatedContactRecords = CommonHandler.RetrieveRecordsByOneValue("adx_invitation_invitecontacts", "adx_invitationid", invitationEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "contactid" });

                if (relatedContactRecords != null && relatedContactRecords.Entities.Count > 0)
                {
                    foreach (Entity relatedContact in relatedContactRecords.Entities)
                    {
                        Guid contactId = relatedContact.Contains("contactid")
                            ? relatedContact.GetAttributeValue<Guid>("contactid")
                            : Guid.Empty;

                        Entity contact = _organizationService.Retrieve("contact", contactId, new ColumnSet("adx_identity_username", "adx_password", "adx_logonenabled", "lastname", "firstname", "fullname"));

                        EmailSender(invitationEntity, contact);

                        contact["adx_password"] = String.Empty;

                        _organizationService.Update(contact);

                        contactCollection.Add(new EntityReference("contact", contact.Id));                        
                    }

                    AssignWebRole(contactCollection, invitationEntity);
                }
            }

            _tracingService.Trace("Ended SendEmployeeCredentials method..");
            return invitationEntity;
        }

        private void AssignWebRole(EntityReferenceCollection contactCollection, Entity invitationEntity)
        {
            _tracingService.Trace("Started AssignWebRole method..");

            EntityCollection invitationWebRoleRecords = CommonHandler.RetrieveRecordsByOneValue("adx_invitation_webrole", "adx_invitationid", invitationEntity.Id, _organizationService, null, OrderType.Ascending, 
                new[] { "adx_webroleid" });

            if (invitationWebRoleRecords != null && invitationWebRoleRecords.Entities.Count > 0)
            {
                foreach (Entity invitationWebRole in invitationWebRoleRecords.Entities)
                {                    
                    Guid webRoleId = invitationWebRole.GetAttributeValue<Guid>("adx_webroleid");
                    
                    _organizationService.Associate("adx_webrole", webRoleId, new Relationship("adx_webrole_contact"), contactCollection);
                }
            }

            _tracingService.Trace("Ended AssignWebRole method..");
        }

        private void EmailSender(Entity invitationEntity, Entity contactEntity)
        {
            _tracingService.Trace("Started EmailSender method..");
            Guid systemUserId = invitationEntity.GetAttributeValue<EntityReference>("ownerid") != null
                ? invitationEntity.GetAttributeValue<EntityReference>("ownerid").Id
                : Guid.Empty;

            String username = contactEntity.Contains("adx_identity_username")
                ? contactEntity.GetAttributeValue<String>("adx_identity_username")
                : String.Empty;

            String password = contactEntity.Contains("adx_password")
                ? contactEntity.GetAttributeValue<String>("adx_password")
                : String.Empty;

            String fullname = contactEntity.Contains("fullname")
                ? contactEntity.GetAttributeValue<String>("fullname")
                : String.Empty;

            //Retrieve email sender user record
            EntityCollection systemUserRecords = CommonHandler.RetrieveRecordsByOneValue("systemuser", "systemuserid", systemUserId, _organizationService, null, OrderType.Ascending,
                new[] { "internalemailaddress" });

            if (systemUserRecords != null && systemUserRecords.Entities.Count > 0)
            {
                //Retrieve email template
                EntityCollection templateRecords = CommonHandler.RetrieveRecordsByOneValue("template", "title", "Send Employee Credentials", _organizationService, null, OrderType.Ascending,
                    new[] { "subjectpresentationxml", "presentationxml" });

                _tracingService.Trace("Template records fetched : " + templateRecords.Entities.Count);

                if (templateRecords != null && templateRecords.Entities.Count > 0)
                {
                    Entity template = templateRecords.Entities[0];

                    XmlDocument subjectXml = new XmlDocument();
                    subjectXml.LoadXml(template.GetAttributeValue<String>("subjectpresentationxml"));

                    XmlDocument bodyXml = new XmlDocument();
                    bodyXml.LoadXml(template.GetAttributeValue<String>("presentationxml"));

                    #region Construct email body
                    Int32 fullnameIndex = bodyXml.DocumentElement.InnerText.IndexOf("{Fullname}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Insert(fullnameIndex, fullname);

                    Int32 usernameIndex = bodyXml.DocumentElement.InnerText.IndexOf("{Username}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Insert(usernameIndex, username);

                    Int32 passwordIndex = bodyXml.DocumentElement.InnerText.IndexOf("{Password}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Insert(passwordIndex, password);

                    fullnameIndex = bodyXml.DocumentElement.InnerText.IndexOf("{Fullname}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Remove(fullnameIndex, 10);

                    usernameIndex = bodyXml.DocumentElement.InnerText.IndexOf("{Username}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Remove(usernameIndex, 10);

                    passwordIndex = bodyXml.DocumentElement.InnerText.IndexOf("{Password}");
                    bodyXml.DocumentElement.InnerText = bodyXml.DocumentElement.InnerText.Remove(passwordIndex, 10);
                    #endregion

                    Entity systemUser = systemUserRecords.Entities[0];

                    Entity from = new Entity("activityparty");
                    from["partyid"] = new EntityReference(systemUser.LogicalName, systemUser.Id);

                    EntityCollection contactRecords = new EntityCollection();
                    contactRecords.EntityName = "activityparty";

                    Entity to = new Entity("activityparty");
                    to["partyid"] = new EntityReference(contactEntity.LogicalName, contactEntity.Id);

                    Entity email = new Entity("email");
                    email["from"] = new Entity[] { from };
                    email["to"] = new Entity[] { to };
                    email["subject"] = subjectXml.DocumentElement.InnerText;
                    email["description"] = bodyXml.DocumentElement.InnerText;
                    Guid emailId = _organizationService.Create(email);

                    SendEmailRequest sendEmailRequest = new SendEmailRequest();
                    sendEmailRequest.EmailId = emailId;
                    sendEmailRequest.IssueSend = true;
                    sendEmailRequest.TrackingToken = "";
                    SendEmailResponse sendEmailResponse = (SendEmailResponse)_organizationService.Execute(sendEmailRequest);
                }
            }

        }
    }
}
