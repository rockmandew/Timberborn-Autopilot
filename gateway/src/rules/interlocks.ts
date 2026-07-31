import type { InterlockConfig } from '../config.js'
import type { BandSensor, GateState } from '../types.js'

/**
 * Safety interlocks: declarative rules that block gate commands whose
 * preconditions don't hold (e.g. "the contamination inlet may only open
 * while the badwater diversion is confirmed open").
 *
 * Interlocks are evaluated against CONFIRMED state, not requested state —
 * a command that has been sent but not acknowledged does not satisfy a
 * precondition.
 */

export interface InterlockContext {
  sensors: Map<string, BandSensor>
  gates: Map<string, GateState>
}

export interface InterlockViolation {
  ruleId: string
  description: string
}

export function checkInterlocks(
  rules: InterlockConfig[],
  gateId: string,
  target: number | 'OPEN' | 'CLOSED',
  ctx: InterlockContext,
): InterlockViolation | null {
  for (const rule of rules) {
    if (rule.gate !== gateId) continue
    if (!commandMatches(rule.whenCommanded, target)) continue
    if (!conditionHolds(rule, ctx)) {
      return { ruleId: rule.id, description: rule.description }
    }
  }
  return null
}

function commandMatches(when: 'open' | 'closed' | number, target: number | 'OPEN' | 'CLOSED'): boolean {
  if (typeof when === 'number') return target === when
  const opening = target === 'OPEN' || (typeof target === 'number' && target > 0)
  return when === 'open' ? opening : !opening
}

function conditionHolds(rule: InterlockConfig, ctx: InterlockContext): boolean {
  const cond = rule.require
  if ('gate' in cond) {
    const gate = ctx.gates.get(cond.gate)
    if (!gate) return false
    const confirmedOpen =
      gate.confirmed === true || (typeof gate.confirmed === 'number' && gate.confirmed > 0)
    return cond.state === 'open' ? confirmedOpen : !confirmedOpen
  }
  const sensor = ctx.sensors.get(cond.sensor)
  if (!sensor || sensor.fault) return false
  if ('atLeast' in cond) {
    // The band's guaranteed floor must clear the requirement.
    return sensor.lo !== null && sensor.lo >= cond.atLeast
  }
  // "below": the band's guaranteed ceiling must be under the limit.
  return sensor.hi !== null && sensor.hi <= cond.below
}
