---
name: github-workflow
description: Git and GitHub conventions for the RISE project (Conventional Commits 1.0 + RISE course rules). Use whenever creating a branch, writing a commit message, pushing, or writing a pull request title/description in this repo.
---

# RISE Git & GitHub workflow

Based on Conventional Commits 1.0 and the RISE course rules (feature branches, mandatory small PRs,
no force push on `main`, GenAI tool mentioned in the commit). Source: team guide by Maëlle Mohr (draft).

**Language: English everywhere** (branches, commits, PRs).

> Jira keys in this project look like `RI2627G12-336` (the guide's examples use `RISE-42`).
> Take the key from the current branch name.

## 0. Rules for Claude in this repo

- Commit and push on your own, without asking: small commits, one logical change each, on the
  current branch.
- Never create branches or pull requests (the sections below describe their format for when the
  user explicitly asks), and never commit or push to `main`.
- Run `dotnet format` before committing (CI enforces it).
- Don't stage unrelated user changes; add files explicitly with `git add <paths>`.

## 1. The flow

```
main ──●────────────────────────────●──────────▶
        \                          ↑ squash and merge
         feat/RISE-42-calendar-availability
          ● feat(client): add availability calendar component
          ● feat(services): add availability query
          ● test(services): cover fully booked periods
                                   │
        PR title (= commit on main):
        feat(calendar): show booked nights as unavailable  [RISE-42]
```

| Where       | Scope                              | Jira key       |
|-------------|------------------------------------|----------------|
| Branch name | —                                  | ✅             |
| Commits     | technical (client, services…)      | ❌ never       |
| PR title    | functional (calendar, booking…)    | ✅ at the end  |

One branch = one user story / Jira ticket = one PR = one commit on `main`.

## 2. Branches

```
<type>/<JIRA-KEY>-<short-description>

feat/RISE-42-calendar-availability
fix/RISE-31-checkout-before-checkin
refactor/RISE-40-rate-service
docs/readme-setup            ← docs/chore branches may skip the key
```

- Lowercase, words separated by `-`, 3–5 words.
- Create from an up-to-date main: `git switch main && git pull && git switch -c feat/RISE-42-calendar-availability`
- Keep in sync: `git pull --rebase origin main`.
- Short-lived: a few days, max one sprint. Delete after merge.
- **Never commit or push directly to `main`. Never force push on `main`.** On your own branch,
  `git push --force-with-lease` after a rebase is OK.
- Don't work on someone else's branch without asking.

## 3. Commit messages (on the branch)

```
<type>(<scope>): <summary>

<body — optional: what and why>

<trailers — optional>
```

Example:

```
feat(client): add availability calendar component

Booked nights are greyed out and can't be selected.

Assisted-by: Claude
```

### Rules
- Format is mandatory on **every** commit (teachers look at each member's commits).
- Max **72 characters** on the first line.
- Imperative, present tense: "add", "fix", "remove" — not "added"/"adds".
- Summary starts **lowercase**, **no final period**.
- **No Jira key** in commits.
- One commit = one logical change. If you need "and", it's probably two commits.

### Types
| Type       | When                                               |
|------------|----------------------------------------------------|
| `feat`     | New feature / behaviour                            |
| `fix`      | Bug fix                                            |
| `refactor` | Code change with no behaviour change               |
| `test`     | Add or update tests only                           |
| `docs`     | README, comments, documentation                    |
| `style`    | Formatting, whitespace, CSS-only tweaks (no logic) |
| `perf`     | Performance improvement                            |
| `build`    | `.csproj`, `.sln`, NuGet config, Dockerfile        |
| `ci`       | Pipeline config                                    |
| `chore`    | Anything else (`.gitignore`…)                      |
| `revert`   | Revert a previous commit                           |

### Scopes = technical layer (projects of `Rise.sln`)
| Scope         | Project(s)                                   |
|---------------|----------------------------------------------|
| `client`      | Rise.Client (Blazor WASM UI) + its tests     |
| `server`      | Rise.Server (ASP.NET Core REST API)          |
| `services`    | Rise.Services + its tests                    |
| `domain`      | Rise.Domain (entities, business rules) + tests |
| `persistence` | Rise.Persistence (EF Core, migrations)       |
| `shared`      | Rise.Shared (DTOs, contracts)                |
| `deps`        | NuGet package updates                        |

- Scope is optional: omit it if the commit touches several layers
  (`refactor: rename Guest to Customer everywhere`) — or better, split the commit.
- For tests, use the layer being tested: `test(services): cover fully booked periods`.
- When committing changes across layers, prefer one commit per layer (domain → persistence →
  services → server → client), each staged with `git add <paths>`.

### Trailers
| Trailer                       | When                                                    |
|-------------------------------|---------------------------------------------------------|
| `Assisted-by: <tool>`         | **Mandatory** if GenAI helped — always add `Assisted-by: Claude` to commits you write |
| `Co-authored-by: Name <email>`| Pair programming                                        |
| `BREAKING CHANGE: …`          | Breaks API, DB schema or config for others (also add `!`: `feat(persistence)!: …`) |

Write multi-line messages with a heredoc:

```bash
git commit -F - <<'EOF'
feat(services): add availability query

Assisted-by: Claude
EOF
```

## 4. Pull requests

### Title = the commit on `main` (squash and merge)

```
<type>(<feature>): <summary>  [<JIRA-KEY>]

feat(calendar): show booked nights as unavailable  [RISE-42]
fix(booking): reject check-out before check-in  [RISE-31]
feat(i18n): add Italian translations  [RISE-22]
```

Same rules as commits (72 chars max, imperative, lowercase, no period), but:
- the scope is **functional** — the feature / user story: `home`, `calendar`, `i18n`, `booking`,
  `payment`, `reviews`, `rooms`, `search`, `account`, `admin`, `email`, `auth`, `infra`
- the **Jira key is mandatory at the end** (except docs/chore PRs without a ticket).

### Rules
- PRs are mandatory for everything; keep them small (reviewable in ~15 min). Split big stories.
- 1 approval from another dev before merging, with real suggestions — not just "LGTM".
- Fill in the PR template (`.github/pull_request_template.md` if present): what, how to test,
  screenshots for UI, GenAI used.
- When squashing, keep the `Assisted-by:` / `Co-authored-by:` trailers in the final message.
- Delete the branch after merge.
- Use `gh pr create --title "..." --body "..."` (only when the user asks for a PR).

## 5. GenAI
Allowed, but:
- the author is responsible for everything checked in and must understand it (oral exam);
- mention the tool with `Assisted-by: <tool>` in every commit where it helped;
- the course recommends not using AI-generated code for 100% of the tasks.

## 6. Tooling (planned / from the team guide)
- `sh setup-hooks.sh` (Git Bash, repo root) enables the hooks in `.githooks/`:
  - `pre-commit` — blocks commits on `main` and branch names not matching `type/KEY-desc`
  - `commit-msg` — blocks wrong format, > 72 chars, final period, uppercase start, Jira key in
    message, unknown scope; warns on past tense, scope ≠ files touched, missing `Assisted-by:`
  - `pre-push` — blocks pushing directly to `main`
  - Types and scopes live in `.githooks/config.sh`. Emergency bypass: `--no-verify` (not a habit).
- GitHub: `.github/workflows/pr-title.yml` checks PR titles; `.github/pull_request_template.md`
  pre-fills PR descriptions. Branch protection on `main`: require PR, 1 approval, PR-title check,
  no force pushes, squash merge only.

These files may not exist yet in the repo — check before relying on them.

## 7. Quick examples

Commits on `feat/RISE-18-homepage-activities`:
```
feat(domain): add Activity entity
feat(persistence): add activities table and seed data
feat(services): add activity listing query
feat(client): add activities section with photo cards
test(services): cover empty activity list
```
PR title → `feat(home): add activities section  [RISE-18]`

Commits on `fix/RISE-31-checkout-before-checkin`:
```
fix(domain): reject check-out date before check-in
test(domain): cover same-day check-in and check-out
```
PR title → `fix(booking): reject check-out before check-in  [RISE-31]`

Commits on `docs/readme-setup`:
```
docs: add local setup steps to README
```
PR title → `docs: add local setup steps`
