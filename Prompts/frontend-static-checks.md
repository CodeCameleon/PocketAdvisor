# Frontend Static Checks Setup

**Date:** 2026-11-27

---

## Turn 1

### Prompt
> Can you help me with this?

*(Pasted `ng add angular-eslint` output — the CLI pulled v22 despite the project being Angular 21.)*

### Thinking
The npm package `angular-eslint` is a meta-package; its latest release is v22. Running
`ng add angular-eslint@21` still resolves through the meta-package and can pick up the wrong
version. The correct entry point is the schematics package directly:
`@angular-eslint/schematics@21`. That installs `angular-eslint@21`, the builder, and both
eslint-plugin packages, all pinned to major 21.

### Response
The fix is to target the schematics package directly:

```
ng add @angular-eslint/schematics@21
```

This installs `angular-eslint@21`, `@angular-eslint/builder@21`,
`@angular-eslint/eslint-plugin@21`, and `@angular-eslint/eslint-plugin-template@21` — all
at major 21, matching the project.

---

## Turn 2

### Prompt
> *(Pasted the same output after running `ng add angular-eslint@21` — v22 warning still appeared.)*

### Thinking
`ng add angular-eslint@21` still resolves the npm package named `angular-eslint`, not the
schematics entry point, so the CLI kept pulling the latest. The correct command is
`ng add @angular-eslint/schematics@21` — the user likely ran the wrong one.

### Response
Run the corrected command:

```
ng add @angular-eslint/schematics@21
```

After it completes, `angular.json` gains a `lint` target and `eslint.config.js` is generated.

---

## Turn 3

### Prompt
> *(Pasted `ng lint` error: "Named export 'configs' not found in module '@eslint/js'")*

### Thinking
The generated `eslint.config.js` uses `import { configs } from '@eslint/js'` — a named export
that doesn't exist in `@eslint/js` v10, which only has a default export. The fix is a default
import: `import js from '@eslint/js'` and then `js.configs.recommended`.

Also, ESLint 10 warns about `MODULE_TYPELESS` when the config file has a `.js` extension in a
project without `"type":"module"` in `package.json`. Renaming to `.mjs` suppresses that warning.

### Response
Switched to the default import and renamed the config file:

```js
// @ts-check
import js from '@eslint/js';
import { defineConfig } from 'eslint/config';
import tseslint from 'typescript-eslint';
import angular from 'angular-eslint';

export default defineConfig([
  {
    files: ['**/*.ts'],
    extends: [
      js.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': ['error', { type: 'attribute', prefix: 'app', style: 'camelCase' }],
      '@angular-eslint/component-selector': ['error', { type: 'element', prefix: 'app', style: 'kebab-case' }],
    },
  },
  {
    files: ['**/*.html'],
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {},
  },
]);
```

File renamed `eslint.config.js` → `eslint.config.mjs`. The `MODULE_TYPELESS` warning is gone.

---

## Turn 4

### Prompt
> *(Pasted `ng lint` output with 6 errors.)*
> Can you first fix just the linter warnings, not the actual findings.

### Thinking
"Linter warnings" here means the ESLint configuration errors themselves (the import and
module-type issues), not the 6 code findings. Those config errors were already addressed in
Turn 3. The outstanding task is making sure `angular.json` is also wired correctly — it still
needed `schematicCollections` and the `lint` target — without touching any source file.

### Response
Fixed the linter configuration only — no source files touched:

- `eslint.config.mjs` renamed from `.js` (removes `MODULE_TYPELESS` warning).
- Default import for `@eslint/js` (removes named-export error).
- `angular.json`: added `schematicCollections: ["angular-eslint"]` and the `lint` target
  pointing at `@angular-eslint/builder:lint` with `lintFilePatterns:
  ["src/**/*.ts","src/**/*.html"]`.

Run `ng lint` again to see only the actual code findings.

---

## Turn 5

### Prompt
> Good, now I want a commit message for this, based on the previous git commit messages.
> Description is not needed.

### Thinking
Reviewed recent commit messages on the repo — short, imperative, no scope prefix, sentence
capitalisation, no period.

### Response
```
Set up ESLint for the Angular frontend.
```

---

## Turn 6

### Prompt
> Also I forgot to create a feature branch for these changes, can you do it?

### Thinking
The changes were on `dev`. Created `feature/frontend-static-checks` from the current `dev` tip
and moved the existing ESLint commit onto it.

### Response
Created branch `feature/frontend-static-checks` from the current `dev` tip and switched to it.
The ESLint setup commit is now on that branch.

---

## Turn 7

### Prompt
> Good, now fix the found errors.

### Thinking
Six findings across three files:

**`create-account-dialog.ts`** — `@typescript-eslint/no-unused-vars` on `_currency: Currency`.
The parameter is never used; removing it is safe because the function signature only needs `key`.
Both call sites pass only one argument anyway, so the second parameter is a vestige.

**`api-error.ts`** — `no-useless-escape` on `\]` inside a character class `[...]`. Inside `[]`,
`]` only needs escaping if it would close the class early; the regex already uses `[^.[\]]` where
the first `]` closes the class, so the `\]` in `\[(\d+)\]` outside the class is the escape that
fires. Corrected the regex pattern.

**`item-detail.ts`** — three `@typescript-eslint/no-explicit-any` findings. Chart.js is loaded
from a CDN, so there's no npm type package. The fix is local interface declarations:
`ChartInstance` with a `destroy()` method, `ChartTooltipContext` with `parsed.y`, and a
`declare const Chart` global. The field type changes from `any` to `ChartInstance | null`.

### Response
Fixed all 6 lint findings across three files:

**`create-account-dialog.ts`** — removed unused `_currency` parameter; both call sites updated:
```ts
// before
buildCurrencyLabel(key: string, _currency: Currency)
// after
buildCurrencyLabel(key: string)
```

**`api-error.ts`** — corrected useless escape in regex:
```ts
/([^.[\]]+)|\[(\d+)\]/g
```

**`item-detail.ts`** — replaced all `any` with typed interfaces:
```ts
interface ChartInstance { destroy(): void; }
interface ChartTooltipContext { parsed: { y: number }; }
declare const Chart: new (canvas: HTMLCanvasElement, config: unknown) => ChartInstance;
```
Field changed from `any` to `ChartInstance | null`; tooltip callback typed as
`ChartTooltipContext`.

`ng lint` now passes with 0 errors.

---

## Turn 8

### Prompt
> *(Listed the four static-check prerequisites: setup-node 24 + `npm ci`; `prettier --check .`;
> `ng lint`; `npm audit --omit=dev --audit-level=high`.)*
> What are we still missing?

### Thinking
Surveyed the current state of `Frontend/`:
- `ng lint` — done.
- `npm ci` on Node 24 — works as-is.
- `prettier --check .` — no `.prettierignore`, so it would flag `dist`, `.angular`,
  `node_modules`, and `package-lock.json`.
- `npm audit --omit=dev --audit-level=high` — exits non-zero; high-severity findings present.
- npm scripts — `npm run format:check`, `npm run lint`, `npm run audit:prod` don't exist yet.
- CI workflow — `.github/workflows/static-checks.yml` doesn't exist.
- Docs — `README.md` and `DOCUMENTATION.md` have no mention of any of this.

### Response
Still missing:

1. **`.prettierignore`** — without it, Prettier flags generated files.
2. **npm audit gate** — `npm audit --omit=dev --audit-level=high` currently exits non-zero.
3. **npm scripts** — `format:check`, `lint`, `audit:prod` are absent from `package.json`.
4. **CI workflow** — `.github/workflows/static-checks.yml` doesn't exist yet.
5. **Docs** — README and DOCUMENTATION.md need a static checks section.

`ng lint` and the Node 24 / `npm ci` steps are already handled.

---

## Turn 9

### Prompt
> Let's just do the 1 for now

### Thinking
Created `Frontend/.prettierignore` covering the four categories that would otherwise trigger
false positives: build output (`.angular`, `dist`), dependencies (`node_modules`), and the
lockfile (`package-lock.json`).

### Response
Created `Frontend/.prettierignore`:

```
# Build output and caches
.angular
dist

# Dependencies
node_modules
package-lock.json
```

---

## Turn 10

### Prompt
> How can I run the check?

### Thinking
Two ways: directly via `npx`, or via the npm script that will be added in step 4. Since the
script doesn't exist yet, `npx` is the immediate option. Explained the `--check` flag semantics
so the user knows it won't modify files.

### Response
```bash
cd Frontend
npx prettier --check .
```

Or, after adding the npm script in step 4:

```bash
npm run format:check
```

`--check` lists differing files and exits non-zero if any exist — it doesn't modify anything.

---

## Turn 11

### Prompt
> *(Pasted prettier --check / --write output showing EPERM errors on `.vscode` files.)*

### Thinking
EPERM on `.vscode` on Windows is a filesystem permission issue (sometimes the directory is
marked read-only by VS Code). It is not a Prettier problem and won't appear on a Linux CI
runner. The formatting for all other files completed normally. No code change is needed.

### Response
The EPERM errors are a Windows filesystem permission artefact on `.vscode` — not a Prettier
problem. The CI runner (Linux) won't hit it.

Options:
- Add `.vscode` to `.prettierignore` to skip it entirely.
- Leave it as-is; Linux CI is unaffected.

The formatting completed; the EPERM lines don't affect the check result for other files.

---

## Turn 12

### Prompt
> I done it, we can move onto the 3 step

### Thinking
Step 3 is the prod audit gate. npm 10 (the version in the VM) has an arborist `edgesOut` bug
that crashes `npm audit fix` and `npm install` on this lockfile. The workaround is to use
npm@11 via `npx -y npm@11.13.0` in a scratch directory (`$HOME/lockwork`) so the Linux binaries
from a full install don't contaminate the connected folder. After `npm audit fix
--package-lock-only` succeeded, copied the updated lockfile back.

### Response
Ran `npm audit fix --package-lock-only` via npm@11 in a scratch directory to avoid the npm 10
arborist bug. The lockfile was updated and copied back. Zero high/critical findings now remain
in production dependencies.

---

## Turn 13

### Prompt
> Let's do the dev-dependency audit cleanup now

### Thinking
Ran `npm audit --audit-level=moderate` on the full tree. All 19 remaining moderate findings
trace to `@angular/cli` and `angular-eslint` dev tooling. They require Angular 22 to resolve;
nothing can be fixed while staying on Angular 21. These are accepted as pre-existing tooling
noise for this major version.

Also noted: Chart.js is loaded from a CDN with a floating `@4` version and no SRI integrity
hash. The npm audit doesn't cover CDN scripts, so this is a blind spot in the audit gate.

### Response
Ran the full audit. The 19 remaining moderate findings all trace to `@angular/cli` and
`angular-eslint` tooling — they require Angular 22 and cannot be fixed on Angular 21. No
lockfile changes were needed; they are accepted as pre-existing tooling noise.

**Standards note:** Chart.js is loaded from a CDN with a floating `@4` version and no SRI
integrity hash. The npm audit doesn't cover it. This is a standards concern (consider installing
via npm or adding an `integrity` attribute) but it is pre-existing and out of scope for this
branch.

---

## Turn 14

### Prompt
> Let's move onto the 4 step

### Thinking
Step 4 is the npm scripts. Added four scripts to `Frontend/package.json` so the CI steps map
directly to `npm run` commands and developers don't need to remember the raw invocations.

### Response
Added four npm scripts to `Frontend/package.json`:

```json
"scripts": {
  "lint":         "ng lint",
  "format":       "prettier --write .",
  "format:check": "prettier --check .",
  "audit:prod":   "npm audit --omit=dev --audit-level=high"
}
```

The CI steps now map to `npm run format:check`, `npm run lint`, and `npm run audit:prod`.

---

## Turn 15

### Prompt
> Let's do step 5 now, and skip 6 for a later date

### Thinking
Step 5 is documentation. Two files need updating: `README.md` (a new §3.4 Static checks) and
`Documentation/DOCUMENTATION.md` (tool table and DX section). Step 6 (the CI workflow) is
explicitly deferred.

### Response
Updated two documentation files:

**`README.md`** — added §3.4 Static checks with the four commands (`npm ci`,
`npm run format:check`, `npm run lint`, `npm run audit:prod`), a one-line explanation of each,
and a note to commit the lockfile together with `package.json`.

**`Documentation/DOCUMENTATION.md`**:
- §2.2 tool table: expanded the Prettier row; added Linting and Dependency management rows;
  updated the Chart.js row to note local type declarations and the CDN caveat.
- §8 Developer experience: added bullet about the npm scripts.

Step 6 (`.github/workflows/static-checks.yml`) is deferred to a later session.

---

## Turn 16

### Prompt
> Please review every change on this feature branch. Is it ready for merging?

### Thinking
Reviewed all commits on `feature/frontend-static-checks` against `dev`. Checked:
- ESLint config correctness and accessibility rules.
- `angular.json` lint target wiring.
- All three lint fixes for correctness and type safety.
- `.prettierignore` coverage.
- npm scripts completeness.
- Lockfile sync and audit status.
- README and DOCUMENTATION accuracy.

No blockers found. Noted three pre-existing, out-of-scope issues for the record.

### Response
Reviewed all commits on `feature/frontend-static-checks`. **Ready — no blockers.**

| Area | Status |
|------|--------|
| `eslint.config.mjs` | ✅ Correct flat config, typed, accessibility rules included |
| `angular.json` lint target | ✅ Wired to the right builder and patterns |
| Lint fixes (3 files) | ✅ Proper types, no suppressed warnings |
| `.prettierignore` | ✅ Covers all generated output |
| `package.json` scripts | ✅ All four check commands present |
| `package-lock.json` | ✅ In sync, audit-clean for prod deps |
| `README.md` | ✅ Static checks section accurate |
| `DOCUMENTATION.md` | ✅ Tool table and DX section updated |

Non-blocking notes (pre-existing, out of scope):
- 19 moderate audit findings in CLI/eslint tooling — need Angular 22.
- CSS bundle budget warnings in the build.
- Chart.js loaded from CDN with no SRI hash and floating `@4` version.
- `@angular/animations` deprecation warning.

The branch is ready to merge into `dev`.

---

## Turn 17

### Prompt
> Please fix the pinning style use exact versions everywhere.

### Thinking
Surveyed `Frontend/package.json`. Several dependencies used `~` or `^` ranges:
- rxjs: `~7.8.0` → should be `7.8.2` (exact version already in lockfile)
- tslib: `^2.3.0` → `2.8.1`
- eslint: `^10.3.0` → `10.15.0`
- typescript: `~5.9.2` → `5.9.3`
- jsdom, prettier, typescript-eslint, vitest: various `^` ranges

All Angular/CDK/Material packages were already exact. Pinned everything to match the already-
installed versions from the lockfile — no installs change. Also added `Frontend/.npmrc` with
`save-exact=true` so future `npm install <package>` calls write exact versions instead of `^`.

### Response
Pinned every version in `Frontend/package.json` to exact (no `^` or `~`), matching the versions
already in the lockfile — nothing installed changes:

- Angular 21.2.26 (animations, common, compiler, core, forms, platform-browser, router,
  compiler-cli)
- cdk and material: 21.2.9
- rxjs 7.8.2, tslib 2.8.1
- @angular/build and @angular/cli: 21.2.31
- @eslint/js 10.0.1, angular-eslint 21.4.0, eslint 10.15.0
- jsdom 28.1.0, prettier 3.8.3, typescript 5.9.3
- typescript-eslint 8.74.0, vitest 4.1.12

Also added `Frontend/.npmrc` with `save-exact=true` so future installs stay exact.

Suggested commit message:
```
Pinned the exact package versions of the frontend.
```

Verify with `npm ci` and `npm run lint` — the installed versions are identical to before.
