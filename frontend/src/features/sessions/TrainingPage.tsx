import { Link, useSearchParams } from 'react-router'
import { monthGrid, parseIsoDate, toIsoDate, todayIso } from '../../lib/dates'
import { texts } from '../../texts'
import type { SessionSummaryDto } from '../../types'
import { DaySessions } from './DaySessions'
import { MonthCalendar } from './MonthCalendar'
import { useSessions } from './useSessions'

export function TrainingPage() {
  // Selected day is stored in the URL (?day=2026-10-15), so a refresh or Back keeps the month
  const [searchParams, setSearchParams] = useSearchParams()
  const today = todayIso()
  const day = searchParams.get('day')

  // The calendar, its arrows and the month picker stay between January 2000 and December of next year
  const firstYear = 2000
  const lastYear = Number(today.slice(0, 4)) + 1
  const dayYear = Number(day?.slice(0, 4))
  const selected = day && parseIsoDate(day) && dayYear >= firstYear && dayYear <= lastYear ? day : today
  const year = Number(selected.slice(0, 4))
  const month = Number(selected.slice(5, 7)) - 1

  // Full grid is loaded, including markers for adjacent months
  const grid = monthGrid(year, month)
  const { sessions, error } = useSessions(toIsoDate(grid[0]), toIsoDate(grid[grid.length - 1]))

  const sessionsByDate = new Map<string, SessionSummaryDto[]>()
  for (const session of sessions ?? []) {
    sessionsByDate.set(session.date, [...(sessionsByDate.get(session.date) ?? []), session])
  }

  // Replacing the history entry keeps Back for leaving the page
  function selectDay(date: string) {
    setSearchParams({ day: date }, { replace: true })
  }

  // New month defaults to today if it contains it, otherwise to its first day
  function showMonth(newYear: number, newMonth: number) {
    const first = toIsoDate(new Date(newYear, newMonth, 1))
    selectDay(today.slice(0, 7) === first.slice(0, 7) ? today : first)
  }

  return (
    <div className="sessions-page">
      <h1>{texts.sessions.title}</h1>
      <div className="sessions-log">
        <Link to="/training/new" className="button button-primary">
          {texts.sessions.logTraining}
        </Link>
      </div>
      <div className="sessions-layout">
        <MonthCalendar
          year={year}
          month={month}
          selected={selected}
          today={today}
          firstYear={firstYear}
          lastYear={lastYear}
          sessionsByDate={sessionsByDate}
          onSelect={selectDay}
          onShowMonth={showMonth}
        />
        <DaySessions date={selected} sessions={sessions === null ? null : (sessionsByDate.get(selected) ?? [])} error={error} />
      </div>
    </div>
  )
}
