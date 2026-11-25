import { useId } from 'react'
import { Button } from '../../components/Button'
import { SegmentedControl } from '../../components/SegmentedControl'
import { Select, type SelectOption } from '../../components/Select'
import { techniqueGroups } from '../../lib/techniques'
import { texts } from '../../texts'
import type { LookupsDto, RoundResult } from '../../types'
import { positionOptions, type RoundErrors, type RoundValues } from './rounds'

const notesMaxLength = 500
const results: RoundResult[] = ['Win', 'Loss', 'Draw']

interface RoundEditorProps {
  lookups: LookupsDto
  disciplineId: string
  round: RoundValues
  errors: RoundErrors
  onChange: (patch: Partial<RoundValues>) => void
  onRemove: () => void
  onDone: () => void
}

// Changes are applied immediately and preserved when the dialog closes
export function RoundEditor({ lookups, disciplineId, round, errors, onChange, onRemove, onDone }: RoundEditorProps) {
  const notesId = useId()
  const groups = techniqueGroups(lookups, disciplineId)
  const positions = positionOptions(lookups, disciplineId)

  // A saved technique or position that the discipline does not offer stays visible, so it can be seen and changed
  const techniqueOffered = groups.some((group) => group.options.some((option) => option.value === round.techniqueId))
  const positionOffered = positions.some((option) => option.value === round.positionId)
  const extraOption = (id: string, offered: boolean, items: { id: number; name: string }[]): SelectOption[] => {
    const item = id && !offered ? items.find((entry) => String(entry.id) === id) : undefined
    return item ? [{ value: id, label: item.name }] : []
  }

  // A draw has no technique or position
  function changeResult(result: RoundResult) {
    onChange(result === 'Draw' ? { result, techniqueId: '', positionId: '' } : { result })
  }

  // A position needs a technique
  function changeTechnique(techniqueId: string) {
    onChange(techniqueId ? { techniqueId } : { techniqueId: '', positionId: '' })
  }

  // Shows errors for hidden fields at the top when the result is a draw
  const draw = round.result === 'Draw'
  const topErrors = [errors.round, draw ? errors.techniqueId : undefined, draw ? errors.positionId : undefined]
    .filter((message) => message !== undefined)
    .join('\n')

  return (
    <div className="sessions-sheet-body">
      {topErrors && <p className="sessions-error">{topErrors}</p>}
      <SegmentedControl
        label={texts.sessions.result}
        options={results.map((result) => ({ value: result, label: texts.sessions.results[result] }))}
        value={round.result}
        onChange={changeResult}
      />

      {!draw && (
        <>
          {!disciplineId && <p className="sessions-muted">{texts.sessions.chooseDisciplineFirst}</p>}
          <Select
            label={texts.sessions.technique}
            value={round.techniqueId}
            options={[
              { value: '', label: texts.sessions.noTechnique },
              ...extraOption(round.techniqueId, techniqueOffered, lookups.techniques),
            ]}
            groups={groups}
            onChange={changeTechnique}
            error={errors.techniqueId}
            disabled={!disciplineId}
          />
          <Select
            label={texts.sessions.position}
            value={round.positionId}
            options={[
              { value: '', label: texts.sessions.noPosition },
              ...extraOption(round.positionId, positionOffered, lookups.positions),
              ...positions,
            ]}
            onChange={(positionId) => onChange({ positionId })}
            error={errors.positionId}
            disabled={!disciplineId || !round.techniqueId}
          />
        </>
      )}

      <div className="field">
        <label htmlFor={notesId}>{texts.sessions.notes}</label>
        <textarea
          id={notesId}
          className="sessions-notes"
          value={round.notes}
          onChange={(event) => onChange({ notes: event.target.value })}
          maxLength={notesMaxLength}
          rows={3}
        />
      </div>

      <div className="sessions-sheet-actions">
        <Button variant="danger" onClick={onRemove}>
          {texts.sessions.removeRound}
        </Button>
        <Button onClick={onDone}>{texts.common.done}</Button>
      </div>
    </div>
  )
}
