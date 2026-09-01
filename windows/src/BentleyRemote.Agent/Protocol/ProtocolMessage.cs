using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace BentleyRemote.Agent.Protocol;

internal sealed record ProtocolMessage(
    int Version,
    string Type,
    string Id,
    string? ReplyTo,
    long SentAt,
    JsonElement Payload);

internal static class ProtocolCodec
{
    private const int MaxMessageBytes = 1_500_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize(string type, object payload, string? replyTo = null, string? id = null)
    {
        var envelope = new
        {
            version = 1,
            type,
            id = id ?? Guid.NewGuid().ToString(),
            replyTo,
            sentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            payload
        };
        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    public static ProtocolMessage Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 32
        });
        var root = document.RootElement;
        var message = new ProtocolMessage(
            root.GetProperty("version").GetInt32(),
            root.GetProperty("type").GetString() ?? throw new JsonException("type is required"),
            root.GetProperty("id").GetString() ?? throw new JsonException("id is required"),
            root.TryGetProperty("replyTo", out var reply) && reply.ValueKind == JsonValueKind.String ? reply.GetString() : null,
            root.GetProperty("sentAt").GetInt64(),
            root.GetProperty("payload").Clone());
        if (message.Version != 1) throw new JsonException($"Unsupported protocol version {message.Version}");
        return message;
    }

    public static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close) return null;
            if (result.MessageType != WebSocketMessageType.Text)
                throw new WebSocketException("Only text WebSocket messages are accepted.");
            stream.Write(buffer, 0, result.Count);
            if (stream.Length > MaxMessageBytes)
                throw new WebSocketException("Protocol message is too large.");
            if (result.EndOfMessage) return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
