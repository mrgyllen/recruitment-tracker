import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { teamApi } from '@/lib/api/team'
import type { AddMemberRequest } from '@/lib/api/team.types'

export function useTeamMembers(recruitmentId: string) {
  return useQuery({
    queryKey: ['recruitment', recruitmentId, 'members'],
    queryFn: () => teamApi.getMembers(recruitmentId),
    enabled: !!recruitmentId,
  })
}

export function useDirectorySearch(recruitmentId: string, searchTerm: string) {
  return useQuery({
    queryKey: ['directory-search', recruitmentId, searchTerm],
    queryFn: () => teamApi.searchDirectory(recruitmentId, searchTerm),
    enabled: searchTerm.length >= 2,
  })
}

export function useAddMember(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AddMemberRequest) =>
      teamApi.addMember(recruitmentId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['recruitment', recruitmentId, 'members'],
      })
    },
  })
}

export function useRemoveMember(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (memberId: string) =>
      teamApi.removeMember(recruitmentId, memberId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['recruitment', recruitmentId, 'members'],
      })
    },
  })
}
