import { apiGet, apiPost, apiPostFormData } from './httpClient'
import type {
  ImportSessionResponse,
  ResolveMatchRequest,
  ResolveMatchResponse,
  StartImportResponse,
} from './import.types'

export const importApi = {
  startImport: (recruitmentId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiPostFormData<StartImportResponse>(
      `/recruitments/${recruitmentId}/import`,
      formData,
    )
  },

  getSession: (importSessionId: string) =>
    apiGet<ImportSessionResponse>(`/import-sessions/${importSessionId}`),

  resolveMatch: (importSessionId: string, data: ResolveMatchRequest) =>
    apiPost<ResolveMatchResponse>(
      `/import-sessions/${importSessionId}/resolve-match`,
      data,
    ),
}
