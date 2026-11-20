import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { ApiError, api, setUnauthorizedHandler } from '../services/api'
import type { CurrentUser } from '../types'
import { AuthContext, type AuthContextValue, type AuthStatus } from './auth-context'

interface State {
  status: AuthStatus
  user: CurrentUser | null
}

const ANONYMOUS: State = { status: 'anonymous', user: null }

/** Asks the API who is signed in. 401 means "nobody"; anything else means we could not find out. */
async function loadSession(): Promise<State> {
  try {
    return { status: 'authenticated', user: await api.me() }
  } catch (error) {
    return error instanceof ApiError && error.status === 401 ? ANONYMOUS : { status: 'unavailable', user: null }
  }
}

/**
 * Holds the signed-in user. The session itself lives in an HttpOnly cookie that JavaScript cannot read, so the
 * app asks the API who it is (GET /api/auth/me) instead of keeping a token in browser storage.
 */
export default function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<State>({ status: 'loading', user: null })

  const refresh = useCallback(async () => {
    setState(await loadSession())
  }, [])

  useEffect(() => {
    // Restore an existing session on first load.
    let cancelled = false
    void loadSession().then((next) => {
      if (!cancelled) setState(next)
    })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    // If any later request finds the session gone (expired, or the account was deactivated), return to sign-in.
    setUnauthorizedHandler(() => setState(ANONYMOUS))
    return () => setUnauthorizedHandler(null)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const user = await api.login(email, password)
    setState({ status: 'authenticated', user })
    return user
  }, [])

  const logout = useCallback(async () => {
    try {
      await api.logout()
    } finally {
      // Even if the request failed, treat this browser as signed out; the cookie expires on its own.
      setState(ANONYMOUS)
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ ...state, login, logout, refresh }),
    [state, login, logout, refresh],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
