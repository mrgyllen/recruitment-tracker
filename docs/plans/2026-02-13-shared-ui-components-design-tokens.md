# Story 1.4: Shared UI Components & Design Tokens — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Establish the design system foundation with If Insurance brand tokens, shadcn/ui components, and custom shared components (StatusBadge, ActionButton, EmptyState, Toast, SkeletonLoader, ErrorBoundary).

**Architecture:** All work in `web/`. Tailwind CSS v4 `@theme` block for design tokens (no JS config). shadcn/ui components copied into `web/src/components/ui/`. Custom shared components in `web/src/components/` extending shadcn/ui primitives. Test-first TDD for all custom components.

**Tech Stack:** React 19, TypeScript, Vite 7, Tailwind CSS v4.1, shadcn/ui, Vitest, Testing Library, vitest-axe, Lucide React

---

## Task 1: Configure path aliases for shadcn/ui (Spike)

shadcn/ui requires `@/` path aliases. The project currently lacks them.

**Files:**
- Modify: `web/tsconfig.json` — add `compilerOptions.baseUrl` and `paths`
- Modify: `web/tsconfig.app.json` — add `baseUrl` and `paths`
- Modify: `web/vite.config.ts` — add `resolve.alias`

**Step 1: Update `web/tsconfig.json`**

```json
{
  "files": [],
  "references": [
    { "path": "./tsconfig.app.json" },
    { "path": "./tsconfig.node.json" }
  ],
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

**Step 2: Update `web/tsconfig.app.json`**

Add to `compilerOptions`:
```json
"baseUrl": ".",
"paths": {
  "@/*": ["./src/*"]
}
```

**Step 3: Update `web/vite.config.ts`**

Add path import and resolve.alias:
```typescript
import path from "path"
// ... existing imports ...

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  // ... rest of config
})
```

**Step 4: Verify**

Run: `cd web && npx tsc -b --noEmit` (should pass with no errors)

**Step 5: Commit**

```bash
git add web/tsconfig.json web/tsconfig.app.json web/vite.config.ts
git commit -m "feat(web): configure @/ path aliases for shadcn/ui"
```

---

## Task 2: Configure Tailwind CSS v4 design tokens (Spike)

**Files:**
- Modify: `web/src/index.css`

**Step 1: Update `web/src/index.css` with `@theme` block**

```css
@import 'tailwindcss';

@theme {
  /* Brand Colors */
  --color-brand-brown: #331e11;
  --color-bg-base: #faf9f7;
  --color-bg-surface: #ffffff;
  --color-border-default: #ede6e1;
  --color-border-subtle: #f5f0ec;
  --color-interactive: #005fcc;
  --color-interactive-hover: #004da6;
  --color-text-secondary: #6b5d54;
  --color-text-tertiary: #9c8e85;

  /* Status Colors */
  --color-status-pass: #1a7d37;
  --color-status-pass-bg: #ecfdf3;
  --color-status-fail: #c4320a;
  --color-status-fail-bg: #fef3f2;
  --color-status-hold: #b54708;
  --color-status-hold-bg: #fffaeb;
  --color-status-info: #005fcc;
  --color-status-info-bg: #eff8ff;
  --color-status-stale: #b54708;

  /* Typography */
  --font-primary: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;

  /* Border Radius */
  --radius-default: 0.375rem;
}
```

**Step 2: Verify** — `npm run build` should succeed.

**Step 3: Commit**

```bash
git add web/src/index.css
git commit -m "feat(web): add If Insurance brand design tokens via Tailwind v4 @theme"
```

---

## Task 3: Initialize shadcn/ui and install base components (Spike)

**Files:**
- Create: `web/components.json` (shadcn config)
- Create: `web/src/lib/utils.ts` (cn utility)
- Create: `web/src/components/ui/*.tsx` (all shadcn components)
- Modify: `web/package.json` (new dependencies)

**Step 1: Run shadcn init**

```bash
cd web && npx shadcn@latest init -d
```

If the CLI prompts, select: New York style, neutral base color, CSS variables enabled, `@/components/ui` for components, `@/lib/utils` for utils, `lucide` for icons.

If init doesn't work cleanly with existing setup, create `components.json` manually:
```json
{
  "$schema": "https://ui.shadcn.com/schema.json",
  "style": "new-york",
  "rsc": false,
  "tsx": true,
  "tailwind": {
    "config": "",
    "css": "src/index.css",
    "baseColor": "neutral",
    "cssVariables": true,
    "prefix": ""
  },
  "aliases": {
    "components": "@/components",
    "utils": "@/lib/utils",
    "ui": "@/components/ui",
    "lib": "@/lib",
    "hooks": "@/hooks"
  },
  "iconLibrary": "lucide"
}
```

And create `web/src/lib/utils.ts`:
```typescript
import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

**Step 2: Install shadcn/ui peer deps (if not installed by init)**

```bash
cd web && npm install tailwind-merge clsx
```

**Step 3: Install all required shadcn components**

```bash
cd web && npx shadcn@latest add button card table dialog select input textarea badge tooltip skeleton separator collapsible progress alert sheet dropdown-menu -y
```

**Step 4: Install form dependencies and form component**

```bash
cd web && npm install react-hook-form @hookform/resolvers zod
cd web && npx shadcn@latest add form -y
```

**Step 5: Install toast component**

```bash
cd web && npx shadcn@latest add sonner -y
```

Note: shadcn/ui uses `sonner` for toast. If the project uses a different toast primitive, adjust accordingly.

**Step 6: Verify all components are installed**

Check that `web/src/components/ui/` contains: `button.tsx`, `card.tsx`, `table.tsx`, `dialog.tsx`, `select.tsx`, `input.tsx`, `textarea.tsx`, `badge.tsx`, `tooltip.tsx`, `skeleton.tsx`, `separator.tsx`, `collapsible.tsx`, `progress.tsx`, `alert.tsx`, `sheet.tsx`, `dropdown-menu.tsx`, `form.tsx`, plus toast-related files.

**Step 7: Verify build passes**

```bash
cd web && npm run build
```

**Step 8: Commit**

```bash
git add -A
git commit -m "feat(web): initialize shadcn/ui with all required base components"
```

---

## Task 4: Customize shadcn/ui theme to If Insurance brand (Spike)

**Files:**
- Modify: `web/src/index.css` — map shadcn CSS variables to brand tokens

**Step 1: Add shadcn variable mappings**

After the `@theme` block in `index.css`, add CSS custom property overrides in `:root` to map shadcn's expected variables to the brand tokens. The exact variables depend on what shadcn init generated — inspect the generated CSS and map:

- `--background` → brand `--color-bg-base` (#faf9f7)
- `--foreground` → brand `--color-brand-brown` (#331e11)
- `--primary` → brand `--color-interactive` (#005fcc)
- `--primary-foreground` → white (#ffffff)
- `--border` → brand `--color-border-default` (#ede6e1)
- `--ring` → brand `--color-interactive` (#005fcc)
- `--muted` → brand `--color-border-subtle` (#f5f0ec)
- `--muted-foreground` → brand `--color-text-secondary` (#6b5d54)
- `--destructive` → brand `--color-status-fail` (#c4320a)
- `--card` → brand `--color-bg-surface` (#ffffff)
- `--card-foreground` → brand `--color-brand-brown` (#331e11)
- Font family → brand `--font-primary`

**Step 2: Verify build**

```bash
cd web && npm run build
```

**Step 3: Commit**

```bash
git add web/src/index.css
git commit -m "feat(web): map shadcn/ui theme variables to If Insurance brand tokens"
```

---

## Task 5: Install vitest-axe for accessibility testing (Spike)

**Files:**
- Modify: `web/package.json`
- Modify: `web/src/test-setup.ts`

**Step 1: Install vitest-axe**

```bash
cd web && npm install -D vitest-axe
```

**Step 2: Add vitest-axe matchers to test setup**

Add to `web/src/test-setup.ts`:
```typescript
import * as matchers from 'vitest-axe/matchers'
import { expect } from 'vitest'
expect.extend(matchers)
```

Also add to `'vitest-axe'` to the types in tsconfig if needed.

**Step 3: Verify tests still pass**

```bash
cd web && npm run test -- --run
```

**Step 4: Commit**

```bash
git add web/package.json web/package-lock.json web/src/test-setup.ts
git commit -m "feat(web): add vitest-axe for accessibility testing"
```

---

## Task 6: Create StatusBadge component (Test-first)

WCAG-critical — must verify accessibility before implementation.

**Files:**
- Create: `web/src/components/StatusBadge.types.ts`
- Create: `web/src/components/StatusBadge.test.tsx`
- Create: `web/src/components/StatusBadge.tsx`

**Step 1: Create types file**

```typescript
// web/src/components/StatusBadge.types.ts
export type StatusVariant = 'pass' | 'fail' | 'hold' | 'stale' | 'not-started'

export interface StatusBadgeProps {
  status: StatusVariant
  'aria-label'?: string
}
```

**Step 2: Write failing tests**

```typescript
// web/src/components/StatusBadge.test.tsx
import { render, screen } from '@/test-utils'
import { axe } from 'vitest-axe'
import { describe, expect, it } from 'vitest'
import { StatusBadge } from './StatusBadge'
import type { StatusVariant } from './StatusBadge.types'

describe('StatusBadge', () => {
  const variants: { status: StatusVariant; label: string; icon?: string }[] = [
    { status: 'pass', label: 'Pass outcome', icon: 'check' },
    { status: 'fail', label: 'Fail outcome', icon: 'x' },
    { status: 'hold', label: 'Hold outcome', icon: 'pause' },
    { status: 'stale', label: 'Stale outcome', icon: 'clock' },
    { status: 'not-started', label: 'Not Started outcome' },
  ]

  variants.forEach(({ status, label }) => {
    it(`should render ${status} variant with correct aria-label`, () => {
      render(<StatusBadge status={status} />)
      expect(screen.getByLabelText(label)).toBeInTheDocument()
    })
  })

  it('should render checkmark icon for pass variant', () => {
    render(<StatusBadge status="pass" />)
    const badge = screen.getByLabelText('Pass outcome')
    expect(badge.querySelector('svg')).toBeInTheDocument()
  })

  it('should render X icon for fail variant', () => {
    render(<StatusBadge status="fail" />)
    const badge = screen.getByLabelText('Fail outcome')
    expect(badge.querySelector('svg')).toBeInTheDocument()
  })

  it('should render pause icon for hold variant', () => {
    render(<StatusBadge status="hold" />)
    const badge = screen.getByLabelText('Hold outcome')
    expect(badge.querySelector('svg')).toBeInTheDocument()
  })

  it('should render clock icon for stale variant', () => {
    render(<StatusBadge status="stale" />)
    const badge = screen.getByLabelText('Stale outcome')
    expect(badge.querySelector('svg')).toBeInTheDocument()
  })

  it('should not render icon for not-started variant', () => {
    render(<StatusBadge status="not-started" />)
    const badge = screen.getByLabelText('Not Started outcome')
    expect(badge.querySelector('svg')).not.toBeInTheDocument()
  })

  it('should use outlined style for stale variant (distinct from hold)', () => {
    const { container } = render(<StatusBadge status="stale" />)
    const badge = container.querySelector('[aria-label="Stale outcome"]')
    // Stale uses border/outline style, not filled background
    expect(badge?.className).toMatch(/border/)
  })

  it('should allow custom aria-label', () => {
    render(<StatusBadge status="pass" aria-label="Custom label" />)
    expect(screen.getByLabelText('Custom label')).toBeInTheDocument()
  })

  // Accessibility
  variants.forEach(({ status }) => {
    it(`should have no axe violations for ${status} variant`, async () => {
      const { container } = render(<StatusBadge status={status} />)
      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })
  })
})
```

**Step 3: Run tests — verify they fail**

```bash
cd web && npx vitest run src/components/StatusBadge.test.tsx
```

**Step 4: Implement StatusBadge**

```typescript
// web/src/components/StatusBadge.tsx
import { Check, Clock, Pause, X } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { StatusBadgeProps, StatusVariant } from './StatusBadge.types'

const variantConfig: Record<StatusVariant, {
  label: string
  text: string
  icon?: React.ComponentType<{ className?: string }>
  className: string
}> = {
  pass: {
    label: 'Pass outcome',
    text: 'Pass',
    icon: Check,
    className: 'bg-status-pass-bg text-status-pass border-transparent',
  },
  fail: {
    label: 'Fail outcome',
    text: 'Fail',
    icon: X,
    className: 'bg-status-fail-bg text-status-fail border-transparent',
  },
  hold: {
    label: 'Hold outcome',
    text: 'Hold',
    icon: Pause,
    className: 'bg-status-hold-bg text-status-hold border-transparent',
  },
  stale: {
    label: 'Stale outcome',
    text: 'Stale',
    icon: Clock,
    className: 'border-status-hold text-status-hold bg-transparent border',
  },
  'not-started': {
    label: 'Not Started outcome',
    text: 'Not Started',
    className: 'bg-border-subtle text-text-secondary border-transparent',
  },
}

export function StatusBadge({ status, 'aria-label': ariaLabel }: StatusBadgeProps) {
  const config = variantConfig[status]
  const Icon = config.icon

  return (
    <Badge
      aria-label={ariaLabel ?? config.label}
      className={cn('inline-flex items-center gap-1 font-medium', config.className)}
    >
      {Icon && <Icon className="h-3.5 w-3.5" />}
      {config.text}
    </Badge>
  )
}
```

**Step 5: Run tests — verify they pass**

```bash
cd web && npx vitest run src/components/StatusBadge.test.tsx
```

**Step 6: Commit**

```bash
git add web/src/components/StatusBadge.tsx web/src/components/StatusBadge.types.ts web/src/components/StatusBadge.test.tsx
git commit -m "feat(web): add StatusBadge component with all 5 variants and accessibility"
```

---

## Task 7: Create ActionButton component (Test-first)

**Files:**
- Create: `web/src/components/ActionButton.test.tsx`
- Create: `web/src/components/ActionButton.tsx`

**Step 1: Write failing tests**

```typescript
// web/src/components/ActionButton.test.tsx
import { render, screen } from '@/test-utils'
import userEvent from '@testing-library/user-event'
import { axe } from 'vitest-axe'
import { describe, expect, it, vi } from 'vitest'
import { ActionButton } from './ActionButton'

describe('ActionButton', () => {
  it('should render primary variant with filled style', () => {
    render(<ActionButton variant="primary">Create</ActionButton>)
    expect(screen.getByRole('button', { name: 'Create' })).toBeInTheDocument()
  })

  it('should render secondary variant with outlined style', () => {
    render(<ActionButton variant="secondary">Cancel</ActionButton>)
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument()
  })

  it('should render destructive variant with outlined style', () => {
    render(<ActionButton variant="destructive">Remove</ActionButton>)
    const button = screen.getByRole('button', { name: 'Remove' })
    expect(button).toBeInTheDocument()
  })

  it('should disable button and show spinner in loading state', () => {
    render(<ActionButton variant="primary" loading loadingText="Creating...">Create</ActionButton>)
    const button = screen.getByRole('button')
    expect(button).toBeDisabled()
    expect(button).toHaveTextContent('Creating...')
  })

  it('should call onClick when clicked', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<ActionButton variant="primary" onClick={onClick}>Create</ActionButton>)
    await user.click(screen.getByRole('button'))
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('should not call onClick when loading', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<ActionButton variant="primary" loading loadingText="Creating..." onClick={onClick}>Create</ActionButton>)
    await user.click(screen.getByRole('button'))
    expect(onClick).not.toHaveBeenCalled()
  })

  // Accessibility
  it('should have no axe violations for primary variant', async () => {
    const { container } = render(<ActionButton variant="primary">Create</ActionButton>)
    expect(await axe(container)).toHaveNoViolations()
  })

  it('should have no axe violations for secondary variant', async () => {
    const { container } = render(<ActionButton variant="secondary">Cancel</ActionButton>)
    expect(await axe(container)).toHaveNoViolations()
  })

  it('should have no axe violations for destructive variant', async () => {
    const { container } = render(<ActionButton variant="destructive">Remove</ActionButton>)
    expect(await axe(container)).toHaveNoViolations()
  })
})
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement ActionButton**

```typescript
// web/src/components/ActionButton.tsx
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

type ActionButtonVariant = 'primary' | 'secondary' | 'destructive'

interface ActionButtonProps extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'children'> {
  variant: ActionButtonVariant
  loading?: boolean
  loadingText?: string
  children: React.ReactNode
}

const variantStyles: Record<ActionButtonVariant, string> = {
  primary: 'bg-interactive text-white hover:bg-interactive-hover',
  secondary: 'border border-brand-brown text-brand-brown bg-transparent hover:bg-border-subtle',
  destructive: 'border border-status-fail text-status-fail bg-transparent hover:bg-status-fail-bg',
}

export function ActionButton({
  variant,
  loading = false,
  loadingText,
  children,
  className,
  disabled,
  ...props
}: ActionButtonProps) {
  return (
    <Button
      className={cn(variantStyles[variant], className)}
      disabled={disabled || loading}
      {...props}
    >
      {loading ? (
        <>
          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          {loadingText ?? children}
        </>
      ) : (
        children
      )}
    </Button>
  )
}
```

**Step 4: Run tests — verify they pass**

**Step 5: Commit**

```bash
git add web/src/components/ActionButton.tsx web/src/components/ActionButton.test.tsx
git commit -m "feat(web): add ActionButton component with primary/secondary/destructive variants"
```

---

## Task 8: Create EmptyState component (Test-first)

**Files:**
- Create: `web/src/components/EmptyState.test.tsx`
- Create: `web/src/components/EmptyState.tsx`

**Step 1: Write failing tests**

```typescript
// web/src/components/EmptyState.test.tsx
import { render, screen } from '@/test-utils'
import userEvent from '@testing-library/user-event'
import { axe } from 'vitest-axe'
import { describe, expect, it, vi } from 'vitest'
import { EmptyState } from './EmptyState'

describe('EmptyState', () => {
  it('should render heading at h2 level by default', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" />)
    expect(screen.getByRole('heading', { level: 2, name: 'No items' })).toBeInTheDocument()
  })

  it('should render heading at h3 level when specified', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" headingLevel="h3" />)
    expect(screen.getByRole('heading', { level: 3, name: 'No items' })).toBeInTheDocument()
  })

  it('should render description text', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" />)
    expect(screen.getByText('Nothing here yet')).toBeInTheDocument()
  })

  it('should render CTA button when actionLabel provided', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" actionLabel="Create" onAction={() => {}} />)
    expect(screen.getByRole('button', { name: 'Create' })).toBeInTheDocument()
  })

  it('should not render CTA button when actionLabel omitted', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" />)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('should call onAction when CTA clicked', async () => {
    const user = userEvent.setup()
    const onAction = vi.fn()
    render(<EmptyState heading="No items" description="Nothing here yet" actionLabel="Create" onAction={onAction} />)
    await user.click(screen.getByRole('button', { name: 'Create' }))
    expect(onAction).toHaveBeenCalledOnce()
  })

  it('should render icon when provided', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" icon={<svg data-testid="custom-icon" />} />)
    expect(screen.getByTestId('custom-icon')).toBeInTheDocument()
  })

  it('should have no axe violations', async () => {
    const { container } = render(<EmptyState heading="No items" description="Nothing here yet" actionLabel="Create" onAction={() => {}} />)
    expect(await axe(container)).toHaveNoViolations()
  })
})
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement EmptyState**

```typescript
// web/src/components/EmptyState.tsx
import { ActionButton } from './ActionButton'

interface EmptyStateProps {
  heading: string
  description: string
  actionLabel?: string
  onAction?: () => void
  headingLevel?: 'h2' | 'h3'
  icon?: React.ReactNode
}

export function EmptyState({
  heading,
  description,
  actionLabel,
  onAction,
  headingLevel = 'h2',
  icon,
}: EmptyStateProps) {
  const Heading = headingLevel

  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      {icon && <div className="mb-4 text-text-tertiary">{icon}</div>}
      <Heading className="mb-2 text-lg font-semibold text-brand-brown">{heading}</Heading>
      <p className="mb-6 max-w-md text-text-secondary">{description}</p>
      {actionLabel && onAction && (
        <ActionButton variant="primary" onClick={onAction}>
          {actionLabel}
        </ActionButton>
      )}
    </div>
  )
}
```

**Step 4: Run tests — verify they pass**

**Step 5: Commit**

```bash
git add web/src/components/EmptyState.tsx web/src/components/EmptyState.test.tsx
git commit -m "feat(web): add EmptyState component with heading, description, and CTA"
```

---

## Task 9: Set up Toast system (Test-first)

Uses shadcn/ui's sonner toast. Configure durations, position, and reduced motion support.

**Files:**
- Create: `web/src/components/Toast.test.tsx`
- Create: `web/src/hooks/useAppToast.ts`
- Modify: `web/src/App.tsx` — add Toaster provider

**Step 1: Write failing tests**

Tests for the custom `useAppToast` hook which wraps sonner's toast with project-specific duration rules.

```typescript
// web/src/components/Toast.test.tsx
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { Toaster } from '@/components/ui/sonner'
import { useAppToast } from '@/hooks/useAppToast'

function ToastTester({ type }: { type: 'success' | 'error' | 'info' }) {
  const toast = useAppToast()
  return (
    <>
      <Toaster />
      <button onClick={() => toast[type]('Test message')}>Show toast</button>
    </>
  )
}

describe('Toast system', () => {
  it('should show success toast with correct content', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="success" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    await waitFor(() => {
      expect(screen.getByText('Test message')).toBeInTheDocument()
    })
  })

  it('should show error toast with correct content', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="error" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    await waitFor(() => {
      expect(screen.getByText('Test message')).toBeInTheDocument()
    })
  })

  it('should show info toast with correct content', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="info" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    await waitFor(() => {
      expect(screen.getByText('Test message')).toBeInTheDocument()
    })
  })
})
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement useAppToast hook**

```typescript
// web/src/hooks/useAppToast.ts
import { toast } from 'sonner'

export function useAppToast() {
  return {
    success: (message: string) => {
      toast.success(message, { duration: 3000 })
    },
    error: (message: string) => {
      toast.error(message, { duration: Infinity })
    },
    info: (message: string) => {
      toast.info(message, { duration: 5000 })
    },
  }
}
```

**Step 4: Add Toaster to App.tsx**

Add `<Toaster />` to the app root with position bottom-right, max 1 visible toast.

```typescript
// In App.tsx, add:
import { Toaster } from '@/components/ui/sonner'

// Inside JSX:
<Toaster position="bottom-right" toastOptions={{ style: { fontFamily: 'var(--font-primary)' } }} visibleToasts={1} />
```

**Step 5: Add reduced motion CSS**

In `index.css`, add:
```css
@media (prefers-reduced-motion: reduce) {
  [data-sonner-toaster] [data-sonner-toast] {
    transition: none !important;
    animation: none !important;
  }
}
```

**Step 6: Run tests — verify they pass**

**Step 7: Commit**

```bash
git add web/src/hooks/useAppToast.ts web/src/components/Toast.test.tsx web/src/App.tsx web/src/index.css
git commit -m "feat(web): add toast system with success/error/info variants and reduced motion"
```

---

## Task 10: Create SkeletonLoader component (Test-first)

**Files:**
- Create: `web/src/components/SkeletonLoader.test.tsx`
- Create: `web/src/components/SkeletonLoader.tsx`

**Step 1: Write failing tests**

```typescript
// web/src/components/SkeletonLoader.test.tsx
import { render } from '@/test-utils'
import { describe, expect, it } from 'vitest'
import { SkeletonLoader } from './SkeletonLoader'

describe('SkeletonLoader', () => {
  it('should render card variant with expected structure', () => {
    const { container } = render(<SkeletonLoader variant="card" />)
    expect(container.querySelector('[data-testid="skeleton-card"]')).toBeInTheDocument()
  })

  it('should render list-row variant with expected structure', () => {
    const { container } = render(<SkeletonLoader variant="list-row" />)
    expect(container.querySelector('[data-testid="skeleton-list-row"]')).toBeInTheDocument()
  })

  it('should render text-block variant with expected structure', () => {
    const { container } = render(<SkeletonLoader variant="text-block" />)
    expect(container.querySelector('[data-testid="skeleton-text-block"]')).toBeInTheDocument()
  })

  it('should apply animate-pulse class', () => {
    const { container } = render(<SkeletonLoader variant="card" />)
    const skeleton = container.firstElementChild
    expect(skeleton?.className).toMatch(/animate-pulse/)
  })
})
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement SkeletonLoader**

```typescript
// web/src/components/SkeletonLoader.tsx
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

type SkeletonVariant = 'card' | 'list-row' | 'text-block'

interface SkeletonLoaderProps {
  variant: SkeletonVariant
  className?: string
}

export function SkeletonLoader({ variant, className }: SkeletonLoaderProps) {
  switch (variant) {
    case 'card':
      return (
        <div data-testid="skeleton-card" className={cn('animate-pulse space-y-3 rounded-md border border-border-default p-4', className)}>
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-20 w-full" />
        </div>
      )
    case 'list-row':
      return (
        <div data-testid="skeleton-list-row" className={cn('animate-pulse flex items-center gap-3 py-3', className)}>
          <Skeleton className="h-8 w-8 rounded-full" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-1/3" />
            <Skeleton className="h-3 w-1/4" />
          </div>
        </div>
      )
    case 'text-block':
      return (
        <div data-testid="skeleton-text-block" className={cn('animate-pulse space-y-2', className)}>
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-5/6" />
          <Skeleton className="h-4 w-4/6" />
        </div>
      )
  }
}
```

**Step 4: Run tests — verify they pass**

**Step 5: Commit**

```bash
git add web/src/components/SkeletonLoader.tsx web/src/components/SkeletonLoader.test.tsx
git commit -m "feat(web): add SkeletonLoader component with card/list-row/text-block variants"
```

---

## Task 11: Create ErrorBoundary component (Test-first)

**Files:**
- Create: `web/src/components/ErrorBoundary.test.tsx`
- Create: `web/src/components/ErrorBoundary.tsx`

**Step 1: Write failing tests**

```typescript
// web/src/components/ErrorBoundary.test.tsx
import { render, screen } from '@/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { ErrorBoundary } from './ErrorBoundary'

function ThrowingComponent({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) throw new Error('Test error')
  return <div>Child content</div>
}

describe('ErrorBoundary', () => {
  // Suppress console.error for expected errors
  const originalError = console.error
  beforeEach(() => { console.error = vi.fn() })
  afterEach(() => { console.error = originalError })

  it('should render children when no error occurs', () => {
    render(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={false} />
      </ErrorBoundary>
    )
    expect(screen.getByText('Child content')).toBeInTheDocument()
  })

  it('should show default fallback when child throws', () => {
    render(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>
    )
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
    expect(screen.getByText('Try refreshing the page')).toBeInTheDocument()
  })

  it('should show custom fallback when provided', () => {
    render(
      <ErrorBoundary fallback={<div>Custom error UI</div>}>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>
    )
    expect(screen.getByText('Custom error UI')).toBeInTheDocument()
  })

  it('should show reload button in default fallback', () => {
    render(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>
    )
    expect(screen.getByRole('button', { name: 'Reload' })).toBeInTheDocument()
  })
})
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement ErrorBoundary**

```typescript
// web/src/components/ErrorBoundary.tsx
import { Component } from 'react'
import type { ErrorInfo, ReactNode } from 'react'
import { ActionButton } from './ActionButton'

interface ErrorBoundaryProps {
  children: ReactNode
  fallback?: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught:', error, errorInfo)
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback
      }

      return (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <h2 className="mb-2 text-lg font-semibold text-brand-brown">Something went wrong</h2>
          <p className="mb-6 text-text-secondary">Try refreshing the page</p>
          <ActionButton variant="primary" onClick={() => window.location.reload()}>
            Reload
          </ActionButton>
        </div>
      )
    }

    return this.props.children
  }
}
```

**Step 4: Run tests — verify they pass**

**Step 5: Commit**

```bash
git add web/src/components/ErrorBoundary.tsx web/src/components/ErrorBoundary.test.tsx
git commit -m "feat(web): add ErrorBoundary component with default and custom fallback"
```

---

## Task 12: Update test-utils.tsx with Toaster provider

**Files:**
- Modify: `web/src/test-utils.tsx`

**Step 1: Add Toaster to AllProviders**

```typescript
import { render } from '@testing-library/react'
import { Toaster } from '@/components/ui/sonner'
import { AuthProvider } from './features/auth/AuthContext'
import type { RenderOptions } from '@testing-library/react'
import type { ReactElement } from 'react'

function AllProviders({ children }: { children: React.ReactNode }) {
  return (
    <AuthProvider>
      {children}
      <Toaster position="bottom-right" visibleToasts={1} />
    </AuthProvider>
  )
}

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => render(ui, { wrapper: AllProviders, ...options })

export * from '@testing-library/react'
export { customRender as render }
```

**Step 2: Verify all tests pass**

```bash
cd web && npm run test -- --run
```

**Step 3: Commit**

```bash
git add web/src/test-utils.tsx
git commit -m "feat(web): add Toaster provider to test-utils for component testing"
```

---

## Task 13: Final verification — build, test, lint

**Step 1:** `cd web && npm run build` — zero errors
**Step 2:** `cd web && npm run test -- --run` — all tests pass
**Step 3:** `cd web && npm run lint` — zero violations
**Step 4:** Fix any issues found.
**Step 5:** Final commit if any fixes needed.

---

## Task 14: Update Dev Agent Record in story file

**Files:**
- Modify: `_bmad-output/implementation-artifacts/1-4-shared-ui-components-design-tokens.md`

Update the Dev Agent Record section with:
- Agent model, testing modes used, key decisions, file list.
