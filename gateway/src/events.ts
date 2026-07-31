import { DatabaseSync } from 'node:sqlite'
import { mkdirSync } from 'node:fs'
import { dirname } from 'node:path'
import type { EventRecord } from './types.js'

/**
 * Append-only event store (SQLite via the Node 22+ built-in driver).
 * Every command, confirmed state change, alarm transition and mode change
 * lands here; Discord posting, trend charts and the beaver-times digest all
 * read from this log later.
 */
export class EventStore {
  private db: DatabaseSync
  private insert: ReturnType<DatabaseSync['prepare']>

  constructor(path: string) {
    if (path !== ':memory:') mkdirSync(dirname(path), { recursive: true })
    this.db = new DatabaseSync(path)
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS events (
        id      INTEGER PRIMARY KEY AUTOINCREMENT,
        ts      INTEGER NOT NULL,
        kind    TEXT    NOT NULL,
        subject TEXT    NOT NULL,
        message TEXT    NOT NULL,
        data    TEXT
      );
      CREATE INDEX IF NOT EXISTS idx_events_ts ON events (ts);
      CREATE INDEX IF NOT EXISTS idx_events_subject ON events (subject, ts);
    `)
    this.insert = this.db.prepare(
      'INSERT INTO events (ts, kind, subject, message, data) VALUES (?, ?, ?, ?, ?)',
    )
  }

  append(kind: string, subject: string, message: string, data?: unknown): void {
    this.insert.run(Date.now(), kind, subject, message, data === undefined ? null : JSON.stringify(data))
  }

  recent(limit = 100): EventRecord[] {
    const rows = this.db
      .prepare('SELECT id, ts, kind, subject, message, data FROM events ORDER BY id DESC LIMIT ?')
      .all(limit) as Array<{ id: number; ts: number; kind: string; subject: string; message: string; data: string | null }>
    return rows.map((row) => ({
      id: row.id,
      ts: row.ts,
      kind: row.kind,
      subject: row.subject,
      message: row.message,
      data: row.data === null ? undefined : JSON.parse(row.data),
    }))
  }

  close(): void {
    this.db.close()
  }
}
