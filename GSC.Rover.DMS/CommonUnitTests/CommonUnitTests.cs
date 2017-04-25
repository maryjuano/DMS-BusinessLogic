using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using GSC.Rover.DMS.BusinessLogic.Common;
namespace CommonUnitTests
{
    [TestClass]
    public class CommonUnitTests
    {
        [TestMethod]
        public void GetEntityAttributeReturnsValue()
        {
            //arrange
            Entity contact = new Entity("contact");
            contact.Attributes.Add("fullname", "my fullname");

            //act
            string result = CommonHandler.GetEntityAttributeSafe<string>(contact, "fullname");

            //assert
            Assert.AreEqual("my fullname", result);
        }
        [TestMethod]
        public void GetGuidReturnsDefaultValue()
        {
            //arrange
            Entity contact = new Entity("contact");
            contact.Attributes.Add("gsc_samplereferenceid", null);

            //act
            Guid result = CommonHandler.GetEntityAttributeSafe<Guid>(contact, "gsc_samplereferenceid");

            //assert
            Assert.AreEqual(default(Guid), result);
        }
        [TestMethod]
        public void GetDoubleReturnsDefaultValue()
        {
            //arrange
            Entity contact = new Entity("contact");
            contact.Attributes.Add("gsc_doubledatatype", null);

            //act
            double result = CommonHandler.GetEntityAttributeSafe<double>(contact, "gsc_doubledatatype");

            //assert
            Assert.AreEqual(default(double), result);
        }


        [TestMethod]
        public void GetEntityReferenceReturnsEmptyGuid()
        {
            //arrange
            Entity contact = new Entity("contact");
            contact.Attributes.Add("gsc_samplereferenceid", null);

            //act
            Guid result = CommonHandler.GetEntityReferenceIdSafe(contact, "gsc_samplereferenceid");

            //assert
            Assert.AreEqual(default(Guid), result);
        }

        [TestMethod]
        public void GetEntityReferenceReturnsValue()
        {
            //arrange
            Guid sampleId = Guid.NewGuid();
            EntityReference sampleReference = new EntityReference();
            sampleReference.Id = sampleId;

            Entity contact = new Entity("contact");
            contact.Attributes.Add("gsc_samplereferenceid", sampleReference);

            //act
            Guid result = CommonHandler.GetEntityReferenceIdSafe(contact, "gsc_samplereferenceid");

            //assert
            Assert.AreEqual(sampleId, result);
        }
    }
}
