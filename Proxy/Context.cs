using System.Text.Json;
using System.Text.Json.Serialization;

namespace FireflySR.Proxy;

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip
)]
[JsonSerializable(typeof(ProxyConfig))]
internal partial class ProxyConfigContext : JsonSerializerContext
{}