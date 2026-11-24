import { useId, useRef, useState } from 'react'
import { Dialog } from '../../components/Dialog'
import { texts } from '../../texts'
import type { LookupsDto, RoundResult } from '../../types'
import { RoundEditor } from './RoundEditor'
import type { RoundValues } from './rounds'

const maxRounds = 50
const results: RoundResult[] = ['Win', 'Loss', 'Draw']

interface RoundsSectionProps {
  lookups: LookupsDto
  disciplineId: string
  rounds: RoundValues[]
  onAdd: (result: RoundResult) => void
  onUpdate: (key: number, patch: Partial<RoundValues>) => void
  onRemove: (key: number) => void
}

// One tap adds a round and the chips show their order
export function RoundsSection({ lookups, disciplineId, rounds, onAdd, onUpdate, onRemove }: RoundsSectionProps) {
  const [editingKey, setEditingKey] = useState<number | null>(null)
  const winRef = useRef<HTMLButtonElement>(null)
  const removed = useRef(false)
  const labelId = useId()
  const full = rounds.length >= maxRounds
  const count = (result: RoundResult) => rounds.filter((round) => round.result === result).length
  const techniqueName = (id: string) => lookups.techniques.find((item) => String(item.id) === id)?.name ?? null
  const editingIndex = rounds.findIndex((round) => round.key === editingKey)
  const editing = editingIndex >= 0 ? rounds[editingIndex] : null

  function remove(key: number) {
    removed.current = true
    onRemove(key)
  }

  // Moves focus to the Win button when the edited chip is removed
  function closeEditor() {
    setEditingKey(null)
    if (removed.current) {
      removed.current = false
      winRef.current?.focus()
    }
  }

  return (
    <div>
      <span id={labelId} className="sessions-label">
        {texts.sessions.rounds}
      </span>
      <div role="group" className="sessions-results" aria-label={texts.sessions.addRound}>
        {results.map((result) => (
          <button
            key={result}
            ref={result === 'Win' ? winRef : undefined}
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
                <button type="button" className="sessions-chip" aria-haspopup="dialog" onClick={() => setEditingKey(round.key)}>
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

      <Dialog
        open={editing !== null}
        title={texts.sessions.roundTitle(editingIndex + 1)}
        className="sessions-sheet"
        onClose={closeEditor}
      >
        {editing && (
          <RoundEditor
            lookups={lookups}
            disciplineId={disciplineId}
            round={editing}
            onChange={(patch) => onUpdate(editing.key, patch)}
            onRemove={() => remove(editing.key)}
            onDone={() => setEditingKey(null)}
          />
        )}
      </Dialog>
    </div>
  )
}
