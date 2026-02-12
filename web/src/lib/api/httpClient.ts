import {
  parseProblemDetails,
  type ProblemDetails,
} from '../utils/problemDetails'

const API_BASE = '/api'

export class AuthError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'AuthError'
  }
}

export class ApiError extends Error {
  readonly status: number
  readonly problemDetails: ProblemDetails

  constructor(status: number, problemDetails: ProblemDetails) {
    super(problemDetails.title)
    this.name = 'ApiError'
    this.status = status
    this.problemDetails = problemDetails
  }
}

async function getAuthHeaders(): Promise<HeadersInit> {
  const isDev = import.meta.env.VITE_AUTH_MODE === 'development'

  if (isDev) {
    const devUser = JSON.parse(
      localStorage.getItem('dev-auth-user') || 'null',
    ) as {
      id: string
      name: string
    } | null
    if (!devUser) {
      return { 'Content-Type': 'application/json' }
    }
    return {
      'X-Dev-User-Id': devUser.id,
      'X-Dev-User-Name': devUser.name,
      'Content-Type': 'application/json',
    }
  }

  // Production mode: acquire token via MSAL
  const { msalInstance, loginRequest } =
    await import('../../features/auth/msalConfig')
  const accounts = msalInstance.getAllAccounts()
  if (accounts.length === 0) {
    throw new AuthError('No active session')
  }
  try {
    const { accessToken } = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account: accounts[0],
    })
    return {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    }
  } catch (error) {
    const { InteractionRequiredAuthError } = await import('@azure/msal-browser')
    if (error instanceof InteractionRequiredAuthError) {
      await msalInstance.loginRedirect(loginRequest)
      throw new AuthError('Session expired — redirecting to login')
    }
    throw error
  }
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (res.status === 401) {
    throw new AuthError('Session expired')
  }

  if (res.status === 204) {
    return undefined as T
  }

  if (!res.ok) {
    let json: unknown
    try {
      json = await res.json()
    } catch {
      // Non-JSON response (e.g. HTML error page from reverse proxy)
    }
    throw new ApiError(
      res.status,
      parseProblemDetails(
        json ?? { title: res.statusText, status: res.status },
      ),
    )
  }

  return res.json() as Promise<T>
}

export async function apiGet<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: await getAuthHeaders(),
  })
  return handleResponse<T>(res)
}

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: await getAuthHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  })
  return handleResponse<T>(res)
}

export async function apiPut<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    headers: await getAuthHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  })
  return handleResponse<T>(res)
}

export async function apiDelete(path: string): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'DELETE',
    headers: await getAuthHeaders(),
  })
  return handleResponse<void>(res)
}
