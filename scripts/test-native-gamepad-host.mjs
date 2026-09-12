import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';
const text=readFileSync(new URL('../project/src/plugins/NativeGamepadHostPrelude.cs',import.meta.url),'utf8');
const source=text.match(/JS_NATIVE_GAMEPAD_HOST_BEGIN[\s\S]*?FactorySource = """\r?\n([\s\S]*?)\r?\n\s*""";/)[1];
let checks=0;function check(name,fn){fn();checks++;console.log('PASS '+name);}
function realm(){const c=vm.createContext({performance:{now:()=>123},navigator:{userAgent:'old',platform:'test',language:'de'}});const run=s=>vm.runInContext(s,c,{timeout:1000});run(source);return run;}
check('empty gamepad list stays empty',()=>{const run=realm();assert.equal(run('navigator.getGamepads().length'),0);});
check('button state uses standard snapshot shape',()=>{const run=realm();run('__urpgGamepadHost.setButton(0,0,true,1)');assert.equal(run('navigator.getGamepads()[0].buttons[0].pressed'),true);assert.equal(run('navigator.getGamepads()[0].buttons[0].value'),1);assert.equal(run('navigator.getGamepads()[0].mapping'),'standard');});
check('axes are preserved',()=>{const run=realm();run('__urpgGamepadHost.setAxis(2,0,-0.75);__urpgGamepadHost.setAxis(2,1,0.5)');assert.equal(run('JSON.stringify(navigator.getGamepads()[2].axes)'),'[-0.75,0.5,0,0]');});
check('snapshots do not mutate backing state',()=>{const run=realm();run('__urpgGamepadHost.setButton(0,0,true,1);globalThis.snap=navigator.getGamepads()[0];__urpgGamepadHost.setButton(0,0,false,0)');assert.equal(run('snap.buttons[0].pressed'),true);assert.equal(run('navigator.getGamepads()[0].buttons[0].pressed'),false);});
check('disconnect removes device without moving indexes',()=>{const run=realm();run('__urpgGamepadHost.setButton(3,1,true,1);__urpgGamepadHost.disconnect(3)');assert.equal(run('navigator.getGamepads()[3]===undefined'),true);});
check('navigator identity metadata is retained',()=>{const run=realm();assert.equal(run('navigator.userAgent+":"+navigator.platform+":"+navigator.language'),'old:test:de');});
check('invalid device button and axis fail',()=>{const run=realm();assert.throws(()=>run('__urpgGamepadHost.setButton(16,0,true,1)'),/device-invalid/);assert.throws(()=>run('__urpgGamepadHost.setButton(0,17,true,1)'),/button-invalid/);assert.throws(()=>run('__urpgGamepadHost.setAxis(0,4,0)'),/axis-invalid/);assert.throws(()=>run('__urpgGamepadHost.setAxis(0,0,2)'),/axis-invalid/);});
check('control object cannot be replaced',()=>{const run=realm();assert.throws(()=>run(`'use strict';__urpgGamepadHost={}`),/read only|readonly|assign/i);});
console.log(`Native gamepad host: ${checks}/${checks} passed (Node; not Godot controller hardware).`);
