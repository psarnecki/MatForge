import { useState } from 'react'
import { Button } from '../../components/Button'
import { Dialog } from '../../components/Dialog'
import { texts } from '../../texts'
import { useCurrentUser } from '../auth/currentUser'
import { DeleteAccountForm } from './DeleteAccountForm'

export function DeleteAccountSection() {
  const { user } = useCurrentUser()
  const [confirming, setConfirming] = useState(false)

  return (
    <section className="profile-section" aria-labelledby="profile-delete-title">
      <h2 id="profile-delete-title">{texts.profile.deleteAccountTitle}</h2>
      <div className="profile-form">
        <p className="profile-muted">{texts.profile.deleteAccountInfo}</p>
        <Button variant="secondary" onClick={() => setConfirming(true)}>
          {texts.profile.deleteAccount}
        </Button>
      </div>
      <Dialog open={confirming} title={texts.profile.deleteAccountTitle} onClose={() => setConfirming(false)}>
        <DeleteAccountForm email={user.email} onCancel={() => setConfirming(false)} />
      </Dialog>
    </section>
  )
}
