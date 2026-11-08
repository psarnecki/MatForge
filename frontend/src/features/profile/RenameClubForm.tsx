import { useState, type SubmitEvent } from 'react'
import { apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { ClubDto, ClubRequest } from '../../types'
import { clubNameError } from './clubErrors'

interface RenameClubFormProps {
  club: ClubDto
  onCancel: () => void
  onRenamed: () => void
}

export function RenameClubForm({ club, onCancel, onRenamed }: RenameClubFormProps) {
  const [name, setName] = useState(club.name)
  const [error, setError] = useState<string | undefined>()
  const [saving, setSaving] = useState(false)
  const showToast = useToast()

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(undefined)
    setSaving(true)
    try {
      const request: ClubRequest = { name: name.trim() }
      await apiFetch<ClubDto>(`/clubs/${club.id}`, { method: 'PUT', body: request })
      onRenamed()
    } catch (caught) {
      const fieldError = clubNameError(caught)
      if (fieldError) {
        setError(fieldError)
      } else {
        showToast(errorMessage(caught), 'error')
      }
      setSaving(false)
    }
  }

  return (
    <form className="profile-form" onSubmit={handleSubmit}>
      <Field label={texts.profile.clubName} value={name} onChange={setName} error={error} required maxLength={100} />
      <div className="profile-dialog-actions">
        <Button variant="secondary" onClick={onCancel}>
          {texts.common.cancel}
        </Button>
        <Button type="submit" loading={saving}>
          {texts.profile.saveClub}
        </Button>
      </div>
    </form>
  )
}
