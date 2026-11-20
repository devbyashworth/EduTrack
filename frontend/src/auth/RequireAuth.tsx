import { Link, Navigate, Outlet, useLocation } from 'react-router-dom'
import type { ReactNode } from 'react'
import type { Role } from '../types'
import { useAuth } from './useAuth'

function CenteredMessage({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-full items-center justify-center px-4 text-center text-sm text-slate-600">
      <div role="status">{children}</div>
    </div>
  )
}

/** Layout route: everything inside requires a signed-in user. */
export default function RequireAuth() {
  const { status, refresh } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <CenteredMessage>Loading…</CenteredMessage>

  if (status === 'unavailable') {
    return (
      <CenteredMessage>
        <p className="font-medium text-slate-800">Cannot reach the server.</p>
        <p className="mt-1">Check your connection, then try again.</p>
        <button
          type="button"
          onClick={() => void refresh()}
          className="mt-4 rounded-md bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700"
        >
          Try again
        </button>
      </CenteredMessage>
    )
  }

  if (status === 'anonymous') {
    // Remember where the person was going so sign-in can send them back.
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}

/**
 * Shows the page only to the listed roles. This is a convenience for the person using the app; the API enforces
 * the same rules on every request, so bypassing this component would not expose any data.
 */
export function RequireRole({ roles, children }: { roles: Role[]; children: ReactNode }) {
  const { user } = useAuth()

  if (!user || !roles.includes(user.role)) {
    return (
      <div className="mx-auto max-w-4xl">
        <div role="alert" className="mt-6 rounded-lg border border-slate-200 bg-white p-10 text-center">
          <h1 className="text-xl font-semibold tracking-tight">Access denied</h1>
          <p className="mt-2 text-sm text-slate-600">Your account does not have permission to view this page.</p>
          <Link to="/dashboard" className="mt-4 inline-block text-sm font-medium text-brand-600 hover:underline">
            Back to the dashboard
          </Link>
        </div>
      </div>
    )
  }

  return <>{children}</>
}
