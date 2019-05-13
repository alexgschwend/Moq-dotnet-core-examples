using System;
using System.Text.RegularExpressions;
using Xunit;
using Moq;
using Moq.Protected;

namespace CreditCardApplications.Tests
{
    public class CreditCardApplicationEvaluatorTest
    {
        private Mock<IFrequentFlyerNumberValidator> mockValidator;
        private CreditCardApplicationEvaluator sut;

        public CreditCardApplicationEvaluatorTest()
        {
            mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.SetupAllProperties();
            mockValidator.Setup( x => x.ServiceInformation.License.LicenseKey )
                         .Returns( "OK" );
            mockValidator.Setup( x => x.IsValid( It.IsAny<string>() ) )
                         .Returns( true );

            sut = new CreditCardApplicationEvaluator( mockValidator.Object );
        }

        [Fact]
        public void AcceptHighIncomeApplications()
        {
            var application = new CreditCardApplication { GrossAnnualIncome = 100_000 };

            CreditCardApplicationDecision decision = sut.Evaluate( application );

            Assert.Equal( CreditCardApplicationDecision.AutoAccepted, decision );

        }

        [Fact]
        public void ReferYoungApplications()
        {
            var application = new CreditCardApplication { Age = 19 };
            CreditCardApplicationDecision decision = sut.Evaluate( application );

            Assert.Equal( CreditCardApplicationDecision.ReferredToHuman, decision );
        }

        [Fact]
        public void DeclineLowIncomeApplications()
        {
            mockValidator.Setup( x => x.IsValid( It.IsRegex("[a-z]", RegexOptions.None)) )
                         .Returns( true );

            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 19_999,
                Age = 42,
                FrequentFlyerNumber = "a"
            };

            CreditCardApplicationDecision decision = sut.Evaluate( application );

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision  );
        }

        //[Fact]
        //public void DeclineLowIncomeApplicationsOutDemo()
        //{
        //    Mock<IFrequentFlyerNumberValidator> mockValidator =
        //        new Mock<IFrequentFlyerNumberValidator>();

        //    bool isValid = true;
        //    mockValidator.Setup( x => x.IsValid( It.IsAny<string>(), out isValid ) ); // 'out' instead of Return(true)

        //    var sut = new CreditCardApplicationEvaluator( mockValidator.Object );

        //    var application = new CreditCardApplication
        //    {
        //        GrossAnnualIncome = 19_999,
        //        Age = 42,
        //        FrequentFlyerNumber = "a"
        //    };

        //    CreditCardApplicationDecision decision = sut.EvaluateUsingOut( application );

        //    Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision  );
        //}


        [Fact]
        public void ReferInvalidFrequentFlyerApplications()
        {
            var application = new CreditCardApplication();

            CreditCardApplicationDecision decision = sut.Evaluate( application );

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision  );
        }

        [Fact]
        public void ReferInvalidFrequentFlyerApplications_Sequence()
        {
            mockValidator.SetupSequence( x => x.IsValid( It.IsAny<string>() ) )
                         .Returns( false )
                         .Returns(true  );
            
            var sut = new CreditCardApplicationEvaluator( mockValidator.Object );

            var application = new CreditCardApplication { Age = 25 };

            CreditCardApplicationDecision firstDecision = sut.Evaluate( application );
            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, firstDecision  );

            CreditCardApplicationDecision secondDecision = sut.Evaluate( application );
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, secondDecision);
        }

        [Fact]
        public void ReferWhenLicenseKeyExpired()
        {
            Mock<IFrequentFlyerNumberValidator> mockValidator =
                new Mock<IFrequentFlyerNumberValidator>();

            mockValidator.Setup( x => x.IsValid( It.IsAny<string>() ) )
                         .Returns( true );

            //var mockLicenseData = new Mock<ILicenseData>();
            //mockLicenseData.Setup( x => x.LicenseKey).Returns( GetLicenseKeyExpiryString);

            //var mockServiceInfo = new Mock<IServiceInformation>();
            //mockServiceInfo.Setup( x => x.License )
            //             .Returns( mockLicenseData.Object );

            //mockValidator.Setup( x => x.ServiceInformation )
            //             .Returns( mockServiceInfo.Object );

            mockValidator.Setup( x => x.ServiceInformation.License.LicenseKey )
                         .Returns( GetLicenseKeyExpiryString );

            var sut = new CreditCardApplicationEvaluator( mockValidator.Object );

            var application = new CreditCardApplication { Age = 42 };

            CreditCardApplicationDecision decision = sut.Evaluate( application );

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision  );
        }

        [Fact]
        public void UseDetailedLookupForOlderApplications()
        {
            Mock<IFrequentFlyerNumberValidator> mockValidator =
                new Mock<IFrequentFlyerNumberValidator>();

            mockValidator.SetupAllProperties();
            mockValidator.Setup( x => x.ServiceInformation.License.LicenseKey )
                         .Returns( "OK");
            //mockValidator.SetupProperty( x => x.ValidationMode ); -> SetupAllProperties()

            var sut = new CreditCardApplicationEvaluator( mockValidator.Object );

            var application = new CreditCardApplication { Age = 30 };

            CreditCardApplicationDecision decision = sut.Evaluate( application );

            Assert.Equal(ValidationMode.Detailed, mockValidator.Object.ValidationMode);
        }

        [Fact]
        public void ValidateFrequentFlyerNumberForLowIncomeApplications()
        {
            var application = new CreditCardApplication { FrequentFlyerNumber = "q" };

            sut.Evaluate( application );
            mockValidator.Verify(x => x.IsValid( It.IsAny<string>()),Times.Once);
        }

        [Fact]
        public void NotValidateFrequentFlyerNumberForHighIncomeApplications()
        {
            var application = new CreditCardApplication { GrossAnnualIncome = 100_000};

            sut.Evaluate( application );
            mockValidator.Verify(x => x.IsValid( It.IsAny<string>()), Times.Never);
        }

        
        [Fact]
        public void CheckLicenseKeyForLowIncomeApplications()
        {
            var application = new CreditCardApplication { GrossAnnualIncome = 99_000};

            sut.Evaluate( application );

            mockValidator.VerifyGet(x => x.ServiceInformation.License.LicenseKey, Times.Once);
        }

        
        [Fact]
        public void SetDetailedLookupForOlderApplications()
        {

            var application = new CreditCardApplication { Age = 30 };

            sut.Evaluate( application );

            mockValidator.VerifySet(x => x.ValidationMode = It.IsAny<ValidationMode>(), Times.Once);
        }

        //[Fact]
        //public void ShouldValidateFrequentFlyerNumberForLowIncomeApplications_CustomMessage()
        //{
        //    var mockValidator = new Mock<IFrequentFlyerNumberValidator>();

        //    mockValidator.Setup( x => x.ServiceInformation.License.LicenseKey )
        //                 .Returns( "OK" );

        //    var sut = new CreditCardApplicationEvaluator( mockValidator.Object );

        //    var application = new CreditCardApplication();

        //    sut.Evaluate( application );
        //    mockValidator.Verify(x => x.IsValid( It.IsNotNull<string>()), "Frequent flyer number passed should not be null");
        //}

        [Fact]
        public void ReferWhenFrequentFlyerValidationError()
        {
            mockValidator.Setup( x => x.IsValid( It.IsAny<string>() ) )
                         .Throws(new Exception("Custom Message"));

            var application = new CreditCardApplication { Age = 42 };

            CreditCardApplicationDecision decision = sut.Evaluate( application );

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision  );
        }
        
        [Fact]
        public void IncrementLookupCount()
        {
            mockValidator.Setup( x => x.IsValid( It.IsAny<string>() ) )
                         .Returns(true)
                         .Raises(x => x.ValidatorLookupPerformed += null, EventArgs.Empty  );

            var application = new CreditCardApplication { FrequentFlyerNumber = "x", Age = 25 };

            sut.Evaluate(application);

            //mockValidator.Raise( x => x.ValidatorLookupPerformed += null, EventArgs.Empty );

            Assert.Equal(1, sut.ValidatorLookupCount);
        }

        
        [Fact]
        public void ReferFraudRisk()
        {
            var mockFraudLookup = new Mock<FraudLookup>();
            //mockFraudLookup.Setup( x => x.IsFraudRisk( It.IsAny<CreditCardApplication>() ))
            //             .Returns( true );

            mockFraudLookup.Protected()
                           .Setup<bool>( "CheckApplication", ItExpr.IsAny<CreditCardApplication>() )
                           .Returns( true );

            var sut = new CreditCardApplicationEvaluator( mockValidator.Object, mockFraudLookup.Object);

            var application = new CreditCardApplication();

            var decision = sut.Evaluate( application );

            Assert.Equal(CreditCardApplicationDecision.ReferredToHumanFraudRisk, decision  );
        }

        
        [Fact]
        public void LinqToMocks()
        {
            //var mockValidator = new Mock<IFrequentFlyerNumberValidator>();

            //mockValidator.Setup( x => x.ServiceInformation.License.LicenseKey )
            //             .Returns( "OK" );
            //mockValidator.Setup( x => x.IsValid( It.IsAny<string>() ) )
            //             .Returns( true );

            IFrequentFlyerNumberValidator mockValidator = Mock.Of<IFrequentFlyerNumberValidator>
            (
                validator =>
                    validator.ServiceInformation.License.LicenseKey == "OK" &&
                    validator.IsValid(It.IsAny<string>()  ) == true
            );

            var sut = new CreditCardApplicationEvaluator( mockValidator);

            var application = new CreditCardApplication { Age = 25 };

            var decision = sut.Evaluate( application );

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision  );
        }

        string GetLicenseKeyExpiryString()
        {
            // E. g. read from vendor-supplied constants file
            return "EXPIRED";
        }

        // other evaluator test conditions
    }
}
