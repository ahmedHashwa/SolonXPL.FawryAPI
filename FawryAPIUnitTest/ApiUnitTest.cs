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
        private const string MerchantCode = "1tSa6uxz2nQsUE4afeg7uA==";
        private const string SecurityKey = "31d932eb514841c8ab35eb455012a53c";

        private readonly CustomerInfo _customer = new CustomerInfo
        (
            Guid.NewGuid().ToString(),
            "01005458821",
            "ahmed.samy@el-eng.menofia.edu.eg"
        );

        private readonly CustomerPaymentCard _customerPaymentCard = new CustomerPaymentCard
        (
            "5123456789012346",
            "100",
            "05",
            "21"
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
                var chargeResponse = await client.CreateChargeTask(198198741, _customer,
                    new PaymentInfo(PaymentMethod.Card, 350, "œÊ—…  ‰„Ì… ﬁœ—«  √⁄÷«¡ ÂÌ∆… «· œ—Ì”", cardTokenResponse.Card.Token),
                    new[]{
                        new ChargeItem
                            {
                                Description = "»—‰«„Ã «·œ—«”… «·–« Ì… ·„ƒ””«  «· ⁄·Ì„ «·⁄«·Ì",
                                Price = 350,
                                ItemId = 234234987,
                            },
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
                _customer.Mobile = "01226610849";

                var client = new FawryApiClient(MerchantCode, SecurityKey, BaseApiUri);
                var chargeResponse = await client.CreateChargeTask(99841813, _customer,
                    new PaymentInfo(PaymentMethod.PayAtFawry, 450, "œÊ—…  ‰„Ì… ﬁœ—«  √⁄÷«¡ ÂÌ∆… «· œ—Ì”", paymentExpiryDateTime: DateTime.Now.AddDays(1)),
                    new[]{
                        new ChargeItem
                        {
                            Description = "»—‰«„Ã «” Œœ«„ ﬁÊ«⁄œ «·»Ì«‰«  «·⁄«·„Ì…",
                            Price = 250,
                            ItemId = 23234387,
                        },
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
                var chargeResponse = await client.RefundTask(946592686, 150);
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
                var chargeResponse = await client.CheckPaymentStatusTask("128157");
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
                //IEnumerable<KeyValuePair<string, string>> notificationDictionary = new[]
                //{
                //    new KeyValuePair<string, string>("MerchantRefNo","128160"),
                //    new KeyValuePair<string, string>("FawryRefNo","946619171"),
                //    new KeyValuePair<string, string>("OrderStatus","NEW"),
                //    new KeyValuePair<string, string>("Amount","150.0"),
                //    new KeyValuePair<string, string>("MessageSignature","8B93D5F78F06909ACBCBEC9DF2FA517E"),
                //};
                //IEnumerable<KeyValuePair<string, string>> notificationDictionary = new[]
                //{
                //    new KeyValuePair<string, string>("MerchantRefNo","128151"),
                //    new KeyValuePair<string, string>("FawryRefNo","946524382"),
                //    new KeyValuePair<string, string>("OrderStatus","PAID"),
                //    new KeyValuePair<string, string>("Amount","500.0"),
                //    new KeyValuePair<string, string>("MessageSignature","9CABFDCF35EBBB195685ED4AC6B56C31"),
                //};
                IEnumerable<KeyValuePair<string, string>> notificationDictionary = new[]
                {
                    new KeyValuePair<string, string>("MerchantRefNo","128161"),
                    new KeyValuePair<string, string>("FawryRefNo","946610689"),
                    new KeyValuePair<string, string>("OrderStatus","PAID"),
                    new KeyValuePair<string, string>("Amount","150.0"),
                    new KeyValuePair<string, string>("MessageSignature","4E8EE4A2C3B09946CA0717E3E91845B9"),
                };


                var (isVerified, resultNotification) = PaymentFlowUtilities.VerifyV1Callback(notificationDictionary, SecurityKey);
                //Debug.WriteLine(resultNotification.Serialize());
                Assert.IsTrue(isVerified);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        [TestMethod]
        public void Encrypt()
        {
            try
            {
                var key = "NA&UW@MPemxb97yGb";
                Console.WriteLine(key.Encrypt());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        [TestMethod]
        public void Decrypt()
        {
            try
            {
                var key = "6dr65hAQaGh5A6XqkOIncXSH1YmD9M76wWSAhhCLVmE8u+whK/GU5+4ReJQ9MI2SSaqikji+SozpWJYN/xL1";
                Console.WriteLine(key.Decrypt());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
    public static class Utilities
    {
        private static string _sharedSecret = "2SFQAxVrewJaWRTk*PPBqjQjPRNYvSC6!gpGqe48zUkAedgkV^tu3h5p3C8ju&h#EY3FH";

        public static string Encrypt(this string clearText)
        {
            var clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (var encryptor = Aes.Create())
            {
                var salt = Convert.FromBase64String("6dr65hAQaGh5A6XqkOIn");
                var iv = salt.Take(15).ToArray();
                var pdb = new Rfc2898DeriveBytes(_sharedSecret, iv);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(iv) + Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public static string Decrypt(this string cipherText)
        {
            cipherText = cipherText.Trim('"');
            var IV = Convert.FromBase64String(cipherText.Substring(0, 20));
            cipherText = cipherText.Substring(20).Replace(" ", "+");
            var cipherBytes = Convert.FromBase64String(cipherText);
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(_sharedSecret, IV);
                if (encryptor != null)
                {
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }

                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            return cipherText;
        }

    }

}
