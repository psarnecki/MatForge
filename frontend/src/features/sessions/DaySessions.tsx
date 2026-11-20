import { Link } from 'react-router'
import { useLookups } from '../../api/useLookups'
import { parseIsoDate } from '../../lib/dates'
import { texts } from '../../texts'
import type { LookupsDto, SessionSummaryDto } from '../../types'

interface DaySessionsProps {
  date: string
  sessions: SessionSummaryDto[] | null
  error: string | null
}

export function DaySessions({ date, sessions, error }: DaySessionsProps) {
  const { lookups, error: lookupsError } = useLookups()
  const shownError = error ?? lookupsError

  return (
    <section className="sessions-day-panel">
      <h2>{texts.sessions.dayTitle(parseIsoDate(date)!)}</h2>
      {shownError ? (
        <p className="sessions-error" role="alert">
          {shownError}
        </p>
      ) : !sessions || !lookups ? (
        <p className="sessions-muted">{texts.common.loading}</p>
      ) : sessions.length === 0 ? (
        <p className="sessions-muted">{texts.sessions.dayEmpty}</p>
      ) : (
        <ul className="sessions-list">
          {sessions.map((session) => (
            <li key={session.id}>
              <Link to={`/training/${session.id}`} className="sessions-item">
                <span>
                  <strong>{sessionTitle(session, lookups)}</strong>
                  <span className="sessions-muted">{texts.sessions.sessionMeta(session.durationMinutes, session.feeling)}</span>
                </span>
                {session.roundsCount > 0 && (
                  <span className="sessions-record" aria-label={texts.sessions.recordLabel(session.wins, session.losses, session.draws)}>
                    {texts.sessions.record(session.wins, session.losses, session.draws)}
                  </span>
                )}
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

// "Sparring, Brazilian Jiu-Jitsu, No-Gi" (format omitted for single-format disciplines)
function sessionTitle(session: SessionSummaryDto, lookups: LookupsDto) {
  if (session.kind === 'Other') {
    return lookups.activityTypes.find((type) => type.id === session.activityTypeId)?.name
  }
  const character = lookups.characters.find((item) => item.id === session.characterId)
  const discipline = lookups.disciplines.find((item) => item.id === session.disciplineId)
  const format = discipline?.formats.find((item) => item.id === session.formatId)
  const parts = [character?.name, discipline?.name, discipline && discipline.formats.length > 1 ? format?.name : null]
  return parts.filter(Boolean).join(', ')
}
