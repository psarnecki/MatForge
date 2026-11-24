import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { apiFetch, errorMessage } from '../../api/apiFetch'
import { useLookups } from '../../api/useLookups'
import { parseIsoDate } from '../../lib/dates'
import { texts } from '../../texts'
import type { CompetitionSummaryDto, LookupsDto } from '../../types'
import { PlaceText } from './PlaceText'

// Whole history, newest first (sorted by the server)
export function CompetitionsPage() {
  const { lookups, error: lookupsError } = useLookups()
  const [competitions, setCompetitions] = useState<CompetitionSummaryDto[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    let ignore = false
    apiFetch<CompetitionSummaryDto[]>('/competitions')
      .then((loaded) => {
        if (!ignore) {
          setCompetitions(loaded)
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setLoadError(errorMessage(caught))
        }
      })
    return () => {
      ignore = true
    }
  }, [])

  const error = loadError ?? lookupsError
  const date = (iso: string) => texts.competitions.shortDate(parseIsoDate(iso)!)
  const record = (competition: CompetitionSummaryDto) =>
    competition.fightsCount > 0 && (
      <span
        className="competitions-record"
        aria-label={texts.competitions.recordLabel(competition.wins, competition.losses, competition.draws)}
      >
        {texts.competitions.recordText(competition.wins, competition.losses, competition.draws)}
      </span>
    )

  return (
    <div className="competitions-page">
      <h1>{texts.competitions.title}</h1>
      <div className="competitions-bar">
        <Link to="/competitions/new" className="button button-primary">
          {texts.competitions.addCompetition}
        </Link>
      </div>
      <div className="competitions-content">
        {error ? (
          <p className="competitions-error" role="alert">
            {error}
          </p>
        ) : !competitions || !lookups ? (
          <p className="competitions-muted">{texts.common.loading}</p>
        ) : competitions.length === 0 ? (
          <p className="competitions-muted">{texts.competitions.empty}</p>
        ) : (
          <>
            <table className="competitions-table">
              <thead>
                <tr>
                  <th scope="col">{texts.competitions.date}</th>
                  <th scope="col">{texts.competitions.competition}</th>
                  <th scope="col">{texts.competitions.place}</th>
                  <th scope="col">{texts.competitions.record}</th>
                </tr>
              </thead>
              <tbody>
                {competitions.map((competition) => (
                  <tr key={competition.id}>
                    <td className="competitions-date">{date(competition.date)}</td>
                    <td>
                      <Link to={`/competitions/${competition.id}`} className="competitions-name">
                        {competition.name}
                      </Link>
                      <span className="competitions-muted competitions-meta">{disciplineText(competition, lookups)}</span>
                    </td>
                    <td>
                      <PlaceText place={competition.place} />
                    </td>
                    <td>{record(competition)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <ul className="competitions-list">
              {competitions.map((competition) => (
                <li key={competition.id}>
                  <Link to={`/competitions/${competition.id}`} className="competitions-item">
                    <span>
                      <strong>{competition.name}</strong>
                      <span className="competitions-muted competitions-meta">
                        {[date(competition.date), disciplineText(competition, lookups)].join(', ')}
                      </span>
                    </span>
                    <span className="competitions-item-result">
                      <PlaceText place={competition.place} />
                      {record(competition)}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          </>
        )}
      </div>
    </div>
  )
}

// Includes the format only when the discipline has multiple formats ("Brazilian Jiu-Jitsu, No-Gi")
function disciplineText(competition: CompetitionSummaryDto, lookups: LookupsDto) {
  const discipline = lookups.disciplines.find((item) => item.id === competition.disciplineId)
  const format = discipline?.formats.find((item) => item.id === competition.formatId)
  return [discipline?.name, discipline && discipline.formats.length > 1 ? format?.name : null].filter(Boolean).join(', ')
}
