import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';

// Execute the exact JS embedded in the C# adapter, not a translated mock.
// This validates JavaScript semantics only. It is not a Jint/Godot test.
const path = fileURLToPath(new URL('../runtime/UniversalRPG.JavaScript.Jint/ScriptInvocationBridge.cs', import.meta.url));
const source = readFileSync(path, 'utf8');
const match = source.match(/JS_BRIDGE_BEGIN[\s\S]*?Source = """\r?\n([\s\S]*?)\r?\n\s*""";[\s\S]*?JS_BRIDGE_END/);
assert.ok(match, 'the production bridge must be present');
const script = match[1];
let checks = 0;
function check(name, test) { test(); checks++; console.log(`PASS ${name}`); }
function realm() {
  const context = vm.createContext({});
  const bridge = vm.runInContext(script, context, { timeout: 1000 });
  return { context, bridge, run: text => vm.runInContext(text, context, { timeout: 1000 }) };
}

check('method retains receiver', () => {
  const { run, bridge } = realm();
  run(`globalThis.api = { value: 40, add(n) { this.value += n; return this.value; } };`);
  assert.equal(bridge('api', 'add', '[2]'), 42);
  assert.equal(run('api.value'), 42);
});
check('strict methods retain receiver', () => {
  const { run, bridge } = realm();
  run(`globalThis.api = { check: function() { 'use strict'; return this === api; } };`);
  assert.equal(bridge('api', 'check', '[]'), true);
});
check('empty/globalThis names use global receiver', () => {
  const { run, bridge } = realm();
  run(`globalThis.probe = function() { 'use strict'; return this === globalThis; };`);
  assert.equal(bridge('', 'probe', '[]'), true);
  assert.equal(bridge('globalThis', 'probe', '[]'), true);
});
check('property getter executes once with correct receiver', () => {
  const { run, bridge } = realm();
  run(`globalThis.calls = 0; globalThis.api = { get method() { calls++; return function() { return this === api; }; } };`);
  assert.equal(bridge('api', 'method', '[]'), true);
  assert.equal(run('calls'), 1);
});
check('member strings are keys, not executable expressions', () => {
  const { run, bridge } = realm();
  run(`globalThis.executed = false; globalThis.api = { ['x); executed = true; //']: function() { return 7; } };`);
  assert.equal(bridge('api', 'x); executed = true; //', '[]'), 7);
  assert.equal(run('executed'), false);
});
check('target strings are exact global keys', () => {
  const { run, bridge } = realm();
  run(`globalThis['a.b'] = { value: 9, read() { return this.value; } };`);
  assert.equal(bridge('a.b', 'read', '[]'), 9);
});
check('primitive argument values and order survive', () => {
  const { run, bridge } = realm();
  run(`globalThis.probe = (...args) => JSON.stringify(args);`);
  const data = [null, true, false, 42, -7, 0.5, '日本語', 'quote"\\\n'];
  assert.equal(bridge('', 'probe', JSON.stringify(data)), JSON.stringify(data));
});
check('captured intrinsics survive plugin replacements', () => {
  const { run, bridge } = realm();
  run(`globalThis.api = { n: 6, times(x) { return this.n * x; } }; JSON.parse = () => { throw Error('replaced'); }; Reflect.apply = () => { throw Error('replaced'); };`);
  assert.equal(bridge('api', 'times', '[7]'), 42);
});
check('replacement globalThis property cannot redirect bridge', () => {
  const { run, bridge } = realm();
  run(`globalThis.probe = () => 42; globalThis.globalThis = { probe: () => -1 };`);
  assert.equal(bridge('globalThis', 'probe', '[]'), 42);
});
check('missing method raises an error', () => {
  const { bridge } = realm();
  assert.throws(() => bridge('', 'missing', '[]'), /not a function|Function|function/i);
});
check('throwing getter is propagated', () => {
  const { run, bridge } = realm();
  run(`globalThis.api = { get method() { throw Error('getter failed'); } };`);
  assert.throws(() => bridge('api', 'method', '[]'), /getter failed/);
});
check('separate realms do not share plugin state', () => {
  const first = realm(); const second = realm();
  first.run(`globalThis.probe = () => 1;`);
  second.run(`globalThis.probe = () => 2;`);
  assert.equal(first.bridge('', 'probe', '[]'), 1);
  assert.equal(second.bridge('', 'probe', '[]'), 2);
});
console.log(`JavaScript invocation bridge: ${checks}/${checks} passed (Node; not Jint/Godot).`);
