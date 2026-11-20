// Last session fields used as defaults for the next session
export interface LastSession {
  disciplineId: number
  formatId: number
  characterId: number
  clubId: number | null
}

const storageKey = 'matforge.lastSession'

// Missing or unavailable storage simply means no defaults are available
export function readLastSession(): Partial<LastSession> {
  try {
    const stored: unknown = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    return typeof stored === 'object' && stored !== null ? stored : {}
  } catch {
    return {}
  }
}

export function saveLastSession(lastSession: LastSession) {
  try {
    localStorage.setItem(storageKey, JSON.stringify(lastSession))
  } catch {
    // Session is already saved, defaults are optional
  }
}
