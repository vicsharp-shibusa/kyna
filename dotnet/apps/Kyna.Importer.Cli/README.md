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
    "eodhd.com": "MY_KEY",
    "polygon.io": "MY_KEY"
  },
  "AccessKeys": {
    "polygon.io": "MY_ACCESS_KEY"
  }
}
```

## Sample JSON Configuration Files

The configuration JSON files are source-specific.
Currently, only `eodhd.com` is supported.

### eodhd.com API

#### Notes

1. The `Purge` action is **DANGEROUS**. It will remove all `api_transactions` records with the `eodhd.com` source and is intended to be used only for testing or "starting over."
1. The `Import Actions` section is the only *required* section and at least one entry in the `Import Actions` section is required.
1. The keys in both `Exchanges` and `Symbol Types` correspond to the values in the `Import Actions`.
1. The values for the `Exchanges` and `Symbol Types` filter the results; you may need to do your own research on valid values, depending on your selected `Exchanges`.
1. `Max Parallelization` is optional. If it is excluded or its value is less than `2`, the program will run without parallelization.
1. If `Bulk` is specified, only bulk calls will be made. Bulk calls are only made for prices, splits, and dividends.
1. Not all possible `eodhd.com` API endpoints are implemented - some were of little or no interest to me. If you're looking for an endpoint that isn't present, create an issue on the GitHub repo and I'll see what I can do.
1. If the application hits your credit limit, it will log the error and stop processing.

The following snippet is a JSON configuration file used for `eodhd.com`.
All possible `Import Actions` are represented here.

```json
{
  "Import Actions": {
    "Purge": "true",
    "Exchange List": "true",
    "Exchange Symbol List": "US",
    "EOD Prices": "US",
    "Splits": "US",
    "Dividends": "US",
    "Fundamentals": "US",
    "Calendar Earnings": "US",
    "Calendar Ipos": "US",
    "Insider Transactions": "US"
  },
  "Exchanges": {
    "US": "NYSE, NYSE ARCA, NASDAQ"
  },
  "Symbol Types": {
    "US": "Common Stock, ETF"
  },
  "Date Ranges": {
    "EOD Prices": "2023-01-01,2023-12-31",
    "Dividends": "2023-01-01,2023-12-31",
    "Splits": "2023-01-01,2023-12-31",
    "Calendar Earnings": "2023-01-01,2023-12-31",
    "Calendar Ipos": "2023-01-01,2023-12-31",
    "Insider Transactions": "2023-01-01,2023-12-31"
  },
  "Options": {
    "Bulk": "false",
    "Max Parallelization": "5"
  }
}
```

A minimal example:

```json
{
  "Import Actions": {
    "Exchange List": "true",
    "Exchange Symbol List": "US"
  }
}
```

An example for collecting price, splits, and dividends across major US exchanges:

```json
{
  "Import Actions": {
    "Exchange List": "true",
    "Exchange Symbol List": "US",
    "EOD Prices": "US",
    "Splits": "US",
    "Dividends": "US"
  },
  "Exchanges": {
    "US": "NYSE, NYSE ARCA, NASDAQ"
  },
  "Symbol Types": {
    "US": "Common Stock, ETF"
  },
  "Options": {
    "Bulk": "false",
    "Max Parallelization": "10"
  }
}
```

Import configuration files for `polygon.io` are a little different.

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
    "Import File Location": "C:\\temp",
    "Max Parallelization": "10",
    "Import File Prefixes": "us_stocks_sip/day_aggs_v1",
    "Years of Data": "10"
  }
}
```