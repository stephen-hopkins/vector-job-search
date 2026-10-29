import { render, screen } from '@testing-library/react'
import { expect, test } from 'vitest'
import App from './App'

// P0-TEST-FRONTEND: the placeholder App component renders its static text.
test('P0-TEST-FRONTEND: App renders the placeholder page', () => {
  render(<App />)
  expect(screen.getByText('Vector Job Search')).toBeInTheDocument()
})
