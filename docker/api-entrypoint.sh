#!/bin/sh
set -eu

key_directory=/home/app/.aspnet/DataProtection-Keys
mkdir -p "$key_directory"
chown -R app:app "$key_directory"

exec setpriv --reuid=app --regid=app --init-groups -- dotnet Sentinel.Api.dll
