using System.Text.Json;
using System.Text.Json.Serialization;
using Frouros.Proxy.Models.Web;

namespace Frouros.Proxy.Models.Serialization.Converters;

public class ModuleActivationJsonConverter : JsonConverter<ModuleActivation>
{
    public override ModuleActivation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "active" => ModuleActivation.Enabled,
            _ => ModuleActivation.Disabled
        };
    }

    public override void Write(Utf8JsonWriter writer, ModuleActivation value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            ModuleActivation.Enabled => "active",
            _ => "inactive"
        });
    }
}