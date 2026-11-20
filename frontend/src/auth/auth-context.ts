import { createContext } from 'react'
import type { CurrentUser } from '../types'

/**
 * loading      - checking whether a session already exists
 * authenticated - signed in
 * anonymous    - not signed in
 * unavailable  - could not reach the API to find out
 */
export type AuthStatus = 'loading' | 'authenticated' | 'anonymous' | 'unavailable'

export interface AuthContextValue {
  status: AuthStatus
  user: CurrentUser | null
  login: (email: string, password: string) => Promise<CurrentUser>
  logout: () => Promise<void>
  /** Re-check the session, e.g. after the API was unreachable. */
  refresh: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
