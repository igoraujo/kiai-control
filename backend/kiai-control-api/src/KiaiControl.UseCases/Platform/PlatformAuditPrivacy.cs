using System.Text.Json;
using System.Text.Json.Nodes;

namespace KiaiControl.UseCases.Platform;

public static class PlatformAuditPrivacy
{
    private static readonly HashSet<string> AllowedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "context", "before", "after", "reason", "source", "method", "route", "statusCode", "correlationId",
        "status", "roles", "role", "isActive", "mustChangePassword", "sessionsRevoked", "archived",
        "privacyHold", "personalDataRedacted", "filters", "organizationId", "from", "to", "page", "pageSize", "action"
    };

    public static string ActorReference(Guid? userId) => userId is Guid id ? $"Conta {id:N}"[..14] : "Sistema / autor não retido";

    public static string SafeMetadata(string? metadata)
    {
        try
        {
            using var document = JsonDocument.Parse(metadata ?? "{}", new JsonDocumentOptions { MaxDepth = 16 });
            return Sanitize(document.RootElement, 0)?.ToJsonString() ?? "{}";
        }
        catch (JsonException) { return "{}"; }
    }

    private static JsonNode? Sanitize(JsonElement element, int depth)
    {
        if (depth > 8) return null;
        if (element.ValueKind == JsonValueKind.Object)
        {
            var result = new JsonObject();
            foreach (var property in element.EnumerateObject().Where(x => AllowedKeys.Contains(x.Name)))
                result[property.Name] = Sanitize(property.Value, depth + 1);
            return result;
        }
        if (element.ValueKind == JsonValueKind.Array)
            return new JsonArray(element.EnumerateArray().Take(30).Select(x => Sanitize(x, depth + 1)).ToArray());
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString() ?? "";
            return JsonValue.Create(value[..Math.Min(value.Length, 500)]);
        }
        return JsonNode.Parse(element.GetRawText());
    }
}
