using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;
using System;

namespace GSC.Rover.DMS.BusinessLogic.DocumentChecklist
{
    public class DocumentChecklistHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public DocumentChecklistHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Leslie Baliguat, Created On: 3/3/2016
        /*Purpose: Replicate Document Details to Document Checklist
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update: gsc_doucmentid
         * Primary Entity: Document Checklist
         */
        public Entity ReplicateDocumentInfo(Entity documentChecklistEntity, String message)
        {
            if (documentChecklistEntity.Contains("gsc_documentid") || documentChecklistEntity.GetAttributeValue<EntityReference>("gsc_documentid") != null)
            {
                _tracingService.Trace("Started ReplicateDocumentInfo method ...");

                var documentid = documentChecklistEntity.GetAttributeValue<EntityReference>("gsc_documentid").Id;

                //Retrieve Document Information
                EntityCollection DocumentRecord = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_document", "gsc_sls_documentid", documentid, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_documentpn", "gsc_documenttype" });

                if (DocumentRecord != null || DocumentRecord.Entities.Count > 0)
                {
                    _tracingService.Trace("Creating Document Checklist Record ...");

                    Entity Document = DocumentRecord.Entities[0];

                    if (message == "Create")
                    {
                        documentChecklistEntity["gsc_documentchecklistpn"] = Document["gsc_documentpn"];
                        documentChecklistEntity["gsc_documenttype"] = Document.GetAttributeValue<Boolean>("gsc_documenttype");
                    }

                    else if (message == "Update")
                    {
                        Entity documentChecklistToUpdate = _organizationService.Retrieve(documentChecklistEntity.LogicalName, documentChecklistEntity.Id, new ColumnSet("gsc_documentchecklistpn", "gsc_documenttype"));
                        documentChecklistToUpdate["gsc_documentchecklistpn"] = Document["gsc_documentpn"];
                        documentChecklistToUpdate["gsc_documenttype"] = Document.GetAttributeValue<Boolean>("gsc_documenttype");

                        _organizationService.Update(documentChecklistToUpdate);
                    }
                }
            }

            _tracingService.Trace("Ended ReplicateDocumentInfo method ...");

            return documentChecklistEntity;
        }


        
    }
}

  