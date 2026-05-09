# Admin Global Categories Page

---

> I need a new page where the admins can manage the global categories. You can reference the personal categories page for that.

**Claude:** Explored the project structure, reading the personal categories module (category-list, create/rename/delete dialogs, CategoryService, routing, navbar) and the backend CategoryController to understand the existing patterns.

Created the following new files:

**`src/app/core/guards/admin.ts`** — Route guard that decodes the stored JWT, reads the `role` claim (supporting the standard ASP.NET Core claim URI as a fallback), and redirects non-Administrators to `/auth/login`.

**`src/app/admin/admin-module.ts`** + **`admin-routing-module.ts`** — Lazy-loaded admin module mounted at `/admin`, with a child route `/admin/categories` pointing to `GlobalCategoryList`.

**`src/app/admin/global-category-list/`** (`.ts`, `.html`, `.css`) — Admin page mirroring the personal categories list. Calls the shared `getCategories()` endpoint and filters to `isGlobal === true`. Every card shows the rename/delete menu (no personal-category branching needed). Empty state uses the `public` icon instead of `label_outline`.

**`src/app/admin/create-global-category-dialog/`** — Calls `createGlobalCategory()`. Uses `public` as the input suffix icon instead of `label_outline`.

**`src/app/admin/update-global-category-name-dialog/`** — Calls `updateGlobalCategoryName()`. Same structure as the personal rename dialog.

**`src/app/admin/delete-global-category-dialog/`** — Calls `deleteGlobalCategory()`. Confirmation text includes "This will remove it from all users."

Modified **`app.routes.ts`** — Added the `/admin` lazy route protected by `adminGuard`.

Modified **`shared/navbar/navbar.ts`** + **`navbar.html`** — Added an `isAdmin` computed signal (same JWT decode logic as the guard) and an "Admin" nav link with the `admin_panel_settings` icon, visible only to administrators.

---

> Good, but I would like you to also port out the 400 error messages on the global category deletion

**Claude:** Investigated the backend: `DeleteGlobalCategoryAsync` returns a 400 (via `HandleFailure` → `BadRequest`) when the category still has associated transactions, with the message _"The category cannot be deleted because it still has transactions associated with it"_ serialised into `ValidationProblemDetails.errors[""]` (the empty-string key used for global/non-field errors).

The existing `resolveErrorMessage` in the delete dialog only handled 404 and fell through to a generic string for everything else. Updated **`delete-global-category-dialog.ts`** to:

- Define a local `ValidationProblemDetails` interface
- Add a `status === 400` branch that reads `errors['']?.[0]` from the response body and surfaces that message directly in the error banner
- Retain the 404 hardcoded message and the generic fallback for any other status
