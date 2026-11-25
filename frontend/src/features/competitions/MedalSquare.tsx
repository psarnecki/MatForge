interface MedalSquareProps {
  // 1 = gold, 2 = silver, 3 = bronze
  place: number
}

export function MedalSquare({ place }: MedalSquareProps) {
  return (
    <svg className={`competitions-medal competitions-medal-${place}`} viewBox="0 0 8 8" aria-hidden="true">
      <rect width="8" height="8" shapeRendering="crispEdges" />
    </svg>
  )
}
