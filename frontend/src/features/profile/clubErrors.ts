import { ApiError } from '../../api/apiFetch'
import { texts } from '../../texts'

// Message for the club name field, or null when the error belongs in a toast
export function clubNameError(error: unknown): string | null {
  if (error instanceof ApiError && error.status === 409 && error.detail) {
    return error.detail
  }
  if (error instanceof ApiError && error.status === 400) {
    return texts.profile.clubNameRequired
  }
  return null
}
