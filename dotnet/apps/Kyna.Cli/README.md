# Kyna

Kyna is a command and control CLI for entry into the other Kyna applications.

## Help Text

```bash
kyna v1

Control CLI for Kyna applications.

[command <args>]                        Run sub-command.
[?|-?|-h|help|--help <command name>]    Show this help.


Commands:
        backtest   | backtests  | backtesting
        import     | importer   | imports
        migrate    | migrator   | migrates
```

You can get command-specific help by either passing the command to the `help` argument or by passing `--help` to the command argument.
The following two commands are equivalent.

```bash
kyna help import
kyna importer --help
```

Any arguments provided after the command are passed to that command. For example:

```bash
kyna migrate -v -f ./configs/test.json
```

The above command invokes the `kyna-migrator` app with the arguments `-v -f ./configs/test.json`.