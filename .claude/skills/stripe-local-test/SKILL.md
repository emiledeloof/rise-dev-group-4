---
name: stripe-local-test
description: Start and check everything needed to test Stripe payments locally in the RISE app (database, server, webhook listener), and verify or troubleshoot a test payment. Use when asked to test Stripe, Pay now, checkout, payments or webhooks locally, or when a booking stays on "Payment processing".
---

# Local Stripe testing

Run commands from the repo root in the Bash tool. The README section "Testing Stripe Payments
Locally" has the human version and the first-time setup.

## Rules

- **Never print secrets.** Don't echo `Stripe:SecretKey` or `Stripe:WebhookSecret`, or the raw
  output of `dotnet user-secrets list` or `stripe listen --print-secret`. Use the scripts below.
- **Never stop a running server to replace it.** It is probably the user's Visual Studio session,
  and a restart in Development deletes and reseeds the database. Only restart it if the user agrees.
- Use test keys only. Don't change secrets without asking: the key is the user's own.

## 1. Check the current state

```bash
bash .claude/skills/stripe-local-test/scripts/preflight.sh
```

| Output | Action |
| --- | --- |
| `KEY: MISSING`, `PLACEHOLDER` or `UNKNOWN FORMAT` | Stop. Ask the user to set their test key (README, one-time setup, step 2). |
| `KEY: LIVE` | Stop and tell the user. |
| `WEBHOOK SECRET: MISMATCH` | Ask before fixing: update the secret (README, step 3), then the server needs a restart. |
| `MISSING` for a URL or currency | Set it as in README step 2. |
| `DB:` not `healthy` | `docker compose up -d`. If Docker itself is down, ask the user to start Docker Desktop. |
| `SERVER: running on 5001` | Reuse it, and don't run `dotnet run`. Otherwise see step 3. |

Compare `LAST SERVER START` (time only; the date is in the log file name) with `LAST COMMIT` and `WORKTREE`. If the
server started before the latest change, it runs old code: tell the user instead of restarting it.

## 2. Start the listener

Start it with `run_in_background: true`, and start it before the server:

```bash
bash .claude/skills/stripe-local-test/scripts/listen.sh
```

It is ready when its output file shows `Ready!`. It lives only as long as this Claude session, so
tell the user that. Without it, bookings stay on "Payment processing".

## 3. Start the server (only if it isn't running)

```bash
dotnet run --project src/Rise.Server/Rise.Server.csproj
```

Use `run_in_background: true`, and wait until the output shows `Now listening on: https://localhost:5001`
or a build error. This reseeds the database, so warn the user if they may have data they care about.

If the build fails with `MSB3027` / `file is locked by`, another `Rise.Server` or Visual Studio is
running. Re-run the preflight: a server on 5001 can be reused. Otherwise ask the user to stop it.

To check whether the app is up, use `curl -sk https://localhost:5001/` and `src/Rise.Server/Logs/`.
`netstat` piped through `grep` missed the listening server here, and PowerShell process inspection
was blocked by auto mode, so don't rely on either.

## 4. Hand over to the user

Unless the user asked you to drive the browser, give them these steps:

1. Open https://localhost:5001, choose dates and a room, enter guest details, click **Pay now**.
2. Pay with `4242 4242 4242 4242`, any future date, any CVC, any name.
3. Expect `/booking/confirmation` to show **Confirmed**.
4. Optional: log in as `admin@example.com` / `A1b2C3!` and open `/admin/invoice-list`. The booking
   shows **Paid**.

Cancel flow: the back arrow on the Stripe page leads to `/payment/cancel`, and the booking stays unpaid.

Without the UI: booking 2 is open in the seed data, and this returns a `checkoutUrl`. It creates a
real test-mode session, so only run it when needed:

```bash
curl -k -X POST https://localhost:5001/api/bookings/2/checkout-session -H "Accept: application/json"
```

## 5. Verify a payment

Read the listener's output file. Success is `--> checkout.session.completed` followed by
`<-- [200] POST https://localhost:5001/api/payments/stripe-webhook`.

Only report the payment as working after seeing that `[200]`. An unsigned `curl` POST to the webhook
returning `400` only shows that the endpoint rejects unsigned calls.

| Symptom | Cause and fix |
| --- | --- |
| No event in the listener | The listener wasn't running during payment. Start it and pay again. |
| Webhook `[400]` | Webhook secret mismatch. See step 1. |
| Webhook `[404]` | The payment is gone: the server restarted and reseeded after it was created. Pay again. |
| **Pay now** fails, everything else works | Placeholder key, or the server started before the key was set. |
| Missing card details on a paid booking | The payment predates those fields. Make a new test payment. |
