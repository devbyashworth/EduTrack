import { NavLink, Outlet } from 'react-router-dom'
import type { Role } from '../types'
import ApiStatus from './ApiStatus'
import { navForRole } from './navigation'

/**
 * TEMPORARY: until authentication is built, the shell previews the administrator navigation.
 * This will come from the signed-in user. Hiding links is only a convenience; the API enforces access.
 */
const PREVIEW_ROLE: Role = 'Administrator'

export default function AppShell() {
  const items = navForRole(PREVIEW_ROLE)

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
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `block rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                  isActive ? 'bg-white/15 text-white' : 'text-indigo-100 hover:bg-white/10'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
          <span className="text-sm font-medium text-slate-600">{PREVIEW_ROLE} (preview)</span>
          <ApiStatus />
        </header>
        <main id="main" className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
