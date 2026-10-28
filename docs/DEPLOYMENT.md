# Deployment

This guide has two parts:

| Part | Where it runs | Affects production? |
| --- | --- | --- |
| [**Part A: Local test deployment**](#part-a-local-test-deployment) (recommended before every release) | Your machine, http://localhost:8080 | No |
| [**Part B: Real deployment**](#part-b-real-deployment) | Production server, via GitHub Actions | **Yes** |

Do Part A first. Only start Part B when the local test works.

---

# Part A: Local test deployment

> Runs only on your machine. Nothing is pushed, published or deployed.

Before every real deployment, run the exact state you are about to release as a production container on your machine. Problems with the Docker image, migrations or production configuration then show up locally instead of on the live server.

## A.1 What the local test deployment is

`docker-compose.local.yml` starts the app the same way production runs it, independent of the development setup:

- **Same image recipe**: the app is built from the same `Dockerfile` as in the pipeline (Release build, `ASPNETCORE_ENVIRONMENT=Production`, plain HTTP on port 8080, non-root user). The image is built locally, not pulled from Docker Hub.
- **Own database**: a separate PostgreSQL container and volume (project `rise-local`). It does not touch your development database (`docker-compose.yml`), and both stacks can run side by side.
- **Startup like production**: migrations are applied on startup (`Database__MigrateOnStartup=true`) without deleting the database, unlike development, which recreates it every time.
- **Test data**: unlike production, it seeds test data by default (`APP_SEED=true`), including `admin@example.com` / `A1b2C3!`.
- **Stripe**: without `STRIPE_SECRET_KEY` in `.env` the app starts with a placeholder key and payments fail. Put your Stripe **test** keys in `.env` to test payments.

## A.2 Run it

Prerequisites: Docker Desktop is running and `.env` exists (`cp .env.example .env`, set `POSTGRES_PASSWORD`).

```bash
git checkout main
git pull origin main

docker compose -f docker-compose.local.yml down -v           # start from an empty database
docker compose -f docker-compose.local.yml up -d --build     # build the image and start app + database
docker compose -f docker-compose.local.yml logs -f app       # follow the app logs
```

## A.3 Check it

Open http://localhost:8080 and check at least:

- The app starts and the logs show no errors (migrations applied, seeding finished).
- The home page and search work, also at phone width.
- Logging in as `admin@example.com` works and the admin pages load.
- The booking flow works up to payment (with Stripe test keys, the full payment).
- Whatever was changed since the last release.

Running `down -v` before `up` tests the migrations on an empty database. To also test them on existing data, run `up -d --build` once on the previous release first, then again on the new `main` **without** `down -v`.

## A.4 Clean up

```bash
docker compose -f docker-compose.local.yml down -v   # stop and delete the test database
```

**Everything works?** Continue with Part B. **Something is broken?** Fix it on `main` and repeat Part A.

---

# Part B: Real deployment

> From here on you work on GitHub and the production server. Step B.2 deploys to the live site.

## How the pipeline works

Production is deployed by the GitHub Actions workflow `.github/workflows/deploy.yaml`. **Only a version tag deploys**; merging into a branch never does.

| Event | What runs |
| --- | --- |
| PR to `main`, push to `main` or `release` | `ci`: restore + CVE audit, `dotnet format --verify-no-changes`, Release build with `--warnaserror`, tests |
| Push of a tag `vX.Y.Z` | `ci` → `build` (Docker image `emiledeloofhg/devops:X.Y.Z` + `:latest`, pushed to Docker Hub) → `deploy` (prod server pulls the image and restarts the `rise-server-prod` container) |

The workflow file of the **tagged commit** is used, so pipeline changes must be on `main` before you release.

## B.1 Merge `main` into `release`

On GitHub, open a pull request with base `release` and compare `main`. Wait for the `ci` check to pass, then merge with **"Create a merge commit"**.

Do not use **"Squash and merge"**: it creates a commit that only exists on `release`. The two branches then have different histories, and the next `main` → `release` PR runs into conflicts and duplicate changes.

This merge does not deploy anything yet.

## B.2 Tag the release (this deploys)

```bash
git checkout release
git pull origin release
git tag -a v1.0.1 -m "Release 1.0.1"
git push origin v1.0.1
```

Pushing the tag starts the deployment. Versions follow `MAJOR.MINOR.PATCH` (the workflow only reacts to tags matching `v[0-9]+.[0-9]+.[0-9]+*`):

- **PATCH** (`v1.0.1`): bug fixes only
- **MINOR** (`v1.1.0`): new features
- **MAJOR** (`v2.0.0`): breaking changes

List existing versions with `git tag -l` (after `git fetch --tags`).

## B.3 Watch the run

In the repository's **Actions** tab, open the run for the tag. `ci`, `Build image` and `Deploy to Production` must all be green. On failure, the `deploy` job prints the container logs.

## B.4 When something goes wrong

- **Don't move or reuse a published tag.** Fix the problem on `main`, test it again with Part A, then release with the next patch version (e.g. `v1.0.2`).
- **Roll back**: in the Actions tab, open the run of the last working tag and click **Re-run all jobs**. This builds and deploys that version again.
- **Pipeline or server problems** (runners, Docker Hub, `/opt/rise/.env` on the prod server): contact the ops team.
