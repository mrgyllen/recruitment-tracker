import { render } from '@testing-library/react'
import { AuthProvider } from './features/auth/AuthContext'
import type { RenderOptions } from '@testing-library/react'
import type { ReactElement } from 'react'

function AllProviders({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>
}

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => render(ui, { wrapper: AllProviders, ...options })

export * from '@testing-library/react'
export { customRender as render }
