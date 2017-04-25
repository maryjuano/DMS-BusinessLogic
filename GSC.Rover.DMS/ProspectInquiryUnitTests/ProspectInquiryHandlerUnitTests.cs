using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSC.Rover.DMS.BusinessLogic.ProspectInquiry;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace ProspectInquiryUnitTests
{
    [TestClass]
    public class ProspectInquiryHandlerUnitTests
    {
        // Created By: Leslie Baliguat, Created On: 2/1/2016
        #region ReplicateProspectInfo
        [TestMethod]

        #region Test Scenario:  Prospect Inquiry - Replicate Prospect to Prospect Inquiry

        public void ReplicateProspectInfoUnitTest()
        {
            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Prospect EntityCollection
            var ProspectCollection = new EntityCollection
            {
                EntityName = "gsc_sls_prospect",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_prospect",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_firstname", "Mark"},
                            {"gsc_middlename", ""},
                            {"gsc_lastname", "Opaco"},
                            {"gsc_mobileno", "09123456789"},
                            {"gsc_emailaddress", "mopaco@gurango.net"},
                            {"gsc_street", "Antipolo St."},
                            {"gsc_cityid",new EntityReference("gsc_syscity", Guid.NewGuid())
                            { Name = "Manila"}},
                            {"gsc_provinceid",new EntityReference("gsc_sysregion", Guid.NewGuid())
                            { Name = "Manila"}},
                            {"gsc_countryid", new EntityReference("gsc_syscountry", Guid.NewGuid()) {Name = "Philippines"}},
                        }
                    }
                }
            };
            #endregion

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "Prospect Inquiry",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Created,
                        Attributes = new AttributeCollection
                        {
                            {"firstname", ""},
                            {"middlename", ""},
                            {"lastname", ""},
                            {"fullname", ""},
                            {"mobilephone", ""},
                            {"emailaddress1", ""},
                            {"address1_line1", ""},
                            {"address1_city", ""},
                            {"address1_stateorprovince",""},
                            {"address1_country", ""},
                            {"gsc_prospectid", new EntityReference("gsc_sls_prospect", ProspectCollection.Entities[0].Id)},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                It.Is<QueryExpression>(expression => expression.EntityName == ProspectCollection.EntityName)
                ))).Returns(ProspectCollection);

            #endregion

            #region 2. Call/Action

            var ProspectInquiryHandler = new ProspectInquiryHandler(orgService, orgTracing);
            Entity prospectInquiry = ProspectInquiryHandler.ReplicateProspectInfo(ProspectInquiryCollection.Entities[0]);
            #endregion

            #region 3. Verify
            var fullname = ProspectCollection.Entities[0]["gsc_firstname"] + " " + ProspectCollection.Entities[0]["gsc_lastname"];
            Assert.AreEqual(ProspectCollection.Entities[0]["gsc_firstname"], prospectInquiry["firstname"]);
            Assert.AreEqual(ProspectCollection.Entities[0]["gsc_middlename"], prospectInquiry["middlename"]);
            Assert.AreEqual(ProspectCollection.Entities[0]["gsc_lastname"], prospectInquiry["lastname"]);
            Assert.AreEqual(fullname, prospectInquiry["fullname"]);
            Assert.AreEqual(ProspectCollection.Entities[0]["gsc_mobileno"], prospectInquiry["mobilephone"]);
            Assert.AreEqual(ProspectCollection.Entities[0]["gsc_emailaddress"], prospectInquiry["emailaddress1"]);
            Assert.AreEqual(ProspectCollection.Entities[0]["gsc_street"], prospectInquiry["address1_line1"]);
            Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cityid").Name, prospectInquiry["address1_city"]);
            Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_provinceid").Name, prospectInquiry["address1_stateorprovince"]);
            Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_countryid").Name, prospectInquiry["address1_country"]);
            #endregion
        }

        #endregion


        #endregion


        // Created By: Leslie Baliguat, Created On: 2/1/2016
        #region ConcatenateVehicleInfo

        #region Test Scenario: Concatenate Vehicle Information as Topic

        [TestMethod]
        public void ConcatenateVehicleInfo()
        {

            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Vehicle Base Model Entity
            var VehicleBaseModelCollection = new EntityCollection()
            {
                EntityName = "gsc_iv_vehiclebasemodel",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclebasemodel",
                        Attributes =
                        {
                            {"gsc_basemodelpn", "Strada"}
                        }
                    }
                }
            };
            #endregion

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclebasemodelid", new EntityReference("gsc_iv_vehiclebasemodel", VehicleBaseModelCollection.Entities[0].Id)},
                            {"fullname", "Leslie Baliguat"},
                            {"topic", ""},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == VehicleBaseModelCollection.EntityName)
                  ))).Returns(VehicleBaseModelCollection);

            #endregion

            #region 2. Call/Action
            var ProspectInquiryHandler = new ProspectInquiryHandler(orgService, orgTracing);
            String Subject = ProspectInquiryHandler.ConcatenateVehicleInfo(ProspectInquiryCollection.Entities[0], "Create");
            #endregion

            #region 3. Verify
            var vehiclemodel = VehicleBaseModelCollection.Entities[0]["gsc_basemodelpn"];
            var fullname = ProspectInquiryCollection.Entities[0]["fullname"];
            var topic = fullname + " - " + vehiclemodel;
            Assert.AreEqual(topic, Subject);
            #endregion

        }

        #endregion

        #region Test Scenaio: Concatenate Vehicle Information as Topic with Null Values

        [TestMethod]
        public void ConcatenateVehicleInfoWithNullValues()
        {

            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region Vehicle Base Model Entity
            var VehicleBaseModelCollection = new EntityCollection()
            {
                EntityName = "gsc_iv_vehiclebasemodel",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_iv_vehiclebasemodel",
                        Attributes =
                        {
                            {"gsc_basemodelpn", "Strada"}
                        }
                    }
                }
            };
            #endregion

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_vehiclebasemodelid", new EntityReference()},
                            {"fullname", ""},
                            {"topic", ""},
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                 It.Is<QueryExpression>(expression => expression.EntityName == VehicleBaseModelCollection.EntityName)
                 ))).Returns(VehicleBaseModelCollection);

            #endregion

            #region 2. Call/Action

            var ProspectInquiryHandler = new ProspectInquiryHandler(orgService, orgTracing);
            String Subject = ProspectInquiryHandler.ConcatenateVehicleInfo(ProspectInquiryCollection.Entities[0], "Create");

            #endregion

            #region 3. Verify
            var vehiclemodel = VehicleBaseModelCollection.Entities[0]["gsc_basemodelpn"];
            var fullname = ProspectInquiryCollection.Entities[0]["fullname"];
            var topic = fullname + " - " + vehiclemodel;
            Assert.AreEqual(topic, Subject);

            #endregion

        }

        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 4/19/16
        #region CreateCustomer

        #region Test Scenario: Customer Type is Individual then Create Contact

        [TestMethod]
        public void CreateCustomerIndividual()
        {

            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            var sampleGuid = new Guid("ccfa2910-fa16-4c77-9c73-a2e646a2fe44");

            #region Collection Entity
            var ContactCollection = new EntityCollection()
            {
                EntityName = "contact",
                Entities =
                {
                }
            };
            #endregion

            #region Prospect EntityCollection
            var ProspectCollection = new EntityCollection
            {
                EntityName = "gsc_sls_prospect",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_prospect",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_customertype", new OptionSetValue(100000000)},
                            {"gsc_firstname", "Leslie"},
                            {"gsc_middlename", "G"},
                            {"gsc_lastname", "Baliguat"},
                            {"gsc_emailaddress", "lbaliguat@gurango.net"},
                            {"gsc_mobileno", "1"},
                            {"gsc_alternatecontactno", "2"},
                            {"gsc_fax", "3"},
                            {"gsc_gender", new OptionSetValue(1)},
                            {"gsc_maritalstatus", new OptionSetValue(1)},
                            {"gsc_countryid", new EntityReference("country", sampleGuid)},
                            {"gsc_provinceid", new EntityReference("province", sampleGuid)},
                            {"gsc_cityid", new EntityReference("city", sampleGuid)},
                            {"gsc_street", "1"},
                            {"gsc_zipcode", "1"},
                            {"gsc_dealerid", new EntityReference("account", sampleGuid)},
                            {"gsc_branchid", new EntityReference("account", sampleGuid)},
                            {"gsc_recordownerid", new EntityReference("contact", sampleGuid)}
                        }
                    }
                }
            };
            #endregion

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_qualified", true},
                            {"gsc_prospectid", new EntityReference(ProspectCollection.EntityName, ProspectCollection.Entities[0].Id)},
                            {"parentaccountid", null},
                            {"parentcontactid", null}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ProspectCollection.EntityName)
                  ))).Returns(ProspectCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ContactCollection.EntityName)
                  ))).Returns(ContactCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(ProspectInquiryCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == ProspectInquiryCollection.Entities[0].LogicalName)))).Callback<Entity>(s => ProspectInquiryCollection.Entities[0] = s);

            #endregion

            #region 2. Call/Action
            var ProspectInquiryHandler = new ProspectInquiryHandler(orgService, orgTracing);
            Entity ContactEntity = ProspectInquiryHandler.CreateCustomer(ProspectInquiryCollection.Entities[0]);
            #endregion

            #region 3. Verify
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_firstname"), ContactEntity.GetAttributeValue<String>("firstname"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_middlename"), ContactEntity.GetAttributeValue<String>("middlename"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_lastname"), ContactEntity.GetAttributeValue<String>("lastname"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_emailaddress"), ContactEntity.GetAttributeValue<String>("emailaddress1"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_mobileno"), ContactEntity.GetAttributeValue<String>("mobilephone"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_alternatecontactno"), ContactEntity.GetAttributeValue<String>("telephone1"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_fax"), ContactEntity.GetAttributeValue<String>("fax"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_gender").Value, ContactEntity.GetAttributeValue<OptionSetValue>("gendercode").Value);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_maritalstatus").Value, ContactEntity.GetAttributeValue<OptionSetValue>("familystatuscode").Value);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_countryid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_countryid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_provinceid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_provinceid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cityid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_cityid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_street"), ContactEntity.GetAttributeValue<String>("address1_line1"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_zipcode"), ContactEntity.GetAttributeValue<String>("address1_postalcode1"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_dealerid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_dealerid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_branchid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_branchid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_recordownerid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_recordownerid").Id);
            //Assert.AreEqual(true, ContactEntity.GetAttributeValue<Boolean>("gsc_iscontact"));
            //Assert.AreEqual(true, ContactEntity.GetAttributeValue<Boolean>("gsc_ispotential"));
            //Assert.AreEqual(true, ContactEntity.GetAttributeValue<Boolean>("gsc_prospect"));
            Assert.AreEqual(false, ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid") == null);
            Assert.AreEqual(true, ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("parentaccountid") == null);
            #endregion
        }

        #endregion

        #region Test Scenario: Customer Type is Individual, Customer already a Contact

        [TestMethod]
        public void CustomerIndividualExist()
        {

            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            var sampleGuid = new Guid("ccfa2910-fa16-4c77-9c73-a2e646a2fe44");

            #region Prospect EntityCollection
            var ProspectCollection = new EntityCollection
            {
                EntityName = "gsc_sls_prospect",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_prospect",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_customertype", new OptionSetValue(100000000)},
                            {"gsc_firstname", "Leslie"},
                            {"gsc_middlename", "G"},
                            {"gsc_lastname", "Baliguat"},
                            {"gsc_emailaddress", "lbaliguat@gurango.net"},
                            {"gsc_mobileno", "1"},
                            {"gsc_alternatecontactno", "2"},
                            {"gsc_fax", "3"},
                            {"gsc_gender", new OptionSetValue(1)},
                            {"gsc_maritalstatus", new OptionSetValue(1)},
                            {"gsc_countryid", new EntityReference("country", sampleGuid)},
                            {"gsc_provinceid", new EntityReference("province", sampleGuid)},
                            {"gsc_cityid", new EntityReference("city", sampleGuid)},
                            {"gsc_street", "1"},
                            {"gsc_zipcode", "1"},
                            {"gsc_dealerid", new EntityReference("account", sampleGuid)},
                            {"gsc_branchid", new EntityReference("account", sampleGuid)},
                            {"gsc_recordownerid", new EntityReference("contact", sampleGuid)}
                        }
                    }
                }
            };
            #endregion

            #region Contact Collection Entity
            var ContactCollection = new EntityCollection()
            {
                EntityName = "contact",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "contact",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_prosectid", new EntityReference(ProspectCollection.EntityName, ProspectCollection.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_qualified", true},
                            {"gsc_prospectid", new EntityReference(ProspectCollection.EntityName, ProspectCollection.Entities[0].Id)},
                            {"parentaccountid", null},
                            {"parentcontactid", new EntityReference(ContactCollection.EntityName, ContactCollection.Entities[0].Id)}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ProspectCollection.EntityName)
                  ))).Returns(ProspectCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ContactCollection.EntityName)
                  ))).Returns(ContactCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(ProspectInquiryCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == ProspectInquiryCollection.Entities[0].LogicalName)))).Callback<Entity>(s => ProspectInquiryCollection.Entities[0] = s);

            #endregion

            #region 2. Call/Action
            var ProspectInquiryHandler = new ProspectInquiryHandler(orgService, orgTracing);
            Entity ContactEntity = ProspectInquiryHandler.CreateCustomer(ProspectInquiryCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(ContactCollection.Entities[0].Id, ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid").Id);
            Assert.AreEqual(null, ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("parentaccountid"));
            #endregion
        }

        #endregion

        #region Test Scenario: Customer Type is Corporate/Government then Create Account

        [TestMethod]
        public void CreateCorporateIndividual()
        {

            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            var sampleGuid = new Guid("ccfa2910-fa16-4c77-9c73-a2e646a2fe44");

            #region Collection Entity
            var ContactCollection = new EntityCollection()
            {
                EntityName = "account",
                Entities =
                {
                }
            };
            #endregion

            #region Prospect EntityCollection
            var ProspectCollection = new EntityCollection
            {
                EntityName = "gsc_sls_prospect",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "gsc_sls_prospect",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_customertype", new OptionSetValue(100000001)},
                            {"gsc_firstname", "Leslie"},
                            {"gsc_middlename", "G"},
                            {"gsc_lastname", "Baliguat"},
                            {"gsc_emailaddress", "lbaliguat@gurango.net"},
                            {"gsc_mobileno", "1"},
                            {"gsc_alternatecontactno", "2"},
                            {"gsc_fax", "3"},
                            {"gsc_gender", new OptionSetValue(1)},
                            {"gsc_maritalstatus", new OptionSetValue(1)},
                            {"gsc_countryid", new EntityReference("country", sampleGuid)},
                            {"gsc_provinceid", new EntityReference("province", sampleGuid)},
                            {"gsc_cityid", new EntityReference("city", sampleGuid)},
                            {"gsc_street", "1"},
                            {"gsc_zipcode", "1"},
                            {"gsc_dealerid", new EntityReference("account", sampleGuid)},
                            {"gsc_branchid", new EntityReference("account", sampleGuid)},
                            {"gsc_recordownerid", new EntityReference("contact", sampleGuid)},
                            {"gsc_companyname", "Star Magic"},
                            {"gsc_phone", "2"},
                            {"gsc_website", "www.starmagic.com"},
                        }
                    }
                }
            };
            #endregion

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_qualified", true},
                            {"gsc_prospectid", new EntityReference(ProspectCollection.EntityName, ProspectCollection.Entities[0].Id)},
                            {"parentaccountid", null},
                            {"parentcontactid", null}
                        }
                    }
                }
            };
            #endregion

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ProspectCollection.EntityName)
                  ))).Returns(ProspectCollection);

            orgServiceMock.Setup((service => service.RetrieveMultiple(
                  It.Is<QueryExpression>(expression => expression.EntityName == ContactCollection.EntityName)
                  ))).Returns(ContactCollection);

            orgServiceMock.Setup(service => service.Retrieve(
             It.IsAny<string>(),
             It.IsAny<Guid>(),
             It.IsAny<ColumnSet>())).Returns(ProspectInquiryCollection.Entities[0]);

            orgServiceMock.Setup((service => service.Update(It.Is<Entity>(entity => entity.LogicalName == ProspectInquiryCollection.Entities[0].LogicalName)))).Callback<Entity>(s => ProspectInquiryCollection.Entities[0] = s);


            #endregion

            #region 2. Call/Action
            var ProspectInquiryHandler = new ProspectInquiryHandler(orgService, orgTracing);
            Entity ContactEntity = ProspectInquiryHandler.CreateCustomer(ProspectInquiryCollection.Entities[0]);
            #endregion

            #region 3. Verify
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<OptionSetValue>("gsc_customertype").Value - 1, ContactEntity.GetAttributeValue<OptionSetValue>("gsc_customertype").Value);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_companyname"), ContactEntity.GetAttributeValue<String>("name"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_phone"), ContactEntity.GetAttributeValue<String>("telephone1"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_website"), ContactEntity.GetAttributeValue<String>("websiteurl"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_fax"), ContactEntity.GetAttributeValue<String>("fax"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_countryid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_countryid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_provinceid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_provinceid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_cityid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_cityid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_street"), ContactEntity.GetAttributeValue<String>("address1_line1"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<String>("gsc_zipcode"), ContactEntity.GetAttributeValue<String>("address1_postalcode"));
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_dealerid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_dealerid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_branchid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_branchid").Id);
            //Assert.AreEqual(ProspectCollection.Entities[0].GetAttributeValue<EntityReference>("gsc_recordownerid").Id, ContactEntity.GetAttributeValue<EntityReference>("gsc_recordownerid").Id);
            //Assert.AreEqual(true, ContactEntity.GetAttributeValue<Boolean>("gsc_isaccount"));
            //Assert.AreEqual(true, ContactEntity.GetAttributeValue<Boolean>("gsc_ispotential"));
            //Assert.AreEqual(true, ContactEntity.GetAttributeValue<Boolean>("gsc_prospect"));
            Assert.AreEqual(true, ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("parentcontactid") == null);
            Assert.AreEqual(false, ProspectInquiryCollection.Entities[0].GetAttributeValue<EntityReference>("parentaccountid") == null);
            #endregion
        }

        #endregion

        #endregion

        //Created By: Leslie Baliguat, Created On: 4/19/2016
        #region CreateOpportunity

        #region Test Scenario: Customer Type is Individual then Create Contact

        [TestMethod]
        public void CreateOpportunity()
        {

            #region 1. Setup / Arrange
            var orgServiceMock = new Mock<IOrganizationService>();
            var orgService = orgServiceMock.Object;
            var orgTracingMock = new Mock<ITracingService>();
            var orgTracing = orgTracingMock.Object;

            #region ProspectInquiry EntityCollection
            var ProspectInquiryCollection = new EntityCollection
            {
                EntityName = "lead",
                Entities =
                {
                    new Entity
                    {
                        Id = Guid.NewGuid(),
                        LogicalName = "lead",
                        EntityState = EntityState.Changed,
                        Attributes = new AttributeCollection
                        {
                            {"gsc_qualified", true}
                        }
                    }
                }
            };
            #endregion

            #endregion

            #region 2. Call/Action
            var ProspectInquiryHandler = new ProspectInquiryHandler(orgService, orgTracing);
            Entity OpportunityEntity = ProspectInquiryHandler.CreateOpportunity(ProspectInquiryCollection.Entities[0]);
            #endregion

            #region 3. Verify
            Assert.AreEqual(ProspectInquiryCollection.Entities[0].Id, OpportunityEntity.GetAttributeValue<EntityReference>("originatinglead").Id);
            #endregion
        }

        #endregion

        #endregion

    }
}
