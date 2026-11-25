import { useId } from 'react'
import { texts } from '../../texts'
import { MedalSquare } from './MedalSquare'

const medalPlaces = [1, 2, 3]

interface PlacePickerProps {
  // Text value, empty when no place is set
  value: string
  onChange: (value: string) => void
  error?: string
}

// Gold, silver and bronze fill in the place number, emptying the number clears the place
export function PlacePicker({ value, onChange, error }: PlacePickerProps) {
  const id = useId()
  const labelId = `${id}-label`
  const errorId = `${id}-error`

  return (
    <div className="field">
      <span id={labelId} className="field-label">
        {texts.competitions.place}
      </span>
      <div role="group" className="competitions-places" aria-labelledby={labelId}>
        {medalPlaces.map((place) => (
          <button key={place} type="button" aria-pressed={value === String(place)} onClick={() => onChange(String(place))}>
            <MedalSquare place={place} />
            {texts.competitions.medals[place]}
          </button>
        ))}
        <input
          type="number"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          aria-label={texts.competitions.otherPlace}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
        />
      </div>
      {error && (
        <p id={errorId} className="field-error">
          {error}
        </p>
      )}
    </div>
  )
}
