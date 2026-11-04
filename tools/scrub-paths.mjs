// Blanks local user-folder paths stored inside exported binaries, keeping each file's length and layout.
// Blender writes them on export and on save. Run it before staging an .fbx or a .blend.
// Usage: node tools/scrub-paths.mjs <file> [more files]
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { extname } from 'node:path';

const USER_PATH = /[A-Za-z]:[\\/]+users[\\/]+[^\x00-\x1f]*/gi;
const SPACE = 0x20;
const NUL = 0x00;

const files = process.argv.slice(2);
if (files.length === 0) {
  console.error('Usage: node tools/scrub-paths.mjs <file> [more files]');
  process.exit(1);
}

let failed = false;
for (const file of files) {
  if (!existsSync(file)) {
    console.error(`File not found: ${file}`);
    failed = true;
    continue;
  }
  const bytes = readFileSync(file);
  const isBlend = extname(file).toLowerCase() === '.blend';
  if (isBlend && bytes.subarray(0, 7).toString('latin1') !== 'BLENDER') {
    console.error(`${file} is saved compressed. Save it in Blender with Compress off, then run this again.`);
    failed = true;
    continue;
  }

  // Latin-1 maps each byte to one character, so match positions are byte positions.
  // A .blend holds these in fixed-size text fields ended by NUL; an FBX holds them in length-prefixed strings.
  const matches = [...bytes.toString('latin1').matchAll(USER_PATH)];
  for (const match of matches) bytes.fill(isBlend ? NUL : SPACE, match.index, match.index + match[0].length);
  if (matches.length > 0) writeFileSync(file, bytes);
  console.log(matches.length > 0 ? `${file}: ${matches.length} path(s) blanked` : `${file}: no paths found`);
}
process.exit(failed ? 1 : 0);
