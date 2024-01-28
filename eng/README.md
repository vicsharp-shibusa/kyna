# Kyna Engineering Folder

## PostgreSQL

For now, you will need [PostgreSQL](https://www.postgresql.org/) to use this app. 

You can download PostgreSQL from [https://www.postgresql.org/](https://www.postgresql.org/) or you can install Docker and use it to create a PostgreSQL image. 

To install Docker for your OS, see [the Docker install page](https://docs.docker.com/engine/install/).

### Creating the PostgreSQL databases

#### eng/create_postgresql_databases.sh

This bash script drops all Kyna databases and re-creates them from scratch. Note that this script has **NOT** been tested against a Docker installation.

From the `eng` folder, run this command:

```bash
./create_postgresql_databases.sh

# to bypass the "are you sure" confirmation check, you can add -y as an argument
./create_postgresql_databases.sh -y
```

To avoid entering your database password multiple times, run the following command first, or add it to your `.bashrc`:

```bash
export PGPASSWORD=MyPassword
```

If you encounter an error about the database being in use, it's likely the result of some database administration tool (e.g., pgAdmin) being open and connected to your database. You can try to disconnect; I close pgAdmin when this happens - that always resolves the issue.

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
