#!/usr/bin/env bash
# Read-only status check for local Stripe testing. Run from the repo root.
# Never prints secret values: only names, key type and match results.

secrets=$(dotnet user-secrets list --project src/Rise.Server 2>/dev/null | tr -d '\r')
get() { printf '%s\n' "$secrets" | sed -n "s/^$1 = //p"; }

key=$(get 'Stripe:SecretKey')
stored_whsec=$(get 'Stripe:WebhookSecret')

case "$key" in
  '') echo "KEY: MISSING" ;;
  sk_test_not_configured) echo "KEY: PLACEHOLDER" ;;
  sk_test_*) echo "KEY: ok (test)" ;;
  sk_live_*) echo "KEY: LIVE - stop, do not test with this" ;;
  *) echo "KEY: UNKNOWN FORMAT" ;;
esac

case "$key" in
  sk_test_*)
    # The output starts with a blank line, so extract the value instead of comparing it raw.
    cli_whsec=$(stripe listen --api-key "$key" --print-secret 2>/dev/null | tr -d '\r' | grep -o 'whsec_[A-Za-z0-9]*' | head -1)
    if [ -z "$cli_whsec" ]; then echo "WEBHOOK SECRET: COULD NOT CHECK (is the Stripe CLI installed?)"
    elif [ "$cli_whsec" = "$stored_whsec" ]; then echo "WEBHOOK SECRET: ok"
    else echo "WEBHOOK SECRET: MISMATCH"
    fi
    ;;
  *) echo "WEBHOOK SECRET: SKIPPED (no test key)" ;;
esac

for name in Currency SuccessUrl CancelUrl; do
  value=$(get "Stripe:$name")
  [ -n "$value" ] && echo "$name: $value" || echo "$name: MISSING"
done

echo "DB: $(docker inspect -f '{{.State.Health.Status}}' rise-db 2>/dev/null || echo 'not running')"

code=$(curl -sk -o /dev/null -w '%{http_code}' --max-time 5 https://localhost:5001/)
if [ "$code" = "200" ]; then echo "SERVER: running on 5001"; else echo "SERVER: not running (HTTP $code)"; fi

log=$(ls -t src/Rise.Server/Logs/* 2>/dev/null | head -1)
if [ -n "$log" ]; then
  echo "LAST SERVER START: $(grep -h 'Now listening on: https' "$log" | tail -1 | cut -c2-9) (log: $log)"
fi
echo "LAST COMMIT: $(git log -1 --format='%cd' --date=format:'%F %T')"
echo "WORKTREE: $(git status --short | wc -l | tr -d ' ') changed file(s)"
