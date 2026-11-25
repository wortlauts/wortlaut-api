using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Wortlaut.Api.Tests.Contracts;

public sealed class OpenApiContractTests : IDisposable
{
    private const string UpdateVariable = "UPDATE_OPENAPI";
    private const string SolutionFile = "Wortlaut.Api.slnx";

    private readonly WebApplicationFactory<Program> factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task TheOpenApiDocument_MatchesTheCommittedContract()
    {
        var generated = await GenerateAsync();
        var contractPath = Path.Combine(RepositoryRoot(), "openapi.json");

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(contractPath, generated.ToJsonString(Indented) + "\n", TestContext.Current.CancellationToken);
        }

        Assert.True(File.Exists(contractPath), $"openapi.json is missing. Run the tests once with {UpdateVariable}=1 to create it.");
        var committed = JsonNode.Parse(await File.ReadAllTextAsync(contractPath, TestContext.Current.CancellationToken))!;
        Assert.True(
            JsonNode.DeepEquals(generated, committed),
            $"The API contract changed. Review the change and run the tests with {UpdateVariable}=1 to update openapi.json.");
    }

    [Fact]
    public async Task TheContract_ListsTheDocumentedEndpoints()
    {
        var paths = (await GenerateAsync())["paths"]!.AsObject().Select(path => path.Key);

        Assert.Equal(
            ["/v1/speakers/suggest", "/v1/speeches/search", "/v1/speeches/{id}", "/v1/trends"],
            paths.Order());
    }

    [Fact]
    public async Task QueryParameters_AreLowercaseWithUnderscores()
    {
        var document = await GenerateAsync();

        var names = document["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject())
            .SelectMany(operation => operation.Value!["parameters"]?.AsArray() ?? [])
            .Select(parameter => parameter!["name"]!.GetValue<string>());

        Assert.All(names, name => Assert.Matches("^[a-z_]+$", name));
    }

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private async Task<JsonNode> GenerateAsync()
    {
        using var client = factory.CreateClient();
        var document = JsonNode.Parse(await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken))!;
        document.AsObject().Remove("servers");
        return document;
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles(SolutionFile).Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException($"{SolutionFile} was not found above the test output.");
    }
}
