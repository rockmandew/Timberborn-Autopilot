import type { SensorConfig } from '../config.js'
import type { Alarm, BandSensor } from '../types.js'

/**
 * Alarm evaluation over band sensors. An alarm rule fires when the sensor's
 * guaranteed ceiling (band `hi`) is at or below the configured level — i.e.
 * we KNOW the value is that low, we're not guessing from a missing reading.
 * Sensor faults raise their own critical alarm.
 */
export function evaluateAlarms(
  sensors: BandSensor[],
  sensorConfigs: SensorConfig[],
  mode: string,
  previous: Map<string, Alarm>,
  now: number,
): Alarm[] {
  const configById = new Map(sensorConfigs.map((c) => [c.id, c]))
  const alarms: Alarm[] = []

  for (const sensor of sensors) {
    if (sensor.fault) {
      alarms.push(keepSince(previous, {
        id: `fault:${sensor.id}`,
        severity: 'critical',
        message: `${sensor.label}: inconsistent threshold pattern — check sensor wiring in the save`,
        since: now,
      }))
    }

    const rules = configById.get(sensor.id)?.alarms ?? []
    for (const rule of rules) {
      if (rule.modes && !rule.modes.includes(mode)) continue
      const firing = sensor.hi !== null && sensor.hi <= rule.belowOrAt
      if (firing) {
        alarms.push(keepSince(previous, {
          id: `${sensor.id}:${rule.belowOrAt}`,
          severity: rule.severity,
          message: rule.message,
          since: now,
        }))
      }
    }
  }

  return alarms.sort((a, b) => severityRank(b) - severityRank(a) || a.since - b.since)
}

function keepSince(previous: Map<string, Alarm>, alarm: Alarm): Alarm {
  const existing = previous.get(alarm.id)
  return existing ? { ...alarm, since: existing.since } : alarm
}

function severityRank(alarm: Alarm): number {
  return alarm.severity === 'critical' ? 1 : 0
}
