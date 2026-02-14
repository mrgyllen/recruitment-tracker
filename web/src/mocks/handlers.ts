import { candidateHandlers } from './candidateHandlers'
import { importHandlers } from './importHandlers'
import { recruitmentHandlers } from './recruitmentHandlers'
import { teamHandlers } from './teamHandlers'
import type { RequestHandler } from 'msw'

export const handlers: RequestHandler[] = [
  ...recruitmentHandlers,
  ...teamHandlers,
  ...candidateHandlers,
  ...importHandlers,
]
