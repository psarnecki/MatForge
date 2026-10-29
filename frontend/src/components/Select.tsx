import { useId } from 'react'

export interface SelectOption {
  value: string
  label: string
}

interface SelectProps {
  label: string
  value: string
  options: SelectOption[]
  onChange: (value: string) => void
  error?: string
  required?: boolean
}

export function Select({ label, value, options, onChange, error, required }: SelectProps) {
  const id = useId()
  const errorId = `${id}-error`

  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <select
        id={id}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required={required}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && (
        <p id={errorId} className="field-error">
          {error}
        </p>
      )}
    </div>
  )
}
