import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import ApiStatus from './ApiStatus'
import { navForRole } from './navigation'

const linkClass = (isActive: boolean) =>
  `block rounded-md px-3 py-2 text-sm font-medium transition-colors ${
    isActive ? 'bg-white/15 text-white' : 'text-indigo-100 hover:bg-white/10'
  }`

export default function AppShell() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  // RequireAuth guarantees a user before this renders; this only narrows the type.
  if (!user) return null
  const items = navForRole(user.role)

  async function signOut() {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex min-h-full">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-50 focus:rounded focus:bg-white focus:px-3 focus:py-2"
      >
        Skip to main content
      </a>

      <aside className="hidden w-60 shrink-0 flex-col bg-brand-900 text-white md:flex">
        <div className="px-6 py-5 text-lg font-semibold tracking-tight">EduTrack</div>
        <nav aria-label="Main" className="flex-1 space-y-1 px-3 pb-6">
          {items.map((item) => (
            <NavLink key={item.to} to={item.to} className={({ isActive }) => linkClass(isActive)}>
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between gap-3 border-b border-slate-200 bg-white px-4 py-3 md:px-6">
          <div className="min-w-0">
            <p className="truncate text-sm font-medium text-slate-900">{user.displayName}</p>
            <p className="text-xs text-slate-500">{user.role}</p>
          </div>
          <div className="flex shrink-0 items-center gap-3">
            <ApiStatus />
            <button
              type="button"
              onClick={() => void signOut()}
              className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-700 ring-1 ring-slate-300 hover:bg-slate-50"
            >
              Sign out
            </button>
          </div>
        </header>

        {/* The sidebar is hidden on small screens, so show the same links as a scrollable strip. */}
        <nav aria-label="Main (mobile)" className="flex gap-1 overflow-x-auto bg-brand-900 px-2 py-2 md:hidden">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `${linkClass(isActive)} whitespace-nowrap`}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <main id="main" className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
