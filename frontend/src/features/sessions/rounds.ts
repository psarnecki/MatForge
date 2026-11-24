import type { RoundDto, RoundRequest, RoundResult } from '../../types'

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
