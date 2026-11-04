import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { after, before, test } from 'node:test';
import { fileURLToPath } from 'node:url';

const tool = join(dirname(dirname(fileURLToPath(import.meta.url))), 'scrub-paths.mjs');
const runTool = (...args) => spawnSync(process.execPath, [tool, ...args], { encoding: 'utf8' });
const BS = String.fromCharCode(92);
const LOCAL_PATH = `C:${BS}Users${BS}someone${BS}Downloads${BS}Project One${BS}Screw.blend`;

let temp;
before(() => { temp = mkdtempSync(join(tmpdir(), 'lab scrub test ')); });
after(() => rmSync(temp, { recursive: true, force: true }));

function fileWith(name, bytes) {
  const path = join(temp, name);
  writeFileSync(path, bytes);
  return path;
}

test('an FBX keeps its length and layout, with the path blanked by spaces', () => {
  const before = Buffer.from(`Kaydara\0S7\0\0\0${LOCAL_PATH}\0\0tail`, 'latin1');
  const path = fileWith('a.fbx', before);
  const result = runTool(path);
  assert.equal(result.status, 0, result.stderr);
  const after = readFileSync(path);
  assert.equal(after.length, before.length);
  assert.ok(!after.toString('latin1').includes('Users'));
  assert.ok(after.toString('latin1').startsWith('Kaydara\0S7\0\0\0   '));
  assert.ok(after.toString('latin1').endsWith('\0\0tail'));
  assert.match(result.stdout, /1 path/);
});

test('a .blend keeps its length, with the path blanked by NUL bytes', () => {
  const before = Buffer.from(`BLENDER17-01\0${LOCAL_PATH}\0more`, 'latin1');
  const path = fileWith('a.blend', before);
  assert.equal(runTool(path).status, 0);
  const after = readFileSync(path);
  assert.equal(after.length, before.length);
  assert.equal(after.toString('latin1'), `BLENDER17-01${'\0'.repeat(LOCAL_PATH.length + 2)}more`);
});

test('forward slashes, other drives and any case are covered', () => {
  const text = 'x\0d:/users/SOMEONE/a.blend\0E:' + BS + BS + 'USERS' + BS + BS + 'me' + BS + BS + 'b\0y';
  const path = fileWith('b.fbx', Buffer.from(text, 'latin1'));
  assert.equal(runTool(path).status, 0);
  const after = readFileSync(path).toString('latin1').toLowerCase();
  assert.ok(!after.includes('users'));
});

test('a path with non-ASCII folder names is blanked completely', () => {
  const before = Buffer.concat([Buffer.from('x\0C:' + BS + 'Users' + BS + 'me' + BS, 'latin1'), Buffer.from('ÄRZTE', 'utf8'), Buffer.from('\0y', 'latin1')]);
  const path = fileWith('c.fbx', before);
  assert.equal(runTool(path).status, 0);
  const after = readFileSync(path);
  assert.equal(after.length, before.length);
  assert.ok(!after.toString('latin1').includes('Users') && !after.includes(Buffer.from('RZTE')));
});

test('paths outside a user folder and relative links are left alone', () => {
  const text = `BLENDER17-01\0//..${BS}Program Files${BS}Blender${BS}a.blend\0C:${BS}Program Files${BS}x\0`;
  const path = fileWith('d.blend', Buffer.from(text, 'latin1'));
  const result = runTool(path);
  assert.equal(result.status, 0);
  assert.equal(readFileSync(path).toString('latin1'), text);
  assert.match(result.stdout, /no paths/);
});

test('a compressed .blend fails clearly and is not changed', () => {
  const bytes = Buffer.from([0x28, 0xb5, 0x2f, 0xfd, 1, 2, 3]);
  const path = fileWith('e.blend', bytes);
  const result = runTool(path);
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /Compress/);
  assert.deepEqual(readFileSync(path), bytes);
});

test('a missing file fails and later files are not skipped silently', () => {
  const good = fileWith('f.fbx', Buffer.from(`x\0${LOCAL_PATH}\0`, 'latin1'));
  const result = runTool(join(temp, 'missing.fbx'), good);
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /not found/);
  assert.ok(!readFileSync(good).toString('latin1').includes('Users'));
});

test('no arguments print the usage', () => {
  const result = runTool();
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /usage/i);
});
