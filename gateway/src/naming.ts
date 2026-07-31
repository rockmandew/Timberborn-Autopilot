/**
 * Parser for the TimberOS naming convention (see docs/NAMING.md).
 *
 * Adapters (game → out):
 *   <DOMAIN>.<SITE>.<MEASURE>.GT_<value>   threshold sensor, e.g. RES.UPPER.DEPTH.GT_2_5
 *   STATE.FG.<SITE>.<NAME>.<position>      gate position acknowledgment
 *   anything else                          surfaced as a raw boolean signal
 *
 * Levers (out → game):
 *   CMD.FG.<SITE>.<NAME>.<position>        gate command; position is OPEN or a value like 1_5
 *   anything else                          surfaced as a raw lever
 *
 * Threshold values use "_" as the decimal separator: GT_2_5 → 2.5, GT_1000 → 1000.
 */

export interface ParsedThreshold {
  kind: 'threshold'
  /** Sensor id = everything before ".GT_", e.g. "RES.UPPER.DEPTH". */
  sensorId: string
  value: number
}

export interface ParsedGateSignal {
  kind: 'gate-command' | 'gate-ack'
  /** Gate id, e.g. "FG.UPPER.SPILLWAY". */
  gateId: string
  /** Numeric position for discrete gates, or 'OPEN' for binary gates. */
  position: number | 'OPEN'
}

export interface ParsedRaw {
  kind: 'raw'
}

export type ParsedName = ParsedThreshold | ParsedGateSignal | ParsedRaw

/** "2_5" → 2.5, "1000" → 1000, "0_0" → 0. Returns null when not numeric. */
export function parseValueToken(token: string): number | null {
  if (!/^\d+(_\d+)?$/.test(token)) return null
  return Number(token.replace('_', '.'))
}

const THRESHOLD_RE = /^(?<sensor>.+)\.GT_(?<value>\d+(?:_\d+)?)$/
const GATE_RE = /^(?<prefix>CMD|STATE)\.(?<gate>FG\.[A-Z0-9_]+\.[A-Z0-9_]+)\.(?<pos>OPEN|\d+_\d+|\d+)$/

export function parseName(name: string): ParsedName {
  const threshold = THRESHOLD_RE.exec(name)
  if (threshold?.groups) {
    const value = parseValueToken(threshold.groups['value']!)
    if (value !== null) {
      return { kind: 'threshold', sensorId: threshold.groups['sensor']!, value }
    }
  }

  const gate = GATE_RE.exec(name)
  if (gate?.groups) {
    const posToken = gate.groups['pos']!
    const position = posToken === 'OPEN' ? ('OPEN' as const) : parseValueToken(posToken)
    if (position !== null) {
      return {
        kind: gate.groups['prefix'] === 'CMD' ? 'gate-command' : 'gate-ack',
        gateId: gate.groups['gate']!,
        position,
      }
    }
  }

  return { kind: 'raw' }
}

/** Reconstructs the lever/adapter name for a gate position. */
export function gateSignalName(prefix: 'CMD' | 'STATE', gateId: string, position: number | 'OPEN'): string {
  const token = position === 'OPEN' ? 'OPEN' : formatValueToken(position)
  return `${prefix}.${gateId}.${token}`
}

/** 2.5 → "2_5", 1000 → "1000", 0 → "0_0" (gate positions always carry a decimal). */
export function formatValueToken(value: number): string {
  if (Number.isInteger(value) && value >= 10) return String(value)
  const [int, frac = '0'] = value.toFixed(1).split('.')
  return `${int}_${frac}`
}
