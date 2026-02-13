import { useAuth } from '@/features/auth/AuthContext'

import { ActionButton } from './ActionButton'

export function AppHeader() {
  const { user, signOut } = useAuth()

  return (
    <header className="flex h-12 items-center justify-between border-b border-border-default bg-bg-surface px-4">
      <span className="text-sm font-semibold text-brand-brown">
        Recruitment Tracker
      </span>
      <div className="flex items-center gap-3">
        {user && (
          <span className="text-sm text-text-secondary">{user.name}</span>
        )}
        <ActionButton variant="secondary" onClick={signOut}>
          Sign out
        </ActionButton>
      </div>
    </header>
  )
}
