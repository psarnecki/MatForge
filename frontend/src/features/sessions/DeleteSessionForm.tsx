import { useState } from 'react'
import { useNavigate } from 'react-router'
import { apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { useToast } from '../../components/useToast'
import { parseIsoDate } from '../../lib/dates'
import { texts } from '../../texts'
import type { SessionDto } from '../../types'

interface DeleteSessionFormProps {
  session: SessionDto
  onCancel: () => void
}

export function DeleteSessionForm({ session, onCancel }: DeleteSessionFormProps) {
  const [deleting, setDeleting] = useState(false)
  const navigate = useNavigate()
  const showToast = useToast()

  async function handleDelete() {
    setDeleting(true)
    try {
      await apiFetch(`/sessions/${session.id}`, { method: 'DELETE' })
      showToast(texts.sessions.sessionDeleted, 'success')
      navigate(`/training?day=${session.date}`)
    } catch (caught) {
      showToast(errorMessage(caught), 'error')
      setDeleting(false)
    }
  }

  return (
    <div className="sessions-dialog">
      <p>{texts.sessions.deleteSessionQuestion(parseIsoDate(session.date)!)}</p>
      <div className="sessions-dialog-actions">
        <Button variant="secondary" onClick={onCancel}>
          {texts.common.cancel}
        </Button>
        <Button loading={deleting} onClick={handleDelete}>
          {texts.sessions.deleteSession}
        </Button>
      </div>
    </div>
  )
}
