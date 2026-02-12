import App from './App'
import { render, screen } from './test-utils'

describe('App', () => {
  it('should render the Vite + React heading', () => {
    render(<App />)
    expect(screen.getByText('Vite + React')).toBeInTheDocument()
  })
})
