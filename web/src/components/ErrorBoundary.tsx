import { Component } from 'react'
import type { ErrorInfo, ReactNode } from 'react'

import { ActionButton } from './ActionButton'

interface ErrorBoundaryProps {
  children: ReactNode
  fallback?: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

export class ErrorBoundary extends Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught:', error, errorInfo)
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback
      }

      return (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <h2 className="mb-2 text-lg font-semibold text-brand-brown">
            Something went wrong
          </h2>
          <p className="mb-6 text-text-secondary">Try refreshing the page</p>
          <ActionButton
            variant="primary"
            onClick={() => window.location.reload()}
          >
            Reload
          </ActionButton>
        </div>
      )
    }

    return this.props.children
  }
}
