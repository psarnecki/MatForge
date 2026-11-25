import { useId, useRef, useState, type KeyboardEvent } from 'react'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { SegmentedControl } from '../../components/SegmentedControl'
import { Select, type SelectOption } from '../../components/Select'
import { techniqueGroups } from '../../lib/techniques'
import { texts } from '../../texts'
import type { FightResult, LookupsDto } from '../../types'
import { allowsTechnique, fightMethods, fightResults, isDurationValid, type FightErrors, type FightValues } from './fights'

const notesMaxLength = 500

interface FightEditorProps {
  lookups: LookupsDto
  disciplineId: string
  fight: FightValues
  errors: FightErrors
  onChange: (patch: Partial<FightValues>) => void
  onRemove: () => void
  onDone: () => void
}

// Changes are applied immediately and preserved when the dialog closes
export function FightEditor({ lookups, disciplineId, fight, errors, onChange, onRemove, onDone }: FightEditorProps) {
  const notesId = useId()
  const durationRef = useRef<HTMLDivElement>(null)
  const [durationError, setDurationError] = useState<string | null>(null)
  const groups = techniqueGroups(lookups, disciplineId)

  // A saved technique that the discipline does not offer stays visible, so it can be seen and changed
  const techniqueOffered = groups.some((group) => group.options.some((option) => option.value === fight.techniqueId))
  const savedTechnique = fight.techniqueId && !techniqueOffered
    ? lookups.techniques.find((item) => String(item.id) === fight.techniqueId)
    : undefined
  const extraOption: SelectOption[] = savedTechnique ? [{ value: fight.techniqueId, label: savedTechnique.name }] : []

  // A draw has no method and no technique
  function changeResult(result: FightResult) {
    onChange(result === 'Draw' ? { result, method: '', techniqueId: '' } : { result })
  }

  // Only a submission, throw or pin keeps its technique
  function changeMethod(text: string) {
    const method = fightMethods.find((item) => item === text) ?? ''
    onChange(allowsTechnique(method) ? { method } : { method, techniqueId: '' })
  }

  // Done checks duration, while other close actions bypass the check
  function done() {
    if (!isDurationValid(fight.duration)) {
      setDurationError(texts.competitions.durationInvalid)
      durationRef.current?.querySelector('input')?.focus()
      return
    }
    onDone()
  }

  // Enter acts as Done to prevent submitting the competition form
  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Enter' && event.target instanceof HTMLInputElement) {
      event.preventDefault()
      done()
    }
  }

  // Shows errors for hidden fields at the top
  const draw = fight.result === 'Draw'
  const showTechnique = !draw && allowsTechnique(fight.method)
  const topErrors = [errors.fight, draw ? errors.method : undefined, showTechnique ? undefined : errors.techniqueId]
    .filter((message) => message !== undefined)
    .join('\n')

  return (
    <div className="competitions-sheet-body" onKeyDown={handleKeyDown}>
      {topErrors && <p className="competitions-error">{topErrors}</p>}
      <SegmentedControl
        label={texts.competitions.result}
        options={fightResults.map((result) => ({ value: result, label: texts.competitions.results[result] }))}
        value={fight.result}
        onChange={changeResult}
        error={errors.result}
      />

      {!draw && (
        <Select
          label={texts.competitions.method}
          value={fight.method}
          options={[
            { value: '', label: texts.competitions.choose },
            ...fightMethods.map((method) => ({ value: method, label: texts.competitions.methods[method] })),
          ]}
          onChange={changeMethod}
          error={errors.method}
        />
      )}

      {showTechnique && (
        <>
          {!disciplineId && <p className="competitions-muted">{texts.competitions.chooseDisciplineFirst}</p>}
          <Select
            label={texts.competitions.technique}
            value={fight.techniqueId}
            options={[{ value: '', label: texts.competitions.noTechnique }, ...extraOption]}
            groups={groups}
            onChange={(techniqueId) => onChange({ techniqueId })}
            error={errors.techniqueId}
            disabled={!disciplineId}
          />
        </>
      )}

      <div ref={durationRef}>
        <Field
          label={texts.competitions.duration}
          value={fight.duration}
          onChange={(duration) => {
            setDurationError(null)
            onChange({ duration })
          }}
          hint={texts.competitions.durationHint}
          error={durationError ?? errors.durationSeconds}
        />
      </div>
      <Field
        label={texts.competitions.opponent}
        value={fight.opponentName}
        onChange={(opponentName) => onChange({ opponentName })}
        error={errors.opponentName}
        maxLength={100}
      />

      <div className="field">
        <label htmlFor={notesId}>{texts.competitions.notes}</label>
        <textarea
          id={notesId}
          className="competitions-notes"
          value={fight.notes}
          onChange={(event) => onChange({ notes: event.target.value })}
          maxLength={notesMaxLength}
          rows={3}
          aria-invalid={errors.notes ? true : undefined}
          aria-describedby={errors.notes ? `${notesId}-error` : undefined}
        />
        {errors.notes && (
          <p id={`${notesId}-error`} className="field-error">
            {errors.notes}
          </p>
        )}
      </div>

      <div className="competitions-sheet-actions">
        <Button variant="danger" onClick={onRemove}>
          {texts.competitions.removeFight}
        </Button>
        <Button onClick={done}>{texts.common.done}</Button>
      </div>
    </div>
  )
}
