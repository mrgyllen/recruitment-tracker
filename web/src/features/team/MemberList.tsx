import { useState } from 'react'
import { useRemoveMember, useTeamMembers } from './hooks/useTeamMembers'
import { InviteMemberDialog } from './InviteMemberDialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { useAppToast } from '@/hooks/useAppToast'
import type { TeamMemberDto } from '@/lib/api/team.types'

interface MemberListProps {
  recruitmentId: string
  disabled?: boolean
}

export function MemberList({ recruitmentId, disabled }: MemberListProps) {
  const { data, isPending } = useTeamMembers(recruitmentId)
  const removeMember = useRemoveMember(recruitmentId)
  const toast = useAppToast()
  const [inviteOpen, setInviteOpen] = useState(false)
  const [confirmRemove, setConfirmRemove] = useState<TeamMemberDto | null>(null)

  if (isPending) {
    return <SkeletonLoader variant="card" />
  }

  const members = data?.members ?? []

  function handleRemove(member: TeamMemberDto) {
    setConfirmRemove(member)
  }

  function confirmRemoval() {
    if (!confirmRemove) return
    removeMember.mutate(confirmRemove.id, {
      onSuccess: () => {
        toast.success('Member removed')
        setConfirmRemove(null)
      },
      onError: () => {
        toast.error('Failed to remove member')
        setConfirmRemove(null)
      },
    })
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Team Members</h2>
        {!disabled && (
          <Button onClick={() => setInviteOpen(true)}>
            Invite Member
          </Button>
        )}
      </div>

      <div className="divide-y rounded-lg border">
        {members.map((member) => (
          <div key={member.id} className="flex items-center justify-between p-4">
            <div className="flex items-center gap-3">
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium">
                    {member.displayName ?? member.userId}
                  </span>
                  {member.isCreator && (
                    <Badge variant="secondary">Creator</Badge>
                  )}
                </div>
                <span className="text-sm text-muted-foreground">
                  {member.role}
                </span>
              </div>
            </div>
            {!member.isCreator && !disabled && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleRemove(member)}
                aria-label={`Remove ${member.displayName ?? 'member'}`}
              >
                Remove
              </Button>
            )}
          </div>
        ))}
      </div>

      {confirmRemove && (
        <div className="rounded-lg border border-destructive bg-destructive/5 p-4">
          <p>
            Remove {confirmRemove.displayName ?? 'this member'} from this
            recruitment?
          </p>
          <div className="mt-3 flex gap-2">
            <Button
              variant="destructive"
              size="sm"
              onClick={confirmRemoval}
              disabled={removeMember.isPending}
            >
              {removeMember.isPending ? 'Removing...' : 'Confirm Remove'}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setConfirmRemove(null)}
            >
              Cancel
            </Button>
          </div>
        </div>
      )}

      <InviteMemberDialog
        recruitmentId={recruitmentId}
        open={inviteOpen}
        onOpenChange={setInviteOpen}
      />
    </div>
  )
}
