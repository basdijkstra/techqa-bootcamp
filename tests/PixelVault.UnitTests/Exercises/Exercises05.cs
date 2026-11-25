using Moq;
using NUnit.Framework;
using PixelVault.Payment.Domain;
using PixelVault.Payment.Services;
using PixelVault.UnitTests.Answers.TestObjects;

namespace PixelVault.UnitTests.Exercises;

/*
 * These exercises use the SimplePaymentService class in the TestObjects folder as you refactored it
 * in Exercises04: it receives its fraud scorer and payment processor through its constructor.
 * Instead of passing in the real FraudScorer and SimulatedPaymentProcessor, you will now pass in mocks.
 */
[TestFixture]
public class Exercises05
{
    [Test]
    public async Task ChargeAsync_PaymentBlockedByFraudCheck_IsDeclinedAndCardIsNotCharged()
    {
        /*
         * TODO: create a mock IFraudScorer that returns a FraudAssessment with a score of 85
         * and IsBlocked set to true, whatever input it is given. The list of reasons should contain
         * a single reason for the fraud suspicion, for example, "High-risk transaction".
         * Note that the amount and the email address used below would give a score of 0
         * with the real FraudScorer: the mock decides the outcome of the fraud check.
         */


        /*
         * TODO: create a mock IPaymentProcessor. It does not need a Setup(): the card should never be charged.
         */


        /*
         * TODO: create a SimplePaymentService, passing in the mocked objects, and call ChargeAsync
         * with an amount of 100.00m, email address "player@example.com", card number "4242424242424242"
         * and an empty list of previous attempts.
         */


        /*
         * TODO: verify that the payment was declined with decline code FRAUD_SUSPECTED,
         * and that the ChargeAsync method of the payment processor was never called.
         */

    }

    [Test]
    public async Task ChargeAsync_PaymentProcessorReturnsError_PaymentFailsWithProcessorError()
    {
        /*
         * TODO: create a mock IFraudScorer that returns a FraudAssessment with a score of 0
         * and IsBlocked set to false, whatever input it is given. The list of reasons should be empty.
         */


        /*
         * TODO: create a mock IPaymentProcessor whose ChargeAsync method returns a ProcessorResult
         * with outcome ProcessorOutcome.Error. ChargeAsync is asynchronous: use ReturnsAsync() instead of Returns().
         */


        /*
         * TODO: create a SimplePaymentService, passing in the mocked objects, and call ChargeAsync
         * with an amount of 100.00m, email address "player@example.com", card number "4242424242424242"
         * and an empty list of previous attempts.
         */


        /*
         * TODO: verify that the payment failed with decline code PROCESSOR_ERROR,
         * and that the ChargeAsync method of the payment processor was called exactly once,
         * with the card number and the amount passed to the SimplePaymentService.
         */

    }
}
