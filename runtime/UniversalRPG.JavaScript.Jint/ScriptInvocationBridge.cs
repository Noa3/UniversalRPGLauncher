using System;
using System.Collections.Generic;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.JavaScript.Jint;

/// <summary>
/// Only primitive values cross this boundary. Method resolution happens inside
/// one constrained JavaScript call, including any user-defined property getter.
/// No CLR object, delegate, or reflection surface is passed to game code.
/// </summary>
internal static class ScriptInvocationBridge
{
    internal const int MaxArguments = 256;
    internal const int MaxStringCharacters = 8192;
    internal const int MaxTotalStringCharacters = 131072;
    private const long MaxSafeInteger = 9007199254740991L;

    // Captured before game code executes. These references cannot be replaced
    // by a plugin overwriting globalThis, JSON.parse, or Reflect.apply.
    // JS_BRIDGE_BEGIN
    internal const string Source = """
        (() => {
            const root = globalThis;
            const parse = JSON.parse;
            const apply = Reflect.apply;
            return function(targetName, memberName, argumentsJson) {
                const receiver = targetName === '' || targetName === 'globalThis'
                    ? root : root[targetName];
                const method = receiver[memberName];
                return apply(method, receiver, parse(argumentsJson));
            };
        })()
        """;
    // JS_BRIDGE_END

    internal static SdkOperationResult SerializeArguments(
        ScriptInvocation request,
        out string argumentsJson)
    {
        argumentsJson = "";
        if (request == null || string.IsNullOrWhiteSpace(request.Member)
            || request.Member.Length > 512 || (request.Target?.Length ?? 0) > 512)
        {
            return SdkOperationResult.Failed("jint.invocation-invalid", "Invocation names must be bounded; a member name is required.");
        }
        if (request.Arguments == null || request.Arguments.Count > MaxArguments)
        {
            return SdkOperationResult.Failed("jint.argument-limit", "Invocation argument count exceeds the bounded limit.");
        }

        var values = new List<object?>(request.Arguments.Count);
        var totalCharacters = 0;
        foreach (var argument in request.Arguments)
        {
            var value = argument?.Value;
            switch (value)
            {
                case null:
                case bool:
                case byte:
                case sbyte:
                case short:
                case ushort:
                case int:
                case uint:
                    break;
                case long integer when integer >= -MaxSafeInteger && integer <= MaxSafeInteger:
                    break;
                case ulong integer when integer <= (ulong)MaxSafeInteger:
                    break;
                case float number when float.IsFinite(number):
                    break;
                case double number when double.IsFinite(number):
                    break;
                case string text:
                    if (text.Length > MaxStringCharacters
                        || text.Length > MaxTotalStringCharacters - totalCharacters)
                    {
                        return SdkOperationResult.Failed("jint.argument-limit", "Invocation strings exceed the bounded limit.");
                    }
                    totalCharacters += text.Length;
                    break;
                default:
                    return SdkOperationResult.Failed(
                        "jint.argument-type",
                        "Only null, booleans, bounded strings, finite numbers, and exactly representable integers may cross into JavaScript.");
            }
            values.Add(value);
        }
        argumentsJson = JsonSerializer.Serialize(values);
        return SdkOperationResult.Succeeded();
    }
}
