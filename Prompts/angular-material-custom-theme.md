# Angular Material Custom Theme

**Date:** 2026-05-03

---

## Turn 1

### User

> I have added the angular material to the frontend. Can you configure it to use the colors from the styles.css file and not the default material theme?

### Assistant Thinking

- Read memory for frontend guidelines: always use Angular Material components and never use colors outside the global palette.
- Scanned the project for CSS/SCSS files; found `Frontend/src/styles.css` (the palette source) and `Frontend/src/material-theme.scss` (the existing M3 theme file).
- Read `styles.css`: confirmed a `:root` block defining CSS custom properties across six colour groups — Primary (cobalt blue), Secondary (indigo-violet), Tertiary (teal-cyan), Error (vivid crimson), Neutral, and Neutral Variant — each with tonal stops at 50/100/200/400/600/800/950.
- Read `material-theme.scss`: confirmed it uses `mat.theme()` with `mat.$azure-palette` (primary) and `mat.$blue-palette` (tertiary) — both default Material palettes with no connection to the project colours.
- Read `angular.json`: confirmed build styles order is `["src/material-theme.scss", "src/styles.css"]`, so the SCSS is compiled first then the CSS variables are available globally.
- Key architectural decision: Angular Material M3's `mat.theme()` accepts custom palette maps (Sass maps of tonal stop → hex). The project's hex values in `styles.css` can be directly mapped to M3 tonal stops (0–100 scale). Each palette needs stops at 0 and 100 as anchors, plus the brand hex values placed at the closest corresponding tone.
- Mapped each brand shade to an M3 tonal value: `*-950` → tone 10, `*-800` → tone 20, `*-600-base` → tone 40, `*-400` → tone 60, `*-200` → tone 80, `*-100` → tone 90. Interpolated intermediate tones (25, 30, 35, 50, 70, 95, 98, 99) to give Material a smooth gradient.
- Six palettes needed: primary, secondary, tertiary, error, neutral, neutral-variant — all supported by `mat.theme()`.

### Assistant Response

Replaced the contents of `material-theme.scss` — swapped out the two default Material palettes for six custom M3 tonal palette maps built directly from the project's hex values.

---

#### Modified: `Frontend/src/material-theme.scss`

```scss
// Angular Material M3 theme wired to the project's brand palette from styles.css.
// Primary:   deep cobalt blue    (#2355D8 base)
// Secondary: electric indigo-violet (#5E38F5 base)
// Tertiary:  teal-cyan pop        (#0DAAA1 base)
// Error:     vivid crimson        (#E31B26 base)
// Learn more: https://material.angular.dev/guide/theming
@use '@angular/material' as mat;

// ---------------------------------------------------------------------------
// Custom palettes built from the CSS-variable colour system in styles.css.
// M3 palettes need tonal values 0–100. We map the five project shades to the
// closest standard tonal stops and let Material interpolate the rest.
// ---------------------------------------------------------------------------

$pocket-primary-palette: (
  0:   #000000,
  10:  #071550,   // --color-primary-950
  20:  #122D8A,   // --color-primary-800
  25:  #1A3CAA,
  30:  #1D45BE,
  35:  #204DCB,
  40:  #2355D8,   // --color-primary-600-base
  50:  #3869E8,
  60:  #4E7CF6,   // --color-primary-400
  70:  #6F96F8,
  80:  #8FABF5,   // --color-primary-200
  90:  #C7D7FF,   // --color-primary-100
  95:  #E0EBFF,
  98:  #F3F6FF,
  99:  #F9FAFF,
  100: #FFFFFF,
);

$pocket-secondary-palette: (
  0:   #000000,
  10:  #150A52,   // --color-secondary-950
  20:  #341B9E,   // --color-secondary-800
  25:  #422AB8,
  30:  #4D30CC,
  35:  #5534E0,
  40:  #5E38F5,   // --color-secondary-600-base
  50:  #7050F6,
  60:  #8A6EFF,   // --color-secondary-400
  70:  #A08BFF,
  80:  #B8A8FF,   // --color-secondary-200
  90:  #DDD5FF,   // --color-secondary-100
  95:  #EEE9FF,
  98:  #F8F6FF,
  99:  #FBFAFF,
  100: #FFFFFF,
);

$pocket-tertiary-palette: (
  0:   #000000,
  10:  #02312E,   // --color-tertiary-950
  20:  #066B66,   // --color-tertiary-800
  25:  #088580,
  30:  #099690,
  35:  #0BA099,
  40:  #0DAAA1,   // --color-tertiary-600-base
  50:  #12C2B8,
  60:  #1DDCD0,   // --color-tertiary-400
  70:  #4CE5DB,
  80:  #78F2E8,   // --color-tertiary-200
  90:  #BDFBF5,   // --color-tertiary-100
  95:  #DDFDFB,
  98:  #F2FFFE,
  99:  #F8FFFF,
  100: #FFFFFF,
);

$pocket-error-palette: (
  0:   #000000,
  10:  #430508,   // --color-error-950
  20:  #8C0E14,   // --color-error-800
  25:  #A81018,
  30:  #C0131D,
  35:  #D41721,
  40:  #E31B26,   // --color-error-600-base
  50:  #F03038,
  60:  #FF5C65,   // --color-error-400
  70:  #FF8088,
  80:  #FFAAB0,   // --color-error-200
  90:  #FFD6D8,   // --color-error-100
  95:  #FFECEC,
  98:  #FFF8F8,
  99:  #FFFBFB,
  100: #FFFFFF,
);

$pocket-neutral-palette: (
  0:   #000000,
  10:  #0C0E18,   // --color-neutral-950
  20:  #1E2130,   // --color-neutral-800
  25:  #282C3E,
  30:  #32374A,
  35:  #3C4256,
  40:  #4A4F5E,   // --color-neutral-600-base
  50:  #636878,
  60:  #7D8293,
  70:  #979BAE,
  80:  #B1B5C6,
  90:  #E2E4E9,   // --color-neutral-100
  95:  #F4F5F7,   // --color-neutral-50
  98:  #F9FAFB,
  99:  #FBFCFD,
  100: #FFFFFF,
);

$pocket-neutral-variant-palette: (
  0:   #000000,
  10:  #0D0F28,   // --color-neutral-variant-950
  20:  #1E2145,   // --color-neutral-variant-800
  25:  #272A57,
  30:  #30346A,
  35:  #383D7C,
  40:  #3D4473,   // --color-neutral-variant-600-base
  50:  #525A90,
  60:  #7A82B0,   // --color-neutral-variant-400
  70:  #9AA1C7,
  80:  #C2C6E0,   // --color-neutral-variant-200
  90:  #E0E2F0,   // --color-neutral-variant-100
  95:  #EFF0F8,
  98:  #F8F8FC,
  99:  #FBFBFE,
  100: #FFFFFF,
);

html {
  height: 100%;
  @include mat.theme(
    (
      color: (
        primary:         $pocket-primary-palette,
        secondary:       $pocket-secondary-palette,
        tertiary:        $pocket-tertiary-palette,
        error:           $pocket-error-palette,
        neutral:         $pocket-neutral-palette,
        neutral-variant: $pocket-neutral-variant-palette,
      ),
      typography: Roboto,
      density: 0,
    )
  );
}

body {
  color-scheme: light;
  background-color: var(--mat-sys-surface);
  color: var(--mat-sys-on-surface);
  font: var(--mat-sys-body-medium);
  margin: 0;
  height: 100%;
}
```

---

### Design Decisions

- **Tonal mapping strategy:** M3 palettes use a 0–100 tonal scale. The project's shades map naturally: `*-950` → tone 10 (near-black), `*-800` → tone 20, `*-600-base` → tone 40 (the "key" colour Material uses for primary containers and main interactions), `*-400` → tone 60, `*-200` → tone 80, `*-100` → tone 90. This preserves the visual intent of the original palette.
- **All six palette roles populated:** M3 requires `neutral` and `neutral-variant` for surface, background, border, and divider tokens. Without them, Material falls back to grey defaults. Supplying them ensures every `--mat-sys-*` variable stays on-brand.
- **No per-component overrides needed:** Because `mat.theme()` derives all `--mat-sys-*` tokens from the palettes, every Material component (buttons, inputs, cards, dialogs, chips, etc.) automatically inherits the brand colours with no additional SCSS.
- **CSS variables in `styles.css` remain unchanged:** The `:root` CSS custom properties (`--color-primary-600-base` etc.) are still available for use in component styles. The Material theme and the CSS variables coexist — Material uses the Sass palette maps at build time; component styles can reference the CSS variables at runtime.

---

## Turn 2

### User

> Export

### Assistant Thinking

- User invoked the Export macro shorthand.
- Retrieved export macro and format reference memories.
- Compiled the full conversation into canonical export format: title derived from the task, date, turns in order, user prompts blockquoted, assistant thinking and response sections, file operation headings, design decisions section.
- Saved to `Prompts\angular-material-custom-theme.md`.

### Assistant Response

Export saved.

---
