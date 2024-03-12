#!/bin/bash

if [ $# -ne 2 ]; then
    echo "Usage: $0 <source directory> <target directory>"
    exit 1
fi

launch_directory=$(pwd)
source_directory="$1"
target_directory="$2"

if [ ! -d "$source_directory" ]; then
    echo "$source_directory is not a directory."
    exit 1
fi

if [ ! -d "$target_directory" ]; then
    echo "$target_directory is not a directory."
    exit 1
fi

kyna_directory="$2/kyna"
importer_directory="$kyna_directory/kyna-importer"
migrator_directory="$kyna_directory/kyna-migrator"
backtest_directory="$kyna_directory/kyna-backtest"
report_directory="$kyna_directory/kyna-report"

if [ -d "$launch_directory/temp" ]; then
    rm -rf $launch_directory/temp
fi

mkdir $launch_directory/temp

if [ -d "$importer_directory" ]; then
    cp $importer_directory/secrets.json $launch_directory/temp/importer_secrets.json
    rm -rf $importer_directory
fi

if [ -d "$migrator_directory" ]; then
    cp $migrator_directory/secrets.json $launch_directory/temp/migrator_secrets.json
    rm -rf $migrator_directory
fi

if [ -d "$backtest_directory" ]; then
    cp $backtest_directory/secrets.json $launch_directory/temp/backtest_secrets.json
    rm -rf $backtest_directory
fi

if [ -d "$report_directory" ]; then
    cp $report_directory/secrets.json $launch_directory/temp/report_secrets.json
    rm -rf $report_directory
fi

if [ -d "$kyna_directory" ]; then
    cp $kyna_directory/secrets.json $launch_directory/temp/kyna_secrets.json
    rm -rf $kyna_directory
fi

dotnet build -c Release $source_directory/Kyna.Cli/Kyna.Cli.csproj -o $kyna_directory
dotnet build -c Release $source_directory/Kyna.Importer.Cli/Kyna.Importer.Cli.csproj -o $importer_directory
dotnet build -c Release $source_directory/Kyna.Migrator.Cli/Kyna.Migrator.Cli.csproj -o $migrator_directory
dotnet build -c Release $source_directory/Kyna.Backtests.Cli/Kyna.Backtests.Cli.csproj -o $backtest_directory
dotnet build -c Release $source_directory/Kyna.Report.Cli/Kyna.Report.Cli.csproj -o $report_directory

mv $launch_directory/temp/importer_secrets.json $importer_directory/secrets.json
mv $launch_directory/temp/migrator_secrets.json $migrator_directory/secrets.json
mv $launch_directory/temp/backtest_secrets.json $backtest_directory/secrets.json
mv $launch_directory/temp/report_secrets.json $report_directory/secrets.json
mv $launch_directory/temp/kyna_secrets.json $kyna_directory/secrets.json

rm -rf $launch_directory/temp/

exit 0