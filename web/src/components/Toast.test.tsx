import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { Toaster } from '@/components/ui/sonner'
import { useAppToast } from '@/hooks/useAppToast'

function ToastTester({ type }: { type: 'success' | 'error' | 'info' }) {
  const toast = useAppToast()
  return (
    <>
      <Toaster position="bottom-right" visibleToasts={1} />
      <button onClick={() => toast[type]('Test message')}>Show toast</button>
    </>
  )
}

describe('Toast system', () => {
  it('should show success toast with correct content', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="success" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    await waitFor(() => {
      expect(screen.getByText('Test message')).toBeInTheDocument()
    })
  })

  it('should show error toast with correct content', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="error" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    await waitFor(() => {
      expect(screen.getByText('Test message')).toBeInTheDocument()
    })
  })

  it('should show info toast with correct content', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="info" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    await waitFor(() => {
      expect(screen.getByText('Test message')).toBeInTheDocument()
    })
  })
})
