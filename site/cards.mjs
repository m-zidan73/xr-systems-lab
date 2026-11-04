import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, posix, resolve } from 'node:path';

export const MAX_GIF_BYTES = 5 * 1024 * 1024;
export const TESTED_ON = ['PC', 'XR simulator (no headset)', 'Android phone', 'browser', 'headset'];
export const CREDIT_SUFFIX = 'Design decisions and explanation: Mohitur Rahman Zidan.';

const ID_PATTERN = /^W\d{2}-D\d-[a-z0-9-]+$/;
const isBlank = value => typeof value !== 'string' || value.trim() === '';

function walkForCards(dir, found) {
  if (!existsSync(dir)) return;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) walkForCards(path, found);
    else if (entry.name === 'card.json') found.push(path);
  }
}

export function findCardFiles(repoRoot) {
  const found = [];
  walkForCards(join(repoRoot, 'unity', 'Assets', 'Lab'), found);
  walkForCards(join(repoRoot, 'web'), found);
  return found.sort();
}

export function loadCard(filePath) {
  try {
    const text = readFileSync(filePath, 'utf8').replace(/^﻿/, '');
    const card = JSON.parse(text);
    if (card === null || typeof card !== 'object' || Array.isArray(card)) throw new Error('expected a JSON object');
    return { ...card, _file: filePath, _dir: dirname(resolve(filePath)) };
  } catch (err) {
    throw new Error(`${filePath}: ${err.message}`);
  }
}

function isRealDate(value) {
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const parsed = new Date(`${value}T00:00:00Z`);
  return !Number.isNaN(parsed.getTime()) && parsed.toISOString().slice(0, 10) === value;
}

function isUnder(folder, path) {
  return typeof path === 'string' && path.startsWith(`${folder}/`) && posix.normalize(path) === path;
}

function validateIdentity(card) {
  const errors = [];
  if (typeof card.id !== 'string' || !ID_PATTERN.test(card.id)) errors.push('id must look like W01-D0-some-name');
  if (!Number.isInteger(card.week) || card.week < 1) errors.push('week must be a whole number of 1 or more');
  if (!Number.isInteger(card.day) || card.day < 0 || card.day > 5) errors.push('day must be a whole number from 0 to 5');
  if (card.status !== 'wip' && card.status !== 'done') errors.push('status must be wip or done');
  for (const field of ['weekTitle', 'title', 'pitch']) {
    if (isBlank(card[field])) errors.push(`${field} must not be blank`);
  }
  return errors;
}

function validateDone(card, repoRoot) {
  const errors = [];
  if (!isRealDate(card.date)) errors.push('date must be a real YYYY-MM-DD date');

  if (!Array.isArray(card.testedOn) || card.testedOn.length === 0) {
    errors.push('testedOn must list what the system was tested on');
  } else {
    for (const value of card.testedOn) {
      if (!TESTED_ON.includes(value)) errors.push(`testedOn has an unknown value: ${value}`);
    }
  }

  for (const field of ['howItWorks', 'next']) {
    if (isBlank(card[field])) errors.push(`${field} must not be blank`);
  }

  if (!Array.isArray(card.decisions) || card.decisions.length === 0) {
    errors.push('decisions must have at least one entry');
  } else {
    card.decisions.forEach((decision, index) => {
      for (const part of ['choice', 'alternative', 'why']) {
        if (isBlank(decision?.[part])) errors.push(`decisions[${index}].${part} must not be blank`);
      }
    });
  }

  const gif = card.media?.gif;
  if (!isUnder('media', gif)) {
    errors.push('media.gif must be a path under media/');
  } else {
    const path = join(repoRoot, gif);
    if (!existsSync(path)) errors.push(`media.gif not found: ${gif}`);
    else if (statSync(path).size > MAX_GIF_BYTES) errors.push(`media.gif is over 5 MB: ${gif}`);
  }

  const video = card.media?.video;
  if (video !== undefined && !isBlank(video) && !String(video).startsWith('https://')) {
    errors.push('media.video must be blank or start with https://');
  }

  const webDemo = card.webDemo;
  if (webDemo !== undefined && !isBlank(webDemo)) {
    const path = isUnder('web', webDemo) ? join(repoRoot, webDemo) : null;
    if (path === null || !existsSync(path) || !statSync(path).isDirectory()) {
      errors.push(`webDemo must be an existing folder under web/: ${webDemo}`);
    }
  }

  if (typeof card.builtWith !== 'string' || !card.builtWith.endsWith(CREDIT_SUFFIX)) {
    errors.push(`builtWith must end with: ${CREDIT_SUFFIX}`);
  }

  if (card.assets !== undefined && !Array.isArray(card.assets)) {
    errors.push('assets must be a list');
  } else {
    (card.assets ?? []).forEach((asset, index) => {
      for (const part of ['name', 'source', 'licence']) {
        if (isBlank(asset?.[part])) errors.push(`assets[${index}].${part} must not be blank`);
      }
    });
  }
  return errors;
}

export function validateCard(card, repoRoot) {
  const errors = validateIdentity(card);
  if (card.status === 'done') errors.push(...validateDone(card, repoRoot));
  return errors;
}

export function sortCards(cards) {
  return [...cards].sort((a, b) => a.week - b.week || a.day - b.day);
}
