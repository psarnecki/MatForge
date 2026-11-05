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
