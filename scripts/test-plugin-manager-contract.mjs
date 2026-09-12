import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';

// Execute the production JavaScript constant, not a separate mock. This suite
// does NOT prove C# compilation, Jint integration or complete MV/MZ execution.
const path = fileURLToPath(new URL('../project/src/plugins/WebPluginManagerShimBuilder.cs', import.meta.url));
const source = readFileSync(path, 'utf8');
const match = source.match(/JS_PLUGIN_MANAGER_BEGIN[\s\S]*?FactorySource = """\r?\n([\s\S]*?)\r?\n\s*""";[\s\S]*?JS_PLUGIN_MANAGER_END/);
assert.ok(match, 'production PluginManager factory exists');
let checks = 0;
function check(label, test) {
  test(); checks++;
  console.log(`PASS ${label}`);
}
function realm({ table = {}, names = [], mz = true } = {}) {
  const context = vm.createContext({});
  const run = text => vm.runInContext(text, context, { timeout: 1000 });
  run(`globalThis.scheduled = JSON.parse(${JSON.stringify(JSON.stringify(names))});`);
  const tableJson = typeof table === 'string' ? table : JSON.stringify(table);
  run(`${match[1]}(JSON.parse(${JSON.stringify(tableJson)}), ${mz}, scheduled);`);
  return run;
}

check('setParameters supports case-insensitive reads and object identity', () => {
  const run = realm();
  run(`globalThis.input = { Rate: '42' }; PluginManager.setParameters('Example', input);`);
  assert.equal(run(`PluginManager.parameters('EXAMPLE') === input`), true);
  assert.equal(run(`PluginManager.parameters('example').Rate`), '42');
});
check('setParameters replacement is visible to later plugins', () => {
  const run = realm({ table: { example: { Version: 'first' } } });
  run(`PluginManager.setParameters('EXAMPLE', { Version: 'second' });`);
  assert.equal(run(`PluginManager.parameters('example').Version`), 'second');
});
check('parameter mutation is shared within the JavaScript realm', () => {
  const run = realm({ table: { example: { Count: '1' } } });
  run(`PluginManager.parameters('Example').Count = '2';`);
  assert.equal(run(`PluginManager.parameters('EXAMPLE').Count`), '2');
});
check('setParameters safely accepts prototype-named plugin keys', () => {
  const run = realm();
  run(`PluginManager.setParameters('__proto__', { Note: 'safe' }); PluginManager.setParameters('constructor', { Value: '42' });`);
  assert.equal(run(`PluginManager.parameters('__PROTO__').Note`), 'safe');
  assert.equal(run(`PluginManager.parameters('constructor').Value`), '42');
  assert.equal(run('Object.getPrototypeOf(PluginManager._parameters) === null'), true);
});
check('raw parameter __proto__ stays own data', () => {
  const run = realm({ table: '{"test":{"__proto__":"string data"}}' });
  assert.equal(run(`Object.hasOwn(PluginManager.parameters('TEST'), '__proto__')`), true);
  assert.equal(run(`PluginManager.parameters('test').__proto__`), 'string data');
});
check('plugin name whitespace is preserved', () => {
  const run = realm();
  run(`PluginManager.setParameters(' Name ', { Enabled: 'true' });`);
  assert.equal(run(`PluginManager.parameters(' NAME ').Enabled`), 'true');
  assert.equal(run(`PluginManager.parameters('name').Enabled`), undefined);
});
check('unknown inherited keys do not resolve to prototype properties', () => {
  const run = realm();
  assert.equal(run(`JSON.stringify(PluginManager.parameters('constructor'))`), '{}');
  assert.equal(run(`JSON.stringify(PluginManager.parameters('__proto__'))`), '{}');
});
check('unknown parameter object is not shared between lookups', () => {
  const run = realm();
  assert.equal(run(`PluginManager.parameters('missing') === PluginManager.parameters('missing')`), false);
});
check('null or non-string plugin names are rejected rather than renamed', () => {
  const run = realm();
  assert.throws(() => run(`PluginManager.parameters(null)`), { name: 'TypeError' });
  assert.throws(() => run(`PluginManager.setParameters(42,{})`), { name: 'TypeError' });
});
check('parameter strings are not coerced or executed', () => {
  const payload = `'}); globalThis.executed = true; //`;
  const run = realm({ table: { test: { Text: payload, Boolean: 'false', Nested: '{"x":1}' } } });
  assert.equal(run(`PluginManager.parameters('Test').Text`), payload);
  assert.equal(run(`PluginManager.parameters('Test').Boolean`), 'false');
  assert.equal(run(`PluginManager.parameters('Test').Nested`), '{"x":1}');
  assert.equal(run('typeof executed'), 'undefined');
});
check('scheduled script names preserve order and exact spelling', () => {
  const names = ['First', ' Unicode 日本語 ', 'Last'];
  const run = realm({ names });
  assert.equal(run('JSON.stringify(PluginManager._scripts)'), JSON.stringify(names));
});
check('scheduled names are a detached array', () => {
  const run = realm({ names: ['A'] });
  run(`scheduled[0] = 'changed'; scheduled.push('B');`);
  assert.equal(run('JSON.stringify(PluginManager._scripts)'), '["A"]');
});
check('names resembling JavaScript remain literal strings', () => {
  const name = `A'); globalThis.injected = true; //`;
  const run = realm({ names: [name] });
  assert.equal(run('PluginManager._scripts[0]'), name);
  assert.equal(run('typeof injected'), 'undefined');
});
check('MV exposes parameter setters, not MZ commands or fake dynamic loading', () => {
  const run = realm({ mz: false });
  assert.equal(run('typeof PluginManager.setParameters'), 'function');
  assert.equal(run('typeof PluginManager.registerCommand'), 'undefined');
  assert.equal(run('typeof PluginManager.loadScript'), 'undefined');
  assert.equal(run('typeof PluginManager.setup'), 'undefined');
});
check('MZ callback preserves the interpreter receiver', () => {
  const run = realm();
  run(`globalThis.owner = { n: 1 }; PluginManager.registerCommand('P', 'Add', function(a){this.n+=a.n;}); PluginManager.callCommand(owner,'P','Add',{n:41});`);
  assert.equal(run('owner.n'), 42);
});
check('MZ callback preserves every falsy argument', () => {
  const run = realm();
  run(`globalThis.values = []; PluginManager.registerCommand('P','C',function(a){values.push(a);}); for(const a of [null,false,0,'']) PluginManager.callCommand(null,'P','C',a);`);
  assert.equal(run('JSON.stringify(values)'), '[null,false,0,""]');
});
check('MZ command matching is case-sensitive', () => {
  const run = realm();
  run(`globalThis.n=0; PluginManager.registerCommand('P','Run',()=>{n++;}); PluginManager.callCommand(null,'p','Run',{}); PluginManager.callCommand(null,'P','run',{});`);
  assert.equal(run('n'), 0);
});
check('independent realms do not share parameters or script lists', () => {
  const first = realm({ names: ['First'] });
  const second = realm({ names: ['Second'] });
  first(`PluginManager.setParameters('P',{Value:'one'}); PluginManager._scripts.push('X');`);
  assert.equal(second(`PluginManager.parameters('P').Value`), undefined);
  assert.equal(second('JSON.stringify(PluginManager._scripts)'), '["Second"]');
});
console.log(`PluginManager contract: ${checks}/${checks} passed (Node; not Jint/Godot).`);
