import { texts } from '../../texts'

interface PlaceTextProps {
  place: number | null
}

// Top three places as medals, remaining places as ordinals
export function PlaceText({ place }: PlaceTextProps) {
  if (place === null) {
    return <span className="competitions-muted">{texts.competitions.noPlace}</span>
  }
  const medal = texts.competitions.medals[place]
  if (!medal) {
    return <span>{texts.competitions.placeOrdinal(place)}</span>
  }
  return (
    <span className="competitions-place">
      <span className={`competitions-medal competitions-medal-${place}`} aria-hidden="true" />
      {medal}
    </span>
  )
}
