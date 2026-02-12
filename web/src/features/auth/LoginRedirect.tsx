import { useEffect } from 'react'

export function LoginRedirect() {
  useEffect(() => {
    const isDev = import.meta.env.VITE_AUTH_MODE === 'development'
    if (!isDev) {
      import('./msalConfig').then(({ msalInstance, loginRequest }) => {
        msalInstance.loginRedirect(loginRequest)
      })
    }
  }, [])

  return <div>Redirecting to login...</div>
}
