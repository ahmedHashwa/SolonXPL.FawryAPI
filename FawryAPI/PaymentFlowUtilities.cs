﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable UnusedMember.Global

namespace SolonXpl.FawryAPI
{
    public class CustomIntDoubleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(int) || objectType == typeof(double) ||
                    Nullable.GetUnderlyingType(objectType) == typeof(int) || Nullable.GetUnderlyingType(objectType) == typeof(double);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value is double v ? v.ToString("F2") : value?.ToString());
        }
    }
    public class CustomDoubleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(value is double v ? v.ToString("F2") : null);
        }
    }
    public static class PaymentFlowUtilities
    {
        public static (bool isVerified, FawryV1NotificationResponse notification) VerifyV1Callback(IEnumerable<KeyValuePair<string, string>> notificationDictionary, string securityKey)
        {
            var dictionary = notificationDictionary.ToDictionary(k => k.Key, v => v.Value);
            var json = dictionary.Serialize();
            var notification = json.Deserialize<FawryV1NotificationResponse>();
            var isVerified = notification.Verify(securityKey);
            return (isVerified, isVerified ? notification : null);
        }
        public static string CreateFawryButton(string successPageUrl, string failurePageUrl,
            string chargeRequest,
            string mainDivId,
            Func<string, string> buttonHtml = null,
            bool excludeDiv = false)
        {
            var onclick = $"FawryPay.checkout({nameof(chargeRequest)},{nameof(successPageUrl)}, {nameof(failurePageUrl)})";
            var imageButtonHtml = $"<input  type='image' onclick='{onclick}' src='https://www.atfawry.com/assets/img/FawryPayLogo.jpg'/>";
            var mainCode = $@"{buttonHtml?.Invoke(onclick) ?? imageButtonHtml}
                    <script>
                            var {nameof(successPageUrl)}='{successPageUrl}';
                            var {nameof(failurePageUrl)}='{failurePageUrl}';
                            var {nameof(chargeRequest)}={chargeRequest};
                    </script>";
            var htmlContent = excludeDiv ? mainCode :
                $@"<div id='{mainDivId}'>
                    {mainCode}
                    </div>";
            return htmlContent;
        }
        public static string GenerateClientChargeRequest(string merchantCode, string secureKey, string merchantRefNumber, CustomerInfoClient customer, Order order, ChargeRequestLanguage lang = ChargeRequestLanguage.Ar)
        {
            var chargeRequest = new
            {
                merchantCode,
                merchantRefNumber,
                customer = new JRaw(customer.Serialize()),
                order = new JRaw(order.Serialize(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Converters = new List<JsonConverter>() { new CustomIntDoubleConverter() } })),
                expiryHours = order.Expiry,
                language = lang == ChargeRequestLanguage.Ar ? "ar-eg" : "en-gb"
            };
            var json = chargeRequest.SignedSerialize(r => new
            {
                r.merchantCode,
                r.merchantRefNumber,
                customerProfileId = customer.ProfileId,
                itemsHash = order.OrderItems.Select(o => $"{o.ProductSku}{o.Quantity}{o.Price:F2}").Join(""),
                r.expiryHours,
                secureKey,
            }, serializerSettings: null);

            return json;
        }


        public static bool VerifyResponse<T>(this T item, Expression<Func<T, string>> signatureFieldNameSelector, Func<T, object> fieldsSelector = null, SignatureOptions options = null)
        {
            var o = item;
            var data = o.AsDictionary();
            var receivedSignature = signatureFieldNameSelector.Compile()(item);
            if (fieldsSelector == null)
            {
                var signatureFiledName = (PropertyInfo)((MemberExpression)signatureFieldNameSelector.Body).Member;
                data.Remove(signatureFiledName.Name);
            }
            var signature = GenerateSignature(fieldsSelector?.Invoke(item).AsDictionary() ?? data.AsDictionary(), options);
            Console.WriteLine(signature);
            return Equals(receivedSignature, signature);
        }


        public static IDictionary<string, object> InjectSignature<T>(this T data, Func<T, object> signatureComponentsSelector,
            string signatureFieldKey = "signature",
            SignatureOptions options = null)
        {
            var dataDictionary = data.AsDictionary().ToDictionary(k => k.Key, v => v.Value);

            var signatureDictionary =
                signatureComponentsSelector(data).AsDictionary().ToDictionary(k => k.Key, v => v.Value);
            var signature = GenerateSignature(signatureDictionary, options);
            dataDictionary[signatureFieldKey] = signature;
            return dataDictionary;
        }

        public static string SignedSerialize<T>(this T data, Func<T, object> signatureComponentsSelector, string signatureFieldKey = "signature", JsonSerializerSettings serializerSettings = null, SignatureOptions options = null)
        {
            var dataDictionary = data.AsDictionary().ToDictionary(k => k.Key, v => v.Value);

            var signatureDictionary = signatureComponentsSelector(data).AsDictionary().ToDictionary(k => k.Key, v => v.Value);
            var signature = GenerateSignature(signatureDictionary, options);
            dataDictionary[signatureFieldKey] = signature;
            dataDictionary
                .Where(o => o.Value is IEnumerable && !(o.Value is string))
                .ToList()
                .ForEach(o => dataDictionary[o.Key] = new JRaw(o.Value.Serialize()));
            var result = dataDictionary.Serialize(serializerSettings);
            return result;

        }
        public static string GenerateSignature(IDictionary<string, object> data, SignatureOptions options = null)
        {
            options ??= new SignatureOptions();
            var dataString = $"{string.Join(options.ConcatenationSeparator, options.SignatureDictionaryTransformerFunc?.Invoke(data) ?? data.Select(d => $"{d.Value}"))}";
            var signature = options.HashingAlgorithm switch
            {
                HashingAlgorithm.Sha256 => dataString.Sha256(),
                HashingAlgorithm.Md5 => dataString.Md5(),
                _ => throw new ArgumentOutOfRangeException(nameof(options.HashingAlgorithm), options.HashingAlgorithm, null)
            };

            return options.SignatureTransformerFunc?.Invoke(signature) ?? signature;
        }

    }

    public class SignatureOptions
    {
        public HashingAlgorithm HashingAlgorithm { get; set; } = HashingAlgorithm.Sha256;

        public Func<IDictionary<string, object>, IEnumerable<string>> SignatureDictionaryTransformerFunc { get; set; } =
            null;
        public Func<string, string> SignatureTransformerFunc { get; set; } = null;
        public string ConcatenationSeparator { get; set; } = "";
    }

}
