import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import AppShell from './components/AppShell'
import { NAV_ITEMS } from './components/navigation'
import DashboardPage from './pages/DashboardPage'
import LoginPage from './pages/LoginPage'
import PlaceholderPage from './pages/PlaceholderPage'

// Every planned page except the dashboard renders a placeholder until its build step.
const PLACEHOLDER_ROUTES = NAV_ITEMS.filter((item) => item.to !== '/dashboard')

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<AppShell />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          {PLACEHOLDER_ROUTES.map((item) => (
            <Route key={item.to} path={item.to} element={<PlaceholderPage title={item.label} />} />
          ))}
        </Route>
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
