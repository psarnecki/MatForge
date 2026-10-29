import { useId } from 'react'

interface FieldProps {
  label: string
  value: string
  onChange: (value: string) => void
  type?: 'text' | 'email' | 'password' | 'number' | 'date'
  error?: string
  autoComplete?: string
  required?: boolean
  maxLength?: number
}

export function Field({ label, value, onChange, type = 'text', error, autoComplete, required, maxLength }: FieldProps) {
  const id = useId()
  const errorId = `${id}-error`

  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        autoComplete={autoComplete}
        required={required}
        maxLength={maxLength}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
      />
      {error && (
        <p id={errorId} className="field-error">
          {error}
        </p>
      )}
    </div>
  )
}
