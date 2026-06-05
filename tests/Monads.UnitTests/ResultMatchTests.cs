namespace MaVe.Monads.UnitTests;

[TestFixture]
public class ResultMatchTests
{
    private static Error TestError => Error.Create("Failed");

    [Test]
    public void Match_ShouldReturnSuccessBranch_WhenSuccess()
    {
        var result = Result.Success("hello");

        var value = result.Match(onSuccess: data => data.Length, onFailure: _ => -1);

        Assert.That(value, Is.EqualTo(5));
    }

    [Test]
    public void Match_ShouldReturnFailureBranch_WhenFailure()
    {
        var result = Result.Failure<string>(TestError);

        var value = result.Match(onSuccess: data => data.Length, onFailure: _ => -1);

        Assert.That(value, Is.EqualTo(-1));
    }

    [Test]
    public async Task MatchAsync_ShouldReturnSuccessBranch_WhenSuccess()
    {
        var result = Result.Success("hello");

        var value = await result.MatchAsync(onSuccess: data => Task.FromResult(data.Length), onFailure: _ => Task.FromResult(-1));

        Assert.That(value, Is.EqualTo(5));
    }

    [Test]
    public async Task MatchAsync_ShouldReturnFailureBranch_WhenFailure()
    {
        var result = Result.Failure<string>(TestError);

        var value = await result.MatchAsync(onSuccess: data => Task.FromResult(data.Length), onFailure: _ => Task.FromResult(-1));

        Assert.That(value, Is.EqualTo(-1));
    }

    [Test]
    public async Task MatchAsync_WithCancellationToken_ShouldPassToken()
    {
        var result = Result.Success("hello");
        using var tokenSource = new CancellationTokenSource();
        await tokenSource.CancelAsync();
        var tokenObserved = false;

        _ = await result.MatchAsync(
            onSuccess: (_, ct) =>
            {
                tokenObserved = ct.IsCancellationRequested;
                return Task.FromResult(1);
            },
            onFailure: (_, _) => Task.FromResult(-1),
            cancellationToken: tokenSource.Token);

        Assert.That(tokenObserved, Is.True);
    }
}
