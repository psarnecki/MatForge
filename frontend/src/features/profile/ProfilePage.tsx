import { texts } from '../../texts'
import { ClubsSection } from './ClubsSection'
import { NameSection } from './NameSection'

export function ProfilePage() {
  return (
    <div className="profile">
      <h1>{texts.profile.title}</h1>
      <NameSection />
      <ClubsSection />
    </div>
  )
}
