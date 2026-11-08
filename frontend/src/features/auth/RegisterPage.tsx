import { useState, type SubmitEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { RegisterRequest, UpdateProfileRequest } from '../../types'
import { registerErrors, type RegisterErrors } from './registerErrors'

const nameMaxLength = 100

export function RegisterPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordsDiffer, setPasswordsDiffer] = useState(false)
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [errors, setErrors] = useState<RegisterErrors>({})
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const showToast = useToast()

  // Register, log in, then save the names (Identity's register takes only email and password)
  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrors({})
    // Checked only here, the confirmation is never sent
    if (password !== confirmPassword) {
      setPasswordsDiffer(true)
      return
    }
    setPasswordsDiffer(false)
    setLoading(true)
    try {
      const request: RegisterRequest = { email, password }
      await apiFetch('/auth/register', { method: 'POST', body: request })
      await apiFetch('/auth/login?useCookies=true', { method: 'POST', body: request, redirectOn401: false })
    } catch (caught) {
      const isValidation = caught instanceof ApiError && caught.status === 400
      setErrors(isValidation ? registerErrors(caught.errors) : { form: errorMessage(caught) })
      setLoading(false)
      return
    }

    try {
      const names: UpdateProfileRequest = { firstName: firstName.trim(), lastName: lastName.trim() }
      await apiFetch('/me', { method: 'PUT', body: names })
      navigate('/', { replace: true })
    } catch {
      // The account already exists, so the profile page lets the user finish
      showToast(texts.auth.namesNotSaved, 'error')
      navigate('/profile', { replace: true })
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
        <h1>{texts.auth.registerTitle}</h1>
        <form className="auth-form" onSubmit={handleSubmit}>
          <Field
            label={texts.auth.email}
            type="email"
            value={email}
            onChange={setEmail}
            error={errors.email}
            autoComplete="email"
            required
          />
          <Field
            label={texts.auth.password}
            type="password"
            value={password}
            onChange={setPassword}
            hint={texts.auth.passwordHint}
            error={errors.password}
            autoComplete="new-password"
            required
          />
          <Field
            label={texts.auth.confirmPassword}
            type="password"
            value={confirmPassword}
            onChange={setConfirmPassword}
            error={passwordsDiffer ? texts.auth.passwordsDoNotMatch : undefined}
            autoComplete="new-password"
            required
          />
          <Field
            label={texts.auth.firstName}
            value={firstName}
            onChange={setFirstName}
            autoComplete="given-name"
            required
            maxLength={nameMaxLength}
          />
          <Field
            label={texts.auth.lastName}
            value={lastName}
            onChange={setLastName}
            autoComplete="family-name"
            maxLength={nameMaxLength}
          />
          {errors.form && (
            <p className="auth-error" role="alert">
              {errors.form}
            </p>
          )}
          <Button type="submit" loading={loading}>
            {texts.auth.createAccount}
          </Button>
        </form>
        <p className="auth-switch">
          {texts.auth.haveAccount} <Link to="/login">{texts.auth.toLogin}</Link>
        </p>
      </main>
    </>
  )
}
