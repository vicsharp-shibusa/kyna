# Kyna Databases

This folder contains sql scripts for database table creations. Scripts are contained in folders named according to the relevant data engine. Hopefully the name of each script is self explanatory.

Check the `eng` folder on the root of the project for scripts that automate the creation of the required databases.

## Notes

### Ticks for Timestamps

When putting a `DateTime` into the database and then fetching it out, there was a small discrepancy (milliseconds), but the discrepancy caused equality operations to fail. By storing ticks as a `BIGINT`, the values going in and out of the database do not change. However, we also want to be able to see and filter by timestamps when querying the database, hence this construct:

```sql
    ticks_utc BIGINT NOT NULL,
    timestamp_utc TIMESTAMP WITH TIME ZONE GENERATED ALWAYS AS (to_timestamp((ticks_utc - 621355968000000000) / 10000000)) STORED,
```

I found this solution on [this StackOverflow post](https://stackoverflow.com/questions/9056193/how-to-convert-a-sql-field-stored-as-a-tick-into-a-date).