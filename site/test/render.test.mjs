import assert from 'node:assert/strict';
import { test } from 'node:test';
import { CREDIT_SUFFIX, loadCard } from '../cards.mjs';
import { escapeHtml, renderRootReadme, renderSitePage, renderSystemReadme } from '../render.mjs';
import { CONFIG, makeRepo, validCard } from './helpers.mjs';

const TEMPLATE = '<h1>{{title}}</h1><p>{{intro}}</p><nav>{{links}}</nav><main>{{weeks}}</main>';
const X_CARD = { id: 'W01-D0-x', media: { gif: 'media/W01-D0-x.gif', video: '' } };

function setup(overrides = {}) {
  const { root, files } = makeRepo({ cards: [validCard({ ...X_CARD, ...overrides })] });
  return { root, card: loadCard(files[0]), options: { repoRoot: root, config: CONFIG, template: TEMPLATE } };
}

test('escapeHtml covers markup and quotes', () => {
  assert.equal(escapeHtml(`<a href="x">&'`), '&lt;a href=&quot;x&quot;&gt;&amp;&#39;');
});

test('system README has GIF, tested-on, owner text and credit', () => {
  const { card, options } = setup();
  const md = renderSystemReadme(card, options);
  assert.match(md, /!\[.*\]\(\.\.\/\.\.\/\.\.\/\.\.\/\.\.\/media\/W01-D0-x\.gif\)/);
  for (const text of ['## How it works', 'It works like this.', '## My decisions', 'This', 'That', 'Because', '## Next steps', 'More of it.', 'Tested on: PC', CREDIT_SUFFIX]) {
    assert.ok(md.includes(text), text);
  }
});

test('system README shows disclaimer and asset credits when present', () => {
  const plain = setup();
  const plainMd = renderSystemReadme(plain.card, plain.options);
  assert.ok(!plainMd.includes('Asset credits'));
  const { card, options } = setup({
    disclaimer: 'Illustrative only.',
    assets: [{ name: 'Model', source: 'Own work', licence: 'CC-BY-4.0', changes: 'Reduced' }],
  });
  const md = renderSystemReadme(card, options);
  for (const text of ['Illustrative only.', 'Asset credits', 'Model', 'Own work', 'CC-BY-4.0', 'Reduced']) assert.ok(md.includes(text), text);
});

test('system README keeps symbols and cannot inject HTML', () => {
  const { card, options } = setup({ howItWorks: 'Ø 3.5 mm × 26 mm <b>' });
  const md = renderSystemReadme(card, options);
  assert.ok(md.includes('Ø 3.5 mm × 26 mm &lt;b&gt;'));
  assert.ok(!md.includes('<b>'));
});

test('text written as full sentences does not double its full stop', () => {
  const { card, options } = setup({
    decisions: [{ choice: 'An index slider.', alternative: 'A float slider.', why: 'Only real sizes exist.' }],
    assets: [{ name: 'Model', source: 'Made in Blender.', licence: 'Own work.', changes: '' }],
  });
  for (const output of [renderSitePage([card], options), renderSystemReadme(card, options)]) {
    assert.ok(!output.includes('..') || output.includes('../'), output);
    assert.ok(!/[a-z]\.\./.test(output), output);
  }
  assert.ok(renderSitePage([card], options).includes('instead of A float slider.'));
});

test('root README groups by week and escapes pipes', () => {
  const { card, options } = setup({ weekTitle: 'Kit', pitch: 'a | b' });
  const md = renderRootReadme([card], options);
  assert.ok(md.includes('## Week 1: Kit'));
  assert.ok(md.includes('a \\| b'));
  assert.ok(md.includes('media/W01-D0-x.gif'));
  assert.ok(md.includes('unity/Assets/Lab/W01-Test/D0-x'));
});

test('root README says when nothing is finished', () => {
  const { options } = setup();
  assert.ok(renderRootReadme([], options).includes('No finished systems yet.'));
});

test('site page escapes HTML and keeps non-ASCII', () => {
  const { card, options } = setup({ title: 'Ø 3.5 mm × 26 mm <b>' });
  const html = renderSitePage([card], options);
  assert.ok(html.includes('Ø 3.5 mm × 26 mm &lt;b&gt;'));
  assert.ok(!html.includes('<b>'));
});

test('card text is not read as a template placeholder', () => {
  const { card, options } = setup({ pitch: 'literal {{intro}} and $& here' });
  const html = renderSitePage([card], options);
  assert.ok(html.includes('literal {{intro}} and $&amp; here'));
});

test('site page fills title, intro and links', () => {
  const { card, options } = setup();
  const html = renderSitePage([card], options);
  for (const text of [CONFIG.siteTitle, CONFIG.intro, CONFIG.repoUrl, CONFIG.links.Behance, 'Behance']) assert.ok(html.includes(text), text);
});

test('no play link without webDemo; play link with it', () => {
  const without = setup();
  assert.ok(!renderSitePage([without.card], without.options).includes('Play demo'));
  const { card, options } = setup({ webDemo: 'web/demo' });
  const html = renderSitePage([card], options);
  assert.ok(html.includes('Play demo'));
  assert.ok(html.includes('href="web/demo/"'));
});

test('site page links code and video when present', () => {
  const without = setup();
  assert.ok(!renderSitePage([without.card], without.options).includes('Watch video'));
  const { card, options } = setup({ media: { gif: 'media/W01-D0-x.gif', video: 'https://example.com/v' } });
  const html = renderSitePage([card], options);
  assert.ok(html.includes(`${CONFIG.repoUrl}/tree/main/unity/Assets/Lab/W01-Test/D0-x`));
  assert.ok(html.includes('href="https://example.com/v"'));
});

test('site page says what it was tested on and credits the owner', () => {
  const { card, options } = setup({ disclaimer: 'Illustrative only.' });
  const html = renderSitePage([card], options);
  for (const text of ['Tested on: PC', 'It works like this.', 'Because', 'More of it.', 'Illustrative only.', CREDIT_SUFFIX]) assert.ok(html.includes(text), text);
});

test('weeks appear in order', () => {
  const { root, files } = makeRepo({
    cards: [
      validCard({ id: 'W02-D0-b', week: 2, weekTitle: 'Second', media: { gif: 'media/b.gif', video: '' } }),
      validCard({ id: 'W01-D0-a', week: 1, weekTitle: 'First', media: { gif: 'media/a.gif', video: '' } }),
    ],
  });
  const cards = files.map(f => loadCard(f));
  const html = renderSitePage(cards, { repoRoot: root, config: CONFIG, template: TEMPLATE });
  assert.ok(html.indexOf('First') < html.indexOf('Second'));
});
