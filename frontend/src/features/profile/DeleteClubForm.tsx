import { useState } from 'react'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { ClubDto } from '../../types'

interface DeleteClubFormProps {
  club: ClubDto
  onCancel: () => void
  onDeleted: () => void
}

export function DeleteClubForm({ club, onCancel, onDeleted }: DeleteClubFormProps) {
  // The server's message when the club is still used by sessions
  const [conflict, setConflict] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const showToast = useToast()

  async function handleDelete() {
    setDeleting(true)
    try {
      await apiFetch(`/clubs/${club.id}`, { method: 'DELETE' })
      onDeleted()
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 409) {
        setConflict(errorMessage(caught))
      } else {
        showToast(errorMessage(caught), 'error')
      }
      setDeleting(false)
    }
  }

  return (
    <div className="profile-form">
      <p>{texts.profile.deleteClubQuestion(club.name)}</p>
      {conflict && (
        <p className="profile-error" role="alert">
          {conflict}
        </p>
      )}
      <div className="profile-dialog-actions">
        <Button variant="secondary" onClick={onCancel}>
          {texts.common.cancel}
        </Button>
        <Button loading={deleting} disabled={conflict !== null} onClick={handleDelete}>
          {texts.profile.deleteClub}
        </Button>
      </div>
    </div>
  )
}
