# Kyna

`kyna` is a command and control CLI for entry into the other Kyna CLI applications.

## Help Text

```bash
kyna v1

Control CLI for Kyna applications.

[command <args>]                        Run sub-command.
[?|-?|-h|help|--help <command name>]    Show this help.


Commands:
        Alias 1     | Alias 2     | Alias 3     | Command Name
----------------------------------------------------------------------
        backtest    | backtests   | backtesting | kyna-backtest.exe
        import      | importer    | imports     | kyna-importer.exe
        migrate     | migrator    | migrates    | kyna-migrator.exe
        report      | reports     | reporting   | kyna-report.exe
        research    |             |             | kyna-research.exe
```

You can get command-specific help by either passing the command to the `help` argument or by passing `--help` to the command argument.
The following two commands are equivalent.

```bash
kyna help import
kyna importer --help
```

Any arguments provided after the command are passed to that command's CLI.
For example, the following two commands are equivalent.

```bash
kyna migrate -v -f ./configs/test.json
kyna-migrator -v -f ./configs/test.json
```
