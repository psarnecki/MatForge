import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { ToastContext, type ToastKind } from './useToast'

interface Toast {
  id: number
  message: string
  kind: ToastKind
}

const visibleMs = 4000

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toast, setToast] = useState<Toast | null>(null)

  const showToast = useCallback((message: string, kind: ToastKind) => {
    setToast({ id: Date.now(), message, kind })
  }, [])

  useEffect(() => {
    if (!toast) {
      return
    }
    const timer = setTimeout(() => setToast(null), visibleMs)
    return () => clearTimeout(timer)
  }, [toast])

  const value = useMemo(() => ({ showToast }), [showToast])

  return (
    <ToastContext.Provider value={value}>
      {children}
      {/* The live region stays mounted so screen readers announce new messages */}
      <div className="toast-region" role="status" aria-live="polite">
        {toast && (
          <p key={toast.id} className={`toast toast-${toast.kind}`}>
            {toast.message}
          </p>
        )}
      </div>
    </ToastContext.Provider>
  )
}
