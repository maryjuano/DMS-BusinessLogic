using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.Product
{
    public class ProductHandler
    {
        private readonly IOrganizationService _organizationService;
        private readonly ITracingService _tracingService;



        public ProductHandler(IOrganizationService service, ITracingService trace)
        {
            _organizationService = service;
            _tracingService = trace;
        }

        //Create By: Raphael Herrera, Created On: 6/28/2016 /*
        /* Purpose:  Set values for Vehicle Name and Vehicle Id
         * Registration Details:
         * Event/Message: 
         *      Post/Update: 
         * Primary Entity: Product
         */
        // 
        public void GenerateProductName(Entity productEntity)
        {
            _tracingService.Trace("Started GenerateProductName Method...");

            var producttype = productEntity.Contains("gsc_producttype")
                ? productEntity.GetAttributeValue<OptionSetValue>("gsc_producttype").Value
                : 0;

            if (producttype == 100000000) //If and only if product type is 'Vehicle'
            {
                var modelCode = productEntity.Contains("gsc_modelcode") ? productEntity.GetAttributeValue<string>("gsc_modelcode")
                    : String.Empty;
                var optionCode = productEntity.Contains("gsc_optioncode") ? productEntity.GetAttributeValue<string>("gsc_optioncode")
                    : String.Empty;

                //productEntity["name"] = modelYear + " " + modelDescription;
                productEntity["productnumber"] = modelCode + "-" + optionCode;
            }

            _tracingService.Trace("Ending GenerateProductName Method...");

        }

        //Created By: Leslie Baliguat, Created on: 10/04/2016
        public void PopulateTaxRate(Entity productEntity)
        {
            _tracingService.Trace("Started PopulateTaxRate Method...");

            Entity productToUpdate = _organizationService.Retrieve(productEntity.LogicalName, productEntity.Id, new ColumnSet("gsc_taxrate"));

            var taxId = productEntity.GetAttributeValue<EntityReference>("gsc_taxid") != null
                ? productEntity.GetAttributeValue<EntityReference>("gsc_taxid").Id
                : Guid.Empty;

            EntityCollection taxCollection = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_taxmaintenance", "gsc_cmn_taxmaintenanceid", taxId, _organizationService, null, OrderType.Ascending,
                new[] { "gsc_rate" });

            if (taxCollection != null && taxCollection.Entities.Count > 0)
            {
                _tracingService.Trace("Retrieve Tax Rate");

                var taxEntity = taxCollection.Entities[0];

                var rate = taxEntity.Contains("gsc_rate")
                    ? taxEntity.GetAttributeValue<Double>("gsc_rate")
                    : 0;

                productToUpdate["gsc_taxrate"] = rate;

                _organizationService.Update(productToUpdate);

                _tracingService.Trace("Rate Updated.");
            }

            _tracingService.Trace("Ended PopulateTaxRate Method...");
        }

        //Create By: Leslie G. Baliguat, Created On: 02/02/2017
        //Populate  Model Description Name
        public void ReplicateModelDescriptionName(Entity productEntity)
        {
            _tracingService.Trace("Started ReplicateModelDescriptionName method..");
            var structure = productEntity.Contains("productstructure")
                ? productEntity.GetAttributeValue<OptionSetValue>("productstructure").Value
                : 0;

            var productType = productEntity.Contains("gsc_producttype")
                ? productEntity.GetAttributeValue<OptionSetValue>("gsc_producttype").Value
                : 0;

            if (structure == 1 && productType == 100000000)
            {
                Entity nameToUpdate = _organizationService.Retrieve(productEntity.LogicalName, productEntity.Id,
                    new ColumnSet("parentproductid", "name"));

                var parentProduct = nameToUpdate.GetAttributeValue<EntityReference>("parentproductid") != null
                    ? nameToUpdate.GetAttributeValue<EntityReference>("parentproductid").Name
                    : String.Empty;

                nameToUpdate["name"] = parentProduct;

                _organizationService.Update(nameToUpdate);
                _tracingService.Trace("Done ReplicateModelDescriptionName method..");
            }

        }

        //Create By: Artum M. Ramos, Created On: 3/2/2017 /*
        /* Purpose:  On Import Product
         * Registration Details:
         * Event/Message: 
         *      Pre-Create: Model Description
         * Primary Entity: Product
         */
        public Entity OnImportProduct(Entity productEntity)
        {
            var structure = productEntity.Contains("productstructure")
                ? productEntity.GetAttributeValue<OptionSetValue>("productstructure").Value
                : 0;

            var productType = productEntity.Contains("gsc_producttype")
                ? productEntity.GetAttributeValue<OptionSetValue>("gsc_producttype").Value
                : 0;

            if (structure == 1 && productType == 100000000)
            {
                if (productEntity.Contains("gsc_modeldescription"))
                {
                    if (productEntity.GetAttributeValue<String>("gsc_modeldescription") == null)
                        return null;
                }
                else
                {
                    return null;
                }
                _tracingService.Trace("Started OnImportProduct method..");
                var modelDescription = productEntity.Contains("gsc_modeldescription")
                    ? productEntity.GetAttributeValue<String>("gsc_modeldescription")
                    : String.Empty;

                _tracingService.Trace("Create Condition List..");
                //check for contact with similar record as prospect inquiry
                var productConditionList = new List<ConditionExpression>
                            {
                                new ConditionExpression("name", ConditionOperator.Equal, modelDescription),
                                new ConditionExpression("productstructure", ConditionOperator.Equal, 2)
                            };
                _tracingService.Trace("Retrieve by Condition..");
                EntityCollection parentProductEC = CommonHandler.RetrieveRecordsByConditions("product", productConditionList, _organizationService, null, OrderType.Ascending,
                         new[] { "productid" });

                _tracingService.Trace("Check if not null..");
                if (parentProductEC != null && parentProductEC.Entities.Count > 0)
                {
                    Entity product = parentProductEC.Entities[0];
                    _tracingService.Trace("product retrieved..");
                    productEntity["parentproductid"] = new EntityReference("product", product.Id);
                    _tracingService.Trace("Product id" + product.Id.ToString());

                    return parentProductEC.Entities[0];
                }
                else
                {
                    throw new InvalidPluginExecutionException("No Parent Product " + modelDescription.ToString());
                }
            }
            return null;
        }

        public void ClearSellingPrice(Entity product)
        {
            var priceListId = product.GetAttributeValue<EntityReference>("pricelevelid") != null
                ? product.GetAttributeValue<EntityReference>("pricelevelid").Id
                : Guid.Empty;

            if(priceListId == Guid.Empty)
            {
                Entity productToUpdate = _organizationService.Retrieve(product.LogicalName, product.Id,
                    new ColumnSet("gsc_sellprice"));
                productToUpdate["gsc_sellprice"] = null;
                _organizationService.Update(productToUpdate);
            }
        }

    }
}
