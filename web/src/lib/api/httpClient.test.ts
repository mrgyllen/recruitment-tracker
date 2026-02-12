import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'

describe('httpClient', () => {
  describe('dev mode', () => {
    beforeEach(() => {
      vi.stubEnv('VITE_AUTH_MODE', 'development')
      localStorage.clear()
      vi.resetModules()
    })

    afterEach(() => {
      vi.unstubAllEnvs()
    })

    it('should send X-Dev-User-Id and X-Dev-User-Name headers when dev persona is set', async () => {
      localStorage.setItem(
        'dev-auth-user',
        JSON.stringify({ id: 'dev-user-a', name: 'Alice Dev' }),
      )

      let capturedHeaders: Record<string, string> = {}
      server.use(
        http.get('/api/test', ({ request }) => {
          capturedHeaders = {
            'x-dev-user-id': request.headers.get('X-Dev-User-Id') ?? '',
            'x-dev-user-name': request.headers.get('X-Dev-User-Name') ?? '',
          }
          return HttpResponse.json({ ok: true })
        }),
      )

      const { apiGet } = await import('./httpClient')
      const result = await apiGet<{ ok: boolean }>('/test')

      expect(result).toEqual({ ok: true })
      expect(capturedHeaders['x-dev-user-id']).toBe('dev-user-a')
      expect(capturedHeaders['x-dev-user-name']).toBe('Alice Dev')
    })

    it('should send no auth headers when dev persona is unauthenticated', async () => {
      let capturedHeaders: Record<string, string | null> = {}
      server.use(
        http.get('/api/test', ({ request }) => {
          capturedHeaders = {
            'x-dev-user-id': request.headers.get('X-Dev-User-Id'),
            authorization: request.headers.get('Authorization'),
          }
          return HttpResponse.json({ ok: true })
        }),
      )

      const { apiGet } = await import('./httpClient')
      const result = await apiGet<{ ok: boolean }>('/test')

      expect(result).toEqual({ ok: true })
      expect(capturedHeaders['x-dev-user-id']).toBeNull()
      expect(capturedHeaders['authorization']).toBeNull()
    })
  })

  describe('error handling', () => {
    beforeEach(() => {
      vi.stubEnv('VITE_AUTH_MODE', 'development')
      localStorage.setItem(
        'dev-auth-user',
        JSON.stringify({ id: 'dev-user-a', name: 'Alice Dev' }),
      )
      vi.resetModules()
    })

    afterEach(() => {
      vi.unstubAllEnvs()
      localStorage.clear()
    })

    it('should throw ApiError with Problem Details on 400 response', async () => {
      server.use(
        http.get('/api/test', () => {
          return HttpResponse.json(
            {
              type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
              title: 'Validation Failed',
              status: 400,
              errors: { Name: ['Name is required.'] },
            },
            { status: 400 },
          )
        }),
      )

      const { apiGet, ApiError } = await import('./httpClient')

      try {
        await apiGet('/test')
        expect.fail('Expected ApiError to be thrown')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        const apiError = error as InstanceType<typeof ApiError>
        expect(apiError.status).toBe(400)
        expect(apiError.problemDetails.title).toBe('Validation Failed')
      }
    })

    it('should throw AuthError on 401 response', async () => {
      server.use(
        http.get('/api/test', () => {
          return HttpResponse.json(
            { title: 'Unauthorized', status: 401 },
            { status: 401 },
          )
        }),
      )

      const { apiGet, AuthError } = await import('./httpClient')

      try {
        await apiGet('/test')
        expect.fail('Expected AuthError to be thrown')
      } catch (error) {
        expect(error).toBeInstanceOf(AuthError)
      }
    })
  })

  describe('HTTP methods', () => {
    beforeEach(() => {
      vi.stubEnv('VITE_AUTH_MODE', 'development')
      localStorage.setItem(
        'dev-auth-user',
        JSON.stringify({ id: 'dev-user-a', name: 'Alice Dev' }),
      )
      vi.resetModules()
    })

    afterEach(() => {
      vi.unstubAllEnvs()
      localStorage.clear()
    })

    it('should send POST with JSON body', async () => {
      let capturedBody: unknown = null
      server.use(
        http.post('/api/items', async ({ request }) => {
          capturedBody = await request.json()
          return HttpResponse.json({ id: '1' }, { status: 201 })
        }),
      )

      const { apiPost } = await import('./httpClient')
      const result = await apiPost<{ id: string }>('/items', { name: 'Test' })

      expect(result).toEqual({ id: '1' })
      expect(capturedBody).toEqual({ name: 'Test' })
    })

    it('should send PUT with JSON body', async () => {
      server.use(
        http.put('/api/items/1', () => {
          return HttpResponse.json({ id: '1', name: 'Updated' })
        }),
      )

      const { apiPut } = await import('./httpClient')
      const result = await apiPut<{ id: string; name: string }>('/items/1', {
        name: 'Updated',
      })

      expect(result).toEqual({ id: '1', name: 'Updated' })
    })

    it('should send DELETE request', async () => {
      server.use(
        http.delete('/api/items/1', () => {
          return new HttpResponse(null, { status: 204 })
        }),
      )

      const { apiDelete } = await import('./httpClient')
      await apiDelete('/items/1')
    })
  })
})
