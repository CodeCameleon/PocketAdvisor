# Dockerfile Linting Implementation

**Date:** 2026-11-27

---

## Turn 1

### Prompt

> Now I want you to do the dockerfile linting on a new feature branch.

### Thinking

Surveyed the repo: two Dockerfiles (`Backend/Dockerfile`, `Frontend/Dockerfile`), a
`docker-compose.yml`, and no existing hadolint configuration.

**Plan:**
1. Create branch `feature/dockerfile-linting`.
2. Run hadolint on both Dockerfiles to identify issues.
3. Create `.hadolint.yaml` with appropriate configuration.
4. Fix any real violations in the Dockerfiles.
5. Update `README.md` and `Documentation/DOCUMENTATION.md`.

Ran hadolint — findings:

- `Backend/Dockerfile` — `DL3008` (apt-get without pinned version for `curl`), `DL3026`
  (image not from a trusted registry — base images need to be listed as trusted),
  `DL3049` (missing OCI labels), `DL3048` (style: label schema).
- `Frontend/Dockerfile` — `DL3026`, `DL3049`, `DL3048`.

For `DL3008` the user was offered three options:
1. Pin `curl` to a specific version.
2. Add a `# hadolint ignore=DL3008` exception with a stated reason.
3. Remove curl (since it was only used for the health check).

### Response

Created branch `feature/dockerfile-linting`.

hadolint reported four rule violations across the two Dockerfiles. Here is what each means and
the options available:

**DL3026** — Base images must come from a trusted registry. Neither `mcr.microsoft.com` nor
`docker.io` is in a trust list yet; both need to be added to `.hadolint.yaml`.

**DL3049 / DL3048** — OCI label schema. The Dockerfiles have no `LABEL` instructions; hadolint
wants at minimum `org.opencontainers.image.title`, `org.opencontainers.image.version`, and
`org.opencontainers.image.source`.

**DL3008 (Backend only)** — `apt-get install curl` without a pinned version. Three options:

| Option | Trade-off |
|---|---|
| Pin `curl` to a specific version | Most precise, but the pinned version can go stale |
| `# hadolint ignore=DL3008` with a reason | Keeps curl, suppresses the warning explicitly |
| **Remove curl (Recommended)** | Eliminates the dependency; the health check must be rebuilt |

Which would you prefer for the curl issue?

---

## Turn 2

### Prompt

> Remove curl (Recommended)

### Thinking

The user chose to remove curl. The health check in `docker-compose.yml` used curl to call
`/health`. Replaced it with a pure-bash `/dev/tcp` solution:

```
test: ["CMD", "bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && head -n 1 <&3 | grep -q ' 200 '"]
```

This opens a TCP connection on fd 3, sends a minimal HTTP/1.1 request, reads the first response
line, and checks for ` 200 `.

Also created `.hadolint.yaml` with:
- `failure-threshold: style` — fail on style warnings and above.
- `trustedRegistries` — `mcr.microsoft.com` and `docker.io`.
- `label-schema` — three OCI fields (`title`, `version`, `source`).
- `strict-labels: true` — any unlisted label is an error.

Removed the curl `RUN` layer from `Backend/Dockerfile` runtime stage entirely. Added `LABEL`
instructions to both Dockerfiles.

Also asked the user whether to pin the hadolint image by digest in `README.md`.

### Response

Done. Here is what changed:

**`Backend/Dockerfile`** — removed the `apt-get install curl` layer from the runtime stage.
The image is now smaller and has no shell utilities in production.

**`docker-compose.yml`** — replaced the curl health check with a bash `/dev/tcp` check:

```yaml
test: ["CMD", "bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && head -n 1 <&3 | grep -q ' 200 '"]
```

**`.hadolint.yaml`** (new file):

```yaml
failure-threshold: style
trustedRegistries:
  - mcr.microsoft.com
  - docker.io
label-schema:
  org.opencontainers.image.title: text
  org.opencontainers.image.version: text
  org.opencontainers.image.source: url
strict-labels: true
```

**`README.md`** — added section **4. Dockerfile linting** with the hadolint run command,
an explanation of the config rules, and guidance on inline suppression with `# hadolint ignore=`.

**`Documentation/DOCUMENTATION.md`** — added a "Dockerfile linting | hadolint" row in
section 2.3 Infrastructure.

Should I pin the hadolint image in `README.md` by digest (tag + `@sha256:…`) rather than tag
alone, as the course notes recommend?

---

## Turn 3

### Prompt

> Yes

### Thinking

Fetched the digest for `hadolint/hadolint:v2.14.0` from Docker Hub:
`sha256:27086352fd5e1907ea2b934eb1023f217c5ae087992eb59fde121dce9c9ff21e`

Updated the run command in `README.md` to:

```
docker run --rm -v "${PWD}:/repo:ro" -w /repo \
  hadolint/hadolint:v2.14.0@sha256:27086352fd5e1907ea2b934eb1023f217c5ae087992eb59fde121dce9c9ff21e \
  /bin/hadolint Backend/Dockerfile Frontend/Dockerfile
```

Also updated `Documentation/DOCUMENTATION.md` to mention the digest pin.

The course notes (class 4, note 7) explicitly warn against trusting image tags alone because
they are mutable; pinning by digest guarantees the exact binary is used in every run.

### Response

Updated. The `README.md` run command now references the image by both tag and digest:

```
hadolint/hadolint:v2.14.0@sha256:27086352fd5e1907ea2b934eb1023f217c5ae087992eb59fde121dce9c9ff21e
```

This matches the course recommendation: a tag alone is mutable and can silently change between
runs, while the digest pins the exact image layer set. `Documentation/DOCUMENTATION.md` updated
to note the digest pin as well.

**Suggested commit messages** (to be committed by you, in the style of earlier commits):

1. `Added the hadolint configuration for the Dockerfiles.` — covers `.hadolint.yaml` and the
   `LABEL` instructions.
2. `Removed curl from the backend image and moved the health check to bash.` — covers
   `Backend/Dockerfile` and `docker-compose.yml`.
3. `Updated the documentation for the Dockerfile linting.` — covers `README.md` and
   `Documentation/DOCUMENTATION.md`.

---

## Turn 4

### Prompt

> *(User attached hadolint run output showing no findings and digest* `sha256:27086352…`*, plus*
> `docker compose up -d --build --wait api` *output showing* `api-1 Healthy`*)*

### Response

Both checks out:

- **hadolint** — no findings at all; the config and Dockerfile changes satisfy every rule.
- **Health check** — the `/dev/tcp` replacement works correctly; the API container reaches
  `Healthy` without curl present.

The branch is clean:

```
c46fce1 Added the hadolint configuration for the Dockerfiles.
37788b2 Removed curl from the backend image and moved the health check to bash.
3c0301f Updated the documentation for the Dockerfile linting.
```

Diff vs `dev`: 5 files, +39/−5. Ready to merge when you are.

---

## Turn 5

### Prompt

> Are we ready to merge? If yes, then export.

### Thinking

Ran a final check: `git status -sb`, `git log dev..HEAD`, hadolint — all clean. Three commits,
no lock files, HADOLINT_OK. Conclusion: ready to merge.

Invoked the `anthropic-skills:pocketadvisor-export` skill to write this file.

### Response

Exported to `Prompts/dockerfile-linting.md`.
