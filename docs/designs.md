# Kyna Designs

## Data Import

The following diagram represents an approximation of the `kyna-data-import` application's design and dependencies.

![Data Import Diagram](./images/kyna-data-import.png)

The CLI, `kyna-data-import`, instantiates an implementation of `IExternalDataImporter` based on the `-s <source name>` argument. Currently, the only supported implementation is the `EodHdImporter`, which has a source name of `eodhd.com`. The `<source name>` must correspond to the `Source` property in your `IExternalDataImporter` implementation (see below) for the connection to take place.

Each implementation of `IExternalDataImporter` must contain some sort of configuration data structure, represented in the diagram as `DataImportConfiguration`. I had hoped to have a generic import configuration, but it ultimately didn't make sense - the variations between the possible importers is too great to try to generify, so each importer will require it's own configuration structure, which may be housed inside the importer class.

`ImportAction` is a simple struct. The `EodHdImporter` takes in its known configuration structure (e.g., `DataImportConfiguration`) and deserializes it into whatever it needs, which must include some representation of one or more `ImportAction` instances.

The `ImportAction` instances inform the `ImportAsync` function what to do to complete the import task.

Each `IExternalDataImporter` implementation should inherit from `DataImporterBase`, which contains references to `ApiTransactionService` (which writes to the `imports.api_transactions` table) and an `HttpClient` instance, which is responsible for communicating with the third-party API.

```csharp
public interface IExternalDataImporter
{
    string Source { get; }

    Task<TimeSpan> ImportAsync(CancellationToken cancellationToken = default);

    event EventHandler<CommunicationEventArgs>? Communicate;

    Task<string> GetInfoAsync();

    void Dispose();
}
```

Not shown in the diagram above is the `secrets.json` file, which contains the database connection strings and also the secret key for the third-party API.

An example of the `secret.json`:

```json
{
  "ConnectionStrings": {
    "Logs": "User ID=postgres;Password=secret_password;Host=127.0.0.1;Port=5432;Database=logs;",
    "Imports": "User ID=postgres;Password=secret_password;Host=127.0.0.1;Port=5432;Database=imports;"
  },
  "DatabaseEngines": {
    "Logs": "PostgreSql",
    "Imports": "PostgreSql"
  },
  "ApiKeys": {
    "eodhd.com": "MY-SECRET-KEY"
  }
}
```

The name of the key, `eodhd.com` in the `ApiKeys` section above must correspond to the `Source` property of your `IExternalDataImport` implementation and, of course, the value must be a valid key.

The `DataImportConfiguration` class is a deserialized implementation of a JSON file passed into the `kyna-data-import` application using the `-f <file name>` argument. For samples of possible configuration files, see the `configs` folder under the `Kyna.FinancialDataImport.Cli` project in the Kyna solution.

The configuration file is passed on the command line, deserialized into the appropriate class, and then used in the instantiation of your `IExternalDataImporter` implementation. See the `ConfigureImporter` function in the `Program.cs` in the `Kyna.FinancialDataImport.Cli` project.

### Creating a new `IExternalDataImporter` Implementation

1. Create a new class that inherits from `DataImportBase` and implements `IExternalDataImporter`.
1. Create a class that represents the input configuration and will cover your input needs. Consider housing this class within your importer.
1. Build a constructor that takes in the configuration class from the step above and in that constructor, convert your nested class into a collection of `ImportAction` instances (and other stuff as needed).
1. Write the `ImportAsync` function to perform your import tasks based on those `ImportAction` instances (and any other configuration that you want to include.)
1. Ensure that the `Source` property in your importer implementation corresponds to the name of your external API, is unique within your system, and aligns with one and only one key in the `ApiKeys` section of your `secrets.json` file.

#### Notes

The abstract class, `DataImportBase` contains a few `protected virtual` functions that you can use or override.

The first is `HideToken`, which is used to obfuscate your secret key when writing to the `imports.api_transactions` table. This prevents your secret key from leaking.

The second is `GetStringResponseAsync`, which makes use of the `HttpClient` to make calls to the external API, write them to the `imports.api_transactions` table, and return the response as a string.

The final is `InvokeApiCallAsync`, which calls `GetStringResponseAsync`, but throws away the string result.

The reason for both `GetStringResponseAsync` and `InvokeApiCallAsync` is that sometimes you need the results in real-time, like when the results inform some subsequent import action you want to take. For example, in the `EodHdImporter` class, a list of symbols is captured (and filtered) and that list of symbols is used for capturing price (and other) data. Other calls are simply made and passed to the `ApiTransactionService` to be written to the data store without the importer concerning itself with their content.
