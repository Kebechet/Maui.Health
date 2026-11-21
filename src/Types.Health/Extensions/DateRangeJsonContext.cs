using System.Text.Json.Serialization;

namespace Kebechet.Types.Health.Extensions;

// Source-generated rather than reflection-based JSON, so trimmed and NativeAOT apps (a watch app) can use the
// pause intervals. The dictionaries keep the {"Start":ms,"End":ms} format that earlier versions stored.
[JsonSerializable(typeof(List<Dictionary<string, long?>>))]
internal sealed partial class DateRangeJsonContext : JsonSerializerContext;
