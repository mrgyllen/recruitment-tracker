export interface TeamMemberDto {
  id: string
  userId: string
  displayName: string | null
  role: string
  isCreator: boolean
  invitedAt: string
}

export interface MembersListResponse {
  members: TeamMemberDto[]
  totalCount: number
}

export interface DirectoryUserDto {
  id: string
  displayName: string
  email: string
}

export interface AddMemberRequest {
  userId: string
  displayName: string
}
