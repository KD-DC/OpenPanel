using System.Text.Json;

namespace OpenPanel.Host.Messaging;

public sealed record UiToHostMessage(string Type, JsonElement? Payload);
