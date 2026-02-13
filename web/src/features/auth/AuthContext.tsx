import { createContext, useContext, useMemo, type ReactNode } from 'react'
import { DevAuthProvider, useDevAuth } from './DevAuthProvider'

interface AuthUser {
  id: string
  name: string
}

interface AuthContextValue {
  isAuthenticated: boolean
  user: AuthUser | null
  login: () => void
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

function DevAuthBridge({ children }: { children: ReactNode }) {
  const { currentUser, isAuthenticated, setPersona } = useDevAuth()

  const value = useMemo(
    () => ({
      isAuthenticated,
      user: currentUser,
      login: () => setPersona('dev-user-a'),
      signOut: () => setPersona('unauthenticated'),
    }),
    [isAuthenticated, currentUser, setPersona],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

function MsalAuthProvider({ children }: { children: ReactNode }) {
  // MSAL provider implementation — will be completed when production auth is needed.
  // Full implementation requires msalInstance.initialize() which is async.
  const value = useMemo(
    () => ({
      isAuthenticated: false,
      user: null,
      login: () => {
        // Will call msalInstance.loginRedirect() when wired up
      },
      signOut: () => {
        // Will call msalInstance.logoutRedirect() when wired up
      },
    }),
    [],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const isDev = import.meta.env.VITE_AUTH_MODE === 'development'

  if (isDev) {
    return (
      <DevAuthProvider>
        <DevAuthBridge>{children}</DevAuthBridge>
      </DevAuthProvider>
    )
  }

  return <MsalAuthProvider>{children}</MsalAuthProvider>
}
