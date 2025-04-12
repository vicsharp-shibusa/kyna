# Kyna Backtests

The Kyna backtests utility is a command line interface for executing back-testing scenarios.

The `-f <configuration file>` argument is *required* when not showing help. The configuration file must be JSON.

## Help Text

```
kyna-backtest v1

CLI for importing financial data.

[-i|--input-dir <directory>]            Directory of JSON import configuration files to process.
[-f|--file <configuration file>]        JSON import configuration file to process.
[-l|--list]                             List process identifiers.
[-d|--delete <process id>]              Delete backtest, results, and stats for specified process id.
[?|-?|-h|--help]                        Show this help.
[-v|--verbose]                          Turn on verbose communication.
```

## Sample JSON Configuration Files

```json
{
  "Type": "Random Baseline",
  "Name": "Baseline 1",
  "Source": "polygon.io",
  "Description": "Testing",
  "EntryPricePoint": "Close",
  "TargetUp": {
    "PricePoint": "High",
    "Value": 0.1
  },
  "TargetDown": {
    "PricePoint": "Low",
    "Value": 0.1
  }
}
```

In the above example, a "random baseline" is defined.

The `Name` and `Description` values are somewhat arbitrary.

`Source` will constrain the data collection (in the `financials` database) to the specified source.

`EntryPricePoint` defines the price on the OHLC record to be used as the entry point for a trade.

`TargetUp` and `TargetDown` represent the competing scenarios - which will occur first after the entry point.
On either, the `PricePoint` is the price on the OHLC used in the determination and the `Value` represents a percentage change.

`TargetUp` is determined by the entry price multiplied by 1 plus the `Value`.
`TargetDown` is determined by the entry price mulitplied by 1 minus the `Value`.

In the example above, the backtester will identify whether the stock first goes up 10% or down 10% after the entry point.