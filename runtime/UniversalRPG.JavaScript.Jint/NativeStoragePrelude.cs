namespace UniversalRPG.JavaScript.Jint;

/// <summary>Installs the method surface used by MV/MZ web-mode StorageManager.</summary>
internal static class NativeStoragePrelude
{
    internal const string Source = """
        ((native) => {
            'use strict';
            const root=globalThis;
            if(Object.prototype.hasOwnProperty.call(root,'localStorage')) throw new Error('storage.host-conflict');
            const call=(op,key='',value='')=>{
                const result=JSON.parse(native(op,String(key),String(value)));
                if(!result.success) throw new Error(result.errorCode||'storage.failure');
                return result;
            };
            const storage={
                getItem(key){const r=call('get',key);return r.found?r.value:null;},
                setItem(key,value){call('set',key,value);},
                removeItem(key){call('remove',key);},
                clear(){call('clear');},
                key(index){const keys=call('keys').keys;const n=Number(index)|0;return n>=0&&n<keys.length?keys[n]:null;}
            };
            Object.defineProperty(storage,'length',{enumerable:true,get(){return call('keys').keys.length;}});
            Object.defineProperty(root,'localStorage',{value:storage,writable:false,configurable:false,enumerable:true});
        })
        """;
}
