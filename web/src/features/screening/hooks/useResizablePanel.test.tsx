import { renderHook, act } from '@testing-library/react'
import { render, screen } from '@testing-library/react'
import { useResizablePanel } from './useResizablePanel'

let resizeCallback: ResizeObserverCallback | null = null
const mockObserve = vi.fn()
const mockDisconnect = vi.fn()

class MockResizeObserver {
  constructor(callback: ResizeObserverCallback) {
    resizeCallback = callback
  }
  observe = mockObserve
  disconnect = mockDisconnect
  unobserve = vi.fn()
}

beforeEach(() => {
  localStorage.clear()
  resizeCallback = null
  vi.stubGlobal('ResizeObserver', MockResizeObserver)
})

afterEach(() => {
  vi.restoreAllMocks()
})

// Helper component that mounts the ref onto a real DOM element
function TestHarness(props: Parameters<typeof useResizablePanel>[0] & { onResult: (r: ReturnType<typeof useResizablePanel>) => void }) {
  const { onResult, ...options } = props
  const result = useResizablePanel(options)
  onResult(result)
  return <div ref={result.containerRef} data-testid="container" />
}

function simulateResize(width: number) {
  act(() => {
    resizeCallback?.(
      [{ contentRect: { width } } as ResizeObserverEntry],
      {} as ResizeObserver,
    )
  })
}

describe('useResizablePanel', () => {
  it('should initialize with zero widths before container observation', () => {
    let latest: ReturnType<typeof useResizablePanel> | null = null
    render(
      <TestHarness
        storageKey="test"
        defaultRatio={0.25}
        onResult={(r) => { latest = r }}
      />,
    )
    // Before ResizeObserver fires
    expect(latest!.leftWidth).toBe(0)
    expect(latest!.centerWidth).toBe(0)
    expect(latest!.isDragging).toBe(false)
  })

  it('should compute widths after container is observed', () => {
    let latest: ReturnType<typeof useResizablePanel> | null = null
    render(
      <TestHarness
        storageKey="test"
        defaultRatio={0.25}
        minLeftPx={250}
        minCenterPx={300}
        onResult={(r) => { latest = r }}
      />,
    )

    expect(mockObserve).toHaveBeenCalled()
    simulateResize(1200)

    // containerWidth=1200, rightPanel=300, divider=4
    // available = 1200 - 300 - 4 = 896
    // leftWidth = 896 * 0.25 = 224 -> clamped to min 250
    expect(latest!.leftWidth).toBe(250)
    expect(latest!.centerWidth).toBe(646)
  })

  it('should restore ratio from localStorage on mount', () => {
    localStorage.setItem('screening-panel-ratio-test', '0.5')
    let latest: ReturnType<typeof useResizablePanel> | null = null
    render(
      <TestHarness
        storageKey="test"
        defaultRatio={0.25}
        minLeftPx={250}
        minCenterPx={300}
        onResult={(r) => { latest = r }}
      />,
    )

    simulateResize(1200)

    // available = 896, 896 * 0.5 = 448
    expect(latest!.leftWidth).toBe(448)
    expect(latest!.centerWidth).toBe(448)
  })

  it('should enforce minimum left width constraint', () => {
    let latest: ReturnType<typeof useResizablePanel> | null = null
    render(
      <TestHarness
        storageKey="test"
        defaultRatio={0.1}
        minLeftPx={250}
        minCenterPx={300}
        onResult={(r) => { latest = r }}
      />,
    )

    simulateResize(1200)

    // available = 896, 896 * 0.1 = 89.6 -> clamped to 250
    expect(latest!.leftWidth).toBe(250)
  })

  it('should enforce minimum center width constraint', () => {
    let latest: ReturnType<typeof useResizablePanel> | null = null
    render(
      <TestHarness
        storageKey="test"
        defaultRatio={0.9}
        minLeftPx={250}
        minCenterPx={300}
        onResult={(r) => { latest = r }}
      />,
    )

    simulateResize(1200)

    // available = 896, 896 * 0.9 = 806.4 -> clamped to 896 - 300 = 596
    expect(latest!.leftWidth).toBe(596)
    expect(latest!.centerWidth).toBe(300)
  })

  it('should provide dividerProps with correct cursor style', () => {
    let latest: ReturnType<typeof useResizablePanel> | null = null
    render(
      <TestHarness
        storageKey="test"
        onResult={(r) => { latest = r }}
      />,
    )

    expect(latest!.dividerProps.style.cursor).toBe('col-resize')
    expect(typeof latest!.dividerProps.onMouseDown).toBe('function')
  })
})
