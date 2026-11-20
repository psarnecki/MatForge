import { useEffect, useState } from 'react'
import { apiFetch, errorMessage } from '../../api/apiFetch'
import type { SessionSummaryDto } from '../../types'

// Results are stored with their range to avoid showing outdated data for months the user left
interface RangeResult {
  range: string
  sessions: SessionSummaryDto[] | null
  error: string | null
}

export function useSessions(from: string, to: string) {
  const range = `${from}/${to}`
  const [result, setResult] = useState<RangeResult | null>(null)

  useEffect(() => {
    let ignore = false
    apiFetch<SessionSummaryDto[]>(`/sessions?from=${from}&to=${to}`)
      .then((sessions) => {
        if (!ignore) {
          setResult({ range: `${from}/${to}`, sessions, error: null })
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setResult({ range: `${from}/${to}`, sessions: null, error: errorMessage(caught) })
        }
      })
    return () => {
      ignore = true
    }
  }, [from, to])

  const current = result?.range === range ? result : null
  return { sessions: current?.sessions ?? null, error: current?.error ?? null }
}
