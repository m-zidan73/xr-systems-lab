import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { build } from '../build.mjs';
import { makeRepo, validCard } from './helpers.mjs';

const siteDir = dirname(dirname(fileURLToPath(import.meta.url)));
const DONE = { id: 'W01-D0-finished', title: 'Finished Thing', media: { gif: 'media/W01-D0-finished.gif', video: '' } };
const WIP = { id: 'W01-D1-unfinished', day: 1, status: 'wip', title: 'Unfinished Thing', howItWorks: '', decisions: [], next: '', media: { gif: 'media/W01-D1-unfinished.gif', video: '' } };

function runCli(root) {
  cpSync(siteDir, join(root, 'site'), { recursive: true, filter: src => !src.includes('dist') && !src.includes('test') });
  return spawnSync(process.execPath, ['site/build.mjs'], { cwd: root, encoding: 'utf8' });
}

test('only done cards reach the README, index and site', () => {
  const { root, files } = makeRepo({ cards: [validCard(DONE), validCard(WIP)] });
  const outDir = join(root, 'dist');
  const { cards, errors } = build({ repoRoot: root, outDir });
  assert.deepEqual(errors, []);
  assert.equal(cards.length, 1);
  const html = readFileSync(join(outDir, 'index.html'), 'utf8');
  const index = readFileSync(join(root, 'README.md'), 'utf8');
  assert.ok(html.includes('Finished Thing') && index.includes('Finished Thing'));
  assert.ok(!html.includes('Unfinished Thing') && !index.includes('Unfinished Thing'));
  assert.ok(existsSync(join(dirname(files[0]), 'README.md')));
  assert.ok(!existsSync(join(dirname(files[1]), 'README.md')));
  assert.ok(existsSync(join(outDir, 'media/W01-D0-finished.gif')));
  assert.ok(!existsSync(join(outDir, 'media/W01-D1-unfinished.gif')));
});

test('no finished systems still builds a page', () => {
  const { root } = makeRepo({ cards: [] });
  const outDir = join(root, 'dist');
  const { cards, errors } = build({ repoRoot: root, outDir });
  assert.deepEqual(errors, []);
  assert.equal(cards.length, 0);
  assert.ok(readFileSync(join(outDir, 'index.html'), 'utf8').includes('No finished systems yet.'));
});

test('web demo is copied only when a card has webDemo', () => {
  const { root } = makeRepo({ cards: [validCard({ ...DONE, webDemo: 'web/demo' })] });
  mkdirSync(join(root, 'web/demo'), { recursive: true });
  mkdirSync(join(root, 'web/other'), { recursive: true });
  mkdirSync(join(root, 'web/_vendor'), { recursive: true });
  writeFileSync(join(root, 'web/demo/index.html'), 'demo');
  writeFileSync(join(root, 'web/other/index.html'), 'other');
  writeFileSync(join(root, 'web/_vendor/three.js'), 'three');
  const outDir = join(root, 'dist');
  assert.deepEqual(build({ repoRoot: root, outDir }).errors, []);
  assert.ok(existsSync(join(outDir, 'web/demo/index.html')));
  assert.ok(existsSync(join(outDir, 'web/_vendor/three.js')));
  assert.ok(!existsSync(join(outDir, 'web/other')));

  const plain = makeRepo({ cards: [validCard(DONE)] });
  mkdirSync(join(plain.root, 'web/_vendor'), { recursive: true });
  const plainOut = join(plain.root, 'dist');
  assert.deepEqual(build({ repoRoot: plain.root, outDir: plainOut }).errors, []);
  assert.ok(!existsSync(join(plainOut, 'web')));
});

test('outDir is emptied first', () => {
  const { root } = makeRepo({ cards: [validCard(DONE)] });
  const outDir = join(root, 'dist');
  mkdirSync(outDir, { recursive: true });
  writeFileSync(join(outDir, 'stale.txt'), 'old');
  assert.deepEqual(build({ repoRoot: root, outDir }).errors, []);
  assert.ok(!existsSync(join(outDir, 'stale.txt')));
});

test('invalid card returns errors and writes nothing', () => {
  const { root, files } = makeRepo({ cards: [validCard(DONE), validCard({ ...DONE, id: 'W01-D1-bad', day: 1, howItWorks: ' ' })] });
  const outDir = join(root, 'dist');
  const { errors } = build({ repoRoot: root, outDir });
  assert.ok(errors.some(e => e.includes(files[1]) && e.includes('howItWorks')), JSON.stringify(errors));
  assert.ok(!existsSync(outDir));
  assert.ok(!existsSync(join(root, 'README.md')));
  assert.ok(!existsSync(join(dirname(files[0]), 'README.md')));
});

test('bad JSON is reported as an error, not a crash', () => {
  const { root, files } = makeRepo({ cards: [validCard(DONE)] });
  writeFileSync(files[0], '{ not json');
  const { errors } = build({ repoRoot: root, outDir: join(root, 'dist') });
  assert.ok(errors.some(e => e.includes(files[0])));
});

test('duplicate ids are an error', () => {
  const { root } = makeRepo({ cards: [validCard(DONE), validCard({ ...DONE, day: 1 })] });
  const { errors } = build({ repoRoot: root, outDir: join(root, 'dist') });
  assert.ok(errors.some(e => e.includes('W01-D0-finished') && e.includes('duplicate')), JSON.stringify(errors));
});

test('missing config is an error', () => {
  const { root } = makeRepo({ cards: [] });
  writeFileSync(join(root, 'lab.config.json'), '{ not json');
  const { errors } = build({ repoRoot: root, outDir: join(root, 'dist') });
  assert.ok(errors.some(e => e.includes('lab.config.json')));
});

test('CLI exits 1 and prints file and field on an invalid card', () => {
  const { root, files } = makeRepo({ cards: [validCard({ ...DONE, next: '' })] });
  const result = runCli(root);
  assert.equal(result.status, 1);
  assert.ok(result.stderr.includes('card.json') && result.stderr.includes('next'), result.stderr);
  assert.ok(result.stderr.includes(join('D0-finished', 'card.json')), files[0]);
});

test('CLI prints how many systems it built', () => {
  const { root } = makeRepo({ cards: [validCard(DONE)] });
  const result = runCli(root);
  assert.equal(result.status, 0, result.stderr);
  assert.ok(result.stdout.includes('Built 1 systems'));
  assert.ok(existsSync(join(root, 'site/dist/index.html')));
});
