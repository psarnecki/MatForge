import { useEffect, useState, type SubmitEvent } from 'react'
import { apiFetch, errorMessage } from '../../api/apiFetch'
import { Button } from '../../components/Button'
import { Dialog } from '../../components/Dialog'
import { Field } from '../../components/Field'
import { useToast } from '../../components/useToast'
import { texts } from '../../texts'
import type { ClubDto, ClubRequest } from '../../types'
import { clubNameError } from './clubErrors'
import { DeleteClubForm } from './DeleteClubForm'
import { RenameClubForm } from './RenameClubForm'

export function ClubsSection() {
  const [clubs, setClubs] = useState<ClubDto[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [newName, setNewName] = useState('')
  const [addError, setAddError] = useState<string | undefined>()
  const [adding, setAdding] = useState(false)
  const [renaming, setRenaming] = useState<ClubDto | null>(null)
  const [deleting, setDeleting] = useState<ClubDto | null>(null)
  const showToast = useToast()

  // Increased after every change, so the list is loaded again
  const [reloadCount, setReloadCount] = useState(0)

  useEffect(() => {
    let ignore = false
    apiFetch<ClubDto[]>('/clubs')
      .then((loaded) => {
        if (!ignore) {
          setClubs(loaded)
          setLoadError(null)
        }
      })
      .catch((caught: unknown) => {
        if (!ignore) {
          setLoadError(errorMessage(caught))
        }
      })
    return () => {
      ignore = true
    }
  }, [reloadCount])

  function handleChanged(message: string) {
    setRenaming(null)
    setDeleting(null)
    showToast(message, 'success')
    setReloadCount((count) => count + 1)
  }

  async function handleAdd(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setAddError(undefined)
    setAdding(true)
    try {
      const request: ClubRequest = { name: newName.trim() }
      await apiFetch<ClubDto>('/clubs', { method: 'POST', body: request })
      setNewName('')
      handleChanged(texts.profile.clubAdded)
    } catch (caught) {
      const fieldError = clubNameError(caught)
      if (fieldError) {
        setAddError(fieldError)
      } else {
        showToast(errorMessage(caught), 'error')
      }
    } finally {
      setAdding(false)
    }
  }

  return (
    <section className="profile-section" aria-labelledby="profile-clubs-title">
      <h2 id="profile-clubs-title">{texts.profile.clubsTitle}</h2>

      {loadError && (
        <p className="profile-error" role="alert">
          {loadError}
        </p>
      )}
      {clubs && clubs.length === 0 && <p className="profile-muted">{texts.profile.clubsEmpty}</p>}
      {clubs && clubs.length > 0 && (
        <ul className="profile-clubs">
          {clubs.map((club) => (
            <li key={club.id} className="profile-club">
              <span>{club.name}</span>
              <button
                type="button"
                className="profile-club-action"
                aria-label={texts.profile.renameClubLabel(club.name)}
                onClick={() => setRenaming(club)}
              >
                {texts.profile.rename}
              </button>
              <button
                type="button"
                className="profile-club-action"
                aria-label={texts.profile.deleteClubLabel(club.name)}
                onClick={() => setDeleting(club)}
              >
                {texts.profile.delete}
              </button>
            </li>
          ))}
        </ul>
      )}

      <form className="profile-add-club" onSubmit={handleAdd}>
        <Field label={texts.profile.newClub} value={newName} onChange={setNewName} error={addError} required maxLength={100} />
        <Button type="submit" variant="secondary" loading={adding}>
          {texts.profile.addClub}
        </Button>
      </form>

      <Dialog open={renaming !== null} title={texts.profile.renameClubTitle} onClose={() => setRenaming(null)}>
        {renaming && (
          <RenameClubForm
            club={renaming}
            onCancel={() => setRenaming(null)}
            onRenamed={() => handleChanged(texts.profile.clubRenamed)}
          />
        )}
      </Dialog>

      <Dialog open={deleting !== null} title={texts.profile.deleteClubTitle} onClose={() => setDeleting(null)}>
        {deleting && (
          <DeleteClubForm
            club={deleting}
            onCancel={() => setDeleting(null)}
            onDeleted={() => handleChanged(texts.profile.clubDeleted)}
          />
        )}
      </Dialog>
    </section>
  )
}
