namespace MaVe.Monads.UnitTests;

[TestFixture]
public class ResultMatchTests
{
    private static Error TestError => Error.Create("Failed");

    [Test]
    public void Match_ShouldReturnSuccessBranch_WhenSuccess()
    {
        var result = Result.Success("hello");

        var value = result.Match(data => data.Length, _ => -1);

        Assert.That(value, Is.EqualTo(5));
    }

    [Test]
    public void Match_ShouldReturnFailureBranch_WhenFailure()
    {
        var result = Result.Failure<string>(TestError);

        var value = result.Match(data => data.Length, _ => -1);

        Assert.That(value, Is.EqualTo(-1));
    }

    [Test]
    public async Task MatchAsync_ShouldReturnSuccessBranch_WhenSuccess()
    {
        var result = Result.Success("hello");

        var value = await result.MatchAsync(data => Task.FromResult(data.Length), _ => Task.FromResult(-1));

        Assert.That(value, Is.EqualTo(5));
    }

    [Test]
    public async Task MatchAsync_ShouldReturnFailureBranch_WhenFailure()
    {
        var result = Result.Failure<string>(TestError);

        var value = await result.MatchAsync(data => Task.FromResult(data.Length), _ => Task.FromResult(-1));

        Assert.That(value, Is.EqualTo(-1));
    }

    [Test]
    public async Task MatchAsync_WithCancellationToken_ShouldPassToken()
    {
        var result = Result.Success("hello");
        using var tokenSource = new CancellationTokenSource();
        var tokenObserved = CancellationToken.None;

        _ = await result.MatchAsync(
            (_, ct) =>
            {
                tokenObserved = ct;
                return Task.FromResult(1);
            },
            (_, _) => Task.FromResult(-1),
            tokenSource.Token);

        Assert.That(tokenObserved, Is.EqualTo(tokenSource.Token));
    }

    [Test]
    public void MatchAsync_WithCancellationToken_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
    {
        var result = Result.Success("hello");
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            _ = await result.MatchAsync(
                (_, _) => Task.FromResult(1),
                (_, _) => Task.FromResult(-1),
                tokenSource.Token));
    }

    [Test]
    public void MatchAsync_WithCancellationToken_ShouldThrowOperationCanceledException_WhenTokenIsCancelled_AndResultIsFailure()
    {
        var result = Result.Failure<string>(TestError);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            _ = await result.MatchAsync(
                (_, _) => Task.FromResult(1),
                (_, _) => Task.FromResult(-1),
                tokenSource.Token));
    }
}
