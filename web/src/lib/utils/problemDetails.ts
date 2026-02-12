export interface ProblemDetails {
  type?: string
  title: string
  status: number
  detail?: string
  errors?: Record<string, string[]>
}

export function parseProblemDetails(json: unknown): ProblemDetails {
  if (
    typeof json === 'object' &&
    json !== null &&
    'title' in json &&
    'status' in json &&
    typeof (json as Record<string, unknown>).title === 'string' &&
    typeof (json as Record<string, unknown>).status === 'number'
  ) {
    const obj = json as Record<string, unknown>
    return {
      type: typeof obj.type === 'string' ? obj.type : undefined,
      title: obj.title as string,
      status: obj.status as number,
      detail: typeof obj.detail === 'string' ? obj.detail : undefined,
      errors: obj.errors as Record<string, string[]> | undefined,
    }
  }

  return {
    title: 'An unexpected error occurred',
    status: 500,
  }
}
