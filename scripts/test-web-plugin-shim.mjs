import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';

// The JS is extracted from production C#. This validates its ECMAScript
// behavior, not the C# generator or the Jint/Godot integration.
const path = fileURLToPath(new URL('../project/src/plugins/WebPluginManagerShimBuilder.cs', import.meta.url));
const text = readFileSync(path, 'utf8');
const match = text.match(/JS_PLUGIN_MANAGER_BEGIN[\s\S]*?FactorySource = """\r?\n([\s\S]*?)\r?\n\s*""";[\s\S]*?JS_PLUGIN_MANAGER_END/);
assert.ok(match, 'production PluginManager factory is present');
let checks = 0;
function check(name, test) { test(); checks++; console.log(`PASS ${name}`); }
function realm(json = '{}', mz = true) {
  const context = vm.createContext({});
  vm.runInContext(`${match[1]}(JSON.parse(${JSON.stringify(json)}), ${mz});`, context, { timeout: 1000 });
  return code => vm.runInContext(code, context, { timeout: 1000 });
}
check('parameter names are case-insensitive; values stay strings', () => {
  const run = realm('{"example":{"Rate":"42","Enabled":"false","Struct":"{\\"x\\":1}"}}');
  assert.equal(run('PluginManager.parameters("EXAMPLE").Rate'), '42');
  assert.equal(run('PluginManager.parameters("Example").Enabled'), 'false');
  assert.equal(run('PluginManager.parameters("example").Struct'), '{"x":1}');
});
check('special plugin and parameter keys remain own data', () => {
  const run = realm('{"__proto__":{"Note":"kept","__proto__":"value"},"constructor":{"Name":"also kept"}}');
  assert.equal(run('PluginManager.parameters("__proto__").Note'), 'kept');
  assert.equal(run('PluginManager.parameters("__proto__").__proto__'), 'value');
  assert.equal(run('PluginManager.parameters("constructor").Name'), 'also kept');
  assert.equal(run('Object.getPrototypeOf(PluginManager._parameters) === null'), true);
});
check('quotes and code-like values never execute', () => {
  const data = { sample: { Text: `'}); globalThis.injected = true; //` } };
  const run = realm(JSON.stringify(data));
  assert.equal(run('PluginManager.parameters("sample").Text'), data.sample.Text);
  assert.equal(run('typeof injected'), 'undefined');
});
check('MZ callbacks retain interpreter receiver', () => {
  const run = realm();
  run(`globalThis.owner = { value: 40 }; PluginManager.registerCommand('A','Add',function(args){ this.value += Number(args.n); }); PluginManager.callCommand(owner,'A','Add',{n:'2'});`);
  assert.equal(run('owner.value'), 42);
});
check('MZ callbacks preserve falsy arguments', () => {
  const run = realm();
  run(`globalThis.seen = []; PluginManager.registerCommand('A','Probe',function(args){seen.push(args);});`);
  run(`for (const value of [null, false, 0, '']) PluginManager.callCommand(null,'A','Probe',value);`);
  assert.equal(run('JSON.stringify(seen)'), '[null,false,0,""]');
});
check('missing commands do not invoke another command', () => {
  const run = realm();
  run(`globalThis.calls = 0; PluginManager.registerCommand('A','Run',()=>{calls++;}); PluginManager.callCommand(null,'B','Run',{});`);
  assert.equal(run('calls'), 0);
});
check('later command registration replaces earlier value', () => {
  const run = realm();
  run(`globalThis.calls = 0; PluginManager.registerCommand('A','Run',()=>{calls++;}); PluginManager.registerCommand('A','Run',null); PluginManager.callCommand(null,'A','Run',{});`);
  assert.equal(run('calls'), 0);
});
check('MV shim does not claim MZ command registration', () => {
  const run = realm('{}', false);
  assert.equal(run('typeof PluginManager.parameters'), 'function');
  assert.equal(run('typeof PluginManager.registerCommand'), 'undefined');
});
console.log(`PluginManager JavaScript: ${checks}/${checks} passed (Node; not Jint/Godot).`);
