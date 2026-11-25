import { useId, useRef, useState } from 'react'
import { Button } from '../../components/Button'
import { Dialog } from '../../components/Dialog'
import { texts } from '../../texts'
import type { LookupsDto } from '../../types'
import { allowsTechnique, maxFights, type FightErrors, type FightValues } from './fights'
import { FightEditor } from './FightEditor'

interface FightsSectionProps {
  lookups: LookupsDto
  disciplineId: string
  fights: FightValues[]
  notice: string | null
  // Stores server errors by fight key until the next save
  errors: Record<number, FightErrors>
  // Returns the new fight key to open its editor
  onAdd: () => number
  onUpdate: (key: number, patch: Partial<FightValues>) => void
  onRemove: (key: number) => void
}

// Compact fight rows in saved order, each opening an editor
export function FightsSection({ lookups, disciplineId, fights, notice, errors, onAdd, onUpdate, onRemove }: FightsSectionProps) {
  const [editingKey, setEditingKey] = useState<number | null>(null)
  const addRef = useRef<HTMLDivElement>(null)
  const removed = useRef(false)
  const labelId = useId()
  const full = fights.length >= maxFights
  const editingIndex = fights.findIndex((fight) => fight.key === editingKey)
  const editing = editingIndex >= 0 ? fights[editingIndex] : null
  const techniqueName = (id: string) => lookups.techniques.find((item) => String(item.id) === id)?.name

  function remove(key: number) {
    removed.current = true
    onRemove(key)
  }

  // Moves focus to Add fight when the edited fight is removed
  function closeEditor() {
    setEditingKey(null)
    if (removed.current) {
      removed.current = false
      addRef.current?.querySelector('button')?.focus()
    }
  }

  return (
    <div>
      <span id={labelId} className="field-label">
        {texts.competitions.fightsLabel(fights.length)}
      </span>
      {notice && (
        <p className="competitions-muted competitions-fights-note" role="status">
          {notice}
        </p>
      )}
      {fights.length > 0 && (
        <ol className="competitions-fights" aria-labelledby={labelId}>
          {fights.map((fight, index) => {
            const method = fight.method ? texts.competitions.methods[fight.method] : null
            const technique = allowsTechnique(fight.method) ? techniqueName(fight.techniqueId) : null
            const details = [method, technique].filter(Boolean).join(', ')
            const invalid = errors[fight.key] !== undefined
            const opponent = fight.opponentName.trim()
            const result = fight.result ? texts.competitions.results[fight.result] : texts.competitions.noResult
            const rowText = [index + 1, opponent || texts.competitions.noOpponent, details, result, fight.duration.trim()]
              .filter(Boolean)
              .join(', ')
            return (
              <li key={fight.key}>
                <button
                  type="button"
                  className={invalid ? 'competitions-fight competitions-fight-invalid' : 'competitions-fight'}
                  aria-haspopup="dialog"
                  aria-label={invalid ? texts.competitions.fightInvalid(rowText) : undefined}
                  onClick={() => setEditingKey(fight.key)}
                >
                  <span className="competitions-fight-number">{index + 1}</span>
                  <span>
                    <span className={opponent ? 'competitions-fight-opponent' : 'competitions-fight-opponent competitions-muted'}>
                      {opponent || texts.competitions.noOpponent}
                    </span>
                    {details && <span className="competitions-muted competitions-meta">{details}</span>}
                  </span>
                  <span className="competitions-fight-result">
                    {fight.result ? (
                      <span className={`competitions-fight-outcome competitions-fight-outcome-${fight.result.toLowerCase()}`}>
                        {result}
                      </span>
                    ) : (
                      <span className="competitions-muted">{result}</span>
                    )}
                    {fight.duration.trim() && <span className="competitions-muted competitions-meta">{fight.duration.trim()}</span>}
                  </span>
                </button>
              </li>
            )
          })}
        </ol>
      )}
      <div ref={addRef} className="competitions-add-fight">
        <Button variant="secondary" disabled={full} onClick={() => setEditingKey(onAdd())}>
          {texts.competitions.addFight}
        </Button>
      </div>
      {full && <p className="competitions-muted competitions-fights-note">{texts.competitions.fightsLimit(maxFights)}</p>}

      <Dialog
        open={editing !== null}
        title={texts.competitions.fightTitle(editingIndex + 1)}
        className="dialog-sheet"
        onClose={closeEditor}
      >
        {editing && (
          <FightEditor
            lookups={lookups}
            disciplineId={disciplineId}
            fight={editing}
            errors={errors[editing.key] ?? {}}
            onChange={(patch) => onUpdate(editing.key, patch)}
            onRemove={() => remove(editing.key)}
            onDone={() => setEditingKey(null)}
          />
        )}
      </Dialog>
    </div>
  )
}
