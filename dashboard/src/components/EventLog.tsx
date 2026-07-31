import type { EventRecord } from '../types'
import { clockTime } from '../format'

export function EventLog({ events }: { events: EventRecord[] }) {
  if (events.length === 0) {
    return <div className="unmapped">No events recorded yet.</div>
  }
  return (
    <div className="events">
      {events.map((event) => (
        <div className="event" key={event.id}>
          <span className="event-ts">{clockTime(event.ts)}</span>
          <span className="event-kind">{event.kind}</span>
          <span className="event-msg">{event.message}</span>
        </div>
      ))}
    </div>
  )
}
