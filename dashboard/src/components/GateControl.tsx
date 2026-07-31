import { useTimberOS } from '../store'
import type { GateState } from '../types'
import { gatePositionLabel } from '../format'

/**
 * Gate command control. Discrete gates render as a segmented position
 * selector; binary gates as an OPEN/CLOSED pair. The gateway enforces
 * mutual exclusion and interlocks — this component only reflects state
 * and issues intent.
 */
export function GateControl({ gate }: { gate: GateState }) {
  const commandGate = useTimberOS((s) => s.commandGate)

  const send = async (position: number | 'OPEN' | 'CLOSED') => {
    const result = await commandGate(gate.id, position, false)
    if (result.status === 'needs-confirm') {
      const ok = confirm(`${gate.label}: ${result.message}\n\nConfirm this protected command?`)
      if (ok) await commandGate(gate.id, position, true)
    }
  }

  return (
    <div className="gate">
      <div className="gate-head">
        <span className="gate-name">
          {gate.label}
          {gate.confirmRequired && <span title="Protected control"> 🔒</span>}
        </span>
        <StatusChip gate={gate} />
      </div>

      {gate.kind === 'discrete' ? (
        <div className="segmented" role="group" aria-label={`${gate.label} position`}>
          {gate.positions.map((pos) => (
            <button
              key={pos}
              className={buttonClass(gate, pos)}
              disabled={!gate.acknowledged && gate.status === 'pending'}
              onClick={() => void send(pos)}
            >
              {pos.toFixed(1)}
            </button>
          ))}
        </div>
      ) : (
        <div className="segmented" role="group" aria-label={`${gate.label} open or closed`}>
          <button
            className={gate.confirmed === false ? 'active' : ''}
            onClick={() => void send('CLOSED')}
          >
            CLOSED
          </button>
          <button
            className={gate.confirmed === true ? 'active' : ''}
            onClick={() => void send('OPEN')}
          >
            OPEN
          </button>
        </div>
      )}

      <div className="gate-status">
        {gate.status === 'pending' && '⏳ awaiting confirmation'}
        {gate.status === 'confirmed' && `✓ confirmed at ${gatePositionLabel(gate)}`}
        {gate.status === 'failed' && '✗ command not acknowledged'}
        {gate.status === 'idle' && (gate.acknowledged ? `at ${gatePositionLabel(gate)}` : `at ${gatePositionLabel(gate)} (unacknowledged)`)}
      </div>
    </div>
  )
}

function buttonClass(gate: GateState, pos: number): string {
  const classes: string[] = []
  if (gate.confirmed === pos) classes.push('active')
  if (gate.requested === pos && gate.confirmed !== pos) classes.push('requested')
  return classes.join(' ')
}

function StatusChip({ gate }: { gate: GateState }) {
  if (!gate.acknowledged) return <span className="chip offline">no ack</span>
  switch (gate.status) {
    case 'pending': return <span className="chip warning">pending</span>
    case 'failed': return <span className="chip critical">failed</span>
    case 'confirmed': return <span className="chip good">confirmed</span>
    default: return <span className="chip good">ok</span>
  }
}
