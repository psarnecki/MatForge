import type { SelectGroup, SelectOption } from '../../components/Select'
import type { LookupsDto, RoundDto, RoundRequest, RoundResult } from '../../types'

// Editable round with ids and notes treated as text values
export interface RoundValues {
  // Stable key to preserve round identity when removing items
  key: number
  result: RoundResult
  techniqueId: string
  positionId: string
  notes: string
}

export function toRoundValues(round: RoundDto, key: number): RoundValues {
  return {
    key,
    result: round.result,
    techniqueId: round.techniqueId === null ? '' : String(round.techniqueId),
    positionId: round.positionId === null ? '' : String(round.positionId),
    notes: round.notes ?? '',
  }
}

export function toRoundRequest(round: RoundValues): RoundRequest {
  return {
    result: round.result,
    techniqueId: round.techniqueId ? Number(round.techniqueId) : null,
    positionId: round.positionId ? Number(round.positionId) : null,
    notes: round.notes.trim() || null,
  }
}

// Groups techniques by category for the selected discipline
export function techniqueGroups(lookups: LookupsDto, disciplineId: string): SelectGroup[] {
  return lookups.techniqueCategories
    .filter((category) => category.disciplineIds.includes(Number(disciplineId)))
    .map((category) => ({
      label: category.name,
      options: lookups.techniques
        .filter((technique) => technique.categoryId === category.id)
        .map((technique) => ({ value: String(technique.id), label: technique.name })),
    }))
    .filter((group) => group.options.length > 0)
}

// Returns positions available in the selected discipline
export function positionOptions(lookups: LookupsDto, disciplineId: string): SelectOption[] {
  return lookups.positions
    .filter((position) => position.disciplineIds.includes(Number(disciplineId)))
    .map((position) => ({ value: String(position.id), label: position.name }))
}
