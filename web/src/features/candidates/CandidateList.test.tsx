import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { CandidateList } from './CandidateList'
import { mockCandidates } from '@/mocks/fixtures/candidates'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@/test-utils'

const recruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('CandidateList', () => {
  it('should render candidate data in list', async () => {
    render(
      <CandidateList recruitmentId={recruitmentId} isClosed={false} />,
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    })
    expect(screen.getByText('Bob Smith')).toBeInTheDocument()
    expect(screen.getByText(/alice@example.com/)).toBeInTheDocument()
    expect(screen.getByText(/bob@example.com/)).toBeInTheDocument()
  })

  it('should display empty state when no candidates exist', async () => {
    server.use(
      http.get('/api/recruitments/:recruitmentId/candidates', () => {
        return HttpResponse.json({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    render(
      <CandidateList recruitmentId={recruitmentId} isClosed={false} />,
    )

    await waitFor(() => {
      expect(screen.getByText('No candidates yet')).toBeInTheDocument()
    })
  })

  it('should show Add Candidate action in empty state', async () => {
    server.use(
      http.get('/api/recruitments/:recruitmentId/candidates', () => {
        return HttpResponse.json({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    render(
      <CandidateList recruitmentId={recruitmentId} isClosed={false} />,
    )

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: /add candidate/i }),
      ).toBeInTheDocument()
    })
  })

  it('should show remove button for each candidate', async () => {
    render(
      <CandidateList recruitmentId={recruitmentId} isClosed={false} />,
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    })

    const removeButtons = screen.getAllByRole('button', { name: /remove/i })
    expect(removeButtons).toHaveLength(mockCandidates.length)
  })

  it('should show confirmation dialog when remove is clicked', async () => {
    const user = userEvent.setup()

    render(
      <CandidateList recruitmentId={recruitmentId} isClosed={false} />,
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    })

    const removeButtons = screen.getAllByRole('button', { name: /remove/i })
    await user.click(removeButtons[0])

    expect(screen.getByText(/remove alice johnson/i)).toBeInTheDocument()
    expect(screen.getByText(/this cannot be undone/i)).toBeInTheDocument()
  })

  it('should call remove API and show toast when confirmed', async () => {
    const user = userEvent.setup()

    render(
      <CandidateList recruitmentId={recruitmentId} isClosed={false} />,
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    })

    const removeButtons = screen.getAllByRole('button', { name: /remove/i })
    await user.click(removeButtons[0])

    const confirmButton = screen.getByRole('button', { name: /^remove$/i })
    await user.click(confirmButton)

    await waitFor(() => {
      expect(screen.getByText('Candidate removed')).toBeInTheDocument()
    })
  })

  it('should hide add and remove actions when recruitment is closed', async () => {
    render(
      <CandidateList recruitmentId={recruitmentId} isClosed={true} />,
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    })

    expect(
      screen.queryByRole('button', { name: /add candidate/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /remove/i }),
    ).not.toBeInTheDocument()
  })

  it('should show skeleton loader while loading', () => {
    server.use(
      http.get('/api/recruitments/:recruitmentId/candidates', () => {
        return new Promise(() => {
          // Never resolves -- simulates loading
        })
      }),
    )

    render(
      <CandidateList recruitmentId={recruitmentId} isClosed={false} />,
    )

    expect(screen.getByTestId('skeleton-card')).toBeInTheDocument()
  })
})
