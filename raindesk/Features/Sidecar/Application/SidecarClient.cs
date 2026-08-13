using System.Diagnostics;
using raindesk.Features.Sidecar.Contracts;

namespace raindesk.Features.Sidecar.Application;

public sealed class SidecarClient : ISidecarClient, IDisposable
{
    private readonly ISidecarConnection _connection;
    private readonly SidecarClientOptions _options;
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly HashSet<string> _capabilities = new(StringComparer.OrdinalIgnoreCase);
    private bool _connected;
    private string? _runtimeVersion;
    private bool _disposed;

    public SidecarClient(ISidecarConnection connection, SidecarClientOptions options)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.DefaultTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Sidecar 默认超时必须大于 0。");
        }
    }

    public SidecarConnectionState State => _connection.State;

    public IReadOnlySet<string> Capabilities => _capabilities;

    public async Task<SidecarInvocationResult> InvokeAsync(
        SidecarInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(invocation);
        if (!IsValidMethod(invocation.Method))
        {
            return SidecarInvocationResult.Failed(
                SidecarErrorCode.InvalidRequest,
                "Sidecar 方法名必须采用 capability.method 格式。");
        }

        TimeSpan timeout = invocation.Timeout ?? _options.DefaultTimeout;
        if (timeout <= TimeSpan.Zero)
        {
            return SidecarInvocationResult.Failed(
                SidecarErrorCode.InvalidRequest,
                "Sidecar 调用超时必须大于 0。");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            SidecarInvocationResult? connectionFailure =
                await EnsureConnectedAsync(timeoutSource.Token).ConfigureAwait(false);
            if (connectionFailure != null)
            {
                return connectionFailure;
            }

            string capability = invocation.Method.Split('.', 2)[0];
            if (!capability.Equals("system", StringComparison.OrdinalIgnoreCase)
                && !_capabilities.Contains(capability))
            {
                return SidecarInvocationResult.Failed(
                    SidecarErrorCode.CapabilityDenied,
                    $"Sidecar 未协商能力: {capability}");
            }

            string requestId = Guid.NewGuid().ToString("N");
            var request = new SidecarWireRequest(
                requestId,
                SidecarProtocol.CurrentVersion,
                invocation.Method,
                invocation.Payload);
            SidecarWireResponse response = await _connection
                .SendAsync(request, timeoutSource.Token)
                .ConfigureAwait(false);

            if (!string.Equals(response.RequestId, requestId, StringComparison.Ordinal)
                || response.ProtocolVersion != SidecarProtocol.CurrentVersion)
            {
                return SidecarInvocationResult.Failed(
                    SidecarErrorCode.InvalidResponse,
                    "Sidecar 响应 request id 或协议版本不匹配。");
            }

            return response.Success
                ? new SidecarInvocationResult(true, response.Payload)
                : SidecarInvocationResult.Failed(
                    SidecarErrorCode.RemoteError,
                    response.Error?.Message ?? "Sidecar 返回未知错误。");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return SidecarInvocationResult.Failed(SidecarErrorCode.Cancelled, "Sidecar 调用已取消。");
        }
        catch (OperationCanceledException)
        {
            return SidecarInvocationResult.Failed(SidecarErrorCode.Timeout, "Sidecar 调用超时。");
        }
        catch (Exception ex)
        {
            _connected = false;
            return SidecarInvocationResult.Failed(SidecarErrorCode.Unavailable, ex.Message);
        }
    }

    public async Task<SidecarHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        SidecarInvocationResult result = await InvokeAsync(
            new SidecarInvocation("system.health"),
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        return new SidecarHealth(
            result.Success,
            State,
            stopwatch.Elapsed,
            _runtimeVersion,
            result.Error);
    }

    private async Task<SidecarInvocationResult?> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connected && _connection.State == SidecarConnectionState.Ready)
        {
            return null;
        }

        await _connectGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connected && _connection.State == SidecarConnectionState.Ready)
            {
                return null;
            }

            SidecarHandshakeResponse handshake = await _connection.ConnectAsync(
                new SidecarHandshakeRequest(
                    SidecarProtocol.CurrentVersion,
                    _options.RequestedCapabilities),
                cancellationToken).ConfigureAwait(false);
            if (handshake.ProtocolVersion != SidecarProtocol.CurrentVersion)
            {
                return SidecarInvocationResult.Failed(
                    SidecarErrorCode.ProtocolMismatch,
                    $"Sidecar 协议版本 {handshake.ProtocolVersion} 与主程序不兼容。");
            }

            var requested = new HashSet<string>(
                _options.RequestedCapabilities,
                StringComparer.OrdinalIgnoreCase);
            _capabilities.Clear();
            foreach (string capability in handshake.Capabilities
                         .Where(requested.Contains)
                         .Where(IsValidCapability))
            {
                _capabilities.Add(capability);
            }

            _runtimeVersion = handshake.RuntimeVersion;
            _connected = true;
            return null;
        }
        finally
        {
            _connectGate.Release();
        }
    }

    private static bool IsValidMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method) || method.Length > 128)
        {
            return false;
        }

        string[] segments = method.Split('.');
        return segments.Length >= 2 && segments.All(IsValidCapability);
    }

    private static bool IsValidCapability(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 64
        && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _connectGate.Dispose();
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
