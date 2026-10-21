import type { NavItem, Role } from '../types'

export const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', to: '/dashboard', roles: ['Administrator', 'Lecturer', 'Student'] },
  { label: 'Students', to: '/students', roles: ['Administrator'] },
  { label: 'Courses', to: '/courses', roles: ['Administrator'] },
  { label: 'Enrolments', to: '/enrollments', roles: ['Administrator'] },
  { label: 'Academic terms', to: '/academic-terms', roles: ['Administrator'] },
  { label: 'Users', to: '/users', roles: ['Administrator'] },
  { label: 'Gradebook', to: '/gradebook', roles: ['Lecturer'] },
  { label: 'Attendance', to: '/attendance', roles: ['Lecturer'] },
  { label: 'My courses', to: '/my-courses', roles: ['Student'] },
  { label: 'My results', to: '/my-results', roles: ['Student'] },
  { label: 'Reports', to: '/reports', roles: ['Administrator', 'Lecturer'] },
  { label: 'Profile', to: '/profile', roles: ['Administrator', 'Lecturer', 'Student'] },
]

export const navForRole = (role: Role): NavItem[] =>
  NAV_ITEMS.filter((item) => item.roles.includes(role))
