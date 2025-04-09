# Kyna Databases

This folder contains sql scripts for database table creations. Scripts are contained in folders named according to the relevant data engine. Hopefully the name of each script is self explanatory.

Check the `eng` folder on the root of the project for scripts that automate the creation of the required databases. The [README.md](../eng/README.md) in the `eng` folder also contains some relevant information about how to use **Docker** for these databases.

## Notes

### Ticks for Timestamps

The database tables have timestamp columns like `created_at` and `updated_at` but also contain `bigint` values that store the Unix milliseconds.
These columns exist to overcome a precision difference that results in undesirable behavior.
If you take a `record` object and set a timestamp, save it to the database, pull it back out and then compare the fetched version with the original version, you get an equality mismatch.
This is because the timestamp changes ever so slightly as a result of more precision in the database engine than in the .NET code base.
To overcome this problem, we preserve the timestamp as Unix milliseconds and use that value to hydrate the dates when deserializing into C# objects.

### Storing Money

A bit of online searching indicates that the proper precision for storing money is `DECIMAL(19,4)`.
See [this StackOverflow post](https://stackoverflow.com/questions/224462/storing-money-in-a-decimal-column-what-precision-and-scale#:~:text=The%20rule%20of%20thumb%20for%20storage%20of%20fixed,than%20you%20actually%20require%20to%20allow%20for%20rounding.).

For PostgreSQL, there is no difference between `NUMERIC` and `DECIMAL`; see [this](https://www.postgresql.org/message-id/20211.1325269672@sss.pgh.pa.us) and [this](https://www.postgresql.org/docs/current/datatype-numeric.html).