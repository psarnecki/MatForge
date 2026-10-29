import { texts } from '../texts'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  // 0 means the server could not be reached
  status: number
  detail: string | null
  errors: Record<string, string[]>

  constructor(status: number, problem: ProblemDetails | undefined) {
    super(problem?.title ?? `HTTP ${status}`)
    this.status = status
    this.detail = problem?.detail ?? null
    this.errors = problem?.errors ?? {}
  }
}

interface ApiFetchOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  // Login answers a wrong password with 401 too, which must show an error instead of redirecting
  redirectOn401?: boolean
}

export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      method: options.method ?? 'GET',
      credentials: 'include',
      headers: options.body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    })
  } catch {
    throw new ApiError(0, undefined)
  }

  const isJson = response.headers.get('Content-Type')?.includes('json') ?? false
  const data: unknown = isJson ? await response.json() : undefined

  if (response.ok) {
    return data as T
  }

  if (response.status === 401 && options.redirectOn401 !== false && window.location.pathname !== '/login') {
    window.location.assign('/login')
  }
  throw new ApiError(response.status, data as ProblemDetails | undefined)
}

// Error message for toasts and forms, with field errors handled separately
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 0) {
      return texts.errors.network
    }
    if (error.status === 409 && error.detail) {
      return error.detail
    }
  }
  return texts.errors.unexpected
}
