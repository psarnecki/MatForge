// Calendar dates as local YYYY-MM-DD strings (toISOString converts to UTC and can shift the day)

export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

export function todayIso(): string {
  return toIsoDate(new Date())
}

// Null for anything that is not a real date, such as 2026-02-30
export function parseIsoDate(text: string | null): Date | null {
  const match = text ? /^(\d{4})-(\d{2})-(\d{2})$/.exec(text) : null
  if (!match) {
    return null
  }
  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
  return toIsoDate(date) === text ? date : null
}

// Whole weeks from Monday to Sunday covering the month, with days of the neighbouring months
export function monthGrid(year: number, month: number): Date[] {
  const offset = (new Date(year, month, 1).getDay() + 6) % 7
  const daysInMonth = new Date(year, month + 1, 0).getDate()
  const count = Math.ceil((offset + daysInMonth) / 7) * 7
  return Array.from({ length: count }, (_, index) => new Date(year, month, 1 - offset + index))
}
