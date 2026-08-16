using System.Text.Json;

namespace PUPlayer.Core.Playback;

public static class PlayerProtocol
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

    public static PlayerRequest DeserializeRequest(string value) =>
        JsonSerializer.Deserialize<PlayerRequest>(value, Json) ?? throw new JsonException("Request is empty.");

    public static PlayerEvent DeserializeEvent(string value) =>
        JsonSerializer.Deserialize<PlayerEvent>(value, Json) ?? throw new JsonException("Event is empty.");
}
