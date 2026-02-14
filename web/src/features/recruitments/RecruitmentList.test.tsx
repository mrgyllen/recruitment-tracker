import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { RecruitmentList } from './RecruitmentList'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@/test-utils'

describe('RecruitmentList', () => {
  it('renders list of recruitments with titles and status', async () => {
    render(<RecruitmentList />)

    await waitFor(() => {
      expect(screen.getByText('Senior .NET Developer')).toBeInTheDocument()
    })
    expect(screen.getByText('Frontend Engineer')).toBeInTheDocument()
    expect(screen.getByText('Active')).toBeInTheDocument()
    expect(screen.getByText('Closed')).toBeInTheDocument()
  })

  it('renders empty state when no recruitments', async () => {
    server.use(
      http.get('/api/recruitments', () => {
        return HttpResponse.json({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    render(<RecruitmentList />)

    await waitFor(() => {
      expect(
        screen.getByRole('heading', {
          name: /create your first recruitment/i,
        }),
      ).toBeInTheDocument()
    })
  })

  it('renders skeleton during loading', () => {
    // Default MSW handler has a response, but the query is pending initially
    render(<RecruitmentList />)

    expect(screen.getAllByTestId('skeleton-card')).toHaveLength(3)
  })

  it('renders recruitment items as links', async () => {
    render(<RecruitmentList />)

    await waitFor(() => {
      expect(screen.getByText('Senior .NET Developer')).toBeInTheDocument()
    })

    const link = screen.getByText('Senior .NET Developer').closest('a')
    expect(link).toHaveAttribute(
      'href',
      '/recruitments/550e8400-e29b-41d4-a716-446655440000',
    )
  })
})
