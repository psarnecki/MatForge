import { useEffect, useId, useRef, useState, type SubmitEvent } from 'react'
import { useNavigate } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { Select, type SelectOption } from '../../components/Select'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { CompetitionDto, CompetitionRequest, FightRequest, LookupsDto } from '../../types'
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
  // Kept and sent back unchanged
  fights: FightRequest[]
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
  const [saving, setSaving] = useState(false)
  const formRef = useRef<HTMLFormElement>(null)
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
  }, [fieldErrors, formError])

  function change(patch: Partial<CompetitionFormValues>) {
    setValues((current) => ({ ...current, ...patch }))
  }

  // Formats depend on the discipline, with a single format selected automatically
  function changeDiscipline(disciplineId: string) {
    const formats = lookups.disciplines.find((item) => String(item.id) === disciplineId)?.formats ?? []
    change({ disciplineId, formatId: formats.length === 1 ? String(formats[0].id) : '' })
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setFieldErrors({})
    setFormError(null)
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
        const errors = readErrors(caught.errors, request)
        setFieldErrors(errors.fields)
        setFormError(errors.form)
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

      {formError && (
        <p className="competitions-error" role="alert">
          {formError}
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
    fights: values.fights,
  }
}

// Maps server validation errors to form messages and shows general errors above Save
function readErrors(serverErrors: Record<string, string[]>, request: CompetitionRequest) {
  const fields: FieldErrors = {}
  const form = new Set<string>()
  for (const [key, messages] of Object.entries(serverErrors)) {
    if (key === 'name' && request.name === '') {
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
  return { fields, form: form.size > 0 ? [...form].join('\n') : null }
}
