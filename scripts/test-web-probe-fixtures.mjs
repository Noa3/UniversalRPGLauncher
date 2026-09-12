import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

function factory(path, marker) {
  const text = readFileSync(new URL(path, import.meta.url), 'utf8');
  const regex = new RegExp(`${marker}_BEGIN[\\s\\S]*?FactorySource = """\\r?\\n([\\s\\S]*?)\\r?\\n\\s*""";[\\s\\S]*?${marker}_END`);
  const match = text.match(regex);
  assert.ok(match, `${marker}: production constant missing`);
  return match[1];
}
const browser = factory('../project/src/plugins/WebBrowserHostPrelude.cs', 'JS_BROWSER_HOST');
const manager = factory('../project/src/plugins/WebPluginManagerShimBuilder.cs', 'JS_PLUGIN_MANAGER');
let passed = 0;
for (const engine of ['mv', 'mz']) {
  const context = vm.createContext({});
  const run = source => vm.runInContext(source, context, { timeout: 1000 });
  run(`${browser}({timers:16,frames:16,callbacks:32,arguments:32,delta:1000});`);
  const root = new URL(`../project/tests/fixtures/web-probe/${engine}/`, import.meta.url);
  run(readFileSync(new URL('js/plugins.js', root), 'utf8'));
  const entries = JSON.parse(run('JSON.stringify($plugins)'));
  const parameters = Object.create(null);
  for (const entry of entries) if (entry.status) parameters[entry.name.toLowerCase()] = entry.parameters;
  run(`${manager}(JSON.parse(${JSON.stringify(JSON.stringify(parameters))}), ${engine === 'mz'}, ['TimingProbe']);`);
  for (const entry of entries) {
    if (!entry.status) continue;
    run(`__urpgBrowserHost.enterScript(${JSON.stringify('urpg://game/js/plugins/' + encodeURIComponent(entry.name) + '.js')});`);
    run(readFileSync(new URL('js/plugins/' + entry.name + '.js', root), 'utf8'));
    run('__urpgBrowserHost.leaveScript();');
  }
  assert.equal(run('typeof URPG_TIMING_PROBE_PASSED'), 'undefined', 'scheduling is not execution');
  run('__urpgBrowserHost.advance(16);');
  assert.equal(run('URPG_TIMING_PROBE_PASSED'), true);
  assert.equal(run('document.currentScript'), null);
  passed++;
  console.log(`PASS synthetic ${engine.toUpperCase()} plugin fixture`);
}
console.log(`MV/MZ synthetic probe fixtures: ${passed}/${passed} passed (Node; not Godot probe or full games).`);
