# Kyna Migrator

The Kyna migrator utility is a command line interface used to migrate data from the `imports.api_transactions` table to the relevant tables in the `financials` database.

The `-f <configuration file>` is *required* when not showing help. The configuration file must be JSON.

You can use `--dry-run` to get a sense of what your configuration file will do without performing any migrations.

Use `--info` to see summary information about your selected configuration file.

## Help Text

```
kyna-migrator v1

CLI for migrating from the imports database to the financials database.

-f|--file <configuration file>  JSON import configuration file to process.
[--dry-run]                     Executes a 'dry run' - reports only what the app would do with the specified configuration.
[--info|--show-info]            Displays summary info from the provided configuration file.
[?|-?|-h|--help]                Show this help.
[-v|--verbose]                  Turn on verbose communication.
```

## Sample JSON Configuration Files

```json
{
  "Source": "polygon.io",
  "Categories": [
    "Ticker Details",
    "Splits",
    "Dividends",
    "Flat Files"
  ],
  "Mode": "Latest",
  "Source Deletion Mode": "All Except Latest",
  "Max Parallelization": 1,
  "Import File Location": "C:\\temp",
  "Import File Prefixes": [
    "us_stocks_sip/day_aggs_v1"
  ]
}
```

In the above example, `Categories` correspond to the categories of the relevant source and the `category` column in the `imports.api_transactions` table.

`Mode` can be either `Rolling` or `Latest`.
`Rolling` will migrate all relevant records in the order in which they were inserted into the `imports.api_transactions` table.
`Latest` will migrate the latest records for each `category`/`sub_category` combination in the `imports.api_transactions` table.

`Source Deletion Mode` refers to the rules for deleting records from the `imports.api_transactions` table.
Excluding this item or using a value of `None` will prevent any records from being deleted.
A value of `All` will delete all records for the `source`/`category`/`sub_category` combos specified.
`All Except Latest` will delete all all records for the `source`/`category`/`sub_category` combos specified except the most recent.

`Price Migration Mode` refers to whether you want the raw data, split-adjusted data, or both.

`MaxParallelization` sets the maximum number of concurrent threads to be used during migration. A value less than '2' will result in no parallelization.