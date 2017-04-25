using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using GSC.Rover.DMS.BusinessLogic.Common;
using System.Linq;
using System.Threading;
using GSC.Rover.DMS.BusinessLogic.PriceList;

namespace GSC.Rover.DMS.BusinessLogic.Quote
{
    public class QuoteHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;

        public QuoteHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Created By : Jerome Anthony Gerero, Created On: 2/10/2016
        /*Purpose: Replicate Opportunity fields into newly created Quote record
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update:
         * Primary Entity: Quote
         */
        public Entity ReplicateOpportunityInfo(Entity quoteEntity)
        {
            _tracingService.Trace("Started ReplicateOpportunityInfo method..");

            var opportunityId = quoteEntity.GetAttributeValue<EntityReference>("opportunityid") != null
                ? quoteEntity.GetAttributeValue<EntityReference>("opportunityid").Id
                : Guid.Empty;

            //Retrieve Opportunity record from Opportunity ID field value
            EntityCollection opportunityRecords = CommonHandler.RetrieveRecordsByOneValue("opportunity", "opportunityid", opportunityId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_salesexecutiveid", "gsc_vehiclebasemodelid", "gsc_colorid", "gsc_leadsourceid", "originatingleadid", "customerid", "gsc_dealerid", "gsc_branchid", "gsc_portaluserid", "name", "gsc_paymentmode", "gsc_financingtermid"});

            if (opportunityRecords != null && opportunityRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieved {" + opportunityRecords.Entities.Count + "}: " + "Retrieving Opportunity...");

                Entity opportunity = opportunityRecords.Entities[0];

                String today = DateTime.Today.ToString("MM-dd-yyyy");

                var productId = quoteEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? quoteEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty;

                if (productId == Guid.Empty)
                {
                    quoteEntity["gsc_salesexecutiveid"] = opportunity.Contains("gsc_salesexecutiveid")
                        ? new EntityReference(opportunity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").LogicalName, opportunity.GetAttributeValue<EntityReference>("gsc_salesexecutiveid").Id)
                        : null;
                    quoteEntity["gsc_leadsourceid"] = opportunity.Contains("gsc_leadsourceid")
                        ? new EntityReference(opportunity.GetAttributeValue<EntityReference>("gsc_leadsourceid").LogicalName, opportunity.GetAttributeValue<EntityReference>("gsc_leadsourceid").Id)
                        : null;
                    quoteEntity["gsc_vehiclebasemodelid"] = opportunity.Contains("gsc_vehiclebasemodelid")
                        ? opportunity.GetAttributeValue<EntityReference>("gsc_vehiclebasemodelid")
                        : null;
                    quoteEntity["gsc_dealerid"] = opportunity.Contains("gsc_dealerid")
                        ? new EntityReference(opportunity.GetAttributeValue<EntityReference>("gsc_dealerid").LogicalName, opportunity.GetAttributeValue<EntityReference>("gsc_dealerid").Id)
                        : null;
                    quoteEntity["gsc_branchid"] = opportunity.Contains("gsc_branchid")
                        ? new EntityReference(opportunity.GetAttributeValue<EntityReference>("gsc_branchid").LogicalName, opportunity.GetAttributeValue<EntityReference>("gsc_branchid").Id)
                        : null;
                    quoteEntity["gsc_recordownerid"] = opportunity.Contains("gsc_portaluserid")
                        ? new EntityReference("contact", new Guid(opportunity.GetAttributeValue<String>("gsc_portaluserid")))
                        : null;
                }

                quoteEntity["customerid"] = opportunity.Contains("customerid")
                    ? opportunity.GetAttributeValue<EntityReference>("customerid")
                    : null;
                quoteEntity["gsc_quotedate"] = Convert.ToDateTime(today);
            }

            PopulatePaymentSummary(quoteEntity);


            _tracingService.Trace("Ended ReplicateOpportunityInfo method..");
            return quoteEntity;
        }

        public void CheckifVehicleHasTax(Entity quoteEntity)
        {
            var vehicle = quoteEntity.Contains("gsc_productid")
                    ? quoteEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? quoteEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
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
        public void CheckifCustomerHasTax(Entity quoteEntity)
        {
            _tracingService.Trace("Started CheckifVehicleHasTax method.");

            var customer = quoteEntity.Contains("customerid")
                    ? quoteEntity.GetAttributeValue<EntityReference>("customerid") != null
                    ? quoteEntity.GetAttributeValue<EntityReference>("customerid")
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

        //Created By Leslie G. Baliguat, Created On: 2/11/2016
        /*Purpose: Populate vehicle description and unit price from vehicle and item catalog to quote
         *         Replicate Vehicle Unit Price to Unit Price in Summary
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update: Model Description = gsc_productid
         * Primary Entity: Quote
         */
        public Entity ConcatenateVehicleDescription(Entity quoteEntity, string message)
        {
            _tracingService.Trace("Started ConcatenateVehicleDescription method ...");

            if (quoteEntity.Contains("gsc_productid"))
            {
                var productId = quoteEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? quoteEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty;

                EntityCollection productRecords = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_enginetype", "gsc_transmission", "gsc_grossvehicleweight", "gsc_vehiclemodelid", "gsc_pistondisplacement", "gsc_fueltype", "gsc_status", 
                        "gsc_sellprice", "gsc_warrantyexpirydays", "gsc_warrantymileage", "gsc_othervehicledetails", "pricelevelid", "gsc_taxid", "gsc_taxrate", "gsc_modelcode", "gsc_optioncode" });

                if (productRecords != null && productRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved {" + productRecords.Entities.Count + "}: " + "Retrieving Product...");

                    Entity product = productRecords.Entities[0];

                    var baseModel = product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid") != null
                        ? product.GetAttributeValue<EntityReference>("gsc_vehiclemodelid")
                        : null;
                    var sellprice = product.Contains("gsc_sellprice")
                        ? product.GetAttributeValue<Money>("gsc_sellprice")
                        : new Money(0);
                    var enginetype = product.Contains("gsc_enginetype")
                        ? product["gsc_enginetype"] 
                        : string.Empty;
                    var transmission = product.Contains("gsc_transmission")
                        ? product.FormattedValues["gsc_transmission"] 
                        : string.Empty;
                    var gvw = product.Contains("gsc_grossvehicleweight")
                        ? product["gsc_grossvehicleweight"] 
                        : string.Empty;
                    var piston = product.Contains("gsc_pistondisplacement")
                        ? product["gsc_pistondisplacement"]
                        : string.Empty;
                    var fuel = product.Contains("gsc_fueltype")
                        ? product.FormattedValues["gsc_fueltype"] 
                        : string.Empty;
                    var status = product.Contains("gsc_status")
                        ? product.FormattedValues["gsc_status"] 
                        : string.Empty;
                    var expirydays = product.Contains("gsc_warrantyexpirydays")
                        ? product["gsc_warrantyexpirydays"]
                        : string.Empty;
                    var warranty = product.Contains("gsc_warrantymileage")
                        ? product["gsc_warrantymileage"] 
                        : string.Empty;
                    var others = product.Contains("gsc_othervehicledetails")
                        ? product["gsc_othervehicledetails"]
                        : string.Empty;
                    var modelCode = product.Contains("gsc_modelcode")
                        ? product["gsc_modelcode"]
                        : string.Empty;
                    var optionCode = product.Contains("gsc_optioncode")
                        ? product["gsc_optioncode"]
                        : string.Empty;

                    String description = "Engine Type: " + enginetype +
                        "\nModel Code: " + modelCode +
                        "\nOption Code: " + optionCode + 
                        "\nTransmission: "+ transmission + 
                        "\nWeight: " + gvw +
                        "\nDisplacement: " + piston +
                        "\nFuel: " + fuel + 
                        "\nStatus: " + status + 
                        "\nWarranty Days: " + expirydays + 
                        "\nWarranty Mileage: " + warranty + 
                        "\nOthers: " + others;
                    description = description.Remove(description.Length - 2, 2);

                    sellprice = new Money(ComputeUnitPrice(product, quoteEntity));
                    quoteEntity["gsc_vehicledetails"] = description;
                    quoteEntity["gsc_vehicleunitprice"] = sellprice;
                    quoteEntity["gsc_unitprice"] = sellprice;
                    quoteEntity["gsc_vehiclebasemodelid"] = baseModel;

                    if (message == "Update")
                    {
                        Entity quoteToUpdate = _organizationService.Retrieve(quoteEntity.LogicalName, quoteEntity.Id,
                            new ColumnSet("gsc_vehicledetails", "gsc_vehicleunitprice", "gsc_unitprice", "gsc_vehiclebasemodelid"));
                        quoteToUpdate["gsc_vehicledetails"] = description;
                        quoteToUpdate["gsc_vehicleunitprice"] = sellprice;
                        quoteToUpdate["gsc_unitprice"] = sellprice;
                        quoteToUpdate["gsc_vehiclebasemodelid"] = baseModel;
                        _organizationService.Update(quoteToUpdate);

                        return quoteToUpdate;
                    }
                }
            }

            _tracingService.Trace("Ended ConcatenateVehicleDescription method ...");

            return quoteEntity;
        }

        //Created By: Leslie Baliguat, Created On: 2/12/2016
        //Modified By: Jessica Casupanan, Modified On: 04/10/2017
                    //:Enhancement for insurance.
        /*Purpose:  If Insurance Total Premium was updated, update Insurance Charges in Payment Summary.
         *         If free in Insurance was checked, update Insurance Charges in Payment Suammry to Zero(0).
         *         If free in Insurance was unchecked, update Insurance Charges in Payment Suammry to the value of Insurance Total Premium.
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_totalpremium, gsc_free
         * Primary Entity: Quote
         */
        public Money UpdateInsurance(Entity quoteEntity, string message)
        {
            _tracingService.Trace("Started PopulateInsuranceCoverage method ...");
            decimal insurance = Decimal.Zero;

            insurance = quoteEntity.Contains("gsc_totalinsurancecharges")
                    ? quoteEntity.GetAttributeValue<Money>("gsc_totalinsurancecharges").Value
                    : Decimal.Zero;

            var paymentmode = quoteEntity.Contains("gsc_paymentmode")
              ? quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
              : Decimal.Zero;

            quoteEntity["gsc_insurance"] = new Money(insurance);
            var cashoutlay = Decimal.Zero;

            //Financing
            if (paymentmode == 100000001)
            {
                cashoutlay = ComputeCashLayout(quoteEntity);
                quoteEntity["gsc_totalcashoutlay"] = new Money(cashoutlay);
            }

            if (message == "Update")
            {
                Entity quoteToUpdate = _organizationService.Retrieve(quoteEntity.LogicalName, quoteEntity.Id,
                    new ColumnSet("gsc_totalchargesamount", "gsc_othercharges", "statecode", "gsc_totalcashoutlay", "gsc_downpayment", "gsc_chattelfee", "gsc_productid",
                "gsc_insurance", "gsc_freightandhandling", "gsc_ccaddons", "gsc_totaldiscount", "gsc_unitprice", "gsc_colorprice", "customerid", "gsc_paymentmode",
                "gsc_netprice", "gsc_accessories", "gsc_vatablesales", "gsc_vatexemptsales", "gsc_zeroratedsales", "gsc_totalsales", "gsc_vatamount", "gsc_totalamountdue"));

                quoteToUpdate["gsc_insurance"] = new Money(insurance);
                quoteToUpdate["gsc_totalcashoutlay"] = new Money(cashoutlay);
                quoteToUpdate = ComputeVAT(quoteToUpdate);

                _tracingService.Trace("Updating insurance field ...");

                _organizationService.Update(quoteToUpdate);

                return quoteToUpdate.GetAttributeValue<Money>("gsc_insurance");
            }


            _tracingService.Trace("Ended PopulateInsuranceCoverage method ...");

            return quoteEntity.GetAttributeValue<Money>("gsc_insurance");
        }

        //Created By : Jerome Anthony Gerero, Created On: 2/15/2016 
        //Modified By: Leslie Baliguat, Modified On: 9/16/2016
        //              Change productsubstitute to vehicle Accessory
        /*Purpose: Replicate Vehicle bundled items to Quote Product
         * Registration Details: 
         * Event/Message:
         *      Post/Update: Model Description = gsc_productid
         *      Post/Create: 
         * Primary Entity: Quote
         */
        public Entity RetrieveAndCreateVehicleFreebies(Entity quoteEntity, string message)
        {
            _tracingService.Trace("Started RetrieveAndCreateVehicleFreebies method..");

            if (quoteEntity.Attributes.Contains("gsc_productid"))
            {
                var product = quoteEntity.GetAttributeValue<EntityReference>("gsc_productid");

                //Create filter for Product in Product Relationship entity
                var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_productid", ConditionOperator.Equal, product.Id),
                        new ConditionExpression("gsc_free", ConditionOperator.Equal, true)
                    };

                //Retrieve related products from Product Relationship entity
                EntityCollection vehicleAccessories = CommonHandler.RetrieveRecordsByConditions("gsc_sls_vehicleaccessory", productConditionList, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_itemid", "gsc_vehicleaccessorypn" });


                if (vehicleAccessories != null && vehicleAccessories.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieve Vehicle Accessories");

                    Entity quoteAccessory = new Entity();

                    //Create retrieved products in Quote Product entity
                    quoteAccessory = new Entity("gsc_sls_quoteaccessory");
                    foreach (var accessory in vehicleAccessories.Entities)
                    {
                        _tracingService.Trace("Set up  Quote Accessory");

                        quoteAccessory["gsc_quoteid"] = new EntityReference(quoteEntity.LogicalName, quoteEntity.Id);
                        quoteAccessory["gsc_productid"] = accessory.Contains("gsc_itemid") ? new EntityReference("product", accessory.GetAttributeValue<EntityReference>("gsc_itemid").Id)
                            : null;
                        quoteAccessory["gsc_itemnumber"] = accessory.Contains("gsc_vehicleaccessorypn")
                            ? accessory.GetAttributeValue<String>("gsc_vehicleaccessorypn")
                            : String.Empty;
                        quoteAccessory["gsc_free"] = true;
                        quoteAccessory["gsc_standard"] = true;
                        _organizationService.Create(quoteAccessory);

                        _tracingService.Trace("Quote Accessory Created");
                    }
                }
            }

            return quoteEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On: 2/16/2016
        /*Purpose: Replicate Vehicle Accessories to Quote Accessory
         * Registration Details: 
         * Event/Message:
         *      Post/Update: Model Description = gsc_productid
         *      Post/Create: 
         * Primary Entity: Quote
         */
        public Entity DeleteExistingVehicleFreebies(Entity quoteEntity, string message)
        {
            _tracingService.Trace("Started DeleteExistingVehicleFreebies method..");

            var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_quoteid", ConditionOperator.Equal, quoteEntity.Id)
                    };

            EntityCollection relatedProducts = CommonHandler.RetrieveRecordsByConditions("gsc_sls_quoteaccessory", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_quoteaccessorypn" });

            if (relatedProducts != null && relatedProducts.Entities.Count > 0)
            {
                foreach (var relatedProduct in relatedProducts.Entities)
                {
                    _organizationService.Delete(relatedProduct.LogicalName, relatedProduct.Id);
                }
            }

            _tracingService.Trace("Ended DeleteExistingVehicleFreebies method..");

            //Call RetrieveAndCreateVehicleFreebies method
            return RetrieveAndCreateVehicleFreebies(quoteEntity, message);
        }

        //Created By: Leslie G. Baliguat, Created On: 02/16/2016
        /*Purpose: Delete Monthly Amortization Records when Financing Scheme or Amount Financed were updated
         * Registration Details:
         * Event/Message: 
         *      Post/Create:
         *      Post/Update:Financing Scheme = gsc_financingschemeid
         *                  Amount Financed = gsc_amountfinanced
         * Primary Entity: Quote
         */
        public Entity CheckMonthlyAmortizationRecord(Entity quoteEntity)
        {
            _tracingService.Trace("Started CheckMonthlyAmortizationRecord method ...");

            EntityCollection MARecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_quotemonthlyamortization", "gsc_quoteid", quoteEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_quoteid" });

            if (MARecords != null || MARecords.Entities.Count > 0)
            {
                foreach (var amortization in MARecords.Entities)
                {
                    _tracingService.Trace("Deleting Monthly Amortization Records...");

                    _organizationService.Delete(amortization.LogicalName, amortization.Id);
                }

                quoteEntity["gsc_netmonthlyamortization"] = new Money(0);

                _tracingService.Trace("Net monthly amortization Updated to 0 ...");
            }

            //Call CreateMonthlyAmortization method
            if (quoteEntity.Contains("gsc_financingschemeid") && quoteEntity.GetAttributeValue<EntityReference>("gsc_financingschemeid") != null)
            {
                return CreateMonthlyAmortization(quoteEntity);
            }

            _tracingService.Trace("Ended CheckMonthlyAmortizationRecord method..");

            return null;
        }

        //Created By: Leslie G. Baliguat, Created On: 02/16/2016
        /*Purpose: Compute Monthly Amortization based on Amount Financed and Financing Scheme Selected
         *         then Create Monthly Amotization records.
         */
        private Entity CreateMonthlyAmortization(Entity quoteEntity)
        {
            _tracingService.Trace("Started CreateMonthlyAmortization method ...");
            var schemeId = CommonHandler.GetEntityReferenceIdSafe(quoteEntity, "gsc_financingschemeid");

            EntityCollection schemeDetailsRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_financingschemedetails", "gsc_financingschemeid", schemeId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_financingtermid", "gsc_addonrate", "gsc_downpaymentfrom", "gsc_downpaymentto", "gsc_zerointerest", "gsc_dealerincome", "gsc_interestrate" });

            if (schemeDetailsRecords != null && schemeDetailsRecords.Entities.Count > 0)
            {
                //Retrieve Financing Scheme Details
                _tracingService.Trace("Retrieve Financing Scheme Details");

                Entity amortization = new Entity("gsc_sls_quotemonthlyamortization");

                foreach (var schemeEntity in schemeDetailsRecords.Entities)
                {
                    var term = CommonHandler.GetEntityReferenceIdSafe(schemeEntity, "gsc_financingtermid");

                    _tracingService.Trace("Creating Monthly Amortization Record ...");

                    amortization["gsc_quoteid"] = new EntityReference("quote", quoteEntity.Id);
                    amortization["gsc_financingtermid"] = new EntityReference("gsc_sls_financingterm", term);
                    amortization["gsc_quotemonthlyamortizationpn"] = string.Format("{0:n0}", ComputeForMonthlyAmortization(quoteEntity, schemeEntity));
                    amortization["gsc_dealerid"] = quoteEntity.Contains("gsc_dealerid") ?
                        quoteEntity.GetAttributeValue<EntityReference>("gsc_dealerid") :
                        null;
                    amortization["gsc_branchid"] = quoteEntity.Contains("gsc_branchid") ?
                        quoteEntity.GetAttributeValue<EntityReference>("gsc_branchid") :
                        null;
                    amortization["gsc_recordownerid"] = quoteEntity.Contains("gsc_recordownerid") ?
                        quoteEntity.GetAttributeValue<EntityReference>("gsc_recordownerid") :
                        null;

                    _organizationService.Create(amortization);

                    _tracingService.Trace("Monthly Amortization Created ...");
                }
                return amortization;
            }
            _tracingService.Trace("Ended CreateMonthlyAmortization method ...");

            return null;
        }

        private double ComputeForMonthlyAmortization(Entity quoteEntity, Entity schemeEntity)
        {
            var amountFinancedDiscount = quoteEntity.Contains("gsc_applytoafpercentage")
                ? quoteEntity.GetAttributeValue<double>("gsc_applytoafpercentage")
                : 0;
            var monthlyAmortization = 0.0;

            if (amountFinancedDiscount != 0)
            {
                if (CheckifZIP(quoteEntity, schemeEntity))
                {//There is discount for amount financed but AOR is Zero or Tagged as ZIP
                    _tracingService.Trace("There is discount for amount financed but AOR is Zero or Tagged as ZIP");

                    monthlyAmortization = NormalFormula(quoteEntity, schemeEntity);
                }
                else
                {//There is discount for amount financed
                    _tracingService.Trace("There is discount for amount financed");
                    monthlyAmortization = PMTFormula(quoteEntity, schemeEntity);
                }
            }
            else
            { //There is no discount for amount financed 
                _tracingService.Trace("There is no discount for amount financed");
                monthlyAmortization = NormalFormula(quoteEntity, schemeEntity);
            }

            return Math.Round(monthlyAmortization, 2);
        }

        private bool CheckifZIP(Entity quoteEntity, Entity schemeEntity)
        {
            //if isZIP checkbox is checked, check if Downpayment is with in the range of DPFrom and DPTo of Finacing Scheme Detail
            if (schemeEntity.GetAttributeValue<bool>("gsc_zerointerest"))
            {
                _tracingService.Trace("Zip = Yes");

                var dpPercent = quoteEntity.Contains("gsc_downpaymentpercentage")
                ? quoteEntity.GetAttributeValue<double>("gsc_downpaymentpercentage")
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
        private double PMTFormula(Entity quoteEntity, Entity schemeEntity)
        {
            _tracingService.Trace("Compute WithAFDiscount.");

            double amountfinanced = quoteEntity.Contains("gsc_amountfinanced")
                ? double.Parse(quoteEntity.GetAttributeValue<Money>("gsc_amountfinanced").Value.ToString())
                : 0;

            var afDiscount = quoteEntity.Contains("gsc_lessdiscountaf")
                ? quoteEntity.GetAttributeValue<Money>("gsc_lessdiscountaf").Value
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
        private double NormalFormula(Entity quoteEntity, Entity schemeEntity)
        {
            _tracingService.Trace("Compute ZIP_ZeroAOR_NoAFDiscount.");

            var amountfinanced = quoteEntity.Contains("gsc_amountfinanced")
                ? double.Parse(quoteEntity.GetAttributeValue<Money>("gsc_amountfinanced").Value.ToString())
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

        //Created By: Leslie Baliguat, Created On: 3/3/2016
        /*Purpose: Delete Requirement Checklist Records when Bank ID was updated
         *          and Create Requirement Checklist Records
         * Registration Details:
         * Event/Message: 
         *      Post/Create:
         *      Post/Update: Bank Id = gsc_bankid
         * Primary Entity: Quote
         */
        public Entity CreateRequirementChecklist(Entity quoteEntity)
        {
            if (quoteEntity.Contains("gsc_bankid") || quoteEntity.GetAttributeValue<EntityReference>("gsc_bankid") != null)
            {
                _tracingService.Trace("Started CreateRequirementChecklist method - Bank id is not null ...");

                var bankid = quoteEntity.GetAttributeValue<EntityReference>("gsc_bankid").Id;

                var RequirementCondition = new List<ConditionExpression>
                {
                    new ConditionExpression("gsc_quoteid", ConditionOperator.Equal, quoteEntity.Id),
                    new ConditionExpression("gsc_bankid", ConditionOperator.Equal, bankid)
                };

                EntityCollection Requirement = CommonHandler.RetrieveRecordsByConditions("gsc_sls_requirementchecklist", RequirementCondition, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_quoteid", "gsc_bankid" });

                //delete existing Requirement Checklsit
                if (Requirement != null || Requirement.Entities.Count > 0)
                {
                    foreach (Entity RequirmenttobeDeleted in Requirement.Entities)
                    {
                        _tracingService.Trace("Deleting existing Requirement Checklist records ...");

                        _organizationService.Delete(RequirmenttobeDeleted.LogicalName, RequirmenttobeDeleted.Id);
                    }
                }

                _tracingService.Trace("Requirement Checklist is now null ...");

                Entity requirementEntity = new Entity("gsc_sls_requirementchecklist");

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

                        requirementEntity["gsc_quoteid"] = new EntityReference("quote", quoteEntity.Id);
                        requirementEntity["gsc_bankid"] = new EntityReference("gsc_sls_bank", bankid);
                        requirementEntity["gsc_documentchecklistid"] = new EntityReference("gsc_sls_documentchecklist", documentrecord.Id);
                        requirementEntity["gsc_requirementchecklistpn"] = documentrecord["gsc_documentchecklistpn"];
                        requirementEntity["gsc_mandatory"] = documentrecord.GetAttributeValue<Boolean>("gsc_mandatory");
                        requirementEntity["gsc_documenttype"] = documentrecord.GetAttributeValue<Boolean>("gsc_documenttype");

                        _organizationService.Create(requirementEntity);
                    }

                    _tracingService.Trace("Ended CreateRequirementChecklist method ...");

                    return requirementEntity;
                }
            }


            _tracingService.Trace("Ended CreateRequirementChecklist method ...");

            return null;
        }

        //Created By: Leslie Baliguat, Created On: 3/16/2016 
        /*Purpose: Populate Additional Price from Vehicle Color to COlor Price in Quote
         *         Compute/Recompute Net Price, AmountFinanced, Net Amount Financed, Total Amount Financed
         *         Compute/Recompute Vatable Sales, Vatamount, Total Due Amount
         * Registration Details:
         * Event/Message: 
         *      Pre/Create: 
         *      Post/Update: Preferred Color 1
         * Primary Entity: Quote
         */
        public Entity PopulateColorPrice(Entity quoteEntity, String message)
        {
            _tracingService.Trace("Started PopulateColorPrice method ...");

            var colorid = quoteEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1") != null
                ? quoteEntity.GetAttributeValue<EntityReference>("gsc_vehiclecolorid1").Id
                : Guid.Empty;

            var additionalprice = new Decimal(0);

            EntityCollection colorRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_vehiclecolor", "gsc_cmn_vehiclecolorid", colorid, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_additionalprice" });

            if (colorRecords != null && colorRecords.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieved Additional Price ...");

                Entity colorEntity = colorRecords.Entities[0];

                additionalprice = colorEntity.Contains("gsc_additionalprice")
                    ? colorEntity.GetAttributeValue<Money>("gsc_additionalprice").Value
                    : 0;
            }

            quoteEntity["gsc_colorprice"] = new Money(additionalprice);

            var netprice = ComputeNetPrice(quoteEntity);

            quoteEntity["gsc_netprice"] = new Money(netprice);

            quoteEntity = ComputeVAT(quoteEntity);

            var paymentmode = quoteEntity.Contains("gsc_paymentmode")
                ? quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
                : Decimal.Zero;

            var amountfinanced = Decimal.Zero;

            //Financing
            if (paymentmode == 100000001 || paymentmode == 100000002)
            {
                amountfinanced = ComputeAmountFinanced(quoteEntity);
                quoteEntity["gsc_amountfinanced"] = new Money(amountfinanced);
                quoteEntity["gsc_totalamountfinanced"] = new Money(amountfinanced);
            }

            if (message == "Update")
            {
                Entity quoteToUpdate = _organizationService.Retrieve(quoteEntity.LogicalName, quoteEntity.Id, new ColumnSet("gsc_colorprice", "gsc_netprice",
                    "gsc_amountfinanced", "gsc_netprice", "gsc_vatablesales",
                    "gsc_vatamount", "gsc_totalamountdue", "gsc_totalamountfinanced"));
                quoteToUpdate["gsc_colorprice"] = new Money(additionalprice);
                quoteToUpdate["gsc_netprice"] = new Money(netprice);
                quoteToUpdate["gsc_amountfinanced"] = new Money(amountfinanced);
                quoteToUpdate["gsc_totalamountfinanced"] = new Money(amountfinanced);

                _organizationService.Update(quoteToUpdate);

                _tracingService.Trace("Updated Color Price ...");

                return quoteToUpdate;
            }

            _tracingService.Trace("Ended PopulateColorPrice method ...");

            return quoteEntity;
        }

        //Created by: Leslie Baliguat, Created on: 3/18/2016
        /*Purpose: Update Discounts in Unit Price, Downpayment, Amount Financed 
         *         Scenario: If Discounts from Quote -> Charge are Null, values to fill discounts
         *                  will be coming from QuoteDiscount associated to Quote
         *         Recompute Net Price, Net Downpayment, AmountFinanced, Net Amount Financed, Total Cash Outlay
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_applytoafamount, gsc_applytoupamount, gsc_applytodpamount
         * Primary Entity: Quote
         */
        public void SetLessDiscountValues(Entity quoteEntity)
        {
            _tracingService.Trace("Started SetLessDiscountValues method...");

            var downpayment = quoteEntity.Contains("gsc_applytodpamount")
                ? quoteEntity.GetAttributeValue<Money>("gsc_applytodpamount").Value
                : 0;
            var amountFinanced = quoteEntity.Contains("gsc_applytoafamount")
                ? quoteEntity.GetAttributeValue<Money>("gsc_applytoafamount").Value
                : 0;
            var unitPrice = quoteEntity.Contains("gsc_applytoupamount")
                ? quoteEntity.GetAttributeValue<Money>("gsc_applytoupamount").Value
                : 0;

            quoteEntity["gsc_totaldiscount"] = new Money(unitPrice);
            quoteEntity["gsc_lessdiscountaf"] = new Money(amountFinanced);
            quoteEntity["gsc_lessdiscount"] = new Money(downpayment);

            //Recompute related fields
            Decimal downPaymentAmount = ComputeDownpaymentAmount(quoteEntity);
            quoteEntity["gsc_downpaymentamount"] = new Money(downPaymentAmount);

            var netprice = ComputeNetPrice(quoteEntity);
            quoteEntity["gsc_netprice"] = new Money(netprice);            

            quoteEntity = ComputeVAT(quoteEntity);

            var paymentmode = quoteEntity.Contains("gsc_paymentmode")
              ? quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value
              : Decimal.Zero;

            var amountfinanced = Decimal.Zero;
            var netamountfinanced = Decimal.Zero;
            var netdp = Decimal.Zero;
            var cashoutlay = Decimal.Zero;

            //Financing
            if (paymentmode == 100000001 || paymentmode == 100000002)
            {
                downPaymentAmount = ComputeDownpaymentAmount(quoteEntity);
                quoteEntity["gsc_downpaymentamount"] = new Money(downPaymentAmount);

                netdp = ComputeNetDownPayment(quoteEntity);
                quoteEntity["gsc_netdownpayment"] = new Money(netdp);
                quoteEntity["gsc_downpayment"] = new Money(netdp);    

                amountfinanced = ComputeAmountFinanced(quoteEntity);
                quoteEntity["gsc_amountfinanced"] = new Money(amountfinanced);
                quoteEntity["gsc_totalamountfinanced"] = new Money(amountfinanced);

                if (paymentmode == 100000001)
                {
                    cashoutlay = ComputeCashLayout(quoteEntity);
                    quoteEntity["gsc_totalcashoutlay"] = new Money(cashoutlay);
                }
            }

            Entity quoteToUpdate = _organizationService.Retrieve(quoteEntity.LogicalName, quoteEntity.Id, 
                           new ColumnSet(true));

            quoteToUpdate["gsc_lessdiscount"] = quoteEntity["gsc_lessdiscount"];
            quoteToUpdate["gsc_lessdiscountaf"] = quoteEntity["gsc_lessdiscountaf"];
            quoteToUpdate["gsc_totaldiscount"] = quoteEntity["gsc_totaldiscount"];
            quoteToUpdate["gsc_netdownpayment"] = new Money(netdp);
            quoteToUpdate["gsc_downpayment"] = new Money(netdp);
            quoteToUpdate["gsc_netprice"] = new Money(netprice);
            quoteToUpdate["gsc_totalcashoutlay"] = new Money(cashoutlay);
            quoteToUpdate["gsc_amountfinanced"] = new Money(amountfinanced);
            quoteToUpdate["gsc_totalamountfinanced"] = new Money(amountfinanced);
            quoteToUpdate["gsc_vatablesales"] = quoteEntity["gsc_vatablesales"];
            quoteToUpdate["gsc_vatexemptsales"] = quoteEntity["gsc_vatexemptsales"];
            quoteToUpdate["gsc_zeroratedsales"] = quoteEntity["gsc_zeroratedsales"];
            quoteToUpdate["gsc_totalsales"] = quoteEntity["gsc_totalsales"];
            quoteToUpdate["gsc_vatamount"] = quoteEntity["gsc_vatamount"];
            quoteToUpdate["gsc_totalamountdue"] = quoteEntity["gsc_totalamountdue"];
            quoteToUpdate["gsc_downpaymentamount"] = quoteEntity["gsc_downpaymentamount"];

            //Update Monthly Amortization
            CheckMonthlyAmortizationRecord(quoteToUpdate);

            _organizationService.Update(quoteToUpdate);

            _tracingService.Trace("Ended SetLessDiscountValues method...");
        }


        //TO BE DELETED
        //Created By: Raphael Herrera, Created On: 12-22-2016
        //Validates Values in Discounts to be aligned with Total Discount Amount
        private Entity ValidateDiscounts(Entity quoteEntity)
        {
            _tracingService.Trace("Started ValidateDiscounts Method...");
            Decimal totalDiscountAmount = quoteEntity.Contains("totaldiscountamount") ? quoteEntity.GetAttributeValue<Money>("totaldiscountamount").Value : 0;
            Double applyToDP = quoteEntity.Contains("gsc_applytodppercentage") ? quoteEntity.GetAttributeValue<Double>("gsc_applytodppercentage") : 0;
            Double applyToAF = quoteEntity.Contains("gsc_applytoafpercentage") ? quoteEntity.GetAttributeValue<Double>("gsc_applytoafpercentage") : 0;
            Double applyToUP = quoteEntity.Contains("gsc_applytouppercentage") ? quoteEntity.GetAttributeValue<Double>("gsc_applytouppercentage") : 0;

            Decimal DPAmount = totalDiscountAmount * ((decimal)applyToDP / 100);
            Decimal AFAmount = totalDiscountAmount * ((decimal)applyToAF / 100);
            Decimal UPAmount = totalDiscountAmount * ((decimal)applyToUP / 100);

            quoteEntity["gsc_applytodpamount"] = new Money(DPAmount);
            quoteEntity["gsc_applytoafamount"] = new Money(AFAmount);
            quoteEntity["gsc_applytoupamount"] = new Money(UPAmount);
            quoteEntity["gsc_lessdiscount"] = new Money(DPAmount);
            quoteEntity["gsc_lessdiscountaf"] = new Money(AFAmount);
            quoteEntity["gsc_totaldiscount"] = new Money(UPAmount);


            _tracingService.Trace("Ending ValidateDiscounts Method...");
            return quoteEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 2/2/2017
        /*Purpose: Compute Down Payment Amount = downpayment - discount
         * Being called by the public methods when net downpayment needs to be recomputed */
        private Decimal ComputeDownpaymentAmount(Entity quoteEntity)
        {
            Decimal netPrice = quoteEntity.Contains("gsc_netprice")
                ? quoteEntity.GetAttributeValue<Money>("gsc_netprice").Value
                : Decimal.Zero;
            Double downPaymentPercentage = quoteEntity.Contains("gsc_downpaymentpercentage")
                ? quoteEntity.GetAttributeValue<Double>("gsc_downpaymentpercentage")
                : 0;
            Decimal downPaymentAmount = Decimal.Zero;

            downPaymentAmount = netPrice * (Decimal)(downPaymentPercentage / 100);

            return downPaymentAmount;
        }


        //Created By: Leslie Baliguat
        /*Purpose: Compute Net Downpayment = downpayment - discount
         * Being called by the public methods when net downpayment needs to be recomputed */
        private Decimal ComputeNetDownPayment(Entity quoteEntity)
        {
            _tracingService.Trace("Started ComputeNetDownPayment method...");

            var lessdiscount = quoteEntity.Contains("gsc_lessdiscount")
                ? quoteEntity.GetAttributeValue<Money>("gsc_lessdiscount").Value
                : 0;
            var downpayment = quoteEntity.Contains("gsc_downpaymentamount")
                ? quoteEntity.GetAttributeValue<Money>("gsc_downpaymentamount").Value
                : 0;
            var netdp = new Decimal(0);

            if (downpayment != 0)
            {
                netdp = downpayment - lessdiscount;
            }
            else
            {
                netdp = downpayment;
            }
            netdp = netdp < 0 ? 0 : netdp;


            _tracingService.Trace("Ended ComputeNetDownPayment method...");

            return netdp;
        }

        //Created By: Leslie Baliguat
        /*Purpose: ComputeAmount Financed = netprice - net downpayment
         * Being called by the public methods when acmount financed needs to be recomputed */
        public Decimal ComputeAmountFinanced(Entity quoteEntity)
        {
            _tracingService.Trace("Started ComputeAmountFinanced method...");

            var grossdp = quoteEntity.Contains("gsc_downpaymentamount")
                ? quoteEntity.GetAttributeValue<Money>("gsc_downpaymentamount").Value
                : 0;
            var netprice = quoteEntity.Contains("gsc_netprice")
                ? quoteEntity.GetAttributeValue<Money>("gsc_netprice").Value
                : 0;

            var amountfinanced = netprice - grossdp;

            _tracingService.Trace("Ended ComputeAmountFinanced method...");

            return amountfinanced;
        }

        //Created By: Leslie Baliguat
        /*Purpose: Compute Net Amount Financed = amount financed - discount
         * Being called by the public methods when net amount financed needs to be recomputed */
        public Decimal ComputeNetAmountFinanced(Entity quoteEntity)
        {
            _tracingService.Trace("Started ComputeNetAmountFinanced method...");

            var amountfinanced = quoteEntity.Contains("gsc_amountfinanced")
                ? quoteEntity.GetAttributeValue<Money>("gsc_amountfinanced").Value
                : 0;
            var lessdiscount = quoteEntity.Contains("gsc_lessdiscountaf")
                ? quoteEntity.GetAttributeValue<Money>("gsc_lessdiscountaf").Value
                : 0;
            var netamountfinanced = amountfinanced;

            _tracingService.Trace("Ended ComputeNetAmountFinanced method...");

            return netamountfinanced;
        }

        //Created By: Leslie Baliguat
        //Modified By: Raphael Herrera   Modified On: 9/21/2016
        //Modification Purpose: Inlude freight price and CC Add Ons in Computation
        /*Purpose: Compute Net Price = unit price - discount
         * Being called by the public methods when net price needs to be recomputed */
        public Decimal ComputeNetPrice(Entity quoteEntity)
        {
            _tracingService.Trace("Started ComputeNetPrice method - Here...");

            var lessdiscount = quoteEntity.GetAttributeValue<Money>("gsc_totaldiscount") != null
                ? quoteEntity.GetAttributeValue<Money>("gsc_totaldiscount").Value
                : 0;

            var unitprice = quoteEntity.GetAttributeValue<Money>("gsc_unitprice") != null
                ? quoteEntity.GetAttributeValue<Money>("gsc_unitprice").Value
                : 0;

            var colorprice = quoteEntity.GetAttributeValue<Money>("gsc_colorprice") != null
                ? quoteEntity.GetAttributeValue<Money>("gsc_colorprice").Value
                : 0;

            var freightPrice = quoteEntity.GetAttributeValue<Money>("gsc_freightandhandling") != null
                ? quoteEntity.GetAttributeValue<Money>("gsc_freightandhandling").Value
                : 0;

            var ccAddOnsPrice = quoteEntity.GetAttributeValue<Money>("gsc_ccaddons") != null
                ? quoteEntity.GetAttributeValue<Money>("gsc_ccaddons").Value
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

        //Created By: Leslie Baliguat, Created On: 3/17/2016
        /*Purpose: Replicate Net Downpayment to Downpayment in Quote Summary (Computation Tab)
         *         Recompute Total Cash Outlay
         *         Replicate Net AmountFinanded to Amount Financed in Quote Summary (Computation Tab)
         * Registration Details:
         * Event/Message: 
         *      Post/Update: gsc_applytoafamount
         * Primary Entity: Quote
         */
        public Entity ReplicateNetDownPaymentAndNetAmountFinanced(Entity quoteEntity)
        {
            _tracingService.Trace("Started ReplicateDownPayment method...");

            var netdownpayment = quoteEntity.Contains("gsc_netdownpayment")
                ? quoteEntity.GetAttributeValue<Money>("gsc_netdownpayment").Value
                : 0;

            quoteEntity["gsc_downpayment"] = new Money(netdownpayment);
            var amountfinanced = ComputeNetAmountFinanced(quoteEntity);
            quoteEntity["gsc_totalamountfinanced"] = new Money(amountfinanced);

            var cashoutlay = ComputeCashLayout(quoteEntity);
            quoteEntity["gsc_totalcashoutlay"] = new Money(cashoutlay);

            Entity quoteToUpdate = _organizationService.Retrieve(quoteEntity.LogicalName, quoteEntity.Id, new ColumnSet("gsc_downpayment",
                "gsc_totalcashoutlay", "gsc_totalamountfinanced", "gsc_netmonthlyamortization", "gsc_financingschemeid", "gsc_dealerid", "gsc_branchid", "gsc_recordownerid"));
            quoteToUpdate["gsc_downpayment"] = new Money(netdownpayment);
            quoteToUpdate["gsc_totalcashoutlay"] = new Money(cashoutlay);
            quoteToUpdate["gsc_totalamountfinanced"] = new Money(amountfinanced);


            _organizationService.Update(quoteToUpdate);

            _tracingService.Trace("Ended ReplicateDownPayment method...");

            return quoteEntity;
        }

        //Created By: Leslie Baliguat
        /*Purpose: Compute Cash Outlay = downpayment + chattel fee + insurance
         * Being called by the public methods when cash outlay needs to be recomputed */
        public Decimal ComputeCashLayout(Entity quoteEntity)
        {
            _tracingService.Trace("Started ComputeCashLayout method...");

            var downpayment = quoteEntity.Contains("gsc_downpayment")
                ? quoteEntity.GetAttributeValue<Money>("gsc_downpayment").Value
                : 0;
            var chattel = quoteEntity.Contains("gsc_chattelfee")
                ? quoteEntity.GetAttributeValue<Money>("gsc_chattelfee").Value
                : 0;
            var insurance = quoteEntity.Contains("gsc_insurance")
                ? quoteEntity.GetAttributeValue<Money>("gsc_insurance").Value
                : 0;
            var charges = quoteEntity.GetAttributeValue<Money>("gsc_othercharges") != null
                ? quoteEntity.GetAttributeValue<Money>("gsc_othercharges").Value
                : Decimal.Zero;
            var accessories = quoteEntity.GetAttributeValue<Money>("gsc_accessories") != null
                ? quoteEntity.GetAttributeValue<Money>("gsc_accessories").Value
                : Decimal.Zero;

            var cashoutlay = downpayment + chattel + insurance + charges + accessories;

            _tracingService.Trace("Ended ComputeCashLayout method...");

            return cashoutlay;
        }

        //Created By : Leslie Baliguat,  Created On : 3/28/2016
        /*Purpose: If free chatel fee field is not selected, check which chattel fee is applicable to vehicle based on its unit price and selected bank
         * hence chattel fee is zero (free)
       * Registration Details:
       * Event/Message: 
       *      Pre/Create: 
       *      Post/Update: Model Description
       *                   Bank Id
         *                 Free Chatel Fee
       * Primary Entity: Quote
       */
        public Entity SetChattelFeeAmount(Entity quoteEntity, String message)
        {
            _tracingService.Trace("Started SetChattelFeeAmount method..");

            var paymentMode = quoteEntity.Contains("gsc_paymentmode") ? quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value : 0;

            if (paymentMode != 100000001)
                return null;

            var bankId = quoteEntity.GetAttributeValue<EntityReference>("gsc_bankid") != null
                ? quoteEntity.GetAttributeValue<EntityReference>("gsc_bankid").Id
                : Guid.Empty;

            Decimal unitPriceAmount = quoteEntity.Contains("gsc_unitprice")
                ? quoteEntity.GetAttributeValue<Money>("gsc_unitprice").Value
                : Decimal.Zero;

            Decimal chattelfee = Decimal.Zero;

            if (quoteEntity.GetAttributeValue<Boolean>("gsc_freechattelfee") == false)
            {
                _tracingService.Trace("Chattel Fee is not free.");

                //Retrieve all Chattel Fee records with the same Bank
                EntityCollection chattelFeeRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_chattelfee", "gsc_bankid", bankId, _organizationService, "gsc_loanamount", OrderType.Ascending,
                    new[] { "gsc_loanamount", "gsc_chattelfeeamount" });

                _tracingService.Trace("Chattel Fee Records: " + chattelFeeRecords.Entities.Count);
                if (chattelFeeRecords != null && chattelFeeRecords.Entities.Count > 0)
                {
                    Int32 chattelFeeRecordsCount = chattelFeeRecords.Entities.Count;
                    _tracingService.Trace("Unit Price: " + unitPriceAmount);
                    //Loop through all Chattel Fee records to match Loan Amount
                    for (int x = 0; x <= chattelFeeRecordsCount - 2; x++)
                    {
                        _tracingService.Trace("From: " + (Int32)(Decimal)chattelFeeRecords.Entities[x].GetAttributeValue<Money>("gsc_loanamount").Value + "To: " + (Int32)(Decimal)chattelFeeRecords.Entities[x + 1].GetAttributeValue<Money>("gsc_loanamount").Value);
                        //if (unitPriceAmount >= (Decimal)chattelFeeRecords.Entities[x].GetAttributeValue<Money>("gsc_loanamount").Value && unitPriceAmount <= (Decimal)chattelFeeRecords.Entities[x+1].GetAttributeValue<Money>("gsc_loanamount").Value)
                        if (Enumerable.Range(((Int32)(Decimal)chattelFeeRecords.Entities[x].GetAttributeValue<Money>("gsc_loanamount").Value), ((Int32)(Decimal)chattelFeeRecords.Entities[x + 1].GetAttributeValue<Money>("gsc_loanamount").Value)).Contains((Int32)unitPriceAmount))
                        {
                            chattelfee = (Decimal)chattelFeeRecords.Entities[x].GetAttributeValue<Money>("gsc_chattelfeeamount").Value;
                            _tracingService.Trace("Range Chattel Fee: " + chattelfee);
                        }
                    }

                    //Check retrieved Chattel Fee record count if odd or even
                    Boolean chattelFeeRecordsOddOrEven = chattelFeeRecordsCount % 2 == 0;

                    //If retrieved Chattel Fee record count is even, validate if Unit Price is greater than the last Loan Amount value
                    //if (!chattelFeeRecordsOddOrEven)
                    //{
                    //    _tracingService.Trace("Chattel Fee is even...");
                    if (unitPriceAmount > (Decimal)chattelFeeRecords.Entities[chattelFeeRecordsCount - 1].GetAttributeValue<Money>("gsc_loanamount").Value)
                    {
                        chattelfee = (Decimal)chattelFeeRecords.Entities[chattelFeeRecordsCount - 1].GetAttributeValue<Money>("gsc_chattelfeeamount").Value;
                        _tracingService.Trace("Condition1 Chattel Fee: " + chattelfee);
                    }
                    //}

                    if (unitPriceAmount < (Decimal)chattelFeeRecords.Entities[0].GetAttributeValue<Money>("gsc_loanamount").Value)
                    {
                        chattelfee = (Decimal)0.00;
                        _tracingService.Trace("Condition2 Chattel Fee: " + chattelfee);
                    }
                }
            }

            quoteEntity["gsc_chattelfee"] = new Money(chattelfee);
            quoteEntity["gsc_chattelfeeeditable"] = new Money(chattelfee);

            var cashoutlay = ComputeCashLayout(quoteEntity);
            quoteEntity["gsc_totalcashoutlay"] = new Money(cashoutlay);

            if (message.Equals("Update"))
            {
                Entity quoteToUpdate = _organizationService.Retrieve(quoteEntity.LogicalName, quoteEntity.Id, new ColumnSet("gsc_chattelfee", "gsc_totalcashoutlay"));
                quoteToUpdate["gsc_chattelfee"] = new Money(chattelfee);
                quoteToUpdate["gsc_chattelfeeeditable"] = new Money(chattelfee);
                quoteToUpdate["gsc_totalcashoutlay"] = new Money(cashoutlay);

                _organizationService.Update(quoteEntity);
            }

            _tracingService.Trace("Ended SetChattelFeeAmount method..");

            return quoteEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 9/15/2016
        /*Purpose: Replicate gsc_chattelfeeeditable value to gsc_chattelfee
         * Registration Details:
         * Event/Message:
         *      Post/Update: Chattel Fee (gsc_chattelfeeeditable)
         * Primary Entity: Quote
         */
        public Entity ReplicateEditableChattelFee(Entity quoteEntity)
        {
            _tracingService.Trace("Started ReplicateEditableChattelFee method..");

            //Return if Free Chattel Fee is checked
            if (quoteEntity.GetAttributeValue<Boolean>("gsc_freechattelfee") == true) { return null; }

            Decimal newChattelFeeAmount = quoteEntity.Contains("gsc_chattelfeeeditable")
                ? quoteEntity.GetAttributeValue<Money>("gsc_chattelfeeeditable").Value
                : Decimal.Zero;

            quoteEntity["gsc_chattelfee"] = new Money(newChattelFeeAmount);

            Decimal cashOutlay = ComputeCashLayout(quoteEntity);

            quoteEntity["gsc_totalcashoutlay"] = new Money(cashOutlay);

            _organizationService.Update(quoteEntity);

            _tracingService.Trace("Ended ReplicateEditableChattelFee method..");
            return quoteEntity;
        }

        //Created By: Raphael Herrera, Cretaed On: 4/26/2016
        /*Purpose: Update Quote with same fields of selected insurance id
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Quote
         */
        public EntityCollection ReplicateInsuranceInformation(Entity quoteEntity)
        {
            _tracingService.Trace("Starting ReplicateInsuranceInformation method...");

            //get insurance id selected from quote
            var quoteInsuranceId = quoteEntity.GetAttributeValue<EntityReference>("gsc_insuranceid") != null
                ? quoteEntity.GetAttributeValue<EntityReference>("gsc_insuranceid")
                : null;

            if (quoteInsuranceId != null)
            {
                _tracingService.Trace("Retrieving insurance fields...");

                EntityCollection insuranceEC = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_insurance", "gsc_cmn_insuranceid", quoteInsuranceId.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_providercompanyid", "gsc_totalpremium", "gsc_cmn_insuranceid" });

                if (insuranceEC != null && insuranceEC.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieved insurance fields...");

                    _tracingService.Trace("Successfully retrieved Insurance fields...");

                    Entity insurance = insuranceEC.Entities[0];

                    quoteEntity["gsc_providercompany"] = insurance.GetAttributeValue<EntityReference>("gsc_providercompanyid") != null
                        ? insurance.GetAttributeValue<EntityReference>("gsc_providercompanyid").Name
                        : String.Empty;
                    quoteEntity["gsc_totalpremium"] = insurance.Contains("gsc_totalpremium")
                        ? insurance.GetAttributeValue<Money>("gsc_totalpremium")
                        : new Money(0);
                    quoteEntity["gsc_originaltotalpremium"] = insurance.Contains("gsc_totalpremium")
                        ? insurance.GetAttributeValue<Money>("gsc_totalpremium")
                        : new Money(0);

                    _tracingService.Trace("Updating Quote entity...");
                    _organizationService.Update(quoteEntity);

                    _tracingService.Trace("Successfully updated quote entity... Ending method...");
                }

                return RelateInsuranceCoverage(quoteEntity, quoteInsuranceId.Id);
            }
            else
            {
                ClearInsuranceInformation(quoteEntity);
                DeleteInsuranceCoverage(quoteEntity);
            }

            return null;
        }

        private void ClearInsuranceInformation(Entity quoteEntity)
        {
            Entity quoteToUpdate = _organizationService.Retrieve(quoteEntity.LogicalName, quoteEntity.Id,
              new ColumnSet("gsc_providercompany", "gsc_originaltotalpremium", "gsc_totalpremium", "gsc_free"));

            quoteToUpdate["gsc_providercompany"] = null;
            quoteToUpdate["gsc_originaltotalpremium"] = null;
            quoteToUpdate["gsc_totalpremium"] = null;
            quoteToUpdate["gsc_free"] = false;

            _organizationService.Update(quoteToUpdate);
        }

        private void DeleteInsuranceCoverage(Entity quoteEntity)
        {
            EntityCollection coverageCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_quotecoverageavailable", "gsc_quoteid", quoteEntity.Id, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_quoteid" });

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

        //Created By: Raphael Herrera, Cretaed On: 4/26/2016
        /*Purpose: Create QuoteCoverage based on inurance selected
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Quote
         */
        private EntityCollection RelateInsuranceCoverage(Entity quoteEntity, Guid insuranceId)
        {
            _tracingService.Trace("Starting RelateInsuranceCoverage method...");

            DeleteInsuranceCoverage(quoteEntity);

            var insuranceConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("gsc_insuranceid", ConditionOperator.Equal, insuranceId)
                            };
            EntityCollection insuranceCoverageEC = CommonHandler.RetrieveRecordsByConditions("gsc_cmn_insurancecoverage", insuranceConditionList, _organizationService,
                    null, OrderType.Ascending, new[] { "gsc_insurancecoveragepn", "gsc_suminsured", "gsc_premium" });

            _tracingService.Trace("Retrieved insurance coverage...");
            _tracingService.Trace(insuranceCoverageEC.Entities.Count.ToString() + " Records retrieved...");

            EntityCollection quoteCoverageEC = new EntityCollection();
            Entity quoteCoverage = new Entity("gsc_cmn_quotecoverageavailable");
            foreach (Entity insuranceCoverage in insuranceCoverageEC.Entities)
            {
                quoteCoverage["gsc_suminsured"] = insuranceCoverage.GetAttributeValue<Money>("gsc_suminsured");
                quoteCoverage["gsc_premium"] = insuranceCoverage.GetAttributeValue<Money>("gsc_premium");
                quoteCoverage["gsc_quotecoverageavailablepn"] = insuranceCoverage.GetAttributeValue<String>("gsc_insurancecoveragepn").ToString();
                quoteCoverage["gsc_quoteid"] = new EntityReference(quoteEntity.LogicalName, quoteEntity.Id);

                quoteCoverageEC.Entities.Add(quoteCoverage);
                _tracingService.Trace("Creating quote coverage " + insuranceCoverage.GetAttributeValue<String>("gsc_insurancecoveragepn").ToString());

                _organizationService.Create(quoteCoverage);
            }
            _tracingService.Trace("Ending RelateInsuranceCoverage method...");

            return quoteCoverageEC;
        }

        //Created By: Leslie Baliguat, Created On:  4/25/2016
        /*Purpose: Retrieve Customer Information in Account/Contact based on Potential Customer field and 
         *         Populate this in Quote's Customer Information
       * Registration Details:
       * Event/Message: 
       *      Pre/Create: 
       *      Post/Update: Potential Customer
       * Primary Entity: Quote
       */
        public Entity PopulateCustomerInformation(Entity quoteEntity, String message)
        {
            _tracingService.Trace("Started PopulateCustomerInformation method ....");

            if (quoteEntity.Contains("customerid"))
            {
                var customerid = quoteEntity.GetAttributeValue<EntityReference>("customerid") != null
                    ? quoteEntity.GetAttributeValue<EntityReference>("customerid").Id
                    : Guid.Empty;

                //Retrieve customer info in account 
                EntityCollection accountRecords = CommonHandler.RetrieveRecordsByOneValue("account", "accountid", customerid, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_countryid", "gsc_provinceid", "gsc_cityid", "address1_line1", "address1_postalcode", "telephone1" });

                if (accountRecords != null && accountRecords.Entities.Count > 0)
                {
                    _tracingService.Trace("Retrieving Info from Account ....");

                    var accountEntity = accountRecords.Entities[0];

                    var country = accountEntity.Contains("gsc_countryid")
                        ? accountEntity.GetAttributeValue<EntityReference>("gsc_countryid").Name
                        : String.Empty;

                    var province = accountEntity.Contains("gsc_provinceid")
                        ? accountEntity.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                        : String.Empty;

                    var city = accountEntity.Contains("gsc_cityid")
                        ? accountEntity.GetAttributeValue<EntityReference>("gsc_cityid").Name
                        : String.Empty;

                    var street = accountEntity.Contains("address1_line1")
                      ? accountEntity.GetAttributeValue<String>("address1_line1")
                      : String.Empty;

                    var zipcode = accountEntity.Contains("address1_postalcode")
                     ? accountEntity.GetAttributeValue<String>("address1_postalcode")
                     : String.Empty;

                    quoteEntity["gsc_address"] = street + " " + city + " " + province + " " + country + " " + zipcode;
                    quoteEntity["gsc_contactno"] = accountEntity.Contains("telephone1")
                        ? accountEntity.GetAttributeValue<String>("telephone1")
                        : String.Empty;
                    quoteEntity["gsc_alternatecontactno"] = String.Empty;
                }

                else
                {
                    //Retrieve customer info in contact
                    EntityCollection contactRecords = CommonHandler.RetrieveRecordsByOneValue("contact", "contactid", customerid, _organizationService, null, OrderType.Ascending,
                        new[] { "gsc_countryid", "gsc_provinceid", "gsc_cityid", "address1_line1", "address1_postalcode", "mobilephone", "telephone1" });

                    if (contactRecords != null && contactRecords.Entities.Count > 0)
                    {
                        _tracingService.Trace("Retrieving Infor from contact ....");

                        var contacEntity = contactRecords.Entities[0];

                        var country = contacEntity.Contains("gsc_countryid")
                                               ? contacEntity.GetAttributeValue<EntityReference>("gsc_countryid").Name
                                               : String.Empty;

                        var province = contacEntity.Contains("gsc_provinceid")
                            ? contacEntity.GetAttributeValue<EntityReference>("gsc_provinceid").Name
                            : String.Empty;

                        var city = contacEntity.Contains("gsc_cityid")
                            ? contacEntity.GetAttributeValue<EntityReference>("gsc_cityid").Name
                            : String.Empty;

                        var street = contacEntity.Contains("address1_line1")
                          ? contacEntity.GetAttributeValue<String>("address1_line1")
                          : String.Empty;

                        var zipcode = contacEntity.Contains("address1_postalcode")
                         ? contacEntity.GetAttributeValue<String>("address1_postalcode")
                         : String.Empty;

                        quoteEntity["gsc_address"] = street + " " + city + " " + province + " " + country + " " + zipcode;
                        quoteEntity["gsc_contactno"] = contacEntity.Contains("mobilephone")
                            ? contacEntity.GetAttributeValue<String>("mobilephone")
                            : String.Empty;
                        quoteEntity["gsc_alternatecontactno"] = contacEntity.Contains("telephone1")
                            ? contacEntity.GetAttributeValue<String>("telephone1")
                            : String.Empty;

                    }
                }

                if (message.Equals("Update"))
                {
                    Entity quoteToUpdate = _organizationService.Retrieve(quoteEntity.LogicalName, quoteEntity.Id,
                      new ColumnSet("gsc_address", "gsc_contactno", "gsc_alternatecontactno"));

                    quoteToUpdate["gsc_address"] = quoteEntity["gsc_address"];
                    quoteToUpdate["gsc_contactno"] = quoteEntity["gsc_contactno"];
                    quoteToUpdate["gsc_alternatecontactno"] = quoteEntity["gsc_alternatecontactno"];

                    _organizationService.Update(quoteToUpdate);

                    _tracingService.Trace("Updated Contact Information ....");
                }
            }

            _tracingService.Trace("Ended PopulateCustomerInformation method ....");

            return quoteEntity;
        }


        //Created By: Raphael Herrera, Created On:  6/24/2016
        //Modified By: Raphael Herrera, Modified On: 9/26/2016
        //Modified By: Jerome Anthony Gerero, Modified On: 10/5/2016
        //Modified By: Raphael Herrera, Modified On: 11/07/2016
        //Modified By: Jessica Casupanan, Modified On: 12/6/2016 - taxtype to taxid, retrieve tax category record of owning branch tax id
        /*Purpose: Computes for values of VAT related fields (VATable Sales, VAT-Exempt Sales, Zero Rated Sales,
        * Total Sales, VAT Amount, Total Amount Due)
        *       
       * Registration Details:
       * Event/Message: 
       *      Pre/Create: 
       *      Post/Update:
       * Primary Entity: Quote
       */
        public Entity ComputeVAT(Entity quoteEntity)
        {
            _tracingService.Trace("Started ComputeVAT method...");

            var customer = quoteEntity.Contains("customerid")
                    ? quoteEntity.GetAttributeValue<EntityReference>("customerid") != null
                    ? quoteEntity.GetAttributeValue<EntityReference>("customerid")
                    : null
                    : null;

            var vehicle = quoteEntity.Contains("gsc_productid")
                    ? quoteEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                    ? quoteEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty
                : Guid.Empty;

            EntityCollection customerCollection = null;
            if (customer != null)
            {
                customerCollection = CommonHandler.RetrieveRecordsByOneValue(customer.LogicalName, customer.LogicalName + "id", customer.Id, _organizationService, null, OrderType.Ascending,
                   new[] { "gsc_taxrate", "gsc_taxid" });
            }

            EntityCollection vehicleCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", vehicle, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_taxrate", "gsc_taxid" });

            if ((vehicleCollection != null && vehicleCollection.Entities.Count > 0) && (customerCollection != null && customerCollection.Entities.Count > 0))
            {
                _tracingService.Trace("Vehicle and Customer are not null.");

                Entity customerEntity = customerCollection.Entities[0];
                Entity vehicleEntity = vehicleCollection.Entities[0];

                _tracingService.Trace(customerEntity.GetAttributeValue<EntityReference>("gsc_taxid") + " " + customerEntity.Contains("gsc_taxrate"));
                _tracingService.Trace(vehicleEntity.GetAttributeValue<EntityReference>("gsc_taxid") + " " + vehicleEntity.Contains("gsc_taxrate"));

                if (customerEntity.GetAttributeValue<EntityReference>("gsc_taxid") == null || customerEntity.Contains("gsc_taxrate") == false)
                    throw new InvalidPluginExecutionException("Cannot proceed with your transaction.\n Please setup tax for Customer.");

                if (vehicleEntity.GetAttributeValue<EntityReference>("gsc_taxid") == null || vehicleEntity.Contains("gsc_taxrate") == false)
                    throw new InvalidPluginExecutionException("Cannot proceed with your transaction.\n Please setup tax for Product Catalog.");

                var customerTaxRate = (decimal)customerEntity.GetAttributeValue<Double>("gsc_taxrate");

                if (customerTaxRate != 0)
                    customerTaxRate = customerTaxRate / 100;

                var vehicleTaxRate = (decimal)vehicleEntity.GetAttributeValue<Double>("gsc_taxrate"); ;

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

                decimal netPrice = quoteEntity.Contains("gsc_netprice") ? quoteEntity.GetAttributeValue<Money>("gsc_netprice").Value : 0;
                decimal insurance = quoteEntity.Contains("gsc_insurance") ? quoteEntity.GetAttributeValue<Money>("gsc_insurance").Value : 0;
                decimal otherCharges = quoteEntity.Contains("gsc_othercharges") ? quoteEntity.GetAttributeValue<Money>("gsc_othercharges").Value : 0;
                decimal paymentMode = quoteEntity.Contains("gsc_paymentmode") ? quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value : 0;
                decimal accessories = quoteEntity.Contains("gsc_accessories") ? quoteEntity.GetAttributeValue<Money>("gsc_accessories").Value : 0;

                decimal sales = 0;

                //Financing
                if (paymentMode == 100000001)
                    sales = (netPrice) / (1 + vehicleTaxRate);
                //Cash/Bank PO/Company PO
                else
                    //sales = (netPrice + otherCharges + insurance + accessories) / (1 + vehicleTaxRate);
                    sales = (netPrice) / (1 + vehicleTaxRate);//changed as per bug#15957. Modified 4-10-17
                sales = Math.Round(sales, 2);

                if (taxCategory == 100000000)//VATable
                {
                    _tracingService.Trace("Tax Category is VATable...");

                    quoteEntity["gsc_vatablesales"] = new Money(sales);
                    quoteEntity["gsc_vatexemptsales"] = new Money(0);
                    quoteEntity["gsc_zeroratedsales"] = new Money(0);
                    quoteEntity["gsc_totalsales"] = new Money(sales);
                    quoteEntity["gsc_vatamount"] = new Money(Math.Round(sales * customerTaxRate, 2));
                    quoteEntity["gsc_totalamountdue"] = new Money(Math.Round(sales + (sales * customerTaxRate), 2));

                }
                else if (taxCategory == 100000002)//Vat Exempt
                {
                    _tracingService.Trace("Tax category is VAT exempt...");
                    quoteEntity["gsc_vatablesales"] = new Money(0);
                    quoteEntity["gsc_vatexemptsales"] = new Money(sales);
                    quoteEntity["gsc_zeroratedsales"] = new Money(0);
                    quoteEntity["gsc_totalsales"] = new Money(sales);
                    quoteEntity["gsc_vatamount"] = new Money(0);
                    quoteEntity["gsc_totalamountdue"] = new Money(sales);
                }
                else if (taxCategory == 100000001)//Zero Rated
                {
                    _tracingService.Trace("Tax Category is Zero Rated...");
                    quoteEntity["gsc_vatablesales"] = new Money(0);
                    quoteEntity["gsc_vatexemptsales"] = new Money(0);
                    quoteEntity["gsc_zeroratedsales"] = new Money(sales);
                    quoteEntity["gsc_totalsales"] = new Money(sales);
                    quoteEntity["gsc_vatamount"] = new Money(0);
                    quoteEntity["gsc_totalamountdue"] = new Money(sales);
                }
            }

            _tracingService.Trace("Ending ComputeVAT method...");
            return quoteEntity;

        }

        //Created By: Leslie G. Baliguat
        public void DeleteExistingCabChassis(Entity quoteEntity)
        {
            _tracingService.Trace("Started DeleteExistingCabChassis method..");

            var productConditionList = new List<ConditionExpression>
                    {
                        new ConditionExpression("gsc_quoteid", ConditionOperator.Equal, quoteEntity.Id)
                    };

            EntityCollection relatedProducts = CommonHandler.RetrieveRecordsByConditions("gsc_sls_quotecabchassis", productConditionList, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_quotecabchassispn" });

            if (relatedProducts != null && relatedProducts.Entities.Count > 0)
            {
                foreach (var relatedProduct in relatedProducts.Entities)
                {
                    _organizationService.Delete(relatedProduct.LogicalName, relatedProduct.Id);
                }
            }

            _tracingService.Trace("Ended DeleteExistingCabChassis method..");
        }


        //Created By: Raphael Herrera, Created On:  9/26/2016
        /*Purpose: Set Financing check box of QuoteCabChassis accordingly on change of payment mode
        *       
        * Registration Details:
        * Event/Message: 
        *      Post/Update: gsc_paymentmode
        * Primary Entity: Quote
        */
        public void SetCabChassisFinancing(Entity quoteEntity)
        {
            _tracingService.Trace("Started SetCabChassisFinancing Method...");

            //Retrieve Quote Cab Chassis records associated to quote
            EntityCollection quoteCCCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_sls_quotecabchassis", "gsc_quoteid", quoteEntity.Id, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_financing" });

            _tracingService.Trace("Quote Cab Chassis Records Retrieved: " + quoteCCCollection.Entities.Count);
            if (quoteCCCollection.Entities.Count > 0)
            {
                decimal paymentMode = quoteEntity.GetAttributeValue<OptionSetValue>("gsc_paymentmode").Value;

                bool financing = false;
                //financing
                if (paymentMode == 100000001)
                    financing = true;
                else
                    financing = false;

                foreach (Entity quoteCC in quoteCCCollection.Entities)
                {
                    quoteCC["gsc_financing"] = financing;

                    _organizationService.Update(quoteCC);
                    _tracingService.Trace("Updated Quote Cab Chassis Financing to " + financing);
                }

            }
            _tracingService.Trace("Ending SetCabChassisFinancing Method...");
        }


        //Created By: Raphael Herrera, Created On:  9/26/2016
        /*Purpose: Set Total Cash Outlay, Amount Financed, Net Amount Financed and Net Monthly Amortization fields to 0.00
        *       
        * Registration Details:
        * Event/Message: 
        *      Post/Update: gsc_paymentmode
        * Primary Entity: Quote
        */
        public void ClearFinancingFields(Entity quoteEntity)
        {
            _tracingService.Trace("Started ClearFinancingFields Method...");

            quoteEntity["gsc_amountfinanced"] = new Money(0);
            quoteEntity["gsc_totalcashoutlay"] = new Money(0);
            quoteEntity["gsc_netmonthlyamortization"] = new Money(0);
            quoteEntity["gsc_totalamountfinanced"] = new Money(0);

            _organizationService.Update(quoteEntity);
            _tracingService.Trace("Quote updated...");

            _tracingService.Trace("Ending ClearFinancingFields Method");
        }

        //Created By: Raphael Herrera, Created On: 9/27/2016
        //Computes and returns unit price as inclusive of tax
        private decimal ComputeUnitPrice(Entity productEntity, Entity quoteEntity)
        {
            _tracingService.Trace("Started ComputeUnitPrice Method...");

         //   var priceLevelId = productEntity.Contains("pricelevelid") ? productEntity.GetAttributeValue<EntityReference>("pricelevelid").Id
               // : Guid.Empty;

            decimal sellPrice = 0;

            PriceListHandler priceListHandler = new PriceListHandler(_organizationService, _tracingService);
            priceListHandler.itemType = 0;
            priceListHandler.productFieldName = "gsc_productid";
            List<Entity> latestPriceList = priceListHandler.RetrievePriceList(quoteEntity, 100000000, 100000003);

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

            //Retrieve price level associated with product
           /* EntityCollection priceLevelCollection = CommonHandler.RetrieveRecordsByOneValue("pricelevel", "pricelevelid", priceLevelId, _organizationService,
                null, OrderType.Ascending, new[] { "gsc_taxstatus", "statecode", "begindate", "enddate" });

            _tracingService.Trace("Price Level Records Retrieved: " + priceLevelCollection.Entities.Count);

            if (priceLevelCollection != null && priceLevelCollection.Entities.Count > 0)
            {
                Entity priceLevelEntity = priceLevelCollection.Entities[0];

                if (priceLevelEntity.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
                {
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

        //Created By: Raphael Herrera, Created On: 9/27/2016
        //Set Payment Summary fields to 0.
        private Entity PopulatePaymentSummary(Entity quoteEntity)
        {
            _tracingService.Trace("Started PopulatePaymentSummary Method...");
            quoteEntity["gsc_unitprice"] = new Money(0);
            quoteEntity["gsc_ccaddons"] = new Money(0);
            quoteEntity["gsc_colorprice"] = new Money(0);
            quoteEntity["gsc_freightandhandling"] = new Money(0);
            quoteEntity["gsc_totaldiscount"] = new Money(0);
            quoteEntity["gsc_netprice"] = new Money(0);
            quoteEntity["gsc_accessories"] = new Money(0);
            quoteEntity["gsc_insurance"] = new Money(0);
            quoteEntity["gsc_chattelfee"] = new Money(0);
            quoteEntity["gsc_othercharges"] = new Money(0);
            quoteEntity["gsc_downpayment"] = new Money(0);
            quoteEntity["gsc_vatablesales"] = new Money(0);
            quoteEntity["gsc_vatexemptsales"] = new Money(0);
            quoteEntity["gsc_zeroratedsales"] = new Money(0);
            quoteEntity["gsc_totalsales"] = new Money(0);
            quoteEntity["gsc_vatamount"] = new Money(0);
            quoteEntity["gsc_totalamountdue"] = new Money(0);
            quoteEntity["gsc_totalcashoutlay"] = new Money(0);
            quoteEntity["gsc_amountfinanced"] = new Money(0);
            quoteEntity["gsc_totalamountfinanced"] = new Money(0);
            quoteEntity["gsc_netmonthlyamortization"] = new Money(0);

            _tracingService.Trace("Ending PopulatePaymentSummary Method...");
            return quoteEntity;

        }

        //Created By: Jerome Anthony Gerero, Created On: 10/4/2016
        /*Purpose: Close related opportunity record
         * Registration Details:
         * Event/Message: 
         * Pre/Create: 
         * Post/Update: Close Quote? = gsc_closequote
         * Primary Entity: Quote
         */
        public Entity CloseRelatedOpportunity(Entity quoteEntity)
        {
            _tracingService.Trace("Started CloseRelatedOpportunity Method...");

            Guid opportunityId = quoteEntity.GetAttributeValue<EntityReference>("opportunityid") != null
                ? quoteEntity.GetAttributeValue<EntityReference>("opportunityid").Id
                : Guid.Empty;

            Boolean closeOpportunity = quoteEntity.GetAttributeValue<Boolean>("gsc_closeopportunity");

            Boolean closeQuote = quoteEntity.GetAttributeValue<Boolean>("gsc_closequote");

            EntityCollection opportunityRecords = CommonHandler.RetrieveRecordsByOneValue("opportunity", "opportunityid", opportunityId, _organizationService, null, OrderType.Ascending,
                new[] { "statecode", "statuscode" });

            if (opportunityRecords != null && opportunityRecords.Entities.Count > 0)
            {
                Entity opportunity = opportunityRecords.Entities[0];

                Int32 opportunityStateCode = opportunity.GetAttributeValue<OptionSetValue>("statecode").Value;
                Int32 opportunityStatusCode = opportunity.GetAttributeValue<OptionSetValue>("statuscode").Value;

                EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "opportunityid", opportunityId, _organizationService, null, OrderType.Ascending,
                    new[] { "statecode", "statuscode" });

                if (quoteRecords != null && quoteRecords.Entities.Count > 0)
                {
                    foreach (Entity quote in quoteRecords.Entities)
                    {
                        if (quote.Id != quoteEntity.Id && closeOpportunity == true)
                        {
                            Int32 quoteStateCode = quote.GetAttributeValue<OptionSetValue>("statecode").Value;
                            Int32 quoteStatusCode = quote.GetAttributeValue<OptionSetValue>("statuscode").Value;

                            if (quoteStateCode == 0 || quoteStateCode == 1)
                            {
                                throw new InvalidPluginExecutionException("There are still active or draft quotes with the associated opportunity. These must be closed before the associated opportunity can be closed.");
                            }
                            else if (quoteStateCode == 2)
                            {
                                throw new InvalidPluginExecutionException("There are won quotes associated with the opportunity. The opportunity must be manually updated from in-progress to won.");
                            }
                        }
                    }

                    if (closeQuote == true)
                    {
                        CloseQuoteAsLost(quoteEntity);
                    }

                    if (closeOpportunity == true && opportunityStateCode != 2)
                    {
                        _tracingService.Trace("Closing opportunity as lost..");

                        LoseOpportunityRequest req = new LoseOpportunityRequest();
                        Entity opportunityClose = new Entity("opportunityclose");
                        opportunityClose.Attributes.Add("opportunityid", new EntityReference(opportunity.LogicalName, opportunity.Id));
                        opportunityClose.Attributes.Add("subject", "Lost the opportunity!");
                        req.OpportunityClose = opportunityClose;
                        OptionSetValue o = new OptionSetValue();
                        o.Value = 4;
                        req.Status = o;
                        LoseOpportunityResponse resp = (LoseOpportunityResponse)_organizationService.Execute(req);
                    }
                }
            }            
            else
            {
                if (closeQuote == true)
                {
                    CloseQuoteAsLost(quoteEntity);
                }
            }

            _tracingService.Trace("Ended CloseRelatedOpportunity Method...");
            return quoteEntity;
        }

        //Created By : Jerome Anthony Gerero, Create On : 10/27/2016
        /*Purpose: Retrieve Customer Information in Account/Contact based on Potential Customer field and 
         *         Populate this in Quote's Customer Information
         * Registration Details:
         * Event/Message: 
         * Pre/Create: 
         * Post/Update: Create Order? = gsc_createorder
         * Primary Entity: Quote
         */
        public Entity CreateOrder(Entity quoteEntity)
        {
            _tracingService.Trace("Started CreateOrder Method...");

            Guid opportunityId = quoteEntity.GetAttributeValue<EntityReference>("opportunityid") != null
                ? quoteEntity.GetAttributeValue<EntityReference>("opportunityid").Id
                : Guid.Empty;

            Boolean closeOpportunity = quoteEntity.GetAttributeValue<Boolean>("gsc_closeopportunity");

            Boolean createOrder = quoteEntity.GetAttributeValue<Boolean>("gsc_createorder");

            if (createOrder == false) { return null; }

            EntityCollection opportunityRecords = CommonHandler.RetrieveRecordsByOneValue("opportunity", "opportunityid", opportunityId, _organizationService, null, OrderType.Ascending,
                new[] { "statecode", "statuscode" });

            if (opportunityRecords != null && opportunityRecords.Entities.Count > 0)
            {
                Entity opportunity = opportunityRecords.Entities[0];

                Int32 opportunityStateCode = opportunity.GetAttributeValue<OptionSetValue>("statecode").Value;
                Int32 opportunityStatusCode = opportunity.GetAttributeValue<OptionSetValue>("statuscode").Value;

                CloseQuoteAsWon(quoteEntity);

                if (closeOpportunity == true && opportunityStateCode == 0)
                {
                    _tracingService.Trace("Closing opportunity as won..");
                    WinOpportunityRequest req = new WinOpportunityRequest();
                    Entity opportunityClose = new Entity("opportunityclose");
                    opportunityClose.Attributes.Add("opportunityid", new EntityReference(opportunity.LogicalName, opportunity.Id));
                    opportunityClose.Attributes.Add("subject", "Won the opportunity!");
                    req.OpportunityClose = opportunityClose;
                    OptionSetValue o = new OptionSetValue();
                    o.Value = 3;
                    req.Status = o;
                    WinOpportunityResponse resp = (WinOpportunityResponse)_organizationService.Execute(req);
                }
            }
            else
            {
                CloseQuoteAsWon(quoteEntity);
            }

            _tracingService.Trace("Ended CreateOrder Method...");
            return quoteEntity;
        }

        //Created By : Jerome Anthony Gerero, Created On : 10/27/2016
        //Purpose : Close quote record as won
        private void CloseQuoteAsWon(Entity quoteEntity)
        {
            _tracingService.Trace("Started CloseQuoteAsWon Method...");

            EntityReference product = quoteEntity.GetAttributeValue<EntityReference>("gsc_productid") != null
                ? quoteEntity.GetAttributeValue<EntityReference>("gsc_productid")
                : null;
            EntityReference customer = quoteEntity.GetAttributeValue<EntityReference>("customerid") != null
                ? quoteEntity.GetAttributeValue<EntityReference>("customerid")
                : null;

            Entity salesOrder = new Entity("salesorder");
            salesOrder["quoteid"] = new EntityReference(quoteEntity.LogicalName, quoteEntity.Id);
            salesOrder["gsc_productid"] = product;
            salesOrder["customerid"] = customer;

            _organizationService.Create(salesOrder);

            SetStateRequest request = new SetStateRequest
            {
                EntityMoniker = new EntityReference("quote", quoteEntity.Id),
                State = new OptionSetValue(1),
                Status = new OptionSetValue(2)
            };
            _organizationService.Execute(request);

            WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
            Entity quoteClose = new Entity("quoteclose");
            quoteClose["quoteid"] = new EntityReference(quoteEntity.LogicalName, quoteEntity.Id);
            quoteClose["subject"] = "Quote Won - " + String.Concat(DateTime.UtcNow);
            winQuoteRequest.QuoteClose = quoteClose;
            winQuoteRequest.Status = new OptionSetValue(4);
            _organizationService.Execute(winQuoteRequest);

            _tracingService.Trace("Ended CloseQuoteAsWon Method...");
        }

        //Created By : Jerome Anthony Gerero, Created On : 10/25/2016
        //Purpose : Close quote record as lost
        private void CloseQuoteAsLost(Entity quoteEntity)
        {
            _tracingService.Trace("Started CloseQuoteAsLost Method...");

            if (quoteEntity.GetAttributeValue<OptionSetValue>("statecode").Value != 3)
            {
                _tracingService.Trace("Closing quote..");
                SetStateRequest request = new SetStateRequest
                {
                    EntityMoniker = new EntityReference("quote", quoteEntity.Id),
                    State = new OptionSetValue(1),
                    Status = new OptionSetValue(2)
                };
                _organizationService.Execute(request);

                CloseQuoteRequest req = new CloseQuoteRequest();
                Entity quoteClose = new Entity("quoteclose");
                quoteClose.Attributes.Add("quoteid", new EntityReference(quoteEntity.LogicalName, quoteEntity.Id));
                quoteClose.Attributes.Add("subject", "Quote closed!");
                req.QuoteClose = quoteClose;
                req.RequestName = "CloseQuote";
                OptionSetValue o = new OptionSetValue();
                o.Value = 5;
                req.Status = o;
                CloseQuoteResponse resp = (CloseQuoteResponse)_organizationService.Execute(req);
            }

            _tracingService.Trace("Ended CloseQuoteAsLost Method...");
        }

        //Created By : Jessica Casupanan, Created On : 12/06/2016
        //Purpose : Validate Unit Price On Create
        //TO BE DELETED
        public bool CheckIfHasUnitPrice(Entity quoteEntity)
        {
            _tracingService.Trace("Started CheckIfHasUnitPrice Method...");
            Guid productId = quoteEntity.GetAttributeValue<EntityReference>("gsc_productid") != null ? quoteEntity.GetAttributeValue<EntityReference>("gsc_productid").Id
                    : Guid.Empty;

            EntityCollection productCollection = CommonHandler.RetrieveRecordsByOneValue("product", "productid", productId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_sellprice", "pricelevelid" });

            if (productCollection != null && productCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieved {" + productCollection.Entities.Count + "}: " + "Retrieving Product...");

                Entity product = productCollection.Entities[0];
                Guid priceLevelId = product.Contains("pricelevelid") ? product.GetAttributeValue<EntityReference>("pricelevelid").Id
               :Guid.Empty;

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

        public void ConvertPotentialtoCustomer(Entity quoteEntity)
        {
            var customer = quoteEntity.Contains("customerid")
                    ? quoteEntity.GetAttributeValue<EntityReference>("customerid") != null
                    ? quoteEntity.GetAttributeValue<EntityReference>("customerid")
                    : null
                    : null;

            if (customer != null)
            {
                //Retrieve Opportunity record from Opportunity ID field value
                EntityCollection customerRecords = CommonHandler.RetrieveRecordsByOneValue(customer.LogicalName, customer.LogicalName + "id", customer.Id, _organizationService, null, OrderType.Ascending,
                    new[] { "gsc_ispotential" });

                if (customerRecords != null && customerRecords.Entities.Count > 0)
                {
                    Entity customerEntity = customerRecords.Entities[0];

                    if (customerEntity.GetAttributeValue<Boolean>("gsc_ispotential"))
                    {
                        customerEntity["gsc_ispotential"] = false;
                        _organizationService.Update(customerEntity);
                    }
                }
            }
        }

        public void ValidateDelete(Entity quoteEntity)
        {
            var stateCode = quoteEntity.GetAttributeValue<OptionSetValue>("statecode").Value;

            if (stateCode != 0)
            {
                throw new InvalidPluginExecutionException("These records cannot be deleted.");
            }

        }
    }
}

