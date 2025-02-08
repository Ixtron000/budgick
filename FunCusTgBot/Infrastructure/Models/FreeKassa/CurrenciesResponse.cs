using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Models.FreeKassa
{
    public class CurrenciesResponse
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("currencies")]
        public List<Currency> Currencies { get; set; }
    }

    public class Currency
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("currency")]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("fields")]
        public Fields Fields { get; set; }

        [JsonPropertyName("is_enabled")]
        public int IsEnabled { get; set; }

        [JsonPropertyName("is_favorite")]
        public int IsFavorite { get; set; }

        [JsonPropertyName("limits")]
        public Limits Limits { get; set; }

        [JsonPropertyName("fee")]
        public Fee Fee { get; set; }
    }

    public class Fields
    {
        [JsonPropertyName("email")]
        public EmailField Email { get; set; }
    }

    public class EmailField
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("placeholder")]
        public string Placeholder { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("validate")]
        public string Validate { get; set; }
    }

    public class Limits
    {
        [JsonPropertyName("min")]
        public decimal Min { get; set; }

        [JsonPropertyName("max")]
        public decimal Max { get; set; }
    }

    public class Fee
    {
        [JsonPropertyName("merchant")]
        public decimal Merchant { get; set; }

        [JsonPropertyName("user")]
        public decimal User { get; set; }

        [JsonPropertyName("default")]
        public decimal Default { get; set; }

        [JsonPropertyName("fee_fix")]
        public decimal? FeeFix { get; set; }
    }

}
