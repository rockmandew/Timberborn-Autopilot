/**
 * Phase-0 tool: discover the exact shape of your Timberborn HTTP API.
 *
 * The endpoint paths in config/timberos.example.json are educated defaults —
 * verify them against your game version before trusting anything else in
 * this repo. With Timberborn running (and at least one HTTP Adapter and
 * HTTP Lever placed in the save), run:
 *
 *   npm run probe                       # tries http://localhost:8080
 *   npm run probe -- http://host:8080   # custom base URL
 *
 * It walks a list of candidate paths, reports which ones respond, and prints
 * the raw payloads so you can update config/endpoints to match reality.
 */

const base = process.argv[2] ?? 'http://localhost:8080'

const CANDIDATES = [
  '/',
  '/adapters',
  '/api/adapters',
  '/switches',
  '/api/switches',
  '/levers',
  '/api/levers',
  '/state',
  '/api/state',
  '/status',
  '/help',
]

async function probe(): Promise<void> {
  console.log(`Probing ${base} …\n`)
  let anyHit = false

  for (const path of CANDIDATES) {
    const url = new URL(path, base)
    try {
      const res = await fetch(url, { signal: AbortSignal.timeout(3000) })
      const body = await res.text()
      const preview = body.length > 400 ? `${body.slice(0, 400)} …[${body.length} bytes]` : body
      console.log(`${res.ok ? '✅' : '▫️'} GET ${path} → ${res.status}`)
      if (res.ok && body.trim().length > 0) {
        anyHit = true
        console.log(`   ${preview.replace(/\n/g, '\n   ')}\n`)
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err)
      console.log(`❌ GET ${path} → ${message}`)
    }
  }

  if (!anyHit) {
    console.log(
      '\nNo endpoints answered. Check that Timberborn is running, a save is loaded,',
      '\nand the HTTP integration is enabled in the game settings.',
    )
  } else {
    console.log('\nUpdate the "endpoints" block in your config to match the paths above.')
  }
}

probe().catch((err) => {
  console.error(err)
  process.exit(1)
})
