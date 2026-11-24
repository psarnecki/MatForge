import type { SelectOption } from '../../components/Select'
import { techniqueGroups } from '../../lib/techniques'
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

// Server errors for a round's fields and other round-related issues
export interface RoundErrors {
  techniqueId?: string
  positionId?: string
  round?: string
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

// Returns positions available in the selected discipline
export function positionOptions(lookups: LookupsDto, disciplineId: string): SelectOption[] {
  return lookups.positions
    .filter((position) => position.disciplineIds.includes(Number(disciplineId)))
    .map((position) => ({ value: String(position.id), label: position.name }))
}

// Clears unavailable techniques and positions and returns changed round numbers
export function clearNotOffered(rounds: RoundValues[], lookups: LookupsDto, disciplineId: string) {
  const techniques = new Set(techniqueGroups(lookups, disciplineId).flatMap((group) => group.options.map((option) => option.value)))
  const positions = new Set(positionOptions(lookups, disciplineId).map((option) => option.value))
  const cleared: number[] = []
  const kept = rounds.map((round, index) => {
    const keepTechnique = !round.techniqueId || techniques.has(round.techniqueId)
    // Also clears the position if its technique is removed
    const keepPosition = !round.positionId || (keepTechnique && positions.has(round.positionId))
    if (keepTechnique && keepPosition) {
      return round
    }
    cleared.push(index + 1)
    return { ...round, techniqueId: keepTechnique ? round.techniqueId : '', positionId: keepPosition ? round.positionId : '' }
  })
  return { rounds: kept, cleared }
}
