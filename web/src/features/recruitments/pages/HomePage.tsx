import { useNavigate } from 'react-router'
import { useRecruitments } from '../hooks/useRecruitments'
import { RecruitmentList } from '../RecruitmentList'
import { Button } from '@/components/ui/button'

export function HomePage() {
  const navigate = useNavigate()
  const { data } = useRecruitments()
  const hasRecruitments = data && data.items.length > 0

  return (
    <>
      {hasRecruitments && (
        <div className="mx-auto flex max-w-4xl items-center justify-between px-6 pt-6">
          <h1 className="text-2xl font-semibold">Recruitments</h1>
          <Button onClick={() => void navigate('/recruitments/new')}>
            Create Recruitment
          </Button>
        </div>
      )}
      <RecruitmentList />
    </>
  )
}
