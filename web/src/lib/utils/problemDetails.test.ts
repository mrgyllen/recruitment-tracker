import { describe, expect, it } from 'vitest'
import { parseProblemDetails } from './problemDetails'

describe('parseProblemDetails', () => {
  it('should parse a valid Problem Details response', () => {
    const json = {
      type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
      title: 'Validation Failed',
      status: 400,
      detail: 'One or more validation errors occurred.',
      errors: { Title: ['Title is required.'] },
    }

    const result = parseProblemDetails(json)

    expect(result.type).toBe('https://tools.ietf.org/html/rfc7231#section-6.5.1')
    expect(result.title).toBe('Validation Failed')
    expect(result.status).toBe(400)
    expect(result.detail).toBe('One or more validation errors occurred.')
    expect(result.errors).toEqual({ Title: ['Title is required.'] })
  })

  it('should handle minimal Problem Details (only status and title)', () => {
    const json = {
      title: 'Not Found',
      status: 404,
    }

    const result = parseProblemDetails(json)

    expect(result.title).toBe('Not Found')
    expect(result.status).toBe(404)
    expect(result.detail).toBeUndefined()
    expect(result.errors).toBeUndefined()
  })

  it('should return fallback for non-Problem Details JSON', () => {
    const json = { message: 'Something went wrong' }

    const result = parseProblemDetails(json)

    expect(result.title).toBe('An unexpected error occurred')
    expect(result.status).toBe(500)
  })
})
