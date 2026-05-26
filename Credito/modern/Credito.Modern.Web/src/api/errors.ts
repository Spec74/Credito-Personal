import type { ProblemDetails } from '../types/api'

export class ApiError extends Error {
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

function readProblemField(
  raw: Record<string, unknown>,
  camel: keyof ProblemDetails,
  pascal: string,
): string | undefined {
  const v = raw[camel] ?? raw[pascal]
  return typeof v === 'string' && v.trim() ? v.trim() : undefined
}

export async function parseApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails | undefined
  let message: string | undefined
  try {
    const raw = (await response.json()) as Record<string, unknown>
    const detail = readProblemField(raw, 'detail', 'Detail')
    const title = readProblemField(raw, 'title', 'Title')
    problem = { detail, title, status: response.status, type: readProblemField(raw, 'type', 'Type') }
    message = detail || title
  } catch {
    /* body vacío o no JSON */
  }
  return new ApiError(
    message || `Error HTTP ${response.status}`,
    response.status,
    problem,
  )
}
