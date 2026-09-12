import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const text = readFileSync(new URL('../project/src/plugins/NativeEntryPointHostPrelude.cs', import.meta.url), 'utf8');
const match = text.match(/JS_NATIVE_ENTRY_HOST_BEGIN[\s\S]*?FactorySource = """\r?\n([\s\S]*?)\r?\n\s*""";/);
assert.ok(match, 'production entry-host constant is present');
const factory = match[1];
let checks = 0;
function check(name, fn) { fn(); checks++; console.log('PASS ' + name); }
function realm(known = ['js/rpg_core.js','js/rpg_managers.js']) {
  const context = vm.createContext({});
  const run = code => vm.runInContext(code, context, { timeout: 1000 });
  run(`globalThis.window=globalThis;globalThis.self=globalThis;globalThis.__currentScript=null;
    globalThis.document=Object.freeze({get currentScript(){return __currentScript;}});
    globalThis.__timers=[];globalThis.setTimeout=(fn)=>{__timers.push(fn);return __timers.length;};
    globalThis.clearTimeout=()=>{};`);
  run(`(${factory})(${JSON.stringify(known)});`);
  const flush = () => { while (run('__timers.length')) run('(__timers.shift())()'); };
  return { run, flush };
}

check('window load listener and onload both dispatch exactly once', () => {
  const {run}=realm();
  run(`globalThis.order=[];addEventListener('load',()=>order.push('listener'));onload=()=>order.push('property');`);
  run('__urpgEntryHost.dispatchLoad()');
  assert.equal(run('JSON.stringify(order)'), '["listener","property"]');
  assert.throws(()=>run('__urpgEntryHost.dispatchLoad()'),/already-dispatched/);
});
check('once listener is removed after first dispatch source',()=>{
  const {run}=realm();run(`globalThis.n=0;addEventListener('custom',()=>n++,{once:true});`);
  // custom window dispatch is intentionally unavailable; document dispatch is.
  run(`globalThis.m=0;document.addEventListener('visibilitychange',()=>m++,{once:true});`);
  run(`__urpgEntryHost.dispatchDocument('visibilitychange')`);
  run(`__urpgEntryHost.dispatchDocument('visibilitychange')`);
  assert.equal(run('m'),1);
});
check('known preloaded script reports asynchronous load without reexecution',()=>{
  const {run,flush}=realm();
  run(`globalThis.n=0;const s=document.createElement('script');s.src='js/rpg_core.js';s.onload=function(e){if(this!==s||e.target!==s)throw Error('receiver');n++;};document.body.appendChild(s);`);
  assert.equal(run('n'),0);flush();assert.equal(run('n'),1);
});
check('unknown dynamic script reports error instead of fake success',()=>{
  const {run,flush}=realm();
  run(`globalThis.kind='';const s=document.createElement('script');s.src='js/plugins/Dynamic.js';s.onerror=e=>kind=e.type;document.body.appendChild(s);`);
  flush();assert.equal(run('kind'),'error');
});
check('currentScript forwards the browser-host metadata object',()=>{
  const {run}=realm();
  run(`__currentScript=Object.freeze({src:'urpg://game/js/main.js'});`);
  assert.equal(run('document.currentScript.src'),'urpg://game/js/main.js');
  run('__currentScript=null');assert.equal(run('document.currentScript'),null);
});
check('basic spinner/error DOM can be added, found and removed',()=>{
  const {run}=realm();
  run(`const a=document.createElement('div');a.id='loadingSpinner';const b=document.createElement('div');b.id='child';a.appendChild(b);document.body.appendChild(a);`);
  assert.equal(run('document.getElementById("loadingSpinner")===a'),true);
  assert.equal(run('document.getElementById("child")===b'),true);
  run('document.body.removeChild(a)');
  assert.equal(run('document.getElementById("loadingSpinner")'),null);
});
check('outerHTML supports MZ error-printer construction',()=>{
  const {run}=realm();
  run(`const d=document.createElement('div');d.id='errorName';d.innerHTML='Failure';globalThis.html=d.outerHTML;`);
  assert.equal(run('html'),'<div id="errorName">Failure</div>');
});
check('head/body tag lists implement item()',()=>{
  const {run}=realm();
  assert.equal(run(`document.getElementsByTagName('head').item(0)===document.head`),true);
  assert.equal(run(`document.getElementsByTagName('body').item(0)===document.body`),true);
});
check('canvas explicitly has no context until native renderer exists',()=>{
  const {run}=realm();assert.equal(run(`document.createElement('canvas').getContext('2d')`),null);
});
check('navigator and location do not expose Node/browser process powers',()=>{
  const {run}=realm();
  assert.equal(run('navigator.userAgent'),'UniversalRPG');
  assert.equal(run('location.href'),'urpg://game/index.html');
  assert.equal(run('typeof process + ":" + typeof require + ":" + typeof fetch'),'undefined:undefined:undefined');
});
check('case-colliding script plan is rejected',()=>{
  assert.throws(()=>realm(['js/A.js','js/a.js']),/script-plan-duplicate/);
});
check('unsafe dynamic script paths never match preloaded sources',()=>{
  const {run,flush}=realm();
  run(`globalThis.kind='';const s=document.createElement('script');s.src='../rpg_core.js';s.onerror=e=>kind=e.type;document.body.appendChild(s);`);
  flush();assert.equal(run('kind'),'error');
});
check('document listeners can be removed',()=>{
  const {run}=realm();run(`globalThis.n=0;const f=()=>n++;document.addEventListener('visibilitychange',f);document.removeEventListener('visibilitychange',f);__urpgEntryHost.dispatchDocument('visibilitychange');`);assert.equal(run('n'),0);
});
console.log(`Native entry host: ${checks}/${checks} passed (Node; not C#/Jint/Godot or rendering).`);
