import { toast } from 'sonner'

export function useAppToast() {
  return {
    success: (message: string) => {
      toast.success(message, { duration: 3000 })
    },
    error: (message: string) => {
      toast.error(message, { duration: Infinity })
    },
    info: (message: string) => {
      toast.info(message, { duration: 5000 })
    },
  }
}
