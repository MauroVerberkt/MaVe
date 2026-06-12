using MaVe.Monads;
using MaVe.Railyard;
using Microsoft.Extensions.DependencyInjection;

namespace MaVe.Railyard.UnitTests;

[TestFixture]
public class DispatchIntegrationTests
{
    [Test]
    public async Task DispatchAsync_WhenOperationExists_ReturnsSerializedOutput()
    {
        var services = new ServiceCollection();
        services.AddRailyard();

        using var serviceProvider = services.BuildServiceProvider();
        var yard = serviceProvider.GetRequiredService<IYard>();

        var result = await yard.DispatchAsync("sync-ping", "{\"Name\":\"World\"}", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.EqualTo("{\"Message\":\"Hello World\"}"));
        });
    }

    [Test]
    public async Task DispatchAsync_WhenOperationNameIsUnknown_ReturnsOperationNotFoundError()
    {
        var services = new ServiceCollection();
        services.AddRailyard();

        using var serviceProvider = services.BuildServiceProvider();
        var yard = serviceProvider.GetRequiredService<IYard>();

        var result = await yard.DispatchAsync("missing-operation", "{}", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error?.Code, Is.EqualTo("RY002"));
        });
    }

}

[Operation("sync-ping", Description = "Integration sync operation")]
public sealed class SyncPingOperation : SyncOperation<SyncPingInput, SyncPingOutput>
{
    protected override Result<SyncPingOutput> Execute(SyncPingInput input)
    {
        return Result.Success(new SyncPingOutput($"Hello {input.Name}"));
    }
}

public sealed record SyncPingInput(string Name);

public sealed record SyncPingOutput(string Message);
