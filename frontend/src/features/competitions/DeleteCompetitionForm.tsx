import { useState } from 'react'
import { useNavigate } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { CompetitionDto } from '../../types'

interface DeleteCompetitionFormProps {
  competition: CompetitionDto
  onCancel: () => void
}

export function DeleteCompetitionForm({ competition, onCancel }: DeleteCompetitionFormProps) {
  // Server message when a goal still uses the competition
  const [conflict, setConflict] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const navigate = useNavigate()
  const showToast = useToast()

  async function handleDelete() {
    setDeleting(true)
    try {
      await apiFetch(`/competitions/${competition.id}`, { method: 'DELETE' })
      showToast(texts.competitions.competitionDeleted, 'success')
      navigate('/competitions')
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
    <div className="competitions-dialog">
      <p>{texts.competitions.deleteCompetitionQuestion(competition.name)}</p>
      {conflict && (
        <p className="competitions-error" role="alert">
          {conflict}
        </p>
      )}
      <div className="competitions-dialog-actions">
        <Button variant="secondary" onClick={onCancel}>
          {texts.common.cancel}
        </Button>
        <Button loading={deleting} disabled={conflict !== null} onClick={handleDelete}>
          {texts.competitions.deleteCompetition}
        </Button>
      </div>
    </div>
  )
}
