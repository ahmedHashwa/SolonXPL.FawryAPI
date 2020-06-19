using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolonXpl.FawryAPI;

// ReSharper disable UnusedVariable
namespace FawryAPIUnitTest
{
    [TestClass]
    public class ApiUnitTest
    {
        private const string BaseApiUri = "https://atfawry.fawrystaging.com";
        private const string MerchantCode = "YOUR_TEST_MERCHAT_CODE";
        private const string SecurityKey = "YOUR_SECURITY_KEY";
        private const string PaymentDescription = "YOUR_PAYMENT_DESCRIPTION";
        private const int MerchantReferenceNumber = 551981;
        private const double PurchaseAmount = 200.0;

        private ChargeItem SampleItem = new ChargeItem
        {
            Description = "YOUR_ITEM_DESCRIPTION",
            Price = 250,
            ItemId = 23234387,
        };
        private readonly CustomerInfo _customer = new CustomerInfo
        (
            Guid.NewGuid().ToString(),
            "CUSTOMER_MOBILE",
            "CUSTOMER_EMAIL"
        );

        private readonly CustomerPaymentCard _customerPaymentCard = new CustomerPaymentCard
        (
            "CARD_NUMBER",
            "CVV",
            "MONTH",
            "YEAR"
        );

        [TestMethod]
        public async Task CreateCreditCardToken()
        {
            try
            {
                var client = new FawryApiClient(MerchantCode, SecurityKey, BaseApiUri);
                var cardTokenResponse = await client.CreateCreditCardTokenTask(_customerPaymentCard, _customer);
                Console.WriteLine($"Token is {cardTokenResponse.Serialize()}");
                Assert.IsTrue(cardTokenResponse != null);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public async Task ListCustomerCards()
        {
            try
            {
                var client = new FawryApiClient(MerchantCode, SecurityKey, BaseApiUri);

                var cardTokenResponse = await client.CreateCreditCardTokenTask(_customerPaymentCard, _customer);

                var clientCards = await client.ListCustomerCardTokensTask(_customer.ProfileId);
                Assert.IsTrue(clientCards != null);
                if (clientCards.Cards.Any())
                    foreach (var clientCardsCard in clientCards.Cards)
                    {
                        var deleteResult =
                            await client.DeleteCustomerCardTokensTask(_customer.ProfileId, clientCardsCard.Token);
                    }
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        [TestMethod]
        public async Task CreateChargeCard()
        {
            try
            {

                var client = new FawryApiClient(MerchantCode, SecurityKey, BaseApiUri);
                var cardTokenResponse = await client.CreateCreditCardTokenTask(_customerPaymentCard, _customer);
                var chargeResponse = await client.CreateChargeTask(MerchantReferenceNumber, _customer,
                    new PaymentInfo(PaymentMethod.Card, PurchaseAmount, PaymentDescription, cardTokenResponse.Card.Token),
                    new[]{SampleItem,
                    });
                Assert.AreEqual(chargeResponse.StatusCode, 200);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public async Task CreateChargePayAtFawry()
        {
            try
            {

                var client = new FawryApiClient(MerchantCode, SecurityKey, BaseApiUri);
                var chargeResponse = await client.CreateChargeTask(MerchantReferenceNumber, _customer,
                    new PaymentInfo(PaymentMethod.PayAtFawry, PurchaseAmount, PaymentDescription, paymentExpiryDateTime: DateTime.Now.AddDays(1)),
                    new[]{
                        SampleItem,
                    });
                Console.WriteLine(chargeResponse.Serialize());
                Assert.AreEqual(chargeResponse.StatusCode, 200);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        [TestMethod]
        public async Task Refund()
        {
            try
            {
                var client = new FawryApiClient(MerchantCode, SecurityKey, BaseApiUri);
                var chargeResponse = await client.RefundTask(MerchantReferenceNumber, PurchaseAmount);
                Console.WriteLine(chargeResponse.Serialize());
                Assert.AreEqual(chargeResponse.StatusCode, 200);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        [TestMethod]
        public async Task CheckPayment()
        {
            try
            {
                var client = new FawryApiClient(MerchantCode, SecurityKey, BaseApiUri);
                var chargeResponse = await client.CheckPaymentStatusTask(MerchantReferenceNumber.ToString());
                Console.WriteLine(chargeResponse.Serialize());
                Assert.AreEqual(chargeResponse.StatusCode, 200);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        [TestMethod]
        public void VerifyV2Notification()
        {
            try
            {
                var notificationObject = @"{
                                ""requestId"": ""c72827d084ea4b88949d91dd2db4996e"",
                                ""fawryRefNumber"": ""970177"",
                                ""merchantRefNumber"": ""9708f1cea8b5426cb57922df51b7f790"",
                                ""customerMobile"": ""01004545545"",
                                ""customerMail"": ""fawry@fawry.com"",
                                ""paymentAmount"": 152.00,
                                ""orderAmount"": 150.00,
                                ""fawryFees"": 2.00,
                                ""shippingFees"": null,
                                ""orderStatus"": ""NEW"",
                                ""paymentMethod"": ""PAYATFAWRY"",
                                ""messageSignature"": ""eab2c9588f0f0cf0f007e2c93f4b6ac91ba38de1023d123ceb0f6ca90a172372"",
                                ""orderExpiryDate"": 1533554719314,
                                ""orderItems"": [{
                                        ""itemCode"": ""e6aacbd5a498487ab1a10ae71061535d"",
                                        ""price"": 150.0,
                                        ""quantity"": 1
                                    }
                                ]
                            }";
                // These are sample data and an actual response need to be tested with actual security key provided by Fawry for the test to succeed
                var notification = notificationObject.Deserialize<FawryV2NotificationResponse>();
                var isVerified = notification.Verify(SecurityKey);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        [TestMethod]
        public void VerifyV1Notification()
        {
            try
            {

                IEnumerable<KeyValuePair<string, string>> notificationDictionary = new[]
                {
                    new KeyValuePair<string, string>("MerchantRefNo","128161"),
                    new KeyValuePair<string, string>("FawryRefNo","946610689"),
                    new KeyValuePair<string, string>("OrderStatus","PAID"),
                    new KeyValuePair<string, string>("Amount","150.0"),
                    new KeyValuePair<string, string>("MessageSignature","4E8EE4A2C3B09946CA0717E3E91845B9"),
                };

                // These are sample data and an actual response need to be tested with actual security key provided by Fawry for the test to succeed

                var (isVerified, resultNotification) = PaymentFlowUtilities.VerifyV1Callback(notificationDictionary, SecurityKey);
                Debug.WriteLine(resultNotification.Serialize());
                Assert.IsTrue(isVerified);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }

}
