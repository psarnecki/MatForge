import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { texts } from '../texts'
import { ToastContext, type ToastKind } from './useToast'

interface Toast {
  id: number
  message: string
  kind: ToastKind
}

// Errors stay longer because the user has to read what to fix
const visibleMs: Record<ToastKind, number> = { success: 4000, error: 8000 }

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toast, setToast] = useState<Toast | null>(null)

  const showToast = useCallback((message: string, kind: ToastKind) => {
    setToast({ id: Date.now(), message, kind })
  }, [])

  useEffect(() => {
    if (!toast) {
      return
    }
    const timer = setTimeout(() => setToast(null), visibleMs[toast.kind])
    return () => clearTimeout(timer)
  }, [toast])

  const value = useMemo(() => ({ showToast }), [showToast])

  return (
    <ToastContext.Provider value={value}>
      {children}
      {/* The live region stays mounted so screen readers announce new messages */}
      <div className="toast-region" role="status" aria-live="polite">
        {toast && (
          <div key={toast.id} className={`toast toast-${toast.kind}`}>
            <p>{toast.message}</p>
            <button type="button" className="icon-button" aria-label={texts.common.close} onClick={() => setToast(null)}>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <path d="M6 6l12 12M18 6L6 18" />
              </svg>
            </button>
          </div>
        )}
      </div>
    </ToastContext.Provider>
  )
}
