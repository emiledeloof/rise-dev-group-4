#!/usr/bin/env bash
# Forwards Stripe test webhooks to the local server, masking secrets in the output.
# Run from the repo root, in the background: it blocks until stopped.

key=$(dotnet user-secrets list --project src/Rise.Server 2>/dev/null | tr -d '\r' | sed -n 's/^Stripe:SecretKey = //p')

case "$key" in
  sk_test_not_configured | '') echo "No usable Stripe test key in User Secrets" >&2; exit 1 ;;
  sk_test_*) ;;
  *) echo "Refusing to listen: Stripe:SecretKey is not a test key" >&2; exit 1 ;;
esac

stripe listen --api-key "$key" \
  --events checkout.session.completed,checkout.session.async_payment_succeeded,checkout.session.async_payment_failed \
  --forward-to https://localhost:5001/api/payments/stripe-webhook 2>&1 |
  sed -u -E 's/whsec_[A-Za-z0-9]+/whsec_***/g; s/(sk|rk)_(test|live)_[A-Za-z0-9*]+/\1_\2_***/g'
