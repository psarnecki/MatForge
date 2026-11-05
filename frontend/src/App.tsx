import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { AppShell } from './components/AppShell'
import { Placeholder } from './components/Placeholder'
import { ToastProvider } from './components/ToastProvider'
import { LoginPage } from './features/auth/LoginPage'
import { RegisterPage } from './features/auth/RegisterPage'
import { RequireAuth } from './features/auth/RequireAuth'
import { texts } from './texts'

export default function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route element={<RequireAuth />}>
            <Route element={<AppShell />}>
              <Route index element={<Placeholder title={texts.nav.dashboard} />} />
              <Route path="training" element={<Placeholder title={texts.nav.training} />} />
              <Route path="competitions" element={<Placeholder title={texts.nav.competitions} />} />
              <Route path="goals" element={<Placeholder title={texts.nav.goals} />} />
              <Route path="library" element={<Placeholder title={texts.nav.library} />} />
              <Route path="body" element={<Placeholder title={texts.nav.body} />} />
              <Route path="profile" element={<Placeholder title={texts.nav.profile} />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </ToastProvider>
  )
}
