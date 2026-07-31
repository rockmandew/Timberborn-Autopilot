import type { BandSensor, Trend } from '../types.js'
import type { SensorConfig } from '../config.js'

/**
 * Threshold-band telemetry: collapses a family of GT_* boolean adapters
 * into a value band, a percentage estimate and a trend.
 */

export interface ThresholdReading {
  value: number
  active: boolean
}

export interface DerivedBand {
  lo: number | null
  hi: number | null
  fraction: number | null
  fault: boolean
}

/**
 * Given ascending thresholds with their states, finds the band the true value
 * sits in. A healthy sensor family reads as a prefix of ONs followed by OFFs;
 * anything else (a higher threshold ON while a lower one is OFF) is flagged
 * as a fault — usually a mis-wired or mis-named sensor in the save.
 */
export function deriveBand(readings: ThresholdReading[], fullScale?: number): DerivedBand {
  const sorted = [...readings].sort((a, b) => a.value - b.value)
  if (sorted.length === 0) return { lo: null, hi: null, fraction: null, fault: false }

  let highestOn = -1
  let lowestOff = sorted.length
  sorted.forEach((r, i) => {
    if (r.active) highestOn = Math.max(highestOn, i)
    else lowestOff = Math.min(lowestOff, i)
  })
  const fault = highestOn > lowestOff

  const lo = highestOn >= 0 ? sorted[highestOn]!.value : null
  const hi = lowestOff < sorted.length ? sorted[lowestOff]!.value : null

  const scale = fullScale ?? sorted[sorted.length - 1]!.value
  let fraction: number | null = null
  if (scale > 0) {
    if (lo !== null && hi !== null) fraction = (lo + hi) / 2 / scale
    else if (lo === null && hi !== null) fraction = hi / 2 / scale
    else if (lo !== null && hi === null) fraction = 1
    if (fraction !== null) fraction = Math.min(1, Math.max(0, fraction))
  }

  return { lo, hi, fraction, fault: fault && highestOn > -1 }
}

interface TrendSample {
  ts: number
  midpoint: number
}

/**
 * Trend from band transitions. With only a handful of thresholds, transitions
 * are rare events — so a single crossing sets the trend, and the trend decays
 * back to "stable" once no crossing has happened inside the window.
 */
export class TrendTracker {
  private last = new Map<string, TrendSample>()
  private direction = new Map<string, { trend: Trend; at: number }>()

  constructor(private readonly windowMs: number) {}

  update(sensorId: string, lo: number | null, hi: number | null, now: number): Trend {
    const midpoint = lo !== null && hi !== null ? (lo + hi) / 2 : lo !== null ? lo : hi !== null ? hi / 2 : null
    if (midpoint === null) return 'unknown'

    const prev = this.last.get(sensorId)
    if (prev && midpoint !== prev.midpoint) {
      this.direction.set(sensorId, { trend: midpoint > prev.midpoint ? 'rising' : 'falling', at: now })
    }
    if (!prev || midpoint !== prev.midpoint) {
      this.last.set(sensorId, { ts: now, midpoint })
    }

    const dir = this.direction.get(sensorId)
    if (dir && now - dir.at <= this.windowMs) return dir.trend
    return 'stable'
  }
}

export function buildBandSensor(
  sensorId: string,
  readings: ThresholdReading[],
  trend: Trend,
  now: number,
  config?: SensorConfig,
): BandSensor {
  const sorted = [...readings].sort((a, b) => a.value - b.value)
  const derived = deriveBand(sorted, config?.fullScale)
  return {
    id: sensorId,
    label: config?.label ?? defaultLabel(sensorId),
    unit: config?.unit ?? null,
    thresholds: sorted.map((r) => r.value),
    active: sorted.map((r) => r.active),
    lo: derived.lo,
    hi: derived.hi,
    fraction: derived.fraction,
    trend,
    fault: derived.fault,
    updatedAt: now,
  }
}

/** "RES.UPPER.DEPTH" → "Res Upper Depth" as a readable fallback. */
function defaultLabel(sensorId: string): string {
  return sensorId
    .split('.')
    .map((part) => part.toLowerCase().replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase()))
    .join(' · ')
}
