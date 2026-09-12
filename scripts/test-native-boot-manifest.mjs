import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';
const text = readFileSync(new URL('../runtime/UniversalRPG.JavaScript.Jint/NativeBootManifestParser.cs', import.meta.url), 'utf8');
const match = text.match(/JS_BOOT_MANIFEST_BEGIN[\s\S]*?FactorySource = """\r?\n([\s\S]*?)\r?\n\s*""";/);
assert.ok(match);
const factory = match[1];
const suffixes = ['core','managers','objects','scenes','sprites','windows'];
const paths = mz => ['js/libs/pixi.js',...suffixes.map(s=>`js/${mz?'rmmz':'rpg'}_${s}.js`),'js/plugins.js'];
const html = list => '<!DOCTYPE html><html><head><meta charset="utf-8"><title>Test</title></head><body>'+list.map(p=>`<script type="text/javascript" src="${p}"></script>`).join('\n')+'</body></html>';
const main = list => `// main.js\nconst scriptUrls = ${JSON.stringify(list)};\nthrow new Error('MUST NEVER EXECUTE MAIN');`;
function parse(mz, index = html(mz?['js/main.js']:[...paths(false),'js/main.js']), entry = main(paths(true))) {
  const realm=vm.createContext({mz,index,entry});
  return JSON.parse(vm.runInContext(`(${factory})(mz,index,entry)`,realm,{timeout:1500}));
}
let tests=0;
function check(name, test){test();tests++;console.log('PASS '+name);}
function bad(mz,index,entry,code){const r=parse(mz,index,entry);assert.equal(r.success,false,JSON.stringify(r)); if(code)assert.match(r.error,new RegExp(code));}
check('MV reads actual static order, deferring main.js',()=>assert.deepEqual(parse(false).paths,paths(false)));
check('MZ reads literal scriptUrls without executing main',()=>assert.deepEqual(parse(true).paths,paths(true)));
check('MZ preserves project-specific libraries before and between cores',()=>{
  const p=paths(true);p.splice(2,0,'js/libs/custom-helper.js');p.push('js/after-config.js');
  assert.deepEqual(parse(true,undefined,main(p)).paths,p);
});
check('MV preserves project-specific scripts rather than substituting fixed defaults',()=>{
 const p=paths(false);p.splice(2,0,'js/custom.js');assert.deepEqual(parse(false,html([...p,'js/main.js'])).paths,p);
});
check('MZ includes classic pre-main index dependencies first',()=>assert.deepEqual(parse(true,html(['js/before-main.js','js/main.js'])).paths,['js/before-main.js',...paths(true)]));
check('HTML comments do not add phantom scripts',()=>assert.equal(parse(false,'<!-- <script src="js/phantom.js"></script> -->'+html([...paths(false),'js/main.js'])).success,true));
check('raw title/style text does not add phantom scripts',()=>assert.deepEqual(parse(false,'<style>/* <script src="js/wrong.js"></script> */</style>'+html([...paths(false),'js/main.js'])).paths,paths(false)));
check('HTML tag and attribute case are insensitive',()=>assert.equal(parse(false,html([...paths(false),'js/main.js']).replaceAll('<script','<SCRIPT').replaceAll('</script','</SCRIPT').replaceAll('src=','SRC=')).success,true));
check('single quoted HTML attributes work',()=>assert.equal(parse(false,html([...paths(false),'js/main.js']).replaceAll('"',"'")).success,true));
check('unquoted HTML source attributes work',()=>assert.equal(parse(false,html([...paths(false),'js/main.js']).replace(/src="([^"]+)"/g,'src=$1')).success,true));
check('one leading relative marker normalizes',()=>assert.deepEqual(parse(false,html([...paths(false).map(p=>'./'+p),'./js/main.js'])).paths,paths(false)));
check('URL-escaped Unicode and internal spaces map once',()=>{
 const p=paths(false);p.splice(1,0,'js/%E6%97%A5%E6%9C%AC%20Helper.js');
 assert.equal(parse(false,html([...p,'js/main.js'])).paths[1],'js/日本 Helper.js');
});
check('MZ comments and strict directive before declaration',()=>assert.equal(parse(true,undefined,'/*header*/\n"use strict";\n'+main(paths(true))).success,true));
check('MZ comments/trailing comma inside literal list',()=>assert.equal(parse(true,undefined,'const scriptUrls = [\n'+paths(true).map(p=>JSON.stringify(p)+', // item\n').join('')+'];').success,true));
check('MZ computed entries rejected, never evaluated',()=>bad(true,undefined,'const scriptUrls = [(()=>{throw Error("executed")})()];','json-strings'));
check('MZ executable prefix is not searched for a later matching declaration',()=>bad(true,undefined,'sideEffect();\n'+main(paths(true)),'literal-required'));
check('MZ string/comment decoys are not mistaken for declarations',()=>bad(true,undefined,'const text = "const scriptUrls = []";\n'+main(paths(true)),'literal-required'));
check('MZ concatenated/mutated declaration expression is rejected',()=>bad(true,undefined,'const scriptUrls = '+JSON.stringify(paths(true))+'.concat(["js/extra.js"]);','dynamic-script-list'));
check('missing required engine core rejected',()=>bad(false,html([...paths(false).filter(p=>!p.includes('objects')),'js/main.js']),undefined,'core-order'));
check('misordered required engine core rejected',()=>{const p=paths(false);[p[2],p[3]]=[p[3],p[2]];bad(false,html([...p,'js/main.js']),undefined,'core-order');});
check('plugin configuration before core rejected',()=>bad(false,html(['js/plugins.js',...paths(false).filter(p=>p!=='js/plugins.js'),'js/main.js']),undefined,'plugin-config-order'));
check('missing config rejected',()=>bad(true,undefined,main(paths(true).slice(0,-1)),'plugin-config-order'));
check('mixed MV and MZ files rejected',()=>bad(true,undefined,main([...paths(true),'js/rpg_core.js']),'mixed-engines'));
check('case-colliding/duplicate script paths rejected instead of skipped',()=>bad(false,html([...paths(false),'js/libs/PIXI.js','js/main.js']),undefined,'duplicate-script'));
check('main must be final index script',()=>bad(false,html(['js/main.js',...paths(false)]),undefined,'main-must-be-last'));
check('second main.js in dependency list rejected',()=>bad(true,undefined,main([...paths(true),'js/main.js']),'duplicate-script'));
check('inline script code is not executed or omitted',()=>bad(false,'<script>globalThis.untrusted=true;</script>'+html([...paths(false),'js/main.js']),undefined,'inline-script'));
check('HTML event handlers rejected',()=>bad(false,'<body onload="evil()">'+html([...paths(false),'js/main.js']),undefined,'inline-event-handler'));
check('HTML base URI rejected',()=>bad(false,'<base href="https://example.com">'+html([...paths(false),'js/main.js']),undefined,'base-url'));
check('async/defer/module/nomodule scheduling rejected',()=>{
 for(const attr of ['async','async="false"','defer','nomodule','type="module"'])bad(false,html([...paths(false),'js/main.js']).replace('type="text/javascript"',attr));
});
check('duplicate src attribute rejected',()=>bad(false,html([...paths(false),'js/main.js']).replace('src="js/libs/pixi.js"','src="js/libs/pixi.js" SRC="js/other.js"'),undefined,'duplicate-attribute'));
check('template/noscript/foreign contexts rejected',()=>{for(const tag of ['template','noscript','textarea','svg'])bad(false,`<${tag}>`+html([...paths(false),'js/main.js'])+`</${tag}>`,undefined,'context-unsupported');});
check('traversal, remote URL, drive, query and fragment paths rejected',()=>{
 for(const path of ['../x.js','js/../x.js','/js/x.js','https://x/y.js','//x/y.js','C:/x.js','js/x.js?x=1','js/x.js#x','js/x.js&amp;x=1','js/%2e%2e/x.js','js/a%2fb.js','js/a%5cb.js','js/%252e%252e/x.js','js/a%00.js','js/COM:foo.js'])
  bad(false,html([path,...paths(false),'js/main.js']));
});
check('truncated comments/tags/scripts fail',()=>{for(const prefix of ['<!--','<script src="js/a.js"','<script src="js/a.js">','<style>'])bad(false,prefix+html([...paths(false),'js/main.js']));});
check('entry source lengths bounded',()=>bad(true,undefined,' '.repeat(524289),'input-limit'));
check('manifest script count bounded',()=>bad(false,html([...Array.from({length:121},(_,i)=>`js/a${i}.js`),...paths(false),'js/main.js']),undefined,'script-limit'));
console.log(`Native boot manifest: ${tests}/${tests} passed (Node; not C#/Jint or a game boot).`);
