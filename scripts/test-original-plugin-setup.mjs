import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import vm from 'node:vm';
const text=readFileSync(new URL('../project/src/plugins/NativeCorePluginSetup.cs',import.meta.url),'utf8');
const factory=text.match(/JS_ORIGINAL_PLUGIN_SETUP_BEGIN[\s\S]*?FactorySource = """\r?\n([\s\S]*?)\r?\n\s*""";/)[1];
const original=readFileSync(new URL('../project/tests/fixtures/core-startup/MVPluginManager.js',import.meta.url),'utf8');
let checks=0;
function check(name,test){test();checks++;console.log('PASS '+name);}
function realm(config, mz=false){
 const context=vm.createContext({});const run=s=>vm.runInContext(s,context,{timeout:1000});
 // The engine normally defines Array.contains. This helper is a test double;
 // the actual original PluginManager file below is byte-for-byte unchanged.
 run('Array.prototype.contains=function(x){return this.indexOf(x)>=0;};');
 run(original);run('globalThis.$plugins='+JSON.stringify(config)+';');
 if(mz)run(`PluginManager.setup=function(plugins){for(const plugin of plugins){if(plugin.status&&!this._scripts.includes(plugin.name)){this.setParameters(plugin.name,plugin.parameters);this.loadScript(plugin.name);this._scripts.push(plugin.name);}}};`);
 run('globalThis.originalLoad = PluginManager.loadScript;globalThis.originalParameters = PluginManager.parameters;');
 return {run,boot:names=>run(`(${factory})(${JSON.stringify(names)},${mz});`)};
}
const entry=(name,status=true,parameters={Value:'42'})=>({name,status,parameters});
check('original MV setup supplies parameters without DOM loading',()=>{const r=realm([entry('A')]);r.boot(['A']);assert.equal(r.run('PluginManager.parameters("a").Value'),'42');});
check('original parameter and loader methods retain identity',()=>{const r=realm([entry('A')]);r.boot(['A']);assert.equal(r.run('PluginManager.parameters===originalParameters && PluginManager.loadScript===originalLoad'),true);});
check('loadScript scheduling adapts MZ name without .js suffix',()=>{const r=realm([entry('A')],true);r.boot(['A']);assert.equal(r.run('PluginManager._scripts[0]'),'A');});
check('disabled entries and later exact duplicates retain original semantics',()=>{const r=realm([entry('A',false),entry('B'),entry('A'),entry('B',true,{Value:'bad'})]);r.boot(['B','A']);assert.equal(r.run('PluginManager.parameters("b").Value'),'42');});
check('case-distinct plugins retain original lowercased parameter collision semantics',()=>{const r=realm([entry('A'),entry('a',true,{Value:'99'})]);r.boot(['A','a']);assert.equal(r.run('PluginManager.parameters("A").Value'),'99');});
check('empty plugin plan runs real setup',()=>{const r=realm([]);r.boot([]);assert.equal(r.run('PluginManager._scripts.length'),0);});
check('game-authored setParameters alias is called, not replaced',()=>{const r=realm([entry('A')]);r.run('const old=PluginManager.setParameters;PluginManager.setParameters=function(n,p){p.Aliased="yes";return old.call(this,n,p);};');r.boot(['A']);assert.equal(r.run('PluginManager.parameters("a").Aliased'),'yes');});
check('subsequent plugin sees a real core method to alias',()=>{const r=realm([entry('A')]);r.run('globalThis.Game_Example=function(){};Game_Example.prototype.value=function(){return 40;};');r.boot(['A']);r.run('const previous=Game_Example.prototype.value;Game_Example.prototype.value=function(){return previous.call(this)+2;};');assert.equal(r.run('new Game_Example().value()'),42);});
check('plugin names remain data, including quotes and Unicode',()=>{const name='日本 x\"; throw Error(\"bad\");//';const r=realm([entry(name)]);r.boot([name]);assert.equal(r.run('PluginManager._scripts[0]'),name);});
check('different configuration/order fails before silently executing wrong plugins',()=>{const r=realm([entry('B'),entry('A')]);assert.throws(()=>r.boot(['A','B']),/schedule-mismatch/);assert.equal(r.run('PluginManager.loadScript===originalLoad'),true);});
check('missing scheduled plugin fails',()=>{const r=realm([entry('A')]);assert.throws(()=>r.boot(['A','B']),/schedule-incomplete/);});
check('extra scheduled plugin fails',()=>{const r=realm([entry('A'),entry('B')]);assert.throws(()=>r.boot(['A']),/schedule-mismatch/);});
check('setup exception restores original loader and propagates',()=>{const r=realm([entry('A')]);r.run('PluginManager.setup=function(){throw Error("custom setup failure");};');assert.throws(()=>r.boot(['A']),/custom setup failure/);assert.equal(r.run('PluginManager.loadScript===originalLoad'),true);});
check('previously initialized manager is refused, not reset',()=>{const r=realm([entry('A')]);r.run('PluginManager._scripts.push("Old");');assert.throws(()=>r.boot(['A']),/already-scheduled/);assert.equal(r.run('PluginManager._scripts[0]'),'Old');});
check('missing real manager is not replaced by fake globals',()=>{const r=realm([]);r.run('PluginManager=undefined;');assert.throws(()=>r.boot([]),/original-plugin-manager-required/);});
check('nonreplaceable custom loader is refused',()=>{const r=realm([entry('A')]);r.run('Object.defineProperty(PluginManager,"loadScript",{configurable:false});');assert.throws(()=>r.boot(['A']),/not-replaceable/);});
check('a forged completion list is rejected',()=>{const r=realm([entry('A')]);r.run('PluginManager.setup=function(){this.loadScript("A.js");this._scripts.push("B");};');assert.throws(()=>r.boot(['A']),/order-mismatch/);});
check('dynamic loading afterwards remains original behavior, not successful no-op',()=>{const r=realm([entry('A')]);r.boot(['A']);assert.throws(()=>r.run('PluginManager.loadScript("Extra.js")'),/document is not defined/);});
console.log(`Original plugin setup: ${checks}/${checks} passed (Node; original MV manager, synthetic MZ scheduling; not a full game).`);
