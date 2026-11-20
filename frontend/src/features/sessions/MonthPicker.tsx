import { useState } from 'react'
import { texts } from '../../texts'

interface MonthPickerProps {
  year: number
  month: number
  firstYear: number
  lastYear: number
  onPick: (year: number, month: number) => void
}

// Rendered only while open, so each opening resets to the calendar year
export function MonthPicker({ year, month, firstYear, lastYear, onPick }: MonthPickerProps) {
  const [shownYear, setShownYear] = useState(year)

  return (
    <div className="sessions-picker">
      <div className="sessions-picker-year">
        <button
          type="button"
          className="sessions-month-button"
          aria-label={texts.sessions.previousYear}
          disabled={shownYear <= firstYear}
          onClick={() => setShownYear(shownYear - 1)}
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <path d="M15 5l-7 7 7 7" />
          </svg>
        </button>
        <span>{shownYear}</span>
        <button
          type="button"
          className="sessions-month-button"
          aria-label={texts.sessions.nextYear}
          disabled={shownYear >= lastYear}
          onClick={() => setShownYear(shownYear + 1)}
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <path d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>
      <div className="sessions-picker-months">
        {texts.sessions.monthsShort.map((name, index) => (
          <button
            key={name}
            type="button"
            className="sessions-picker-month"
            aria-pressed={shownYear === year && index === month}
            onClick={() => onPick(shownYear, index)}
          >
            {name}
          </button>
        ))}
      </div>
    </div>
  )
}
