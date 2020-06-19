using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

// ReSharper disable UnusedMember.Global

namespace SolonXpl.FawryAPI
{
    public class FawryApiClient
    {
        private readonly string _baseApiUri;
        private readonly string _merchantCode;
        private readonly string _securityKey;
        private readonly HttpClient _client;
        public FawryApiClient(string merchantCode, string securityKey, string baseApiUri)
        {
            _baseApiUri = baseApiUri;
            _merchantCode = merchantCode;
            _securityKey = securityKey;
            ServicePointManager.SecurityProtocol =
        SecurityProtocolType.Tls12 |
        SecurityProtocolType.Tls11 |
        SecurityProtocolType.Tls;
            _client = HttpClientFactory.Create();
        }
        public async Task<CreateCardTokenResponse> CreateCreditCardTokenTask(CustomerPaymentCard card, CustomerInfo customerInfo, CancellationToken cancellationToken = default)
        {
            var parameters = new
            {
                merchantCode = _merchantCode,
                customerProfileId = customerInfo.ProfileId,
                customerMobile = customerInfo.Mobile,
                customerEmail = customerInfo.Email,
                cardNumber = card.CardNumber,
                expiryYear = card.Year,
                expiryMonth = card.Month,
                cvv = card.Cvv,
            };
            var json = parameters.Serialize();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_baseApiUri.TrimEnd('/')}/ECommerceWeb/Fawry/cards/cardToken", content, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ResponseException(response);
            if (cancellationToken.IsCancellationRequested)
                return null;
            var responseData = await response.Content.ReadAsStringAsync();
            var fawryResponseData = responseData.Deserialize<CreateCardTokenResponse>();
            if (fawryResponseData.StatusCode == 200)
                return fawryResponseData;
            throw new Exception($"{fawryResponseData.StatusDescription}")
            {
                Data = {
                        { nameof(fawryResponseData.Type), fawryResponseData.Type },
                        { nameof(fawryResponseData.StatusCode), fawryResponseData.StatusCode },
                        { nameof(fawryResponseData.StatusDescription), fawryResponseData.StatusDescription },
                    }
            };
        }
        public async Task<FawryResponse> RefundTask(int referenceNumber, double refundAmount, string reason = null, CancellationToken cancellationToken = default)
        {
            var parameters = new
            {
                merchantCode = _merchantCode,
                referenceNumber = referenceNumber.ToString(),
                refundAmount,
                reason
            };
            var json = parameters.SignedSerialize(p => new
            {
                p.merchantCode,
                p.referenceNumber,
                refundAmount = refundAmount.ToString("F2"),
                p.reason,
                _securityKey

            }, serializerSettings: new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Converters = { new CustomDoubleConverter() } });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_baseApiUri.TrimEnd('/')}/ECommerceWeb/Fawry/payments/refund", content, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ResponseException(response);
            if (cancellationToken.IsCancellationRequested)
                return null;
            var responseData = await response.Content.ReadAsStringAsync();
            var fawryResponseData = responseData.Deserialize<FawryResponse>();
            if (fawryResponseData.StatusCode == 200)
                return fawryResponseData;
            throw new Exception($"{fawryResponseData.StatusDescription}")
            {
                Data = {
                        { nameof(fawryResponseData.Type), fawryResponseData.Type },
                        { nameof(fawryResponseData.StatusCode), fawryResponseData.StatusCode },
                        { nameof(fawryResponseData.StatusDescription), fawryResponseData.StatusDescription },
                    }
            };
        }

        public async Task<CreatingChargesResponse> CreateChargeTask(int merchantReferenceNumber,
            string customerProfileId, PaymentInfo paymentInfo, ChargeItem[] chargeItems = null,
            CancellationToken cancellationToken = default)
        {
            return await CreateChargeTask(merchantReferenceNumber, new CustomerInfo(customerProfileId, null, null), paymentInfo, chargeItems,
                cancellationToken);
        }

        public async Task<CreatingChargesResponse> CreateChargeTask(int merchantReferenceNumber, CustomerInfo customerInfo, PaymentInfo paymentInfo, ChargeItem[] chargeItems, CancellationToken cancellationToken = default)
        {
            var parameters = new
            {
                merchantCode = _merchantCode,
                merchantRefNum = merchantReferenceNumber,
                customerProfileId = customerInfo.ProfileId,
                customerMobile = customerInfo.Mobile,
                customerEmail = customerInfo.Email,
                paymentMethod = paymentInfo.PaymentMethod.ToString().ToUpper(),
                amount = paymentInfo.Amount.ToString("F2"),
                currencyCode = paymentInfo.CurrencyCode,
                description = paymentInfo.Description,
                paymentExpiry = paymentInfo.PaymentExpiry,
                chargeItems
            };
            var json = parameters.SignedSerialize(p => new
            {

                p.merchantCode,
                p.merchantRefNum,
                p.customerProfileId,
                p.paymentMethod,
                p.amount,
                Token = paymentInfo.PaymentMethod == PaymentMethod.PayAtFawry ? "" : paymentInfo.CardToken,
                _securityKey
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_baseApiUri.TrimEnd('/')}/ECommerceWeb/Fawry/payments/charge", content, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ResponseException(response);
            if (cancellationToken.IsCancellationRequested)
                return null;
            var responseData = await response.Content.ReadAsStringAsync();
            var fawryResponseData = responseData.Deserialize<CreatingChargesResponse>();
            if (fawryResponseData.StatusCode == 200)
                return fawryResponseData;
            throw new Exception($"{fawryResponseData.StatusDescription}")
            {
                Data = {
                        { nameof(fawryResponseData.Type), fawryResponseData.Type },
                        { nameof(fawryResponseData.StatusCode), fawryResponseData.StatusCode },
                        { nameof(fawryResponseData.StatusDescription), fawryResponseData.StatusDescription },
                    }
            };
        }
        public async Task<ListCustomerTokensResponse> ListCustomerCardTokensTask(string customerProfileId, CancellationToken cancellationToken = default)
        {
            var parameters = new
            {
                merchantCode = _merchantCode,
                customerProfileId,
                
            };
            var url =
                $"{_baseApiUri.TrimEnd('/')}/ECommerceWeb/Fawry/cards/cardToken?{parameters.InjectSignature(p=>new{p.merchantCode,p.customerProfileId, _securityKey},options:null).ToFormData()}";
            var response = await _client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ResponseException(response);
            if (cancellationToken.IsCancellationRequested)
                return null;
            var responseData = await response.Content.ReadAsStringAsync();
            var fawryResponseData = responseData.Deserialize<ListCustomerTokensResponse>();
            if (fawryResponseData.StatusCode == 200)
                return fawryResponseData;
            throw new Exception($"{fawryResponseData.StatusDescription}")
            {
                Data = {
                        { nameof(fawryResponseData.Type), fawryResponseData.Type },
                        { nameof(fawryResponseData.StatusCode), fawryResponseData.StatusCode },
                        { nameof(fawryResponseData.StatusDescription), fawryResponseData.StatusDescription },
                    }
            };
        }
        public async Task<PaymentStatusResponse> CheckPaymentStatusTask(string merchantRefNumber, CancellationToken cancellationToken = default)
        {
            var parameters = new
            {
                merchantCode = _merchantCode,
                merchantRefNumber,
            };
            var url =
                $"{_baseApiUri.TrimEnd('/')}/ECommerceWeb/Fawry/payments/status?{parameters.InjectSignature(p=>new{p.merchantCode,p.merchantRefNumber,_securityKey}).ToFormData()}";
            var response = await _client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ResponseException(response);
            if (cancellationToken.IsCancellationRequested)
                return null;
            var responseData = await response.Content.ReadAsStringAsync();
            var fawryResponseData = responseData.Deserialize<PaymentStatusResponse>();
            if (fawryResponseData.StatusCode == 200)
                return fawryResponseData;
            throw new Exception($"{fawryResponseData.StatusDescription}")
            {
                Data = {
                        { nameof(fawryResponseData.Type), fawryResponseData.Type },
                        { nameof(fawryResponseData.StatusCode), fawryResponseData.StatusCode },
                        { nameof(fawryResponseData.StatusDescription), fawryResponseData.StatusDescription },
                    }
            };
        }
        public async Task<CardTokenResponse> DeleteCustomerCardTokensTask(string customerProfileId, string cardToken, CancellationToken cancellationToken = default)
        {
            var parameters = new
            {
                merchantCode = _merchantCode,
                customerProfileId,
                cardToken
            };
            var url =
                $"{_baseApiUri.TrimEnd('/')}/ECommerceWeb/Fawry/cards/cardToken?" +
                $"{parameters.InjectSignature(p=>new{p.merchantCode,p.customerProfileId,p.cardToken,_securityKey}).ToFormData()}";
            var response = await _client.DeleteAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new ResponseException(response);
            if (cancellationToken.IsCancellationRequested)
                return null;
            var responseData = await response.Content.ReadAsStringAsync();
            var fawryResponseData = responseData.Deserialize<CardTokenResponse>();
            if (fawryResponseData.StatusCode == 200)
                return fawryResponseData;
            throw new Exception($"{fawryResponseData.StatusDescription}")
            {
                Data = {
                        { nameof(fawryResponseData.Type), fawryResponseData.Type },
                        { nameof(fawryResponseData.StatusCode), fawryResponseData.StatusCode },
                        { nameof(fawryResponseData.StatusDescription), fawryResponseData.StatusDescription },
                    }
            };
        }
    }


}
