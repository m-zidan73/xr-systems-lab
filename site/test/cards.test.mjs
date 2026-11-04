import assert from 'node:assert/strict';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { test } from 'node:test';
import { CREDIT_SUFFIX, findCardFiles, loadCard, MAX_GIF_BYTES, sortCards, TESTED_ON, validateCard } from '../cards.mjs';
import { emptyRepo, makeRepo, validCard, writeCard } from './helpers.mjs';

const errorsFor = (overrides, options = {}) => {
  const { root, files } = makeRepo({ cards: [validCard(overrides)], ...options });
  return validateCard(loadCard(files[0]), root);
};
const assertFails = (overrides, field, options) => {
  const errors = errorsFor(overrides, options);
  assert.ok(errors.some(m => m.includes(field)), `${field}: ${JSON.stringify(errors)}`);
};

test('constants match the spec', () => {
  assert.equal(MAX_GIF_BYTES, 5242880);
  assert.deepEqual(TESTED_ON, ['PC', 'XR simulator (no headset)', 'Android phone', 'browser', 'headset']);
  assert.equal(CREDIT_SUFFIX, 'Design decisions and explanation: Mohitur Rahman Zidan.');
});

test('valid done card has no errors', () => {
  assert.deepEqual(errorsFor({}), []);
});

test('blank owner text fails, naming the field', () => {
  for (const [field, value] of [
    ['howItWorks', '   '],
    ['next', ''],
    ['decisions', []],
    ['decisions', [{ choice: 'a', alternative: '', why: 'b' }]],
  ]) {
    assertFails({ [field]: value }, field);
  }
});

test('missing owner text fails instead of throwing', () => {
  const card = validCard();
  delete card.howItWorks;
  delete card.decisions;
  delete card.media;
  const { root, files } = makeRepo({ cards: [card] });
  const errors = validateCard(loadCard(files[0]), root);
  for (const field of ['howItWorks', 'decisions', 'media.gif']) assert.ok(errors.some(m => m.includes(field)), field);
});

test('wip card may leave owner text blank', () => {
  const overrides = { status: 'wip', howItWorks: '', decisions: [], next: '', media: { gif: '', video: '' } };
  assert.deepEqual(errorsFor(overrides), []);
});

test('every card needs its identity and summary', () => {
  for (const status of ['wip', 'done']) {
    for (const [field, value] of [
      ['id', 'W1-D0-x'],
      ['id', 'W01-D0-Upper'],
      ['week', 0],
      ['week', '1'],
      ['day', 6],
      ['day', 1.5],
      ['weekTitle', ' '],
      ['title', ''],
      ['pitch', '  '],
    ]) {
      assertFails({ status, [field]: value }, field);
    }
  }
  assertFails({ status: 'draft' }, 'status');
});

test('done card needs a real date', () => {
  for (const date of ['', '4 Nov 2026', '2026-13-40', '2026-02-30']) assertFails({ date }, 'date');
});

test('strips a BOM', () => {
  const { files } = makeRepo();
  writeFileSync(files[0], '﻿' + JSON.stringify(validCard()));
  assert.equal(loadCard(files[0]).id, 'W01-D0-test-system');
});

test('loadCard records its file and folder', () => {
  const { files } = makeRepo();
  const card = loadCard(files[0]);
  assert.equal(card._file, files[0]);
  assert.ok(files[0].startsWith(card._dir));
});

test('bad JSON error names the file', () => {
  const { files } = makeRepo();
  writeFileSync(files[0], '{ not json');
  assert.throws(() => loadCard(files[0]), err => err.message.startsWith(files[0]));
});

test('testedOn must be known and non-empty', () => {
  assertFails({ testedOn: ['Quest'] }, 'testedOn');
  assertFails({ testedOn: [] }, 'testedOn');
  assert.deepEqual(errorsFor({ testedOn: ['XR simulator (no headset)'] }), []);
});

test('gif must exist and be at most 5 MB', () => {
  assertFails({}, 'media.gif', { gifBytes: null });
  assertFails({}, 'media.gif', { gifBytes: MAX_GIF_BYTES + 1 });
  assert.deepEqual(errorsFor({}, { gifBytes: MAX_GIF_BYTES }), []);
});

test('gif must live under media', () => {
  assertFails({ media: { gif: 'other/a.gif', video: '' } }, 'media.gif');
  assertFails({ media: { gif: 'media/../other/a.gif', video: '' } }, 'media.gif');
});

test('video must be https or blank', () => {
  const media = video => ({ gif: 'media/W01-D0-test-system.gif', video });
  assertFails({ media: media('http://x') }, 'media.video');
  assert.deepEqual(errorsFor({ media: media('') }), []);
  assert.deepEqual(errorsFor({ media: media('https://x') }), []);
});

test('webDemo must be blank or an existing folder under web', () => {
  assertFails({ webDemo: 'web/missing' }, 'webDemo');
  const { root, files } = makeRepo({ cards: [validCard({ webDemo: 'web/demo' })] });
  mkdirSync(join(root, 'web/demo'), { recursive: true });
  assert.deepEqual(validateCard(loadCard(files[0]), root), []);
});

test('builtWith must end with the credit suffix', () => {
  assertFails({ builtWith: 'Built with AI.' }, 'builtWith');
});

test('assets need name, source and licence', () => {
  assertFails({ assets: [{ name: 'x' }] }, 'assets');
  assert.deepEqual(errorsFor({ assets: [{ name: 'x', source: 'y', licence: 'z', changes: '' }] }), []);
});

test('sorts numerically by week then day', () => {
  const cards = [{ week: 10, day: 0 }, { week: 2, day: 3 }, { week: 2, day: 0 }];
  const sorted = sortCards(cards);
  assert.deepEqual(sorted.map(c => [c.week, c.day]), [[2, 0], [2, 3], [10, 0]]);
  assert.equal(cards[0].week, 10);
});

test('finds cards in unity and web, tolerates missing folders', () => {
  const root = emptyRepo();
  assert.deepEqual(findCardFiles(root), []);
  const a = writeCard(root, validCard());
  const b = writeCard(root, validCard(), 'web');
  assert.deepEqual(findCardFiles(root), [a, b].sort());
});
