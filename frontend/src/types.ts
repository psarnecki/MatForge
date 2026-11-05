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
  firstName: string | null
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
