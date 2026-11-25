import { techniqueGroups } from '../../lib/techniques'
import type { FightDto, FightMethod, FightRequest, FightResult, LookupsDto } from '../../types'

// Editable fight with ids and other values stored as text
export interface FightValues {
  // Stable key to preserve fight identity when removing items
  key: number
  // Empty for a new fight until a result is chosen
  result: FightResult | ''
  method: FightMethod | ''
  techniqueId: string
  // m:ss
  duration: string
  opponentName: string
  notes: string
}

// Server errors for fight fields and related issues
export interface FightErrors {
  result?: string
  method?: string
  techniqueId?: string
  durationSeconds?: string
  opponentName?: string
  notes?: string
  fight?: string
}

export const maxFights = 30
export const fightResults: FightResult[] = ['Win', 'Loss', 'Draw']
export const fightMethods: FightMethod[] = ['Submission', 'Throw', 'Pin', 'Points', 'Decision', 'Disqualification', 'Walkover']

// Only these methods allow a deciding technique
export function allowsTechnique(method: FightMethod | ''): boolean {
  return method === 'Submission' || method === 'Throw' || method === 'Pin'
}

export function formatDuration(seconds: number): string {
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`
}

// Returns null for empty input and 0 for invalid format to trigger server validation
export function parseDuration(text: string): number | null {
  const trimmed = text.trim()
  if (!trimmed) {
    return null
  }
  const match = /^(\d{1,2}):([0-5]\d)$/.exec(trimmed)
  return match ? Number(match[1]) * 60 + Number(match[2]) : 0
}

// Accepts empty input or durations from 0:01 to 60:00 (m:ss)
export function isDurationValid(text: string): boolean {
  const seconds = parseDuration(text)
  return seconds === null || (seconds >= 1 && seconds <= 3600)
}

export function toFightValues(fight: FightDto, key: number): FightValues {
  return {
    key,
    result: fight.result,
    method: fight.method ?? '',
    techniqueId: fight.techniqueId === null ? '' : String(fight.techniqueId),
    duration: fight.durationSeconds === null ? '' : formatDuration(fight.durationSeconds),
    opponentName: fight.opponentName ?? '',
    notes: fight.notes ?? '',
  }
}

export function toFightRequest(fight: FightValues): FightRequest {
  return {
    opponentName: fight.opponentName.trim() || null,
    result: fight.result || null,
    method: fight.method || null,
    techniqueId: fight.techniqueId ? Number(fight.techniqueId) : null,
    durationSeconds: parseDuration(fight.duration),
    notes: fight.notes.trim() || null,
  }
}

// Returns fight numbers with techniques unavailable in the selected discipline
export function notOfferedFights(fights: FightValues[], lookups: LookupsDto, disciplineId: string): number[] {
  const techniques = new Set(techniqueGroups(lookups, disciplineId).flatMap((group) => group.options.map((option) => option.value)))
  return fights.flatMap((fight, index) => (fight.techniqueId && !techniques.has(fight.techniqueId) ? [index + 1] : []))
}
