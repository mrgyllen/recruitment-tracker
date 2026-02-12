export const mockUsers = {
  userA: { id: 'dev-user-a', name: 'Alice Dev' },
  userB: { id: 'dev-user-b', name: 'Bob Dev' },
  admin: { id: 'dev-admin', name: 'Admin Dev' },
} as const

export function setMockUser(user: { id: string; name: string } | null): void {
  if (user) {
    localStorage.setItem('dev-auth-user', JSON.stringify(user))
  } else {
    localStorage.removeItem('dev-auth-user')
  }
}
