using GSC.Rover.DMS.BusinessLogic.CommittedFirmOrderQuantityDetail;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSC.Rover.DMS.BusinessLogic.CommittedFirmOrderQuantity
{
    public class CommittedFirmOrderQuantityHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;


        public CommittedFirmOrderQuantityHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By: Jessica Casupanan, Created On: 3/1/2017
        /*Purpose: Generate Purchase Order
         * Registration Details: 
         * Event/Message: 
         *      Post/Update: gsc_generatepo
         * Primary Entity: Committed Firm Order Quantity
        */
        public List<Entity> GeneratePO(Entity cfoQuantityEntity)
        {
            List<Entity> cfoQuantityEntities = new List<Entity>();

            if (cfoQuantityEntity.GetAttributeValue<Boolean>("gsc_generatepo"))
            {
                _tracingService.Trace("Started GeneratePO Method.");

                EntityCollection cfoQuantityDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantitydetail", "gsc_committedfirmorderquantityid", cfoQuantityEntity.Id,
                    _organizationService, null, OrderType.Ascending, new[] {"gsc_productid", "gsc_recordownerid",
                "gsc_dealerid", "gsc_branchid", "gsc_vehiclebasemodelid","gsc_committedfirmorderquantityid","gsc_vpobalance","gsc_vpoquantityforsubmission","gsc_siteid"});

                if (cfoQuantityDetailRecords != null && cfoQuantityDetailRecords.Entities.Count > 0)
                {
                    foreach (var cfoQuantityDetailEntity in cfoQuantityDetailRecords.Entities)
                    {
                        int vpoBalance = cfoQuantityDetailEntity.Contains("gsc_vpobalance") ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_vpobalance") : 0;
                        if (vpoBalance > 0)
                        {
                            _tracingService.Trace("Retrieve CFO Quantity Details.");
                            //Retrieve fields from cfoquantitydetails.
                            int vpoQtyForSubmission = cfoQuantityDetailEntity.Contains("gsc_vpoquantityforsubmission") ? cfoQuantityDetailEntity.GetAttributeValue<Int32>("gsc_vpoquantityforsubmission") : 0;
                            Guid productid = cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                                    ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                                    : Guid.Empty;
                            Guid baseModel = cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid") != null
                                     ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid").Id
                                     : Guid.Empty;
                            Guid site = cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                                     ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                                     : Guid.Empty;
                            Guid cfoQtyId = cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_committedfirmorderquantityid") != null
                                     ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_committedfirmorderquantityid").Id
                                     : Guid.Empty;
                            Guid recordOwner = cfoQuantityEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                                     ? cfoQuantityEntity.GetAttributeValue<EntityReference>("gsc_recordownerid").Id
                                     : Guid.Empty;
                            Guid branchId = cfoQuantityEntity.GetAttributeValue<EntityReference>("gsc_branchid") != null
                                    ? cfoQuantityEntity.GetAttributeValue<EntityReference>("gsc_branchid").Id
                                    : Guid.Empty;
                            Guid dealerId = cfoQuantityEntity.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                                    ? cfoQuantityEntity.GetAttributeValue<EntityReference>("gsc_dealerid").Id
                                    : Guid.Empty;

                            //Retrieve Product Info.
                            EntityCollection productRecords = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productid, _organizationService, null, OrderType.Ascending,
                                new[] { "gsc_modelyear", "gsc_modelcode", "gsc_optioncode" });

                            if (productRecords != null && productRecords.Entities.Count > 0)
                            {
                                _tracingService.Trace("Retrieve Products Model Year.");

                                Entity product = productRecords.Entities[0];
                                String modelYear = product.Contains("gsc_modelyear") ? product.GetAttributeValue<String>("gsc_modelyear") : String.Empty;
                                String optionCode = product.Contains("gsc_optioncode") ? product.GetAttributeValue<String>("gsc_optioncode") : String.Empty;
                                String modelCode = product.Contains("gsc_modelcode") ? product.GetAttributeValue<String>("gsc_modelcode") : String.Empty;


                                //Create PO and PO Item Entity depends on VPO For Submission
                                while (vpoQtyForSubmission != 0)
                                {
                                    _tracingService.Trace("Create PO Entity.");

                                    //call private method  GetMMPCVendor
                                    //var vendorEntity = GetMMPCVendor();

                                    Entity poEntity = new Entity("gsc_cmn_purchaseorder");
                                    poEntity["gsc_vpostatus"] = new OptionSetValue(100000000);
                                    poEntity["gsc_vpotype"] = new OptionSetValue(100000000);
                                    poEntity["gsc_cfonumber"] = cfoQuantityEntity.Contains("gsc_committedfirmorderquantitypn")
                                        ? cfoQuantityEntity.GetAttributeValue<String>("gsc_committedfirmorderquantitypn")
                                        : String.Empty;
                                    poEntity["gsc_vpodate"] = DateTime.Now;
                                    //poEntity["gsc_vendorid"] = new EntityReference(vendorEntity.LogicalName, vendorEntity.Id);
                                    if (dealerId != Guid.Empty)
                                    {
                                        poEntity["gsc_todealerid"] = new EntityReference("account", dealerId);
                                        poEntity["gsc_dealerid"] = new EntityReference("account", dealerId);
                                    }
                                    if (branchId != Guid.Empty)
                                    {
                                        poEntity["gsc_tobranchid"] = new EntityReference("account", branchId);
                                        poEntity["gsc_branchid"] = new EntityReference("account", branchId);
                                    }
                                    if (recordOwner != Guid.Empty)
                                        poEntity["gsc_recordownerid"] = new EntityReference("contact", recordOwner);
                                    if (site != Guid.Empty)
                                        poEntity["gsc_siteid"] = new EntityReference("gsc_iv_site", site);

                                    var poEntityId = _organizationService.Create(poEntity);
                                    cfoQuantityEntities.Add(poEntity);

                                    _tracingService.Trace("PO Entity Created.");

                                    _tracingService.Trace("Create PO Item Entity.");

                                    Entity poItemEntity = new Entity("gsc_cmn_purchaseorderitemdetails");
                                    poItemEntity["gsc_purchaseorderid"] = new EntityReference("gsc_cmn_purchaseorder", poEntityId);
                                    if (baseModel != Guid.Empty)
                                        poItemEntity["gsc_basemodelid"] = new EntityReference("gsc_iv_vehiclebasemodel", baseModel);
                                    poItemEntity["gsc_parentproductid"] = new EntityReference("product", productid);
                                    poItemEntity["gsc_modelcode"] = modelCode;
                                    poItemEntity["gsc_optioncode"] = optionCode;
                                    poItemEntity["gsc_modelyear"] = modelYear;
                                    /*poItemEntity["gsc_vehiclecolorid"] = cfoQuantityDetailEntity.Contains("gsc_vehiclecolorid")
                                        ? cfoQuantityDetailEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid")
                                        : null;*/
                                    if (recordOwner != Guid.Empty)
                                        poItemEntity["gsc_recordownerid"] = new EntityReference("contact", recordOwner);
                                    if (dealerId != Guid.Empty)
                                        poItemEntity["gsc_dealerid"] = new EntityReference("account", dealerId);
                                    if (branchId != Guid.Empty)
                                        poItemEntity["gsc_branchid"] = new EntityReference("account", branchId);
                                    _organizationService.Create(poItemEntity);
                                    cfoQuantityEntities.Add(poItemEntity);
                                    _tracingService.Trace("PO ItemEntity Created.");
                                    vpoQtyForSubmission--;
                                }
                            }
                        }
                    }

                    //if (zeroVPOBalance > 0)
                    //   throw new InvalidPluginExecutionException("!_There are "+ zeroVPOBalance +" record(s) that cannot generate VPO. Please be sure that the VPO balance is not equal to zero.");

                    Entity cfoQuantityToUpdate = _organizationService.Retrieve(cfoQuantityEntity.LogicalName, cfoQuantityEntity.Id,
                                   new ColumnSet("gsc_generatepo"));
                    cfoQuantityToUpdate["gsc_generatepo"] = false;

                    _organizationService.Update(cfoQuantityToUpdate);

                    _tracingService.Trace("Generate PO back to false.");
                }
            }
            _tracingService.Trace("Ended GeneratePO Method.");

            return cfoQuantityEntities;
        }


        //Retrieve MMPC from Vendor Master
        private Entity GetMMPCVendor()
        {
            _tracingService.Trace("Started GetMMPCVendor Method.");

            var vendorConditionList = new List<ConditionExpression>
                {
                  new ConditionExpression("gsc_vendornamepn", ConditionOperator.Equal, "MMPC")
                };

            EntityCollection vendorRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_vendor", vendorConditionList, _organizationService, null, OrderType.Descending,
                new[] { "gsc_vendornamepn" });

            if (vendorRecords != null && vendorRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve MMPC Vendor.");
                _tracingService.Trace("Ended GetMMPCVendor Method.");
                return vendorRecords.Entities[0];
            }
            else
            {
                _tracingService.Trace("Ended GetMMPCVendor Method.");
                throw new InvalidPluginExecutionException("Cannot Process your Purchase Order generation. Please contact the administrator.");
            }
        }

        //Created By: Leslie Baliguat, Created On: 6/30/2016
        //Modified By: Artum M. Ramos, Modified On: 3/2/2017
        public void SubmitCFO(Entity cfoQuantityEntity)
        {
            _tracingService.Trace("Submit CFO.");

            EntityCollection cfoQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantity", "gsc_sls_committedfirmorderquantityid", cfoQuantityEntity.Id,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_cfostatus" });
            _tracingService.Trace("check if not null");
            if (cfoQuantityCollection != null && cfoQuantityCollection.Entities.Count > 0)
            {
                Entity cfoQuantityToUpdate = cfoQuantityCollection.Entities[0];
                _tracingService.Trace("Update CFO Quantity");
                cfoQuantityToUpdate["gsc_cfostatus"] = new OptionSetValue(100000001);
                _organizationService.Update(cfoQuantityToUpdate);

                _tracingService.Trace("CFO Submitted.");
            }
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/2/2017
        /*Purpose: Update site field of related detail records on site field change
         * Registration Details: 
         * Event/Message: 
         *      Pre/Create: Site / gsc_site
         * Primary Entity: Committed Firm Order Quantity
         */
        public Entity UpdateSiteField(Entity cfoQuantityEntity)
        {
            _tracingService.Trace("Started UpdateSiteField method..");

            Guid siteId = cfoQuantityEntity.Contains("gsc_siteid")
                ? cfoQuantityEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                : Guid.Empty;

            EntityCollection cfoQuantityDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_committedfirmorderquantitydetail", "gsc_committedfirmorderquantityid", cfoQuantityEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_siteid" });

            if (cfoQuantityDetailRecords != null && cfoQuantityDetailRecords.Entities.Count > 0)
            {
                foreach (Entity cfoQuantityDetail in cfoQuantityDetailRecords.Entities)
                {
                    cfoQuantityDetail["gsc_siteid"] = cfoQuantityEntity.Contains("gsc_siteid")
                        ? cfoQuantityEntity.GetAttributeValue<EntityReference>("gsc_siteid")
                        : null;

                    _organizationService.Update(cfoQuantityDetail);
                }
            }

            _tracingService.Trace("Ended UpdateSiteField method..");
            return cfoQuantityEntity;
        }

        public void UpdateVPOSubmitted(Entity cfoQuantityEntity)
        {
            var detailConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_committedfirmorderquantityid", ConditionOperator.Equal, cfoQuantityEntity.Id),
                    new ConditionExpression("gsc_vpoquantityforsubmission", ConditionOperator.NotNull)
                };

            EntityCollection detailRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_committedfirmorderquantitydetail", detailConditionList, _organizationService, "createdon", OrderType.Descending,
                    new[] { "gsc_vpoquantityforsubmission", "gsc_submittedvpo", "gsc_allocatedquantity", "gsc_vpobalance", "gsc_vpobalfromprevmonth", "gsc_productid", "gsc_siteid", "gsc_committedfirmorderquantityid"});

            if (detailRecords != null && detailRecords.Entities.Count > 0)
            {
                foreach(Entity detail in detailRecords.Entities)
                {
                    CommittedFirmOrderQuantityDetailHandler cfoQuantityDetailHandler = new CommittedFirmOrderQuantityDetailHandler(_organizationService, _tracingService);
                    cfoQuantityDetailHandler.SubmitVPO(detail, "Update");
                }
            }
        }
    }
}
