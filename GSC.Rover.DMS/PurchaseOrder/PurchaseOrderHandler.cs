using GSC.Rover.DMS.BusinessLogic.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;

using System.Globalization;

namespace GSC.Rover.DMS.BusinessLogic.PurchaseOrder
{
    public class PurchaseOrderHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public PurchaseOrderHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jefferson Cordero, Created On : 4/21/2016
        public Entity PopulateVendorDetails(Entity purchaseOrderEntity)
        {
            _tracingService.Trace("Started PopulateVendorDetails method..");


            var vendorId = purchaseOrderEntity.Contains("gsc_vendorid")
            ? purchaseOrderEntity.GetAttributeValue<EntityReference>("gsc_vendorid").Id
            : Guid.Empty;

            EntityCollection vendorRecords = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", vendorId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_street", "gsc_cityid", "gsc_provinceid", "gsc_countryid", "gsc_zipcode", "gsc_phone" });

            if (vendorRecords != null && vendorRecords.Entities.Count > 0)
            {

                Entity vendor = vendorRecords.Entities[0];

                var vendorname = vendor.Contains("gsc_vendorname")
                ? vendor.GetAttributeValue<String>("gsc_vendorname")
                    : String.Empty;

                var street = vendor.Contains("gsc_street")
                ? vendor.GetAttributeValue<String>("gsc_street")
                    : String.Empty;

                var city = vendor.Contains("gsc_cityid")
                ? vendor.GetAttributeValue<EntityReference>("gsc_cityid").Id
                : Guid.Empty;

                var province = vendor.Contains("gsc_provinceid")
                ? vendor.GetAttributeValue<EntityReference>("gsc_provinceid").Id
                : Guid.Empty;

                var country = vendor.Contains("gsc_countryid")
                ? vendor.GetAttributeValue<EntityReference>("gsc_countryid").Id
                : Guid.Empty;

                var zipcode = vendor.Contains("gsc_zipcode")
                ? vendor.GetAttributeValue<String>("gsc_zipcode")
                : String.Empty;

                var phone = vendor.Contains("gsc_phone")
                ? vendor.GetAttributeValue<String>("gsc_phone")
                    : String.Empty;

                purchaseOrderEntity["gsc_vendorname"] = vendorname;
                purchaseOrderEntity["gsc_street"] = street;
                purchaseOrderEntity["gsc_zipcode"] = zipcode;
                purchaseOrderEntity["gsc_contactno"] = phone;

                if (city != Guid.Empty)
                    purchaseOrderEntity["gsc_cityid"] = new EntityReference("gsc_syscity", city);

                if (province != Guid.Empty)
                    purchaseOrderEntity["gsc_provincestateid"] = new EntityReference("gsc_sysprovince", province);

                if (country != Guid.Empty)
                    purchaseOrderEntity["gsc_countryid"] = new EntityReference("gsc_syscountry", country);


                _organizationService.Update(purchaseOrderEntity);

            }

            _tracingService.Trace("Ended PopulateVendorDetails method..");
            return purchaseOrderEntity;


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
        public Entity GetLevel1ApproverEmails(Entity purchaseOrderEntity)
        {
            _tracingService.Trace("Started GetLevel1ApproverEmails method...");

            //Retrieve Purchase Order record
            EntityCollection purchaseOrderRecords = CommonHandler.RetrieveRecordsByOneValue(purchaseOrderEntity.LogicalName, "gsc_cmn_purchaseorderid", purchaseOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_purchaseorderpn", "gsc_potypeid" });

            if (purchaseOrderRecords != null || purchaseOrderRecords.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderRecords.Entities[0];

                var poTypeId = purchaseOrder.GetAttributeValue<EntityReference>("gsc_potypeid") != null
                    ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_potypeid").Id
                    : Guid.Empty;

                //Create filter for PO Type and Level 1 Approvers
                var purchaseOrderApproverConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_potypeid", ConditionOperator.Equal, poTypeId),
                    new ConditionExpression("gsc_approverlevel", ConditionOperator.Equal, 100000000)
                };

                EntityCollection contactRecords = CommonHandler.RetrieveRecordsByConditions("contact", purchaseOrderApproverConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "emailaddress1" });

                if (contactRecords != null && contactRecords.Entities.Count > 0)
                {
                    CreateEmail(purchaseOrder, contactRecords);
                    //Entity currentList = new Entity("list");
                    //currentList["listname"] = "Level 1 Approvers";
                    //currentList["createdfromcode"] = new OptionSetValue(2);
                    //Guid marketingListId = _organizationService.Create(currentList);

                    //List<Guid> memberListIds = new List<Guid>();

                    //foreach (Entity approver in contactRecords.Entities)
                    //{
                    //    memberListIds.Add(approver.Id);
                    //}

                    //AddListMembersListRequest addMemberRequest = new AddListMembersListRequest();
                    //addMemberRequest.ListId = marketingListId;
                    //addMemberRequest.MemberIds = memberListIds.ToArray();
                    //AddListMembersListResponse addMemberResponse = _organizationService.Execute(addMemberRequest) as AddListMembersListResponse;                    
                }
            }
            _tracingService.Trace("Ended GetLevel1ApproverEmails method...");
            return purchaseOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/21/2016
        public Entity GetLevel2ApproverEmails(Entity purchaseOrderEntity)
        {
            _tracingService.Trace("Started GetLevel2ApproverEmails method...");

            //Retrieve Purchase Order record
            EntityCollection purchaseOrderRecords = CommonHandler.RetrieveRecordsByOneValue(purchaseOrderEntity.LogicalName, "gsc_cmn_purchaseorderid", purchaseOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_purchaseorderpn", "gsc_potypeid" });

            if (purchaseOrderRecords != null && purchaseOrderRecords.Entities.Count > 0)
            {
                Entity purchaseOrder = purchaseOrderRecords.Entities[0];

                var poTypeId = purchaseOrder.GetAttributeValue<EntityReference>("gsc_potypeid") != null
                    ? purchaseOrder.GetAttributeValue<EntityReference>("gsc_potypeid").Id
                    : Guid.Empty;

                //Create filter for PO Type and Level 2 Approvers
                var purchaseOrderApproverConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_potypeid", ConditionOperator.Equal, poTypeId),
                    new ConditionExpression("gsc_approverlevel", ConditionOperator.Equal, 100000001)
                };

                EntityCollection contactRecords = CommonHandler.RetrieveRecordsByConditions("contact", purchaseOrderApproverConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "emailaddress1" });

                if (contactRecords != null && contactRecords.Entities.Count > 0)
                {
                    CreateEmail(purchaseOrder, contactRecords);
                }
            }
            _tracingService.Trace("Ended GetLevel2ApproverEmails method...");
            return purchaseOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/29/2016
        private void CreateEmail(Entity purchaseOrderEntity, EntityCollection recipients)
        {
            //Retrieve System Admininstrator contact
            EntityCollection sysAdmin = CommonHandler.RetrieveRecordsByOneValue("systemuser", "fullname", "System Administrator", _organizationService, null, OrderType.Ascending,
                new[] { "internalemailaddress" });

            if (sysAdmin != null || sysAdmin.Entities.Count > 0)
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
                email["description"] = "Please be notified that the Purchase Order : " + purchaseOrderEntity["gsc_purchaseorderpn"] + " has been approved.";
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
    }
}