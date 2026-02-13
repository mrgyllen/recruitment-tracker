import { createBrowserRouter } from 'react-router'
import { RootLayout } from './RootLayout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { HomePage } from '@/features/recruitments/pages/HomePage'

export const routeConfig = [
  {
    element: <RootLayout />,
    children: [
      {
        element: <ProtectedRoute />,
        children: [{ path: '/', element: <HomePage /> }],
      },
    ],
  },
]

export const router = createBrowserRouter(routeConfig)
