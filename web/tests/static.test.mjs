import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const read = (path) => readFileSync(join(root, path), "utf8");
test("public PWA manifest is scoped to its repository Pages path", () => {
  const m = JSON.parse(read("manifest.webmanifest"));
  assert.equal(m.start_url, "./"); assert.equal(m.scope, "./"); assert.equal(m.display, "standalone");
  for (const icon of m.icons) assert.ok(existsSync(join(root, icon.src)));
});
test("one truthful fact graph for all audiences", () => {
  const info = JSON.parse(read("app-info.json"));
  assert.equal(info.acoustic_output, false);
  assert.equal(info.remote_control, false);
  assert.equal(info.biometric_data, false);
  assert.equal(info.treatment_claims, false);
  for (const path of ["index.html","app-info.json","mark.svg"]) assert.ok(existsSync(join(root,path)));
});
test("index remains useful without script and clearly discloses disabled sound", () => {
  const html = read("index.html");
  assert.match(html, /<main\b/i); assert.match(html, /<nav\b/i); assert.match(html, /<noscript\b/i);
  assert.match(html, /no sound output|does not play sound|Sound output unavailable/i);
  assert.match(html, /not a medical device/i);
  assert.match(html, /<fieldset class="themes">/);
  assert.match(html, /aria-live="polite"/);
  assert.match(html, /href="\.\/manifest\.webmanifest"/);
  assert.doesNotMatch(html, /<audio\b|<video\b|autoplay|https?:\/\/[^"]+\.(js|css)/i);
});
test("visual preview JavaScript cannot drive a listening session", () => {
  const code = read("app.js");
  assert.doesNotMatch(code, /AudioContext|webkitAudioContext|getUserMedia|MediaRecorder|createOscillator|navigator\.bluetooth|\/api\/|\.play\s*\(/);
  assert.match(code, /serviceWorker\.register\("\.\/sw\.js"/);
  assert.match(code, /localStorage\.setItem\("aural-preview-theme"/);
  assert.match(code, /reset/);
});
test("offline shell is restricted to local static assets", () => {
  const sw = read("sw.js");
  assert.match(sw, /url\.origin !== self\.location\.origin/);
  assert.match(sw, /request\.method !== "GET"/);
  assert.match(sw, /new URL\(self\.registration\.scope\)\.pathname/);
  assert.doesNotMatch(sw, /pushManager|sync\.register|postMessage|analytics|\/api\//i);
});
test("responsive themes, keyboard focus and reduced motion have explicit styles", () => {
  const css = read("style.css");
  for (const required of ["data-theme=\"dark\"","prefers-color-scheme:dark","max-width:360px","forced-colors:active","prefers-reduced-motion:reduce",":focus-visible"]) {
    assert.ok(css.includes(required), required);
  }
  assert.match(css, /min-height:44px/);
});