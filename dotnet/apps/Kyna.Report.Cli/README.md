# Kyna Reports

The Kyna report utility is a command line interface for generating reports from the backtests database.

The `-o <output directory>` argument is *required* when not showing help.
The output directory will be created if it does not exist.

The `-p <process id>` argument is required.
You can retrieve a list of process ids using the `--list` argument.

Use `-d <process id>` to purge unwanted data from the back-testing database.

A type of report is required (e.g., `--stats`, `--splits`, etc. etc.).

## Help Text

```
kyna-report v1

CLI for generating reports from Kyna data sources.

-o|--output|--output-dir <output directory>     Set (or create) output directory.
-p|--process|--process-id <process id>          Filter report by specified process id.
[--stats]                                       Generate the backtesting stats report.
[--splits]                                      Generate a report comparing splits between data providers.
[--compare-charts]                              Generate a report that compares adjusted charts between data sources.
[-l|--list]                                     List process identifiers.
[-d|--delete <process id>]                      Delete backtest, results, and stats for specified process id.
[?|-?|-h|--help]                                Show this help.
[-v|--verbose]                                  Turn on verbose communication.
```
