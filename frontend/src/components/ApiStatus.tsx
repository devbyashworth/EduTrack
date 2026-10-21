import { useEffect, useState } from 'react'
import { api } from '../services/api'

type State = 'checking' | 'healthy' | 'degraded' | 'offline'

const LABELS: Record<State, string> = {
  checking: 'Checking API…',
  healthy: 'API connected',
  degraded: 'API up, database unavailable',
  offline: 'API offline',
}

const DOT: Record<State, string> = {
  checking: 'bg-slate-400',
  healthy: 'bg-emerald-500',
  degraded: 'bg-amber-500',
  offline: 'bg-red-500',
}

/** Small live indicator proving the frontend can reach the API and the API can reach SQL Server. */
export default function ApiStatus() {
  const [state, setState] = useState<State>('checking')

  useEffect(() => {
    let cancelled = false

    async function check() {
      try {
        const health = await api.health()
        if (!cancelled) setState(health.status)
      } catch (error) {
        // A 503 from /api/health means the API is up but the database is not.
        const status = (error as { status?: number }).status
        if (!cancelled) setState(status === 503 ? 'degraded' : 'offline')
      }
    }

    void check()
    const id = window.setInterval(() => void check(), 30_000)
    return () => {
      cancelled = true
      window.clearInterval(id)
    }
  }, [])

  return (
    <span
      role="status"
      className="inline-flex items-center gap-2 rounded-full bg-white px-3 py-1 text-xs font-medium text-slate-700 ring-1 ring-slate-200"
    >
      <span aria-hidden="true" className={`h-2 w-2 rounded-full ${DOT[state]}`} />
      {LABELS[state]}
    </span>
  )
}
