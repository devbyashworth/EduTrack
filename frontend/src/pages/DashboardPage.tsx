const MILESTONES = [
  { name: 'Foundation', detail: 'Repository, API, database model, app shell', done: true },
  { name: 'Security', detail: 'Login, roles, record-level access', done: false },
  { name: 'Core workflows', detail: 'Students, enrolments, marks, attendance', done: false },
  { name: 'Quality', detail: 'Dashboards, automated and security tests', done: false },
  { name: 'Release', detail: 'Deployment, documentation, demo', done: false },
]

export default function DashboardPage() {
  return (
    <div className="mx-auto max-w-4xl">
      <h1 className="text-2xl font-semibold tracking-tight">Dashboard</h1>
      <p className="mt-1 text-sm text-slate-600">
        Live statistics arrive with the core workflows. This page shows build progress for now.
      </p>

      <ol className="mt-6 space-y-3">
        {MILESTONES.map((m) => (
          <li
            key={m.name}
            className="flex items-start gap-3 rounded-lg bg-white p-4 ring-1 ring-slate-200"
          >
            <span
              aria-hidden="true"
              className={`mt-0.5 h-5 w-5 shrink-0 rounded-full ${
                m.done ? 'bg-emerald-500' : 'border-2 border-slate-300'
              }`}
            />
            <div>
              <p className="font-medium">
                {m.name}
                <span className="sr-only">{m.done ? ' (complete)' : ' (not started)'}</span>
              </p>
              <p className="text-sm text-slate-600">{m.detail}</p>
            </div>
          </li>
        ))}
      </ol>
    </div>
  )
}
