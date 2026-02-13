import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { toast } from 'sonner'
import { describe, expect, it, vi } from 'vitest'
import { Toaster } from '@/components/ui/sonner'
import { useAppToast } from '@/hooks/useAppToast'

vi.mock('sonner', async (importOriginal) => {
  const actual = await importOriginal<typeof import('sonner')>()
  return {
    ...actual,
    toast: {
      ...actual.toast,
      success: vi.fn(actual.toast.success),
      error: vi.fn(actual.toast.error),
      info: vi.fn(actual.toast.info),
    },
  }
})

function ToastTester({ type }: { type: 'success' | 'error' | 'info' }) {
  const appToast = useAppToast()
  return (
    <>
      <Toaster position="bottom-right" visibleToasts={1} />
      <button onClick={() => appToast[type]('Test message')}>Show toast</button>
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

  it('should call toast.success with 3s duration', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="success" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    expect(toast.success).toHaveBeenCalledWith('Test message', {
      duration: 3000,
    })
  })

  it('should call toast.error with Infinity duration (persistent)', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="error" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    expect(toast.error).toHaveBeenCalledWith('Test message', {
      duration: Infinity,
    })
  })

  it('should call toast.info with 5s duration', async () => {
    const user = userEvent.setup()
    render(<ToastTester type="info" />)
    await user.click(screen.getByRole('button', { name: 'Show toast' }))
    expect(toast.info).toHaveBeenCalledWith('Test message', {
      duration: 5000,
    })
  })
})
