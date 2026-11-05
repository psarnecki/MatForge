import { useState, type SubmitEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { texts } from '../../texts'
import type { LoginRequest } from '../../types'

function loginErrorMessage(error: unknown) {
  if (error instanceof ApiError && error.status === 401) {
    return error.detail === 'LockedOut' ? texts.auth.lockedOut : texts.auth.wrongCredentials
  }
  return errorMessage(error)
}

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const request: LoginRequest = { email, password }
      await apiFetch('/auth/login?useCookies=true', { method: 'POST', body: request, redirectOn401: false })
      navigate('/', { replace: true })
    } catch (caught) {
      setError(loginErrorMessage(caught))
      setLoading(false)
    }
  }

  return (
    <>
      <header className="auth-bar">
        <Link to="/" className="shell-logo">
          {texts.appName}
        </Link>
      </header>
      <main className="auth-main">
        <h1>{texts.auth.loginTitle}</h1>
        <form className="auth-form" onSubmit={handleSubmit}>
          <Field label={texts.auth.email} type="email" value={email} onChange={setEmail} autoComplete="email" required />
          <Field
            label={texts.auth.password}
            type="password"
            value={password}
            onChange={setPassword}
            autoComplete="current-password"
            required
          />
          {error && (
            <p className="auth-error" role="alert">
              {error}
            </p>
          )}
          <Button type="submit" loading={loading}>
            {texts.auth.logIn}
          </Button>
        </form>
        <p className="auth-switch">
          {texts.auth.noAccount} <Link to="/register">{texts.auth.toRegister}</Link>
        </p>
      </main>
    </>
  )
}
