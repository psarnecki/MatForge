import { useState } from 'react'
import { ApiError, apiFetch, errorMessage } from '../../api/apiFetch'
import { useLookups } from '../../api/useLookups'
import { Button } from '../../components/Button'
import { Select } from '../../components/Select'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { BeltRequest, ProfileBeltDto, UpdateBeltsRequest } from '../../types'
import { useCurrentUser } from '../auth/currentUser'

// The chosen belt and degree per discipline id (no entry means no belt)
type Choices = Record<number, BeltRequest>

interface BeltErrors {
  beltId?: string
  degree?: string
}

function toChoices(belts: ProfileBeltDto[]): Choices {
  return Object.fromEntries(belts.map((belt) => [belt.disciplineId, belt]))
}

function range(min: number, max: number) {
  return Array.from({ length: max - min + 1 }, (_, index) => min + index)
}

export function BeltsSection() {
  const { user, setUser } = useCurrentUser()
  const { lookups, error: lookupsError } = useLookups()
  const [choices, setChoices] = useState<Choices>(() => toChoices(user.belts))
  // Keyed by discipline id, built from the server's belts[i].beltId / belts[i].degree keys
  const [errors, setErrors] = useState<Record<number, BeltErrors>>({})
  const [saving, setSaving] = useState(false)
  const showToast = useToast()

  // Wrestling and sambo have no belts
  const disciplines = lookups?.disciplines.filter((discipline) => discipline.belts.length > 0) ?? []

  function chooseBelt(disciplineId: number, beltIdText: string) {
    const next = { ...choices }
    const belt = disciplines.find((d) => d.id === disciplineId)?.belts.find((b) => b.id === Number(beltIdText))
    if (belt) {
      next[disciplineId] = { disciplineId, beltId: belt.id, degree: belt.minDegree }
    } else {
      delete next[disciplineId]
    }
    setChoices(next)
  }

  function chooseDegree(disciplineId: number, degreeText: string) {
    setChoices({ ...choices, [disciplineId]: { ...choices[disciplineId], degree: Number(degreeText) } })
  }

  async function handleSave() {
    setErrors({})
    setSaving(true)
    const request: UpdateBeltsRequest = { belts: Object.values(choices) }
    try {
      const saved = await apiFetch<ProfileBeltDto[]>('/me/belts', { method: 'PUT', body: request })
      setUser({ ...user, belts: saved })
      setChoices(toChoices(saved))
      showToast(texts.profile.beltsSaved, 'success')
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 400) {
        setErrors(errorsByDiscipline(request.belts, caught.errors))
      } else {
        showToast(errorMessage(caught), 'error')
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="profile-section" aria-labelledby="profile-belts-title">
      <h2 id="profile-belts-title">{texts.profile.beltsTitle}</h2>
      {lookupsError && (
        <p className="profile-error" role="alert">
          {lookupsError}
        </p>
      )}
      {lookups && (
        <div className="profile-form">
          {disciplines.map((discipline) => {
            const choice = choices[discipline.id]
            const belt = discipline.belts.find((b) => b.id === choice?.beltId)
            return (
              <div key={discipline.id} className="profile-belt">
                <Select
                  label={discipline.name}
                  value={choice ? String(choice.beltId) : ''}
                  options={[
                    { value: '', label: texts.profile.noBelt },
                    ...discipline.belts.map((b) => ({ value: String(b.id), label: b.name })),
                  ]}
                  onChange={(value) => chooseBelt(discipline.id, value)}
                  error={errors[discipline.id]?.beltId}
                />
                {/* Hidden when the belt allows only one value (judo kyu belts) */}
                {belt && belt.maxDegree > belt.minDegree && (
                  <Select
                    label={texts.profile.degree}
                    value={String(choice.degree)}
                    options={range(belt.minDegree, belt.maxDegree).map((d) => ({ value: String(d), label: String(d) }))}
                    onChange={(value) => chooseDegree(discipline.id, value)}
                    error={errors[discipline.id]?.degree}
                  />
                )}
              </div>
            )
          })}
          <Button loading={saving} onClick={handleSave}>
            {texts.profile.saveBelts}
          </Button>
        </div>
      )}
    </section>
  )
}

// Maps "belts[1].degree" back to the discipline of the second sent belt
function errorsByDiscipline(sent: BeltRequest[], serverErrors: Record<string, string[]>) {
  const result: Record<number, BeltErrors> = {}
  sent.forEach((belt, index) => {
    const beltId = serverErrors[`belts[${index}].beltId`] ?? serverErrors[`belts[${index}].disciplineId`]
    const degree = serverErrors[`belts[${index}].degree`]
    if (beltId || degree) {
      result[belt.disciplineId] = { beltId: beltId?.join('\n'), degree: degree?.join('\n') }
    }
  })
  return result
}
