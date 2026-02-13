import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { HomePage } from './HomePage'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@/test-utils'

describe('HomePage', () => {
  it('should render empty state when no recruitments exist', async () => {
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

    render(<HomePage />)

    await waitFor(() => {
      expect(
        screen.getByRole('heading', {
          name: /create your first recruitment/i,
        }),
      ).toBeInTheDocument()
    })
  })

  it('should render recruitment list when recruitments exist', async () => {
    server.use(
      http.get('/api/recruitments', () => {
        return HttpResponse.json({
          items: [
            {
              id: 'test-id',
              title: 'Senior Developer',
              description: 'A test recruitment',
              status: 'Active',
              createdAt: new Date().toISOString(),
              closedAt: null,
              stepCount: 3,
              memberCount: 1,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    render(<HomePage />)

    await waitFor(() => {
      expect(screen.getByText('Senior Developer')).toBeInTheDocument()
    })
  })

  it('should render "Create Recruitment" CTA in empty state', async () => {
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

    render(<HomePage />)

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: /create recruitment/i }),
      ).toBeInTheDocument()
    })
  })
})
