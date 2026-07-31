import { useTimberOS } from '../store'

/** The operating modes from gateway config, keyed by id. */
const MODE_LABELS: Record<string, string> = {
  normal: 'Normal Operations',
  drought_prep: 'Drought Preparation',
  drought_emergency: 'Drought Emergency',
  badtide_isolation: 'Badtide Isolation',
  recovery: 'Reservoir Recovery',
  manual: 'Manual Engineering',
}

export function ModeSelector({ current }: { current: string }) {
  const setMode = useTimberOS((s) => s.setMode)
  return (
    <div className="modes">
      {Object.entries(MODE_LABELS).map(([id, label]) => (
        <button
          key={id}
          className={id === current ? 'active' : ''}
          onClick={() => void setMode(id)}
        >
          {label}
        </button>
      ))}
    </div>
  )
}
