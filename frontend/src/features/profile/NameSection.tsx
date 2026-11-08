import { useState, type SubmitEvent } from 'react'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { ProfileDto, UpdateProfileRequest } from '../../types'
import { useCurrentUser } from '../auth/currentUser'

const nameMaxLength = 100

export function NameSection() {
  const { user, setUser } = useCurrentUser()
  const [firstName, setFirstName] = useState(user.firstName ?? '')
  const [lastName, setLastName] = useState(user.lastName ?? '')
  const [firstNameError, setFirstNameError] = useState<string | undefined>()
  const [saving, setSaving] = useState(false)
  const showToast = useToast()

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setFirstNameError(undefined)
    setSaving(true)
    try {
      const request: UpdateProfileRequest = { firstName: firstName.trim(), lastName: lastName.trim() || null }
      const saved = await apiFetch<ProfileDto>('/me', { method: 'PUT', body: request })
      setUser(saved)
      setFirstName(saved.firstName ?? '')
      setLastName(saved.lastName ?? '')
      showToast(texts.profile.nameSaved, 'success')
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 400) {
        setFirstNameError(texts.profile.firstNameRequired)
      } else {
        showToast(errorMessage(caught), 'error')
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="profile-section" aria-labelledby="profile-name-title">
      <h2 id="profile-name-title">{texts.profile.nameTitle}</h2>
      <form className="profile-form" onSubmit={handleSubmit}>
        <p className="profile-email">
          <span>{texts.profile.email}</span>
          {user.email}
        </p>
        <div className="profile-names">
          <Field
            label={texts.profile.firstName}
            value={firstName}
            onChange={setFirstName}
            error={firstNameError}
            autoComplete="given-name"
            required
            maxLength={nameMaxLength}
          />
          <Field
            label={texts.profile.lastName}
            value={lastName}
            onChange={setLastName}
            hint={texts.profile.lastNameHint}
            autoComplete="family-name"
            maxLength={nameMaxLength}
          />
        </div>
        <Button type="submit" loading={saving}>
          {texts.profile.saveName}
        </Button>
      </form>
    </section>
  )
}
