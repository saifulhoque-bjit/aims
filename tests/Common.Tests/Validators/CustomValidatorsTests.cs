#region using

using BuildingBlocks.Validators;
using FluentValidation;
using Xunit;

#endregion

namespace Common.Tests.Validators;

public class CustomValidatorsTests
{
    #region Fields, Properties and Indexers

    public class Model
    {
        public IList<string> Items { get; set; } = default!;
    }

    private class MustHaveValidator : AbstractValidator<Model>
    {
        public MustHaveValidator(int num)
        {
            RuleFor(x => x.Items).MustHave(num);
        }
    }

    #endregion

    #region Implementations

    [Fact]
    public void MustHave_EmptyList_FailsWhenOneItemIsRequired()
    {
        var result = new MustHaveValidator(1).Validate(new Model { Items = new List<string>() });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Items" && e.ErrorMessage == "'Items' must have at least 1 item(s).");
    }

    [Fact]
    public void MustHave_NullList_Fails()
    {
        var result = new MustHaveValidator(1).Validate(new Model { Items = null! });

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Theory]
    [InlineData(3, 2)]
    [InlineData(2, 0)]
    public void MustHave_FewerItemsThanRequired_Fails(int num, int count)
    {
        var items = Enumerable.Range(0, count).Select(i => i.ToString()).ToList();

        var result = new MustHaveValidator(num).Validate(new Model { Items = items });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == $"'Items' must have at least {num} item(s).");
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(2, 5)]
    [InlineData(0, 0)]
    public void MustHave_EnoughItems_Passes(int num, int count)
    {
        var items = Enumerable.Range(0, count).Select(i => i.ToString()).ToList();

        var result = new MustHaveValidator(num).Validate(new Model { Items = items });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void MustHave_NegativeNum_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MustHaveValidator(-1));
    }

    #endregion
}
