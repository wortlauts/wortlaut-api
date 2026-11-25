using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Wortlaut.Api.Elasticsearch;
using Wortlaut.Api.Http;
using Wortlaut.Api.Speakers;
using Wortlaut.Api.Speeches;
using Wortlaut.Api.Trends;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EsOptions>(builder.Configuration.GetSection(EsOptions.SectionName));
builder.Services.ConfigureHttpJsonOptions(options => ApiJson.Configure(options.SerializerOptions));

builder.Services.AddHttpClient<ISpeechIndex, EsSpeechIndex>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<EsOptions>>().Value;
    client.BaseAddress = options.Url;
    if (options.Username is not null)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }
});

builder.Services.AddScoped<SpeechService>();
builder.Services.AddScoped<TrendService>();
builder.Services.AddScoped<SpeakerService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SpeechIndexExceptionHandler>();
builder.Services.AddHealthChecks().AddCheck<EsHealthCheck>("elasticsearch");
builder.Services.AddOpenApi("v1", options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Wortlaut API";
        document.Info.Description = "Search over the speeches of the German Bundestag.";
        return Task.CompletedTask;
    }));

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Wortlaut API v1"));
app.MapHealthChecks("/health");

var v1 = app.MapGroup("/v1");
v1.MapSpeechEndpoints();
v1.MapTrendEndpoints();
v1.MapSpeakerEndpoints();

app.Run();

public partial class Program;
