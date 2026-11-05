import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { findCardFiles, loadCard, sortCards, validateCard } from './cards.mjs';
import { renderRootReadme, renderSitePage, renderSystemReadme } from './render.mjs';

const siteDir = dirname(fileURLToPath(import.meta.url));

function loadConfig(repoRoot) {
  const path = join(repoRoot, 'lab.config.json');
  try {
    return { config: JSON.parse(readFileSync(path, 'utf8').replace(/^﻿/, '')), errors: [] };
  } catch (err) {
    return { config: null, errors: [`${path}: ${err.message}`] };
  }
}

function readUnityVersion(repoRoot) {
  const path = join(repoRoot, 'unity', 'ProjectSettings', 'ProjectVersion.txt');
  if (!existsSync(path)) return null;
  return readFileSync(path, 'utf8').match(/^m_EditorVersion:\s*(\S+)/m)?.[1] ?? null;
}

function loadAndCheckCards(repoRoot) {
  const errors = [];
  const cards = [];
  for (const file of findCardFiles(repoRoot)) {
    try {
      const card = loadCard(file);
      cards.push(card);
      for (const message of validateCard(card, repoRoot)) errors.push(`${file}: ${message}`);
    } catch (err) {
      errors.push(err.message);
    }
  }
  const firstFileForId = new Map();
  for (const card of cards) {
    if (firstFileForId.has(card.id)) {
      errors.push(`${card._file}: duplicate id ${card.id}, also used by ${firstFileForId.get(card.id)}`);
    } else {
      firstFileForId.set(card.id, card._file);
    }
  }
  return { cards, errors };
}

function copyInto(repoRoot, outDir, path) {
  const target = join(outDir, path);
  mkdirSync(dirname(target), { recursive: true });
  cpSync(join(repoRoot, path), target, { recursive: true });
}

export function build({ repoRoot, outDir }) {
  const loadedConfig = loadConfig(repoRoot);
  const loaded = loadAndCheckCards(repoRoot);
  const errors = [...loadedConfig.errors, ...loaded.errors];
  const doneCards = sortCards(loaded.cards.filter(card => card.status === 'done'));
  if (errors.length > 0) return { cards: doneCards, errors };

  const template = readFileSync(join(siteDir, 'template.html'), 'utf8');
  const options = { repoRoot, config: loadedConfig.config, template, unityVersion: readUnityVersion(repoRoot) };

  for (const card of doneCards) {
    writeFileSync(join(card._dir, 'README.md'), renderSystemReadme(card, options));
  }
  writeFileSync(join(repoRoot, 'README.md'), renderRootReadme(doneCards, options));

  rmSync(outDir, { recursive: true, force: true });
  mkdirSync(outDir, { recursive: true });
  writeFileSync(join(outDir, 'index.html'), renderSitePage(doneCards, options));
  for (const card of doneCards) {
    copyInto(repoRoot, outDir, card.media.gif);
    if (card.webDemo) copyInto(repoRoot, outDir, card.webDemo);
  }
  if (doneCards.some(card => card.webDemo) && existsSync(join(repoRoot, 'web/_vendor'))) {
    copyInto(repoRoot, outDir, 'web/_vendor');
  }
  return { cards: doneCards, errors };
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const repoRoot = dirname(siteDir);
  const { cards, errors } = build({ repoRoot, outDir: join(siteDir, 'dist') });
  if (errors.length > 0) {
    for (const message of errors) console.error(message);
    process.exit(1);
  }
  console.log(`Built ${cards.length} systems`);
}
