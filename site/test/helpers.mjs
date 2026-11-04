import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';

const tempRoots = [];
process.on('exit', () => {
  for (const root of tempRoots) rmSync(root, { recursive: true, force: true });
});

export const CONFIG = {
  owner: 'Test Owner',
  siteTitle: 'Test Lab',
  intro: 'A lab used by the tests.',
  repoUrl: 'https://github.com/test/lab',
  siteUrl: 'https://test.github.io/lab/',
  links: { LinkedIn: 'https://example.com/in', Behance: 'https://example.com/be' },
};

export function validCard(overrides = {}) {
  return {
    id: 'W01-D0-test-system',
    week: 1,
    weekTitle: 'Test week',
    day: 0,
    date: '2026-11-04',
    status: 'done',
    title: 'Test System',
    pitch: 'A system used by the tests.',
    tags: ['Unity'],
    shows: ['Testing'],
    testedOn: ['PC'],
    howItWorks: 'It works like this.',
    decisions: [{ choice: 'This', alternative: 'That', why: 'Because' }],
    next: 'More of it.',
    media: { gif: 'media/W01-D0-test-system.gif', video: '' },
    webDemo: '',
    builtWith: 'Built with Claude Code. Design decisions and explanation: Mohitur Rahman Zidan.',
    assets: [],
    ...overrides,
  };
}

export function emptyRepo() {
  const root = mkdtempSync(join(tmpdir(), 'lab-'));
  tempRoots.push(root);
  writeFileSync(join(root, 'lab.config.json'), JSON.stringify(CONFIG));
  return root;
}

export function writeCard(root, card, folder = 'unity/Assets/Lab/W01-Test') {
  const slug = String(card.id).split('-').slice(2).join('-');
  const dir = join(root, folder, `D${card.day}-${slug}`);
  mkdirSync(dir, { recursive: true });
  const file = join(dir, 'card.json');
  writeFileSync(file, JSON.stringify(card, null, 2));
  return file;
}

export function makeRepo({ cards = [validCard()], gifBytes = 10 } = {}) {
  const root = emptyRepo();
  const files = cards.map(card => {
    const file = writeCard(root, card);
    const gif = card.media?.gif;
    if (gif && gifBytes !== null) {
      const path = join(root, gif);
      mkdirSync(dirname(path), { recursive: true });
      writeFileSync(path, Buffer.alloc(gifBytes));
    }
    return file;
  });
  return { root, files };
}
