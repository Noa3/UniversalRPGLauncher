using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Small lifecycle/DOM surface required to execute the original MV/MZ entry
/// point after its declared project scripts have already been loaded. This is
/// not a renderer or browser: script tags can only acknowledge already-loaded
/// sources, canvas contexts are unavailable, and unknown dynamic scripts fail.
/// </summary>
public static class NativeEntryPointHostPrelude
{
    public const string ModuleId = "compat:native-entry-host";
    public const string ControlTarget = "__urpgEntryHost";
    public const int MaxKnownScripts = 256;

    // JS_NATIVE_ENTRY_HOST_BEGIN
    internal const string FactorySource = """
        ((knownPaths) => {
            'use strict';
            const root = globalThis;
            const sourceDocument = root.document;
            const apply = Reflect.apply;
            const define = Object.defineProperty;
            const create = Object.create;
            const freeze = Object.freeze;
            const ErrorType = Error;
            const fail = code => { throw new ErrorType(code); };
            if (!sourceDocument || typeof root.setTimeout !== 'function') fail('entry.frame-host-required');
            if (!Array.isArray(knownPaths) || knownPaths.length > 256) fail('entry.script-plan-invalid');
            if (Object.prototype.hasOwnProperty.call(root, '__urpgEntryHost')) fail('entry.host-conflict');

            const normalize = value => {
                if (typeof value !== 'string' || value.length === 0 || value.length > 4096) return null;
                let path = value;
                if (path.startsWith('urpg://game/')) path = path.slice('urpg://game/'.length);
                while (path.startsWith('./')) path = path.slice(2);
                if (/[\\?#\x00-\x1f\x7f]/.test(path)) return null;
                const parts = path.split('/');
                if (parts.some(part => !part || part === '.' || part === '..' || /[. ]$/.test(part))) return null;
                return parts.join('/');
            };
            const known = new Set();
            for (const item of knownPaths) {
                const path = normalize(item);
                if (!path) fail('entry.script-plan-invalid');
                const key = path.toLowerCase();
                if (known.has(key)) fail('entry.script-plan-duplicate');
                known.add(key);
            }

            const windowListeners = create(null);
            const documentListeners = create(null);
            const addListener = (table, type, callback, options) => {
                if (typeof type !== 'string' || typeof callback !== 'function') fail('entry.listener-invalid');
                if (type.length > 64) fail('entry.listener-invalid');
                const list = table[type] || (table[type] = []);
                if (list.some(item => item.callback === callback)) return;
                if (list.length >= 128) fail('entry.listener-limit');
                list.push({ callback, once: !!(options && options.once) });
            };
            const removeListener = (table, type, callback) => {
                const list = table[type];
                if (!list) return;
                table[type] = list.filter(item => item.callback !== callback);
            };
            const emit = (table, type, receiver, event) => {
                const list = (table[type] || []).slice();
                for (const item of list) {
                    const live = table[type] || [];
                    if (!live.some(entry => entry.callback === item.callback)) continue;
                    if (item.once) removeListener(table, type, item.callback);
                    apply(item.callback, receiver, [event]);
                }
            };

            const ids = create(null);
            const escapeAttr = text => String(text).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;');
            const makeElement = tagName => {
                const tag = String(tagName).toUpperCase();
                const element = {
                    tagName: tag,
                    nodeName: tag,
                    nodeType: 1,
                    parentNode: null,
                    children: [],
                    style: create(null),
                    type: '', src: '', async: true, defer: false,
                    onload: null, onerror: null, onclick: null, ontouchstart: null,
                    _url: '', _id: '', _innerHTML: '',
                    appendChild(child) {
                        if (!child || typeof child !== 'object') fail('entry.dom-child-invalid');
                        if (child.parentNode && child.parentNode !== this) child.parentNode.removeChild(child);
                        if (this.children.indexOf(child) < 0) this.children.push(child);
                        child.parentNode = this;
                        if (child.id) ids[child.id] = child;
                        if (child.tagName === 'SCRIPT') scheduleScript(child);
                        return child;
                    },
                    removeChild(child) {
                        const index = this.children.indexOf(child);
                        if (index < 0) fail('entry.dom-child-missing');
                        this.children.splice(index, 1);
                        if (child.id && ids[child.id] === child) delete ids[child.id];
                        child.parentNode = null;
                        return child;
                    },
                    addEventListener(type, callback, options = false) {
                        this.__listeners ||= create(null);
                        addListener(this.__listeners, type, callback, options);
                    },
                    removeEventListener(type, callback) {
                        if (this.__listeners) removeListener(this.__listeners, type, callback);
                    },
                    getContext() { return null; }
                };
                define(element, 'id', {
                    enumerable: true, configurable: true,
                    get() { return this._id; },
                    set(value) {
                        const next = String(value);
                        if (this._id && ids[this._id] === this) delete ids[this._id];
                        this._id = next;
                        if (next && this.parentNode) ids[next] = this;
                    }
                });
                define(element, 'innerHTML', {
                    enumerable: true, configurable: true,
                    get() { return this._innerHTML; }, set(value) { this._innerHTML = String(value); }
                });
                define(element, 'outerHTML', {
                    enumerable: true, configurable: true,
                    get() {
                        const name = tag.toLowerCase();
                        const id = this.id ? ` id="${escapeAttr(this.id)}"` : '';
                        return `<${name}${id}>${this.innerHTML}</${name}>`;
                    }
                });
                return element;
            };
            const body = makeElement('body');
            const head = makeElement('head');
            const html = makeElement('html');
            html.appendChild(head); html.appendChild(body);
            const listOf = element => {
                const list = [element];
                list.item = index => list[index] || null;
                return list;
            };
            const scheduleScript = element => {
                const normalized = normalize(element.src || element._url || '');
                const success = normalized && known.has(normalized.toLowerCase());
                root.setTimeout(() => {
                    const event = freeze({ type: success ? 'load' : 'error', target: element, currentTarget: element });
                    const handler = success ? element.onload : element.onerror;
                    if (typeof handler === 'function') apply(handler, element, [event]);
                    if (element.__listeners) emit(element.__listeners, event.type, element, event);
                }, 0);
            };

            const documentSurface = create(null);
            define(documentSurface, 'currentScript', { enumerable: true, get: () => sourceDocument.currentScript });
            define(documentSurface, 'body', { value: body, enumerable: true });
            define(documentSurface, 'head', { value: head, enumerable: true });
            define(documentSurface, 'documentElement', { value: html, enumerable: true });
            define(documentSurface, 'fonts', { value: null, enumerable: true });
            define(documentSurface, 'hidden', { value: false, enumerable: true });
            define(documentSurface, 'visibilityState', { value: 'visible', enumerable: true });
            documentSurface.createElement = tag => makeElement(tag);
            documentSurface.getElementById = id => ids[String(id)] || null;
            documentSurface.getElementsByTagName = name => {
                const tag = String(name).toLowerCase();
                if (tag === 'head') return listOf(head);
                if (tag === 'body') return listOf(body);
                return Object.assign([], { item: () => null });
            };
            documentSurface.addEventListener = (type, callback, options = false) => addListener(documentListeners, type, callback, options);
            documentSurface.removeEventListener = (type, callback) => removeListener(documentListeners, type, callback);

            root.addEventListener = (type, callback, options = false) => addListener(windowListeners, type, callback, options);
            root.removeEventListener = (type, callback) => removeListener(windowListeners, type, callback);
            root.document = documentSurface;
            if (!('navigator' in root)) root.navigator = freeze({ userAgent: 'UniversalRPG', platform: 'UniversalRPG', language: 'en', getGamepads: () => [] });
            if (!('location' in root)) root.location = { href: 'urpg://game/index.html', pathname: '/index.html', search: '', hash: '' };
            if (!('innerWidth' in root)) root.innerWidth = 816;
            if (!('innerHeight' in root)) root.innerHeight = 624;

            let loadDispatched = false;
            const dispatchLoad = () => {
                if (loadDispatched) fail('entry.load-already-dispatched');
                loadDispatched = true;
                const event = freeze({ type: 'load', target: root, currentTarget: root });
                emit(windowListeners, 'load', root, event);
                if (typeof root.onload === 'function') apply(root.onload, root, [event]);
            };
            const dispatchDocument = type => {
                if (typeof type !== 'string' || type.length === 0 || type.length > 64) fail('entry.document-event-invalid');
                const event = freeze({ type, target: documentSurface, currentTarget: documentSurface });
                emit(documentListeners, type, documentSurface, event);
            };
            define(root, '__urpgEntryHost', { value: freeze({ dispatchLoad, dispatchDocument }) });
        })
        """;
    // JS_NATIVE_ENTRY_HOST_END

    public static ScriptModule Build(string languageId, IEnumerable<string> alreadyLoadedPaths)
    {
        if (languageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            throw new ArgumentException("Entry host requires MV or MZ.", nameof(languageId));
        if (alreadyLoadedPaths == null) throw new ArgumentNullException(nameof(alreadyLoadedPaths));
        var paths = alreadyLoadedPaths.Take(MaxKnownScripts + 1).ToArray();
        if (paths.Length > MaxKnownScripts) throw new ArgumentException("Entry host script plan exceeds its bound.", nameof(alreadyLoadedPaths));
        var normalized = new List<string>(paths.Length);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (!LogicalGamePath.TryNormalize(path, out var value))
                throw new ArgumentException("Entry host contains an unsafe script path.", nameof(alreadyLoadedPaths));
            if (!seen.Add(value)) throw new ArgumentException("Entry host contains a case-colliding script path.", nameof(alreadyLoadedPaths));
            normalized.Add(value);
        }
        var source = FactorySource + "(" + JsonSerializer.Serialize(normalized) + ");\n";
        return new ScriptModule
        {
            Descriptor = new EngineScriptDescriptor
            {
                Id = ModuleId, DisplayName = "URPG native entry lifecycle",
                LanguageId = languageId, Origin = ScriptOrigin.CompatibilityShim,
                Required = true, LoadOrder = int.MinValue + 1,
            },
            Source = Encoding.UTF8.GetBytes(source),
        };
    }
}
