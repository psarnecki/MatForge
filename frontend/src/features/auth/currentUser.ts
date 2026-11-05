import { createContext, useContext } from 'react'
import type { ProfileDto } from '../../types'

interface CurrentUserValue {
  user: ProfileDto
  // Allows the top bar to display the saved name immediately on the profile page
  setUser: (user: ProfileDto) => void
  logout: () => Promise<void>
}

export const CurrentUserContext = createContext<CurrentUserValue | null>(null)

export function useCurrentUser() {
  const context = useContext(CurrentUserContext)
  if (!context) {
    throw new Error('useCurrentUser must be used inside RequireAuth')
  }
  return context
}

// The email stands in when registration did not save the names
export function displayName(user: ProfileDto) {
  if (!user.firstName) {
    return user.email
  }
  return user.lastName ? `${user.firstName} ${user.lastName}` : user.firstName
}
