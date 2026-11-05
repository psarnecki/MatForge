import type { ReactNode } from 'react'

interface ButtonProps {
  children: ReactNode
  type?: 'button' | 'submit'
  variant?: 'primary' | 'secondary'
  loading?: boolean
  disabled?: boolean
  onClick?: () => void
}

export function Button({ children, type = 'button', variant = 'primary', loading = false, disabled = false, onClick }: ButtonProps) {
  return (
    <button
      type={type}
      className={`button button-${variant}`}
      disabled={disabled || loading}
      aria-busy={loading}
      onClick={onClick}
    >
      {children}
    </button>
  )
}
