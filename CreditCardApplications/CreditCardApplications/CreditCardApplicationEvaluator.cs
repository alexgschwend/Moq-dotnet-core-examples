﻿using System;

namespace CreditCardApplications
{
    public class CreditCardApplicationEvaluator
    {
        private const int AutoReferralMaxAge = 20;
        private const int HighIncomeThreshhold = 100_000;
        private const int LowIncomeThreshhold = 20_000;

        private readonly IFrequentFlyerNumberValidator _validator;
        private readonly FraudLookup _fraudLookup;

        public int ValidatorLookupCount { get; private set; }

        public CreditCardApplicationEvaluator(IFrequentFlyerNumberValidator validator, 
                                              FraudLookup fraudLookup = null )
        {
            _validator = validator ??
                throw new ArgumentNullException(nameof(validator));

            _validator.ValidatorLookupPerformed += ValidatorLookupPerformed;

            _fraudLookup = fraudLookup;
        }

        private void ValidatorLookupPerformed( object sender, EventArgs e )
        {
            ValidatorLookupCount++;
        }

        public CreditCardApplicationDecision Evaluate(CreditCardApplication application)
        {
            if ( _fraudLookup != null && _fraudLookup.IsFraudRisk( application ) )
            {
                return CreditCardApplicationDecision.ReferredToHumanFraudRisk;
            }

            if (application.GrossAnnualIncome >= HighIncomeThreshhold)
            {
                return CreditCardApplicationDecision.AutoAccepted;
            }

            if ( _validator.ServiceInformation.License.LicenseKey == "EXPIRED" )
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            _validator.ValidationMode = application.Age >= 30 ? ValidationMode.Detailed : ValidationMode.Quick;

            bool isValidFrequentNumber = false;

            try
            {
                isValidFrequentNumber = _validator.IsValid( application.FrequentFlyerNumber );
            }
            catch ( Exception  )
            {
                // log
                return CreditCardApplicationDecision.ReferredToHuman;
            }
            
            if ( !isValidFrequentNumber )
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (application.Age <= AutoReferralMaxAge)
            {
                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (application.GrossAnnualIncome < LowIncomeThreshhold)
            {
                return CreditCardApplicationDecision.AutoDeclined;
            }

            return CreditCardApplicationDecision.ReferredToHuman;
        }       

        //public CreditCardApplicationDecision EvaluateUsingOut(CreditCardApplication application)
        //{
        //    if (application.GrossAnnualIncome >= HighIncomeThreshhold)
        //    {
        //        return CreditCardApplicationDecision.AutoAccepted;
        //    }

        //    _validator.IsValid( application.FrequentFlyerNumber, out var isValidFrequentNumber );

        //    if ( !isValidFrequentNumber )
        //    {
        //        return CreditCardApplicationDecision.ReferredToHuman;
        //    }

        //    if (application.Age <= AutoReferralMaxAge)
        //    {
        //        return CreditCardApplicationDecision.ReferredToHuman;
        //    }

        //    if (application.GrossAnnualIncome < LowIncomeThreshhold)
        //    {
        //        return CreditCardApplicationDecision.AutoDeclined;
        //    }

        //    return CreditCardApplicationDecision.ReferredToHuman;
        //} 
    }
}
