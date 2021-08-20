using System;
using Newtonsoft.Json;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace SolonXpl.FawryAPI
{

    public class FawryV1NotificationResponse
    {
        public string MerchantRefNo { get; set; }
        public long FawryRefNo { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public float Amount { get; set; }
        public string MessageSignature { get; set; }

        public bool Verify(string secureKey) => this.VerifyResponse(r => r.MessageSignature, r => new
        {
            secureKey,
            Amount = r.Amount.ToString("F1"),
            r.FawryRefNo,
            r.MerchantRefNo,
            OrderStatus = r.OrderStatus.ToString().ToUpper(),
        },new SignatureOptions{HashingAlgorithm=HashingAlgorithm.Md5,SignatureTransformerFunc = s=>s.ToUpper()});
    }
    public class FawryV2NotificationResponse
    {
        public string RequestId { get; set; }
        public string FawryRefNumber { get; set; }
        public string MerchantRefNumber { get; set; }
        public string CustomerMobile { get; set; }
        public string CustomerMail { get; set; }
        public float PaymentAmount { get; set; }
        public float OrderAmount { get; set; }
        public float FawryFees { get; set; }
        public float? ShippingFees { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string MessageSignature { get; set; }
        public long? OrderExpiryDate { get; set; }
        public DateTime? OrderExpiryDateTime => OrderExpiryDate == null ? (DateTime?)null : DateTimeOffset.FromUnixTimeMilliseconds(OrderExpiryDate.Value).LocalDateTime;

        public NotificationOrderItem[] OrderItems { get; set; }
        public bool Verify(string secureKey) => this.VerifyResponse(n => n.MessageSignature, n => new
        {
            n.FawryRefNumber,
            n.MerchantRefNumber,
            PaymentAmount = n.PaymentAmount.ToString("F2"),
            OrderAmount = n.OrderAmount.ToString("F2"),
            OrderStatus = n.OrderStatus.ToString().ToUpper(),
            PaymentMethod = n.PaymentMethod.ToString().ToUpper(),
            secureKey
        });
    }

    public class NotificationOrderItem
    {
        public string ItemCode { get; set; }
        public float Price { get; set; }
        public int Quantity { get; set; }
    }

    public class FawryResponse
    {
        public string Type { get; set; }
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; }
    }
    public class CardTokenResponse : FawryResponse
    {
    }
    public class CreatingChargesResponse : FawryResponse
    {
        public string ReferenceNumber { get; set; }
        public string MerchantRefNumber { get; set; }
        public long? ExpirationTime { get; set; }
        public DateTime? ExpirationDateTime => ExpirationTime == null ? (DateTime?)null : DateTimeOffset.FromUnixTimeMilliseconds(ExpirationTime.Value).LocalDateTime;
    }
    public class CreateCardTokenResponse : FawryResponse
    {
        public Card Card { get; set; }
    }
    public class ListCustomerTokensResponse : FawryResponse
    {
        public Card[] Cards { get; set; }
    }
    public class PaymentStatusResponse : FawryResponse
    {
        public string ReferenceNumber { get; set; }
        public string MerchantRefNumber { get; set; }
        public double PaymentAmount { get; set; }
        public long? PaymentDate { get; set; }
        public DateTime? ExpirationDateTime => ExpirationTime == null ? (DateTime?)null : DateTimeOffset.FromUnixTimeMilliseconds(ExpirationTime.Value).LocalDateTime;
        public DateTime? PaymentDateTime => PaymentDate == null ? (DateTime?)null : DateTimeOffset.FromUnixTimeMilliseconds(PaymentDate.Value).LocalDateTime;
        public long? ExpirationTime { get; set; }
        public string PaymentStatus { get; set; }
        public PaymentStatus Status => PaymentStatus.Parse<PaymentStatus>();
        public string PaymentMethod { get; set; }
        public PaymentMethod Method => PaymentMethod.Parse<PaymentMethod>();
    }
    public class CustomerPaymentCard
    {
        public CustomerPaymentCard(string cardNumber, string cvv, string month, string year)
        {
            CardNumber = cardNumber;
            Cvv = cvv;
            Month = month;
            Year = year;
        }
        public string CardNumber { get; set; }
        public string Cvv { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
    }
    public class PaymentInfo
    {
        public PaymentInfo(PaymentMethod paymentMethod, double amount, string description, string cardToken = null, DateTime? paymentExpiryDateTime = null)
        {
            PaymentMethod = paymentMethod;
            Amount = amount;
            Description = description;
            CardToken = cardToken;
            //if (paymentMethod == PaymentMethod.Card && CardToken == null)
            //    throw new ArgumentException($"When Payment Method is Card, {nameof(cardToken)} parameter must be set", nameof(cardToken));
        }
        public PaymentMethod PaymentMethod { get; set; }
        public double Amount { get; set; }
        public string CurrencyCode { get; set; } = "EGP";
        public string CardToken { get; set; }
        public string Description { get; set; }
        public DateTime? PaymentExpiryDateTime { get; set; }
        public long? PaymentExpiry => PaymentExpiryDateTime.HasValue ? (long)PaymentExpiryDateTime.Value.TotalMilliseconds() : (long?)null;
    }
    public class ChargeItem
    {

        [JsonProperty("itemId")]
        public int ItemId { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("price")]
        public float Price { get; set; }
        [JsonProperty("quantity")]
        public int Quantity { get; set; } = 1;

        public ChargeItem()
        {
        }

        public ChargeItem(int itemId, string description, float price, int quantity)
        {
            ItemId = itemId;
            Description = description;
            Price = price;
            Quantity = quantity;
        }
    }
    public class CustomerInfo
    {
        public CustomerInfo(string profileId, string mobile, string email)
        {
            ProfileId = profileId;
            Mobile = mobile.Replace(" ", "").Replace("+2", "");
            Email = email;
        }
        public string ProfileId { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
    }
    public class CustomerInfoClient
    {
        public CustomerInfoClient(string name, string mobile, string email, string profileId = null)
        {
            ProfileId = profileId;
            Mobile = mobile;
            Email = email;
            Name = name;
        }
        [JsonProperty("customerProfileId")]
        public string ProfileId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("mobile")]
        public string Mobile { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
    }
    public class Card
    {
        public string Token { get; set; }
        public long CreationDate { get; set; }
        public DateTime CreationDateTime => DateTimeOffset.FromUnixTimeMilliseconds(CreationDate).LocalDateTime;
        public string LastFourDigits { get; set; }
        public string Brand { get; set; }
    }

    public class Order
    {/// <summary>
     /// Creates order object
     /// </summary>
     /// <param name="description">POS Description</param>
     /// <param name="expiry">No of hours before order is expired</param>
     /// <param name="orderItems">Requested orders</param>
        public Order(string description, OrderItem[] orderItems, int? expiry = null)
        {
            Description = description;
            Expiry = expiry;
            OrderItems = orderItems;
        }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("expiry")]
        public int? Expiry { get; set; }
        [JsonProperty("orderItems")]
        public OrderItem[] OrderItems { get; set; }
    }


    public class FawryChargeResponse
    {
        public int MerchantRefNumber { get; set; }
        public long FawryRefNumber { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Signature { get; set; }
    }

    public class OrderItem
    {
        [JsonProperty("productSKU")]
        public string ProductSku { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("price")]
        public double Price { get; set; }
        [JsonProperty("quantity")]
        public int Quantity { get; set; } = 1;
        [JsonProperty("width")]
        public int? Width { get; set; }
        [JsonProperty("height")]
        public int? Height { get; set; }
        [JsonProperty("length")]
        public int? Length { get; set; }
        [JsonProperty("weight")]
        public int? Weight { get; set; }
    }
    public enum PaymentMethod
    {
        PayAtFawry, CashOnDelivery, Card, Wallet
    }
    public enum ChargeRequestLanguage
    {
        Ar, En
    }

    public enum HashingAlgorithm
    {
        Sha256, Md5
    }
    public enum OrderStatus
    {
        New=0, Paid=1, Canceled=2, Delivered=3, Refunded=4, Expired=5,Failed=6,ORDER_FAILED=6
    }
    public enum PaymentStatus
    {
        Paid, Unpaid, Refunded, Expired, Cancelled, Failed
    }
}