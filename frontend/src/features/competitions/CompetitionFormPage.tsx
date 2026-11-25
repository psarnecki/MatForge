import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { useLookups } from '../../api/useLookups'
import { Dialog } from '../../components/Dialog'
import { todayIso } from '../../lib/dates'
import { texts } from '../../texts'
import type { CompetitionDto } from '../../types'
import { CompetitionForm, type CompetitionFormValues } from './CompetitionForm'
import { DeleteCompetitionForm } from './DeleteCompetitionForm'

// Creates a new competition or edits an existing one based on the route
export function CompetitionFormPage() {
  const { id } = useParams()
  const { lookups, error: lookupsError } = useLookups()
  // Undefined while loading, null for a new competition
  const [competition, setCompetition] = useState<CompetitionDto | null | undefined>(id ? undefined : null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [deleteOpen, setDeleteOpen] = useState(false)

  useEffect(() => {
    if (!id) {
      return
    }
    let ignore = false
    apiFetch<CompetitionDto>(`/competitions/${id}`)
      .then((loaded) => {
        if (!ignore) {
          setCompetition(loaded)
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setLoadError(caught instanceof ApiError && caught.status === 404 ? texts.competitions.notFound : errorMessage(caught))
        }
      })
    return () => {
      ignore = true
    }
  }, [id])

  const error = lookupsError ?? loadError

  return (
    <div className="competitions-form-page">
      <Link to="/competitions" className="competitions-back">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
          <path d="M15 5l-7 7 7 7" />
        </svg>
        {texts.competitions.backToCompetitions}
      </Link>
      <h1>{id ? texts.competitions.editCompetitionTitle : texts.competitions.newCompetitionTitle}</h1>
      {error ? (
        <p className="competitions-error" role="alert">
          {error}
        </p>
      ) : !lookups || competition === undefined ? (
        <p className="competitions-muted">{texts.common.loading}</p>
      ) : (
        <>
          <CompetitionForm
            lookups={lookups}
            competitionId={competition?.id}
            initialValues={competition ? competitionValues(competition) : newCompetitionValues()}
            onDelete={competition ? () => setDeleteOpen(true) : undefined}
          />
          {competition && (
            <Dialog open={deleteOpen} title={texts.competitions.deleteCompetition} onClose={() => setDeleteOpen(false)}>
              <DeleteCompetitionForm competition={competition} onCancel={() => setDeleteOpen(false)} />
            </Dialog>
          )}
        </>
      )}
    </div>
  )
}

function newCompetitionValues(): CompetitionFormValues {
  return {
    name: '',
    date: todayIso(),
    location: '',
    disciplineId: '',
    formatId: '',
    ageCategory: '',
    skillLevel: '',
    weightCategory: '',
    place: '',
    resultNote: '',
    notes: '',
    fights: [],
  }
}

function competitionValues(competition: CompetitionDto): CompetitionFormValues {
  return {
    name: competition.name,
    date: competition.date,
    location: competition.location ?? '',
    disciplineId: String(competition.disciplineId),
    formatId: String(competition.formatId),
    ageCategory: competition.ageCategory ?? '',
    skillLevel: competition.skillLevel ?? '',
    weightCategory: competition.weightCategory ?? '',
    place: competition.place === null ? '' : String(competition.place),
    resultNote: competition.resultNote ?? '',
    notes: competition.notes ?? '',
    fights: competition.fights,
  }
}
