using System;
using System.Collections.Generic;
using System.Text.Json;
using global::Jint;
using global::Jint.Native;

namespace UniversalRPG.JavaScript.Jint;

public sealed class NativeBootManifest
{
    public bool Success { get; init; }
    public string Error { get; init; } = "";
    public IReadOnlyList<string> Paths { get; init; } = Array.Empty<string>();
    public string DeferredEntryPoint { get; init; } = "js/main.js";
}

/// <summary>
/// Reads a conservative static startup manifest. Only this trusted parser is
/// evaluated; index.html, main.js and the discovered sources are never executed
/// by inspection. This extracts dependencies, NOT main.js startup behavior.
/// </summary>
public static class NativeBootManifestParser
{
    public const int MaxStartupCharacters = 524288;
    public const int MaxScripts = 120;

    // JS_BOOT_MANIFEST_BEGIN
    internal const string FactorySource = """
        ((mz, html, mainSource) => {
            'use strict';
            const fail = message => { throw new Error(message); };
            try {
                if (typeof mz !== 'boolean' || typeof html !== 'string' || typeof mainSource !== 'string'
                    || html.length > 524288 || mainSource.length > 524288) fail('boot.manifest.input-limit');
                const space = c => c !== undefined && /\s/.test(c);
                const nameChar = c => c !== undefined && /[A-Za-z0-9_:.-]/.test(c);
                const indexPaths = [];
                let i = html.charCodeAt(0) === 0xfeff ? 1 : 0;
                while (i < html.length) {
                    if (html[i] !== '<') { i++; continue; }
                    if (html.startsWith('<!--', i)) {
                        const end = html.indexOf('-->', i + 4);
                        if (end < 0) fail('boot.manifest.unterminated-comment');
                        i = end + 3; continue;
                    }
                    if (/^<!doctype\s/i.test(html.slice(i, i + 12))) {
                        const end = html.indexOf('>', i + 2);
                        if (end < 0) fail('boot.manifest.unterminated-doctype');
                        i = end + 1; continue;
                    }
                    i++;
                    const closing = html[i] === '/';
                    if (closing) i++;
                    const start = i;
                    while (nameChar(html[i])) i++;
                    if (start === i) fail('boot.manifest.unsupported-markup');
                    const tag = html.slice(start, i).toLowerCase();
                    const attrs = Object.create(null);
                    let selfClosing = false;
                    while (i < html.length) {
                        while (space(html[i])) i++;
                        if (html[i] === '>') { i++; break; }
                        if (html[i] === '/' && html[i + 1] === '>') { i += 2; selfClosing = true; break; }
                        const keyStart = i;
                        while (nameChar(html[i])) i++;
                        if (keyStart === i) fail('boot.manifest.invalid-attribute');
                        const key = html.slice(keyStart, i).toLowerCase();
                        if (Object.hasOwn(attrs, key)) fail('boot.manifest.duplicate-attribute');
                        if (key.startsWith('on')) fail('boot.manifest.inline-event-handler');
                        while (space(html[i])) i++;
                        let value = '';
                        if (html[i] === '=') {
                            i++; while (space(html[i])) i++;
                            const quote = html[i];
                            if (quote === '"' || quote === "'") {
                                const valueStart = ++i;
                                while (i < html.length && html[i] !== quote) i++;
                                if (i === html.length) fail('boot.manifest.unterminated-attribute');
                                value = html.slice(valueStart, i++);
                            } else {
                                const valueStart = i;
                                while (i < html.length && !space(html[i]) && html[i] !== '>') i++;
                                value = html.slice(valueStart, i);
                                if (!value || /["'<=`]/.test(value)) fail('boot.manifest.invalid-attribute');
                            }
                        }
                        attrs[key] = value;
                    }
                    if (i > html.length || html[i - 1] !== '>') fail('boot.manifest.unterminated-tag');
                    if (closing) continue;
                    if (tag === 'base') fail('boot.manifest.base-url-unsupported');
                    // These have special parsing/execution contexts. Do not mistake
                    // a script-looking string inside one for an active script tag.
                    if (['template','noscript','iframe','textarea','xmp','plaintext','svg','math','object'].includes(tag))
                        fail('boot.manifest.embedded-context-unsupported');
                    if (tag === 'style' || tag === 'title') {
                        const pattern = new RegExp('</' + tag + '\\s*>', 'ig'); pattern.lastIndex = i;
                        const end = pattern.exec(html);
                        if (!end) fail('boot.manifest.unterminated-raw-text');
                        i = pattern.lastIndex; continue;
                    }
                    if (tag !== 'script') continue;
                    if (selfClosing) fail('boot.manifest.self-closing-script');
                    const endPattern = /<\/script\s*>/ig; endPattern.lastIndex = i;
                    const end = endPattern.exec(html);
                    if (!end) fail('boot.manifest.unterminated-script');
                    const body = html.slice(i, end.index); i = endPattern.lastIndex;
                    if (body.trim()) fail('boot.manifest.inline-script-unsupported');
                    if (!Object.hasOwn(attrs, 'src') || !attrs.src) fail('boot.manifest.script-src-required');
                    if (Object.hasOwn(attrs,'async') || Object.hasOwn(attrs,'defer') || Object.hasOwn(attrs,'nomodule'))
                        fail('boot.manifest.script-scheduling-unsupported');
                    const type = (attrs.type || 'text/javascript').toLowerCase();
                    if (type !== 'text/javascript' && type !== 'application/javascript') fail('boot.manifest.script-type-unsupported');
                    indexPaths.push(attrs.src);
                    if (indexPaths.length > 120) fail('boot.manifest.script-limit');
                }

                const safePath = source => {
                    if (typeof source !== 'string' || source.length === 0 || source.length > 2048)
                        fail('boot.manifest.path-limit');
                    // Decode URI paths once; never decode them again at the VFS.
                    // Query strings, remote URLs and HTML entities are explicit
                    // unsupported cases rather than a guessed filename mapping.
                    if (/[&?#\\\s]/.test(source) || /%(?:2f|5c)/i.test(source)) fail('boot.manifest.unsafe-url');
                    let path;
                    try { path = decodeURIComponent(source); } catch (_) { fail('boot.manifest.bad-url-encoding'); }
                    if (path.startsWith('./')) path = path.slice(2);
                    if (!path.startsWith('js/') || !path.endsWith('.js') || /[\x00-\x1f\x7f\\:%?#]/.test(path))
                        fail('boot.manifest.unsafe-path');
                    const parts = path.split('/');
                    if (parts.some(p => !p || p === '.' || p === '..' || /[. ]$/.test(p))) fail('boot.manifest.unsafe-path');
                    return path;
                };
                const ordered = indexPaths.map(safePath);
                if (ordered.length === 0 || ordered[ordered.length - 1] !== 'js/main.js')
                    fail('boot.manifest.main-must-be-last');
                ordered.pop();

                if (mz) {
                    // Read only a leading literal declaration, allowing whitespace
                    // and comments. No regex search through arbitrary executable JS.
                    let at = mainSource.charCodeAt(0) === 0xfeff ? 1 : 0;
                    const skip = () => {
                        while (at < mainSource.length) {
                            if (space(mainSource[at])) { at++; continue; }
                            if (mainSource.startsWith('//', at)) {
                                const end = mainSource.indexOf('\n', at + 2); at = end < 0 ? mainSource.length : end + 1; continue;
                            }
                            if (mainSource.startsWith('/*', at)) {
                                const end = mainSource.indexOf('*/', at + 2);
                                if (end < 0) fail('boot.manifest.main-comment');
                                at = end + 2; continue;
                            }
                            break;
                        }
                    };
                    skip();
                    for (const directive of ['"use strict";', "'use strict';"]) {
                        if (mainSource.startsWith(directive, at)) { at += directive.length; break; }
                    }
                    skip();
                    const declaration = /^(?:const|let|var)\s+scriptUrls\s*=/.exec(mainSource.slice(at));
                    if (!declaration) fail('boot.manifest.mz-literal-required');
                    at += declaration[0].length; skip();
                    if (mainSource[at++] !== '[') fail('boot.manifest.mz-literal-required');
                    while (true) {
                        skip();
                        if (mainSource[at] === ']') { at++; break; }
                        if (mainSource[at] !== '"') fail('boot.manifest.mz-json-strings-required');
                        const start = at++;
                        while (at < mainSource.length) {
                            if (mainSource[at] === '\\') { at += 2; continue; }
                            if (mainSource[at++] === '"') break;
                        }
                        let item;
                        try { item = JSON.parse(mainSource.slice(start, at)); } catch (_) { fail('boot.manifest.invalid-string'); }
                        ordered.push(safePath(item));
                        if (ordered.length > 120) fail('boot.manifest.script-limit');
                        skip();
                        if (mainSource[at] === ',') { at++; continue; }
                        if (mainSource[at] === ']') { at++; break; }
                        fail('boot.manifest.dynamic-script-list');
                    }
                    skip();
                    if (mainSource[at] !== ';') fail('boot.manifest.dynamic-script-list');
                }
                const seen = new Set();
                for (const path of ordered) {
                    const key = path.toLowerCase();
                    if (key === 'js/main.js' || seen.has(key)) fail('boot.manifest.duplicate-script');
                    seen.add(key);
                }
                const prefix = mz ? 'rmmz_' : 'rpg_';
                const other = mz ? 'rpg_' : 'rmmz_';
                const families = ['core','managers','objects','scenes','sprites','windows'];
                let last = -1;
                for (const family of families) {
                    const slot = ordered.indexOf('js/' + prefix + family + '.js');
                    if (slot <= last) fail('boot.manifest.core-order-or-file');
                    last = slot;
                    if (ordered.includes('js/' + other + family + '.js')) fail('boot.manifest.mixed-engines');
                }
                if (ordered.indexOf('js/plugins.js') <= last) fail('boot.manifest.plugin-config-order');
                if (ordered.length > 120) fail('boot.manifest.script-limit');
                return JSON.stringify({ success: true, paths: ordered, error: '' });
            } catch (error) {
                return JSON.stringify({ success: false, paths: [], error: String(error.message) });
            }
        })
        """;
    // JS_BOOT_MANIFEST_END

    public static NativeBootManifest Parse(bool mz, string html, string mainSource)
    {
        if (html == null || mainSource == null || html.Length > MaxStartupCharacters || mainSource.Length > MaxStartupCharacters)
            return Failure("boot.manifest.input-limit");
        try
        {
            var options = new Options().LimitMemory(64L * 1024 * 1024)
                .TimeoutInterval(TimeSpan.FromSeconds(1)).MaxStatements(8_000_000)
                .LimitRecursion(128).DisableStringCompilation();
            options.Interop.Enabled = false;
            using var engine = new Engine(options);
            var parser = engine.Evaluate(FactorySource);
            var json = engine.Invoke(parser, mz, html, mainSource).AsString();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.GetProperty("success").GetBoolean()) return Failure(root.GetProperty("error").GetString() ?? "boot.manifest.invalid");
            var paths = new List<string>();
            foreach (var item in root.GetProperty("paths").EnumerateArray()) paths.Add(item.GetString()!);
            return new NativeBootManifest { Success = true, Paths = paths.AsReadOnly() };
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            return Failure("boot.manifest.parser-failed: " + exception.GetType().Name);
        }
    }

    private static NativeBootManifest Failure(string error) => new() { Error = error };
}
