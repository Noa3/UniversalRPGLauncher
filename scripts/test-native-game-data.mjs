import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

// Production JS; in-memory native transport and a controlled timer fixture.
// This is not C#/Jint, real disk/archive I/O, or an end-to-end game test.
const source = readFileSync(new URL('../runtime/UniversalRPG.JavaScript.Jint/NativeDataRequestPrelude.cs', import.meta.url), 'utf8');
const match = source.match(/JS_NATIVE_DATA_BEGIN[\s\S]*?Source = """\r?\n([\s\S]*?)\r?\n\s*""";[\s\S]*?JS_NATIVE_DATA_END/);
assert.ok(match, 'production native data adapter is present');
const dataManager = readFileSync(new URL('../project/tests/fixtures/native-data/MVDataManager.excerpt.js', import.meta.url), 'utf8');
let passed = 0;
function check(name, action) { action(); passed++; console.log('PASS ' + name); }
function realm(files = {}) {
    const reads = [];
    const context = vm.createContext({ __read: url => {
        reads.push(url);
        return JSON.stringify(Object.hasOwn(files, url)
            ? { success: true, text: files[url], errorCode: '' }
            : { success: false, text: '', errorCode: 'data.read-failed' });
    } });
    const run = code => vm.runInContext(code, context, { timeout: 1000 });
    run(`globalThis.window = globalThis;
        let next = 1; const timers = new Map();
        globalThis.setTimeout = callback => { const id=next++;timers.set(id,callback);return id; };
        globalThis.clearTimeout = id => timers.delete(id);
        globalThis.tick = () => { for(const [id,fn] of [...timers]) if(timers.delete(id)) fn(); };
    `);
    run(`${match[1]}(__read); delete globalThis.__read;`);
    return { run, tick: () => run('tick()'), reads, files };
}
function request(run, suffix = '') {
    run(`globalThis.x = new XMLHttpRequest(); x.open('GET','data/System.json'); ${suffix}`);
}
check('load is asynchronous and carries exact JSON text', () => {
    const r=realm({'data/System.json':'{"title":"世界"}'});
    request(r.run, 'globalThis.done=false;x.onload=function(e){done=this===x && e.target===x;};x.send();');
    assert.equal(r.reads.length,0);assert.equal(r.run('done'),false);
    r.tick();assert.equal(r.run('x.status'),200);assert.equal(r.run('x.responseText'),' {"title":"世界"}'.trim());
    assert.equal(r.run('done'),true);assert.equal(r.run('x.response'),r.run('x.responseText'));
});
check('ready states and completion events have defined order', () => {
    const r=realm({'data/System.json':'{}'});
    r.run(`globalThis.order=[];globalThis.x=new XMLHttpRequest();
        x.onreadystatechange=()=>order.push(x.readyState);x.onload=()=>order.push('load');
        x.onloadstart=()=>order.push('start');x.onloadend=()=>order.push('end');
        x.open('GET','data/System.json');x.send();`);
    r.tick();assert.equal(r.run('JSON.stringify(order)'),'[1,"start",2,3,4,"load","end"]');
});
check('local missing file reports error rather than successful empty data',()=>{
    const r=realm();request(r.run,"globalThis.kind='';x.onload=()=>kind='load';x.onerror=()=>kind='error';x.send();");
    r.tick();assert.equal(r.run('kind'),'error');assert.equal(r.run('x.status'),0);
    assert.equal(r.run('x.urpgErrorCode'),'data.read-failed');
});
check('abort before execution performs no native read',()=>{
    const r=realm();request(r.run,"globalThis.events=[];x.onabort=()=>events.push('abort');x.onloadend=()=>events.push('end');x.send();x.abort();");
    r.tick();assert.equal(r.reads.length,0);assert.equal(r.run('x.readyState'),0);
    assert.equal(r.run('JSON.stringify(events)'),'["abort","end"]');
});
check('reopening pending request cancels old response',()=>{
    const r=realm({'data/System.json':'old','data/Map002.json':'new'});
    request(r.run,"x.send();x.open('GET','data/Map002.json');x.send();");r.tick();
    assert.deepEqual(r.reads,['data/Map002.json']);assert.equal(r.run('x.responseText'),'new');
});
check('reopening during headers never delivers stale load event',()=>{
    const r=realm({'data/System.json':'old','data/Map002.json':'new'});
    request(r.run,`globalThis.loads=[];x.onload=()=>loads.push(x.responseText);
        x.onreadystatechange=function(){if(x.readyState===2 && x.responseURL==='data/System.json'){
            x.open('GET','data/Map002.json');x.send();}};x.send();`);
    r.tick();assert.equal(r.run('loads.length'),0);r.tick();assert.equal(r.run('JSON.stringify(loads)'),'["new"]');
});
check('abort inside loadstart releases capacity and cancels read',()=>{
    const r=realm();request(r.run,'x.onloadstart=()=>x.abort();x.send();');r.tick();assert.equal(r.reads.length,0);
});
check('abort inside loading prevents load callback',()=>{
    const r=realm({'data/System.json':'{}'});
    request(r.run,'globalThis.loaded=false;x.onload=()=>loaded=true;x.onreadystatechange=()=>{if(x.readyState===3)x.abort();};x.send();');
    r.tick();assert.equal(r.run('loaded'),false);assert.equal(r.run('x.readyState'),0);
});
check('completion callback can reuse the same request',()=>{
    const r=realm({'data/System.json':'one','data/Map002.json':'two'});
    request(r.run,`globalThis.loads=[];x.onload=()=>{loads.push(x.responseText);if(loads.length===1){x.open('GET','data/Map002.json');x.send();}};x.send();`);
    r.tick();r.tick();assert.equal(r.run('JSON.stringify(loads)'),'["one","two"]');
});
check('json response type returns parsed data and denies responseText',()=>{
    const r=realm({'data/System.json':'{"value":42}'});
    request(r.run,"x.responseType='json';x.send();");r.tick();
    assert.equal(r.run('x.response.value'),42);assert.throws(()=>r.run('x.responseText'),/response-is-json/);
});
check('invalid JSON in json response type yields null, text type preserves bytes',()=>{
    const r=realm({'data/System.json':'invalid'});
    request(r.run,"x.responseType='json';x.send();");r.tick();assert.equal(r.run('x.response'),null);
});
check('event listeners retain their receiver and honor removal/once',()=>{
    const r=realm({'data/System.json':'{}'});
    request(r.run,`globalThis.n=0;function onLoad(e){if(this!==x||e.currentTarget!==x)throw Error('receiver');n++;}
        x.addEventListener('load',onLoad,{once:true});x.addEventListener('load',onLoad);x.send();`);
    r.tick();r.run("x.open('GET','data/System.json');x.send();");r.tick();assert.equal(r.run('n'),1);
});
check('GET only; no writes, sync requests, bodies, credentials or binary transport',()=>{
    const r=realm();request(r.run);
    for(const command of ["x.open('POST','data/System.json')", "x.open('GET','data/System.json',false)",
        "x.open('GET','data/System.json',true,'user','pass')", "x.send('body')", "x.responseType='arraybuffer'",
        "x.withCredentials=true", "x.timeout=100", "x.setRequestHeader('X','value')"])
        assert.throws(()=>r.run(command),/data\./);
    assert.equal(r.reads.length,0);
});
check('sending twice fails before another read',()=>{
    const r=realm({'data/System.json':'{}'});request(r.run,'x.send();');
    assert.throws(()=>r.run('x.send()'),/invalid-send-state/);r.tick();assert.equal(r.reads.length,1);
});
check('request and instance constants agree',()=>{
    const r=realm();assert.equal(r.run('XMLHttpRequest.DONE'),4);
    request(r.run);assert.equal(r.run('x.OPENED'),1);
});
check('response metadata is exposed only after successful read',()=>{
    const r=realm({'data/System.json':'{}'});request(r.run,'x.send();');
    assert.equal(r.run('x.getResponseHeader("Content-Type")'),null);r.tick();
    assert.equal(r.run('x.getResponseHeader("Content-Type")'),'application/json; charset=utf-8');
    assert.equal(r.run('x.getResponseHeader("Secret")'),null);
});
check('queues are bounded and abort frees a slot',()=>{
    const r=realm();r.run("globalThis.requests=[];for(let i=0;i<128;i++){const x=new XMLHttpRequest();x.open('GET','data/A.json');x.send();requests.push(x);}");
    assert.throws(()=>r.run("const more=new XMLHttpRequest();more.open('GET','data/A.json');more.send();"),/pending-limit/);
    r.run("requests[0].abort();const nextRequest=new XMLHttpRequest();nextRequest.open('GET','data/A.json');nextRequest.send();");
});
check('ordinary scripts cannot access the private native read function',()=>{
    const r=realm();assert.equal(r.run('typeof readEnvelope + ":" + typeof __read'),'undefined:undefined');
    assert.equal(r.run('typeof System + ":" + typeof process + ":" + typeof require'),'undefined:undefined:undefined');
});
check('modified JSON.parse cannot corrupt transport parsing',()=>{
    const r=realm({'data/System.json':'{}'});r.run("JSON.parse=()=>{throw Error('replaced');}");
    request(r.run,'x.send();');r.tick();assert.equal(r.run('x.status'),200);
});
check('different realms do not share pending counts or response state',()=>{
    const a=realm({'data/System.json':'A'}),b=realm({'data/System.json':'B'});
    request(a.run,'x.send();');request(b.run,'x.send();');a.tick();assert.equal(b.run('x.readyState'),1);
    b.tick();assert.equal(a.run('x.responseText'),'A');assert.equal(b.run('x.responseText'),'B');
});
check('load callback exception propagates, not silent success',()=>{
    const r=realm({'data/System.json':'{}'});request(r.run,"x.onload=()=>{throw Error('plugin failure');};x.send();");
    assert.throws(()=>r.tick(),/plugin failure/);
});

function originalManager() {
    const r=realm();
    // Explicit test doubles ONLY for unrelated rendering/option dependencies.
    // No renderer or complete engine is being certified by these tests.
    r.run(`globalThis.Utils={isOptionValid:()=>false};globalThis.Decrypter={};
        globalThis.imageCalls=0;globalThis.Scene_Boot={loadSystemImages(){imageCalls++;}};
        globalThis.ResourceHandler={createLoader:(url)=>()=>{throw Error('missing map '+url);}};
        Number.prototype.padZero=function(n){return String(this).padStart(n,'0');};
        String.prototype.format=function(value){return this.replace('%1',value);};`);
    r.run(dataManager);
    const names=JSON.parse(r.run('JSON.stringify(DataManager._databaseFiles.map(x=>x.src))'));
    for(const name of names) r.files['data/'+name]=JSON.stringify([null,{id:1,note:'<tag><count:42>'}]);
    r.files['data/System.json']=JSON.stringify({gameTitle:'Native data test',hasEncryptedImages:true});
    r.files['data/Map001.json']=JSON.stringify({width:2,height:1,data:[0,0],note:'<region:first>',events:[null,{id:1,note:'<door>'}]});
    r.files['data/Map002.json']=JSON.stringify({width:1,height:1,data:[1],note:'<region:second>',events:[null]});
    return r;
}
check('original MV DataManager loads all 14 database files without changed engine code',()=>{
    const r=originalManager();r.run('DataManager.loadDatabase();');assert.equal(r.run('DataManager.isDatabaseLoaded()'),false);
    assert.equal(r.reads.length,0);r.tick();assert.equal(r.run('DataManager.isDatabaseLoaded()'),true);
    assert.equal(r.reads.length,14);assert.equal(r.run('$dataSystem.gameTitle'),'Native data test');
    assert.equal(r.run('imageCalls'),1);
});
check('original MV note tags and null-index database layout survive',()=>{
    const r=originalManager();r.run('DataManager.loadDatabase();');r.tick();
    assert.equal(r.run('$dataActors[0]'),null);assert.equal(r.run('$dataActors[1].meta.tag'),true);
    assert.equal(r.run('$dataActors[1].meta.count'),'42');assert.equal(r.run('Decrypter.hasEncryptedImages'),true);
});
check('original MV map loader loads requested map, then another lazily',()=>{
    const r=originalManager();r.run('DataManager.loadMapData(1);');r.tick();
    assert.equal(r.run('$dataMap.meta.region'),'first');assert.equal(r.run('$dataMap.events[1].meta.door'),true);
    r.run('DataManager.loadMapData(2);');assert.equal(r.run('DataManager.isMapLoaded()'),false);r.tick();
    assert.equal(r.run('$dataMap.meta.region'),'second');assert.deepEqual(r.reads,['data/Map001.json','data/Map002.json']);
});
check('original MV plugins can extend database list and alias onLoad',()=>{
    const r=originalManager();r.files['data/Custom.json']=JSON.stringify([null,{note:'<custom>',value:7}]);
    r.run(`globalThis.customCalls=0;DataManager._databaseFiles.push({name:'$custom',src:'Custom.json'});
        const original=DataManager.onLoad;DataManager.onLoad=function(object){original.call(this,object);if(object===window.$custom)customCalls++;};
        DataManager.loadDatabase();`);
    r.tick();assert.equal(r.run('DataManager.isDatabaseLoaded()'),true);
    assert.equal(r.run('$custom[1].value'),7);assert.equal(r.run('$custom[1].meta.custom'),true);assert.equal(r.run('customCalls'),1);
});
check('original MV missing database surfaces its own load error',()=>{
    const r=originalManager();delete r.files['data/Actors.json'];r.run('DataManager.loadDatabase();');r.tick();
    assert.throws(()=>r.run('DataManager.isDatabaseLoaded()'),/Failed to load: data\/Actors.json/);
});
check('original MV malformed database JSON is not silently replaced',()=>{
    const r=originalManager();r.files['data/Actors.json']='{bad';r.run('DataManager.loadDatabase();');
    assert.throws(()=>r.tick(),/JSON|Unexpected|property/i);
});
console.log(`Native game-data JavaScript: ${passed}/${passed} passed (Node; mocked storage/timers, not .NET or full game).`);
