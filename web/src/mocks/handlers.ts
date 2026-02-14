import { candidateHandlers } from './candidateHandlers'
import { importHandlers } from './importHandlers'
import { recruitmentHandlers } from './recruitmentHandlers'
import { screeningHandlers } from './screeningHandlers'
import { teamHandlers } from './teamHandlers'
import type { RequestHandler } from 'msw'

export const handlers: RequestHandler[] = [
  ...recruitmentHandlers,
  ...teamHandlers,
  ...candidateHandlers,
  ...importHandlers,
  ...screeningHandlers,
]
