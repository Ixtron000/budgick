using Infrastructure.Models.FreeKassa;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Bussines.Services
{
    public class FreeKassaService
    {
        private const int ShopId = 53325;
        private const string ApiKey = "f9009b140a7e56a63f0f4235d71baed8"; // Replace with your actual API key
        private const string Email = "vitcher20u@gmail.com";
        private const string IpAddress = "89.111.141.136";

        public async Task<CurrenciesResponse> GetCurrencies()
        {
            var data = new Dictionary<string, object>
        {
            { "shopId", ShopId },
            { "nonce", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        };

            var signature = CreateHmacSha256Signature(data);
            data["signature"] = signature;

            var request = JsonConvert.SerializeObject(data);
            var resultJson = await SendRequestAsync("https://api.freekassa.com/v1/currencies", request);
            var result = JsonConvert.DeserializeObject<CurrenciesResponse>(resultJson.jsonResponse);

            foreach (var currency in result.Currencies)
            {
                Console.WriteLine($"Currency: {currency.Name} ({currency.CurrencyCode})");
                Console.WriteLine($"Limits: {currency.Limits.Min} - {currency.Limits.Max}");
                Console.WriteLine($"Fee (Merchant): {currency.Fee.Merchant}%");
            }

            return result;
        }

        public async Task<List<Currency>> GetAvailableCurrencies(List<Currency> currencies)
        {
            var resultCurrency = new List<Currency>();

            foreach (var currency in currencies)
            {
                var data = new Dictionary<string, object>
            {
                { "shopId", ShopId },
                { "nonce", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            };

                var signature = CreateHmacSha256Signature(data);
                data["signature"] = signature;

                var request = JsonConvert.SerializeObject(data);
                var resultResponse = await SendRequestAsync($"https://api.freekassa.com/v1/currencies/{currency.Id}/status", request);
                if (resultResponse.isSuccess)
                {
                    resultCurrency.Add(currency);
                    Console.WriteLine($"isSuccess: {resultResponse.jsonResponse})");
                    Console.WriteLine($"Currency: {currency.Name} ({currency.CurrencyCode})");
                    Console.WriteLine($"Limits: {currency.Limits.Min} - {currency.Limits.Max}");
                    Console.WriteLine($"Fee (Merchant): {currency.Fee.Merchant}%");
                }
            }

            return resultCurrency;
        }

        static string GenerateLongNonce(int length)
        {
            byte[] bytes = new byte[length / 2];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public async Task<Dictionary<string, object>> CreateLinkForPayAsync(long userId, double price, int paySystemId)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "shopId", ShopId },
                    { "nonce", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                    { "i", paySystemId },
                    { "email", Email },
                    { "ip", IpAddress },
                    { "paymentId", userId },
                    { "amount", price },
                    { "currency", "RUB" },
                };

                var signature = CreateHmacSha256Signature(data);
                data["signature"] = signature;

                var request = JsonConvert.SerializeObject(data);
                var result = await SendRequestAsync("https://api.freekassa.com/v1/orders/create", request);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.jsonResponse);

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Exception: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}");
                return null;
            }
        }

        public async Task<Dictionary<string, object>> GetOrderAsync(string orderId)
        {
            try
            {
                var data = new Dictionary<string, object>
            {
                { "shopId", ShopId },
                { "nonce", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "orderId", orderId }
            };

                var signature = CreateHmacSha256Signature(data);
                data["signature"] = signature;

                var request = JsonConvert.SerializeObject(data);
                var result = await SendRequestAsync("https://api.freekassa.com/v1/orders", request);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.jsonResponse);

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Exception: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}");
                return null;
            }
        }

        private string CreateHmacSha256Signature(Dictionary<string, object> data)
        {
            var sortedData = data.OrderBy(d => d.Key).ToDictionary(d => d.Key, d => d.Value);
            var signData = string.Join("|", sortedData.Values);

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signData));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private async Task<(bool isSuccess, string jsonResponse)> SendRequestAsync(string url, string json)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer your_token_if_any");
                var response = await client.PostAsync(url, content);
                //response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return (response.IsSuccessStatusCode, System.Text.RegularExpressions.Regex.Unescape(responseContent));
            }
        }
    }
}
