// Turns a screen recording into a GIF under a size limit, for media/.
// Usage: node tools/clip-to-gif.mjs <clip.mp4> <out.gif> [maxMB, default 5]
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, rmSync, statSync } from 'node:fs';
import { dirname } from 'node:path';

// Width in pixels and frames per second, tried in order until the file fits.
const ATTEMPTS = [[800, 15], [640, 15], [640, 12], [480, 12], [480, 10], [360, 10]];

function fail(message) {
  console.error(message);
  process.exit(1);
}

const [inPath, outPath, maxMbText = '5'] = process.argv.slice(2);
if (!inPath || !outPath) fail('Usage: node tools/clip-to-gif.mjs <clip.mp4> <out.gif> [maxMB]');
if (!existsSync(inPath)) fail(`Clip not found: ${inPath}`);
const maxMb = Number(maxMbText);
if (!(maxMb > 0)) fail(`maxMB must be a number above 0: ${maxMbText}`);

mkdirSync(dirname(outPath), { recursive: true });

for (const [width, fps] of ATTEMPTS) {
  const filter = `fps=${fps},scale=${width}:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=5`;
  const run = spawnSync('ffmpeg', ['-y', '-loglevel', 'error', '-i', inPath, '-vf', filter, outPath], { stdio: ['ignore', 'ignore', 'inherit'] });
  if (run.error || run.status !== 0) {
    rmSync(outPath, { force: true });
    fail(`ffmpeg failed on ${inPath}${run.error ? `: ${run.error.message}` : ''}`);
  }
  const bytes = statSync(outPath).size;
  if (bytes <= maxMb * 1024 * 1024) {
    console.log(`${outPath}: ${(bytes / (1024 * 1024)).toFixed(2)} MB at ${width} px wide, ${fps} fps`);
    process.exit(0);
  }
}

rmSync(outPath, { force: true });
fail(`${inPath} doesn't fit the ${maxMb} MB limit even at the smallest setting. Record a shorter clip or a smaller area.`);
