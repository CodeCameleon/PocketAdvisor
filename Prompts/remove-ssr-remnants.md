# Remove SSR Remnants

**Date:** 2026-10-19

---

## Turn 1

### Prompt
> I want you to fix the SSR remnants audit findings.

### Thinking
Read `Frontend/package.json`, `Frontend/tsconfig.app.json`, `angular.json`, the Dockerfile,
and `Documentation/AUDIT.md` to understand the full scope of SSR leftovers.

The audit finding only mentioned the `serve:ssr:PocketAdvisor.Client` npm script, but
investigation revealed two additional SSR scaffolding remnants:
- `@types/node` in `devDependencies` — only needed for Node.js server code, not a browser SPA
- `"types": ["node"]` in `tsconfig.app.json` — pollutes browser code's type-checking with Node.js globals

`angular.json` confirmed no SSR builder configuration: the `build` architect uses
`@angular/build:application` with only a browser entry point. The Dockerfile confirmed
the same: it copies only `dist/PocketAdvisor.Client/browser` to nginx.

**Plan:**
1. Remove `serve:ssr:PocketAdvisor.Client` script from `package.json`
2. Remove `@types/node` from `devDependencies` in `package.json`
3. Change `"types": ["node"]` to `"types": []` in `tsconfig.app.json`
4. Update `package-lock.json` to match

**Lockfile approach:** Running `npm install --package-lock-only` with the sandbox's npm v10
(project pins npm@11.13.0) rewrote ~90 unrelated lines — stripped `libc` fields from optional
native deps, changed optional/peer flags on unrelated packages. To avoid a noisy diff,
restored `package-lock.json` from `git show HEAD:Frontend/package-lock.json`, then edited
only the three necessary entries using a Python script with exact string matching and
assertion guards:
- Removed `"@types/node": "^20.17.19"` from the top-level `devDependencies` block
- Added `"optional": true, "peer": true` to the `node_modules/@types/node` entry
- Added `"optional": true, "peer": true` to the `node_modules/undici-types` entry

**Verification:** TypeScript type-check (`tsc --noEmit`) passed. Full `ng build` in a
scratch environment passed (only a pre-existing size budget warning for `item-detail.css`
at +49 bytes over 4 kB, unrelated to this change). The build output contained only a
`browser/` folder — no `server/` folder — confirming SSR is fully removed.

**AUDIT.md:** Added a **Status: Resolved** paragraph to the SSR finding.

### Response
Fixed all SSR scaffolding remnants across four files:

**`Frontend/package.json`** — removed the `serve:ssr:PocketAdvisor.Client` script and
the `@types/node` devDependency.

**`Frontend/tsconfig.app.json`** — changed `"types": ["node"]` to `"types": []`. No
source file in `src/` uses Node.js APIs, so the Node type namespace should not be
available in browser code's type-checking.

**`Frontend/package-lock.json`** — updated to match: removed the `@types/node` entry
from the top-level `devDependencies` block; marked the `node_modules/@types/node` and
`node_modules/undici-types` entries as `optional` + `peer` (they remain in the lockfile
because vitest/build tools depend on them as peer deps).

**`Documentation/AUDIT.md`** — added a Status: Resolved note to the SSR finding,
documenting what was removed and why.

`angular.json` and the Dockerfile were read but not modified — neither contained SSR
configuration.

---

## Turn 2

### Prompt
> Please move the changes to a feature branch.

### Thinking
The project uses `feature/<topic>` branches off `dev`. The current branch was `dev`.
Project rules prohibit creating git commits, so the changes must live as unstaged
modifications on the new branch.

Ran: `git switch -c feature/remove-ssr-remnants` from `D:\GitHub\PocketAdvisor`.

### Response
Created branch `feature/remove-ssr-remnants` from `dev`. All four modified files carried
over as unstaged changes:

```
Switched to a new branch 'feature/remove-ssr-remnants'
 M Documentation/AUDIT.md
 M Frontend/package-lock.json
 M Frontend/package.json
 M Frontend/tsconfig.app.json
```

`dev` is unchanged. When ready to commit, a suitable message would be:
"Removed the SSR remnants from the frontend."
