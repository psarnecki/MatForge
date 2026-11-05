import { useCallback, useEffect, useMemo, useState } from 'react'
import { Outlet, useNavigate } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { ProfileDto } from '../../types'
import { CurrentUserContext } from './currentUser'

// Loads the signed-in user; a 401 from /me sends the visitor to /login (apiFetch)
export function RequireAuth() {
  const [user, setUser] = useState<ProfileDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()
  const showToast = useToast()

  useEffect(() => {
    let ignore = false
    apiFetch<ProfileDto>('/me')
      .then((me) => {
        if (!ignore) {
          setUser(me)
        }
      })
      .catch((caught: unknown) => {
        if (!ignore && !(caught instanceof ApiError && caught.status === 401)) {
          setError(errorMessage(caught))
        }
      })
    return () => {
      ignore = true
    }
  }, [])

  const logout = useCallback(async () => {
    try {
      await apiFetch('/auth/logout', { method: 'POST' })
      setUser(null)
      navigate('/login', { replace: true })
    } catch {
      showToast(texts.auth.logoutFailed, 'error')
    }
  }, [navigate, showToast])

  const value = useMemo(() => (user ? { user, logout } : null), [user, logout])

  if (error) {
    return (
      <p className="auth-guard-error" role="alert">
        {error}
      </p>
    )
  }
  if (!value) {
    return null
  }
  return (
    <CurrentUserContext.Provider value={value}>
      <Outlet />
    </CurrentUserContext.Provider>
  )
}
