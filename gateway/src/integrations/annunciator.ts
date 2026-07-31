import type { Alarm, Snapshot } from '../types.js'

/**
 * Annunciators are the physical/ambient outputs: Hue groups, the Govee
 * status tower, Discord channels, Alexa announcements, PC audio cues.
 *
 * v1 ships only the console annunciator; the interface is the seam the
 * hardware integrations plug into. Annunciators are OUTPUT-ONLY — they
 * observe state, they never command gates.
 */
export interface Annunciator {
  readonly id: string
  /** Called after every accepted state change (debounced upstream). */
  onSnapshot(snapshot: Snapshot): void | Promise<void>
  /** Called on alarm raise/clear edges only, never on steady state. */
  onAlarm(alarm: Alarm, edge: 'raised' | 'cleared'): void | Promise<void>
  /** Called when the operating mode changes. */
  onMode(mode: string): void | Promise<void>
}

export class ConsoleAnnunciator implements Annunciator {
  readonly id = 'console'

  onSnapshot(): void {
    // Steady-state snapshots stay quiet — ambient outputs shouldn't chatter.
  }

  onAlarm(alarm: Alarm, edge: 'raised' | 'cleared'): void {
    const badge = edge === 'raised' ? (alarm.severity === 'critical' ? '🔴' : '🟠') : '🟢'
    console.log(`${badge} [${edge.toUpperCase()}] ${alarm.message}`)
  }

  onMode(mode: string): void {
    console.log(`◈ Operating mode → ${mode}`)
  }
}
