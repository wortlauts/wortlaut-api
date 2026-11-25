# Contracts

`index-template.json` is the Elasticsearch index template published by [wortlaut-indexer](https://github.com/wortlauts/wortlaut-indexer) (`schema/index-template.json`). The API pins one version of it by copying the file here; it is not shared as code.

| Pinned from | Indexer commit |
| --- | --- |
| `schema/index-template.json` | `06ca70b` |

The contract tests start Elasticsearch with this template and check that

- the seed data of the integration tests fits the strict mapping,
- every field the API reads from a speech is mapped,
- every field the API queries is mapped, including the sub-field `text.exact`.

To move to a newer template, copy the file from the indexer, update the commit above and run the tests. A failing test means the API has to change as well.
