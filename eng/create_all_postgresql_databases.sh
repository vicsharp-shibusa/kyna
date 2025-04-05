#!/bin/bash

##
## Best to run this from the kyna/database directory.
##

# Check PostgreSQL connection at start
PG_HOST="${PG_HOST:-localhost}"
PG_PORT="${PG_PORT:-5432}"
PG_USER="${PG_USER:-postgres}"

# Test connection
psql -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -c "SELECT 1" >/dev/null 2>&1 || {
    printf "Error: Cannot connect to PostgreSQL at %s:%s as %s\n" "$PG_HOST" "$PG_PORT" "$PG_USER"
    exit 1
}

# Confirmation prompt unless -y flag is provided
if [ "$#" -eq 0 ] || [ "$1" != "-y" ]; then
    printf "\n\nThis script will COMPLETELY REMOVE AND REPLACE your Kyna PostgreSQL databases.\n"
    printf "There is no recovery from this unless you have backups.\n\n"
    
    read -p "Are you sure you want to do this? (y/n): " confirmation
    
    if [ "$confirmation" != "y" ]; then
        printf "\nCrisis averted.\n"
        exit 0
    fi
fi

create_db() {
    local DB_NAME=$1
    local PG_HOST="${PG_HOST:-localhost}"
    local PG_USER="${PG_USER:-postgres}"
    local PG_PORT="${PG_PORT:-5432}"
    
    printf "\nDROPPING %s\n" "$DB_NAME"
    dropdb --if-exists -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" "$DB_NAME" || {
        printf "Error: Failed to drop database %s\n" "$DB_NAME"
        exit 1
    }
    
    printf "\nCREATING %s\n" "$DB_NAME"
    createdb -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" "$DB_NAME" || {
        printf "Error: Failed to create database %s\n" "$DB_NAME"
        exit 1
    }
    
    printf "_______________________________________\n"
}

run_script() {
    local DB_NAME=$1
    local SCRIPT_LOCATION=$2
    local PG_HOST="${PG_HOST:-localhost}"
    local PG_USER="${PG_USER:-postgres}"
    local PG_PORT="${PG_PORT:-5432}"
    
    printf "\nRUNNING %s AGAINST %s\n" "$SCRIPT_LOCATION" "$DB_NAME"
    psql -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$DB_NAME" -f "$SCRIPT_LOCATION" || {
        printf "Error: Failed to run %s on %s\n" "$SCRIPT_LOCATION" "$DB_NAME"
        exit 1
    }
    
    printf "_______________________________________\n"
}

# Assuming script is run from directory containing 'postgresql' subdirectory
SCRIPT_DIR="postgresql"

LOG_DBS=("logs_test" "logs")
for db_name in "${LOG_DBS[@]}"; do
    create_db "$db_name"
    run_script "$db_name" "$SCRIPT_DIR/create_log_tables.sql"
done

IMPORT_DBS=("imports_test" "imports")
for db_name in "${IMPORT_DBS[@]}"; do
    create_db "$db_name"
    run_script "$db_name" "$SCRIPT_DIR/create_import_tables.sql"
done

FINANCIALS_DBS=("financials_test" "financials")
for db_name in "${FINANCIALS_DBS[@]}"; do
    create_db "$db_name"
    run_script "$db_name" "$SCRIPT_DIR/create_prices_tables.sql"
    run_script "$db_name" "$SCRIPT_DIR/create_fundamental_tables.sql"
done

BACKTESTS_DBS=("backtests_test" "backtests")
for db_name in "${BACKTESTS_DBS[@]}"; do
    create_db "$db_name"
    run_script "$db_name" "$SCRIPT_DIR/create_backtest_tables.sql"
done

printf "\nDatabase setups complete.\n"
exit 0