export type Role = 'Administrator' | 'Lecturer' | 'Student'

export interface HealthResponse {
  status: 'healthy' | 'degraded'
  database: boolean
}

/** The signed-in user, as returned by POST /api/auth/login and GET /api/auth/me. */
export interface CurrentUser {
  id: number
  email: string
  displayName: string
  role: Role
}

export interface NavItem {
  label: string
  to: string
  /** Roles allowed to see this entry. Real enforcement always happens on the API. */
  roles: Role[]
}
