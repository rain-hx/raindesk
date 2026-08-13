using raindesk.Features.Sidecar.Contracts;

namespace raindesk.Features.Sidecar.Infrastructure;

public sealed class UnavailableSidecarConnection : ISidecarConnection
{
    private readonly string _reason;

    public UnavailableSidecarConnection(string reason)
    {
        _reason = string.IsNullOrWhiteSpace(reason) ? "Node sidecar 未配置。" : reason;
    }

    public SidecarConnectionState State => SidecarConnectionState.Disconnected;

    public Task<SidecarHandshakeResponse> ConnectAsync(
        SidecarHandshakeRequest request,
        CancellationToken cancellationToken = default) =>
        global::System.Threading.Tasks.Task.FromException<SidecarHandshakeResponse>(
            new InvalidOperationException(_reason));

    public Task<SidecarWireResponse> SendAsync(
        SidecarWireRequest request,
        CancellationToken cancellationToken = default) =>
        global::System.Threading.Tasks.Task.FromException<SidecarWireResponse>(
            new InvalidOperationException(_reason));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
