import type { BandSensor } from '../types'
import { bandLabel, percentLabel, trendGlyph } from '../format'

/**
 * A threshold-band gauge: one segment per GT_* threshold, filled up to the
 * derived band. Deliberately reads as a coarse band, not a false-precision
 * needle. Fault patterns render as hazard stripes.
 */
export function BandGauge({ sensor }: { sensor: BandSensor }) {
  const fraction = sensor.fraction ?? 0
  return (
    <div className="sensor-card">
      <div className="sensor-head">
        <span className="sensor-name">{sensor.label}</span>
        <span className="sensor-band">
          {bandLabel(sensor)} {percentLabel(sensor) && <em>· {percentLabel(sensor)}</em>}
        </span>
      </div>
      <div className="bandgauge" role="meter" aria-valuetext={bandLabel(sensor)}>
        {sensor.thresholds.map((threshold, i) => {
          const on = sensor.active[i]
          const severity = fraction <= 0.25 ? 'crit' : fraction <= 0.5 ? 'low' : ''
          const cls = ['seg', on ? 'on' : '', on ? severity : '', sensor.fault ? 'fault' : '']
            .filter(Boolean)
            .join(' ')
          return <div key={threshold} className={cls} title={`> ${threshold}`} />
        })}
      </div>
      <div className="sensor-scale">
        <span className="trend">{trendGlyph(sensor.trend)}</span>
        {sensor.thresholds.length > 0 && (
          <span>
            {sensor.thresholds[0]}–{sensor.thresholds[sensor.thresholds.length - 1]}
            {sensor.unit ? ` ${sensor.unit}` : ''}
          </span>
        )}
      </div>
    </div>
  )
}
