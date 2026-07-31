/** Shared domain types for the TimberOS gateway. */

/** A single boolean signal read from the game (HTTP Adapter) or written to it (HTTP Lever). */
export interface SignalReading {
  name: string
  state: boolean
}

export type Trend = 'rising' | 'falling' | 'stable' | 'unknown'

/**
 * A threshold-band sensor derived from a family of GT_* adapters,
 * e.g. RES.UPPER.DEPTH.GT_0_5 … GT_3_0 collapse into one BandSensor.
 */
export interface BandSensor {
  /** Stable id: the adapter name prefix, e.g. "RES.UPPER.DEPTH". */
  id: string
  label: string
  unit: string | null
  /** Ascending threshold values contributed by the GT_* adapters. */
  thresholds: number[]
  /** Which thresholds are currently ON (parallel to `thresholds`). */
  active: boolean[]
  /** Derived band. `lo` null = below lowest threshold, `hi` null = above highest. */
  lo: number | null
  hi: number | null
  /** Midpoint of the band as a fraction of full scale (0..1), null when indeterminate. */
  fraction: number | null
  trend: Trend
  /** True when the pattern is non-monotonic (a higher threshold ON while a lower one is OFF). */
  fault: boolean
  updatedAt: number
}

export type GateKind = 'discrete' | 'binary'
export type CommandStatus = 'idle' | 'pending' | 'confirmed' | 'failed'

export interface GateState {
  /** Stable id, e.g. "FG.UPPER.SPILLWAY". */
  id: string
  label: string
  kind: GateKind
  /** Available discrete positions (empty for binary gates). */
  positions: number[]
  /** Requested position: number for discrete, boolean for binary, null when never commanded. */
  requested: number | boolean | null
  /** Last confirmed position (from STATE.* adapters when present, else assumed). */
  confirmed: number | boolean | null
  status: CommandStatus
  /** True when a matching STATE.* adapter family exists to acknowledge commands. */
  acknowledged: boolean
  /** Interlock rule id currently blocking this gate, if any. */
  blockedBy: string | null
  /** True when commanding this gate requires an explicit confirm flag (two-step commit). */
  confirmRequired: boolean
  updatedAt: number
}

export type AlarmSeverity = 'warning' | 'critical'

export interface Alarm {
  id: string
  severity: AlarmSeverity
  message: string
  since: number
}

/** A raw boolean signal that did not match any naming convention — surfaced as-is. */
export interface RawSignal {
  name: string
  state: boolean
  kind: 'adapter' | 'lever'
}

export interface Snapshot {
  connected: boolean
  simulated: boolean
  mode: string
  automationSuspended: boolean
  sensors: BandSensor[]
  gates: GateState[]
  alarms: Alarm[]
  unmapped: RawSignal[]
  updatedAt: number
}

export interface EventRecord {
  id: number
  ts: number
  /** e.g. "command", "state", "alarm", "mode", "system" */
  kind: string
  subject: string
  message: string
  data: unknown
}
