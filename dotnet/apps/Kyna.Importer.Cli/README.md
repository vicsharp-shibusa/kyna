# Kyna Importer

The Kyna import utility is a command line interface used to import data from an external API and preserve the results in the `api_transactions` table in the `imports` database.

The `-f <configuration file>` is *required* when not showing help.
The configuration file must be JSON.

You can use `--dry-run` to get a sense of what your configuration file will do.

Use `--info` to see information (if available) about the state of your subscription (different results for different sources).

## Help Text

```
kyna-importer v1

CLI for importing financial data.

-f|--file <configuration file>  JSON import configuration file to process.
[--dry-run]                     Executes a 'dry run' - reports only what the app would do with the specified configuration.
[--info|--show-info]            Show source-specific information.
[-y]                            Accept danger automatically.
[?|-?|-h|--help]                Show this help.
[-v|--verbose]                  Turn on verbose communication.
```

## Sample `secrets.json` File

```json
{
  "ConnectionStrings": {
    "Logs": "User ID=postgres;Password=MY_PASSWORD;Host=127.0.0.1;Port=5432;Database=logs;",
    "Imports": "User ID=postgres;Password=MY_PASSWORD;Host=127.0.0.1;Port=5432;Database=imports;"
  },
  "DatabaseEngines": {
    "Logs": "PostgreSql",
    "Imports": "PostgreSql"
  },
  "ApiKeys": {
    "polygon.io": "MY_KEY"
  },
  "AccessKeys": {
    "polygon.io": "MY_ACCESS_KEY"
  }
}
```

## Sample JSON Configuration Files

The configuration JSON files are source-specific.

### polygon.io

```json
{
  "Import Actions": {
    "Tickers": "stocks,indices,options,crypto",
    "Ticker Details": "stocks",
    "Splits": "stocks",
    "Dividends": "stocks",
    "Flat Files": "true"
  },
  "Options": {
    "Import File Location": "C:\\temp\\importer",
    "Max Parallelization": "10",
    "Import File Prefixes": "us_stocks_sip/day_aggs_v1",
    "Years of Data": "10"
  }
}
```