import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'

export interface DevUser {
  id: string
  name: string
}

interface DevAuthContextValue {
  currentUser: DevUser | null
  setPersona: (id: string) => void
  isAuthenticated: boolean
}

const DevAuthContext = createContext<DevAuthContextValue | null>(null)

const personas: Record<string, DevUser> = {
  'dev-user-a': { id: 'dev-user-a', name: 'Alice Dev' },
  'dev-user-b': { id: 'dev-user-b', name: 'Bob Dev' },
  'dev-admin': { id: 'dev-admin', name: 'Admin Dev' },
}

function getInitialPersona(): DevUser | null {
  const stored = localStorage.getItem('dev-auth-user')
  if (stored) {
    try {
      return JSON.parse(stored) as DevUser
    } catch {
      return personas['dev-user-a']
    }
  }
  // Default to User A
  const defaultUser = personas['dev-user-a']
  localStorage.setItem('dev-auth-user', JSON.stringify(defaultUser))
  return defaultUser
}

export function DevAuthProvider({ children }: { children: ReactNode }) {
  const [currentUser, setCurrentUser] = useState<DevUser | null>(getInitialPersona)

  const setPersona = useCallback((id: string) => {
    if (id === 'unauthenticated') {
      setCurrentUser(null)
      localStorage.removeItem('dev-auth-user')
    } else {
      const user = personas[id]
      if (user) {
        setCurrentUser(user)
        localStorage.setItem('dev-auth-user', JSON.stringify(user))
      }
    }
  }, [])

  const value = useMemo(
    () => ({
      currentUser,
      setPersona,
      isAuthenticated: currentUser !== null,
    }),
    [currentUser, setPersona],
  )

  return (
    <DevAuthContext.Provider value={value}>
      {children}
      <DevToolbar currentPersonaId={currentUser?.id ?? 'unauthenticated'} onSelect={setPersona} />
    </DevAuthContext.Provider>
  )
}

export function useDevAuth(): DevAuthContextValue {
  const context = useContext(DevAuthContext)
  if (!context) {
    throw new Error('useDevAuth must be used within a DevAuthProvider')
  }
  return context
}

function DevToolbar({
  currentPersonaId,
  onSelect,
}: {
  currentPersonaId: string
  onSelect: (id: string) => void
}) {
  return (
    <div
      style={{
        position: 'fixed',
        bottom: 16,
        right: 16,
        zIndex: 9999,
        background: '#ff6b35',
        color: 'white',
        padding: '8px 12px',
        borderRadius: 8,
        fontFamily: 'monospace',
        fontSize: 13,
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        boxShadow: '0 2px 8px rgba(0,0,0,0.3)',
      }}
    >
      <span style={{ fontWeight: 'bold' }}>DEV MODE</span>
      <select
        value={currentPersonaId}
        onChange={(e) => onSelect(e.target.value)}
        style={{
          background: 'white',
          color: '#333',
          border: 'none',
          borderRadius: 4,
          padding: '2px 4px',
          fontSize: 13,
        }}
      >
        <option value="dev-user-a">Alice Dev (User A)</option>
        <option value="dev-user-b">Bob Dev (User B)</option>
        <option value="dev-admin">Admin Dev</option>
        <option value="unauthenticated">Unauthenticated</option>
      </select>
    </div>
  )
}
