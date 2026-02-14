import { apiDelete, apiGet, apiPost } from './httpClient'
import type {
  AddMemberRequest,
  DirectoryUserDto,
  MembersListResponse,
} from './team.types'

export const teamApi = {
  getMembers: (recruitmentId: string) =>
    apiGet<MembersListResponse>(`/recruitments/${recruitmentId}/members`),

  searchDirectory: (recruitmentId: string, query: string) =>
    apiGet<DirectoryUserDto[]>(
      `/recruitments/${recruitmentId}/directory-search?q=${encodeURIComponent(query)}`,
    ),

  addMember: (recruitmentId: string, data: AddMemberRequest) =>
    apiPost<{ id: string }>(`/recruitments/${recruitmentId}/members`, data),

  removeMember: (recruitmentId: string, memberId: string) =>
    apiDelete(`/recruitments/${recruitmentId}/members/${memberId}`),
}
