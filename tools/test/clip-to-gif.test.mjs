import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync, mkdtempSync, readFileSync, rmSync, statSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { after, before, test } from 'node:test';
import { fileURLToPath } from 'node:url';

const tool = join(dirname(dirname(fileURLToPath(import.meta.url))), 'clip-to-gif.mjs');
const runTool = (...args) => spawnSync(process.execPath, [tool, ...args], { encoding: 'utf8' });

let temp;
let clip;

before(() => {
  temp = mkdtempSync(join(tmpdir(), 'lab gif test '));
  clip = join(temp, 'clip.mp4');
  const made = spawnSync('ffmpeg', ['-y', '-loglevel', 'error', '-f', 'lavfi', '-i', 'testsrc=duration=3:size=1280x720:rate=30', clip]);
  assert.equal(made.status, 0, 'ffmpeg could not make the test clip');
});

after(() => rmSync(temp, { recursive: true, force: true }));

test('a clip in a folder with a space becomes a GIF of at most 5 MB', () => {
  const out = join(temp, 'out.gif');
  const result = runTool(clip, out);
  assert.equal(result.status, 0, result.stderr);
  assert.equal(readFileSync(out).subarray(0, 3).toString('ascii'), 'GIF');
  assert.ok(statSync(out).size <= 5 * 1024 * 1024);
});

test('the output folder is created when missing', () => {
  const out = join(temp, 'new folder', 'out.gif');
  assert.equal(runTool(clip, out).status, 0);
  assert.ok(existsSync(out));
});

test('a missing input fails and writes nothing', () => {
  const out = join(temp, 'none.gif');
  const result = runTool(join(temp, 'missing.mp4'), out);
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /not found/);
  assert.ok(!existsSync(out));
});

test('a limit that cannot be met fails clearly and leaves no file', () => {
  const out = join(temp, 'tiny.gif');
  const result = runTool(clip, out, '0.001');
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /limit/);
  assert.ok(!existsSync(out));
});

test('a failed run does not keep an older GIF at the output path', () => {
  const out = join(temp, 'older.gif');
  assert.equal(runTool(clip, out).status, 0);
  assert.notEqual(runTool(clip, out, '0.001').status, 0);
  assert.ok(!existsSync(out));
});

test('missing arguments print the usage', () => {
  const result = runTool();
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /usage/i);
});
