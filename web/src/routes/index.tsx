import { createBrowserRouter } from 'react-router'
import { RootLayout } from './RootLayout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { CreateRecruitmentPage } from '@/features/recruitments/pages/CreateRecruitmentPage'
import { HomePage } from '@/features/recruitments/pages/HomePage'
import { RecruitmentPage } from '@/features/recruitments/pages/RecruitmentPage'

export const routeConfig = [
  {
    element: <RootLayout />,
    children: [
      {
        element: <ProtectedRoute />,
        children: [
          { path: '/', element: <HomePage /> },
          { path: '/recruitments/new', element: <CreateRecruitmentPage /> },
          { path: '/recruitments/:recruitmentId', element: <RecruitmentPage /> },
        ],
      },
    ],
  },
]

export const router = createBrowserRouter(routeConfig)
