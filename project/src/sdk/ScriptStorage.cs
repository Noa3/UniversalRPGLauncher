using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Sdk;

public sealed class ScriptStorageValueResult
{
    private ScriptStorageValueResult(bool success, bool found, string value, string code, string message)
    { Success=success; Found=found; Value=value; ErrorCode=code; ErrorMessage=message; }
    public bool Success { get; }
    public bool Found { get; }
    public string Value { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    public static ScriptStorageValueResult FoundValue(string value)=>new(true,true,value??"","","");
    public static ScriptStorageValueResult Missing()=>new(true,false,"","","");
    public static ScriptStorageValueResult Failed(string code,string message)=>new(false,false,"",code??"storage.failure",message??"Storage read failed.");
}

public sealed class ScriptStorageKeysResult
{
    private ScriptStorageKeysResult(bool success,IReadOnlyList<string> keys,string code,string message)
    {Success=success;Keys=keys;ErrorCode=code;ErrorMessage=message;}
    public bool Success { get; }
    public IReadOnlyList<string> Keys { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    public static ScriptStorageKeysResult Succeeded(IEnumerable<string> keys)=>new(true,(keys??Array.Empty<string>()).ToArray(),"","");
    public static ScriptStorageKeysResult Failed(string code,string message)=>new(false,Array.Empty<string>(),code??"storage.failure",message??"Storage enumeration failed.");
}

/// <summary>
/// App-owned key/value persistence used by engine save/config APIs. Imported
/// scripts never receive native paths or a filesystem object through this API.
/// </summary>
public interface IScriptKeyValueStorage : IDisposable
{
    string Id { get; }
    ScriptStorageValueResult GetItem(string key);
    SdkOperationResult SetItem(string key,string value);
    SdkOperationResult RemoveItem(string key);
    SdkOperationResult Clear();
    ScriptStorageKeysResult GetKeys();
}

/// <summary>Nonpersistent implementation for probes/tests; bounded like the native host.</summary>
public sealed class MemoryScriptKeyValueStorage : IScriptKeyValueStorage
{
    public const int MaxEntries=256;
    public const int MaxKeyCharacters=512;
    public const int MaxValueCharacters=16*1024*1024;
    private readonly Dictionary<string,string> _items=new(StringComparer.Ordinal);
    private bool _disposed;
    public string Id { get; }
    public MemoryScriptKeyValueStorage(string id="memory-script-storage")=>Id=string.IsNullOrWhiteSpace(id)?"memory-script-storage":id;
    public ScriptStorageValueResult GetItem(string key)
    {
        if(_disposed)return ScriptStorageValueResult.Failed("storage.disposed","Storage is disposed.");
        if(!ValidKey(key))return ScriptStorageValueResult.Failed("storage.key-invalid","Storage key is outside the bounded limit.");
        return _items.TryGetValue(key,out var value)?ScriptStorageValueResult.FoundValue(value):ScriptStorageValueResult.Missing();
    }
    public SdkOperationResult SetItem(string key,string value)
    {
        if(_disposed)return SdkOperationResult.Failed("storage.disposed","Storage is disposed.");
        if(!ValidKey(key)||value==null||value.Length>MaxValueCharacters)return SdkOperationResult.Failed("storage.value-invalid","Storage key/value is outside the bounded limit.");
        if(!_items.ContainsKey(key)&&_items.Count>=MaxEntries)return SdkOperationResult.Failed("storage.entry-limit","Storage entry limit reached.");
        _items[key]=value;return SdkOperationResult.Succeeded();
    }
    public SdkOperationResult RemoveItem(string key)
    {
        if(_disposed)return SdkOperationResult.Failed("storage.disposed","Storage is disposed.");
        if(!ValidKey(key))return SdkOperationResult.Failed("storage.key-invalid","Storage key is outside the bounded limit.");
        _items.Remove(key);return SdkOperationResult.Succeeded();
    }
    public SdkOperationResult Clear(){if(_disposed)return SdkOperationResult.Failed("storage.disposed","Storage is disposed.");_items.Clear();return SdkOperationResult.Succeeded();}
    public ScriptStorageKeysResult GetKeys()=>_disposed?ScriptStorageKeysResult.Failed("storage.disposed","Storage is disposed."):ScriptStorageKeysResult.Succeeded(_items.Keys.OrderBy(k=>k,StringComparer.Ordinal));
    public void Dispose(){if(_disposed)return;_disposed=true;_items.Clear();}
    private static bool ValidKey(string key)=>key!=null&&key.Length<=MaxKeyCharacters;
}
