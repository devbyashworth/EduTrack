import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import AuthProvider from './auth/AuthProvider'
import RequireAuth, { RequireRole } from './auth/RequireAuth'
import AppShell from './components/AppShell'
import { NAV_ITEMS } from './components/navigation'
import DashboardPage from './pages/DashboardPage'
import LoginPage from './pages/LoginPage'
import PlaceholderPage from './pages/PlaceholderPage'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<RequireAuth />}>
            <Route element={<AppShell />}>
              {NAV_ITEMS.map((item) => (
                <Route
                  key={item.to}
                  path={item.to}
                  element={
                    <RequireRole roles={item.roles}>
                      {item.to === '/dashboard' ? <DashboardPage /> : <PlaceholderPage title={item.label} />}
                    </RequireRole>
                  }
                />
              ))}
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
