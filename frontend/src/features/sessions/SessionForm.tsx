import { useEffect, useId, useRef, useState, type SubmitEvent } from 'react'
import { useNavigate } from 'react-router'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { Select, type SelectOption } from '../../components/Select'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { ClubDto, LookupsDto, SessionDto, SessionKind, SessionRequest } from '../../types'
import { saveLastSession } from './lastSession'
import { toRoundRequest, type RoundValues } from './rounds'
import { SegmentedControl } from './SegmentedControl'

// Form values (also ids and numbers) as text, with empty strings for unselected fields
export interface SessionFormValues {
  kind: SessionKind
  date: string
  duration: string
  feeling: string
  disciplineId: string
  formatId: string
  characterId: string
  clubId: string
  activityTypeId: string
  notes: string
  // In the order they are saved
  rounds: RoundValues[]
}

// Request fields whose server errors are shown under their input
const fieldKeys = ['date', 'durationMinutes', 'feeling', 'notes', 'disciplineId', 'formatId', 'characterId', 'clubId', 'activityTypeId'] as const
type FieldKey = (typeof fieldKeys)[number]
type FieldErrors = Partial<Record<FieldKey, string>>

function isFieldKey(key: string): key is FieldKey {
  return (fieldKeys as readonly string[]).includes(key)
}

const notesMaxLength = 4000
const quickDurations = ['60', '90', '120']
const feelings = ['1', '2', '3', '4', '5'].map((value) => ({ value, label: value }))

interface SessionFormProps {
  lookups: LookupsDto
  clubs: ClubDto[]
  initialValues: SessionFormValues
  // Set when editing
  sessionId?: number
  // Provided when editing to show the Delete session button
  onDelete?: () => void
}

export function SessionForm({ lookups, clubs, initialValues, sessionId, onDelete }: SessionFormProps) {
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
      formRef.current?.querySelector('.sessions-error')?.scrollIntoView({ block: 'center' })
    }
  }, [fieldErrors, formError])

  function change(patch: Partial<SessionFormValues>) {
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
      const saved = sessionId
        ? await apiFetch<SessionDto>(`/sessions/${sessionId}`, { method: 'PUT', body: request })
        : await apiFetch<SessionDto>('/sessions', { method: 'POST', body: request })
      // Only new sessions update defaults, editing does not
      if (!sessionId && request.kind === 'Grappling') {
        saveLastSession({
          disciplineId: request.disciplineId!,
          formatId: request.formatId!,
          characterId: request.characterId!,
          clubId: request.clubId,
        })
      }
      showToast(texts.sessions.sessionSaved, 'success')
      navigate(`/training?day=${saved.date}`)
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 400) {
        const roundCharacters = lookups.characters.filter((item) => item.allowsRounds).map((item) => item.name)
        const errors = readErrors(caught.errors, request, texts.sessions.roundsKept(values.rounds.length,roundCharacters.join(', ')))
        setFieldErrors(errors.fields)
        setFormError(errors.form)
      } else {
        showToast(errorMessage(caught), 'error')
      }
    } finally {
      setSaving(false)
    }
  }

  const choose: SelectOption = { value: '', label: texts.sessions.choose }
  const options = (items: { id: number; name: string }[]) => items.map((item) => ({ value: String(item.id), label: item.name }))
  const discipline = lookups.disciplines.find((item) => String(item.id) === values.disciplineId)

  return (
    <form ref={formRef} className="sessions-form" onSubmit={handleSubmit}>
      <SegmentedControl<SessionKind>
        label={texts.sessions.kind}
        hideLabel
        options={[
          { value: 'Grappling', label: texts.sessions.kindGrappling },
          { value: 'Other', label: texts.sessions.kindOther },
        ]}
        value={values.kind}
        onChange={(kind) => change({ kind })}
      />

      <div className="sessions-pair">
        <Field
          label={texts.sessions.date}
          type="date"
          value={values.date}
          onChange={(date) => change({ date })}
          error={fieldErrors.date}
          required
        />
        <div>
          <Field
            label={texts.sessions.duration}
            type="number"
            value={values.duration}
            onChange={(duration) => change({ duration })}
            error={fieldErrors.durationMinutes}
            required
          />
          <div role="group" className="sessions-segments sessions-quick" aria-label={texts.sessions.quickDuration}>
            {quickDurations.map((minutes) => (
              <button key={minutes} type="button" aria-pressed={values.duration === minutes} onClick={() => change({ duration: minutes })}>
                {minutes}
              </button>
            ))}
          </div>
        </div>
      </div>

      <SegmentedControl
        label={texts.sessions.feeling}
        options={feelings}
        value={values.feeling}
        onChange={(feeling) => change({ feeling })}
        error={fieldErrors.feeling}
      />

      {values.kind === 'Grappling' ? (
        <div className="sessions-pair">
          <Select
            label={texts.sessions.discipline}
            value={values.disciplineId}
            options={[choose, ...options(lookups.disciplines)]}
            onChange={changeDiscipline}
            error={fieldErrors.disciplineId}
            required
          />
          <Select
            label={texts.sessions.format}
            value={values.formatId}
            options={[choose, ...options(discipline?.formats ?? [])]}
            onChange={(formatId) => change({ formatId })}
            error={fieldErrors.formatId}
            required
          />
          <Select
            label={texts.sessions.character}
            value={values.characterId}
            options={[choose, ...options(lookups.characters)]}
            onChange={(characterId) => change({ characterId })}
            error={fieldErrors.characterId}
            required
          />
          <Select
            label={texts.sessions.club}
            value={values.clubId}
            options={[...options(clubs), { value: '', label: texts.sessions.otherClub }]}
            onChange={(clubId) => change({ clubId })}
            error={fieldErrors.clubId}
          />
        </div>
      ) : (
        <Select
          label={texts.sessions.activityType}
          value={values.activityTypeId}
          options={[choose, ...options(lookups.activityTypes)]}
          onChange={(activityTypeId) => change({ activityTypeId })}
          error={fieldErrors.activityTypeId}
          required
        />
      )}

      {/* Inline layout, as notes are the only multi-line field */}
      <div className="field">
        <label htmlFor={notesId}>{texts.sessions.notes}</label>
        <textarea
          id={notesId}
          className="sessions-notes"
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
        <p className="sessions-error" role="alert">
          {formError}
        </p>
      )}
      <div className="sessions-actions">
        <div className="sessions-bar">
          <Button type="submit" loading={saving}>
            {texts.sessions.saveSession}
          </Button>
        </div>
        {onDelete && (
          <Button variant="secondary" onClick={onDelete}>
            {texts.sessions.deleteSession}
          </Button>
        )}
      </div>
    </form>
  )
}

// Switching kinds preserves the form values, but inactive fields are sent as null
// Rounds are always sent so the server can validate them against the selected kind and character
function toRequest(values: SessionFormValues): SessionRequest {
  const grappling = values.kind === 'Grappling'
  const id = (text: string) => (text ? Number(text) : null)
  return {
    kind: values.kind,
    date: values.date || null,
    // Send 0 for empty or invalid input so the server can return a field error
    durationMinutes: Math.trunc(Number(values.duration)) || 0,
    feeling: Number(values.feeling) || 0,
    notes: values.notes.trim() || null,
    disciplineId: grappling ? id(values.disciplineId) : null,
    formatId: grappling ? id(values.formatId) : null,
    characterId: grappling ? id(values.characterId) : null,
    clubId: grappling ? id(values.clubId) : null,
    activityTypeId: grappling ? null : id(values.activityTypeId),
    rounds: values.rounds.map(toRoundRequest),
  }
}

// Maps server validation errors to form messages
// Round and unknown field errors are shown above Save
function readErrors(serverErrors: Record<string, string[]>, request: SessionRequest, roundsHint: string) {
  const fields: FieldErrors = {}
  const form = new Set<string>()
  for (const [key, messages] of Object.entries(serverErrors)) {
    const round = /^rounds\[(\d+)\]/.exec(key)
    if (key === 'durationMinutes') {
      fields.durationMinutes = texts.sessions.durationInvalid
    } else if (key === 'feeling') {
      fields.feeling = texts.sessions.feelingRequired
    } else if (key === 'date' && request.date === null) {
      fields.date = texts.sessions.dateRequired
    } else if (isFieldKey(key)) {
      fields[key] = messages[0]
    } else if (key === 'rounds') {
      form.add(`${messages[0]} ${roundsHint}`)
    } else if (round) {
      form.add(texts.sessions.roundError(Number(round[1]) + 1, messages[0]))
    } else {
      form.add(texts.errors.unexpected)
    }
  }
  return { fields, form: form.size > 0 ? [...form].join('\n') : null }
}
