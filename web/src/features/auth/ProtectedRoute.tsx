import { useEffect } from 'react'
import { Outlet } from 'react-router'
import { useAuth } from './AuthContext'

export function ProtectedRoute() {
  const { isAuthenticated, login } = useAuth()

  useEffect(() => {
    if (!isAuthenticated) {
      login()
    }
  }, [isAuthenticated, login])

  if (!isAuthenticated) {
    return <p>Redirecting to login...</p>
  }

  return <Outlet />
}
