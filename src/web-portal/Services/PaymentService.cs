using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenMU.PlayerWeb.Data;

namespace OpenMU.PlayerWeb.Services;

public class PaymentService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly string _clientId;
    private readonly string _apiKey;
    private readonly string _checksumKey;

    public PaymentService(IDbContextFactory<AppDbContext> dbFactory, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _clientId = config["PAYOS_CLIENT_ID"] ?? string.Empty;
        _apiKey = config["PAYOS_API_KEY"] ?? string.Empty;
        _checksumKey = config["PAYOS_CHECKSUM_KEY"] ?? string.Empty;
    }

    public string CreatePaymentLink(Guid accountId, int amountVnd)
    {
        // Simple mock for generating a payment link. 
        // In a real app, we would HTTP POST to PayOS API to get a checkoutUrl.
        // For the scope of this implementation without adding NuGet packages,
        // we will generate a mock URL that the user can visit.
        
        // OrderCode must be numeric and unique for PayOS. We use a random number for mockup.
        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var signatureData = $"amount={amountVnd}&cancelUrl=http://localhost:3007/payment/cancel&description=Topup WCoin for {accountId}&orderCode={orderCode}&returnUrl=http://localhost:3007/payment/success";
        
        // Creating a mockup URL for demonstration
        return $"https://pay.payos.vn/mockup?orderCode={orderCode}&amount={amountVnd}&description=Account:{accountId}";
    }

    public async Task<bool> HandleWebhookAsync(JsonElement payload)
    {
        // PayOS webhook body: { "code": "00", "desc": "success", "success": true,
        //   "data": { "orderCode": 123, "amount": 10000, "description": "Account:xxx-xxx",
        //             "status": "PAID", ... }, "signature": "<HMACSHA256 hex>" }
        //
        // The signature signs the "data" object only, serialized with sorted keys, using the
        // checksum key as the HMAC secret. Without this check, anyone could POST a forged body and
        // mint free WCoin, so verification is mandatory before any balance change.
        try
        {
            if (!payload.TryGetProperty("data", out var dataElement)) return false;

            if (!this.VerifySignature(dataElement, payload))
            {
                return false;
            }

            if (!dataElement.TryGetProperty("description", out var descElement)) return false;
            if (!dataElement.TryGetProperty("amount", out var amountElement)) return false;
            if (!dataElement.TryGetProperty("status", out var statusElement)) return false;

            // Only credit on an explicitly paid status (PayOS uses "PAID" for successful payment).
            if (!string.Equals(statusElement.GetString(), "PAID", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var desc = descElement.GetString() ?? "";
            var amount = amountElement.GetInt64();

            // Reject zero/negative amounts outright: 1 WCoin = 100 VND, so amounts below 100 VND
            // would silently credit 0 WCoin and anything negative would drain the balance.
            if (amount < 100)
            {
                return false;
            }

            // Extract AccountId from description
            if (desc.StartsWith("Account:", StringComparison.Ordinal) && Guid.TryParse(desc[8..], out var accountId))
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
                if (account != null)
                {
                    int wCoinToAdd = (int)(amount / 100);
                    account.WCoin += wCoinToAdd;
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies the webhook <c>signature</c> field against the HMAC-SHA256 of the <c>data</c> object
    /// (serialized with sorted keys, lowercase hex), using the checksum key as the secret. Returns true
    /// when the signature matches or when no checksum key is configured yet (local/dev only, before
    /// production credentials are provided).
    /// </summary>
    private bool VerifySignature(JsonElement dataElement, JsonElement payload)
    {
        if (!payload.TryGetProperty("signature", out var signatureElement))
        {
            return false;
        }

        var provided = signatureElement.GetString();
        if (string.IsNullOrEmpty(provided))
        {
            return false;
        }

        // Without a configured checksum key we cannot verify; only allow this in a non-production
        // environment so a locally-configured gateway never mints coins without credentials.
        if (string.IsNullOrEmpty(_checksumKey))
        {
            return false;
        }

        var canonical = this.SortJsonAndSerialize(dataElement);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        var expected = Convert.ToHexStringLower(hash);

        return string.Equals(provided, expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Serializes a JSON element with object keys sorted lexicographically, matching PayOS's signature
    /// canonicalization. Nested objects are sorted recursively; arrays and scalars are emitted as-is.
    /// </summary>
    private string SortJsonAndSerialize(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            this.WriteSortedElement(writer, element);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private void WriteSortedElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    this.WriteSortedElement(writer, property.Value);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    this.WriteSortedElement(writer, item);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText());
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                writer.WriteBooleanValue(element.GetBoolean());
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                writer.WriteNullValue();
                break;
        }
    }
}
