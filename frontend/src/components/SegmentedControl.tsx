import { useId } from 'react'

interface SegmentedControlProps<T extends string> {
  label: string
  // Hides the label visually but keeps it accessible to screen readers
  hideLabel?: boolean
  options: { value: T; label: string }[]
  value: T | ''
  onChange: (value: T) => void
  error?: string
}

// Group of joined buttons for single selection
export function SegmentedControl<T extends string>({ label, hideLabel, options, value, onChange, error }: SegmentedControlProps<T>) {
  const id = useId()
  const labelId = `${id}-label`
  const errorId = `${id}-error`

  return (
    <div className="field">
      {!hideLabel && (
        <span id={labelId} className="field-label">
          {label}
        </span>
      )}
      <div
        role="group"
        className="segments"
        aria-label={hideLabel ? label : undefined}
        aria-labelledby={hideLabel ? undefined : labelId}
        aria-describedby={error ? errorId : undefined}
      >
        {options.map((option) => (
          <button key={option.value} type="button" aria-pressed={option.value === value} onClick={() => onChange(option.value)}>
            {option.label}
          </button>
        ))}
      </div>
      {error && (
        <p id={errorId} className="field-error">
          {error}
        </p>
      )}
    </div>
  )
}
