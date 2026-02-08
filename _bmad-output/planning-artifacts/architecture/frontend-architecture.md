# Frontend Architecture

_Extracted from the Architecture Decision Document. The core document ([architecture.md](../architecture.md)) contains core decisions. See also [patterns-frontend.md](./patterns-frontend.md) for naming conventions and UI consistency rules._

## Framework & Libraries

**Framework:** React 19 + TypeScript (strict) + Vite 7 + Tailwind CSS v4

**Viewport constraint:** Desktop-first, minimum viewport width of 1280px. The screening split-panel layout requires sufficient horizontal space for candidate list + PDF viewer + outcome form. Below 1280px, display a "please use a wider browser window" message. No responsive breakpoints — the PRD specifies desktop-only (Edge/Chrome).

**Authentication library:** `@azure/msal-browser` + `@azure/msal-react` for Entra ID OIDC (Authorization Code flow with PKCE). MSAL handles token acquisition, caching, and silent refresh. The `AuthContext` wraps MSAL's `useMsal` hook to provide a consistent interface for the rest of the app. The `msalInstance` (PublicClientApplication) is created and exported from `msalConfig.ts` — both `AuthContext.tsx` and `httpClient.ts` import from there to avoid circular dependencies.

## State Management

- **Server state:** TanStack Query (caching, refetching, optimistic updates)
- **URL state:** React Router v7 params (active recruitment, candidate selection)
- **Client state:** Component-local `useState`/`useReducer` + React Context for shared concerns (auth, screening session)

## Folder Structure

Feature-based:

```
web/src/
  features/
    auth/              # Auth context, login redirect
    recruitments/      # List, create, edit, close
    candidates/        # List, detail, import, manual create
    screening/         # Split-panel batch screening
    overview/          # Dashboard, health indicators
    team/              # Membership management
  components/          # Shared UI components
  hooks/               # Shared hooks
  lib/
    api/               # API client modules (per feature)
  routes/              # Route definitions (thin layer)
```

## API Client Contract Pattern

Each feature has a corresponding API module in `lib/api/`. TanStack Query hooks consume these modules:

```typescript
// lib/api/recruitments.ts — API client module
export const recruitmentApi = {
  getById: (id: string) => fetch(`/api/recruitments/${id}`).then(handleResponse),
  getOverview: (id: string) => fetch(`/api/recruitments/${id}/overview`).then(handleResponse),
};

// features/overview/hooks/useRecruitmentOverview.ts — TanStack Query hook
export const useRecruitmentOverview = (id: string) =>
  useQuery({
    queryKey: ['recruitment', id, 'overview'],
    queryFn: () => recruitmentApi.getOverview(id),
  });
```

Whether the API modules are hand-written or generated from OpenAPI (deferred decision), the consumption pattern is the same. Features never call `fetch` directly.

## HTTP Client Foundation Pattern

`httpClient.ts` is the single entry point for all API communication. Every API module imports from here — never from `fetch` directly:

```typescript
// lib/api/httpClient.ts
import { msalInstance } from '../../features/auth/msalConfig';
import { InteractionRequiredAuthError } from '@azure/msal-browser';

const API_BASE = '/api';
const isDev = import.meta.env.VITE_AUTH_MODE === 'development';

async function getAuthHeaders(): Promise<HeadersInit> {
  if (isDev) {
    // Dev mode: read from localStorage dev persona, send custom headers
    const devUser = JSON.parse(localStorage.getItem('dev-auth-user') || 'null');
    if (!devUser) return { 'Content-Type': 'application/json' }; // unauthenticated
    return {
      'X-Dev-User-Id': devUser.id,
      'X-Dev-User-Name': devUser.name,
      'Content-Type': 'application/json',
    };
  }

  // Production mode: acquire token via MSAL
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) throw new AuthError('No active session');
  try {
    const { accessToken } = await msalInstance.acquireTokenSilent({
      scopes: ['api://<client-id>/.default'],
      account: accounts[0],
    });
    return { Authorization: `Bearer ${accessToken}`, 'Content-Type': 'application/json' };
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      msalInstance.loginRedirect();
      throw new AuthError('Session expired — redirecting to login');
    }
    throw error;
  }
}

export async function apiGet<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { headers: await getAuthHeaders() });
  return handleResponse<T>(res);
}

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: await getAuthHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  });
  return handleResponse<T>(res);
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (res.status === 401) {
    // Token expired or invalid — redirect to login
    msalInstance.loginRedirect();
    throw new AuthError('Session expired');
  }
  if (!res.ok) {
    const problem = await res.json(); // ProblemDetails shape
    throw new ApiError(res.status, problem);
  }
  return res.json() as Promise<T>;
}
```

API modules then use these helpers instead of raw `fetch`:

```typescript
// lib/api/recruitments.ts
import { apiGet, apiPost } from './httpClient';
import type { Recruitment, RecruitmentOverview } from './recruitments.types';

export const recruitmentApi = {
  getById: (id: string) => apiGet<Recruitment>(`/recruitments/${id}`),
  getOverview: (id: string) => apiGet<RecruitmentOverview>(`/recruitments/${id}/overview`),
  create: (data: CreateRecruitmentRequest) => apiPost<Recruitment>('/recruitments', data),
};
```

## Batch Screening Architecture

- PDF pre-fetching via SAS URLs from candidate list response
- Three isolated state domains (candidate list, PDF viewer, outcome form)
- Focus management contract: focus returns to candidate list after outcome submission
- Optimistic outcome recording with non-blocking retry
- Keyboard-first navigation

## PDF Viewing

react-pdf (PDF.js wrapper) with self-authenticating SAS URLs. Text layer enabled for screen reader accessibility. Per-page lazy loading for screening performance.
