#nullable enable

namespace WebApiHttpClientExample.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Route("[controller]")]
    public class CreateConsentController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CreateConsentController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [HttpGet]
        public async Task<string> Get()
        {
            using var client = _httpClientFactory.CreateClient("TBC.OpenBanking.Client");
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/consents");

            request.Headers.TryAddWithoutValidation("X-Request-ID", Guid.NewGuid().ToString("N"));
            request.Headers.TryAddWithoutValidation("TPP-Redirect-URI", "https://test-openbanking.tbcbank.ge/callback");
            request.Headers.TryAddWithoutValidation("TPP-Explicit-Authorisation-Preferred", "false");
            request.Headers.TryAddWithoutValidation("PSU-IP-Address", "127.0.0.1");
            request.Headers.TryAddWithoutValidation("PSU-Accept-Language", "en");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            request.Content = new StringContent(
                JsonSerializer.Serialize(
                    new Consents
                    {
                        CombinedServiceIndicator = false,
                        FrequencyPerDay = 3,
                        ValidUntil = DateTimeOffset.Now.AddDays(10),
                        RecurringIndicator = true,
                        Access = new AccountAccess
                        {
                            Accounts = new List<AccountReference>(0),
                            Balances = new List<AccountReference>(0),
                            Transactions = new List<AccountReference>(0),
                        }
                    }));

            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            using var response = await client.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
        }
    }

    internal sealed class Consents
    {
        [JsonPropertyName("access")]
        public AccountAccess? Access { get; set; } = new AccountAccess();

        [JsonPropertyName("recurringIndicator")]
        public bool RecurringIndicator { get; set; }

        [JsonPropertyName("validUntil")]
        public DateTimeOffset ValidUntil { get; set; }

        [JsonPropertyName("frequencyPerDay")]
        public int FrequencyPerDay { get; set; }

        [JsonPropertyName("combinedServiceIndicator")]
        public bool? CombinedServiceIndicator { get; set; }

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }
    }

    internal sealed class AccountAccess
    {
        [JsonPropertyName("accounts")]
        public ICollection<AccountReference>? Accounts { get; set; }

        [JsonPropertyName("balances")]
        public ICollection<AccountReference>? Balances { get; set; }

        [JsonPropertyName("transactions")]
        public ICollection<AccountReference>? Transactions { get; set; }

        [JsonPropertyName("additionalInformation")]
        public AdditionalInformationAccess? AdditionalInformation { get; set; }

        [JsonPropertyName("restrictedTo")]
        public ICollection<string>? RestrictedTo { get; set; }

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }
    }

    internal sealed class AccountReference
    {
        [JsonPropertyName("iban")]
        public string? Iban { get; set; }

        [JsonPropertyName("bban")]
        public string? Bban { get; set; }

        [JsonPropertyName("pan")]
        public string? Pan { get; set; }

        [JsonPropertyName("maskedPan")]
        public string? MaskedPan { get; set; }

        [JsonPropertyName("msisdn")]
        public string? Msisdn { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("cashAccountType")]
        public string? CashAccountType { get; set; }

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }
    }

    internal sealed class AdditionalInformationAccess
    {
        [JsonPropertyName("ownerName")]
        public ICollection<AccountReference>? OwnerName { get; set; }

        [JsonPropertyName("trustedBeneficiaries")]
        public ICollection<AccountReference>? TrustedBeneficiaries { get; set; }

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }
    }
}
