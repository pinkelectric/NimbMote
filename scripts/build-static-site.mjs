import { spawnSync } from 'node:child_process';
import { join } from 'node:path';

const cli = join(process.cwd(), 'node_modules', 'vinext', 'dist', 'cli.js');

const result = spawnSync(process.execPath, [cli, 'build'], {
  env: { ...process.env, NIMBMOTE_STATIC_EXPORT: '1' },
  stdio: 'inherit',
});

if (result.error) {
  throw result.error;
}

process.exit(result.status ?? 1);
