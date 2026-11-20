import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { ApiError } from '../services/api'

interface FieldErrors {
  email?: string
  password?: string
}

function validate(email: string, password: string): FieldErrors {
  const errors: FieldErrors = {}
  if (!email.trim()) errors.email = 'Enter your email address.'
  else if (!/^\S+@\S+\.\S+$/.test(email)) errors.email = 'Enter a valid email address.'
  if (!password) errors.password = 'Enter your password.'
  return errors
}

/** Only follow in-app paths, so a crafted link can never send someone to another website after sign-in. */
function safeDestination(from: unknown): string {
  return typeof from === 'string' && from.startsWith('/') && !from.startsWith('//') && from !== '/login'
    ? from
    : '/dashboard'
}

// Local-development demo accounts, created by the API's development seeder. Not included in production builds.
const DEMO_PASSWORD = 'Dev-Passw0rd!'
const DEMO_ACCOUNTS = [
  { label: 'Administrator', email: 'admin@edutrack.test' },
  { label: 'Lecturer', email: 'lecturer@edutrack.test' },
  { label: 'Student', email: 'student@edutrack.test' },
]

export default function LoginPage() {
  const { status, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const destination = safeDestination((location.state as { from?: unknown } | null)?.from)

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Already signed in: nothing to do here.
  if (status === 'authenticated') return <Navigate to={destination} replace />

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)

    const found = validate(email, password)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSubmitting(true)
    try {
      await login(email.trim(), password)
      navigate(destination, { replace: true })
    } catch (error) {
      setFormError(error instanceof ApiError ? error.message : 'Something went wrong. Please try again.')
      setPassword('')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-full items-center justify-center px-4 py-8">
      <div className="w-full max-w-sm space-y-4">
        <form
          onSubmit={onSubmit}
          noValidate
          className="space-y-5 rounded-xl bg-white p-8 shadow-sm ring-1 ring-slate-200"
        >
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Sign in to EduTrack</h1>
            <p className="mt-1 text-sm text-slate-600">Student management system</p>
          </div>

          {formError && (
            <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
              {formError}
            </p>
          )}

          <div>
            <label htmlFor="email" className="block text-sm font-medium">
              Email
            </label>
            <input
              id="email"
              type="email"
              autoComplete="username"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              aria-invalid={errors.email ? true : undefined}
              aria-describedby={errors.email ? 'email-error' : undefined}
              className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            />
            {errors.email && (
              <p id="email-error" className="mt-1 text-sm text-red-700">
                {errors.email}
              </p>
            )}
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium">
              Password
            </label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              aria-invalid={errors.password ? true : undefined}
              aria-describedby={errors.password ? 'password-error' : undefined}
              className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            />
            {errors.password && (
              <p id="password-error" className="mt-1 text-sm text-red-700">
                {errors.password}
              </p>
            )}
          </div>

          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-md bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {submitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        {import.meta.env.DEV && (
          <div className="rounded-lg border border-dashed border-slate-300 bg-white p-4 text-sm">
            <p className="font-medium text-slate-700">Development demo accounts</p>
            <p className="mt-0.5 text-xs text-slate-500">Fictional data on your own machine. Fills the form only.</p>
            <div className="mt-3 flex flex-wrap gap-2">
              {DEMO_ACCOUNTS.map((account) => (
                <button
                  key={account.email}
                  type="button"
                  onClick={() => {
                    setEmail(account.email)
                    setPassword(DEMO_PASSWORD)
                    setErrors({})
                    setFormError(null)
                  }}
                  className="rounded-md px-3 py-1.5 text-xs font-medium text-slate-700 ring-1 ring-slate-300 hover:bg-slate-50"
                >
                  {account.label}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
