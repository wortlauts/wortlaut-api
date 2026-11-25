# wortlaut-api

Part of [Wortlaut](https://github.com/wortlauts/wortlaut), a search engine over German Bundestag speeches.

A read-only REST API on top of the speeches that [wortlaut-indexer](https://github.com/wortlauts/wortlaut-indexer) writes to Elasticsearch. It reads through the alias `speeches` and never writes. The frontend talks only to this API.

## Requirements

- .NET 10 SDK
- Docker, for the integration tests and the local Elasticsearch

## Run locally

Start Elasticsearch with the stack from [wortlaut-infra](https://github.com/wortlauts/wortlaut-infra) and import some speeches with the indexer, then:

```powershell
dotnet run --project src/Wortlaut.Api
```

| URL | Content |
| --- | --- |
| `/swagger` | Swagger UI |
| `/openapi/v1.json` | OpenAPI document |
| `/health` | health check, reflects the state of Elasticsearch |

Example:

```powershell
curl "http://localhost:5000/v1/speeches/search?q=Digitalisierung&faction=SPD&from=2026-01-01"
```

## Endpoints (v1)

| Endpoint | Purpose |
| --- | --- |
| `GET /v1/speeches/search` | Search speeches. Parameters: `q`, `mode` (`stem`, `phrase`, `fuzzy`), `from`, `to`, `faction`, `speaker`, `legislative_period`, `reaction` (`applause`, `laughter`, `heckle`, `objection`), `sort` (`relevance`, `date`), `page`, `page_size`. Returns hits with highlights and facet counts. |
| `GET /v1/speeches/{id}` | One speech with its full text, reactions, source and rights. |
| `GET /v1/trends` | Number of speeches containing a term per `bucket` (`year`, `legislative_period`), with `normalize=true` as share of all speeches in the bucket. |
| `GET /v1/speakers/suggest` | Autocomplete for speakers by the beginning of a name. |

Invalid parameters are answered with `400` and a problem document that names the parameter. If Elasticsearch is not reachable, the API answers `503` without internal details.

Breaking changes go to `/v2`; additive changes stay in `/v1`.

## Configuration

| Setting | Default | Meaning |
| --- | --- | --- |
| `Elasticsearch:Url` | `http://localhost:9200` | Elasticsearch base URL |
| `Elasticsearch:Alias` | `speeches` | alias to read from |
| `Elasticsearch:Username`, `Elasticsearch:Password` | none | credentials of a read-only user, if security is enabled |

Settings are read from `appsettings.json`, then from environment variables (for example `Elasticsearch__Url`). Credentials never belong into the repository.

## Contract

`openapi.json` in the repository root is the contract of the API. A test compares it with the document the API generates and fails when they differ. After an intended change, regenerate it:

```powershell
$env:UPDATE_OPENAPI = "1"; dotnet test; Remove-Item Env:UPDATE_OPENAPI
```

CI publishes the file as an artifact; the frontend generates its client from it.

The Elasticsearch index template the API depends on is pinned in [contracts](contracts).

## Development

```powershell
dotnet build
dotnet test
```

The integration tests start Elasticsearch with Testcontainers and carry the trait `Category=Integration`.

```text
src/Wortlaut.Api/
  Speeches/               search and speech detail
  Trends/                 term trends
  Speakers/               speaker suggestions
  Elasticsearch/          access to the index, health check, error handling
  Http/                   JSON settings and parameter validation
contracts/                pinned Elasticsearch index template
tests/                    unit, integration and contract tests
openapi.json              API contract
```

## Container image

```powershell
docker build -t wortlaut-api .
docker run --rm -p 8080:8080 -e Elasticsearch__Url=http://host.docker.internal:9200 wortlaut-api
```

CI builds the image and pushes it to `ghcr.io/wortlauts/wortlaut-api` on every push to `main`.
