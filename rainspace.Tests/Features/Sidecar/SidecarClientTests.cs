using System.Text.Json;
using rainspace.Features.Sidecar.Application;
using rainspace.Features.Sidecar.Contracts;
using rainspace.Features.Sidecar.Infrastructure;

namespace rainspace.Tests.Features.Sidecar;

[TestClass]
public class SidecarClientTests
{
    [TestMethod]
    public async global::System.Threading.Tasks.Task Invoke_NegotiatesCapabilitiesAndCorrelatesResponse()
    {
        var connection = new FakeConnection
        {
            Handshake = new SidecarHandshakeResponse(
                SidecarProtocol.CurrentVersion,
                new[] { "search", "unrequested" },
                "node-test"),
            Send = request => new SidecarWireResponse(
                request.RequestId,
                request.ProtocolVersion,
                true,
                JsonSerializer.SerializeToElement(new { count = 2 }))
        };
        await using var client = new SidecarClient(
            connection,
            new SidecarClientOptions(new[] { "search" }, TimeSpan.FromSeconds(1)));

        SidecarInvocationResult result = await client.InvokeAsync(
            new SidecarInvocation("search.query"));

        Assert.IsTrue(result.Success, result.Error);
        Assert.IsTrue(client.Capabilities.SetEquals(new[] { "search" }));
        Assert.AreEqual(1, connection.ConnectCount);
        Assert.AreEqual(1, connection.SendCount);
        Assert.AreEqual(2, result.Payload?.GetProperty("count").GetInt32());
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Invoke_UnnegotiatedCapability_IsRejectedBeforeTransportSend()
    {
        var connection = new FakeConnection
        {
            Handshake = new SidecarHandshakeResponse(
                SidecarProtocol.CurrentVersion,
                new[] { "search" },
                "node-test")
        };
        await using var client = new SidecarClient(
            connection,
            new SidecarClientOptions(new[] { "search" }, TimeSpan.FromSeconds(1)));

        SidecarInvocationResult result = await client.InvokeAsync(
            new SidecarInvocation("filesystem.read"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SidecarErrorCode.CapabilityDenied, result.ErrorCode);
        Assert.AreEqual(0, connection.SendCount);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Invoke_TransportExceedsDeadline_ReturnsStructuredTimeout()
    {
        var connection = new FakeConnection
        {
            Handshake = new SidecarHandshakeResponse(
                SidecarProtocol.CurrentVersion,
                new[] { "search" },
                "node-test"),
            SendAsyncOverride = async (_, cancellationToken) =>
            {
                await global::System.Threading.Tasks.Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken);
                throw new AssertFailedException("unreachable");
            }
        };
        await using var client = new SidecarClient(
            connection,
            new SidecarClientOptions(new[] { "search" }, TimeSpan.FromMilliseconds(30)));

        SidecarInvocationResult result = await client.InvokeAsync(
            new SidecarInvocation("search.query"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SidecarErrorCode.Timeout, result.ErrorCode);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Invoke_MismatchedRequestId_ReturnsInvalidResponse()
    {
        var connection = new FakeConnection
        {
            Handshake = new SidecarHandshakeResponse(
                SidecarProtocol.CurrentVersion,
                new[] { "search" },
                "node-test"),
            Send = request => new SidecarWireResponse(
                "wrong-id",
                request.ProtocolVersion,
                true)
        };
        await using var client = new SidecarClient(
            connection,
            new SidecarClientOptions(new[] { "search" }, TimeSpan.FromSeconds(1)));

        SidecarInvocationResult result = await client.InvokeAsync(
            new SidecarInvocation("search.query"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SidecarErrorCode.InvalidResponse, result.ErrorCode);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task UnavailableConnection_DoesNotCrashCaller()
    {
        await using var client = new SidecarClient(
            new UnavailableSidecarConnection("not configured"),
            SidecarClientOptions.Disabled);

        SidecarInvocationResult result = await client.InvokeAsync(
            new SidecarInvocation("system.health"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SidecarErrorCode.Unavailable, result.ErrorCode);
        StringAssert.Contains(result.Error, "not configured");
    }

    private sealed class FakeConnection : ISidecarConnection
    {
        public SidecarConnectionState State { get; private set; } = SidecarConnectionState.Disconnected;
        public SidecarHandshakeResponse Handshake { get; init; } = new(
            SidecarProtocol.CurrentVersion,
            Array.Empty<string>(),
            "fake");
        public Func<SidecarWireRequest, SidecarWireResponse>? Send { get; init; }
        public Func<SidecarWireRequest, CancellationToken, Task<SidecarWireResponse>>?
            SendAsyncOverride { get; init; }
        public int ConnectCount { get; private set; }
        public int SendCount { get; private set; }

        public Task<SidecarHandshakeResponse> ConnectAsync(
            SidecarHandshakeRequest request,
            CancellationToken cancellationToken = default)
        {
            ConnectCount++;
            State = SidecarConnectionState.Ready;
            return global::System.Threading.Tasks.Task.FromResult(Handshake);
        }

        public Task<SidecarWireResponse> SendAsync(
            SidecarWireRequest request,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            if (SendAsyncOverride != null)
            {
                return SendAsyncOverride(request, cancellationToken);
            }

            return global::System.Threading.Tasks.Task.FromResult(Send?.Invoke(request)
                ?? new SidecarWireResponse(
                    request.RequestId,
                    request.ProtocolVersion,
                    true));
        }

        public ValueTask DisposeAsync()
        {
            State = SidecarConnectionState.Disconnected;
            return ValueTask.CompletedTask;
        }
    }
}
