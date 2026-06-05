namespace MaVe.Monads.UnitTests;

[TestFixture]
public class ResultLinqTests
{
    private static Error TestError => Error.Create("Failed");

    [Test]
    public void Select_ShouldMapData_WhenSuccess()
    {
        var result = Result.Success("hello");

        var mapped = result.Select(data => data.Length);

        Assert.Multiple(() =>
        {
            Assert.That(mapped.IsSuccess, Is.True);
            Assert.That(mapped.Data, Is.EqualTo(5));
        });
    }

    [Test]
    public void SelectMany_ShouldCompose_WhenBothSuccess()
    {
        var a = Result.Success(3);
        var b = Result.Success(7);

        var query = from x in a
                    from y in b
                    select x + y;

        Assert.Multiple(() =>
        {
            Assert.That(query.IsSuccess, Is.True);
            Assert.That(query.Data, Is.EqualTo(10));
        });
    }

    [Test]
    public void SelectMany_ShouldPropagateOuterFailure_WhenOuterFails()
    {
        var outerError = Error.Create("outer");
        var a = Result.Failure<int>(outerError);

        var query = from x in a
                    from y in Result.Success(7)
                    select x + y;

        Assert.Multiple(() =>
        {
            Assert.That(query.IsFailure, Is.True);
            Assert.That(query.Error, Is.EqualTo(outerError));
        });
    }

    [Test]
    public void SelectMany_ShouldPropagateInnerFailure_WhenInnerFails()
    {
        var innerError = Error.Create("inner");
        var a = Result.Success(3);

        var query = from x in a
                    from y in Result.Failure<int>(innerError)
                    select x + y;

        Assert.Multiple(() =>
        {
            Assert.That(query.IsFailure, Is.True);
            Assert.That(query.Error, Is.EqualTo(innerError));
        });
    }
}
