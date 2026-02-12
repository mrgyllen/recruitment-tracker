import { screen } from './test-utils'
import { render } from './test-utils'
import App from './App'

describe('App', () => {
  it('should render the Vite + React heading', () => {
    render(<App />)
    expect(screen.getByText('Vite + React')).toBeInTheDocument()
  })
})
