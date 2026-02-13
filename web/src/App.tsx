import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { RouterProvider } from 'react-router'

import { Toaster } from '@/components/ui/sonner'

import { AuthProvider } from './features/auth/AuthContext'
import { queryClient } from './lib/queryClient'
import { router } from './routes'

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
        <Toaster
          position="bottom-right"
          toastOptions={{ style: { fontFamily: 'var(--font-primary)' } }}
          visibleToasts={1}
        />
      </AuthProvider>
      {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
    </QueryClientProvider>
  )
}

export default App
