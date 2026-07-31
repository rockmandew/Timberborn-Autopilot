import { resolve, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { loadConfig } from './config.js'
import { Engine } from './engine.js'
import { EventStore } from './events.js'
import { ConsoleAnnunciator } from './integrations/annunciator.js'
import { buildServer } from './server.js'
import { HttpTimberbornClient, type TimberbornApi } from './timberborn/client.js'
import { SimulatedTimberborn } from './timberborn/simulator.js'

const HERE = dirname(fileURLToPath(import.meta.url))

async function main(): Promise<void> {
  const simulate = process.argv.includes('--simulate') || process.env['TIMBEROS_SIMULATE'] === '1'
  const configFlagIndex = process.argv.indexOf('--config')
  const configPath = configFlagIndex >= 0 ? process.argv[configFlagIndex + 1] : process.env['TIMBEROS_CONFIG']

  const { config, path } = loadConfig(configPath)
  console.log(`TimberOS gateway · config: ${path}`)

  const api: TimberbornApi = simulate ? new SimulatedTimberborn() : new HttpTimberbornClient(config.endpoints)
  if (simulate) console.log('Running against the SIMULATOR — no game connection will be made.')
  else console.log(`Timberborn API: ${config.endpoints.baseUrl}`)

  const events = new EventStore(resolve(HERE, '../..', config.gateway.eventStore))
  const engine = new Engine(config, api, events, [new ConsoleAnnunciator()])
  engine.start()

  const server = await buildServer(engine)
  await server.listen({ port: config.gateway.port, host: '127.0.0.1' })
  console.log(`Gateway listening on http://127.0.0.1:${config.gateway.port} (REST + /ws)`)

  const shutdown = async (): Promise<void> => {
    engine.stop()
    await server.close()
    events.close()
    process.exit(0)
  }
  process.on('SIGINT', () => void shutdown())
  process.on('SIGTERM', () => void shutdown())
}

main().catch((err) => {
  console.error(err)
  process.exit(1)
})
