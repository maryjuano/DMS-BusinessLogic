using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using System.Globalization;
using GSC.Rover.DMS.BusinessLogic.InventoryMovement;

namespace GSC.Rover.DMS.BusinessLogic.VehiclePurchaseOrder
{

    /* Purpose:  This handler used for gsc_cmn_purchaseorder entity
 
    */
    public class VehiclePurchasOrderHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public VehiclePurchasOrderHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jefferson Cordero, Created On : 4/22/2016
        public Entity PopulateShipDetails(Entity purchaseOrderEntity)
        {
            _tracingService.Trace("Started PopulateShipDetails method..");

            var branchId = purchaseOrderEntity.Contains("gsc_branchcodeid")
             ? purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_branchcodeid").Id
             : Guid.Empty;

            EntityCollection branchRecords = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", branchId, _organizationService, null, OrderType.Ascending,
                new[] { "name", "address1_line1", "gsc_cityid", "gsc_provinceid", "gsc_countryid", "address1_postalcode", "telephone1" });

            if (branchRecords != null && branchRecords.Entities.Count > 0)
            {

                Entity branch = branchRecords.Entities[0];

                var name = branch.Contains("name")
                ? branch.GetAttributeValue<String>("name")
                : String.Empty;

                var street = branch.Contains("address1_line1")
                ? branch.GetAttributeValue<String>("address1_line1")
                : String.Empty;

                var city = branch.Contains("gsc_cityid")
                ? branch.GetAttributeValue<EntityReference>("gsc_cityid").Id
                : Guid.Empty;

                var province = branch.Contains("gsc_provinceid")
                ? branch.GetAttributeValue<EntityReference>("gsc_provinceid").Id
                : Guid.Empty;

                var country = branch.Contains("gsc_countryid")
                ? branch.GetAttributeValue<EntityReference>("gsc_countryid").Id
                : Guid.Empty;

                var zipcode = branch.Contains("address1_postalcode")
                ? branch.GetAttributeValue<String>("address1_postalcode")
                : String.Empty;

                var phone = branch.Contains("telephone1")
                ? branch.GetAttributeValue<String>("telephone1")
                : String.Empty;

                purchaseOrderEntity["gsc_branchname"] = name;
                purchaseOrderEntity["gsc_shiptostreet"] = street;
                purchaseOrderEntity["gsc_shiptozipcode"] = zipcode;
                purchaseOrderEntity["gsc_shiptocontactno"] = phone;

                if (city != Guid.Empty)
                    purchaseOrderEntity["gsc_shiptocityid"] = new EntityReference("gsc_syscity", city);

                if (province != Guid.Empty)
                    purchaseOrderEntity["gsc_shiptoprovincestateid"] = new EntityReference("gsc_sysprovince", province);

                if (country != Guid.Empty)
                    purchaseOrderEntity["gsc_shiptocountry"] = new EntityReference("gsc_syscountry", country);


                _organizationService.Update(purchaseOrderEntity);

            }

            _tracingService.Trace("Ended PopulateVendorDetails method..");
            return purchaseOrderEntity;


        }
        //Created By : Jefferson Cordero, Created On : 4/28/2016
        public Entity DeactivatePurchaseOrder(Entity purchaseOrder)
        {
            SetStateRequest setStateRequest = new SetStateRequest();

            setStateRequest.EntityMoniker = new EntityReference("gsc_cmn_purchaseorder", purchaseOrder.Id);
            setStateRequest.State = new OptionSetValue(1);
            setStateRequest.Status = new OptionSetValue(2);

            _organizationService.Execute(setStateRequest);

            Entity PO = _organizationService.Retrieve(purchaseOrder.LogicalName, purchaseOrder.Id,
            new ColumnSet());

            _organizationService.Update(PO);

            return purchaseOrder;

        }

        //Created By : Jerome Anthony Gerero, Created On : 4/21/2016
        //Modify By : Artum M. Ramos,  Modify On : 1/25/2017
        /*Purpose: Send Email to Every Approvers in Every Branch
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create:
         * Primary Entity: Purchase Order
         */
        public Entity GetLevel1ApproverEmails(Entity purchaseOrderEntity)
        {
            _tracingService.Trace("Started GetLevel1ApproverEmails method...");

            //Retrieve Purchase Order record
            EntityCollection purchaseOrderRecords = CommonHandler.RetrieveRecordsByOneValue(purchaseOrderEntity.LogicalName, "gsc_cmn_purchaseorderid", purchaseOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_branchid", "gsc_purchaseorderpn" });

            if (purchaseOrderRecords != null || purchaseOrderRecords.Entities.Count > 0)
            {
                _tracingService.Trace("purchase order entity...");
                Entity purchaseOrder = purchaseOrderRecords.Entities[0];

                var branchId = purchaseOrder.GetAttributeValue<EntityReference>("gsc_branchid") != null
                    ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_branchid").Id
                    : Guid.Empty;
                _tracingService.Trace("Create Filter for PO...");
                //Create filter for PO Type and Level 1 Approvers
                var purchaseOrderApproverConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_branchid", ConditionOperator.Equal, branchId),
                    new ConditionExpression("gsc_transactiontype", ConditionOperator.Equal, 100000000)
                };

                _tracingService.Trace("Retrive ApproverSetup Record...");
                EntityCollection approverSetupRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_approversetup", purchaseOrderApproverConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_cmn_approversetupid" });

                _tracingService.Trace("Approver Setup Records Count : " + approverSetupRecords.Entities.Count.ToString());
                _tracingService.Trace("Check ApproverSetup Record if null...");
                if (approverSetupRecords != null && approverSetupRecords.Entities.Count > 0)
                {
                    Entity approverSetup = approverSetupRecords.Entities[0];
                    _tracingService.Trace("Retrieve Approver Details Records..." + approverSetup.Id.ToString());
                    //EntityCollection approverDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_approver", "gsc_approversetupid", approverSetupId, _organizationService, null, OrderType.Ascending,
                    //new[] { "gsc_contactid" });

                    QueryExpression approverquery = new QueryExpression("gsc_cmn_approver");
                    approverquery.ColumnSet.AddColumns("gsc_contactid");
                    approverquery.Criteria.AddCondition("gsc_approversetupid", ConditionOperator.Equal, approverSetup.Id);
                    EntityCollection approverDetailRecords = _organizationService.RetrieveMultiple(approverquery);


                    _tracingService.Trace("Approver Detail Records Count : " + approverDetailRecords.Entities.Count.ToString());

                    if (approverDetailRecords != null && approverDetailRecords.Entities.Count > 0)
                    {
                        _tracingService.Trace("For Each ContactId...");
                        foreach (Entity approverDetail in approverDetailRecords.Entities)
                        {
                            _tracingService.Trace("ContactId...");
                            var contactid = approverDetail.Contains("gsc_contactid")
                                ? approverDetail.GetAttributeValue<EntityReference>("gsc_contactid").Id
                                : Guid.Empty;                            

                            _tracingService.Trace("Retrieve Contact Records Email Address..");
                            EntityCollection contactRecords = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", contactid, _organizationService, null, OrderType.Ascending,
                            new[] { "emailaddress1" });

                            _tracingService.Trace("Check If contact Record is Null...");
                            if (contactRecords != null && contactRecords.Entities.Count > 0)
                            {
                                String emailAddress = contactRecords.Entities[0].Contains("emailaddress1")
                                    ? contactRecords.Entities[0].GetAttributeValue<String>("emailaddress1")
                                    : String.Empty;

                                if (!String.IsNullOrEmpty(emailAddress))
                                {
                                    _tracingService.Trace("Call CreateEmail Method...");
                                    CreateEmail(purchaseOrder, contactRecords);
                                }                                
                            }
                        }
                    }
                }
            }
            _tracingService.Trace("Ended GetLevel1ApproverEmails method...");
            return purchaseOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/29/2016
        private void CreateEmail(Entity purchaseOrderEntity, EntityCollection recipients)
        {
            //Retrieve System Admininstrator contact
            EntityCollection sysAdmin = CommonHandler.RetrieveRecordsByOneValue("systemuser", "fullname", "System Administrator", _organizationService, null, OrderType.Ascending,
                new[] { "internalemailaddress" });

            if (sysAdmin != null && sysAdmin.Entities.Count > 0)
            {
                Entity systemAdministrator = sysAdmin.Entities[0];

                Entity from = new Entity("activityparty");
                from["partyid"] = new EntityReference(systemAdministrator.LogicalName, systemAdministrator.Id);

                EntityCollection recipientsList = new EntityCollection();
                recipientsList.EntityName = "activityparty";

                foreach (Entity recipientEntity in recipients.Entities)
                {
                    Entity recipientActivityParty = new Entity("activityparty");
                    recipientActivityParty["partyid"] = new EntityReference(recipientEntity.LogicalName, recipientEntity.Id);
                    recipientsList.Entities.Add(recipientActivityParty);
                }

                Entity to = new Entity("activityparty");
                to["partyid"] = recipientsList;

                Entity email = new Entity("email");
                email["from"] = new Entity[] { from };
                email["to"] = recipientsList;
                email["subject"] = "Purchase Order No. " + purchaseOrderEntity["gsc_purchaseorderpn"] + " Order Approved";
                email["description"] = "Please be notified that the Purchase Order : " + purchaseOrderEntity["gsc_purchaseorderpn"] + " has been for Approval.";
                Guid emailId = _organizationService.Create(email);

                SendEmailRequest req = new SendEmailRequest();
                req.EmailId = emailId;
                req.IssueSend = true;
                req.TrackingToken = "";
                SendEmailResponse res = (SendEmailResponse)_organizationService.Execute(req);
            }
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/3/2016
        public Entity CreateApprovalRecord(Entity purchaseOrderEntity)
        {
            _tracingService.Trace("Started CreateApprovalRecord method...");

            //Retrieve Purchase Order record
            EntityCollection purchaseOrderRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorder", "gsc_cmn_purchaseorderid", purchaseOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_recordownerid", "gsc_approvalstatus" });

            if (purchaseOrderRecords != null && purchaseOrderRecords.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderRecords.Entities[0];

                var ownerId = purchaseOrder.GetAttributeValue<EntityReference>("gsc_recordownerid") != null
                    ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_recordownerid").Id
                    : Guid.Empty;

                var approvalStatus = purchaseOrder.GetAttributeValue<OptionSetValue>("gsc_approvalstatus") != null
                    ? purchaseOrder.GetAttributeValue<OptionSetValue>("gsc_approvalstatus").Value
                    : 0;

                //Retrieve current user (approver) information
                EntityCollection approverRecords = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", ownerId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_potypeid", "fullname", "gsc_approverlevel" });

                if (approverRecords != null && approverRecords.Entities.Count > 0)
                {
                    Entity approver = approverRecords.Entities[0];

                    EntityCollection purchaseOrderApprovalRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_svc_purchaseorderapproval", "gsc_purchaseorderid", purchaseOrder.Id, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_svc_purchaseorderapprovalid" });

                    if (purchaseOrderApprovalRecords.Entities.Count <= 2)
                    {
                        if (approvalStatus == 100000001 || approvalStatus == 100000002)
                        {
                            Entity purchaseOrderApproval = new Entity("gsc_svc_purchaseorderapproval");
                            purchaseOrderApproval["gsc_purchaseorderid"] = new EntityReference(purchaseOrderEntity.LogicalName, purchaseOrderEntity.Id);
                            purchaseOrderApproval["gsc_svc_purchaseorderapprovalpn"] = approver.GetAttributeValue<String>("fullname");
                            purchaseOrderApproval["gsc_approverlevel"] = new OptionSetValue(approver.GetAttributeValue<OptionSetValue>("gsc_approverlevel").Value);
                            purchaseOrderApproval["gsc_isapproved"] = true;
                            purchaseOrderApproval["gsc_dateapproveddisapproved"] = DateTime.UtcNow;
                            _organizationService.Create(purchaseOrderApproval);
                        }
                        else if (approvalStatus == 100000003)
                        {
                            Entity purchaseOrderApproval = new Entity("gsc_svc_purchaseorderapproval");
                            purchaseOrderApproval["gsc_purchaseorderid"] = new EntityReference(purchaseOrderEntity.LogicalName, purchaseOrderEntity.Id);
                            purchaseOrderApproval["gsc_svc_purchaseorderapprovalpn"] = approver.GetAttributeValue<String>("fullname");
                            purchaseOrderApproval["gsc_approverlevel"] = new OptionSetValue(approver.GetAttributeValue<OptionSetValue>("gsc_approverlevel").Value);
                            purchaseOrderApproval["gsc_isapproved"] = false;
                            purchaseOrderApproval["gsc_dateapproveddisapproved"] = DateTime.UtcNow;
                            _organizationService.Create(purchaseOrderApproval);
                        }
                    }
                }
            }

            _tracingService.Trace("Ended CreateApprovalRecord method...");
            return purchaseOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 5/4/2016
        public Entity GeneratePurchaseOrderNumber(Entity purchaseOrderEntity)
        {
            _tracingService.Trace("Started GeneratePurchaseOrderNumber method...");

            //Entity vendor = _organizationService.Retrieve("account", purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_vendorid").Id, new ColumnSet("name"));
            String vendorName = purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_vendorid") != null
                ? purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_vendorid").Name
                : "";

            if (vendorName.Equals("MMPC"))
            {
                //Entity purchaseOrderType = _organizationService.Retrieve("gsc_svc_potype", purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_potypeid").Id, new ColumnSet("gsc_name"));
                //Entity purchaseOrderSuffix = _organizationService.Retrieve("gsc_svc_posuffix", purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_posuffixid").Id, new ColumnSet("gsc_name"));

                String purchaseOrderType = purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_potypeid") != null
                    ? String.Concat(purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_potypeid").Name[0])
                    : "Z";

                String purchaseOrderSuffix = purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_posuffixid") != null
                    ? String.Concat(purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_posuffixid").Name[0])
                    : "X";

                //Create filter for purchase orders
                var purchaseOrderConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("createdon", ConditionOperator.Last7Days)
                };

                //Retrieve purchase order type records
                EntityCollection purchaseOrderRecords = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_purchaseorder", purchaseOrderConditionList, _organizationService, "createdon", OrderType.Descending,
                    new[] { "gsc_purchaseorderpn" });

                Int32 seriesNo = 0;
                Int32 weekNo = 0;
                String lastPurchaseOrderNo = "ZyyMM0100";

                if (purchaseOrderRecords != null && purchaseOrderRecords.Entities.Count >= 2)
                {
                    lastPurchaseOrderNo = purchaseOrderRecords.Entities[1].GetAttributeValue<String>("gsc_purchaseorderpn");
                    lastPurchaseOrderNo = lastPurchaseOrderNo.Remove(lastPurchaseOrderNo.Length - 1);
                }

                weekNo = Convert.ToInt32(lastPurchaseOrderNo.Substring(5, 2));
                seriesNo = Convert.ToInt32(lastPurchaseOrderNo.Substring(7)) + 1;
                Int32 currentWeekNo = GetIso8601WeekOfYear(DateTime.UtcNow);

                if (weekNo != currentWeekNo)
                {
                    weekNo = currentWeekNo;
                    seriesNo = 1;
                }

                String poNumber = purchaseOrderType + DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM").PadLeft(2, '0') + weekNo.ToString().PadLeft(2, '0') + seriesNo.ToString().PadLeft(4, '0') + purchaseOrderSuffix;
                purchaseOrderEntity["gsc_purchaseorderpn"] = poNumber;
                _organizationService.Update(purchaseOrderEntity);
            }

            _tracingService.Trace("Ended GeneratePurchaseOrderNumber method...");
            return purchaseOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 7/5/2016
        /*Purpose: Adjust product quantity of VPO Detail Vehicle
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create:
         * Primary Entity: Purchase Order
         */
        public Entity AdjustProductQuantity(Entity purchaseOrderEntity)
        {
            _tracingService.Trace("Started AdjustProductQuantity method...");

            if (!purchaseOrderEntity.FormattedValues["gsc_vpostatus"].Equals("Ordered")) { return null; }

            var siteId = purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_siteid") != null
                ? purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_siteid").Id
                : Guid.Empty;

            //Retrieve Purchase Order Detail Entity
            EntityCollection purchaseOrderDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", purchaseOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_productid" });

            _tracingService.Trace("Purchase Order Detail Records Count : " + purchaseOrderDetailRecords.Entities.Count.ToString());

            if (purchaseOrderDetailRecords != null && purchaseOrderDetailRecords.Entities.Count > 0)
            {
                foreach (Entity purchaseOrderDetail in purchaseOrderDetailRecords.Entities)
                {
                    var productId = purchaseOrderDetail.Contains("gsc_productid")
                        ? purchaseOrderDetail.GetAttributeValue<EntityReference>("gsc_productid").Id
                        : Guid.Empty;

                    //Create filter for Product Quantity record
                    var productQuantityConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_siteid", ConditionOperator.Equal, siteId),
                        new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId)
                    };

                    //Retrieve Product Quantity record using ConditionExpression
                    EntityCollection productQuantityRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityConditionList, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_onorder" });

                    _tracingService.Trace("Product Quantity Records Count : " + productQuantityRecords.Entities.Count.ToString());

                    if (productQuantityRecords != null && productQuantityRecords.Entities.Count > 0)
                    {
                        foreach (Entity productQuantity in productQuantityRecords.Entities)
                        {
                            Int32 onOrderCount = productQuantity.Contains("gsc_onorder")
                                ? productQuantity.GetAttributeValue<Int32>("gsc_onorder")
                                : 0;

                            productQuantity["gsc_onorder"] = onOrderCount + 1;

                            _organizationService.Update(productQuantity);
                        }
                    }
                }
            }

            _tracingService.Trace("Ended AdjustProductQuantity method...");
            return purchaseOrderEntity;
        }

        //Taken from https://blogs.msdn.microsoft.com/shawnste/2006/01/24/iso-8601-week-of-year-format-in-microsoft-net/
        //This presumes that weeks start with Monday.
        //Week 1 is the 1st week of the year with a Thursday in it.
        public static int GetIso8601WeekOfYear(DateTime time)
        {
            //Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            //be the same week# as whatever Thursday, Friday or Saturday are,
            //and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            //Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        //Create By: Raphael Herrera, Created On: 6/29/2016 /*
        /* Purpose:  Validate if record can be deleted based on vpostatus field
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Purchase Order
         */
        public bool ValidateDelete(Entity vehiclePurchaseOrder)
        {
            _tracingService.Trace("Starting ValidateDelete Method...");
            var vpoStatus = vehiclePurchaseOrder.Contains("gsc_vpostatus") ? vehiclePurchaseOrder.GetAttributeValue<OptionSetValue>("gsc_vpostatus").Value
                : 0;

            if (vpoStatus == 100000000)
                return true;
            else return false;
        }

        //Create By: Raphael Herrera, Created On: 6/30/2016 /*
        /* Purpose:  Validate of desired delivery date is not less than createdon date
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Purchase Order
         */
        public bool ValidateDesiredDate(Entity vehiclePurchaseOrder)
        {
            _tracingService.Trace("Starting ValidateDesiredDate Method...");
            DateTime createdOn = vehiclePurchaseOrder.GetAttributeValue<DateTime>("createdon").Date;
            DateTime desiredDate = vehiclePurchaseOrder.GetAttributeValue<DateTime>("gsc_desireddate");

            if (desiredDate < createdOn)
            {
                return false;
            }
            return true;
        }

        public Entity UpdatePurchaseOrderStatusCopy(Entity vehiclePurchaseOrder)
        {
            _tracingService.Trace("Starting UpdatePurchaseOrderStatusCopy Method...");
            vehiclePurchaseOrder["gsc_vpostatuscopy"] = vehiclePurchaseOrder.Contains("gsc_vpostatus") ? vehiclePurchaseOrder.GetAttributeValue<OptionSetValue>("gsc_vpostatus")
                : null;
            _organizationService.Update(vehiclePurchaseOrder);

            _tracingService.Trace("Ending UpdatePurchaseOrderStatusCopy Method...");
            return vehiclePurchaseOrder;
        }

        //Created By: Raphael Herrera, Created On: 01/30/2017
        /* Purpose:  Create Product Qunatity record for PO
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_vpostatus = 100,000,002 Ordered
         * Primary Entity: Purchase Order
         */
        public Entity CreateProductQuantity(Entity vehiclePurchaseOrder, Entity preVehiclePurchaseOrder)
        {
            if (!vehiclePurchaseOrder.FormattedValues["gsc_vpostatus"].Equals("Ordered")) { return null; }

            if (ValidateSubmit(preVehiclePurchaseOrder) == true)
            {
                _tracingService.Trace("Starting CreateProductQuantity Method");
                var siteId = vehiclePurchaseOrder.Contains("gsc_siteid") ? vehiclePurchaseOrder.GetAttributeValue<EntityReference>("gsc_siteid").Id
                    : Guid.Empty;

                String siteName = vehiclePurchaseOrder.Contains("gsc_siteid") ? vehiclePurchaseOrder.GetAttributeValue<EntityReference>("gsc_siteid").Name
                    : String.Empty;

                EntityCollection purchaseOrderDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", vehiclePurchaseOrder.Id,
                    _organizationService, null, OrderType.Ascending, new[] { "gsc_productid", "gsc_vehiclecolorid", "gsc_basemodelid", "gsc_modelcode",
                "gsc_optioncode", "gsc_modelyear"});

                _tracingService.Trace("VPO Details Records Retrieved: " + purchaseOrderDetailsCollection.Entities.Count);
                if (purchaseOrderDetailsCollection.Entities.Count > 0)
                {
                    Entity purchaseOrderDetailsEntity = purchaseOrderDetailsCollection.Entities[0];
                    var productId = purchaseOrderDetailsEntity.Contains("gsc_productid") ? purchaseOrderDetailsEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                        : Guid.Empty;
                    var colorId = purchaseOrderDetailsEntity.Contains("gsc_vehiclecolorid") ? purchaseOrderDetailsEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid").Id
                        : Guid.Empty;
                    String productName = purchaseOrderDetailsEntity.Contains("gsc_productid") ? purchaseOrderDetailsEntity.GetAttributeValue<EntityReference>("gsc_productid").Name
                        : String.Empty;
                    var productQuantityId = Guid.Empty;

                    var productQuantityConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                   // new ConditionExpression("gsc_isonorder", ConditionOperator.Equal, true),
                    new ConditionExpression("gsc_vehiclecolorid", ConditionOperator.Equal, colorId)
                };

                    if (siteId == Guid.Empty)
                    {
                        _tracingService.Trace("Empty Site...");
                        productQuantityConditionList.Add(new ConditionExpression("gsc_siteid", ConditionOperator.Null));
                    }
                    else//filter by site id
                        productQuantityConditionList.Add(new ConditionExpression("gsc_siteid", ConditionOperator.Equal, siteId));

                    EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByConditions("gsc_iv_productquantity", productQuantityConditionList, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_onorder" });

                    _tracingService.Trace("Product Quantity Records Retrieved: " + productQuantityCollection.Entities.Count);
                    if (productQuantityCollection.Entities.Count > 0)//Update Existing Product Quantity
                    {
                        Entity productQuantityEntity = productQuantityCollection.Entities[0];
                        Int32 onOrder = productQuantityEntity.Contains("gsc_onorder") ? productQuantityEntity.GetAttributeValue<Int32>("gsc_onorder") : 0;
                        productQuantityEntity["gsc_onorder"] = onOrder + 1;

                        _organizationService.Update(productQuantityEntity);
                        productQuantityId = productQuantityEntity.Id;
                        _tracingService.Trace("Updated Existing Product Quantity");

                        InventoryMovementHandler handler = new InventoryMovementHandler(_organizationService, _tracingService);
                        handler.LogInventoryQuantityOnorder(vehiclePurchaseOrder, purchaseOrderDetailsEntity, productQuantityEntity, true, 100000000);
                    }
                    else//Create New Product Quantity Record
                    {
                        Entity productQuantityEntity = new Entity("gsc_iv_productquantity");
                        if (siteId != Guid.Empty)
                            productQuantityEntity["gsc_siteid"] = new EntityReference("gsc_iv_site", siteId);
                        productQuantityEntity["gsc_vehiclecolorid"] = new EntityReference("gsc_cmn_vehiclecolor", colorId);
                        productQuantityEntity["gsc_productid"] = new EntityReference("product", productId);
                        productQuantityEntity["gsc_vehiclemodelid"] = purchaseOrderDetailsEntity.GetAttributeValue<EntityReference>("gsc_basemodelid") != null
                            ? purchaseOrderDetailsEntity.GetAttributeValue<EntityReference>("gsc_basemodelid") :
                            null;
                        //productQuantityEntity["gsc_isonorder"] = true;
                        productQuantityEntity["gsc_onorder"] = 1;
                        productQuantityEntity["gsc_productquantitypn"] = productName;
                        productQuantityEntity["gsc_dealerid"] = vehiclePurchaseOrder.Contains("gsc_dealerid") ? vehiclePurchaseOrder.GetAttributeValue<EntityReference>("gsc_dealerid")
                            : null;
                        productQuantityEntity["gsc_branchid"] = vehiclePurchaseOrder.Contains("gsc_branchid") ? vehiclePurchaseOrder.GetAttributeValue<EntityReference>("gsc_branchid")
                            : null;
                        productQuantityEntity["gsc_recordownerid"] = vehiclePurchaseOrder.Contains("gsc_recordownerid") ? vehiclePurchaseOrder.GetAttributeValue<EntityReference>("gsc_recordownerid")
                            : null;
                        productQuantityEntity["gsc_onhand"] = 0;
                        productQuantityEntity["gsc_available"] = 0;
                        productQuantityEntity["gsc_allocated"] = 0;
                        productQuantityEntity["gsc_sold"] = 0;

                        productQuantityId = _organizationService.Create(productQuantityEntity);
                        _tracingService.Trace("Created New Product QUantity");

                        EntityCollection newPQCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId,
                    _organizationService, null, OrderType.Ascending, new[] { "gsc_productquantitypn" });

                        if (newPQCollection.Entities.Count > 0)//Update Existing Product Quantity
                        {
                            InventoryMovementHandler handler = new InventoryMovementHandler(_organizationService, _tracingService);
                            handler.LogInventoryQuantityOnorder(vehiclePurchaseOrder, purchaseOrderDetailsEntity, newPQCollection.Entities[0], true, 100000000);
                        }
                    }

                    vehiclePurchaseOrder["gsc_productquantityid"] = new EntityReference("gsc_iv_productquantity", productQuantityId);
                    _organizationService.Update(vehiclePurchaseOrder);
                    _tracingService.Trace("Updated Purchase Order Record...");
                }
                _tracingService.Trace("Ending CreateProductQuantity Method...");

                return vehiclePurchaseOrder;
            }
            else
                throw new InvalidPluginExecutionException("Unable to post this record.");
        }

        private bool ValidateSubmit(Entity vehiclePurchaseOrder)
        {
            _tracingService.Trace("Started Validate Submit Method...");
            var approverStatus = vehiclePurchaseOrder.GetAttributeValue<OptionSetValue>("gsc_approvalstatus").Value;
            var vpoStatus = vehiclePurchaseOrder.GetAttributeValue<OptionSetValue>("gsc_vpostatus").Value;

            var approverSetupCondition = new List<ConditionExpression>
            {
                new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                new ConditionExpression("gsc_transactiontype", ConditionOperator.Equal, 100000000),
                new ConditionExpression("gsc_branchid", ConditionOperator.Equal, vehiclePurchaseOrder.GetAttributeValue<EntityReference>("gsc_branchid").Id)
            };

            EntityCollection approverCollection = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_approversetup", approverSetupCondition, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_approversetuppn" });
            _tracingService.Trace("Retrieved approver setup records...");
            if (approverCollection.Entities != null && approverCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Approver setupn found...");
                _tracingService.Trace("approverStatus: " + approverStatus + " vpoStatus:" + vpoStatus);
                if (vpoStatus == 100000001 && approverStatus == 100000000)
                    return true;
                else
                    return false;
            }
            else if (vpoStatus == 100000001)
                return true;
            else return false;
        }

        public Entity SubtractUnservedVPO(Entity purchaseOrder)
        {
            if (!purchaseOrder.FormattedValues["gsc_vpostatus"].Equals("Cancelled")) { return null; }

            _tracingService.Trace("Started SubtractUnservedVPO method...");

            var productQuantityId = CommonHandler.GetEntityReferenceIdSafe(purchaseOrder, "gsc_productquantityid");

            EntityCollection productQuantityCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_productquantity", "gsc_iv_productquantityid", productQuantityId, _organizationService, null, OrderType.Ascending,
           new[] { "gsc_onorder" });

            if (productQuantityCollection != null && productQuantityCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Product Quantity...");

                Entity productQuantity = productQuantityCollection.Entities[0];

                var onOrder = productQuantity.Contains("gsc_onorder")
                    ? productQuantity.GetAttributeValue<Int32>("gsc_onorder") : 0;

                productQuantity["gsc_onorder"] = onOrder - 1;

                _organizationService.Update(productQuantity);

                EntityCollection purchaseOrderDetailsCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_purchaseorderitemdetails", "gsc_purchaseorderid", purchaseOrder.Id,
                _organizationService, null, OrderType.Ascending, new[] { "gsc_productid", "gsc_vehiclecolorid", "gsc_basemodelid", "gsc_modelcode",
                "gsc_optioncode", "gsc_modelyear"});

                if (purchaseOrderDetailsCollection.Entities.Count > 0)
                {
                    InventoryMovementHandler handler = new InventoryMovementHandler(_organizationService, _tracingService);
                    handler.LogInventoryQuantityOnorder(purchaseOrder, purchaseOrderDetailsCollection.Entities[0], productQuantity, false, 100000004);
                }

            }
            _tracingService.Trace("Ended SubtractUnservedVPO method...");

            return purchaseOrder;
        }

        //Created By: Jessica Casupanan, Created On: 01/31/2017
        /* Purpose:  Populate vendor and bank details
         * Registration Details:
         * Event/Message: 
         *      Post/Create:
         * Primary Entity: Purchase Order
         */
        public void PopulateDetails(Entity purchaseOrder)
        {
            _tracingService.Trace("PopulateDetails Method Started...");
            //Vendor Details
            Guid vendorId = purchaseOrder.Contains("gsc_vendorid") ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_vendorid").Id : Guid.Empty;
            EntityCollection vendorEC = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", vendorId, _organizationService, null, OrderType.Ascending,
            new[] { "accountnumber" });

            if (vendorEC != null && vendorEC.Entities.Count > 0) 
            {
                Entity vendorEntity = vendorEC.Entities[0];
                purchaseOrder["gsc_vendorname"] = vendorEntity.Contains("accountnumber") ? vendorEntity.GetAttributeValue<string>("accountnumber") : string.Empty;
            }

            //Bank Details
            Guid bankId = purchaseOrder.Contains("gsc_bankid") ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_bankid").Id : Guid.Empty;
            EntityCollection bankEC = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_bank", "gsc_sls_bankid", bankId, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_name" });

            if (bankEC != null && bankEC.Entities.Count > 0)
            {
                Entity bankEntity = bankEC.Entities[0];
                purchaseOrder["gsc_bankname"] = bankEntity.Contains("gsc_name") ? bankEntity.GetAttributeValue<string>("gsc_name") : string.Empty;
            }
            _organizationService.Update(purchaseOrder);
            _tracingService.Trace("PopulateDetails Method Ended...");
        }

        //Created By: Leslie G. Baliguat, Created On: 01/31/2017
        //Replicate Branch and Dealer to Ship-To Information
        public void PopulateShipToInformation(Entity purchaseOrder)
        {
            var dealer = purchaseOrder.GetAttributeValue<EntityReference>("gsc_dealerid") != null
                ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_dealerid")
                : null;

            var branch = purchaseOrder.GetAttributeValue<EntityReference>("gsc_branchid") != null
                ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_branchid")
                : null;

            purchaseOrder["gsc_tobranchid"] = branch;
            purchaseOrder["gsc_todealerid"] = dealer;
        }

    }
}
