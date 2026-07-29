using OpenPanel.Host.Models;

namespace OpenPanel.Host.Messaging;

public sealed record HostToUiMessage(string Type, DashboardState Payload);
