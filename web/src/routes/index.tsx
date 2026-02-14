import { createBrowserRouter } from 'react-router'
import { RootLayout } from './RootLayout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { CandidateDetail } from '@/features/candidates/CandidateDetail'
import { CreateRecruitmentPage } from '@/features/recruitments/pages/CreateRecruitmentPage'
import { HomePage } from '@/features/recruitments/pages/HomePage'
import { RecruitmentPage } from '@/features/recruitments/pages/RecruitmentPage'
import { ScreeningLayout } from '@/features/screening/ScreeningLayout'

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
          { path: '/recruitments/:recruitmentId/candidates/:candidateId', element: <CandidateDetail /> },
          { path: '/recruitments/:recruitmentId/screening', element: <ScreeningLayout /> },
        ],
      },
    ],
  },
]

export const router = createBrowserRouter(routeConfig)
