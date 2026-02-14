import { useMutation, useQueryClient } from '@tanstack/react-query'
import { recruitmentApi } from '@/lib/api/recruitments'
import type {
  AddWorkflowStepRequest,
  ReorderStepsRequest,
  UpdateRecruitmentRequest,
} from '@/lib/api/recruitments.types'

export function useUpdateRecruitment(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateRecruitmentRequest) =>
      recruitmentApi.update(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['recruitment', id] })
      void queryClient.invalidateQueries({ queryKey: ['recruitments'] })
    },
  })
}

export function useAddWorkflowStep(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AddWorkflowStepRequest) =>
      recruitmentApi.addStep(recruitmentId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['recruitment', recruitmentId],
      })
    },
  })
}

export function useRemoveWorkflowStep(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (stepId: string) =>
      recruitmentApi.removeStep(recruitmentId, stepId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['recruitment', recruitmentId],
      })
    },
  })
}

export function useReorderWorkflowSteps(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ReorderStepsRequest) =>
      recruitmentApi.reorderSteps(recruitmentId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['recruitment', recruitmentId],
      })
    },
  })
}
