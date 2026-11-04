#!/usr/bin/env bash
# Loads the infrastructure deploy variables from infra/.env into the current shell.
# Must be sourced, not executed:
#   source infra/scripts/load-env.sh [path/to/.env]
# Empty values are unset, so the readEnvironmentVariable() defaults in prod.bicepparam apply.

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
  echo "Run it with: source ${0}" >&2
  exit 1
fi

_env_file="${1:-$(dirname "${BASH_SOURCE[0]}")/../.env}"
if [ ! -f "$_env_file" ]; then
  echo "File not found: $_env_file (copy infra/.env.example to infra/.env and fill it in)." >&2
  return 1
fi

set -a
# shellcheck disable=SC1090
source "$_env_file"
set +a

_loaded=$(grep -E '^[[:space:]]*(export[[:space:]]+)?[A-Za-z_][A-Za-z0-9_]*=' "$_env_file" \
  | sed -E 's/^[[:space:]]*(export[[:space:]]+)?([A-Za-z_][A-Za-z0-9_]*)=.*/\2/')
for _name in $_loaded; do
  [ -z "${!_name}" ] && unset "$_name"
done
echo "Loaded variables from $_env_file: $(echo $_loaded | tr ' ' ',')"

for _name in AZURE_RESOURCE_GROUP AZURE_AD_TENANT_ID AZURE_AD_CLIENT_ID SQL_SERVER_NAME SQL_DATABASE_NAME; do
  [ -z "${!_name}" ] && echo "Warning: required variable still empty: $_name" >&2
done

unset _env_file _loaded _name
