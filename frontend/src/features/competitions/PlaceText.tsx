import { texts } from '../../texts'
import { MedalSquare } from './MedalSquare'

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
      <MedalSquare place={place} />
      {medal}
    </span>
  )
}
