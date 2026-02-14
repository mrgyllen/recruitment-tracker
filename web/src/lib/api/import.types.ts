export type ImportSessionStatus = 'Processing' | 'Completed' | 'Failed'

export interface ImportRowResult {
  rowNumber: number
  candidateEmail: string | null
  action: 'Created' | 'Updated' | 'Errored' | 'Flagged'
  errorMessage: string | null
  resolution: string | null
}

export interface ImportDocumentDto {
  id: string
  candidateName: string
  blobStorageUrl: string
  workdayCandidateId: string | null
  matchStatus: 'Pending' | 'AutoMatched' | 'Unmatched' | 'ManuallyAssigned'
  matchedCandidateId: string | null
}

export interface ImportSessionResponse {
  id: string
  recruitmentId: string
  status: ImportSessionStatus
  sourceFileName: string
  createdAt: string
  completedAt: string | null
  totalRows: number
  createdCount: number
  updatedCount: number
  erroredCount: number
  flaggedCount: number
  failureReason: string | null
  rowResults: ImportRowResult[]
  pdfTotalCandidates: number | null
  pdfSplitCandidates: number | null
  pdfSplitErrors: number
  originalBundleBlobUrl: string | null
  importDocuments: ImportDocumentDto[]
}

export interface StartImportResponse {
  importSessionId: string
  statusUrl: string
}

export interface ResolveMatchRequest {
  matchIndex: number
  action: 'Confirm' | 'Reject'
}

export interface ResolveMatchResponse {
  matchIndex: number
  action: string
  candidateEmail: string | null
}
