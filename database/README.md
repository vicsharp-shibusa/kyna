# Kyna Databases

This folder contains sql scripts for database table creations. Scripts are contained in folders named according to the relevant data engine. Hopefully the name of each script is self explanatory.

Check the `eng` folder on the root of the project for scripts that automate the creation of the required databases. The [README.md](../eng/README.md) in the `eng` folder also contains some relevant information about how to use **Docker** for these databases.

## Notes

### Ticks for Timestamps

When putting a `DateTime` into the database and then fetching it out, there was a small discrepancy (milliseconds), but the discrepancy caused equality operations to fail. By storing ticks as a `BIGINT`, the values going in and out of the database do not change. However, we also want to be able to see and filter by timestamps when querying the database, hence this construct:

```sql
    ticks_utc BIGINT NOT NULL,
    timestamp_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((ticks_utc - 621355968000000000) / 10000000)) STORED,
```

I found this solution on [this StackOverflow post](https://stackoverflow.com/questions/9056193/how-to-convert-a-sql-field-stored-as-a-tick-into-a-date).

### Storing Money

A bit of online searching indicates that the proper precision for storing money is `DECIMAL(19,4)`.
See [this StackOverflow post](https://stackoverflow.com/questions/224462/storing-money-in-a-decimal-column-what-precision-and-scale#:~:text=The%20rule%20of%20thumb%20for%20storage%20of%20fixed,than%20you%20actually%20require%20to%20allow%20for%20rounding.).

For PostgreSQL, there is no difference between `NUMERIC` and `DECIMAL`; see [this](https://www.postgresql.org/message-id/20211.1325269672@sss.pgh.pa.us) and [this](https://www.postgresql.org/docs/current/datatype-numeric.html).