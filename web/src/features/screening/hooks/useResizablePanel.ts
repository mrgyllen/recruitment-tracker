import { useState, useRef, useCallback, useEffect } from 'react'

const STORAGE_PREFIX = 'screening-panel-ratio-'

interface UseResizablePanelOptions {
  storageKey: string
  defaultRatio?: number
  minLeftPx?: number
  minCenterPx?: number
}

interface UseResizablePanelReturn {
  containerRef: React.RefObject<HTMLDivElement | null>
  leftWidth: number
  centerWidth: number
  isDragging: boolean
  dividerProps: {
    onMouseDown: (e: React.MouseEvent) => void
    style: React.CSSProperties
  }
}

const RIGHT_PANEL_WIDTH = 300
const DIVIDER_WIDTH = 4

export function useResizablePanel({
  storageKey,
  defaultRatio = 0.25,
  minLeftPx = 250,
  minCenterPx = 300,
}: UseResizablePanelOptions): UseResizablePanelReturn {
  const containerRef = useRef<HTMLDivElement>(null)
  const [ratio, setRatio] = useState(() => {
    const stored = localStorage.getItem(STORAGE_PREFIX + storageKey)
    return stored ? parseFloat(stored) : defaultRatio
  })
  const [isDragging, setIsDragging] = useState(false)
  const [containerWidth, setContainerWidth] = useState(0)

  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const observer = new ResizeObserver(([entry]) => {
      setContainerWidth(entry.contentRect.width)
    })
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  const availableWidth = Math.max(0, containerWidth - RIGHT_PANEL_WIDTH - DIVIDER_WIDTH)
  const rawLeft = availableWidth * ratio
  const leftWidth = Math.max(minLeftPx, Math.min(availableWidth - minCenterPx, rawLeft))
  const centerWidth = availableWidth - leftWidth

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault()
      setIsDragging(true)

      const startX = e.clientX
      const startRatio = ratio

      const onMouseMove = (moveEvent: MouseEvent) => {
        if (availableWidth <= 0) return
        const delta = moveEvent.clientX - startX
        const newLeft = availableWidth * startRatio + delta
        const clamped = Math.max(minLeftPx, Math.min(availableWidth - minCenterPx, newLeft))
        setRatio(clamped / availableWidth)
      }

      const onMouseUp = () => {
        setIsDragging(false)
        document.removeEventListener('mousemove', onMouseMove)
        document.removeEventListener('mouseup', onMouseUp)
        setRatio((currentRatio) => {
          localStorage.setItem(STORAGE_PREFIX + storageKey, currentRatio.toString())
          return currentRatio
        })
      }

      document.addEventListener('mousemove', onMouseMove)
      document.addEventListener('mouseup', onMouseUp)
    },
    [ratio, availableWidth, minLeftPx, minCenterPx, storageKey],
  )

  return {
    containerRef,
    leftWidth: containerWidth > 0 ? leftWidth : 0,
    centerWidth: containerWidth > 0 ? centerWidth : 0,
    isDragging,
    dividerProps: {
      onMouseDown: handleMouseDown,
      style: { cursor: 'col-resize', width: `${DIVIDER_WIDTH}px` },
    },
  }
}
