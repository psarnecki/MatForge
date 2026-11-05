import { useEffect, useState } from 'react'
import type { LookupsDto } from '../types'
import { apiFetch, errorMessage } from './apiFetch'

// Loaded once and shared by every screen (a failed load is forgotten, so the next screen tries again)
let cachedLookups: Promise<LookupsDto> | null = null

function loadLookups() {
  if (!cachedLookups) {
    cachedLookups = apiFetch<LookupsDto>('/lookups')
    cachedLookups.catch(() => {
      cachedLookups = null
    })
  }
  return cachedLookups
}

export function useLookups() {
  const [lookups, setLookups] = useState<LookupsDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let ignore = false
    loadLookups()
      .then((loaded) => {
        if (!ignore) {
          setLookups(loaded)
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setError(errorMessage(caught))
        }
      })
    return () => {
      ignore = true
    }
  }, [])

  return { lookups, error }
}
