import { useEffect, useRef, useState } from 'react'
import { Link, NavLink, Outlet, useLocation } from 'react-router'
import { displayName, useCurrentUser } from '../features/auth/currentUser'
import { texts } from '../texts'

const navItems = [
  { to: '/', label: texts.nav.dashboard },
  { to: '/training', label: texts.nav.training },
  { to: '/competitions', label: texts.nav.competitions },
  { to: '/goals', label: texts.nav.goals },
  { to: '/library', label: texts.nav.library },
  { to: '/body', label: texts.nav.body },
]

// Desktop and tablet: links in the top bar; phone: a hamburger opens the same links in a side panel
export function AppShell() {
  const { user, logout } = useCurrentUser()
  const { pathname } = useLocation()
  const menuRef = useRef<HTMLDialogElement>(null)
  // The menu belongs to the page it was opened on, so any navigation closes it
  const [menuOpenedAt, setMenuOpenedAt] = useState<string | null>(null)
  const menuOpen = menuOpenedAt === pathname

  // Native modal <dialog>: moves focus inside, closes on Escape, returns focus to the hamburger
  useEffect(() => {
    const menu = menuRef.current
    if (!menu) {
      return
    }
    if (menuOpen && !menu.open) {
      menu.showModal()
    }
    if (!menuOpen && menu.open) {
      menu.close()
    }
  }, [menuOpen])

  const closeMenu = () => setMenuOpenedAt(null)

  return (
    <>
      <header className="shell-topbar">
        <button
          type="button"
          className="icon-button shell-menu-button"
          aria-label={texts.nav.openMenu}
          aria-expanded={menuOpen}
          aria-controls="shell-menu"
          onClick={() => setMenuOpenedAt(pathname)}
        >
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <path d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <Link to="/" className="shell-logo">
          {texts.appName}
        </Link>
        <nav className="shell-links" aria-label={texts.nav.main}>
          {navItems.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.to === '/'} className="shell-link">
              {item.label}
            </NavLink>
          ))}
        </nav>
        <NavLink to="/profile" className="shell-link shell-user">
          {displayName(user)}
        </NavLink>
        <button type="button" className="shell-logout" onClick={logout}>
          {texts.nav.logOut}
        </button>
        <NavLink to="/profile" className="icon-button shell-profile-icon" aria-label={texts.nav.profile}>
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <circle cx="12" cy="8" r="4" />
            <path d="M4 21c0-4 4-6 8-6s8 2 8 6" />
          </svg>
        </NavLink>
      </header>

      <dialog
        ref={menuRef}
        id="shell-menu"
        className="shell-menu"
        aria-label={texts.nav.main}
        onClose={closeMenu}
        onClick={(event) => {
          // A click on the dialog itself, not its content, is a click on the backdrop
          if (event.target === event.currentTarget) {
            closeMenu()
          }
        }}
      >
        <div className="shell-menu-panel">
          <button type="button" className="icon-button shell-menu-close" aria-label={texts.common.close} onClick={closeMenu}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <path d="M6 6l12 12M18 6L6 18" />
            </svg>
          </button>
          <nav className="shell-menu-links">
            {navItems.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.to === '/'} className="shell-menu-item" onClick={closeMenu}>
                {item.label}
              </NavLink>
            ))}
          </nav>
          <button type="button" className="shell-menu-item shell-menu-logout" onClick={logout}>
            {texts.nav.logOut}
          </button>
        </div>
      </dialog>

      <main className="shell-main">
        <Outlet />
      </main>
    </>
  )
}
