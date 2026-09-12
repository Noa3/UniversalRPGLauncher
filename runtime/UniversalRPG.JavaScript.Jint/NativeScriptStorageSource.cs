using System;
using System.Linq;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.JavaScript.Jint;

/// <summary>Primitive-envelope bridge around an app-owned save/config store.</summary>
internal sealed class NativeScriptStorageSource
{
    private readonly IScriptKeyValueStorage _storage;
    private readonly Func<bool> _allowed;

    public NativeScriptStorageSource(IScriptKeyValueStorage storage, Func<bool> allowed)
    {
        _storage=storage??throw new ArgumentNullException(nameof(storage));
        _allowed=allowed??throw new ArgumentNullException(nameof(allowed));
    }

    internal string Invoke(string operation,string key,string value)
    {
        if(!_allowed()) return Failure("storage.denied");
        try
        {
            switch(operation)
            {
                case "get":
                {
                    var result=_storage.GetItem(key);
                    return result.Success
                        ? JsonSerializer.Serialize(new{success=true,found=result.Found,value=result.Value,keys=Array.Empty<string>(),errorCode=""})
                        : Failure(result.ErrorCode);
                }
                case "set":
                {
                    var result=_storage.SetItem(key,value);
                    return result.Success?Success():Failure(result.ErrorCode);
                }
                case "remove":
                {
                    var result=_storage.RemoveItem(key);
                    return result.Success?Success():Failure(result.ErrorCode);
                }
                case "clear":
                {
                    var result=_storage.Clear();
                    return result.Success?Success():Failure(result.ErrorCode);
                }
                case "keys":
                {
                    var result=_storage.GetKeys();
                    return result.Success
                        ? JsonSerializer.Serialize(new{success=true,found=false,value="",keys=result.Keys.Take(MemoryScriptKeyValueStorage.MaxEntries).ToArray(),errorCode=""})
                        : Failure(result.ErrorCode);
                }
                default:return Failure("storage.operation-invalid");
            }
        }
        catch(Exception exception) when(exception is not OutOfMemoryException and not StackOverflowException)
        {
            return Failure("storage.provider-failed");
        }
    }

    internal static string Failure(string code)=>JsonSerializer.Serialize(new{success=false,found=false,value="",keys=Array.Empty<string>(),errorCode=code??"storage.failure"});
    private static string Success()=>JsonSerializer.Serialize(new{success=true,found=false,value="",keys=Array.Empty<string>(),errorCode=""});
}
