using System.Text.Json;

namespace raindesk.Features.Sidecar.Contracts;

public static class SidecarProtocol
{
    public const int CurrentVersion = 1;
}

public enum SidecarConnectionState
{
    Disconnected,
    Connecting,
    Ready,
    Faulted
}

public enum SidecarErrorCode
{
    None,
    InvalidRequest,
    CapabilityDenied,
    ProtocolMismatch,
    Timeout,
    Cancelled,
    Unavailable,
    RemoteError,
    InvalidResponse
}

public sealed record SidecarClientOptions(
    IReadOnlyList<string> RequestedCapabilities,
    TimeSpan DefaultTimeout)
{
    public static SidecarClientOptions Disabled { get; } =
        new(Array.Empty<string>(), TimeSpan.FromSeconds(5));
}

public sealed record SidecarHandshakeRequest(
    int ProtocolVersion,
    IReadOnlyList<string> RequestedCapabilities);

public sealed record SidecarHandshakeResponse(
    int ProtocolVersion,
    IReadOnlyList<string> Capabilities,
    string RuntimeVersion);

public sealed record SidecarWireRequest(
    string RequestId,
    int ProtocolVersion,
    string Method,
    JsonElement? Payload = null);

public sealed record SidecarWireError(string Code, string Message);

public sealed record SidecarWireResponse(
    string RequestId,
    int ProtocolVersion,
    bool Success,
    JsonElement? Payload = null,
    SidecarWireError? Error = null);

public sealed record SidecarInvocation(
    string Method,
    JsonElement? Payload = null,
    TimeSpan? Timeout = null);

public sealed record SidecarInvocationResult(
    bool Success,
    JsonElement? Payload = null,
    SidecarErrorCode ErrorCode = SidecarErrorCode.None,
    string? Error = null)
{
    public static SidecarInvocationResult Failed(SidecarErrorCode code, string error) =>
        new(false, ErrorCode: code, Error: error);
}

public sealed record SidecarHealth(
    bool Available,
    SidecarConnectionState State,
    TimeSpan Latency,
    string? RuntimeVersion = null,
    string? Error = null);

public interface ISidecarConnection : IAsyncDisposable
{
    SidecarConnectionState State { get; }

    Task<SidecarHandshakeResponse> ConnectAsync(
        SidecarHandshakeRequest request,
        CancellationToken cancellationToken = default);

    Task<SidecarWireResponse> SendAsync(
        SidecarWireRequest request,
        CancellationToken cancellationToken = default);
}

public interface ISidecarClient : IAsyncDisposable
{
    SidecarConnectionState State { get; }

    IReadOnlySet<string> Capabilities { get; }

    Task<SidecarInvocationResult> InvokeAsync(
        SidecarInvocation invocation,
        CancellationToken cancellationToken = default);

    Task<SidecarHealth> CheckHealthAsync(CancellationToken cancellationToken = default);
}
