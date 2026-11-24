import { useState } from 'react'
import { Dialog } from '../../components/Dialog'
import { monthGrid, toIsoDate } from '../../lib/dates'
import { texts } from '../../texts'
import type { SessionSummaryDto } from '../../types'
import { MonthPicker } from './MonthPicker'

interface MonthCalendarProps {
  year: number
  month: number
  selected: string
  today: string
  firstYear: number
  lastYear: number
  sessionsByDate: Map<string, SessionSummaryDto[]>
  onSelect: (date: string) => void
  // Out-of-range months (-1 or 12) roll over into neighbouring years
  onShowMonth: (year: number, month: number) => void
}

export function MonthCalendar({ year, month, selected, today, firstYear, lastYear, sessionsByDate, onSelect, onShowMonth }: MonthCalendarProps) {
  const [pickerOpen, setPickerOpen] = useState(false)
  const isFirstMonth = year === firstYear && month === 0
  const isLastMonth = year === lastYear && month === 11

  function pickMonth(pickedYear: number, pickedMonth: number) {
    setPickerOpen(false)
    onShowMonth(pickedYear, pickedMonth)
  }

  return (
    <div className="sessions-calendar">
      <div className="sessions-month">
        <button type="button" className="sessions-month-button" aria-label={texts.sessions.previousMonth} disabled={isFirstMonth} onClick={() => onShowMonth(year, month - 1)}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <path d="M15 5l-7 7 7 7" />
          </svg>
        </button>
        <button
          type="button"
          className="sessions-month-title"
          aria-haspopup="dialog"
          aria-label={texts.sessions.chooseMonthLabel(year, month)}
          onClick={() => setPickerOpen(true)}
        >
          {texts.sessions.monthTitle(year, month)}
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <path d="M6 9l6 6 6-6" />
          </svg>
        </button>
        <button type="button" className="sessions-month-button" aria-label={texts.sessions.nextMonth} disabled={isLastMonth} onClick={() => onShowMonth(year, month + 1)}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <path d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>

      <Dialog open={pickerOpen} title={texts.sessions.chooseMonth} onClose={() => setPickerOpen(false)}>
        <MonthPicker year={year} month={month} firstYear={firstYear} lastYear={lastYear} onPick={pickMonth} />
      </Dialog>

      {/* Hidden from screen readers, because every day button has its full date as its label */}
      <div className="sessions-weekdays" aria-hidden="true">
        {texts.sessions.weekdaysShort.map((name) => (
          <span key={name}>{name}</span>
        ))}
      </div>

      <div className="sessions-grid">
        {monthGrid(year, month).map((date) => {
          const iso = toIsoDate(date)
          const daySessions = sessionsByDate.get(iso) ?? []
          const grappling = daySessions.filter((session) => session.kind === 'Grappling').length
          const other = daySessions.length - grappling
          const classes = ['sessions-day']
          if (date.getMonth() !== month) {
            classes.push('sessions-day-outside')
          }
          if (iso === today) {
            classes.push('sessions-day-today')
          }
          return (
            <button
              key={iso}
              type="button"
              className={classes.join(' ')}
              aria-label={texts.sessions.dayLabel(date, grappling, other)}
              aria-pressed={iso === selected}
              aria-current={iso === today ? 'date' : undefined}
              onClick={() => onSelect(iso)}
            >
              {date.getDate()}
              {/* One square per kind present that day, plus the total when there is more than one session */}
              <span className="sessions-markers">
                {grappling > 0 && <Marker className="sessions-marker" />}
                {other > 0 && <Marker className="sessions-marker sessions-marker-other" />}
                {daySessions.length > 1 && <span>{daySessions.length}</span>}
              </span>
            </button>
          )
        })}
      </div>
    </div>
  )
}

// SVG square (crispEdges stays pixel-aligned at any scale)
function Marker({ className }: { className: string }) {
  return (
    <svg className={className} viewBox="0 0 8 8" aria-hidden="true">
      <rect width="8" height="8" shapeRendering="crispEdges" />
    </svg>
  )
}
