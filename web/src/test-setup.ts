import '@testing-library/jest-dom'
import { afterAll, afterEach, beforeAll, expect } from 'vitest'
import * as axeMatchers from 'vitest-axe/matchers'
import { server } from './mocks/server'

expect.extend(axeMatchers)

// Node 25+ has a built-in localStorage that conflicts with jsdom.
// Replace it with a simple Map-based implementation for tests.
const localStorageMap = new Map<string, string>()
const localStorageMock: Storage = {
  getItem: (key: string) => localStorageMap.get(key) ?? null,
  setItem: (key: string, value: string) => {
    localStorageMap.set(key, value)
  },
  removeItem: (key: string) => {
    localStorageMap.delete(key)
  },
  clear: () => {
    localStorageMap.clear()
  },
  get length() {
    return localStorageMap.size
  },
  key: (index: number) => [...localStorageMap.keys()][index] ?? null,
}
Object.defineProperty(globalThis, 'localStorage', {
  value: localStorageMock,
  writable: true,
})

beforeAll(() => server.listen({ onUnhandledRequest: 'bypass' }))
afterEach(() => server.resetHandlers())
afterAll(() => server.close())
