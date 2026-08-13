using System.Diagnostics;
using System.Text.Json;
using raindesk.Features.Sidecar.Contracts;

namespace raindesk.Features.Sidecar.Infrastructure;

public sealed record NodeSidecarProcessOptions(
    string NodeExecutablePath,
    string EntryScriptPath,
    string? WorkingDirectory = null,
    IReadOnlyList<string>? Arguments = null,
    int MaxRestarts = 2);

/// <summary>
/// 一行一个 JSON 消息的 Node 进程适配器。请求串行化，任何超时都会终止进程，
/// 防止迟到响应污染下一次调用；下一次连接可在重启上限内重新拉起。
/// </summary>
public sealed class NodeProcessSidecarConnection : ISidecarConnection
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NodeSidecarProcessOptions _options;
    private readonly SemaphoreSlim _exchangeGate = new(1, 1);
    private Process? _process;
    private int _restartCount;
    private bool _disposed;

    public NodeProcessSidecarConnection(NodeSidecarProcessOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.NodeExecutablePath)
            || string.IsNullOrWhiteSpace(options.EntryScriptPath))
        {
            throw new ArgumentException("Node 可执行文件和 sidecar 入口脚本不能为空。", nameof(options));
        }
        if (options.MaxRestarts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "重启次数不能小于 0。");
        }
    }

    public SidecarConnectionState State { get; private set; } = SidecarConnectionState.Disconnected;

    public async Task<SidecarHandshakeResponse> ConnectAsync(
        SidecarHandshakeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        State = SidecarConnectionState.Connecting;
        try
        {
            JsonElement payload = JsonSerializer.SerializeToElement(request, JsonOptions);
            SidecarWireResponse response = await ExchangeAsync(
                new SidecarWireRequest(
                    Guid.NewGuid().ToString("N"),
                    request.ProtocolVersion,
                    "system.handshake",
                    payload),
                cancellationToken).ConfigureAwait(false);
            if (!response.Success || response.Payload == null)
            {
                throw new InvalidOperationException(
                    response.Error?.Message ?? "Node sidecar 握手失败。");
            }

            SidecarHandshakeResponse handshake = response.Payload.Value
                .Deserialize<SidecarHandshakeResponse>(JsonOptions)
                ?? throw new JsonException("Node sidecar 握手响应为空。");
            State = SidecarConnectionState.Ready;
            return handshake;
        }
        catch
        {
            State = SidecarConnectionState.Faulted;
            StopProcess();
            throw;
        }
    }

    public async Task<SidecarWireResponse> SendAsync(
        SidecarWireRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (State != SidecarConnectionState.Ready)
        {
            throw new InvalidOperationException("Node sidecar 尚未完成握手。");
        }

        try
        {
            return await ExchangeAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            State = SidecarConnectionState.Faulted;
            StopProcess();
            throw;
        }
    }

    private async Task<SidecarWireResponse> ExchangeAsync(
        SidecarWireRequest request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _exchangeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureProcessStarted();
            string requestJson = JsonSerializer.Serialize(request, JsonOptions);
            await _process!.StandardInput.WriteLineAsync(requestJson.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
            await _process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);

            string? line = await _process.StandardOutput.ReadLineAsync(cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new EndOfStreamException("Node sidecar 未返回响应。");
            }

            SidecarWireResponse response = JsonSerializer.Deserialize<SidecarWireResponse>(line, JsonOptions)
                ?? throw new JsonException("Node sidecar 响应为空。");
            if (!string.Equals(response.RequestId, request.RequestId, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Node sidecar 响应 request id 不匹配。");
            }
            return response;
        }
        finally
        {
            _exchangeGate.Release();
        }
    }

    private void EnsureProcessStarted()
    {
        if (_process is { HasExited: false })
        {
            return;
        }

        if (_process != null)
        {
            _process.Dispose();
            _process = null;
            _restartCount++;
        }
        if (_restartCount > _options.MaxRestarts)
        {
            throw new InvalidOperationException("Node sidecar 已超过允许的重启次数。");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.NodeExecutablePath,
            WorkingDirectory = string.IsNullOrWhiteSpace(_options.WorkingDirectory)
                ? Path.GetDirectoryName(_options.EntryScriptPath) ?? AppContext.BaseDirectory
                : _options.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(_options.EntryScriptPath);
        foreach (string argument in _options.Arguments ?? Array.Empty<string>())
        {
            startInfo.ArgumentList.Add(argument);
        }

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!_process.Start())
        {
            _process.Dispose();
            _process = null;
            throw new InvalidOperationException("无法启动 Node sidecar 进程。");
        }

        _ = DrainStandardErrorAsync(_process);
    }

    private static async global::System.Threading.Tasks.Task DrainStandardErrorAsync(Process process)
    {
        try
        {
            while (await process.StandardError.ReadLineAsync().ConfigureAwait(false) != null)
            {
                // 先保证管道不阻塞；正式诊断适配器可在 composition root 注入日志 sink。
            }
        }
        catch
        {
        }
    }

    private void StopProcess()
    {
        Process? process = _process;
        _process = null;
        if (process == null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(2000);
            }
        }
        catch
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        State = SidecarConnectionState.Disconnected;
        StopProcess();
        _exchangeGate.Dispose();
        return ValueTask.CompletedTask;
    }
}
