import { useState } from 'react'
import { useDebounce } from '@/hooks/useDebounce'
import { useAddMember, useDirectorySearch } from './hooks/useTeamMembers'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'
import type { DirectoryUserDto } from '@/lib/api/team.types'

interface InviteMemberDialogProps {
  recruitmentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function InviteMemberDialog({
  recruitmentId,
  open,
  onOpenChange,
}: InviteMemberDialogProps) {
  const [searchTerm, setSearchTerm] = useState('')
  const debouncedTerm = useDebounce(searchTerm, 300)
  const { data: searchResults, isPending: isSearching } = useDirectorySearch(
    recruitmentId,
    debouncedTerm,
  )
  const addMember = useAddMember(recruitmentId)
  const toast = useAppToast()
  const [error, setError] = useState<string | null>(null)

  function handleSelect(user: DirectoryUserDto) {
    setError(null)
    addMember.mutate(
      { userId: user.id, displayName: user.displayName },
      {
        onSuccess: () => {
          toast.success(`${user.displayName} added to team`)
          setSearchTerm('')
          onOpenChange(false)
        },
        onError: (err) => {
          if (err instanceof ApiError) {
            setError(err.problemDetails.detail ?? err.problemDetails.title)
          } else {
            setError('Failed to add member')
          }
        },
      },
    )
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      setSearchTerm('')
      setError(null)
    }
    onOpenChange(nextOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Invite Team Member</DialogTitle>
        </DialogHeader>

        <Input
          placeholder="Search by name or email..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          autoFocus
        />

        {error && (
          <p className="text-sm text-destructive">{error}</p>
        )}

        {isSearching && debouncedTerm.length >= 2 && (
          <p className="text-sm text-muted-foreground">Searching...</p>
        )}

        {searchResults && searchResults.length > 0 && (
          <div className="max-h-60 space-y-1 overflow-y-auto">
            {searchResults.map((user) => (
              <Button
                key={user.id}
                variant="ghost"
                className="w-full justify-start"
                onClick={() => handleSelect(user)}
                disabled={addMember.isPending}
              >
                <div className="text-left">
                  <div className="font-medium">{user.displayName}</div>
                  <div className="text-sm text-muted-foreground">{user.email}</div>
                </div>
              </Button>
            ))}
          </div>
        )}

        {searchResults && searchResults.length === 0 && debouncedTerm.length >= 2 && (
          <p className="text-sm text-muted-foreground">No users found</p>
        )}
      </DialogContent>
    </Dialog>
  )
}
