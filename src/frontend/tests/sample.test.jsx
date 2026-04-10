import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import App from '../src/App.jsx'

describe('App', () => {
  it('renders the BookFast platform shell', () => {
    render(<App />)

    expect(
      screen.getByRole('heading', { name: /BookFast integration platform/i })
    ).toBeTruthy()
    expect(screen.getByText(/REST \/api\/v1/i)).toBeTruthy()
    expect(screen.getByText(/Azure API Management/i)).toBeTruthy()
  })
})
