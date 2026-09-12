import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// Execute the production C# JavaScript constant. Node is test tooling only;
// these checks do NOT execute the C# adapter, Jint or a complete MV/MZ game.
const source = readFileSync(new URL('../project/src/plugins/WebBrowserHostPrelude.cs', import.meta.url), 'utf8');
const match = source.match(/JS_BROWSER_HOST_BEGIN[\s\S]*?FactorySource = """\r?\n([\s\S]*?)\r?\n\s*""";[\s\S]*?JS_BROWSER_HOST_END/);
assert.ok(match, 'production browser-host constant is present');
const factory = match[1];
const defaults = { timers: 2048, frames: 2048, callbacks: 4096, arguments: 32, delta: 1000 };
let count = 0;
function realm(overrides = {}) {
  const context = vm.createContext({});
  const run = code => vm.runInContext(code, context, { timeout: 1000 });
  run(`${factory}(${JSON.stringify({ ...defaults, ...overrides })});`);
  return { run, tick: delta => run(`__urpgBrowserHost.advance(${JSON.stringify(delta)})`) };
}
function check(name, action) { action(); count++; console.log(`PASS ${name}`); }

check('window/self aliases refer to the realm, without Node or CLR', () => {
  const { run } = realm();
  assert.equal(run('window === globalThis && self === window'), true);
  assert.equal(run('typeof process + ":" + typeof require + ":" + typeof System'), 'undefined:undefined:undefined');
});
check('unsupported rendering/network APIs are not fabricated', () => {
  const { run } = realm();
  assert.equal(run('typeof document.createElement + ":" + typeof Image + ":" + typeof fetch'), 'undefined:undefined:undefined');
});
check('performance starts at zero and advances in milliseconds', () => {
  const { run, tick } = realm();
  assert.equal(run('performance.now()'), 0);
  tick(10); tick(2.5);
  assert.equal(run('performance.now()'), 12.5);
});
check('scheduling does not execute callbacks', () => {
  const { run } = realm();
  run('globalThis.n = 0; setTimeout(() => n++, 0); requestAnimationFrame(() => n++);');
  assert.equal(run('n'), 0);
});
check('timeouts run once and not before their due time', () => {
  const { run, tick } = realm();
  run('globalThis.n = 0; setTimeout(() => n++, 10);');
  tick(9); assert.equal(run('n'), 0);
  tick(1); assert.equal(run('n'), 1);
  tick(100); assert.equal(run('n'), 1);
});
check('equal-deadline timers keep registration order', () => {
  const { run, tick } = realm();
  run('globalThis.order = []; for (let i=0;i<4;i++) setTimeout(() => order.push(i), 0);');
  tick(0);
  assert.equal(run('JSON.stringify(order)'), '[0,1,2,3]');
});
check('different deadlines are ordered by deadline', () => {
  const { run, tick } = realm();
  run('globalThis.order = []; setTimeout(() => order.push(20),20); setTimeout(() => order.push(5),5);');
  tick(20);
  assert.equal(run('JSON.stringify(order)'), '[5,20]');
});
check('timer callback gets global receiver even in strict mode', () => {
  const { run, tick } = realm();
  run('globalThis.result = false; setTimeout(function(){"use strict"; result = this === window;},0);');
  tick(0); assert.equal(run('result'), true);
});
check('timer arguments include objects and falsy values within the JS realm', () => {
  const { run, tick } = realm();
  run(`globalThis.object = {}; globalThis.ok = false; setTimeout((a,b,c,d,e) => {
    ok = a === object && b === false && c === 0 && d === null && e === '';
  },0,object,false,0,null,'');`);
  tick(0); assert.equal(run('ok'), true);
});
check('clearInterval can cancel a timeout and clearTimeout an interval', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; clearInterval(setTimeout(()=>n++,0)); clearTimeout(setInterval(()=>n++,0));');
  tick(100); assert.equal(run('n'), 0);
});
check('one callback can cancel another timer in the current batch', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; setTimeout(()=>clearTimeout(later),0); const later=setTimeout(()=>n++,0);');
  tick(0); assert.equal(run('n'), 0);
});
check('new zero-delay callbacks are deferred to the next pump', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; setTimeout(()=>{n++;setTimeout(()=>n++,0);},0);');
  tick(0); assert.equal(run('n'), 1);
  tick(0); assert.equal(run('n'), 2);
});
check('intervals coalesce missed periods instead of catching up indefinitely', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; setInterval(()=>n++,5);');
  tick(100); assert.equal(run('n'), 1);
  tick(0); assert.equal(run('n'), 1);
  tick(5); assert.equal(run('n'), 2);
});
check('an interval can cancel itself', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; const id=setInterval(()=>{n++;clearInterval(id);},0);');
  tick(0); tick(1); assert.equal(run('n'), 1);
});
check('zero intervals still run at most once per pump', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; setInterval(()=>n++,0);');
  tick(100); assert.equal(run('n'), 1);
  tick(0); assert.equal(run('n'), 2);
});
check('nested timers apply the four-millisecond minimum after level five', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; function again(){n++;setTimeout(again,0);} setTimeout(again,0);');
  for(let i=0;i<8;i++) tick(0);
  assert.equal(run('n'), 6);
  tick(3); assert.equal(run('n'), 6);
  tick(1); assert.equal(run('n'), 7);
});
check('timeout uses signed-long delay coercion', () => {
  const { run, tick } = realm();
  run(`globalThis.n=0; for (const delay of [-1,NaN,Infinity,2147483648,undefined]) setTimeout(()=>n++,delay);`);
  tick(0); assert.equal(run('n'), 5);
});
check('fractional timeout is truncated', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; setTimeout(()=>n++,1.9);');
  tick(1); assert.equal(run('n'), 1);
});
check('animation callbacks run once with shared monotonic timestamp', () => {
  const { run, tick } = realm();
  run('globalThis.seen=[]; requestAnimationFrame(t=>seen.push(t)); requestAnimationFrame(t=>seen.push(t));');
  tick(16); tick(16);
  assert.equal(run('JSON.stringify(seen)'), '[16,16]');
});
check('strict animation callbacks have undefined receiver', () => {
  const { run, tick } = realm();
  run('globalThis.ok=false; requestAnimationFrame(function(){"use strict";ok=this===undefined;});');
  tick(1); assert.equal(run('ok'), true);
});
check('animation cancellation also affects the current frame batch', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; requestAnimationFrame(()=>cancelAnimationFrame(later)); const later=requestAnimationFrame(()=>n++);');
  tick(16); assert.equal(run('n'), 0);
});
check('recursively requested animation callbacks wait until next frame', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; function loop(){n++;requestAnimationFrame(loop);} requestAnimationFrame(loop);');
  tick(16); assert.equal(run('n'), 1);
  tick(16); assert.equal(run('n'), 2);
});
check('animation work scheduled by timers is deferred under the explicit pump policy', () => {
  const { run, tick } = realm();
  run('globalThis.n=0; setTimeout(()=>requestAnimationFrame(()=>n++),0);');
  tick(16); assert.equal(run('n'), 0);
  tick(16); assert.equal(run('n'), 1);
});
check('timer callbacks precede the snapshotted animation batch', () => {
  const { run, tick } = realm();
  run('globalThis.order=[]; requestAnimationFrame(()=>order.push("frame")); setTimeout(()=>order.push("timer"),0);');
  tick(16); assert.equal(run('JSON.stringify(order)'), '["timer","frame"]');
});
check('timer and animation cancellation do not alias their distinct IDs', () => {
  const { run, tick } = realm();
  run('globalThis.n=0;const t=setTimeout(()=>n++,0), f=requestAnimationFrame(()=>n++);cancelAnimationFrame(t);clearTimeout(f);');
  tick(0); assert.equal(run('n'), 2);
});
check('string timer handlers fail explicitly without evaluating code', () => {
  const { run } = realm();
  assert.throws(()=>run(`setTimeout('globalThis.injected=true',0)`), /function-callback-required/);
  assert.equal(run('typeof injected'), 'undefined');
});
check('nonfunction animation handlers are refused', () => {
  const { run } = realm();
  assert.throws(()=>run('requestAnimationFrame(null)'), /function-callback-required/);
});
check('pending timers are bounded and cancellation frees capacity', () => {
  const { run, tick } = realm({ timers: 1 });
  run('const id=setTimeout(()=>{},10);');
  assert.throws(()=>run('setTimeout(()=>{},10)'), /timer-limit/);
  run('clearTimeout(id);globalThis.n=0;setTimeout(()=>n++,0);');
  tick(0); assert.equal(run('n'), 1);
});
check('pending animation requests are bounded', () => {
  const { run } = realm({ frames: 1 });
  run('const id=requestAnimationFrame(()=>{});');
  assert.throws(()=>run('requestAnimationFrame(()=>{})'), /animation-frame-limit/);
  run('cancelAnimationFrame(id);requestAnimationFrame(()=>{});');
});
check('timer argument count is bounded', () => {
  const { run } = realm({ arguments: 1 });
  assert.throws(()=>run('setTimeout(()=>{},0,1,2)'), /timer-argument-limit/);
});
check('callback budget faults without silently discarding an over-budget batch', () => {
  const { run, tick } = realm({ callbacks: 1 });
  run('globalThis.n=0;setTimeout(()=>n++,0);requestAnimationFrame(()=>n++);');
  assert.throws(()=>tick(0), /callback-budget/);
  assert.equal(run('n'), 1);
  assert.throws(()=>tick(0), /web.host.faulted/);
  assert.throws(()=>run('setTimeout(()=>{},0)'), /web.host.faulted/);
});
check('callback exception prevents later callbacks and faults the host', () => {
  const { run, tick } = realm();
  run('globalThis.n=0;setTimeout(()=>{throw Error("plugin failure");},0);setTimeout(()=>n++,0);');
  assert.throws(()=>tick(0), /plugin failure/);
  assert.equal(run('n'), 0);
  assert.throws(()=>tick(1), /web.host.faulted/);
});
check('nested frame pumping is refused', () => {
  const { run, tick } = realm();
  run('setTimeout(()=>__urpgBrowserHost.advance(0),0);');
  assert.throws(()=>tick(0), /reentrant-frame/);
});
check('invalid delta does not move the clock or poison a recoverable host request', () => {
  const { run, tick } = realm();
  for (const value of ['-1','NaN','Infinity','1001','"1"'])
    assert.throws(()=>run(`__urpgBrowserHost.advance(${value})`), /invalid-delta/);
  assert.equal(run('performance.now()'), 0);
  tick(5); assert.equal(run('performance.now()'), 5);
});
check('currentScript is null outside top-level plugin execution', () => {
  const { run, tick } = realm();
  assert.equal(run('document.currentScript'), null);
  run(`__urpgBrowserHost.enterScript('urpg://game/js/plugins/My%20Plugin.js');
    globalThis.src = document.currentScript.src;
    globalThis.atCallback = false;
    setTimeout(()=>{atCallback = document.currentScript === null;},0);
    __urpgBrowserHost.leaveScript();`);
  assert.equal(run('src'), 'urpg://game/js/plugins/My%20Plugin.js');
  tick(0); assert.equal(run('atCallback'), true);
});
check('script metadata cannot be mutated in place', () => {
  const { run } = realm();
  run(`__urpgBrowserHost.enterScript('urpg://game/js/plugins/A.js');`);
  assert.equal(run('Object.isFrozen(document.currentScript)'), true);
  assert.throws(()=>run(`'use strict';document.currentScript.src='bad';`), /read only|readonly|assign/i);
});
check('frame pumping during synchronous plugin execution is refused', () => {
  const { run, tick } = realm();
  run(`__urpgBrowserHost.enterScript('urpg://game/js/plugins/A.js');`);
  assert.throws(()=>tick(0), /frame-during-script/);
  run('__urpgBrowserHost.leaveScript()');
  tick(1);
});
check('script scopes cannot overlap', () => {
  const { run } = realm();
  run(`__urpgBrowserHost.enterScript('urpg://game/js/plugins/A.js');`);
  assert.throws(()=>run(`__urpgBrowserHost.enterScript('urpg://game/js/plugins/B.js');`), /script-scope-busy/);
});
check('clock progression is independent of Date.now', () => {
  const { run, tick } = realm();
  run('Date.now = () => -999999;'); tick(17);
  assert.equal(run('performance.now()'), 17);
});
check('captured intrinsics survive plugin replacements', () => {
  const { run, tick } = realm();
  run(`globalThis.n=0;setTimeout(()=>n++,0);Object.keys=()=>{throw Error('replaced');};
    Reflect.apply=()=>{throw Error('replaced');};Array.prototype.sort=()=>{throw Error('replaced');};`);
  tick(0); assert.equal(run('n'), 1);
});
check('host control cannot be replaced by a plugin', () => {
  const { run } = realm();
  assert.throws(()=>run(`'use strict'; __urpgBrowserHost = {};`), /read only|readonly|assign/i);
});
check('two independent hosts have separate queues and clocks', () => {
  const a=realm(), b=realm();
  a.run('globalThis.n=0;setTimeout(()=>n++,0);');
  b.run('globalThis.n=0;setTimeout(()=>n++,0);');
  a.tick(10);
  assert.equal(a.run('n'),1); assert.equal(b.run('n'),0);
  assert.equal(b.run('performance.now()'),0);
});
check('invalid configuration is rejected', () => {
  for (const config of [{timers:0},{frames:9999},{callbacks:-1},{arguments:257},{delta:Infinity}])
    assert.throws(()=>realm(config), /invalid-limits/);
});
check('existing browser globals are not overwritten or silently merged', () => {
  const context = vm.createContext({window:{sentinel:true}});
  assert.throws(()=>vm.runInContext(`${factory}(${JSON.stringify(defaults)});`,context,{timeout:1000}), /global-conflict/);
});
check('timer capacity is checked again after delay coercion executes plugin code', () => {
  const { run, tick } = realm({ timers: 1 });
  run('globalThis.n=0;');
  assert.throws(()=>run('setTimeout(()=>n+=100,{valueOf(){setTimeout(()=>n++,0);return 0;}})'), /timer-limit/);
  tick(0); assert.equal(run('n'),1);
});
check('plugins may patch performance.now without modifying internal deadlines', () => {
  const { run, tick } = realm();
  run('globalThis.n=0;performance.now=()=>123;setTimeout(()=>n++,10);');
  tick(9);assert.equal(run('n'),0);tick(1);assert.equal(run('n'),1);
  assert.equal(run('performance.now()'),123);
});
console.log(`Browser host JavaScript: ${count}/${count} passed (Node; not C#/Jint/Godot).`);
