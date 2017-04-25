using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSC.Rover.DMS.BusinessLogic.Common;

namespace GSC.Rover.DMS.BusinessLogic.AppliedCharges
{
    public class AppliedChargesHandler
    {
        //Created By : Jerome Anthony Gerero, Created On: 2/22/2016
        public Entity SetTotalChargesAmount(Entity appliedChargesEntity, IOrganizationService service, ITracingService trace, String message)
        {
            trace.Trace("Started SetTotalChargesAmount method..");
            var quoteId = appliedChargesEntity.GetAttributeValue<EntityReference>("gsc_quoteid") != null
                ? appliedChargesEntity.GetAttributeValue<EntityReference>("gsc_quoteid").Id
                : Guid.Empty;

            Decimal totalChargesAmount = 0;

            //Retrieve Applied Charges records with the same Quote
            EntityCollection appliedChargesRecords = CommonHandler.RetrieveRecordsByOneValue("gsc_cmn_appliedcharges", "gsc_quoteid", quoteId, service, null, OrderType.Ascending,
                new[] { "gsc_chargeamount", "gsc_free" });

            //Retrieve Quote record from Quote field value
            EntityCollection quoteRecords = CommonHandler.RetrieveRecordsByOneValue("quote", "quoteid", quoteId, service, null, OrderType.Ascending,
                new[] { "gsc_totalchargesamount", "statecode" });

            if (appliedChargesRecords != null && appliedChargesRecords.Entities.Count > 0)
            {
                foreach (var appliedCharges in appliedChargesRecords.Entities)
                {                    
                    if (appliedCharges.Contains("gsc_chargeamount"))
                    {
                        totalChargesAmount += appliedCharges.GetAttributeValue<Boolean>("gsc_free")
                            ? Decimal.Zero
                            : appliedCharges.GetAttributeValue<Money>("gsc_chargeamount").Value;                  
                    }                    
                }

                if (appliedChargesEntity.Contains("gsc_chargeamount") && message.Equals("Delete"))
                {
                    totalChargesAmount = totalChargesAmount - (Decimal)appliedChargesEntity.GetAttributeValue<Money>("gsc_chargeamount").Value;
                }
            }

            if (quoteRecords != null && quoteRecords.Entities.Count > 0 && quoteRecords.Entities[0].GetAttributeValue<OptionSetValue>("statecode").Value == 0)
            {
                Entity quote = quoteRecords.Entities[0];
                quote["gsc_totalchargesamount"] = new Money(totalChargesAmount);
                service.Update(quote);

                return quote;
            }
            trace.Trace("Ended SetTotalChargesAmount method..");
            return appliedChargesEntity;
        }
    }
}
