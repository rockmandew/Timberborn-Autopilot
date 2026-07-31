import type { Alarm } from '../types'
import { relativeTime } from '../format'

export function AlarmPanel({ alarms, now }: { alarms: Alarm[]; now: number }) {
  if (alarms.length === 0) {
    return <div className="all-clear">✓ ALL SYSTEMS STABLE — no active alarms</div>
  }
  return (
    <div>
      {alarms.map((alarm) => (
        <div key={alarm.id} className={`alarm ${alarm.severity}`}>
          <span className="alarm-icon">{alarm.severity === 'critical' ? '🔴' : '⚠️'}</span>
          <span className="alarm-msg">{alarm.message}</span>
          <span className="alarm-age">{relativeTime(alarm.since, now)}</span>
        </div>
      ))}
    </div>
  )
}
