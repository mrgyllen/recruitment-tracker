import { useEffect, useState, type ReactNode } from 'react'

const MIN_WIDTH = 1280
const MEDIA_QUERY = `(min-width: ${MIN_WIDTH}px)`

export function ViewportGuard({ children }: { children: ReactNode }) {
  const [isWideEnough, setIsWideEnough] = useState(
    () => window.matchMedia(MEDIA_QUERY).matches,
  )

  useEffect(() => {
    const mql = window.matchMedia(MEDIA_QUERY)
    const handler = (e: MediaQueryListEvent) => setIsWideEnough(e.matches)
    mql.addEventListener('change', handler)
    return () => mql.removeEventListener('change', handler)
  }, [])

  if (!isWideEnough) {
    return (
      <div className="flex h-screen items-center justify-center p-8 text-center">
        <p
          role="alert"
          aria-live="assertive"
          className="max-w-md text-brand-brown"
        >
          This application is designed for desktop browsers (1280px or wider).
          Please use a wider browser window.
        </p>
      </div>
    )
  }

  return <>{children}</>
}
