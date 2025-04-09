# Kyna Engineering Folder

## Local Deployment

The **deploy-from-local.sh** script can be used to deploy the *kyna/dotnet/app* CLI applications to a target directory.
The script takes two arguments:

1. A path to the apps, e.g., **kyna/dotnet/apps**.
2. A path to the target location, e.g., **/c/bin**

```bash
./eng/deploy-from-local.sh ./dotnet/apps /c/bin
```

The above script will:

1. Find all the `secrets.json` files in the target directory structure and copy them to a temp location.
2. Perform a **Release** build of the Kyna applications and deploy them to their respective target directories.
3. Move the `secrets.json` files back to their original locations and delete the temp location.

By adding `/c/bin/kyna` to your $PATH, you can then execute the kyna applications from anywhere.

## PostgreSQL

For now, you will need [PostgreSQL](https://www.postgresql.org/) to use this app. 

You can download PostgreSQL from [https://www.postgresql.org/](https://www.postgresql.org/) or you can install Docker and use it to create a PostgreSQL image. 

To install Docker for your OS, see [the Docker install page](https://docs.docker.com/engine/install/).

### Creating the PostgreSQL databases

#### Individual Database Scripts

There are a number of scripts for creating the various databases:

1. **create_log_databases.sh** - creates the `logs` and `log_tests` databases.
1. **create_import_databases.sh** - creates the `imports` and `import_tests` databases.
1. **create_financial_databases.sh** - creates the `financials` and `financial_tests` databases.
1. **create_backtest_databases.sh** - creates the `backtests` and `backtest_tests` databases.

An argument of `-y` can be passed to any of the above scripts to bypass the "are you sure?" prompt.

Note that **NONE** of these script have been tested against a Docker installation.

#### All-in-One Script

The **eng/create_all_postgresql_databases.sh** script drops all Kyna databases and re-creates them from scratch. Note that this script has **NOT** been tested against a Docker installation.

From the `eng` folder, run this command:

```bash
./create_all_postgresql_databases.sh

# to bypass the "are you sure" confirmation check, you can add -y as an argument
./create_all_postgresql_databases.sh -y
```

To avoid entering your database password multiple times, run the following command first, or add it to your `.bashrc`:

```bash
export PGPASSWORD=MyPassword
```

If you encounter an error about the database being in use, it's likely the result of some database administration tool (e.g., pgAdmin) being open and connected to your database.
I close pgAdmin when this happens - that seems to resolve the issue.

If the script will not run on your local machine, it may be related to permissions. From the `eng` folder, try:

```bash
chmod +x create_postgresql_databases.sh
```

### PostgreSQL Backups and Restores

See [pg_dump help](https://www.postgresql.org/docs/9.3/app-pgdump.html)

```bash
pg_dump -U postgres -h localhost -p 5432 -j 5 -f dumpdir -Fd $DB_NAME
```

or (for Docker installations)

```bash
docker exec -i docker_postgresql_1 pg_dump -U postgres -h localhost -p 5432 -j 5 -f dumpdir -Fd $DB_NAME
```

To copy the files from your Docker container to your local box:

```bash
docker cp docker_postgresql_1:/dumpdir .
```

To remove the `dumpdir` directory from your Docker container:

```bash
docker exec -i docker_postgresql_1 rm -r dumpdir
```

To copy files from your box to your Docker container:

```bash
docker cp dumpdir docker_postgresql_1:.
```

To restore, see [pg_restore help](https://www.postgresql.org/docs/9.2/app-pgrestore.html)

Create the relevant database, and then run:

```bash
pg_restore -U postgres -h localhost -p 5432 -d $DB_NAME dumpdir
```

or (for Docker installations)

```bash
docker exec -i docker_postgresql_1 pg_restore -U postgres -h localhost -p 5432 -d $DB_NAME dumpdir
```
