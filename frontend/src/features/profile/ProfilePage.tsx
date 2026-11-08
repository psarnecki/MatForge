import { texts } from '../../texts'
import { BeltsSection } from './BeltsSection'
import { ClubsSection } from './ClubsSection'
import { DeleteAccountSection } from './DeleteAccountSection'
import { NameSection } from './NameSection'

export function ProfilePage() {
  return (
    <div className="profile">
      <h1>{texts.profile.title}</h1>
      <NameSection />
      <ClubsSection />
      <BeltsSection />
      <DeleteAccountSection />
    </div>
  )
}
