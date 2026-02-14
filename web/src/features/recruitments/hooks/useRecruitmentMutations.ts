import { useMutation, useQueryClient } from '@tanstack/react-query'
import type {
  AddWorkflowStepRequest,
  ReorderStepsRequest,
  UpdateRecruitmentRequest,
} from '@/lib/api/recruitments.types'
import { recruitmentApi } from '@/lib/api/recruitments'

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

export function useCloseRecruitment(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => recruitmentApi.close(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['recruitment', id] })
      void queryClient.invalidateQueries({ queryKey: ['recruitments'] })
    },
  })
}
