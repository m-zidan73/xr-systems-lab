import { join, relative } from 'node:path';

const GENERATED_NOTE = '<!-- Generated from card.json by site/build.mjs. Edit card.json instead. -->';
const NO_SYSTEMS = 'No finished systems yet.';

const toPosix = path => path.replace(/\\/g, '/');
const cardFolder = (card, repoRoot) => toPosix(relative(repoRoot, card._dir));
// For text placed before a full stop of ours, since the owner may have written a full sentence.
const withoutFinalStop = text => String(text).trim().replace(/\.+$/, '');

export function escapeHtml(text) {
  return String(text)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

// Markdown shows & < > literally once they are entities, and everything else passes through.
function mdText(text) {
  return String(text).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function mdCell(text) {
  return mdText(text).replace(/\|/g, '\\|').replace(/\r?\n/g, ' ');
}

function groupByWeek(cards) {
  const weeks = new Map();
  for (const card of cards) {
    if (!weeks.has(card.week)) weeks.set(card.week, []);
    weeks.get(card.week).push(card);
  }
  return [...weeks.entries()].sort(([a], [b]) => a - b);
}

const sortedByDay = cards => [...cards].sort((a, b) => a.day - b.day);
const testedOnLine = card => `Tested on: ${card.testedOn.join(', ')}`;

export function renderSystemReadme(card, { repoRoot }) {
  const gifPath = toPosix(relative(card._dir, join(repoRoot, card.media.gif)));
  const lines = [
    GENERATED_NOTE,
    `# ${mdText(card.title)}`,
    '',
    `![${mdText(card.title).replace(/\]/g, '\\]')}](${encodeURI(gifPath)})`,
    '',
    mdText(card.pitch),
    '',
    testedOnLine(card),
  ];
  if (card.media.video) lines.push('', `[Watch the video](${card.media.video})`);
  if (card.disclaimer) lines.push('', `> ${mdText(card.disclaimer)}`);
  lines.push('', '## How it works', '', mdText(card.howItWorks), '', '## My decisions', '');
  for (const decision of card.decisions) {
    lines.push(`- **Chose:** ${mdText(decision.choice)} **Instead of:** ${mdText(decision.alternative)} **Why:** ${mdText(decision.why)}`);
  }
  lines.push('', '## Next steps', '', mdText(card.next));
  if (card.assets?.length) {
    lines.push('', '## Asset credits', '');
    for (const asset of card.assets) {
      const changes = asset.changes ? ` Changes: ${mdText(asset.changes)}` : '';
      lines.push(`- **${mdText(asset.name)}:** ${mdText(withoutFinalStop(asset.source))}. Licence: ${mdText(withoutFinalStop(asset.licence))}.${changes}`);
    }
  }
  lines.push('', '---', '', mdText(card.builtWith), '');
  return lines.join('\n');
}

export function renderRootReadme(doneCards, { repoRoot, config }) {
  const links = [`[Portfolio page](${config.siteUrl})`, ...Object.entries(config.links).map(([label, url]) => `[${mdCell(label)}](${url})`)];
  const lines = [GENERATED_NOTE, `# ${mdText(config.siteTitle)}`, '', mdText(config.intro), '', links.join(' · '), ''];
  if (doneCards.length === 0) lines.push(NO_SYSTEMS, '');
  for (const [week, cards] of groupByWeek(doneCards)) {
    lines.push(`## Week ${week}: ${mdText(cards[0].weekTitle)}`, '', '| | System | What it does | Tested on |', '|---|---|---|---|');
    for (const card of sortedByDay(cards)) {
      const alt = escapeHtml(card.title).replace(/\|/g, '&#124;');
      const thumb = `<img src="${encodeURI(card.media.gif)}" alt="${alt}" width="200">`;
      const link = `[${mdCell(card.title)}](${encodeURI(cardFolder(card, repoRoot))})`;
      lines.push(`| ${thumb} | ${link} | ${mdCell(card.pitch)} | ${mdCell(card.testedOn.join(', '))} |`);
    }
    lines.push('');
  }
  return lines.join('\n');
}

function renderDecisions(card) {
  const items = card.decisions.map(d => `<li><strong>${escapeHtml(d.choice)}</strong> <span class="alternative">instead of ${escapeHtml(withoutFinalStop(d.alternative))}.</span> ${escapeHtml(d.why)}</li>`);
  return `<ul class="decisions">${items.join('')}</ul>`;
}

function renderAssets(card) {
  if (!card.assets?.length) return '';
  const items = card.assets.map(a => {
    const changes = a.changes ? ` Changes: ${escapeHtml(a.changes)}` : '';
    return `<li>${escapeHtml(a.name)}: ${escapeHtml(withoutFinalStop(a.source))}. Licence: ${escapeHtml(withoutFinalStop(a.licence))}.${changes}</li>`;
  });
  return `<details class="assets"><summary>Asset credits</summary><ul>${items.join('')}</ul></details>`;
}

function renderSystem(card, { repoRoot, config }) {
  const links = [`<a href="${escapeHtml(encodeURI(`${config.repoUrl}/tree/main/${cardFolder(card, repoRoot)}`))}">Code</a>`];
  if (card.media.video) links.push(`<a href="${escapeHtml(card.media.video)}">Watch video</a>`);
  if (card.webDemo) links.push(`<a href="${escapeHtml(encodeURI(card.webDemo))}/">Play demo</a>`);
  const disclaimer = card.disclaimer ? `<p class="disclaimer">${escapeHtml(card.disclaimer)}</p>` : '';
  return [
    '<article class="system">',
    `<img src="${escapeHtml(encodeURI(card.media.gif))}" alt="${escapeHtml(card.title)}: animated demo" loading="lazy">`,
    '<div class="body">',
    `<h3>${escapeHtml(card.title)}</h3>`,
    `<p class="pitch">${escapeHtml(card.pitch)}</p>`,
    `<p class="tested">${escapeHtml(testedOnLine(card))}</p>`,
    `<h4>How it works</h4><p>${escapeHtml(card.howItWorks)}</p>`,
    `<h4>My decisions</h4>${renderDecisions(card)}`,
    `<h4>Next steps</h4><p>${escapeHtml(card.next)}</p>`,
    disclaimer,
    renderAssets(card),
    `<p class="links">${links.join('')}</p>`,
    `<p class="credit">${escapeHtml(card.builtWith)}</p>`,
    '</div>',
    '</article>',
  ].filter(Boolean).join('\n');
}

export function renderSitePage(doneCards, options) {
  const { config, template } = options;
  const links = [`<a href="${escapeHtml(config.repoUrl)}">GitHub</a>`, ...Object.entries(config.links).map(([label, url]) => `<a href="${escapeHtml(url)}">${escapeHtml(label)}</a>`)];
  const weeks = groupByWeek(doneCards).map(([week, cards]) => [
    '<section class="week">',
    `<h2><span>Week ${week}</span> ${escapeHtml(cards[0].weekTitle)}</h2>`,
    '<div class="systems">',
    ...sortedByDay(cards).map(card => renderSystem(card, options)),
    '</div>',
    '</section>',
  ].join('\n'));
  const values = {
    title: escapeHtml(config.siteTitle),
    intro: escapeHtml(config.intro),
    links: links.join('\n'),
    weeks: weeks.length ? weeks.join('\n') : `<p class="empty">${NO_SYSTEMS}</p>`,
  };
  // One pass with a function, so card text is never read as a placeholder or a $ pattern.
  return template.replace(/\{\{(title|intro|links|weeks)\}\}/g, (_, key) => values[key]);
}
