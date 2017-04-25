using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;
using GSC.Rover.DMS.BusinessLogic.InventoryMovement;
using GSC.Rover.DMS.BusinessLogic.RequirementChecklist;
using GSC.Rover.DMS.BusinessLogic.PriceList;

namespace GSC.Rover.DMS.BusinessLogic.SalesOrder
{
    public class SalesOrderHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;
        private static readonly Object thisLock = new Object();

        public SalesOrderHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On : 2/12/2016
        /*Purpose: Replicate Quote fields into newly created Order record
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: Quote ID = quoteid
         *      Post/Update:
         *      Post/Create:
         * Primary Entity: Sales Order
         */
        public Entity ReplicateQuoteInfo(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ReplicateQuoteInfo method..");

            var quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;
            var productId = salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;

            _tracingService.Trace("Quote ID " + String.Concat(quoteId));

            //Retrieve Quote record from Quote ID field value
            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_dealerid", "gsc_branchid", "gsc_salesexecutiveid", "gsc_paymentmode", "gsc_productid", "gsc_vehiclecolorid1", "gsc_recordownerid",
                   "gsc_vehiclecolorid2", "gsc_vehiclecolorid3", "gsc_remarks", "gsc_vehicleunitprice", "gsc_vehicledetails", "customerid", "gsc_address",
                   "gsc_leadsourceid", "gsc_financingschemeid", "gsc_bankid", "gsc_freechattelfee", "gsc_insuranceid", "gsc_free",
                   "gsc_downpaymentamount", "gsc_downpaymentpercentage", "gsc_applytodppercentage", "gsc_applytouppercentage", "gsc_applytoafpercentage",
                   "gsc_applytodpamount", "gsc_applytoupamount", "gsc_applytoafamount", "gsc_netmonthlyamortization", "gsc_portaluserid",
                   "opportunityid", "gsc_vehicletype", "gsc_vehicleuse", "gsc_lessdiscount", "gsc_netdownpayment", "gsc_lessdiscountaf", "gsc_totalamountfinanced"});

            _tracingService.Trace("Quote records count " + String.Concat(quoteRecords.Entities.Count));

            if (quoteRecords != null && quoteRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieved {" + quoteRecords.Entities.Count + "}: " + "Retrieving Quote...");

                Entity quote = quoteRecords.Entities[0];

                //order information
                salesOrderEntity["opportunityid"] = quote.GetAttributeValue<EntityReference>("opportunityid") != null
                    ? quote.GetAttributeValue<EntityReference>("opportunityid")
                    : null;
                salesOrderEntity["gsc_leadsourceid"] = quote.GetAttributeValue<EntityReference>("gsc_leadsourceid") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_leadsourceid")
                    : null;
                salesOrderEntity["gsc_paymentmode"] = quote.Contains("gsc_paymentmode")
                    ? quote.GetAttributeValue<OptionSetValue>("gsc_paymentmode")
                    : null;
                salesOrderEntity["gsc_salesexecutiveid"] = quote.GetAttributeValue<EntityReference>("gsc_salesexecutiveid") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_salesexecutiveid")
                    : null;
                //Vehicle Information
                salesOrderEntity["gsc_productid"] = quote.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_productid")
                    : null;
                salesOrderEntity["gsc_vehiclecolorid1"] = quote.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1")
                    : null;
                salesOrderEntity["gsc_vehiclecolorid2"] = quote.GetAttributeValue<EntityReference>("gsc_vehiclecolorid2") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_vehiclecolorid2")
                    : null;
                salesOrderEntity["gsc_vehiclecolorid3"] = quote.GetAttributeValue<EntityReference>("gsc_vehiclecolorid3") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_vehiclecolorid3")
                    : null;
                salesOrderEntity["gsc_remarks"] = quote.Contains("gsc_remarks")
                    ? quote.GetAttributeValue<String>("gsc_remarks")
                    : String.Empty;
                //Quote Details
                salesOrderEntity["gsc_downpaymentamount"] = quote.Contains("gsc_downpaymentamount")
                    ? quote.GetAttributeValue<Money>("gsc_downpaymentamount")
                    : new Money(0);
                salesOrderEntity["gsc_downpaymentpercentage"] = quote.Contains("gsc_downpaymentpercentage")
                    ? quote.GetAttributeValue<Double>("gsc_downpaymentpercentage")
                    : 0.0;
                salesOrderEntity["gsc_downpaymentdiscount"] = quote.Contains("gsc_lessdiscount")
                    ? quote.GetAttributeValue<Money>("gsc_lessdiscount")
                    : new Money(0);
                salesOrderEntity["gsc_netdownpayment"] = quote.Contains("gsc_netdownpayment")
                    ? quote.GetAttributeValue<Money>("gsc_netdownpayment")
                    : new Money(0);
                salesOrderEntity["gsc_discountamountfinanced"] = quote.Contains("gsc_lessdiscountaf")
                    ? quote.GetAttributeValue<Money>("gsc_lessdiscountaf")
                    : new Money(0);
                salesOrderEntity["gsc_bankid"] = quote.GetAttributeValue<EntityReference>("gsc_bankid") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_bankid")
                    : null;
                salesOrderEntity["gsc_financingschemeid"] = quote.GetAttributeValue<EntityReference>("gsc_financingschemeid") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_financingschemeid")
                    : null;
                salesOrderEntity["gsc_freechattelfee"] = quote.GetAttributeValue<Boolean>("gsc_freechattelfee");
                salesOrderEntity["gsc_applytodppercentage"] = quote.Contains("gsc_applytodppercentage")
                    ? quote.GetAttributeValue<Double>("gsc_applytodppercentage")
                    : 0.0;
                salesOrderEntity["gsc_applytouppercentage"] = quote.Contains("gsc_applytouppercentage")
                    ? quote.GetAttributeValue<Double>("gsc_applytouppercentage")
                    : 0.0;
                salesOrderEntity["gsc_applytoafpercentage"] = quote.Contains("gsc_applytoafpercentage")
                    ? quote.GetAttributeValue<Double>("gsc_applytoafpercentage")
                    : 0.0;
                salesOrderEntity["gsc_applytodpamount"] = quote.Contains("gsc_applytodpamount")
                    ? quote.GetAttributeValue<Money>("gsc_applytodpamount")
                    : new Money(0);
                salesOrderEntity["gsc_applytoupamount"] = quote.Contains("gsc_applytoupamount")
                    ? quote.GetAttributeValue<Money>("gsc_applytoupamount")
                    : new Money(0);
                salesOrderEntity["gsc_applytoafamount"] = quote.Contains("gsc_applytoafamount")
                    ? quote.GetAttributeValue<Money>("gsc_applytoafamount")
                    : new Money(0);
                salesOrderEntity["gsc_insuranceid"] = quote.GetAttributeValue<EntityReference>("gsc_insuranceid") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_insuranceid")
                    : null;
                salesOrderEntity["gsc_vehicletypeid"] = quote.GetAttributeValue<EntityReference>("gsc_vehicletype") != null
                    ? quote.GetAttributeValue<EntityReference>("gsc_vehicletype")
                    : null;
                salesOrderEntity["gsc_vehicleuse"] = quote.GetAttributeValue<OptionSetValue>("gsc_vehicleuse") != null
                    ? quote.GetAttributeValue<OptionSetValue>("gsc_vehicleuse")
                    : null;

                salesOrderEntity["gsc_free"] = quote.GetAttributeValue<Boolean>("gsc_free");

                //Record Information
                salesOrderEntity["gsc_status"] = new OptionSetValue(100000000);
                salesOrderEntity["gsc_dealerid"] = quote.Contains("gsc_dealerid")
                    ? new EntityReference(quote.GetAttributeValue<EntityReference>("gsc_dealerid").LogicalName, quote.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                    : null;
                salesOrderEntity["gsc_branchid"] = quote.Contains("gsc_branchid")
                    ? new EntityReference(quote.GetAttributeValue<EntityReference>("gsc_branchid").LogicalName, quote.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                    : null;
                salesOrderEntity["gsc_recordownerid"] = quote.Contains("gsc_portaluserid")
                    ? new EntityReference(quote.GetAttributeValue<EntityReference>("gsc_recordownerid").LogicalName, new Guid(quote.GetAttributeValue<String>("gsc_portaluserid")))
                    : null;
            }

            //Populate Customer Information
            salesOrderEntity = GetCustomerInfo(salesOrderEntity);

            //Set Payment Summary values to zero
            salesOrderEntity = PopulatePaymentSummary(salesOrderEntity);

            _tracingService.Trace("Ended ReplicateQuoteInfo method..");

            return salesOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/14/2016
        public Entity ReplicateQuoteDiscount(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ReplicateQuoteDiscount method..");

            var quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;

            //Retrieve Quote Discount records
            EntityCollection quoteDiscountRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_quotediscount", "gsc_quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_quotediscountpn", "gsc_pricelistid", "gsc_description", "gsc_discountamount", "gsc_applypercentagetodp", "gsc_applypercentagetoaf", "gsc_applypercentagetoup", "gsc_applyamounttodp", "gsc_applyamounttoaf", "gsc_applyamounttoup" });

            if (quoteDiscountRecords != null || quoteDiscountRecords.Entities.Count > 0)
            {
                foreach (Entity quoteDiscount in quoteDiscountRecords.Entities)
                {
                    _tracingService.Trace("Retrieve Quote Discounts..");

                    Entity salesOrderDiscount = new Entity("gsc_cmn_salesorderdiscount");

                    salesOrderDiscount["gsc_salesorderid"] = new EntityReference(salesOrderEntity.LogicalName, salesOrderEntity.Id);
                    salesOrderDiscount["gsc_salesorderdiscountpn"] = quoteDiscount.GetAttributeValue<String>("gsc_quotediscountpn");
                    salesOrderDiscount["gsc_pricelistid"] = quoteDiscount.GetAttributeValue<EntityReference>("gsc_pricelistid") != null
                        ? new EntityReference("pricelevel", quoteDiscount.GetAttributeValue<EntityReference>("gsc_pricelistid").Id)
                        : null;
                    salesOrderDiscount["gsc_discountamount"] = quoteDiscount.GetAttributeValue<Money>("gsc_discountamount") != null
                        ? new Money(quoteDiscount.GetAttributeValue<Money>("gsc_discountamount").Value)
                        : new Money(Decimal.Zero);
                    salesOrderDiscount["gsc_description"] = quoteDiscount.GetAttributeValue<String>("gsc_description") != null
                        ? quoteDiscount.GetAttributeValue<String>("gsc_description")
                        : String.Empty;
                    salesOrderDiscount["gsc_applypercentagetodp"] = quoteDiscount.Contains("gsc_applypercentagetodp")
                        ? quoteDiscount.GetAttributeValue<Double>("gsc_applypercentagetodp")
                        : 0;
                    salesOrderDiscount["gsc_applypercentagetoaf"] = quoteDiscount.Contains("gsc_applypercentagetoaf")
                        ? quoteDiscount.GetAttributeValue<Double>("gsc_applypercentagetoaf")
                        : 0;
                    salesOrderDiscount["gsc_applypercentagetoup"] = quoteDiscount.Contains("gsc_applypercentagetoup")
                        ? quoteDiscount.GetAttributeValue<Double>("gsc_applypercentagetoup")
                        : 0;
                    salesOrderDiscount["gsc_applyamounttodp"] = quoteDiscount.GetAttributeValue<Money>("gsc_applyamounttodp") != null
                        ? quoteDiscount.GetAttributeValue<Money>("gsc_applyamounttodp")
                        : new Money(Decimal.Zero);
                    salesOrderDiscount["gsc_applyamounttoaf"] = quoteDiscount.GetAttributeValue<Money>("gsc_applyamounttoaf") != null
                        ? quoteDiscount.GetAttributeValue<Money>("gsc_applyamounttoaf")
                        : new Money(Decimal.Zero);
                    salesOrderDiscount["gsc_applyamounttoup"] = quoteDiscount.GetAttributeValue<Money>("gsc_applyamounttoup") != null
                        ? quoteDiscount.GetAttributeValue<Money>("gsc_applyamounttoup")
                        : new Money(Decimal.Zero);

                    _organizationService.Create(salesOrderDiscount);

                    _tracingService.Trace("Order Discount Created..");
                }
            }

            _tracingService.Trace("Ended ReplicateQuoteDiscount method..");
            return salesOrderEntity;
        }

        public Entity ReplicateQuoteCharges(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ReplicateQuoteCharges method..");

            var quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;

            //Retrieve Quote Charge records
            EntityCollection quoteChargeRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_quotecharge", "gsc_quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_quotechargepn", "gsc_chargesid", "gsc_description", "gsc_chargetype", "gsc_chargeamount", "gsc_actualcost", "gsc_free" });


            if (quoteChargeRecords != null || quoteChargeRecords.Entities.Count > 0)
            {
                foreach (Entity quoteCharge in quoteChargeRecords.Entities)
                {
                    _tracingService.Trace("Retrieve Quote Charges..");

                    Entity salesOrderCharge = new Entity("gsc_cmn_ordercharge");

                    salesOrderCharge["gsc_orderid"] = new EntityReference(salesOrderEntity.LogicalName, salesOrderEntity.Id);
                    salesOrderCharge["gsc_free"] = quoteCharge.GetAttributeValue<Boolean>("gsc_free");
                    salesOrderCharge["gsc_orderchargepn"] = quoteCharge.GetAttributeValue<String>("gsc_quotechargepn") != null
                        ? quoteCharge.GetAttributeValue<String>("gsc_quotechargepn")
                        : String.Empty;
                    salesOrderCharge["gsc_chargesid"] = quoteCharge.GetAttributeValue<EntityReference>("gsc_chargesid") != null
                        ? new EntityReference("gsc_cmn_charges", quoteCharge.GetAttributeValue<EntityReference>("gsc_chargesid").Id)
                        : null;
                    salesOrderCharge["gsc_chargetype"] = quoteCharge.GetAttributeValue<OptionSetValue>("gsc_chargetype") != null
                        ? quoteCharge.GetAttributeValue<OptionSetValue>("gsc_chargetype")
                        : null;
                    salesOrderCharge["gsc_description"] = quoteCharge.GetAttributeValue<String>("gsc_description") != null
                        ? quoteCharge.GetAttributeValue<String>("gsc_description")
                        : String.Empty;
                    salesOrderCharge["gsc_amount"] = quoteCharge.GetAttributeValue<Money>("gsc_amount") != null
                        ? new Money(quoteCharge.GetAttributeValue<Money>("gsc_amount").Value)
                        : new Money(Decimal.Zero);
                    salesOrderCharge["gsc_actualcost"] = quoteCharge.GetAttributeValue<Money>("gsc_actualcost") != null
                        ? new Money(quoteCharge.GetAttributeValue<Money>("gsc_actualcost").Value)
                        : new Money(Decimal.Zero);

                    _organizationService.Create(salesOrderCharge);

                    _tracingService.Trace("Order Charges Created..");
                }
            }

            _tracingService.Trace("Ended ReplicateQuoteCharges method..");

            return salesOrderEntity;
        }
        
        public void CheckifVehicleHasTax(Entity salesOrderEntity)
        {
            var vehicle = salesOrderEntity.Contains("gsc_productid")
                    ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty
                : Guid.Empty;

            EntityCollection vehicleCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", vehicle, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_taxrate", "gsc_taxid" });

            if (vehicleCollection != null && vehicleCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Vehicle is not null.");
                Entity vehicleEntity = vehicleCollection.Entities[0];

                _tracingService.Trace(vehicleEntity.GetAttributeValue<EntityReference>("gsc_taxid") + " " + vehicleEntity.Contains("gsc_taxrate"));

                if (vehicleEntity.GetAttributeValue<EntityReference>("gsc_taxid") == null || vehicleEntity.Contains("gsc_taxrate") == false)
                    throw new InvalidPluginExecutionException("Cannot proceed with your transaction.\n Please setup tax for Product Catalog.");

            }
        }

        public void CheckifCustomerHasTax(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started CheckifVehicleHasTax method.");

            var customer = salesOrderEntity.Contains("customerid")
                    ? salesOrderEntity.GetAttributeValue<EntityReference>("customerid") != null
                    ? salesOrderEntity.GetAttributeValue<EntityReference>("customerid")
                    : null
                    : null;

            EntityCollection customerCollection = null;
            if (customer != null)
            {
                customerCollection = CommonHandler.RetrieveRecordsByOneValue(customer.LogicalName, customer.LogicalName + "id", customer.Id, _organizationService, null, OrderType.Ascending,
                   new[] { "gsc_taxrate", "gsc_taxid" });
            }

            if (customerCollection != null && customerCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Customer is not null.");

                Entity customerEntity = customerCollection.Entities[0];
                if (customerEntity.GetAttributeValue<EntityReference>("gsc_taxid") == null || customerEntity.Contains("gsc_taxrate") == false)
                    throw new InvalidPluginExecutionException("Cannot proceed with your transaction.\n Please setup tax for Customer.");
            }
        }

        private Entity GetCustomerInfo(Entity salesOrderEntity)
        {
            var customer = salesOrderEntity.GetAttributeValue<EntityReference>("customerid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("customerid")
                : null;
            var customerID = String.Empty;
            var streetName = String.Empty;
            var cityName = String.Empty;
            var zipCode = String.Empty;
            var province = String.Empty;
            var country = String.Empty;
            var tin = String.Empty;
            var customerType = new OptionSetValue(100000000);

            //Retrieve Contact record from Customer ID field value
            if (customer != null && customer.LogicalName == "contact")
            {
                EntityCollection contactRecords = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", customer.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_tin", "gsc_countryid", "gsc_provinceid", "gsc_cityid", "address1_line1", "address1_postalcode", "gsc_customerid" });

                _tracingService.Trace("Contact records count " + String.Concat(contactRecords.Entities.Count));

                if (contactRecords != null && contactRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved {" + contactRecords.Entities.Count + "}: " + "Retrieving Contact...");

                    Entity contact = contactRecords.Entities[0];

                    customerID = contact.Contains("gsc_customerid")
                        ? contact.GetAttributeValue<String>("gsc_customerid")
                        : String.Empty;
                    streetName = contact.Contains("address1_line1")
                        ? contact.GetAttributeValue<String>("address1_line1")
                        : String.Empty;
                    cityName = contact.GetAttributeValue<EntityReference>("gsc_cityid") != null
                        ? contact.GetAttributeValue<EntityReference>("gsc_cityid").Name
                        : String.Empty;
                    zipCode = contact.Contains("address1_postalcode")
                        ? contact.GetAttributeValue<String>("address1_postalcode")
                        : String.Empty;
                    province = contact.GetAttributeValue<EntityReference>("gsc_provinceid") != null
                        ? contact.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                        : String.Empty;
                    country = contact.GetAttributeValue<EntityReference>("gsc_countryid") != null
                        ? contact.GetAttributeValue<EntityReference>("gsc_countryid").Name
                        : String.Empty;
                    tin = contact.Contains("gsc_tin") ? contact.GetAttributeValue<String>("gsc_tin") : String.Empty;
                }
            }
            else if (customer != null && customer.LogicalName == "account")
            {
                EntityCollection contactRecords = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", customer.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_tin", "gsc_countryid", "gsc_provinceid", "gsc_cityid", "address1_line1", "address1_postalcode", "accountnumber", "gsc_customertype" });

                _tracingService.Trace("Contact records count " + String.Concat(contactRecords.Entities.Count));

                if (contactRecords != null && contactRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved {" + contactRecords.Entities.Count + "}: " + "Retrieving Contact...");

                    Entity contact = contactRecords.Entities[0];

                    customerID = contact.Contains("accountnumber")
                        ? contact.GetAttributeValue<String>("accountnumber")
                        : String.Empty;
                    streetName = contact.Contains("address1_line1")
                        ? contact.GetAttributeValue<String>("address1_line1")
                        : String.Empty;
                    cityName = contact.GetAttributeValue<EntityReference>("gsc_cityid") != null
                        ? contact.GetAttributeValue<EntityReference>("gsc_cityid").Name
                        : String.Empty;
                    zipCode = contact.Contains("address1_postalcode")
                        ? contact.GetAttributeValue<String>("address1_postalcode")
                        : String.Empty;
                    province = contact.GetAttributeValue<EntityReference>("gsc_provinceid") != null
                        ? contact.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                        : String.Empty;
                    country = contact.GetAttributeValue<EntityReference>("gsc_countryid") != null
                        ? contact.GetAttributeValue<EntityReference>("gsc_countryid").Name
                        : String.Empty;
                    tin = contact.Contains("gsc_tin") ? contact.GetAttributeValue<String>("gsc_tin") : String.Empty;
                    customerType = contact.Contains("gsc_customertype")
                        ? new OptionSetValue(contact.GetAttributeValue<OptionSetValue>("gsc_customertype").Value + 1)
                        : customerType;
                }
            }

            if (customer != null)
            {
                //sales information
                salesOrderEntity["gsc_customerid"] = customerID;
                salesOrderEntity["gsc_customertype"] = customerType;
                salesOrderEntity["gsc_address"] = streetName + ", " + cityName + ", " + province + ", " + country + " " + zipCode;
                salesOrderEntity["gsc_tin"] = tin;
            }

            return salesOrderEntity;
        }

        public void PopulateCustomerInfo(Entity salesOrderEntity)
        {
            var customer = salesOrderEntity.GetAttributeValue<EntityReference>("customerid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("customerid")
                : null;

            Entity orderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
                new ColumnSet("gsc_customerid", "gsc_customertype", "gsc_address", "gsc_tin", "customerid"));
            orderToUpdate = GetCustomerInfo(orderToUpdate);
            _organizationService.Update(orderToUpdate);
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/8/2016
        /*Purpose: Replicate vehicle fields from product id field.
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: gsc_productid
         *      Post/Create: gsc_productid
         * Primary Entity: Sales Order
         */
        public Entity ReplicateVehicleDetails(Entity salesOrderEntity, String message)
        {
            _tracingService.Trace("Started ReplicateVehicleDetails method..");

            var productId = salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                : Guid.Empty;

            //Retrieve Product record using Product ID field value
            _tracingService.Trace("Retrieve Product Record..");
            EntityCollection productRecords = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_enginetype", "gsc_transmission", "gsc_grossvehicleweight", "gsc_pistondisplacement", "gsc_fueltype", "gsc_status", "gsc_sellprice",
                    "gsc_warrantyexpirydays", "gsc_warrantymileage", "gsc_othervehicledetails", "pricelevelid", "gsc_taxid", "gsc_taxrate", "gsc_vehiclemodelid", "gsc_modelcode", "gsc_optioncode" });

            if (productRecords != null && productRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieved {" + productRecords.Entities.Count + "}: " + "Retrieving Product...");

                Entity product = productRecords.Entities[0];

                _tracingService.Trace("Retrieve EngineType..");
                var engineType = product.Contains("gsc_enginetype")
                    ? product.GetAttributeValue<String>("gsc_enginetype") 
                    : String.Empty;

                var transmission = product.Contains("gsc_transmission")
                    ? product.FormattedValues["gsc_transmission"] 
                    : String.Empty;

                var grossVehicleWeight = product.Contains("gsc_grossvehicleweight")
                    ? product.GetAttributeValue<String>("gsc_grossvehicleweight") 
                    : String.Empty;

                var pistonDisplacement = product.Contains("gsc_pistondisplacement")
                    ? product.GetAttributeValue<String>("gsc_pistondisplacement") 
                    : String.Empty;

                var fuelType = product.Contains("gsc_fueltype")
                    ? product.FormattedValues["gsc_fueltype"] 
                    : String.Empty;

                var status = product.Contains("gsc_status")
                    ? product.FormattedValues["gsc_status"]
                    : String.Empty;

                var warrantyExpiryDays = product.Contains("gsc_warrantyexpirydays")
                    ? product.GetAttributeValue<String>("gsc_warrantyexpirydays") 
                    : String.Empty;

                var warrantyMileage = product.Contains("gsc_warrantymileage")
                    ? product.GetAttributeValue<String>("gsc_warrantymileage") 
                    : String.Empty;

                _tracingService.Trace("Retrieve Vehicle Details..");
                var otherVehicleDetails = product.Contains("gsc_othervehicledetails")
                    ? product.GetAttributeValue<String>("gsc_othervehicledetails") 
                    : String.Empty;

                var modelCode = product.Contains("gsc_modelcode")
                        ? product["gsc_modelcode"]
                        : String.Empty;

                var optionCode = product.Contains("gsc_optioncode")
                    ? product["gsc_optioncode"]
                    : String.Empty;

                String description = "Engine Type: " + engineType +
                        "\nModel Code: " + modelCode +
                        "\nOption Code: " + optionCode + 
                        "\nTransmission: " + transmission +
                        "\nWeight: " + grossVehicleWeight +
                        "\nDisplacement: " + pistonDisplacement +
                        "\nFuel: " + fuelType +
                        "\nStatus: " + status +
                        "\nWarranty Days: " + warrantyExpiryDays +
                        "\nWarranty Mileage: " + warrantyMileage +
                        "\nOthers: " + otherVehicleDetails;
                description = description.Remove(description.Length - 2, 2);

                var sellPrice = new Money(ComputeUnitPrice(product, salesOrderEntity));
                salesOrderEntity["gsc_vehicledetails"] = description;
                salesOrderEntity["gsc_vehicleunitprice"] = sellPrice;
                salesOrderEntity["gsc_unitprice"] = sellPrice;
                salesOrderEntity["gsc_vehiclebasemodelid"] = product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                    ? product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid")
                    : null;

                _tracingService.Trace("Update SalesOrder Entity..");
                if (message.Equals("Update"))
                {
                    _organizationService.Update(salesOrderEntity);
                }
            }

            _tracingService.Trace("Ended ReplicateVehicleDetails method..");
            return salesOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/9/2016
        //Modified By: Raphael Herrera, Modified On: 9/19/2016
        //              Change productsubstitute to vehicle Accessory
        /*Purpose: Replicate quote accessories to order accessories
         * Registration Details: 
         * Event/Message:
         *      Post/Update: 
         *      Post/Create: Sales Order
         * Primary Entity: Sales Order
         */
        public Entity ReplicateQuoteVehicleAccessories(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started RetrieveAndCreateVehicleFreebies method..");

            var quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;

            //Retrieve quote accessories related to quote id
            _tracingService.Trace("Retrieve Quote Accessories..");
            EntityCollection quoteAccessoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_quoteaccessory", "gsc_quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_productid", "gsc_free", "gsc_itemnumber", "gsc_standard" });

            _tracingService.Trace("Quote Accessory records retrieved: " + quoteAccessoryCollection.Entities.Count);

            foreach (Entity quoteAccessory in quoteAccessoryCollection.Entities)
            {
                var itemId = quoteAccessory.GetAttributeValue<EntityReference>("gsc_productid") != null
                      ? new EntityReference("product", quoteAccessory.GetAttributeValue<EntityReference>("gsc_productid").Id)
                      : null;
                var itemDescription = quoteAccessory.Contains("gsc_itemnumber")
                    ? quoteAccessory.GetAttributeValue<String>("gsc_itemnumber")
                    : String.Empty;

                CreateVehicleAccessories(salesOrderEntity, itemDescription, itemId, quoteAccessory.GetAttributeValue<Boolean>("gsc_standard"), quoteAccessory.GetAttributeValue<Boolean>("gsc_free"));
            }

            _tracingService.Trace("Exiting RetrieveAndCreateVehicleFreebies method..");
            return salesOrderEntity;
        }

        //Created By : Artum Ramos, Created On : 12/6/2016
        /*Purpose: Generate Accessories for the Corresponding Vehicle Model
         * Registration Details: 
         * Event/Message:
         *      Post/Create: 
         * Primary Entity: Sales Order
         */
        public Entity GenerateAccessoriesforVehicleModel(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started GenerateAccessoriesforVehicleModel Method...");

            var quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;

            if (quoteId == Guid.Empty)
            {
                var productId = salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty;

                _tracingService.Trace("Product ID " + String.Concat(productId)); 

                var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_productid", ConditionOperator.Equal, productId),
                        new ConditionExpression("gsc_free", ConditionOperator.Equal, true)
                    };

                //Retrieve related products from Product Relationship entity
                EntityCollection VehicleAccessoriesItems = CommonHandler.RetrieveRecordsByConditions("gsc_sls_vehicleaccessory", productConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_itemid", "gsc_vehicleaccessorypn", "gsc_productid" });

                if (VehicleAccessoriesItems != null && VehicleAccessoriesItems.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Vehicle Accessories");

                    foreach (var VehicleAccessories in VehicleAccessoriesItems.Entities)
                    {
                        var ItemNumber = VehicleAccessories.Contains("gsc_vehicleaccessorypn")
                            ? VehicleAccessories.GetAttributeValue<String>("gsc_vehicleaccessorypn")
                            : String.Empty;

                        var ItemDescription = VehicleAccessories.GetAttributeValue<EntityReference>("gsc_itemid") != null
                            ? VehicleAccessories.GetAttributeValue<EntityReference>("gsc_itemid")
                            : null;

                        CreateVehicleAccessories(salesOrderEntity, ItemNumber, ItemDescription, true, true);
                    }
                }
            }
            return salesOrderEntity;
        }

        private void CreateVehicleAccessories(Entity salesOrderEntity, String itemNumber, EntityReference itemId, Boolean standard, Boolean free)
        {
            _tracingService.Trace("Create Sale Order Accessory");
            Entity orderAccessory = new Entity("gsc_sls_orderaccessory");
            orderAccessory["gsc_itemnumber"] = itemNumber;
            orderAccessory["gsc_productid"] = itemId;
            orderAccessory["gsc_standard"] = standard;
            orderAccessory["gsc_free"] = free;
            orderAccessory["gsc_orderid"] = new EntityReference("salesorder", salesOrderEntity.Id);

            _organizationService.Create(orderAccessory);
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/9/2016
        //Modified By: Raphael Herrera, Modified On: 9/23/2016
        //Purpose of Modification: Changed items being deleted to order accessories
        public Entity DeleteExistingVehicleFreebies(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started DeleteExistingVehicleFreebies method..");

            if (salesOrderEntity.Attributes.Contains("gsc_productid"))
            {
                EntityCollection orderAccessoryCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_orderaccessory", "gsc_orderid", salesOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_orderaccessorypn" });

                if (orderAccessoryCollection != null && orderAccessoryCollection.Entities.Count > 0)
                {
                    foreach (var orderAccessory in orderAccessoryCollection.Entities)
                    {
                        _organizationService.Delete(orderAccessory.LogicalName, orderAccessory.Id);
                    }
                }

                //Call RetrieveAndCreateVehicleFreebies method
                return GenerateAccessoriesforVehicleModel(salesOrderEntity);
            }

            _tracingService.Trace("Ended DeleteExistingVehicleFreebies method..");
            return salesOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 3/15/2016
        /*Purpose: Compute for chattel fee amount.
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: gsc_bankid, gsc_productid
         *      Post/Create:
         * Primary Entity: Sales Order
         */
        public Entity SetChattelFeeAmount(Entity salesOrderEntity, String message)
        {
            _tracingService.Trace("Started SetChattelFeeAmount method..");

            var paymentmode = salesOrderEntity.Contains("gsc_paymentmode")
              ? salesOrderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
              : Decimal.Zero;

            if (paymentmode != 100000001)
                return null;

            var bankId = salesOrderEntity.GetAttributeValue<EntityReference>("gsc_bankid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_bankid").Id
                : Guid.Empty;

            Decimal unitPriceAmount = salesOrderEntity.GetAttributeValue<Money>("gsc_unitprice") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_unitprice").Value
                : Decimal.Zero;

            //Retrieve all Chattel Fee records with the same Bank
            EntityCollection chattelFeeRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_chattelfee", "gsc_bankid", bankId, _organizationService, "gsc_loanamount", OrderType.Ascending,
                new[] { "gsc_loanamount", "gsc_chattelfeeamount" });

            if (chattelFeeRecords != null && chattelFeeRecords.Entities.Count > 0 && salesOrderEntity.GetAttributeValue<Boolean>("gsc_freechattelfee") == false)
            {
                Int32 chattelFeeRecordsCount = chattelFeeRecords.Entities.Count;

                //Loop through all Chattel Fee records to match Loan Amount
                for (int x = 0; x <= chattelFeeRecordsCount - 2; x++)
                {
                    //if (unitPriceAmount >= (Decimal)chattelFeeRecords.Entities[x].GetAttributeValue<Money>("gsc_loanamount").Value && unitPriceAmount <= (Decimal)chattelFeeRecords.Entities[x+1].GetAttributeValue<Money>("gsc_loanamount").Value)
                    if (Enumerable.Range(((Int32)(Decimal)chattelFeeRecords.Entities[x].GetAttributeValue<Money>("gsc_loanamount").Value), ((Int32)(Decimal)chattelFeeRecords.Entities[x + 1].GetAttributeValue<Money>("gsc_loanamount").Value)).Contains((Int32)unitPriceAmount))
                    {
                        salesOrderEntity["gsc_chattelfee"] = new Money((Decimal)chattelFeeRecords.Entities[x].GetAttributeValue<Money>("gsc_chattelfeeamount").Value);
                        salesOrderEntity["gsc_chattelfeeeditable"] = new Money((Decimal)chattelFeeRecords.Entities[x].GetAttributeValue<Money>("gsc_chattelfeeamount").Value);
                    }
                }

                //Check retrieved Chattel Fee record count if odd or even
                Boolean chattelFeeRecordsOddOrEven = chattelFeeRecordsCount % 2 == 0;

                //If retrieved Chattel Fee record count is even, validate if Unit Price is greater than the last Loan Amount value
                if (!chattelFeeRecordsOddOrEven)
                {
                    if (unitPriceAmount > (Decimal)chattelFeeRecords.Entities[chattelFeeRecordsCount - 1].GetAttributeValue<Money>("gsc_loanamount").Value)
                    {
                        salesOrderEntity["gsc_chattelfee"] = new Money((Decimal)chattelFeeRecords.Entities[chattelFeeRecordsCount - 1].GetAttributeValue<Money>("gsc_chattelfeeamount").Value);
                        salesOrderEntity["gsc_chattelfeeeditable"] = new Money((Decimal)chattelFeeRecords.Entities[chattelFeeRecordsCount - 1].GetAttributeValue<Money>("gsc_chattelfeeamount").Value);
                    }
                }

                if (unitPriceAmount < (Decimal)chattelFeeRecords.Entities[0].GetAttributeValue<Money>("gsc_loanamount").Value)
                {
                    salesOrderEntity["gsc_chattelfee"] = new Money((Decimal)0.00);
                    salesOrderEntity["gsc_chattelfeeeditable"] = new Money((Decimal)0.00);
                }
            }
            else
            {
                salesOrderEntity["gsc_chattelfee"] = new Money(Decimal.Zero);
                salesOrderEntity["gsc_chattelfeeeditable"] = new Money(Decimal.Zero);
            }

            if (message.Equals("Update"))
            {
                _organizationService.Update(salesOrderEntity);
            }

            _tracingService.Trace("Ended SetChattelFeeAmount method..");
            return salesOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 9/15/2016
        /*Purpose: Replicate gsc_chattelfeeeditable value to gsc_chattelfee
         * Registration Details:
         * Event/Message:
         *      Post/Update: Chattel Fee (gsc_chattelfeeeditable)
         * Primary Entity: Sales Order
         */
        public Entity ReplicateEditableChattelFee(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ReplicateEditableChattelFee method..");

            //Return if Free Chattel Fee is checked
            if (salesOrderEntity.GetAttributeValue<Boolean>("gsc_freechattelfee") == true) { return null; }

            Decimal newChattelFeeAmount = salesOrderEntity.Contains("gsc_chattelfeeeditable")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_chattelfeeeditable").Value
                : Decimal.Zero;

            salesOrderEntity["gsc_chattelfee"] = new Money(newChattelFeeAmount);

            _organizationService.Update(salesOrderEntity);

            _tracingService.Trace("Ended ReplicateEditableChattelFee method..");
            return salesOrderEntity;
        }

        /*Purpose: Compute for less discount amount.
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: gsc_applytodpamount, gsc_applytoafamount, gsc_applytoupamount
         *      Post/Create:
         * Primary Entity: Sales Order
         */
        public Entity SetLessDiscountValues(Entity salesOrderEntity, String message)
        {
            _tracingService.Trace("Started SetLessDiscountValues method..");

            Decimal downpayment = salesOrderEntity.GetAttributeValue<Money>("gsc_applytodpamount") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_applytodpamount").Value
                : Decimal.Zero;

            Decimal amountFinanced = salesOrderEntity.GetAttributeValue<Money>("gsc_applytoafamount") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_applytoafamount").Value
                : Decimal.Zero;

            Decimal unitPrice = salesOrderEntity.GetAttributeValue<Money>("gsc_applytoupamount") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_applytoupamount").Value
                : Decimal.Zero;

            salesOrderEntity["gsc_discount"] = new Money(unitPrice);
            salesOrderEntity["gsc_discountamountfinanced"] = new Money(amountFinanced);
            salesOrderEntity["gsc_downpaymentdiscount"] = new Money(downpayment);

            //Recompute related fields
            var netprice = ComputeNetPrice(salesOrderEntity);
            salesOrderEntity["gsc_netprice"] = new Money(netprice);

            salesOrderEntity = ComputeVAT(salesOrderEntity);

            var paymentmode = salesOrderEntity.Contains("gsc_paymentmode")
              ? salesOrderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
              : Decimal.Zero;

            var amountfinanced = Decimal.Zero;
            var netdp = Decimal.Zero;
            Decimal downPaymentAmount = Decimal.Zero;

            //Financing
            if (paymentmode == 100000001 || paymentmode == 100000002)
            {
                netdp = ComputeNetDownPayment(salesOrderEntity);
                downPaymentAmount = ComputeDownPaymentAmount(salesOrderEntity);

                salesOrderEntity["gsc_downpaymentamount"] = new Money(downPaymentAmount);
                salesOrderEntity["gsc_netdownpayment"] = new Money(netdp);
                salesOrderEntity["gsc_downpayment"] = new Money(netdp);

                amountfinanced = ComputeAmountFinanced(salesOrderEntity);
                salesOrderEntity["gsc_amountfinanced"] = new Money(amountfinanced);
                salesOrderEntity["gsc_totalamountfinanced"] = new Money(amountfinanced);

                if (paymentmode == 100000001)
                    salesOrderEntity = SetTotalCashOutlayAmount(salesOrderEntity, "create");
            }
            else
                salesOrderEntity = SetTotalCashOutlayAmount(salesOrderEntity, "create");

            Entity orderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
                           new ColumnSet(true));

            orderToUpdate["gsc_discount"] = salesOrderEntity["gsc_discount"];
            orderToUpdate["gsc_discountamountfinanced"] = salesOrderEntity["gsc_discountamountfinanced"];
            orderToUpdate["gsc_downpaymentdiscount"] = salesOrderEntity["gsc_downpaymentdiscount"];
            orderToUpdate["gsc_downpaymentamount"] = new Money(downPaymentAmount);
            orderToUpdate["gsc_netdownpayment"] = new Money(netdp);
            orderToUpdate["gsc_downpayment"] = new Money(netdp);
            orderToUpdate["gsc_netprice"] = new Money(netprice);
            orderToUpdate["gsc_totalcashoutlay"] = salesOrderEntity.GetAttributeValue<Money>("gsc_totalcashoutlay");
            orderToUpdate["gsc_amountfinanced"] = new Money(amountfinanced);
            orderToUpdate["gsc_totalamountfinanced"] = new Money(amountfinanced);
            orderToUpdate["gsc_vatablesales"] = salesOrderEntity["gsc_vatablesales"];
            orderToUpdate["gsc_vatexemptsales"] = salesOrderEntity["gsc_vatexemptsales"];
            orderToUpdate["gsc_zeroratedsales"] = salesOrderEntity["gsc_zeroratedsales"];
            orderToUpdate["gsc_totalsales"] = salesOrderEntity["gsc_totalsales"];
            orderToUpdate["gsc_vatamount"] = salesOrderEntity["gsc_vatamount"];
            orderToUpdate["gsc_totalamountdue"] = salesOrderEntity["gsc_totalamountdue"];

           /* if (downpayment == 0 && amountFinanced == 0 && unitPrice == 0)
            {
                _tracingService.Trace("Get Discount Values from Order Discount...");

                //Retrieve Quote Discount records with the same Order
                EntityCollection orderDiscountRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_salesorderdiscount", "gsc_salesorderid", salesOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_applyamounttodp", "gsc_applyamounttoaf", "gsc_applyamounttoup" });

                if (orderDiscountRecords != null && orderDiscountRecords.Entities.Count > 0)
                {
                    foreach (var orderDiscount in orderDiscountRecords.Entities)
                    {
                        _tracingService.Trace("Compute Total Discounts ... ");

                        downpayment += orderDiscount.Contains("gsc_applyamounttodp")
                            ? orderDiscount.GetAttributeValue<Money>("gsc_applyamounttodp").Value
                            : Decimal.Zero;

                        amountFinanced += orderDiscount.Contains("gsc_applyamounttoaf")
                            ? orderDiscount.GetAttributeValue<Money>("gsc_applyamounttoaf").Value
                            : Decimal.Zero;

                        unitPrice += orderDiscount.Contains("gsc_applyamounttoup")
                            ? orderDiscount.GetAttributeValue<Money>("gsc_applyamounttoup").Value
                            : Decimal.Zero;
                    }
                }

            }

            salesOrderEntity["gsc_discount"] = new Money(unitPrice);
            salesOrderEntity["gsc_discountamountfinanced"] = new Money(amountFinanced);
            salesOrderEntity["gsc_downpaymentdiscount"] = new Money(downpayment);

            salesOrderEntity = ValidateDiscounts(salesOrderEntity);

            var netdp = ComputeNetDownPayment(salesOrderEntity);
            salesOrderEntity["gsc_netdownpayment"] = new Money(netdp);
            salesOrderEntity["gsc_downpayment"] = new Money(netdp);

            if (message.Equals("Update"))
            {
                _organizationService.Update(salesOrderEntity);
            }*/

            _organizationService.Update(orderToUpdate);

            DeleteExistingMonthlyAmortizationRecords(orderToUpdate);

            _tracingService.Trace("Ended SetLessDiscountValues method..");
            return salesOrderEntity;
        }

        //Created By: Raphael Herrera, Created On: 12-22-2016
        //Validates Values in Discounts to be aligned with Total Discount Amount
        private Entity ValidateDiscounts(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ValidateDiscounts Method...");
            Decimal totalDiscountAmount = salesOrderEntity.Contains("gsc_totaldiscountamount") ? salesOrderEntity.GetAttributeValue<Money>("gsc_totaldiscountamount").Value : 0;
            Double applyToDP = salesOrderEntity.Contains("gsc_applytodppercentage") ? salesOrderEntity.GetAttributeValue<Double>("gsc_applytodppercentage") : 0;
            Double applyToAF = salesOrderEntity.Contains("gsc_applytoafpercentage") ? salesOrderEntity.GetAttributeValue<Double>("gsc_applytoafpercentage") : 0;
            Double applyToUP = salesOrderEntity.Contains("gsc_applytouppercentage") ? salesOrderEntity.GetAttributeValue<Double>("gsc_applytouppercentage") : 0;

            Decimal DPAmount = totalDiscountAmount * ((decimal)applyToDP / 100);
            Decimal AFAmount = totalDiscountAmount * ((decimal)applyToAF / 100);
            Decimal UPAmount = totalDiscountAmount * ((decimal)applyToUP / 100);

            salesOrderEntity["gsc_applytodpamount"] = new Money(DPAmount);
            salesOrderEntity["gsc_applytoafamount"] = new Money(AFAmount);
            salesOrderEntity["gsc_applytoupamount"] = new Money(UPAmount);
            salesOrderEntity["gsc_downpaymentdiscount"] = new Money(DPAmount);
            salesOrderEntity["gsc_discountamountfinanced"] = new Money(AFAmount);
            salesOrderEntity["gsc_discount"] = new Money(UPAmount);


            _tracingService.Trace("Ending ValidateDiscounts Method...");
            return salesOrderEntity;
        }

        //Created By: Leslie Baliguat, Created On; 3/29/2016
        public Entity SetDates(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started SetDates method ...");

            var quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
            ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
            : Guid.Empty;

            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
            new[] { "createdon" });

            if (quoteRecords != null && quoteRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Quote Createdon");

                salesOrderEntity["gsc_quotedate"] = quoteRecords.Entities[0].GetAttributeValue<DateTime>("createdon");
            }


            salesOrderEntity["gsc_orderdate"] = salesOrderEntity.GetAttributeValue<DateTime>("createdon");

            _tracingService.Trace("Ended SetDates method ...");

            return salesOrderEntity;
        }

        //Created By: Leslie Baliguat, Created On: 3/30/2016
        /*Purpose: Replicate Details of Selected Insurance to Sales Order 
         * Registration Details:
         * Event/Message: 
         *      Post/Create: 
         *      Post/Update: gsc_insuranceid
         * Primary Entity: Order
         */
        public Entity ReplicateInsuranceDetails(Entity salesOrderEntity, String message)
        {
            _tracingService.Trace("Started ReplicateInsuranceDetails method ...");

            if (salesOrderEntity.Contains("gsc_insuranceid") && salesOrderEntity.GetAttributeValue<EntityReference>("gsc_insuranceid") != null)
            {
                var insuranceid = salesOrderEntity.GetAttributeValue<EntityReference>("gsc_insuranceid").Id;
                var isfree = salesOrderEntity.GetAttributeValue<Boolean>("gsc_free");

                EntityCollection insuranceRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_insurance", "gsc_cmn_insuranceid", insuranceid,
                    _organizationService, null, OrderType.Ascending, new[] { "gsc_vehicletypeid", "gsc_vehicleuse", "gsc_totalpremium", "gsc_providercompanyid"});

                if (insuranceRecords != null && insuranceRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Insurance Details");

                    Entity insuranceEntity = insuranceRecords.Entities[0];
                    var providerCompany = insuranceEntity.GetAttributeValue<EntityReference>("gsc_providercompanyid");
                    var totalpremium = insuranceEntity.GetAttributeValue<Money>("gsc_totalpremium") != null
                        ? insuranceEntity.GetAttributeValue<Money>("gsc_totalpremium").Value
                        : Decimal.Zero;
                    var insurance = new Money(0);

                    salesOrderEntity["gsc_providercompanyid"] = providerCompany != null
                        ? new EntityReference(providerCompany.LogicalName, providerCompany.Id)
                        : null;
                    salesOrderEntity["gsc_totalpremium"] = new Money(totalpremium);
                    salesOrderEntity["gsc_originaltotalpremuim"] = new Money(totalpremium);
                    if (isfree == false)
                    {
                        _tracingService.Trace("Insurance is not Free");
                        insurance = new Money(totalpremium);
                    }

                    salesOrderEntity["gsc_insurance"] = insurance;


                    if (message.Equals("Update"))
                    {
                        _tracingService.Trace("Updating Insurance Details");

                        Entity orderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
                            new ColumnSet("gsc_totalpremium", "gsc_originaltotalpremuim", "gsc_insurance"));

                        orderToUpdate["gsc_providercompanyid"] = providerCompany != null
                            ? new EntityReference(providerCompany.LogicalName, providerCompany.Id)
                            : null;
                        orderToUpdate["gsc_totalpremium"] = new Money(totalpremium);
                        orderToUpdate["gsc_originaltotalpremuim"] = new Money(totalpremium);
                        orderToUpdate["gsc_insurance"] = insurance;

                        _organizationService.Update(orderToUpdate);
                    }
                }
            }
            else
            {
                if (message.Equals("Update"))
                {
                    ClearInsuranceInformation(salesOrderEntity);
                    DeleteInsuranceCoverage(salesOrderEntity);
                }
            }

            _tracingService.Trace("Ended ReplicateInsuranceDetails method ...");

            return salesOrderEntity;
        }

        private void ClearInsuranceInformation(Entity salesOrderEntity)
        {
            Entity orderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
              new ColumnSet("gsc_providercompanyid", "gsc_originaltotalpremuim", "gsc_totalpremium", "gsc_free"));

            orderToUpdate["gsc_providercompanyid"] = null;
            orderToUpdate["gsc_originaltotalpremuim"] = null;
            orderToUpdate["gsc_totalpremium"] = null;
            orderToUpdate["gsc_free"] = false;

            _organizationService.Update(orderToUpdate);
        }

        private void DeleteInsuranceCoverage(Entity salesOrderEntity)
        {
            EntityCollection coverageCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_ordercoverageavailable", "gsc_orderid", salesOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_orderid" });

            if (coverageCollection != null && coverageCollection.Entities.Count > 0)
            {
                foreach (var coverageEntity in coverageCollection.Entities)
                {
                    _tracingService.Trace("Deleting Coverage...");
                    _organizationService.Delete(coverageEntity.LogicalName, coverageEntity.Id);
                }
                _tracingService.Trace("Coverage Deleted...");
            }
        }

        //Created By: Leslie Baliguat, Created On: 3/31/2016
        /*Purpose: Copy Coverage Availalble Records of the Insurance selected and Create Order Coverage available Records 
         *             associated to the Sales Order
         * Registration Details:
         * Event/Message: 
         *      Post/Create: 
         *      Post/Update: gsc_insuranceid
         * Primary Entity: Order
         */
        public Entity CreateCoverageAvailable(Entity salesOrderEntity, String message)
        {
            //delete existing records before creating new
            DeleteExistingCoverage(salesOrderEntity);

            if (salesOrderEntity.Contains("gsc_insuranceid") && salesOrderEntity.GetAttributeValue<EntityReference>("gsc_insuranceid") != null)
            {
                _tracingService.Trace("Started CreateCoverageAvailable method ...");

                Entity orderCoverageAvailable = new Entity();

                var insuranceid = salesOrderEntity.GetAttributeValue<EntityReference>("gsc_insuranceid").Id;
                //Retrieve Covergae Available 
                EntityCollection coverageAvailableListRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_insurancecoverage", "gsc_insuranceid", insuranceid,
                     _organizationService, null, OrderType.Ascending, new[] { "gsc_insurancecoveragepn", "gsc_suminsured", "gsc_premium", "gsc_insuranceid" });

                if (coverageAvailableListRecords != null && coverageAvailableListRecords.Entities.Count > 0)
                {
                    foreach (var coverageAvailable in coverageAvailableListRecords.Entities)
                    {
                        _tracingService.Trace("Setting up Order Coverage Available Record");

                        orderCoverageAvailable = new Entity("gsc_cmn_ordercoverageavailable");
                        orderCoverageAvailable["gsc_orderid"] = new EntityReference(salesOrderEntity.LogicalName, salesOrderEntity.Id);
                        orderCoverageAvailable["gsc_ordercoverageavailablepn"] = coverageAvailable.Contains("gsc_insurancecoveragepn")
                                                                                ? coverageAvailable.GetAttributeValue<String>("gsc_insurancecoveragepn")
                                                                                : "";
                        orderCoverageAvailable["gsc_suminsured"] = coverageAvailable.Contains("gsc_suminsured")
                                                                   ? coverageAvailable.GetAttributeValue<Money>("gsc_suminsured")
                                                                   : new Money(0);
                        orderCoverageAvailable["gsc_premium"] = coverageAvailable.Contains("gsc_premium")
                                                                    ? coverageAvailable.GetAttributeValue<Money>("gsc_premium")
                                                                    : new Money(0);

                        _organizationService.Create(orderCoverageAvailable);

                        _tracingService.Trace("Order Coverage Available Record Created");
                    }
                }

                _tracingService.Trace("Ended CreateCoverageAvailable method ...");

                return orderCoverageAvailable;
            }

            return null;
        }

        //Delete Existing Coverage associated to Order
        private void DeleteExistingCoverage(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started DeleteExistingCoverage method ...");

            var coverageConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_orderid", ConditionOperator.Equal, salesOrderEntity.Id)
                    };

            EntityCollection coverageAvailable = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_ordercoverageavailable", coverageConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_orderid" });

            if (coverageAvailable != null && coverageAvailable.Entities.Count > 0)
            {
                foreach (var orderCoverage in coverageAvailable.Entities)
                {
                    _tracingService.Trace("Deleting record ...");

                    _organizationService.Delete(orderCoverage.LogicalName, orderCoverage.Id);
                }
            }

            _tracingService.Trace("Ended DeleteExistingCoverage method ...");
        }

        //Created By: leslie Baliguat, Created On: 3/31/2016
        /*Purpose: If Insurance Total Premium was updated, update Insurance Charges in Payment Summary.
         *         If free in Insurance was checked, update Insurance Charges in Payment Suammry to Zero(0).
         *         If free in Insurance was unchecked, update Insurance Charges in Payment Suammry to the value of Insurance Total Premium.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_totalpremium, gsc_free
         * Primary Entity: Order
         */
        public Money UpdateInsurance(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started FreeInsurance method ...");

            var isfree = salesOrderEntity.GetAttributeValue<Boolean>("gsc_free");

            if (salesOrderEntity.Contains("gsc_totalinsurancecharges"))
            {
                Decimal insurance = Decimal.Zero;

                insurance = salesOrderEntity.Contains("gsc_totalinsurancecharges")
                        ? salesOrderEntity.GetAttributeValue<Money>("gsc_totalinsurancecharges").Value
                        : Decimal.Zero;

                Entity orderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
                new ColumnSet("gsc_insurance", "gsc_paymentmode", "gsc_netprice", "gsc_othercharges", "gsc_vatablesales", "gsc_vatexemptsales",
                    "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount", "gsc_totalamountdue", "gsc_totalcashoutlay", "gsc_netdownpayment",
                    "gsc_chattelfee", "gsc_reservation", "gsc_productid", "customerid"));

                orderToUpdate["gsc_insurance"] = new Money(insurance);

                orderToUpdate = ComputeVAT(orderToUpdate);

                orderToUpdate = SetTotalCashOutlayAmount(orderToUpdate, "Create");

                _organizationService.Update(orderToUpdate);

                _tracingService.Trace("Insurance field Updated.");

                return salesOrderEntity.GetAttributeValue<Money>("gsc_insurance");
            }
            return new Money(Decimal.Zero);
        }

        //TO BE DELETED
        /*//Created By : Jerome Anthony Gerero, Created On : 3/31/2016
        public Entity SetAddAccessoriesAmount(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started SetAddAccessoriesAmount method..");

            Decimal totalAccessoriesAmount = 0;

            //Retrieve Order Product records using Sales Order ID
            EntityCollection orderProductRecords = CommonHandler.RetrieveRecordsByOneValue("salesorderdetail", "salesorderid", salesOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "priceperunit" });

            if (orderProductRecords != null && orderProductRecords.Entities.Count > 0)
            {
                foreach (Entity orderProduct in orderProductRecords.Entities)
                {
                    totalAccessoriesAmount += orderProduct.GetAttributeValue<Money>("priceperunit") != null
                        ? orderProduct.GetAttributeValue<Money>("priceperunit").Value
                        : Decimal.Zero;
                }
                salesOrderEntity["gsc_accessories"] = new Money(totalAccessoriesAmount);
            }
            else
            {
                salesOrderEntity["gsc_accessories"] = new Money(Decimal.Zero);
            }
            _organizationService.Update(salesOrderEntity);

            _tracingService.Trace("Ended SetAddAccessoriesAmount method..");
            return salesOrderEntity;
        }*/

        //Created By : Jerome Anthony Gerero, Created On : 3/31/2016
        public Entity SetVehicleColorAmount(Entity salesOrderEntity, String message)
        {
            _tracingService.Trace("Started SetVehicleColorAmount method..");

            var vehicleColorId = salesOrderEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1").Id
                : Guid.Empty;

            //Retrieve Vehicle Color records using Vehicle Color ID
            EntityCollection vehicleColorRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_vehiclecolor", "gsc_cmn_vehiclecolorid", vehicleColorId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_additionalprice" });

            if (vehicleColorRecords != null && vehicleColorRecords.Entities.Count > 0)
            {
                Entity vehicleColor = vehicleColorRecords.Entities[0];

                salesOrderEntity["gsc_colorprice"] = vehicleColor.Contains("gsc_additionalprice")
                    ? new Money(vehicleColor.GetAttributeValue<Money>("gsc_additionalprice").Value)
                    : new Money(Decimal.Zero);
            }
            else
            {
                salesOrderEntity["gsc_colorprice"] = new Money(Decimal.Zero);
            }

            if (message.Equals("Update"))
            {
                _organizationService.Update(salesOrderEntity);
            }

            _tracingService.Trace("Ended SetVehicleColorAmount method..");
            return salesOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/6/2016
        public Entity DeleteExistingMonthlyAmortizationRecords(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started DeleteExistingMonthlyAmortizationRecords method..");

            EntityCollection salesOrderMonthlyAmortizationRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_ordermonthlyamortization", "gsc_orderid", salesOrderEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_financingtermid" });

            if (salesOrderMonthlyAmortizationRecords != null || salesOrderMonthlyAmortizationRecords.Entities.Count > 0)
            {
                foreach (var salesOrderMonthlyAmortization in salesOrderMonthlyAmortizationRecords.Entities)
                {
                    _organizationService.Delete(salesOrderMonthlyAmortization.LogicalName, salesOrderMonthlyAmortization.Id);
                }
                salesOrderEntity["gsc_netmonthlyamortization"] = new Money(0);
                _organizationService.Update(salesOrderEntity);
            }

            //Call CreateMonthlyAmortization method if Financing Scheme field is not null
            if (salesOrderEntity.GetAttributeValue<EntityReference>("gsc_financingschemeid") != null)
            {
                return CreateMonthlyAmortization(salesOrderEntity);
            }
            _tracingService.Trace("Ended DeleteExistingMonthlyAmortizationRecords method..");
            return null;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/5/2016
        private Entity CreateMonthlyAmortization(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started SetOrderMonthlyAmortizationAmount method..");

            var financingSchemeId = CommonHandler.GetEntityReferenceIdSafe(salesOrderEntity, "gsc_financingschemeid");

            //Retrieve Financing Scheme Detail record from Financing Scheme field
            EntityCollection financingSchemeDetailRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_financingschemedetails", "gsc_financingschemeid", financingSchemeId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_financingtermid", "gsc_addonrate", "gsc_downpaymentfrom", "gsc_downpaymentto", "gsc_zerointerest", "gsc_dealerincome", "gsc_interestrate" });

            if (financingSchemeDetailRecords != null && financingSchemeDetailRecords.Entities.Count > 0)
            {
                Entity monthlyAmortization = new Entity("gsc_sls_ordermonthlyamortization");

                foreach (var financingSchemeDetail in financingSchemeDetailRecords.Entities)
                {
                    var financingTermId = CommonHandler.GetEntityReferenceIdSafe(financingSchemeDetail, "gsc_financingtermid");



                    monthlyAmortization["gsc_orderid"] = new EntityReference("salesorder", salesOrderEntity.Id);
                    monthlyAmortization["gsc_financingtermid"] = new EntityReference("gsc_sls_financingterm", financingTermId);
                    monthlyAmortization["gsc_ordermonthlyamortizationpn"] = string.Format("{0:n0}", ComputeForMonthlyAmortization(salesOrderEntity, financingSchemeDetail));

                    _organizationService.Create(monthlyAmortization);

                }
                return monthlyAmortization;
            }
            _tracingService.Trace("Ended SetOrderMonthlyAmortizationAmount method..");

            return salesOrderEntity;
        }

        private double ComputeForMonthlyAmortization(Entity salesOrderEntity, Entity schemeEntity)
        {
            var amountFinancedDiscount = salesOrderEntity.Contains("gsc_applytoafpercentage")
                ? salesOrderEntity.GetAttributeValue<double>("gsc_applytoafpercentage")
                : 0;

            var monthlyAmortization = 0.0;

            if (amountFinancedDiscount != 0)
            {
                if (CheckifZIP(salesOrderEntity, schemeEntity))
                {//There is discount for amount financed but AOR is Zero or Tagged as ZIP
                    _tracingService.Trace("There is discount for amount financed but AOR is Zero or Tagged as ZIP");

                    monthlyAmortization = NormalFormula(salesOrderEntity, schemeEntity);
                }
                else
                {//There is discount for amount financed
                    _tracingService.Trace("There is discount for amount financed");
                    monthlyAmortization = PMTFormula(salesOrderEntity, schemeEntity);
                }
            }
            else
            { //There is no discount for amount financed 
                _tracingService.Trace("There is no discount for amount financed");
                monthlyAmortization = NormalFormula(salesOrderEntity, schemeEntity);
            }

            return Math.Round(monthlyAmortization, 2);
        }

        private bool CheckifZIP(Entity salesOrderEntity, Entity schemeEntity)
        {
            //if isZIP checkbox is checked, check if Downpayment is with in the range of DPFrom and DPTo of Finacing Scheme Detail
            if (schemeEntity.GetAttributeValue<bool>("gsc_zerointerest"))
            {
                _tracingService.Trace("Zip = Yes");

                var dpPercent = salesOrderEntity.Contains("gsc_downpaymentpercentage")
                ? salesOrderEntity.GetAttributeValue<double>("gsc_downpaymentpercentage")
                : 0.0;

                var dpFrom = schemeEntity.Contains("gsc_downpaymentfrom")
                ? schemeEntity.GetAttributeValue<double>("gsc_downpaymentfrom")
                : 0.0;

                var dpTo = schemeEntity.Contains("gsc_downpaymentto")
                ? schemeEntity.GetAttributeValue<double>("gsc_downpaymentto")
                : 0.0;

                if (dpPercent >= dpFrom && dpPercent <= dpTo)
                {
                    _tracingService.Trace("DownPayment with in the range.");
                    return true;
                }
            }

            return false;
        }

        /*Monthly Amortization = PMT((Interest Rate/Per Annum,Number of terms-((Amount Financed*(1+Dealer Income Rate))-AF Discount)) 
        * This formula applies when there is discount applied for Amount Financed.*/
        private double PMTFormula(Entity salesOrderEntity, Entity schemeEntity)
        {
            _tracingService.Trace("Compute WithAFDiscount.");

            double amountfinanced = salesOrderEntity.Contains("gsc_amountfinanced")
                ? double.Parse(salesOrderEntity.GetAttributeValue<Money>("gsc_amountfinanced").Value.ToString())
                : 0;

            var afDiscount = salesOrderEntity.Contains("gsc_discountamountfinanced")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_discountamountfinanced").Value
                : 0;

            var term = schemeEntity.GetAttributeValue<EntityReference>("gsc_financingtermid") != null
                ? Int32.Parse(schemeEntity.GetAttributeValue<EntityReference>("gsc_financingtermid").Name)
                : 0;

            double dealerRate = schemeEntity.Contains("gsc_dealerincome")
                ? schemeEntity.GetAttributeValue<double>("gsc_dealerincome")
                : 0;

            double interestRate = schemeEntity.Contains("gsc_interestrate")
                ? schemeEntity.GetAttributeValue<double>("gsc_interestrate")
                : 0;


            double PV = (amountfinanced * (1 + (dealerRate / 100))) - (double)afDiscount;

            double rate = interestRate / 100;

            if (rate == 0)
            {
                _tracingService.Trace("WithAFDiscount Computed.");
                return PV / term;
            }

            _tracingService.Trace("WithAFDiscount Computed.");

            return ((PV * (rate / 12)) / (1 - Math.Pow((1 + (rate / 12)), (-1 * term))));
        }

        /*Monthly Amortization =  (Total Amount Financed x (1+AOR)/ Financing Terms
         * This formula applies when there is no discount applied for Amount Financed.
         this also applies when AOR is zero and tagged as ZIP in the Financing Scheme setup 
         * even if there is a discount applied for Amount Financed.*/
        private double NormalFormula(Entity salesOrderEntity, Entity schemeEntity)
        {
            _tracingService.Trace("Compute ZIP_ZeroAOR_NoAFDiscount.");

            var amountfinanced = salesOrderEntity.Contains("gsc_amountfinanced")
                ? double.Parse(salesOrderEntity.GetAttributeValue<Money>("gsc_amountfinanced").Value.ToString())
                : 0;

            var aor = schemeEntity.Contains("gsc_addonrate")
                ? schemeEntity.GetAttributeValue<double>("gsc_addonrate")
                : 0.0;

            var term = schemeEntity.GetAttributeValue<EntityReference>("gsc_financingtermid") != null
                ? Int32.Parse(schemeEntity.GetAttributeValue<EntityReference>("gsc_financingtermid").Name)
                : 0;

            _tracingService.Trace(amountfinanced + "");

            _tracingService.Trace("ZIP_ZeroAOR_NoAFDiscount Computed.");
            return (amountfinanced * (1 + (aor / 100))) / term;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/7/2016
        public Entity SetNetPriceAmount(Entity salesOrderEntity, String message)
        {
            _tracingService.Trace("Started SetNetPriceAmount method..");

            Decimal unitPrice = salesOrderEntity.GetAttributeValue<Money>("gsc_unitprice") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_unitprice").Value
                : Decimal.Zero;

            Decimal colorPrice = salesOrderEntity.GetAttributeValue<Money>("gsc_colorprice") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_colorprice").Value
                : Decimal.Zero;

            Decimal freightAndHandling = salesOrderEntity.GetAttributeValue<Money>("gsc_freightandhandling") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_freightandhandling").Value
                : Decimal.Zero;

            Decimal discountAmount = salesOrderEntity.GetAttributeValue<Money>("gsc_discount") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_discount").Value
                : Decimal.Zero;

            var ccAddOnsPrice = salesOrderEntity.GetAttributeValue<Money>("gsc_ccaddons") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_ccaddons").Value
                : 0;

            Decimal totalPrice = unitPrice + colorPrice + freightAndHandling + ccAddOnsPrice;

            if (totalPrice > discountAmount)
            {
                Decimal netPrice = totalPrice - discountAmount;

                if (netPrice > 0)
                {
                    salesOrderEntity["gsc_netprice"] = new Money(netPrice);
                }
                else
                {
                    salesOrderEntity["gsc_netprice"] = new Money(Decimal.Zero);
                }
            }
            else
            {
                salesOrderEntity["gsc_netprice"] = new Money(Decimal.Zero);
            }

            Int32 paymentMode = salesOrderEntity.Contains("gsc_paymentmode")
              ? salesOrderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
              : 0;

            Decimal amountfinanced = Decimal.Zero;

            if (paymentMode == 100000001 || paymentMode == 100000002)
            {
                amountfinanced = ComputeAmountFinanced(salesOrderEntity);
                salesOrderEntity["gsc_amountfinanced"] = new Money(amountfinanced);
                salesOrderEntity["gsc_totalamountfinanced"] = new Money(amountfinanced);
            }
            else if (paymentMode == 100000000 || paymentMode == 100000003)
            {
                salesOrderEntity["gsc_amountfinanced"] = new Money(0);
                salesOrderEntity["gsc_totalamountfinanced"] = new Money(0);
            }

            //ADDED BY: JGC_12192016
            salesOrderEntity = ComputeVAT(salesOrderEntity);
            //END

            if (message.Equals("Update"))
            {
                _organizationService.Update(salesOrderEntity);
            }

            _tracingService.Trace("Ended SetNetPriceAmount method..");
            return salesOrderEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 4/7/2016
        //Modified By : Jerome Anthony Gerero, Modified On : 12/19/2016
        //Purpose : Deduct reservation fee from total cash outlay amount
        public Entity SetTotalCashOutlayAmount(Entity salesOrderEntity, String message)
        {
            _tracingService.Trace("Started SetTotalCashOutlayAmount method..");

            Decimal netDownPaymentAmount = salesOrderEntity.GetAttributeValue<Money>("gsc_netdownpayment") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_netdownpayment").Value
                : Decimal.Zero;

            Decimal chattelFeeAmount = salesOrderEntity.GetAttributeValue<Money>("gsc_chattelfee") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_chattelfee").Value
                : Decimal.Zero;

            Decimal insuranceAmount = salesOrderEntity.GetAttributeValue<Money>("gsc_insurance") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_insurance").Value
                : Decimal.Zero;

            Decimal chargesAmount = salesOrderEntity.GetAttributeValue<Money>("gsc_othercharges") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_othercharges").Value
                : Decimal.Zero;

            Decimal reservationFee = salesOrderEntity.GetAttributeValue<Money>("gsc_reservation") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_reservation").Value
                : Decimal.Zero;

            Decimal totalAmountDue = salesOrderEntity.GetAttributeValue<Money>("gsc_totalamountdue") != null
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_totalamountdue").Value
                : Decimal.Zero;

            Int32 paymentMode = salesOrderEntity.Contains("gsc_paymentmode")
                ? salesOrderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                : 0;
            Decimal totalCashOutlay = 0;

            //financing
            if (paymentMode == 100000001)
            {
                totalCashOutlay = netDownPaymentAmount + chattelFeeAmount + insuranceAmount + chargesAmount;

                if (totalCashOutlay > reservationFee)
                {
                    totalCashOutlay = totalCashOutlay - reservationFee;
                }
            }
            //cash
            else
                totalCashOutlay = totalAmountDue - reservationFee;

            if (totalCashOutlay > 0)
            {
                salesOrderEntity["gsc_totalcashoutlay"] = new Money(totalCashOutlay);
            }

            if (message.Equals("Update"))
            {
                _organizationService.Update(salesOrderEntity);
            }

            _tracingService.Trace("Started SetTotalCashOutlayAmount method..");
            return salesOrderEntity;
        }

        //Created By: Leslie Baliguat, Created On: 3/3/2016
        /*Purpose: Delete Requirement Checklist Records when Bank ID was updated
         *          and Create Requirement Checklist Records
         * Modified By: Raphael Herrera, Modified On: 11/15/2016
         * Purpose of Modification: Transferred deletion of existing requirement checklist to a separate method.          
         * Registration Details:
         * Event/Message: 
         *      Post/Create: 
         *      Post/Update: Bank Id = gsc_bankid
         * Primary Entity: Order
         */
        public Entity CreateRequirementChecklist(Entity orderEntity, string message)
        {
            if (orderEntity.Contains("gsc_bankid") && orderEntity.GetAttributeValue<EntityReference>("gsc_bankid") != null)
            {
                _tracingService.Trace("Started CreateRequirementChecklist method - Bank id is not null ...");

                var bankid = orderEntity.GetAttributeValue<EntityReference>("gsc_bankid").Id;

                var DocumentCondition = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_bankid", ConditionOperator.Equal, bankid)
                    };
                
                EntityCollection Document = CommonHandler.RetrieveRecordsByConditions("gsc_sls_documentchecklist", DocumentCondition, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_documentid", "gsc_documentchecklistpn", "gsc_customertype", "gsc_mandatory", "gsc_documenttype" });

                if (Document != null || Document.Entities.Count > 0)
                {
                    foreach (var documentrecord in Document.Entities)
                    {
                        _tracingService.Trace("Creating Requirement Checklist ...");

                        Entity requirementEntity = new Entity("gsc_sls_requirementchecklist");
                        requirementEntity["gsc_orderid"] = new EntityReference("salesorder", orderEntity.Id);
                        requirementEntity["gsc_bankid"] = new EntityReference("gsc_sls_bank", bankid);
                        requirementEntity["gsc_documentchecklistid"] = new EntityReference("gsc_sls_documentchecklist", documentrecord.Id);
                        requirementEntity["gsc_requirementchecklistpn"] = documentrecord["gsc_documentchecklistpn"];
                        requirementEntity["gsc_mandatory"] = documentrecord.GetAttributeValue<Boolean>("gsc_mandatory");
                        requirementEntity["gsc_documenttype"] = documentrecord.GetAttributeValue<Boolean>("gsc_documenttype");
                        requirementEntity["gsc_predefined"] = true;
                        //Added by: Jessica Casupanan, Added On: 1/5/2017                       
                        requirementEntity["gsc_documentname"] = documentrecord["gsc_documentchecklistpn"];
                        //End
                        //Edited by: Raphael Herrera, Edited On: 5/4/2016
                        //added gsc_predefined in fields created

                        _organizationService.Create(requirementEntity);
                    }
                    
                }
            }
            if(message == "Create")
                CreateInternalDocumentChecklist(orderEntity);
            

            _tracingService.Trace("Ended CreateRequirementChecklist method ...");

            return orderEntity;
        }

        //Created By: Raphael Herrera, Created On: 3/14/2017
        /*Purpose: Delete Requirement Checklist Records when Bank ID was updated. Takes pre image bankid as parameter.
         */
        private void CreateInternalDocumentChecklist(Entity orderEntity)
        {
            var internalDocumentCondition = new List<ConditionExpression>
            {
                new ConditionExpression("gsc_documenttype", ConditionOperator.Equal, true),
                new ConditionExpression("gsc_default", ConditionOperator.Equal, true)
            };
            EntityCollection documentCollection = CommonHandler.RetrieveRecordsByConditions("gsc_sls_document", internalDocumentCondition, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_documenttype", "gsc_documentpn" });

            _tracingService.Trace("Document records retrieved: " + documentCollection.Entities.Count);
            if (documentCollection.Entities.Count > 0)
            {
                foreach (Entity documentEntity in documentCollection.Entities)
                {
                    _tracingService.Trace("Creating internal document requirement checklist...");
                    Entity requirementEntity = new Entity("gsc_sls_requirementchecklist");
                    requirementEntity["gsc_orderid"] = new EntityReference("salesorder", orderEntity.Id);
                    requirementEntity["gsc_requirementchecklistpn"] = documentEntity["gsc_documentpn"];
                    requirementEntity["gsc_documentid"] = new EntityReference("gsc_sls_document", documentEntity.Id);
                    requirementEntity["gsc_mandatory"] = true;
                    requirementEntity["gsc_documenttype"] = documentEntity.GetAttributeValue<Boolean>("gsc_documenttype");
                    requirementEntity["gsc_documentname"] = documentEntity["gsc_documentpn"];

                    _organizationService.Create(requirementEntity);
                    _tracingService.Trace("Created document record...");
                }

            }
        }

        //Created By: Raphael Herrera, Created On: 11/15/2016
        /*Purpose: Delete Requirement Checklist Records when Bank ID was updated. Takes pre image bankid as parameter.
         * Registration Details:
         * Event/Message: 
         *      Post/Create: 
         *      Post/Update: gsc_bankid
         * Primary Entity: Order
         */
        public void DeleteExistingRequirementChecklist(Entity orderEntity)
        {
            _tracingService.Trace("Started DeleteExistingRequirementChecklist Method...");
            var bankId = orderEntity.Contains("gsc_bankid") ? orderEntity.GetAttributeValue<EntityReference>("gsc_bankid").Id
                : Guid.Empty;

            var requirementCondition = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_orderid", ConditionOperator.Equal, orderEntity.Id),
                    new ConditionExpression("gsc_bankid", ConditionOperator.Equal, bankId)
                };

            EntityCollection requirementCollection = CommonHandler.RetrieveRecordsByConditions("gsc_sls_requirementchecklist", requirementCondition, _organizationService, null, OrderType.Ascending,
            new[] { "gsc_orderid", "gsc_bankid", "gsc_predefined" });

            RequirementChecklistHandler requirementHandler = new RequirementChecklistHandler(_organizationService, _tracingService);
            _tracingService.Trace("Requirement Checklist records retrieved: " + requirementCollection.Entities.Count);
            if (requirementCollection.Entities.Count > 0)
            {
                foreach (Entity requirementEntity in requirementCollection.Entities)
                {
                    requirementHandler.ValidateDelete(requirementEntity, "Update");
                    _tracingService.Trace("Deleted requirement checklist...");
                }
            }

            _tracingService.Trace("Ending DeleteExistingRequirementChecklist Method...");
        }


        //Created By: Raphael Herrera, Created On: 4/29/2016
        /*Purpose: Set Status of order to For Allocation. Parameter is Guid to be updated.
         * Registration Details:
         * Event/Message: 
         *      Post/Create:
         *      Post/Update: gsc_status = 'For Allocation'
         * Primary Entity: Order
         */
        public void SetStatus(Guid salesOrderGuid)
        {
            _tracingService.Trace("Starting SetStatus method..");

            Entity salesOrderEntity = _organizationService.Retrieve("salesorder", salesOrderGuid, new ColumnSet("gsc_status"));
            _tracingService.Trace("Retrieved sales order entity to update..");

            salesOrderEntity["gsc_status"] = new OptionSetValue(100000002);

            _organizationService.Update(salesOrderEntity);
            _tracingService.Trace("Update successful. Ending SetStatus method..");

        }

        //Created By: Leslie Baliguat, Created On: 5/10/2016
        //Modified By: Artum Ramos, Modified On: 12/27/2016
        /*Purpose: Set Request AllocationDate in Sales Order when Request for Vehicle Allocation was clicked
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_isrequestforallocation
         * Primary Entity: Order
         */
        public void SetRequestAllocationDate(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started SetRequestAllocationDate method");

            var isrequested = salesOrderEntity.GetAttributeValue<Boolean>("gsc_isrequestforallocation");

            _tracingService.Trace(isrequested.ToString());

            if (isrequested == true)
            {
                _tracingService.Trace("Pass the condition is equal True");
                String today = DateTime.Today.ToString("MM-dd-yyyy");

                _tracingService.Trace("Retrieve Sales Order Entity");
                Entity orderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
                    new ColumnSet("gsc_requestedallocationdate"));
                _tracingService.Trace("gsc_requestallocationdate is equal to date today");
                orderToUpdate["gsc_requestedallocationdate"] = Convert.ToDateTime(today);

                _tracingService.Trace("update gsc_requestedallocationdate fields");
                _organizationService.Update(orderToUpdate);
                _tracingService.Trace("Finish update requesallocation date");
            }

            _tracingService.Trace("Ended SetRequestAllocationDate method");
        }

        //Created By: leslie Baliguat, Created On: 5/17/2016
        /*Purpose: Vehicle Allocation Process, When Process Button was clicked in Sales Order,
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_isrequestforallocation
         * Primary Entity: Order
         */
        public Entity AllocateVehicle(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started AllocateVehicle Method.");

            lock (thisLock)
            {
                _tracingService.Trace("Object not locked");

                Guid inventoryId = salesOrderEntity.Contains("gsc_inventoryidtoallocate")
                    ? new Guid(salesOrderEntity.GetAttributeValue<String>("gsc_inventoryidtoallocate"))
                    : Guid.Empty;

                String today = DateTime.Today.ToString("MM-dd-yyyy");

                EntityCollection inventoryRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_inventory", "gsc_iv_inventoryid", inventoryId,
                    _organizationService, null, OrderType.Ascending, new[] { "gsc_color", "gsc_csno", "gsc_engineno", "gsc_modelcode", "gsc_modelyear",
                        "gsc_optioncode", "gsc_productionno", "gsc_vin", "gsc_productquantityid", "gsc_status", "gsc_siteid", "gsc_productid", "gsc_basemodelid"});
            
                if (inventoryRecords != null && inventoryRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved Inventory Details"); 

                    Entity inventoryEntity = inventoryRecords.Entities[0];

                    var isAllocated = CheckifAllocated(inventoryEntity);

                    //if (!isAllocated)
                   // {
                        _tracingService.Trace("Proceed in Allocation ...");

                        Entity allocatedVehicle = new Entity("gsc_iv_allocatedvehicle");
                        allocatedVehicle["gsc_modelyear"] = inventoryEntity.GetAttributeValue<String>("gsc_modelyear");
                        allocatedVehicle["gsc_color"] = inventoryEntity.GetAttributeValue<String>("gsc_color");
                        allocatedVehicle["gsc_csno"] = inventoryEntity.GetAttributeValue<String>("gsc_csno");
                        allocatedVehicle["gsc_engineno"] = inventoryEntity.GetAttributeValue<String>("gsc_engineno");
                        allocatedVehicle["gsc_modelcode"] = inventoryEntity.GetAttributeValue<String>("gsc_modelcode");
                        allocatedVehicle["gsc_optioncode"] = inventoryEntity.GetAttributeValue<String>("gsc_optioncode");
                        allocatedVehicle["gsc_productionno"] = inventoryEntity.GetAttributeValue<String>("gsc_productionno");
                        allocatedVehicle["gsc_vin"] = inventoryEntity.GetAttributeValue<String>("gsc_vin");
                        allocatedVehicle["gsc_vehicleallocateddate"] = Convert.ToDateTime(today);
                        allocatedVehicle["gsc_vehicleallocationage"] = 0;
                        allocatedVehicle["gsc_inventoryid"] = new EntityReference(inventoryEntity.LogicalName, inventoryEntity.Id);
                        allocatedVehicle["gsc_orderid"] = new EntityReference(salesOrderEntity.LogicalName, salesOrderEntity.Id);
                        _organizationService.Create(allocatedVehicle);

                        _tracingService.Trace("Allocated Vehicle Created ...");

                        InventoryMovementHandler inventoryMovementHandler = new InventoryMovementHandler(_organizationService, _tracingService);
                        inventoryMovementHandler.UpdateInventoryStatus(inventoryEntity, 100000001);
                        Entity productQuantityEntity = inventoryMovementHandler.UpdateProductQuantity(inventoryEntity, 0, -1, 1, 0, 0, 0, 0, 0);

                        Entity salesOrderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
                          new ColumnSet("gsc_status", "gsc_vehicleallocateddate", "name", "gsc_status"));
                        salesOrderToUpdate["gsc_status"] = new OptionSetValue(100000003);
                        salesOrderToUpdate["gsc_vehicleallocateddate"] = Convert.ToDateTime(today);
                        _organizationService.Update(salesOrderToUpdate);

                        _tracingService.Trace("Sales Order Updated ...");

                        // Create Inventory History Log
                        inventoryMovementHandler.CreateInventoryQuantityAllocated(salesOrderToUpdate, inventoryEntity, productQuantityEntity, salesOrderToUpdate.GetAttributeValue<string>("name"),
                            DateTime.UtcNow, "Allocated", Guid.Empty, 100000001);

                        _tracingService.Trace("Ended AllocateVehicle Method.");
                        return allocatedVehicle;
                   // }
                }
            }
            return null;
        }

        //Check if Inventory Item Selected was already allocated/still available
        private Boolean CheckifAllocated(Entity inventoryEntity)
        {
            var ConditionList = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_inventoryid", ConditionOperator.Equal, inventoryEntity.Id)
                };

            EntityCollection allocatedRecords = CommonHandler.RetrieveRecordsByConditions("gsc_iv_allocatedvehicle", ConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_inventoryid" });

            if (allocatedRecords != null && allocatedRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Inventory Item already tagged in AllocatedVehicle");
                return true;
            }

            _tracingService.Trace("Inventory Item not yet allocated");
            return false;
        }

        //Created By: Leslie Baliguat
        /*Purpose: Compute Net Price 
         * Being called by the other methods when net price needs to be recomputed */
        public Decimal ComputeNetPrice(Entity orderEntity)
        {
            _tracingService.Trace("Started ComputeNetPrice method - Here...");

            var lessdiscount = orderEntity.GetAttributeValue<Money>("gsc_discount") != null
                ? orderEntity.GetAttributeValue<Money>("gsc_discount").Value
                : 0;

            var unitprice = orderEntity.GetAttributeValue<Money>("gsc_unitprice") != null
                ? orderEntity.GetAttributeValue<Money>("gsc_unitprice").Value
                : 0;

            var colorprice = orderEntity.GetAttributeValue<Money>("gsc_colorprice") != null
                ? orderEntity.GetAttributeValue<Money>("gsc_colorprice").Value
                : 0;

            var freightPrice = orderEntity.GetAttributeValue<Money>("gsc_freightandhandling") != null
                ? orderEntity.GetAttributeValue<Money>("gsc_freightandhandling").Value
                : 0;

            var ccAddOnsPrice = orderEntity.GetAttributeValue<Money>("gsc_ccaddons") != null
                ? orderEntity.GetAttributeValue<Money>("gsc_ccaddons").Value
                : 0;

            var totalprice = unitprice + colorprice + freightPrice + ccAddOnsPrice;

            if (totalprice != 0)
            {
                var netprice = totalprice - lessdiscount;

                if (netprice < 0)
                {
                    _tracingService.Trace("Ended ComputeNetPrice method...");
                    return 0;
                }

                else
                {
                    _tracingService.Trace("Ended ComputeNetPrice method...");
                    return netprice;
                }
            }

            _tracingService.Trace("Ended ComputeNetPrice method...");

            return 0;
        }

        //Created By: Raphael Herrera, Created On:  6/24/2016
        /*Purpose: Computes for values of VAT related fields (VATable Sales, VAT-Exempt Sales, Zero Rated Sales,
        * Total Sales, VAT Amount, Total Amount Due)
        *       
       * Registration Details:
       * Event/Message: 
       *      Pre/Create: 
       *      Post/Update:
       * Primary Entity: Order
       */
        public Entity ComputeVAT(Entity salesOrderEntity)
        {
            //Modified by: JGC_12202016
            _tracingService.Trace("Started ComputeVAT method...");

            #region Get customer tax rate and vehicle tax rate
            var customer = salesOrderEntity.Contains("customerid")
                   && salesOrderEntity.GetAttributeValue<EntityReference>("customerid") != null
                   ? salesOrderEntity.GetAttributeValue<EntityReference>("customerid")
                   : null;

            var vehicle = salesOrderEntity.Contains("gsc_productid")
                    && salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty;


            EntityCollection customerCollection = null;
            EntityCollection vehicleCollection = null;

            if (customer != null)
            {
                customerCollection = CommonHandler.RetrieveRecordsByOneValue(customer.LogicalName, customer.LogicalName + "id", customer.Id, _organizationService, null, OrderType.Ascending,
                   new[] { "gsc_taxrate", "gsc_taxid" });
            }

            if (vehicle != null)
            {
                vehicleCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", vehicle, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_taxrate", "gsc_taxid", "pricelevelid" });
            }

            if ((vehicleCollection != null && vehicleCollection.Entities.Count > 0) && (customerCollection != null && customerCollection.Entities.Count > 0))
            {
                _tracingService.Trace("Vehicle and Customer are not null.");

                Entity customerEntity = customerCollection.Entities[0];
                Entity vehicleEntity = vehicleCollection.Entities[0];

                if (customerEntity.GetAttributeValue<EntityReference>("gsc_taxid") == null || customerEntity.Contains("gsc_taxrate") == false)
                    throw new InvalidPluginExecutionException("Cannot proceed with your transaction.\n Please setup tax for Customer.");

                if (vehicleEntity.GetAttributeValue<EntityReference>("gsc_taxid") == null || vehicleEntity.Contains("gsc_taxrate") == false)
                    throw new InvalidPluginExecutionException("Cannot proceed with your transaction.\n Please setup tax for Product Catalog.");

                var customerTaxRate = (decimal)customerEntity.GetAttributeValue<Double>("gsc_taxrate");

                if (customerTaxRate != 0)
                    customerTaxRate = customerTaxRate / 100;

                var vehicleTaxRate = (decimal)vehicleEntity.GetAttributeValue<Double>("gsc_taxrate");

                if (vehicleTaxRate != 0)
                    vehicleTaxRate = vehicleTaxRate / 100;

                decimal taxCategory = 0;

                EntityCollection taxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_taxmaintenance", "gsc_cmn_taxmaintenanceid", CommonHandler.GetEntityReferenceIdSafe(customerEntity, "gsc_taxid"), _organizationService, null, OrderType.Ascending,
                new[] { "gsc_taxcategory" });

                if (taxCollection != null && taxCollection.Entities.Count > 0)
                {
                    Entity taxEntity = taxCollection.Entities[0];
                    taxCategory = taxEntity.GetAttributeValue<OptionSetValue>("gsc_taxcategory").Value;
                }
            #endregion

                Int32 paymentMode = salesOrderEntity.Contains("gsc_paymentmode")
                    ? salesOrderEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                    : 0;
                Decimal netPrice = salesOrderEntity.Contains("gsc_netprice")
                    ? salesOrderEntity.GetAttributeValue<Money>("gsc_netprice").Value
                    : Decimal.Zero;
                Decimal otherChargesAmount = salesOrderEntity.Contains("gsc_othercharges")
                    ? salesOrderEntity.GetAttributeValue<Money>("gsc_othercharges").Value
                    : Decimal.Zero;
                Decimal insuranceCharge = salesOrderEntity.Contains("gsc_insurance")
                    ? salesOrderEntity.GetAttributeValue<Money>("gsc_insurance").Value
                    : Decimal.Zero;
                Decimal sales = Decimal.Zero;

                //Financing
                if (paymentMode == 100000001)
                {
                    sales = (netPrice) / (Decimal)(1 + vehicleTaxRate);
                }
                //Cash/BPO/Company PO
                else
                {
                    //sales = (netPrice + otherChargesAmount + insuranceCharge) / (Decimal)(1 + vehicleTaxRate);
                    sales = (netPrice) / (Decimal)(1 + vehicleTaxRate);//changed as per bug#15957. Modified 4-10-17
                }


                if (taxCategory == 100000000)//VATable
                {
                    salesOrderEntity["gsc_vatablesales"] = new Money(sales);
                    salesOrderEntity["gsc_vatexemptsales"] = new Money(0);
                    salesOrderEntity["gsc_zeroratedsales"] = new Money(0);
                    salesOrderEntity["gsc_totalsales"] = new Money(sales);
                    salesOrderEntity["gsc_vatamount"] = new Money(Math.Round(sales * customerTaxRate, 2));
                    salesOrderEntity["gsc_totalamountdue"] = new Money(Math.Round(sales + (sales * customerTaxRate), 2));
                    _tracingService.Trace("Tax Category is VATable...");
                }
                else if (taxCategory == 100000002)//vat exempt
                {
                    salesOrderEntity["gsc_vatablesales"] = new Money(0);
                    salesOrderEntity["gsc_vatexemptsales"] = new Money(sales);
                    salesOrderEntity["gsc_zeroratedsales"] = new Money(0);
                    salesOrderEntity["gsc_totalsales"] = new Money(sales);
                    salesOrderEntity["gsc_vatamount"] = new Money(0);
                    salesOrderEntity["gsc_totalamountdue"] = new Money(sales);
                    _tracingService.Trace("Tax category is VAT exempt...");
                }
                else if (taxCategory == 100000001)//Zero Rated
                {
                    salesOrderEntity["gsc_vatablesales"] = new Money(0);
                    salesOrderEntity["gsc_vatexemptsales"] = new Money(0);
                    salesOrderEntity["gsc_zeroratedsales"] = new Money(sales);
                    salesOrderEntity["gsc_totalsales"] = new Money(sales);
                    salesOrderEntity["gsc_vatamount"] = new Money(0);
                    salesOrderEntity["gsc_totalamountdue"] = new Money(sales);
                    _tracingService.Trace("Tax Category is Zero Rated...");
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("Vehicle or Customer are null.");
            }
            _tracingService.Trace("Ending ComputeVAT method...");
            return salesOrderEntity;
        }

        //Created By: Jerome Anthony Gerero
        /*Purpose: Compute Down Payment Amount = Net Price * Down Payment Percentage / 100
         * Being called by the public methods when down payment needs to be computed. */
        private Decimal ComputeDownPaymentAmount(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ComputeDownPaymentAmount method...");
            
            Decimal netPrice = salesOrderEntity.Contains("gsc_netprice")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_netprice").Value
                : 0;
            Double downPaymentPercentage = salesOrderEntity.Contains("gsc_downpaymentpercentage")
                ? salesOrderEntity.GetAttributeValue<Double>("gsc_downpaymentpercentage")
                : 0.0;
            Decimal downPaymentAmount = Decimal.Zero;

            if (downPaymentPercentage != 0.0 || downPaymentPercentage != null)
            {
                downPaymentAmount = netPrice * ((Decimal)downPaymentPercentage / 100);
            }
            
            _tracingService.Trace("Started ComputeDownPaymentAmount method...");            
            return downPaymentAmount;
        }

        //Created By: Leslie Baliguat
        /*Purpose: Compute Net Downpayment = downpayment - discount
         * Being called by the public methods when net downpayment needs to be recomputed */
        private Decimal ComputeNetDownPayment(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ComputeNetDownPayment method...");            

            var lessdiscount = salesOrderEntity.Contains("gsc_downpaymentdiscount")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_downpaymentdiscount").Value
                : 0;
            var downpayment = salesOrderEntity.Contains("gsc_downpaymentamount")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_downpaymentamount").Value
                : 0;
            var netdp = new Decimal(0);

            downpayment = ComputeDownPaymentAmount(salesOrderEntity);

            if (downpayment != 0)
            {
                netdp = lessdiscount > downpayment ? 0 : downpayment - lessdiscount; 
            }
            else
            {
                netdp = downpayment;
            }

            _tracingService.Trace("Ended ComputeNetDownPayment method...");

            return netdp;
        }

        //Created By: Leslie Baliguat
        //Modified By : Jerome Anthony Gerero, Modified On : 12/21/2016
        /*Purpose: ComputeAmount Financed = netprice - net downpayment
         * Modification purpose : Change the computation to netprice - downpayment
         * Being called by the public methods when acmount financed needs to be recomputed */
        public Decimal ComputeAmountFinanced(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ComputeAmountFinanced method...");

            var downpayment = salesOrderEntity.Contains("gsc_downpaymentamount")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_downpaymentamount").Value
                : 0;
            var netprice = salesOrderEntity.Contains("gsc_netprice")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_netprice").Value
                : 0;

            //var amountfinanced = netprice - grossdp;
            var amountfinanced = netprice - downpayment;

            _tracingService.Trace("Ended ComputeAmountFinanced method...");

            return amountfinanced;
        }

        //Created By: Leslie Baliguat
        /*Purpose: Compute Net Amount Financed = amount financed - discount
         * Being called by the public methods when net amount financed needs to be recomputed */
        public Decimal ComputeNetAmountFinanced(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ComputeNetAmountFinanced method...");

            var amountfinanced = salesOrderEntity.Contains("gsc_amountfinanced")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_amountfinanced").Value
                : 0;
            var lessdiscount = salesOrderEntity.Contains("gsc_discountamountfinanced")
                ? salesOrderEntity.GetAttributeValue<Money>("gsc_discountamountfinanced").Value
                : 0;
            var netamountfinanced = amountfinanced;

            _tracingService.Trace("Ended ComputeNetAmountFinanced method...");

            return netamountfinanced;
        }

        //Created By: Jerome Anthony Gerero, Created On 7/28/2016
        /*Purpose: Replicate Quote Monthly Amortization records
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Update:
         *      Post/Create: Sales Order Id = salesorderid
         * Primary Entity: Sales Order
         */
        public Entity ReplicateQuoteMonthlyAmortization(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ReplicateQuoteMonthlyAmortization method...");

            Guid quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;

            //Retrieve Quote Monthly Amortization records
            EntityCollection quoteMontlyAmortizationRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_quotemonthlyamortization", "gsc_quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_isselected", "gsc_financingtermid", "gsc_quotemonthlyamortizationpn" });

            if (quoteMontlyAmortizationRecords != null && quoteMontlyAmortizationRecords.Entities.Count > 0)
            {
                foreach (Entity quoteMonthlyAmortization in quoteMontlyAmortizationRecords.Entities)
                {
                    Entity salesOrderMonthlyAmortization = new Entity("gsc_sls_ordermonthlyamortization");

                    salesOrderMonthlyAmortization["gsc_selected"] = quoteMonthlyAmortization.GetAttributeValue<Boolean>("gsc_isselected");
                    salesOrderMonthlyAmortization["gsc_orderid"] = new EntityReference("salesorder", salesOrderEntity.Id);
                    salesOrderMonthlyAmortization["gsc_financingtermid"] = quoteMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_financingtermid") != null
                        ? new EntityReference("gsc_sls_financingterm", quoteMonthlyAmortization.GetAttributeValue<EntityReference>("gsc_financingtermid").Id)
                        : null;
                    salesOrderMonthlyAmortization["gsc_ordermonthlyamortizationpn"] = quoteMonthlyAmortization.Contains("gsc_quotemonthlyamortizationpn")
                        ? quoteMonthlyAmortization.GetAttributeValue<String>("gsc_quotemonthlyamortizationpn")
                        : String.Empty;

                    _organizationService.Create(salesOrderMonthlyAmortization);
                }
            }

            _tracingService.Trace("Ended ReplicateQuoteMonthlyAmortization method...");
            return salesOrderEntity;
        }

        //Created By: Raphael Herrera, Created On 9/21/2016
        /*Purpose: Replicate QuoteCabChassis to OrderCabChassis
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         *      Post/Create: sales order
         * Primary Entity: Sales Order
         */
        public void ReplicateQuoteCabChassis(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ReplicateQuoteCabChassis Method...");

            var quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;

            //Retrieve quote cab chassis to be replicated
            EntityCollection quoteCabChassisCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_quotecabchassis", "gsc_quoteid", quoteId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_financing", "gsc_itemnumber", "gsc_vehiclecabchassisid", "gsc_quotecabchassispn", "gsc_amount" });

            _tracingService.Trace("Quote Cab Chassis Records Retrieved: " + quoteCabChassisCollection.Entities.Count);
            if (quoteCabChassisCollection.Entities.Count > 0)
            {
                //Replicate each record
                foreach (Entity quoteCabChassis in quoteCabChassisCollection.Entities)
                {
                    Entity orderCabChassis = new Entity("gsc_sls_ordercabchassis");
                    //orderCabChassis["gsc_amount"]
                    orderCabChassis["gsc_financing"] = quoteCabChassis.GetAttributeValue<bool>("gsc_financing");
                    orderCabChassis["gsc_itemnumber"] = quoteCabChassis.Contains("gsc_itemnumber") ? quoteCabChassis.GetAttributeValue<string>("gsc_itemnumber")
                        : String.Empty;
                    orderCabChassis["gsc_amount"] = quoteCabChassis.Contains("gsc_amount") ? quoteCabChassis.GetAttributeValue<Money>("gsc_amount")
                        : new Money(0);
                    orderCabChassis["gsc_ordercabchassispn"] = quoteCabChassis.GetAttributeValue<string>("gsc_quotecabchassispn");
                    orderCabChassis["gsc_vehiclecabchassisid"] = quoteCabChassis.GetAttributeValue<EntityReference>("gsc_vehiclecabchassisid") != null
                        ? quoteCabChassis.GetAttributeValue<EntityReference>("gsc_vehiclecabchassisid") : null;
                    orderCabChassis["gsc_orderid"] = new EntityReference(salesOrderEntity.LogicalName, salesOrderEntity.Id);

                    _organizationService.Create(orderCabChassis);

                    _tracingService.Trace("Created Order Cab Chassis Record...");
                }

            }
            _tracingService.Trace("Ending ReplicateQuoteCabChassis Method...");
        }

        public void GetSelectedMonthlyAmortization(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started GetSelectedMonthlyAmortization method...");

            Guid quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;

            if (quoteId != Guid.Empty)
            {
                _tracingService.Trace("Quote Id Exists");

                var financingTermId = Guid.Empty;

                var quoteConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_quoteid", ConditionOperator.Equal, quoteId),
                        new ConditionExpression("gsc_isselected", ConditionOperator.Equal, true)
                    };

                EntityCollection quoteMontlyAmortizationRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_quotemonthlyamortization", quoteConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_financingtermid" });

                if (quoteMontlyAmortizationRecords != null && quoteMontlyAmortizationRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Selected Quote Monthly Amortization");

                    var quoteMonthlyAmoritzation = quoteMontlyAmortizationRecords.Entities[0];

                    financingTermId = quoteMonthlyAmoritzation.GetAttributeValue<EntityReference>("gsc_financingtermid") != null
                        ? quoteMonthlyAmoritzation.GetAttributeValue<EntityReference>("gsc_financingtermid").Id
                        : Guid.Empty;
                }

                var salesOrderConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_orderid", ConditionOperator.Equal, salesOrderEntity.Id),
                        new ConditionExpression("gsc_financingtermid", ConditionOperator.Equal, financingTermId)
                    };

                EntityCollection orderMontlyAmortizationRecords = CommonHandler.RetrieveRecordsByConditions("gsc_sls_ordermonthlyamortization", salesOrderConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_financingtermid", "gsc_ordermonthlyamortizationpn" });

                if (orderMontlyAmortizationRecords != null && orderMontlyAmortizationRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve to be Selected Order Monthly Amortization");

                    var orderMonthlyAmoritzation = orderMontlyAmortizationRecords.Entities[0];

                    orderMonthlyAmoritzation["gsc_selected"] = true;

                    _organizationService.Update(orderMonthlyAmoritzation);

                    _tracingService.Trace("Order Monthly Amortization Selected");

                    Entity orderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
                        new ColumnSet("gsc_netmonthlyamortization"));

                    orderToUpdate["gsc_netmonthlyamortization"] = orderMonthlyAmoritzation.Contains("gsc_ordermonthlyamortizationpn")
                        ? new Money(Decimal.Parse(orderMonthlyAmoritzation.GetAttributeValue<String>("gsc_ordermonthlyamortizationpn").Trim(',')))
                        : new Money(0);

                    _organizationService.Update(orderToUpdate);

                    _tracingService.Trace("Order Net Monthly Amoritzation Updated.");

                }

            }

            _tracingService.Trace("Ended GetSelectedMonthlyAmortization method...");
        }

        //Created By: Raphael Herrera, Created On 9/23/2016
        /*Purpose: Delete associated order cab chassis records before creating
         * Registration Details: 
         * Event/Message: 
         *      Post/Update: porductid
         * Primary Entity: Sales Order
         */
        public void DeleteOrderCabChassis(Entity salesOrder)
        {
            _tracingService.Trace("Started DeleteQuoteCabChassis Method...");

            EntityCollection orderCCCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_ordercabchassis", "gsc_orderid", salesOrder.Id, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_orderid" });

            _tracingService.Trace("Order Cab Chassis Records Retrieved: " + orderCCCollection.Entities.Count);
            if (orderCCCollection.Entities.Count > 0)
            {
                foreach (Entity orderCC in orderCCCollection.Entities)
                {
                    _organizationService.Delete(orderCC.LogicalName, orderCC.Id);
                    _tracingService.Trace("Order Cab Chassis Deleted...");
                }


            }

            _tracingService.Trace("Ending DeleteQuoteCabChassis Method...");
        }

        //Created By: Raphael Herrera, Created On 10/06/2016
        public void UpdateStatus(Entity salesOrder)
        {
            _tracingService.Trace("Started UpdateStatus Method...");

            var status = salesOrder.Contains("gsc_status")
               ? salesOrder.GetAttributeValue<OptionSetValue>("gsc_status")
               : null;

            Entity orderToUpdate = _organizationService.Retrieve(salesOrder.LogicalName, salesOrder.Id,
                new ColumnSet("gsc_statuscopy", "gsc_ordercancelleddate"));

            orderToUpdate["gsc_statuscopy"] = salesOrder.Contains("gsc_status")
                ? salesOrder.GetAttributeValue<OptionSetValue>("gsc_status")
                : null;
            //Cancelled status
            if (status.Value == 100000006)
            {
                orderToUpdate["gsc_ordercancelleddate"] = DateTime.UtcNow;
            }

            _organizationService.Update(orderToUpdate);

            _tracingService.Trace("Ending UpdateStatus Method");
        }

        //Created By: Raphael Herrera, Created On: 9/27/2016
        //Computes and returns unit price as inclusive of tax
        private decimal ComputeUnitPrice(Entity productEntity, Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ComputeUnitPrice Method...");

            decimal sellPrice = 0;

            PriceListHandler priceListHandler = new PriceListHandler(_organizationService, _tracingService);
            priceListHandler.itemType = 0;
            priceListHandler.productFieldName = "gsc_productid";
            List<Entity> latestPriceList = priceListHandler.RetrievePriceList(salesOrderEntity, 100000000, 100000003);

            if (latestPriceList.Count > 0)
            {
                Entity priceListItem = latestPriceList[0];
                Entity priceList = latestPriceList[1];

                sellPrice = priceListItem.GetAttributeValue<Money>("amount").Value;

                decimal taxStatus = priceList.GetAttributeValue<OptionSetValue>("gsc_taxstatus").Value;

                var taxRate = productEntity.Contains("gsc_taxrate")
                    ? (Decimal)productEntity.GetAttributeValue<Double>("gsc_taxrate")
                    : 0;

                if (taxRate != 0)
                {
                    taxRate = taxRate / 100;
                }

                //Tax Exclusive
                if (taxStatus == 100000001)
                    sellPrice = sellPrice * (1 + taxRate);
            }
            else
            {
                throw new InvalidPluginExecutionException("There is no effecive Price List for the selected Vehicle.");
            }

         /*   //Retrieve price level associated with product
            EntityCollection priceLevelCollection = CommonHandler.RetrieveRecordsByOneValue("pricelevel", "pricelevelid", priceLevelId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_taxstatus", "statecode", "begindate", "enddate" });

            _tracingService.Trace("Price Level Records Retrieved: " + priceLevelCollection.Entities.Count);
            if (priceLevelCollection != null && priceLevelCollection.Entities.Count > 0)
            {
                Entity priceLevelEntity = priceLevelCollection.Entities[0];

                if (priceLevelEntity.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
                {
                    PriceListHandler priceListHandler = new PriceListHandler(_organizationService, _tracingService);
                    bool isLatest = priceListHandler.CheckifDefaultPriceListIsLatest(priceLevelEntity);

                    if (!isLatest)
                        throw new InvalidPluginExecutionException("Price List associated to this item is out of date.");

                    decimal taxStatus = priceLevelEntity.GetAttributeValue<OptionSetValue>("gsc_taxstatus").Value;
                    sellPrice = productEntity.Contains("gsc_sellprice") ? productEntity.GetAttributeValue<Money>("gsc_sellprice").Value
                        : 0;

                    var taxRate = productEntity.Contains("gsc_taxrate")
                        ? (Decimal)productEntity.GetAttributeValue<Double>("gsc_taxrate")
                        : 0;

                    if (taxRate != 0)
                    {
                        taxRate = taxRate / 100;
                    }

                    //Tax Exclusive
                    if (taxStatus == 100000001)
                        sellPrice = sellPrice * (1 + taxRate);
                }
                else
                {
                    throw new InvalidPluginExecutionException("There is no price list for the selected vehicle.");
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("There is no price list for the selected vehicle.");
            }*/

            _tracingService.Trace("Ending ComputeUnitPrice Method...");
            return sellPrice;
        }

        //Created By: Leslie Baliguat, Created On: 10/06/2016
        public void UnAllocateVehicle(Entity orderEntity)
        {
            _tracingService.Trace("Started UnAllocateVehicle Method");
            var status = orderEntity.Contains("gsc_status")
                ? orderEntity.GetAttributeValue<OptionSetValue>("gsc_status")
                : null;

            if (status.Value == 100000006)
            {
                EntityCollection allocatedCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_allocatedvehicle", "gsc_orderid", orderEntity.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_orderid" });

                if (allocatedCollection != null && allocatedCollection.Entities.Count > 0)
                {
                    foreach (var vehicle in allocatedCollection.Entities)
                    {
                        _organizationService.Delete(vehicle.LogicalName, vehicle.Id);
                    }
                }
            }

            

            _tracingService.Trace("Ending UnAllocateVehicle Method");
        }
        //Created By : Jessica Casupanan, Created On : 11/23/2016
        /*Purpose: Validate if the record can be deleted based on status
         * Registration Details: 
         * Event/Message:
         *      Pre/Create:
         *      Post/Update: 
         *      Post/Create: 
         * Primary Entity: Sales Order
         */
        public bool ValidateDelete(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started ValidateDelete Method...");

            if (salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") == null)
            {
                var salesorderstatus = salesOrderEntity.Contains("gsc_statuscopy") ? salesOrderEntity.GetAttributeValue<OptionSetValue>("gsc_statuscopy").Value
                       : 0;
                if (salesorderstatus != 100000000)
                    return true;
                else return false;
            }
            else return true;
        }

        //Created By : Jerome Anthony Gerero, Created On : 12/16/2016
        /*Purpose: Set payment summary fields to zero.
         * Registration Details: 
         * Event/Message:
         *      Pre/Create: 
         * Primary Entity: Sales Order
         */
        private Entity PopulatePaymentSummary(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started PopulatePaymentSummary Method...");

            Guid quoteId = salesOrderEntity.GetAttributeValue<EntityReference>("quoteid") != null
                ? salesOrderEntity.GetAttributeValue<EntityReference>("quoteid").Id
                : Guid.Empty;

            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_unitprice", "gsc_colorprice", "gsc_freightandhandling", "gsc_totaldiscount", "gsc_netprice", "gsc_accessories", "gsc_insurance", "gsc_chattelfee",
                "gsc_othercharges", "gsc_reservation", "gsc_downpayment", "gsc_vatablesales", "gsc_vatexemptsales", "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount",
                "gsc_totalamountdue", "gsc_totalcashoutlay", "gsc_amountfinanced", "gsc_netmonthlyamortization", "gsc_ccaddons" });

            if (quoteRecords != null && quoteRecords.Entities.Count > 0)
            {
                Entity quote = quoteRecords.Entities[0];

                salesOrderEntity["gsc_unitprice"] = quote.Contains("gsc_unitprice")
                    ? quote.GetAttributeValue<Money>("gsc_unitprice")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_ccaddons"] = quote.Contains("gsc_ccaddons")
                    ? quote.GetAttributeValue<Money>("gsc_ccaddons")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_colorprice"] = quote.Contains("gsc_colorprice")
                    ? quote.GetAttributeValue<Money>("gsc_colorprice")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_freightandhandling"] = quote.Contains("gsc_freightandhandling")
                    ? quote.GetAttributeValue<Money>("gsc_freightandhandling")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_discount"] = quote.Contains("gsc_totaldiscount")
                    ? quote.GetAttributeValue<Money>("gsc_totaldiscount")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_netprice"] = quote.Contains("gsc_netprice")
                    ? quote.GetAttributeValue<Money>("gsc_netprice")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_accessories"] = quote.Contains("gsc_accessories")
                    ? quote.GetAttributeValue<Money>("gsc_accessories")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_insurance"] = quote.Contains("gsc_insurance")
                    ? quote.GetAttributeValue<Money>("gsc_insurance")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_chattelfee"] = quote.Contains("gsc_chattelfee")
                    ? quote.GetAttributeValue<Money>("gsc_chattelfee")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_othercharges"] = quote.Contains("gsc_othercharges")
                    ? quote.GetAttributeValue<Money>("gsc_othercharges")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_reservation"] = quote.Contains("gsc_reservation")
                    ? quote.GetAttributeValue<Money>("gsc_reservation")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_downpayment"] = quote.Contains("gsc_downpayment")
                    ? quote.GetAttributeValue<Money>("gsc_downpayment")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_vatablesales"] = quote.Contains("gsc_vatablesales")
                    ? quote.GetAttributeValue<Money>("gsc_vatablesales")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_vatexemptsales"] = quote.Contains("gsc_vatexemptsales")
                    ? quote.GetAttributeValue<Money>("gsc_vatexemptsales")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_zeroratedsales"] = quote.Contains("gsc_zeroratedsales")
                    ? quote.GetAttributeValue<Money>("gsc_zeroratedsales")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_totalsales"] = quote.Contains("gsc_totalsales")
                    ? quote.GetAttributeValue<Money>("gsc_totalsales")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_vatamount"] = quote.Contains("gsc_vatamount")
                    ? quote.GetAttributeValue<Money>("gsc_vatamount")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_totalamountdue"] = quote.Contains("gsc_totalamountdue")
                    ? quote.GetAttributeValue<Money>("gsc_totalamountdue")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_totalcashoutlay"] = quote.Contains("gsc_totalcashoutlay")
                    ? quote.GetAttributeValue<Money>("gsc_totalcashoutlay")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_amountfinanced"] = quote.Contains("gsc_amountfinanced")
                    ? quote.GetAttributeValue<Money>("gsc_amountfinanced")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_totalamountfinanced"] = quote.Contains("gsc_totalamountfinanced")
                    ? quote.GetAttributeValue<Money>("gsc_totalamountfinanced")
                    : new Money(Decimal.Zero);
                salesOrderEntity["gsc_netmonthlyamortization"] = quote.Contains("gsc_netmonthlyamortization")
                    ? quote.GetAttributeValue<Money>("gsc_netmonthlyamortization")
                    : new Money(Decimal.Zero);
            }
            else
            {
                salesOrderEntity["gsc_unitprice"] = new Money(0);
                salesOrderEntity["gsc_ccaddons"] = new Money(0);
                salesOrderEntity["gsc_colorprice"] = new Money(0);
                salesOrderEntity["gsc_freightandhandling"] = new Money(0);
                salesOrderEntity["gsc_discount"] = new Money(0);
                salesOrderEntity["gsc_netprice"] = new Money(0);
                salesOrderEntity["gsc_accessories"] = new Money(0);
                salesOrderEntity["gsc_insurance"] = new Money(0);
                salesOrderEntity["gsc_chattelfee"] = new Money(0);
                salesOrderEntity["gsc_othercharges"] = new Money(0);
                salesOrderEntity["gsc_reservation"] = new Money(0);
                salesOrderEntity["gsc_downpayment"] = new Money(0);
                salesOrderEntity["gsc_vatablesales"] = new Money(0);
                salesOrderEntity["gsc_vatexemptsales"] = new Money(0);
                salesOrderEntity["gsc_zeroratedsales"] = new Money(0);
                salesOrderEntity["gsc_totalsales"] = new Money(0);
                salesOrderEntity["gsc_vatamount"] = new Money(0);
                salesOrderEntity["gsc_totalamountdue"] = new Money(0);
                salesOrderEntity["gsc_totalcashoutlay"] = new Money(0);
                salesOrderEntity["gsc_amountfinanced"] = new Money(0);
                salesOrderEntity["gsc_netmonthlyamortization"] = new Money(0);
            }
            
            _tracingService.Trace("Ending PopulatePaymentSummary Method...");
            return salesOrderEntity;
        }

        //Created By : Artum Ramos, Created On : 12/20/2016
        /*Purpose: Delete Vehicle Allocated Items Record
         * Registration Details: 
         * Event/Message:
         *      Post/Delete: 
         * Primary Entity: Sales Order
         */
        public void DeleteVehicleAllocatedItems(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started DeleteVehicleAllocatedItems Method...");

            Guid allocatedItemsid = salesOrderEntity.Contains("gsc_allocateditemstodelete")
                ? new Guid(salesOrderEntity.GetAttributeValue<String>("gsc_allocateditemstodelete"))
                : Guid.Empty;


            _tracingService.Trace("Retrieve Record");
            EntityCollection AllocatedItemsRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_iv_allocatedvehicle", "gsc_iv_allocatedvehicleid", allocatedItemsid, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_iv_allocatedvehicleid" });
            _tracingService.Trace("condition if null");
            if (AllocatedItemsRecords != null || AllocatedItemsRecords.Entities.Count > 0)
            {
                _tracingService.Trace("For Each record");
                foreach (var AllocatedItems in AllocatedItemsRecords.Entities)
                {
                    _tracingService.Trace("Process Deleting");
                    _organizationService.Delete(AllocatedItems.LogicalName, AllocatedItems.Id);
                    //_organizationService.Delete("gsc_iv_allocatedvehicle", allocatedItemsid);
                }
            }

            _tracingService.Trace("Ending DeleteVehicleAllocatedItems Method...");

        }

        //Created By: Artum Ramos, Created On: 12/27/2016
        /*Purpose: Set Transferred date for invoicing in Sales Order when Transfer for invoiced button was clicked
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_istransferforinvoicing
         * Primary Entity: Order
         */
        public void SetTransferInvoiceDate(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started SetTransferInvoiceDate method");

            var istransfer = salesOrderEntity.GetAttributeValue<Boolean>("gsc_istransferforinvoicing");

            _tracingService.Trace(istransfer.ToString());

            if (istransfer == true)
            {
                _tracingService.Trace("Pass the condition is equal True");
                String today = DateTime.Today.ToString("MM-dd-yyyy");

                _tracingService.Trace("Retrieve Sales Order Entity");
                Entity orderToUpdate = _organizationService.Retrieve(salesOrderEntity.LogicalName, salesOrderEntity.Id,
                    new ColumnSet("gsc_transferreddateforinvoicing"));
                _tracingService.Trace("gsc_transferreddateforinvoicing is equal to date today");
                orderToUpdate["gsc_transferreddateforinvoicing"] = Convert.ToDateTime(today);

                _tracingService.Trace("update gsc_transferreddateforinvoicing fields");
                _organizationService.Update(orderToUpdate);
                _tracingService.Trace("Finish update SetTransferInvoiceDate date");
            }

            _tracingService.Trace("Ended SetTransferInvoiceDate method");
        }

        //Created By : Jessica Casupanan, Created On : 01/19/2017
        //Purpose : Validate Unit Price On Create
        //TO BE DELETED
        public bool CheckIfHasUnitPrice(Entity salesOrderEntity)
        {
            _tracingService.Trace("Started CheckIfHasUnitPrice Method...");
            Guid productId = salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid") != null ? salesOrderEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty;

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_sellprice", "pricelevelid" });

            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieved {" + productCollection.Entities.Count + "}: " + "Retrieving Product...");

                Entity product = productCollection.Entities[0];
                Guid priceLevelId = product.Contains("pricelevelid") ? product.GetAttributeValue<EntityReference>("pricelevelid").Id
               : Guid.Empty;

                if (!product.Contains("gsc_sellprice"))
                {
                    _tracingService.Trace("true");
                    return true;
                }
                if (priceLevelId == Guid.Empty)
                {
                    return true;
                }
            }
            _tracingService.Trace("Ended CheckIfHasUnitPrice Method...");
            return false;

        }

        public void UpdateReservation(Entity salesOrder)
        {
            var status = salesOrder.Contains("gsc_status")
                ? salesOrder.GetAttributeValue<OptionSetValue>("gsc_status").Value
                : 0;

            salesOrder["gsc_reservation"] = salesOrder.Contains("gsc_reservationfee")
                ? salesOrder.GetAttributeValue<Money>("gsc_reservationfee")
                : null;
            salesOrder = SetTotalCashOutlayAmount(salesOrder, "create");

            _organizationService.Update(salesOrder);

            Entity orderToUpdate = _organizationService.Retrieve(salesOrder.LogicalName, salesOrder.Id,
                new ColumnSet("gsc_totalcashoutlay", "gsc_reservation", "gsc_status"));

            orderToUpdate["gsc_totalcashoutlay"] = salesOrder["gsc_totalcashoutlay"];
            orderToUpdate["gsc_reservation"] = salesOrder["gsc_reservation"];

            if (status != 100000002 && status != 100000003)
                orderToUpdate["gsc_status"] = new OptionSetValue(100000001);

            _organizationService.Update(orderToUpdate);
        }

    }
}
