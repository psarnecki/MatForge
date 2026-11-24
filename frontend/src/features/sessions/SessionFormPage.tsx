import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { useLookups } from '../../api/useLookups'
import { Dialog } from '../../components/Dialog'
import { parseIsoDate, todayIso } from '../../lib/dates'
import { texts } from '../../texts'
import type { ClubDto, LookupsDto, SessionDto } from '../../types'
import { DeleteSessionForm } from './DeleteSessionForm'
import { readLastSession } from './lastSession'
import { toRoundValues } from './rounds'
import { SessionForm, type SessionFormValues } from './SessionForm'

interface Loaded {
  clubs: ClubDto[]
  // Null when creating a new session
  session: SessionDto | null
}

// Creates a new session or edits an existing one based on the route
export function SessionFormPage() {
  const { id } = useParams()
  const [searchParams] = useSearchParams()
  const { lookups, error: lookupsError } = useLookups()
  const [loaded, setLoaded] = useState<Loaded | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [deleteOpen, setDeleteOpen] = useState(false)

  // Keep the selected calendar date and let the server validate it on save
  const day = searchParams.get('day')
  const newDate = day && parseIsoDate(day) ? day : todayIso()

  // Fetch fresh clubs because the current user's club list may be outdated
  useEffect(() => {
    let ignore = false
    Promise.all([apiFetch<ClubDto[]>('/clubs'), id ? apiFetch<SessionDto>(`/sessions/${id}`) : null])
      .then(([clubs, session]) => {
        if (!ignore) {
          setLoaded({ clubs, session })
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setLoadError(caught instanceof ApiError && caught.status === 404 ? texts.sessions.notFound : errorMessage(caught))
        }
      })
    return () => {
      ignore = true
    }
  }, [id])

  const error = lookupsError ?? loadError
  const session = loaded?.session ?? null

  return (
    <div className="sessions-form-page">
      <Link to={`/training?day=${session?.date ?? newDate}`} className="sessions-back">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
          <path d="M15 5l-7 7 7 7" />
        </svg>
        {texts.sessions.backToTraining}
      </Link>
      <h1>{id ? texts.sessions.editSessionTitle : texts.sessions.newSessionTitle}</h1>
      {error ? (
        <p className="sessions-error" role="alert">
          {error}
        </p>
      ) : !lookups || !loaded ? (
        <p className="sessions-muted">{texts.common.loading}</p>
      ) : (
        <>
          <SessionForm
            lookups={lookups}
            clubs={loaded.clubs}
            sessionId={session?.id}
            initialValues={session ? sessionValues(session) : newSessionValues(lookups, loaded.clubs, newDate)}
            onDelete={session ? () => setDeleteOpen(true) : undefined}
          />
          {session && (
            <Dialog open={deleteOpen} title={texts.sessions.deleteSession} onClose={() => setDeleteOpen(false)}>
              <DeleteSessionForm session={session} onCancel={() => setDeleteOpen(false)} />
            </Dialog>
          )}
        </>
      )}
    </div>
  )
}

// Use the selected date and available fields from the last session as defaults
function newSessionValues(lookups: LookupsDto, clubs: ClubDto[], date: string): SessionFormValues {
  const last = readLastSession()
  const discipline = lookups.disciplines.find((item) => item.id === last.disciplineId)
  const format = discipline?.formats.find((item) => item.id === last.formatId)
  const character = lookups.characters.find((item) => item.id === last.characterId)
  // Fall back to Other if the saved club no longer exists
  const club = clubs.find((item) => item.id === last.clubId)
  const idText = (item: { id: number } | undefined) => (item ? String(item.id) : '')

  return {
    kind: 'Grappling',
    date,
    duration: '',
    feeling: '',
    disciplineId: idText(discipline),
    formatId: idText(format),
    characterId: idText(character),
    clubId: idText(club),
    activityTypeId: '',
    notes: '',
    rounds: [],
  }
}

function sessionValues(session: SessionDto): SessionFormValues {
  const idText = (id: number | null) => (id === null ? '' : String(id))
  return {
    kind: session.kind,
    date: session.date,
    duration: String(session.durationMinutes),
    feeling: String(session.feeling),
    disciplineId: idText(session.disciplineId),
    formatId: idText(session.formatId),
    characterId: idText(session.characterId),
    clubId: idText(session.clubId),
    activityTypeId: idText(session.activityTypeId),
    notes: session.notes ?? '',
    rounds: session.rounds.map(toRoundValues),
  }
}
