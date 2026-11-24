// Hand-written copies of the backend DTOs

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
}

export interface ProfileDto {
  id: string
  email: string
  firstName: string | null
  lastName: string | null
  clubs: ClubDto[]
  belts: ProfileBeltDto[]
}

export interface ClubDto {
  id: number
  name: string
}

export interface ProfileBeltDto {
  disciplineId: number
  beltId: number
  degree: number
}

export interface UpdateProfileRequest {
  firstName: string
  lastName: string | null
}

export interface ClubRequest {
  name: string
}

export interface BeltRequest {
  disciplineId: number
  beltId: number
  degree: number
}

export interface UpdateBeltsRequest {
  belts: BeltRequest[]
}

export type SessionKind = 'Grappling' | 'Other'

export type RoundResult = 'Win' | 'Loss' | 'Draw'

export interface RoundDto {
  result: RoundResult
  techniqueId: number | null
  positionId: number | null
  notes: string | null
}

export interface SessionDto {
  id: number
  kind: SessionKind
  date: string
  durationMinutes: number
  feeling: number
  notes: string | null
  disciplineId: number | null
  formatId: number | null
  characterId: number | null
  clubId: number | null
  activityTypeId: number | null
  rounds: RoundDto[]
}

export interface SessionSummaryDto {
  id: number
  kind: SessionKind
  date: string
  durationMinutes: number
  feeling: number
  disciplineId: number | null
  formatId: number | null
  characterId: number | null
  clubId: number | null
  activityTypeId: number | null
  roundsCount: number
  wins: number
  losses: number
  draws: number
}

export interface RoundRequest {
  result: RoundResult
  techniqueId: number | null
  positionId: number | null
  notes: string | null
}

export interface SessionRequest {
  kind: SessionKind
  date: string | null
  durationMinutes: number
  feeling: number
  notes: string | null
  disciplineId: number | null
  formatId: number | null
  characterId: number | null
  clubId: number | null
  activityTypeId: number | null
  rounds: RoundRequest[]
}

export type FightResult = 'Win' | 'Loss' | 'Draw'

export type FightMethod = 'Submission' | 'Throw' | 'Pin' | 'Points' | 'Decision' | 'Disqualification' | 'Walkover'

export interface FightDto {
  opponentName: string | null
  result: FightResult
  method: FightMethod | null
  techniqueId: number | null
  durationSeconds: number | null
  notes: string | null
}

export interface CompetitionDto {
  id: number
  name: string
  date: string
  location: string | null
  disciplineId: number
  formatId: number
  ageCategory: string | null
  skillLevel: string | null
  weightCategory: string | null
  place: number | null
  resultNote: string | null
  notes: string | null
  fights: FightDto[]
}

export interface CompetitionSummaryDto {
  id: number
  name: string
  date: string
  location: string | null
  disciplineId: number
  formatId: number
  ageCategory: string | null
  skillLevel: string | null
  weightCategory: string | null
  place: number | null
  resultNote: string | null
  fightsCount: number
  wins: number
  losses: number
  draws: number
}

export interface FightRequest {
  opponentName: string | null
  result: FightResult | null
  method: FightMethod | null
  techniqueId: number | null
  durationSeconds: number | null
  notes: string | null
}

export interface CompetitionRequest {
  name: string
  date: string | null
  location: string | null
  disciplineId: number | null
  formatId: number | null
  ageCategory: string | null
  skillLevel: string | null
  weightCategory: string | null
  place: number | null
  resultNote: string | null
  notes: string | null
  fights: FightRequest[]
}

export interface LookupsDto {
  disciplines: DisciplineDto[]
  characters: TrainingCharacterDto[]
  activityTypes: LookupItemDto[]
  techniqueCategories: TechniqueCategoryDto[]
  techniques: TechniqueDto[]
  positions: PositionDto[]
  materialCategories: LookupItemDto[]
}

export interface LookupItemDto {
  id: number
  code: string
  name: string
}

export interface DisciplineDto {
  id: number
  code: string
  name: string
  formats: LookupItemDto[]
  belts: BeltDto[]
}

export interface BeltDto {
  id: number
  code: string
  name: string
  rank: number
  minDegree: number
  maxDegree: number
}

export interface TrainingCharacterDto {
  id: number
  code: string
  name: string
  allowsRounds: boolean
}

export interface TechniqueCategoryDto {
  id: number
  code: string
  name: string
  disciplineIds: number[]
}

export interface TechniqueDto {
  id: number
  code: string
  name: string
  categoryId: number
}

export interface PositionDto {
  id: number
  code: string
  name: string
  disciplineIds: number[]
}
