import { useState, type SubmitEvent } from 'react'
import { useNavigate } from 'react-router'
import { apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'

interface DeleteAccountFormProps {
  email: string
  onCancel: () => void
}

export function DeleteAccountForm({ email, onCancel }: DeleteAccountFormProps) {
  const [typedEmail, setTypedEmail] = useState('')
  const [deleting, setDeleting] = useState(false)
  const navigate = useNavigate()
  const showToast = useToast()
  const confirmed = typedEmail.trim().toLowerCase() === email.toLowerCase()

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!confirmed) {
      return
    }
    setDeleting(true)
    try {
      await apiFetch('/me', { method: 'DELETE' })
      showToast(texts.profile.accountDeleted, 'success')
      navigate('/login', { replace: true })
    } catch (caught) {
      showToast(errorMessage(caught), 'error')
      setDeleting(false)
    }
  }

  return (
    <form className="profile-form" onSubmit={handleSubmit}>
      <p>{texts.profile.deleteAccountInfo}</p>
      <Field
        label={texts.profile.confirmEmail}
        type="email"
        value={typedEmail}
        onChange={setTypedEmail}
        hint={email}
        autoComplete="off"
      />
      <div className="profile-dialog-actions">
        <Button variant="secondary" onClick={onCancel}>
          {texts.common.cancel}
        </Button>
        <Button type="submit" loading={deleting} disabled={!confirmed}>
          {texts.profile.deleteAccount}
        </Button>
      </div>
    </form>
  )
}
