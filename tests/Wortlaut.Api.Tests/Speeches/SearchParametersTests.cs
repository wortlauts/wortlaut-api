using Wortlaut.Api.Speeches;

namespace Wortlaut.Api.Tests.Speeches;

public class SearchParametersTests
{
    [Fact]
    public void WithoutAnyParameter_TheDefaultsApply()
    {
        var created = Parameters().TryCreateRequest(out var request, out var errors);

        Assert.True(created);
        Assert.Empty(errors);
        Assert.Equal(SearchMode.Stem, request!.Mode);
        Assert.Equal(SearchSort.Relevance, request.Sort);
        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
        Assert.Null(request.Query);
    }

    [Fact]
    public void ParametersAreTakenOverIncludingSnakeCaseValues()
    {
        var parameters = Parameters() with
        {
            Query = "Digitalisierung",
            Mode = "PHRASE",
            Sort = "date",
            Reaction = "heckle",
            From = new DateOnly(2026, 1, 1),
            To = new DateOnly(2026, 12, 31),
            Faction = "SPD",
            Speaker = "Muster",
            LegislativePeriod = 21,
            Page = 3,
            PageSize = 10,
        };

        Assert.True(parameters.TryCreateRequest(out var request, out _));

        Assert.Equal(
            new SearchRequest(
                "Digitalisierung", SearchMode.Phrase, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
                "SPD", "Muster", 21, ReactionType.Heckle, SearchSort.Date, 3, 10),
            request);
    }

    [Theory]
    [InlineData("mode", "fuzzzy")]
    [InlineData("sort", "newest")]
    [InlineData("reaction", "cheers")]
    [InlineData("mode", "7")]
    public void UnknownEnumValue_IsReportedWithTheAllowedValues(string parameter, string value)
    {
        var parameters = parameter switch
        {
            "mode" => Parameters() with { Mode = value },
            "sort" => Parameters() with { Sort = value },
            _ => Parameters() with { Reaction = value },
        };

        Assert.False(parameters.TryCreateRequest(out var request, out var errors));

        Assert.Null(request);
        Assert.Contains("Allowed values", Assert.Single(errors[parameter]));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public void PageAndPageSize_MustBeInRange(int page, int pageSize)
    {
        var parameters = Parameters() with { Page = page, PageSize = pageSize };

        Assert.False(parameters.TryCreateRequest(out _, out var errors));

        Assert.Contains(errors.Keys, key => key is "page" or "page_size");
    }

    [Fact]
    public void PagingBeyondTenThousandHits_IsRejected()
    {
        var parameters = Parameters() with { Page = 201, PageSize = 50 };

        Assert.False(parameters.TryCreateRequest(out _, out var errors));

        Assert.Contains("page", errors.Keys);
    }

    [Fact]
    public void FromAfterTo_IsRejected()
    {
        var parameters = Parameters() with { From = new DateOnly(2026, 6, 1), To = new DateOnly(2026, 1, 1) };

        Assert.False(parameters.TryCreateRequest(out _, out var errors));

        Assert.Contains("from", errors.Keys);
    }

    [Fact]
    public void BlankQuery_IsTreatedAsNoQuery()
    {
        Assert.True((Parameters() with { Query = "   " }).TryCreateRequest(out var request, out _));

        Assert.Null(request!.Query);
    }

    private static SearchParameters Parameters() =>
        new(Query: null, Mode: null, From: null, To: null, Faction: null, Speaker: null,
            LegislativePeriod: null, Reaction: null, Sort: null, Page: null, PageSize: null);
}
