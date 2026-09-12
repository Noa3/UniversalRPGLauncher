import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import vm from 'node:vm';
const text=readFileSync(new URL('../project/src/plugins/NativeEntryPointHostPrelude.cs',import.meta.url),'utf8');
const source=text.match(/JS_NATIVE_ENTRY_HOST_BEGIN[\s\S]*?FactorySource = """\r?\n([\s\S]*?)\r?\n\s*""";/)[1];
let checks=0;
function check(name,fn){fn();checks++;console.log('PASS '+name);}
function realm(){const c=vm.createContext({});const run=s=>vm.runInContext(s,c,{timeout:1000});run(`window=globalThis;self=globalThis;document=Object.freeze({get currentScript(){return null;}});setTimeout=fn=>0;clearTimeout=()=>{};(${source})(['js/rpg_core.js']);`);return run;}
check('keydown exposes legacy keyCode/which and preventDefault',()=>{const run=realm();run(`globalThis.seen=null;document.addEventListener('keydown',e=>{e.preventDefault();seen=[e.type,e.keyCode,e.which,e.defaultPrevented];});`);run(`__urpgEntryHost.dispatchKey('keydown',37,false,0)`);assert.equal(run('JSON.stringify(seen)'),'["keydown",37,37,true]');});
check('keyup reaches original-style listener',()=>{const run=realm();run(`globalThis.up=0;document.addEventListener('keyup',e=>up=e.keyCode);__urpgEntryHost.dispatchKey('keyup',90,false,0);`);assert.equal(run('up'),90);});
check('repeat and modifier flags are preserved',()=>{const run=realm();run(`globalThis.flags='';document.addEventListener('keydown',e=>flags=[e.repeat,e.altKey,e.ctrlKey,e.shiftKey,e.metaKey].join(','));__urpgEntryHost.dispatchKey('keydown',13,true,15);`);assert.equal(run('flags'),'true,true,true,true,true');});
check('duplicate listener follows EventTarget dedupe rule',()=>{const run=realm();run(`globalThis.n=0;const f=()=>n++;document.addEventListener('keydown',f);document.addEventListener('keydown',f);__urpgEntryHost.dispatchKey('keydown',13,false,0);`);assert.equal(run('n'),1);});
check('removed listener is not called',()=>{const run=realm();run(`globalThis.n=0;const f=()=>n++;document.addEventListener('keydown',f);document.removeEventListener('keydown',f);__urpgEntryHost.dispatchKey('keydown',13,false,0);`);assert.equal(run('n'),0);});
check('blur window event reaches focus-loss listener',()=>{const run=realm();run(`globalThis.n=0;addEventListener('blur',()=>n++);__urpgEntryHost.dispatchWindow('blur');`);assert.equal(run('n'),1);});
check('invalid key event type and code fail',()=>{const run=realm();assert.throws(()=>run(`__urpgEntryHost.dispatchKey('keypress',13,false,0)`),/key-event-type/);assert.throws(()=>run(`__urpgEntryHost.dispatchKey('keydown',-1,false,0)`),/key-code-invalid/);assert.throws(()=>run(`__urpgEntryHost.dispatchKey('keydown',70000,false,0)`),/key-code-invalid/);});
check('preventDefault state is per event',()=>{const run=realm();run(`globalThis.states=[];document.addEventListener('keydown',e=>{states.push(e.defaultPrevented);if(e.keyCode===8)e.preventDefault();states.push(e.defaultPrevented);});__urpgEntryHost.dispatchKey('keydown',8,false,0);__urpgEntryHost.dispatchKey('keydown',13,false,0);`);assert.equal(run('JSON.stringify(states)'),'[false,true,false,false]');});
console.log(`Native input host: ${checks}/${checks} passed (Node; not Godot input or a game).`);
