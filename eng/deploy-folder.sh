#!/bin/bash

if [ $# -ne 2 ]; then
    echo "Usage: $0 <source directory> <target directory>"
    exit 1
fi

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

cp -r $source_directory $target_directory

exit 0