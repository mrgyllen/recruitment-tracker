# Visual Design Foundation

## Brand Integration Strategy

recruitment-tracker is an internal tool that belongs in the If Insurance ecosystem. The visual layer follows the If corporate identity (warm browns, cream backgrounds, Nordic minimalism) while the interaction layer delivers the engineering-grade efficiency that makes developers respect it. The "programmer feel" comes from behavior (keyboard shortcuts, split panels, instant feedback, information density), not from visual treatment (no dark backgrounds, no monospace fonts, no terminal aesthetics).

This separation is strategic: corporate stakeholders (Anders, Sara, Erik's line managers) see a trustworthy organizational tool. Engineers (Lina) feel the speed and efficiency in how it responds, not in how it looks. Dark mode is deferred to Growth phase -- shadcn/ui's CSS variable architecture makes it straightforward to add.

## Color System

**All color tokens defined once in Tailwind CSS v4 `@theme` block. No hardcoded hex values in component code.** This single-source approach makes future dark mode implementation trivial: swap `@theme` values under a `.dark` class or `prefers-color-scheme` media query.

**Core palette derived from If Insurance brand identity (if.se):**

| Token | Hex | Usage |
|-------|-----|-------|
| `--color-brand-brown` | `#331e11` | Primary text, headings, high-emphasis content. If's signature dark brown replaces pure black for a warmer, branded feel. |
| `--color-bg-base` | `#faf9f7` | Page background, candidate list background, overview section. If's warm cream -- not stark white, distinctly Nordic. |
| `--color-bg-surface` | `#ffffff` | Cards, panels, elevated surfaces. Pure white creates subtle lift against the cream background. **Also used as CV viewer panel background** -- the document reading area must be neutral white to avoid warm tint affecting PDF perception ("paper on desk" effect). |
| `--color-border-default` | `#ede6e1` | Panel dividers, card borders, table row separators. If's warm taupe. |
| `--color-border-subtle` | `#f5f0ec` | Lighter variant for nested borders within surfaces. |
| `--color-interactive` | `#005fcc` | Links, focus rings, active states, selected candidate indicator. If's brand blue for interactive elements. |
| `--color-interactive-hover` | `#004da6` | Darker blue for hover states. |
| `--color-text-secondary` | `#6b5d54` | Secondary text, metadata, timestamps. Warm gray derived from brand brown. **Validate during implementation** -- if it feels muddy at small sizes (13px), consider `#78716c` (Tailwind stone-500) as a cooler alternative that still harmonizes with the warm palette. |
| `--color-text-tertiary` | `#9c8e85` | Placeholder text, disabled states. Lighter warm gray. |

**Interactive states for candidate list:**

| State | Treatment |
|-------|-----------|
| **Default** | Cream background (`#faf9f7`), brown text |
| **Hover** | Subtle warm shift (`#f5f0ec` background) |
| **Selected** | Left border accent in If blue (`#005fcc`, 3px) + subtle blue background tint (`#eff8ff`). Mirrors VS Code's active file indicator pattern. |
| **Focused (keyboard)** | Blue focus ring (2px outline, 2px offset) |

**Semantic status colors (harmonized with warm palette):**

| Token | Hex | Icon | Usage |
|-------|-----|------|-------|
| `--status-pass` | `#1a7d37` | Checkmark (✓) | Pass outcome, positive indicators |
| `--status-pass-bg` | `#ecfdf3` | -- | Pass badge background |
| `--status-fail` | `#c4320a` | X icon (✕) | Fail outcome, error states |
| `--status-fail-bg` | `#fef3f2` | -- | Fail badge background |
| `--status-hold` | `#b54708` | Pause icon (⏸) | Hold outcome, warning states |
| `--status-hold-bg` | `#fffaeb` | -- | Hold badge background |
| `--status-info` | `#005fcc` | Info icon (ℹ) | Informational indicators |
| `--status-info-bg` | `#eff8ff` | -- | Info badge background |
| `--status-stale` | `#b54708` | Clock icon (⏱) | Stale step indicators |

All status colors paired with distinctive icons -- color is never the sole differentiator (WCAG compliance). Status badge backgrounds are tinted variants of the status color for visual grouping without overwhelming the warm palette.

**Contrast compliance:**
- `#331e11` on `#faf9f7`: ratio ~13:1 (exceeds WCAG AAA)
- `#331e11` on `#ffffff`: ratio ~15:1 (exceeds WCAG AAA)
- `#6b5d54` on `#faf9f7`: ratio ~5.5:1 (exceeds WCAG AA)
- `#005fcc` on `#ffffff`: ratio ~5.2:1 (exceeds WCAG AA)
- All status colors on their respective backgrounds: minimum 4.5:1 ratio

## Typography System

**Font stack:**

```css
--font-primary: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
```

Segoe UI is the primary font -- zero loading cost on Windows corporate machines, Microsoft ecosystem default. No web fonts loaded in MVP (all end users are on Windows corporate machines). The fallback to `-apple-system` / `BlinkMacSystemFont` handles developers working on macOS during development (San Francisco -- perfectly fine). Inter is not bundled.

**Type scale (based on Tailwind defaults, adjusted for information density):**

| Level | Size | Weight | Line height | Usage |
|-------|------|--------|-------------|-------|
| **Page title** | 1.5rem (24px) | 600 (semibold) | 1.33 | Recruitment name in header |
| **Section heading** | 1.125rem (18px) | 600 (semibold) | 1.33 | "Overview", step names in pipeline |
| **Card title** | 0.875rem (14px) | 500 (medium) | 1.43 | KPI card labels, column headers |
| **Body** | 0.875rem (14px) | 400 (regular) | 1.57 | Candidate names, reason text, general content |
| **Body small** | 0.8125rem (13px) | 400 (regular) | 1.54 | Timestamps, metadata, secondary info |
| **Caption** | 0.75rem (12px) | 400 (regular) | 1.33 | Badge text, shortcut hints, progress counters |
| **KPI number** | 1.5rem (24px) | 700 (bold) | 1.25 | Overview KPI card primary numbers. Sized for space efficiency -- prominent but not dominating the collapsible overview section. |

**Design rationale:** 14px body text is the density sweet spot -- large enough for comfortable reading during extended screening sessions, small enough for information-dense layouts. The type scale is compressed (24px max) because this is a tool, not a content site. No hero-sized headings.

## Spacing & Layout Foundation

**Spacing scale (Tailwind default 4px base):**

| Token | Value | Usage |
|-------|-------|-------|
| `space-1` | 4px | Inline spacing, icon gaps |
| `space-2` | 8px | Tight component padding (badge, compact button) |
| `space-3` | 12px | Standard component padding (input, card inner) |
| `space-4` | 16px | Section gaps, card padding |
| `space-6` | 24px | Panel gaps, section separation |
| `space-8` | 32px | Major section separation |

**Layout density:** Denser than typical consumer apps, slightly more spacious than VS Code. The target is "professional data application" -- enough whitespace to breathe, not so much that information feels sparse. Candidate list rows use compact padding (8px vertical) for maximum visible candidates without scrolling.

**Grid structure:**

The single-page layout uses CSS Grid at two levels:

1. **Page-level grid (vertical):**

```
[App header - fixed 48px: breadcrumb left, user menu right]
[Overview section - collapsible, auto height]
[Main content area - fills remaining viewport]
```

2. **Main content grid (horizontal, screening mode):**

```
[Candidate list - min 250px, max 400px, resizable] | [CV viewer - flex, white bg] | [Outcome panel - fixed 300px]
```

**App header (48px):** The only persistent chrome. Left side: recruitment name as breadcrumb (Recruitment Name > Step Name). Right side: user avatar/name + sign out. No navigation tabs in the header -- navigation is via candidate list filters and overview click-through. Minimal and permanent.

**Overview section -- expanded vs. collapsed:**

| State | Content |
|-------|---------|
| **Expanded** | KPI cards in a horizontal row (3-4 cards side by side, not stacked) + pipeline breakdown below. Cards show: total candidates, screened count, pending action, stale count. |
| **Collapsed** | Single-line summary bar: "130 candidates · 47 screened · 3 stale". Key numbers inline so that even collapsed, basic status is visible at a glance. Lina gets minimal-footprint status without expanding. |

Collapse state persisted in localStorage. Expand/collapse toggle in the section header.

**Layout constants:**

| Element | Value | Notes |
|---------|-------|-------|
| App header height | 48px | Breadcrumb + user menu. Compact. |
| Minimum viewport width | 1280px | Per architecture spec |
| Candidate list min width | 250px | Enough for name + status badge |
| Candidate list max width | 400px | Prevents over-expansion |
| Outcome panel width | 300px | Fixed -- outcome buttons, reason field, confirm |
| CV viewer | Flexible | Takes remaining space, white background |
| Candidate list row height | 48px | Compact: name + step + status badge on one line |
| Table header height | 40px | Column labels, sort indicators |

**Border radius:** `rounded-md` (6px) as default. Sharper than If's marketing site (12px) for a more functional, tool-like feel. Applied to cards, buttons, inputs, badges. Panels and page-level containers use square corners (0px).

## Accessibility Considerations

**WCAG 2.1 AA compliance (minimum):**

- All text meets 4.5:1 contrast ratio against its background
- Large text (18px+ or 14px bold) meets 3:1 ratio
- Interactive elements have visible focus indicators (If blue `#005fcc`, 2px outline, 2px offset)
- Status indicators use color + icon + shape (never color alone)
- Keyboard navigation for all interactive elements (Radix UI primitives handle this)
- Focus management returns to outcome panel after submission (documented in Step 7 mechanics)

**Color-blind safe status system:**
- Pass: green + checkmark icon + solid badge shape
- Fail: red + X icon + solid badge shape
- Hold: amber + pause icon + solid badge shape
- Stale: amber + clock icon + outlined badge shape (distinct from Hold by shape)

Each status is distinguishable by icon alone, without any color perception.

**Keyboard accessibility:**
- All actions reachable via keyboard (Tab navigation + custom shortcuts)
- Shortcut keys (`1`/`2`/`3`) scoped to avoid conflict with text input
- Focus trapping in dialogs (Radix UI handles this)
- Skip-to-content link for screen readers

**Reduced motion:**
- All animations respect `prefers-reduced-motion` media query
- When reduced motion is preferred: transitions become instant, toast appears without slide-in
