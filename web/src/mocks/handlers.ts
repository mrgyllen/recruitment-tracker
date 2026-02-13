import { recruitmentHandlers } from './recruitmentHandlers'
import type { RequestHandler } from 'msw'

export const handlers: RequestHandler[] = [...recruitmentHandlers]
