import { useEffect, useId, useRef, useState, type SubmitEvent } from 'react'
import { useNavigate } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { Select, type SelectOption } from '../../components/Select'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { CompetitionDto, CompetitionRequest, LookupsDto } from '../../types'
import { maxFights, notOfferedFights, toFightRequest, type FightErrors, type FightValues } from './fights'
import { FightsSection } from './FightsSection'
import { PlacePicker } from './PlacePicker'

// Form values (also ids and numbers) as text, with empty strings for unset fields
export interface CompetitionFormValues {
  name: string
  date: string
  location: string
  disciplineId: string
  formatId: string
  ageCategory: string
  skillLevel: string
  weightCategory: string
  place: string
  resultNote: string
  notes: string
  // In the order they are saved
  fights: FightValues[]
}

// Fields whose server errors are shown under their inputs
const fieldKeys = [
  'name', 'date', 'location', 'disciplineId', 'formatId', 'ageCategory', 'skillLevel', 'weightCategory', 'place', 'resultNote', 'notes',
] as const
type FieldKey = (typeof fieldKeys)[number]
type FieldErrors = Partial<Record<FieldKey, string>>

function isFieldKey(key: string): key is FieldKey {
  return (fieldKeys as readonly string[]).includes(key)
}

const notesMaxLength = 4000

interface CompetitionFormProps {
  lookups: LookupsDto
  initialValues: CompetitionFormValues
  // Competition ID when editing
  competitionId?: number
  // Enables Delete competition button when editing
  onDelete?: () => void
}

export function CompetitionForm({ lookups, initialValues, competitionId, onDelete }: CompetitionFormProps) {
  const [values, setValues] = useState(initialValues)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  // Keeps errors attached to the correct fight when another is removed
  const [fightErrors, setFightErrors] = useState<Record<number, FightErrors>>({})
  const [saving, setSaving] = useState(false)
  // Tracks fights affected by the last discipline change until the next change
  const [fightsNotice, setFightsNotice] = useState<string | null>(null)
  const formRef = useRef<HTMLFormElement>(null)
  const nextFightKey = useRef(initialValues.fights.length)
  const notesId = useId()
  const navigate = useNavigate()
  const showToast = useToast()

  // After a failed save, focus the first invalid field or scroll to the general error message
  useEffect(() => {
    const field = formRef.current?.querySelector('.field-error')?.closest('.field')
    if (field) {
      field.querySelector<HTMLElement>('input, select, textarea, button')?.focus()
    } else {
      formRef.current?.querySelector('.competitions-error')?.scrollIntoView({ block: 'center' })
    }
  }, [fieldErrors, formError, fightErrors])

  function change(patch: Partial<CompetitionFormValues>) {
    setValues((current) => ({ ...current, ...patch }))
  }

  // Formats depend on the discipline, with a single format selected automatically
  // Unavailable techniques remain selected and trigger a notice (none when no discipline is chosen)
  function changeDiscipline(disciplineId: string) {
    const discipline = lookups.disciplines.find((item) => String(item.id) === disciplineId)
    const formats = discipline?.formats ?? []
    const affected = discipline ? notOfferedFights(values.fights, lookups, disciplineId) : []
    change({ disciplineId, formatId: formats.length === 1 ? String(formats[0].id) : '' })
    setFightsNotice(discipline && affected.length > 0 ? texts.competitions.fightsNotOffered(discipline.name, affected) : null)
  }

  function addFight() {
    const key = nextFightKey.current++
    const fight: FightValues = { key, result: '', method: '', techniqueId: '', duration: '', opponentName: '', notes: '' }
    setValues((current) => ({ ...current, fights: [...current.fights, fight] }))
    setFightsNotice(null)
    return key
  }

  function updateFight(key: number, patch: Partial<FightValues>) {
    setFightsNotice(null)
    setValues((current) => ({
      ...current,
      fights: current.fights.map((fight) => (fight.key === key ? { ...fight, ...patch } : fight)),
    }))
  }

  function removeFight(key: number) {
    setFightsNotice(null)
    setValues((current) => ({ ...current, fights: current.fights.filter((fight) => fight.key !== key) }))
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setFieldErrors({})
    setFormError(null)
    setFightErrors({})
    setSaving(true)
    const request = toRequest(values)
    try {
      if (competitionId) {
        await apiFetch<CompetitionDto>(`/competitions/${competitionId}`, { method: 'PUT', body: request })
      } else {
        await apiFetch<CompetitionDto>('/competitions', { method: 'POST', body: request })
      }
      showToast(texts.competitions.competitionSaved, 'success')
      navigate('/competitions')
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 400) {
        // Request fights follow form order, so each index maps to its fight key
        const errors = readErrors(caught.errors, request, values.fights.map((fight) => fight.key))
        setFieldErrors(errors.fields)
        setFormError(errors.form)
        setFightErrors(errors.fights)
      } else {
        showToast(errorMessage(caught), 'error')
      }
    } finally {
      setSaving(false)
    }
  }

  const choose: SelectOption = { value: '', label: texts.competitions.choose }
  const options = (items: { id: number; name: string }[]) => items.map((item) => ({ value: String(item.id), label: item.name }))
  const discipline = lookups.disciplines.find((item) => String(item.id) === values.disciplineId)
  // Shows errors for each invalid fight in the current order
  const fightErrorLines = values.fights.flatMap((fight, index) => {
    const errors = fightErrors[fight.key] ?? {}
    return [errors.fight, errors.result, errors.method, errors.techniqueId, errors.durationSeconds, errors.opponentName, errors.notes]
      .filter((message) => message !== undefined)
      .map((message) => texts.competitions.fightError(index + 1, message))
  })
  const errorText = [...fightErrorLines, formError].filter((line) => line !== null).join('\n')

  return (
    <form ref={formRef} className="competitions-form" onSubmit={handleSubmit}>
      <Field
        label={texts.competitions.name}
        value={values.name}
        onChange={(name) => change({ name })}
        error={fieldErrors.name}
        maxLength={150}
        required
      />

      <div className="competitions-pair">
        <Field
          label={texts.competitions.date}
          type="date"
          value={values.date}
          onChange={(date) => change({ date })}
          error={fieldErrors.date}
          required
        />
        <Field
          label={texts.competitions.location}
          value={values.location}
          onChange={(location) => change({ location })}
          error={fieldErrors.location}
          maxLength={150}
        />
        <Select
          label={texts.competitions.discipline}
          value={values.disciplineId}
          options={[choose, ...options(lookups.disciplines)]}
          onChange={changeDiscipline}
          error={fieldErrors.disciplineId}
          required
        />
        <Select
          label={texts.competitions.format}
          value={values.formatId}
          options={[choose, ...options(discipline?.formats ?? [])]}
          onChange={(formatId) => change({ formatId })}
          error={fieldErrors.formatId}
          required
        />
      </div>

      <div className="competitions-categories">
        <Field
          label={texts.competitions.ageCategory}
          value={values.ageCategory}
          onChange={(ageCategory) => change({ ageCategory })}
          error={fieldErrors.ageCategory}
          maxLength={50}
        />
        <Field
          label={texts.competitions.skillLevel}
          value={values.skillLevel}
          onChange={(skillLevel) => change({ skillLevel })}
          error={fieldErrors.skillLevel}
          maxLength={50}
        />
        <Field
          label={texts.competitions.weightCategory}
          value={values.weightCategory}
          onChange={(weightCategory) => change({ weightCategory })}
          error={fieldErrors.weightCategory}
          maxLength={50}
        />
      </div>

      <PlacePicker value={values.place} onChange={(place) => change({ place })} error={fieldErrors.place} />
      <Field
        label={texts.competitions.resultNote}
        value={values.resultNote}
        onChange={(resultNote) => change({ resultNote })}
        error={fieldErrors.resultNote}
        maxLength={100}
      />

      <FightsSection
        lookups={lookups}
        disciplineId={values.disciplineId}
        fights={values.fights}
        notice={fightsNotice}
        errors={fightErrors}
        onAdd={addFight}
        onUpdate={updateFight}
        onRemove={removeFight}
      />

      <div className="field">
        <label htmlFor={notesId}>{texts.competitions.notes}</label>
        <textarea
          id={notesId}
          className="competitions-notes"
          value={values.notes}
          onChange={(event) => change({ notes: event.target.value })}
          maxLength={notesMaxLength}
          rows={4}
          aria-invalid={fieldErrors.notes ? true : undefined}
          aria-describedby={fieldErrors.notes ? `${notesId}-error` : undefined}
        />
        {fieldErrors.notes && (
          <p id={`${notesId}-error`} className="field-error">
            {fieldErrors.notes}
          </p>
        )}
      </div>

      {errorText && (
        <p className="competitions-error" role="alert">
          {errorText}
        </p>
      )}
      <div className="competitions-actions">
        <div className="competitions-bar">
          <Button type="submit" loading={saving}>
            {texts.competitions.saveCompetition}
          </Button>
        </div>
        {onDelete && (
          <Button variant="secondary" onClick={onDelete}>
            {texts.competitions.deleteCompetition}
          </Button>
        )}
      </div>
    </form>
  )
}

function toRequest(values: CompetitionFormValues): CompetitionRequest {
  const id = (text: string) => (text ? Number(text) : null)
  const optional = (text: string) => text.trim() || null
  return {
    name: values.name.trim(),
    date: values.date || null,
    location: optional(values.location),
    disciplineId: id(values.disciplineId),
    formatId: id(values.formatId),
    ageCategory: optional(values.ageCategory),
    skillLevel: optional(values.skillLevel),
    weightCategory: optional(values.weightCategory),
    // Sends 0 for invalid input to trigger server validation
    place: values.place === '' ? null : Math.trunc(Number(values.place)) || 0,
    resultNote: optional(values.resultNote),
    notes: optional(values.notes),
    fights: values.fights.map(toFightRequest),
  }
}

// Maps server validation errors to form messages and shows general errors above Save
// Shows fight errors next to their fights and above Save
function readErrors(serverErrors: Record<string, string[]>, request: CompetitionRequest, fightKeys: number[]) {
  const fields: FieldErrors = {}
  const form = new Set<string>()
  const fights: Record<number, FightErrors> = {}
  for (const [key, messages] of Object.entries(serverErrors)) {
    const fight = /^fights\[(\d+)\](?:\.(\w+))?$/.exec(key)
    const fightKey = fight ? fightKeys[Number(fight[1])] : undefined
    if (fight && fightKey !== undefined) {
      fights[fightKey] = { ...fights[fightKey], ...fightError(fight[2], messages[0]) }
    } else if (key === 'fights') {
      form.add(texts.competitions.fightsLimit(maxFights))
    } else if (key === 'name' && request.name === '') {
      fields.name = texts.competitions.nameRequired
    } else if (key === 'date' && request.date === null) {
      fields.date = texts.competitions.dateRequired
    } else if (key === 'disciplineId' && request.disciplineId === null) {
      fields.disciplineId = texts.competitions.disciplineRequired
    } else if (key === 'formatId' && request.formatId === null) {
      fields.formatId = texts.competitions.formatRequired
    } else if (key === 'place') {
      fields.place = texts.competitions.placeInvalid
    } else if (isFieldKey(key)) {
      fields[key] = messages[0]
    } else {
      form.add(texts.errors.unexpected)
    }
  }
  return { fields, form: form.size > 0 ? [...form].join('\n') : null, fights }
}

// Custom messages for result and duration, server messages for other fields
function fightError(field: string | undefined, message: string): FightErrors {
  switch (field) {
    case 'result':
      return { result: texts.competitions.resultRequired }
    case 'durationSeconds':
      return { durationSeconds: texts.competitions.durationInvalid }
    case 'method':
      return { method: message }
    case 'techniqueId':
      return { techniqueId: message }
    case 'opponentName':
      return { opponentName: message }
    case 'notes':
      return { notes: message }
    default:
      return { fight: message }
  }
}
