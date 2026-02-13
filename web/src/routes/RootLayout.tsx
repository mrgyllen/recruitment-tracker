import { Outlet } from 'react-router'

import { AppHeader } from '@/components/AppHeader'
import { ViewportGuard } from '@/components/ViewportGuard'

export function RootLayout() {
  return (
    <ViewportGuard>
      <div className="grid h-screen grid-rows-[48px_1fr]">
        <a
          href="#main-content"
          className="sr-only focus:not-sr-only focus:fixed focus:left-2 focus:top-2 focus:z-50 focus:rounded focus:bg-interactive focus:px-4 focus:py-2 focus:text-white"
        >
          Skip to main content
        </a>
        <AppHeader />
        <main id="main-content" className="overflow-auto">
          <Outlet />
        </main>
      </div>
    </ViewportGuard>
  )
}
