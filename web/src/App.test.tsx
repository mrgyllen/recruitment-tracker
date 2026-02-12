import App from './App'
import { screen } from './test-utils'
import { render } from './test-utils'

describe('App', () => {
  it('should render the Vite + React heading', () => {
    render(<App />)
    expect(screen.getByText('Vite + React')).toBeInTheDocument()
  })
})
