import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const text=readFileSync(new URL('../runtime/UniversalRPG.JavaScript.Jint/NativeStoragePrelude.cs',import.meta.url),'utf8');
const match=text.match(/internal const string Source = """\r?\n([\s\S]*?)\r?\n\s*""";/);
assert.ok(match,'production localStorage adapter found');
const source=match[1];
let checks=0;
function check(name,fn){fn();checks++;console.log('PASS '+name);}
function realm({deny=false,throwProvider=false}={}){
 const map=new Map();
 const native=(op,key,value)=>{
   if(deny)return JSON.stringify({success:false,found:false,value:'',keys:[],errorCode:'storage.denied'});
   if(throwProvider)throw new Error('/private/host/path');
   switch(op){
    case 'get':return JSON.stringify({success:true,found:map.has(key),value:map.get(key)||'',keys:[],errorCode:''});
    case 'set':map.set(key,value);break;
    case 'remove':map.delete(key);break;
    case 'clear':map.clear();break;
    case 'keys':return JSON.stringify({success:true,found:false,value:'',keys:[...map.keys()].sort(),errorCode:''});
    default:return JSON.stringify({success:false,found:false,value:'',keys:[],errorCode:'storage.operation-invalid'});
   }
   return JSON.stringify({success:true,found:false,value:'',keys:[],errorCode:''});
 };
 const context=vm.createContext({native});
 const run=code=>vm.runInContext(code,context,{timeout:1000});
 run(`(${source})(native);`);
 return {run,map};
}
check('missing value returns null',()=>{const {run}=realm();assert.equal(run(`localStorage.getItem('none')`),null);});
check('set/get use string coercion like Web Storage',()=>{const {run}=realm();run(`localStorage.setItem(7,false)`);assert.equal(run(`localStorage.getItem('7')`),'false');});
check('empty string key and value are valid',()=>{const {run}=realm();run(`localStorage.setItem('','')`);assert.equal(run(`localStorage.getItem('')`),'');});
check('remove deletes only requested key',()=>{const {run}=realm();run(`localStorage.setItem('a','1');localStorage.setItem('b','2');localStorage.removeItem('a')`);assert.equal(run(`localStorage.getItem('a')`),null);assert.equal(run(`localStorage.getItem('b')`),'2');});
check('clear removes all values',()=>{const {run}=realm();run(`localStorage.setItem('a','1');localStorage.setItem('b','2');localStorage.clear()`);assert.equal(run('localStorage.length'),0);});
check('length and key enumerate stable provider keys',()=>{const {run}=realm();run(`localStorage.setItem('z','1');localStorage.setItem('a','2')`);assert.equal(run('localStorage.length'),2);assert.equal(run('localStorage.key(0)'),'a');assert.equal(run('localStorage.key(1)'),'z');assert.equal(run('localStorage.key(2)'),null);});
check('overwriting a key does not add another entry',()=>{const {run}=realm();run(`localStorage.setItem('a','1');localStorage.setItem('a','2')`);assert.equal(run('localStorage.length'),1);assert.equal(run(`localStorage.getItem('a')`),'2');});
check('denied provider surfaces stable error',()=>{const {run}=realm({deny:true});assert.throws(()=>run(`localStorage.setItem('a','1')`),/storage.denied/);});
check('host conflict is refused',()=>{const context=vm.createContext({native:()=>'',localStorage:{sentinel:true}});assert.throws(()=>vm.runInContext(`(${source})(native)`,context),/storage.host-conflict/);});
check('adapter does not expose provider/native object on globalThis',()=>{const {run}=realm();assert.equal(run(`typeof nativeLocalStorage + ':' + typeof System + ':' + typeof require`),'undefined:undefined:undefined');});
console.log(`Native localStorage adapter: ${checks}/${checks} passed (Node; mock provider, not .NET persistence).`);
