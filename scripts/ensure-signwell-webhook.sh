#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_PROJECT_PATH="$ROOT_DIR/src/DocumentGenerator.Api"
SIGNWELL_BASE_URL="${SIGNWELL_BASE_URL:-https://www.signwell.com}"
WEBHOOK_URL="${1:-${SIGNWELL_WEBHOOK_URL:-}}"
API_APPLICATION_ID="${SIGNWELL_API_APPLICATION_ID:-}"

if [[ -z "$WEBHOOK_URL" ]]; then
  echo "Usage: $0 <webhook-url>" >&2
  echo "Or set SIGNWELL_WEBHOOK_URL in the environment." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is required but was not found on PATH." >&2
  exit 1
fi

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required but was not found on PATH." >&2
  exit 1
fi

SIGNWELL_API_KEY="$(
  dotnet user-secrets list --project "$API_PROJECT_PATH" |
    awk -F ' = ' '$1 == "SignWell:ApiKey" { print $2 }'
)"

if [[ -z "$SIGNWELL_API_KEY" ]]; then
  echo "SignWell:ApiKey was not found in dotnet user-secrets for $API_PROJECT_PATH." >&2
  exit 1
fi

echo "Checking existing SignWell webhooks for $WEBHOOK_URL"

EXISTING_HOOKS_RESPONSE="$(
  curl --silent --show-error --fail \
    --request GET \
    --url "$SIGNWELL_BASE_URL/api/v1/hooks" \
    --header "X-Api-Key: $SIGNWELL_API_KEY"
)"

if printf '%s' "$EXISTING_HOOKS_RESPONSE" | grep -F "\"callback_url\":\"$WEBHOOK_URL\"" >/dev/null 2>&1 ||
   printf '%s' "$EXISTING_HOOKS_RESPONSE" | grep -F "\"callback_url\": \"$WEBHOOK_URL\"" >/dev/null 2>&1
then
  echo "SignWell webhook already exists for $WEBHOOK_URL"
  exit 0
fi

REQUEST_BODY="$(printf '{"callback_url":"%s"}' "$WEBHOOK_URL")"

if [[ -n "$API_APPLICATION_ID" ]]; then
  REQUEST_BODY="$(printf '{"callback_url":"%s","api_application_id":"%s"}' "$WEBHOOK_URL" "$API_APPLICATION_ID")"
fi

echo "Creating SignWell webhook for $WEBHOOK_URL"

CREATE_RESPONSE="$(
  curl --silent --show-error --fail \
    --request POST \
    --url "$SIGNWELL_BASE_URL/api/v1/hooks" \
    --header "X-Api-Key: $SIGNWELL_API_KEY" \
    --header "Content-Type: application/json" \
    --data "$REQUEST_BODY"
)"

echo "SignWell webhook created successfully."
echo "$CREATE_RESPONSE"
