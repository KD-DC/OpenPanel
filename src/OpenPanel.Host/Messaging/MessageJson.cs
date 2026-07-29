using System.Text.Json;

namespace OpenPanel.Host.Messaging;

public static class MessageJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };
}
