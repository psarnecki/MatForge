import { useId } from 'react'
import { texts } from '../../texts'
import type { LookupsDto, RoundResult } from '../../types'
import type { RoundValues } from './rounds'

const maxRounds = 50
const results: RoundResult[] = ['Win', 'Loss', 'Draw']

interface RoundsSectionProps {
  lookups: LookupsDto
  rounds: RoundValues[]
  onAdd: (result: RoundResult) => void
}

// One tap adds a round and the chips show their order
export function RoundsSection({ lookups, rounds, onAdd }: RoundsSectionProps) {
  const labelId = useId()
  const full = rounds.length >= maxRounds
  const count = (result: RoundResult) => rounds.filter((round) => round.result === result).length
  const techniqueName = (id: string) => lookups.techniques.find((item) => String(item.id) === id)?.name ?? null

  return (
    <div>
      <span id={labelId} className="sessions-label">
        {texts.sessions.rounds}
      </span>
      <div role="group" className="sessions-results" aria-label={texts.sessions.addRound}>
        {results.map((result) => (
          <button
            key={result}
            type="button"
            className={`sessions-result sessions-result-${result.toLowerCase()}`}
            disabled={full}
            onClick={() => onAdd(result)}
          >
            {texts.sessions.results[result]}
          </button>
        ))}
      </div>
      {full && <p className="sessions-muted sessions-rounds-note">{texts.sessions.roundsLimit(maxRounds)}</p>}
      {rounds.length > 0 && (
        <ol className="sessions-chips" aria-labelledby={labelId}>
          {rounds.map((round, index) => {
            // Win and Loss are colored only when a technique is selected
            const marked = round.result !== 'Draw' && round.techniqueId !== ''
            return (
              <li key={round.key}>
                <button type="button" className="sessions-chip">
                  {marked && <span className={`sessions-chip-marker sessions-chip-marker-${round.result.toLowerCase()}`} />}
                  {texts.sessions.roundChip(index + 1, texts.sessions.results[round.result], techniqueName(round.techniqueId))}
                </button>
              </li>
            )
          })}
        </ol>
      )}
      {/* Keeps added rounds accessible to screen readers */}
      <p className="sessions-muted sessions-summary" role="status">
        {rounds.length > 0 ? texts.sessions.roundsSummary(count('Win'), count('Loss'), count('Draw')) : ''}
      </p>
    </div>
  )
}
