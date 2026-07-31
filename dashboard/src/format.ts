import type { BandSensor, GateState } from './types'

/** Human-readable band label, e.g. "2.0–2.5 m", "> 3.0 m", "< 0.5 m", "—". */
export function bandLabel(sensor: BandSensor): string {
  const unit = sensor.unit ? ` ${sensor.unit}` : ''
  const fmt = (v: number) => (Number.isInteger(v) ? v.toString() : v.toFixed(1))
  if (sensor.fault) return 'FAULT'
  if (sensor.lo !== null && sensor.hi !== null) return `${fmt(sensor.lo)}–${fmt(sensor.hi)}${unit}`
  if (sensor.lo !== null) return `> ${fmt(sensor.lo)}${unit}`
  if (sensor.hi !== null) return `< ${fmt(sensor.hi)}${unit}`
  return '—'
}

export function percentLabel(sensor: BandSensor): string {
  return sensor.fraction === null ? '' : `~${Math.round(sensor.fraction * 100)}%`
}

export function trendGlyph(trend: BandSensor['trend']): string {
  switch (trend) {
    case 'rising': return '▲ rising'
    case 'falling': return '▼ falling'
    case 'stable': return '▬ stable'
    default: return '· —'
  }
}

export function gatePositionLabel(gate: GateState): string {
  const value = gate.confirmed ?? gate.requested
  if (value === null) return 'unknown'
  if (typeof value === 'boolean') return value ? 'OPEN' : 'CLOSED'
  return `${value.toFixed(1)} m`
}

export function relativeTime(ts: number, now: number): string {
  const secs = Math.max(0, Math.round((now - ts) / 1000))
  if (secs < 60) return `${secs}s`
  const mins = Math.round(secs / 60)
  if (mins < 60) return `${mins}m`
  return `${Math.round(mins / 60)}h`
}

export function clockTime(ts: number): string {
  const d = new Date(ts)
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}
