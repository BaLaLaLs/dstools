using System.Collections.Generic;
using System.Text.Json.Serialization;
using dstools.ViewModels;

namespace dstools.Models;

public class PullModelRequest
{
    [JsonPropertyName("name")]
    public string Model { get; set; } = string.Empty;
}
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(DeleteModelRequest))]
[JsonSerializable(typeof(PullModelRequest))]
[JsonSerializable(typeof(PullModelResponse))]
[JsonSerializable(typeof(TagsResponse))]
[JsonSerializable(typeof(List<ModelInfo>))]
[JsonSerializable(typeof(ModelInfo))]
[JsonSerializable(typeof(DeleteModelRequest))]
partial class OllamaJsonContext : JsonSerializerContext
{
}

public class TagsResponse
{
    [JsonPropertyName("models")] public List<ModelInfo> Models { get; set; } = new();
}

public class ModelInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;

    [JsonPropertyName("modified_at")] public string ModifiedAt { get; set; } = string.Empty;

    [JsonPropertyName("size")] public long Size { get; set; }

    [JsonPropertyName("digest")] public string Digest { get; set; } = string.Empty;
}

public class DeleteModelRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
}

public class PullModelResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("total")]
    public long Total { get; set; }

    [JsonPropertyName("downloaded")]
    public long Downloaded { get; set; }
}