using System.Net;

namespace Wortlaut.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class HealthTests(ApiFixture api)
{
    [Fact]
    public async Task WithAReachableIndex_TheApiIsHealthy()
    {
        using var response = await api.Api.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
