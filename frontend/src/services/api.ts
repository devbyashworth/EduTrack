import type { CurrentUser, HealthResponse } from '../types'

/** Base path for the API. In dev, Vite proxies it; in production set VITE_API_BASE_URL. */
const BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? ''

/**
 * The API refuses state-changing requests (POST, PUT, PATCH, DELETE) that lack this header. A forged request from
 * another website cannot add it without a CORS preflight, which the API only approves for this app's own origin.
 */
const CLIENT_HEADER = { 'X-Requested-With': 'EduTrack' }

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

type UnauthorizedHandler = () => void
let onUnauthorized: UnauthorizedHandler | null = null

/** Lets the auth layer react when any request finds the session has expired or been revoked. */
export function setUnauthorizedHandler(handler: UnauthorizedHandler | null) {
  onUnauthorized = handler
}

async function messageFrom(response: Response): Promise<string> {
  if (response.status === 429) return 'Too many attempts. Please wait a minute and try again.'
  if (response.status >= 500) return 'The server is unavailable right now. Please try again shortly.'
  if (response.status === 403) return 'You do not have permission to do that.'

  // The API reports problems as { "message": "..." }; fall back to a generic text for anything else.
  try {
    const body = (await response.json()) as { message?: unknown }
    if (typeof body.message === 'string' && body.message) return body.message
  } catch {
    // Not JSON: use the generic message below.
  }
  return `Request failed (${response.status}).`
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = (init.method ?? 'GET').toUpperCase()
  const changesState = method !== 'GET' && method !== 'HEAD'

  let response: Response
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      ...init,
      // Same-origin in dev (via the Vite proxy); a cross-origin production API needs credentials sent explicitly.
      credentials: BASE_URL ? 'include' : 'same-origin',
      headers: {
        ...(init.body ? { 'Content-Type': 'application/json' } : {}),
        ...(changesState ? CLIENT_HEADER : {}),
        ...init.headers,
      },
    })
  } catch {
    // Network failure: surface a friendly message rather than a raw TypeError.
    throw new ApiError(0, 'Cannot reach the server. Check your connection and try again.')
  }

  if (response.status === 401) onUnauthorized?.()

  if (!response.ok) {
    throw new ApiError(response.status, await messageFrom(response))
  }
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export const api = {
  health: () => request<HealthResponse>('/api/health'),
  me: () => request<CurrentUser>('/api/auth/me'),
  login: (email: string, password: string) =>
    request<CurrentUser>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
}
