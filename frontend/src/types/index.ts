export type Role = 'Administrator' | 'Lecturer' | 'Student'

export interface HealthResponse {
  status: 'healthy' | 'degraded'
  database: boolean
}

export interface NavItem {
  label: string
  to: string
  /** Roles allowed to see this entry. Real enforcement always happens on the API. */
  roles: Role[]
}
