# Frontend Dockerization

**Date:** 2026-10-13

---

## Turn 1

### Prompt
> I want you to do the docker container for the frontend now.

### Thinking
Looked at how the backend had been containerized (`Backend/Dockerfile`, the `api` service in
`docker-compose.yml`) to keep the same conventions: multi-stage builds, non-root runtime user,
loopback-only published ports, and a `/health`-style health check. For an Angular SPA this
means a build stage (Node.js, `ng build`) producing static files, and a separate, much smaller
runtime stage that only serves them — nginx is the standard choice, and `nginxinc/nginx-unprivileged`
avoids running the container as root without extra configuration.

Planned the pieces: `Frontend/Dockerfile` (Node build stage → nginx runtime stage), an nginx
config template with SPA fallback (`try_files $uri /index.html`) so Angular's client-side
router works on a hard refresh, a `/health` endpoint for Compose health checks, far-future
cache headers for hashed build output, gzip, and a Content-Security-Policy header. The CSP's
`connect-src` needs to allow the API origin, which at the time was passed in as a runtime
environment variable (`API_URL`) substituted into the config via nginx's built-in
`envsubst`-on-startup mechanism (`/etc/nginx/templates/*.template`).

### Response
Added `Frontend/Dockerfile`, `Frontend/nginx/default.conf.template`, `Frontend/.dockerignore`,
and a `web` service in `docker-compose.yml` with `build.args` for `APP_VERSION` and an
`API_URL` environment variable, built from `.env.example`. Published the port on `127.0.0.1`
only, matching the `api` service's convention.

---

## Turn 2

### Prompt
> [Reported the `npm ci`/compose build failing with an `ERESOLVE` dependency conflict.]

### Thinking
The committed `package-lock.json` had `@angular/animations@21.2.12`, which requires
`@angular/core@21.2.12` exactly, while the rest of the lockfile was pinned to
`@angular/core@21.2.11` — a lockfile that had drifted out of sync with `package.json`.

### Response
Pinned `@angular/animations` back to `^21.2.11` in `package.json` and regenerated
`package-lock.json` so every Angular package resolves to the same `21.2.11` version. The user
later reported the same failure recurring (an earlier edit hadn't persisted); re-applied the
fix directly to the files on the user's machine and confirmed by diff that it stuck.

---

## Turn 3

### Prompt
> The 45 line in the default.conf.template is really long. Can you do something about it?

### Thinking
nginx can't fold a single quoted string literal across multiple source lines without the
literal newline becoming part of the header value sent to the browser, so the fix isn't
line-wrapping — it's splitting the CSP into several statements that build up the same final
value.

### Response
Rewrote the CSP block as a chain of `set $csp "${csp}...";` statements, one per directive,
each well under 100 characters, concatenating into the same header value as before (verified
byte-for-byte identical via `curl` before and after).

---

## Turn 4

### Prompt
> The web container is reported to be unhealthy? *(with an attached log)*

### Thinking
The healthcheck ran `wget --spider http://localhost:8080/health` from inside the Alpine-based
nginx image. Alpine resolves `localhost` to `::1` (IPv6) first, and BusyBox's `wget` doesn't
fall back to IPv4 when the IPv6 connection is refused — but nginx in this image only listens
on IPv4. The two were never going to agree on `localhost`.

### Response
Changed the healthcheck's URL from `http://localhost:8080/health` to
`http://127.0.0.1:8080/health`, removing the ambiguity entirely.

---

## Turn 5

### Prompt
> I want you to review the current feature branch if it is ready to merge?

### Thinking
Cloned the branch into a disposable sandbox, test-merged it into `dev` to check for conflicts,
and ran both Angular build configurations. Found one real problem: `.env.example` had
`API_URL` pointing at the production API, uncommented, which meant anyone following the README
literally would build their local Docker frontend against production while
`ASPNETCORE_ENVIRONMENT=Development` pointed the backend at local Postgres — a confusing,
inconsistent default.

### Response
Flagged the `.env.example` inconsistency as the one blocking issue, along with a list of
non-blocking suggestions (image tagging doesn't reflect build configuration, base images
pinned by tag rather than digest, no dependency/image scanning yet). Did not fix anything in
this turn — this was a review, not a change.

---

## Turn 6

### Prompt
> Actually, let's go with the environment.prod.ts solution option.

### Thinking
This picked between two ways to get the production API URL into the Angular bundle that had
been discussed as alternatives: Angular's `define` build option (injecting a compile-time
global constant via `declare const API_URL: string`) versus the more idiomatic
`fileReplacements` mechanism, swapping `environment.ts` for a per-configuration
`environment.prod.ts`. The latter is also what the project's own `AUDIT.md` already
recommended as the fix for a "no production environment file" finding, so adopting it also
closed that audit item.

### Response
Added `Frontend/src/environments/environment.prod.ts` with the production `apiUrl`, reverted
`environment.ts` to the local development URL, and added a `fileReplacements` entry to the
`production` configuration in `angular.json`. Removed the `define`-based approach entirely.

---

## Turn 7

### Prompt
> Is the environment: API_URL still needed in the web part of the compose?

### Thinking
With the bundle's API URL now fixed at Angular build time via `environment.prod.ts` /
`environment.ts`, the only remaining job for the runtime `API_URL` variable was feeding the
CSP's `connect-src` — and there was nothing enforcing that the two stayed in sync. A developer
could build the `production` Angular configuration while leaving `API_URL` pointed at the
local API in `.env`, giving a CSP that silently didn't match what the bundle actually called.

Decided the right fix was to derive the CSP's allowed origin from the exact same environment
file the Angular build uses, at build time, rather than keep it as a second, independently-set
variable.

### Response
Answered no — proposed removing `API_URL` from the compose file entirely in favor of a
build-time script that reads the `apiUrl` straight out of whichever environment file
`fileReplacements` selects for the given configuration.

---

## Turn 8

### Prompt
> How would the 1. change look like, don't do it yet, just so the codes here.

### Thinking
Sketched the design without touching any files: a small Node.js script run inside the Docker
build stage, after `ng build`, that reads `angular.json` to find the `fileReplacements` entry
for the given `--configuration`, dynamically imports that environment file (mirroring exactly
what Angular's own build does), validates its `apiUrl`, and substitutes it into
`default.conf.template` once, producing a final `default.conf` baked into the image — removing
both the `API_URL` build arg/environment variable and nginx's startup-time `envsubst` step.

### Response
Presented the full design and the exact code for `Frontend/nginx/render-config.mjs`, the
`Dockerfile` RUN step invoking it, and the resulting `default.conf.template` change, without
applying any of it, per the request.

---

## Turn 9

### Prompt
> I see, you can do the change

### Thinking
Implemented exactly what was designed in Turn 8.

### Response
Added `Frontend/nginx/render-config.mjs`. Changed the `Dockerfile` to run it right after
`ng build` and copy its rendered `default.conf` straight into
`/etc/nginx/conf.d/default.conf` (no more `/etc/nginx/templates/`). Removed `API_URL` from
`docker-compose.yml`'s `web` service and from `.env.example`. Updated `DOCUMENTATION.md`'s
security section to describe the CSP as rendered from the same environment file as the bundle,
so the two can't diverge. Deleted the "No production environment file in Angular" finding from
`AUDIT.md`, following the exact removal pattern of two earlier commits on the project
(heading, body, and trailing separator all removed together). Verified both the
`development` and `production` Angular configurations still produce a `default.conf` whose
`connect-src` exactly matches the bundle's compiled-in `apiUrl`.

---

## Turn 10

### Prompt
> Now create a commit message for the staged changes base on the prev commit messages.

### Thinking
Matched the project's established commit style: short, imperative, past-tense, single
sentences ending in a period (e.g. "Added the frontend docker file.",
"Updated the docker compose file."). Did not run `git commit` — only produced the message text,
per the standing project rule against creating commits.

### Response
Proposed: "Moved the api url definition." for the change moving `API_URL` out of the compose
file and `.env.example` and into the build-time rendering script.

---

## Turn 11

### Prompt
> [Pasted browser console output from testing the Development build locally:]
> `Connecting to 'https://cdn.jsdelivr.net/npm/chart.js@4/dist/chart.umd.min.js.map' violates
> the following Content Security Policy directive: "connect-src 'self' http://localhost:5078/api/"`
> and
> `Access to fetch at 'http://localhost:5078/api/sessions/login' from origin
> 'http://127.0.0.1:4200' has been blocked by CORS policy`

### Thinking
Two unrelated issues in the same log. The CSP warning is Chrome DevTools trying to fetch a
source map for a CDN script only because DevTools is open — it never happens for a real user,
and isn't a reason to loosen the CSP for a CDN host used only for a dev-tooling convenience.
The CORS error is more informative: `127.0.0.1:4200` and `localhost:4200` are different browser
origins even though they reach the same server, and the backend's CORS policy
(`Frontend:BaseUrl`) was correctly configured to allow only `http://localhost:4200`. The error
meant the user had opened the app at `127.0.0.1:4200`, not that anything was misconfigured.

### Response
Explained both findings and the fix: use `http://localhost:4200`, not `127.0.0.1:4200`, when
testing. Recommended explicitly against widening the backend's CORS policy to accept both
origins just to paper over this. Left the jsDelivr CSP source-map warning alone as a harmless
DevTools artifact.

---

## Turn 12

### Prompt
> Please add the note for the localhost.

### Response
Added a blockquote to `README.md`, right after the existing CORS-origin explanation, noting
that the app must be opened at `http://localhost:4200` and not `http://127.0.0.1:4200` — the
latter causing every API call to be blocked by CORS — and explaining that the port link shown
by Docker Desktop opens `127.0.0.1` because the port is published on the loopback interface.

---

## Turn 13

### Prompt
> Do you think this feature branch is ready to merge?

### Thinking
Re-verified the branch's real, pushed state at its latest commit: fully synced with its
remote, no uncommitted changes; a fresh test-merge into `origin/dev` in a disposable clone
completed cleanly with no manual conflict resolution; both Angular build configurations still
produce a CSP that exactly matches their bundle's compiled-in API URL; the README now carries
the `localhost` vs `127.0.0.1` note.

### Response
Yes, with one thing to confirm before opening the PR: capture a healthy `docker compose ps`
from an actual `docker compose up -d --build --wait` run (not possible from this sandbox,
since container registries aren't reachable from here) and do a real browser login test at
`http://localhost:4200`. Listed the non-blocking known limitations worth mentioning in the PR
description: image tags don't reflect the Angular build configuration or minification state,
base images are pinned by tag rather than digest with no vulnerability scan yet, and Chart.js
is loaded from a CDN without a Subresource Integrity hash.
