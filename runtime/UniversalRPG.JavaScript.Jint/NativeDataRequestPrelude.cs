namespace UniversalRPG.JavaScript.Jint;

/// <summary>
/// Minimal local-data XMLHttpRequest adapter for MV/MZ DataManager. Not an HTTP
/// client or general browser implementation. The private native callback only
/// returns bounded UTF-8 JSON text from the authorized VFS data/ mount.
/// </summary>
internal static class NativeDataRequestPrelude
{
    // JS_NATIVE_DATA_BEGIN
    internal const string Source = """
        ((readEnvelope) => {
            'use strict';
            const root = globalThis;
            const parse = JSON.parse;
            const apply = Reflect.apply;
            const define = Object.defineProperty;
            const slice = Array.prototype.slice;
            const ErrorType = Error;
            const states = new WeakMap();
            const events = ['readystatechange','loadstart','load','error','abort','loadend'];
            let pendingCount = 0;
            const fail = code => { throw new ErrorType(code); };
            if (typeof readEnvelope !== 'function' || 'XMLHttpRequest' in root)
                fail('data.host-conflict');
            const stateOf = object => {
                const state = states.get(object);
                if (!state) fail('data.invalid-receiver');
                return state;
            };
            const emit = (self, state, type) => {
                const event = Object.freeze({ type, target: self, currentTarget: self, code: state.errorCode });
                const handler = self['on' + type];
                if (typeof handler === 'function') apply(handler, self, [event]);
                const listeners = apply(slice, state.listeners, []);
                for (let i = 0; i < listeners.length; i++) {
                    const listener = listeners[i];
                    if (listener.type !== type || state.listeners.indexOf(listener) < 0) continue;
                    if (listener.once) state.listeners.splice(state.listeners.indexOf(listener), 1);
                    apply(listener.callback, self, [event]);
                }
            };
            const release = state => {
                if (state.pending) { state.pending = false; pendingCount--; }
                state.timer = null;
            };
            const cancel = state => {
                const timer = state.timer;
                release(state);
                state.generation++;
                state.sending = false;
                if (timer !== null) root.clearTimeout(timer);
            };
            const clearResponse = state => {
                state.status = 0; state.text = ''; state.response = null;
                state.responseURL = ''; state.errorCode = '';
            };
            const changeState = (self, state, value) => {
                state.readyState = value;
                emit(self, state, 'readystatechange');
            };
            class LocalDataRequest {
                constructor() {
                    states.set(this, {
                        readyState: 0, status: 0, text: '', response: null, responseURL: '',
                        responseType: '', mime: 'application/json', url: '', errorCode: '',
                        generation: 0, sending: false, pending: false, timer: null, listeners: []
                    });
                    for (let i = 0; i < events.length; i++) this['on' + events[i]] = null;
                }
                open(method, url, async = true, username = null, password = null) {
                    const state = stateOf(this);
                    if (typeof method !== 'string' || method.toUpperCase() !== 'GET') fail('data.get-only');
                    if (async !== true) fail('data.async-only');
                    if (username !== null || password !== null) fail('data.credentials-unsupported');
                    if (typeof url !== 'string' || url.length === 0 || url.length > 4096) fail('data.invalid-url');
                    cancel(state);
                    clearResponse(state);
                    state.url = url;
                    changeState(this, state, 1);
                }
                send(body = null) {
                    const state = stateOf(this);
                    if (state.readyState !== 1 || state.sending) fail('data.invalid-send-state');
                    if (body !== null && body !== undefined) fail('data.request-body-unsupported');
                    if (typeof root.setTimeout !== 'function' || typeof root.clearTimeout !== 'function')
                        fail('data.frame-host-required');
                    if (pendingCount >= 128) fail('data.pending-limit');
                    const generation = state.generation;
                    state.sending = true;
                    state.pending = true;
                    pendingCount++;
                    try {
                        emit(this, state, 'loadstart');
                        if (generation !== state.generation) return;
                        state.timer = root.setTimeout(() => {
                            if (generation !== state.generation || !state.sending) return;
                            release(state);
                            // The native function receives a primitive string, never
                            // this object, handlers, the VM, or a host path object.
                            const envelope = parse(readEnvelope(state.url));
                            if (!envelope || typeof envelope.success !== 'boolean') fail('data.invalid-response');
                            if (!envelope.success) {
                                clearResponse(state);
                                state.errorCode = typeof envelope.errorCode === 'string' ? envelope.errorCode : 'data.read-failed';
                                state.sending = false;
                                changeState(this, state, 4);
                                if (generation !== state.generation) return;
                                emit(this, state, 'error');
                                if (generation === state.generation) emit(this, state, 'loadend');
                                return;
                            }
                            if (typeof envelope.text !== 'string') fail('data.invalid-response');
                            state.status = 200;
                            state.responseURL = state.url;
                            changeState(this, state, 2);
                            if (generation !== state.generation) return;
                            state.text = envelope.text;
                            if (state.text.length !== 0) {
                                changeState(this, state, 3);
                                if (generation !== state.generation) return;
                            }
                            if (state.responseType === 'json') {
                                try { state.response = parse(state.text); }
                                catch (_) { state.response = null; }
                            } else state.response = state.text;
                            state.sending = false;
                            changeState(this, state, 4);
                            if (generation !== state.generation) return;
                            emit(this, state, 'load');
                            if (generation === state.generation) emit(this, state, 'loadend');
                        }, 0);
                    } catch (error) {
                        if (generation === state.generation) cancel(state);
                        throw error;
                    }
                }
                abort() {
                    const state = stateOf(this);
                    const active = state.sending;
                    cancel(state);
                    const generation = state.generation;
                    clearResponse(state);
                    if (active) {
                        changeState(this, state, 4);
                        if (generation !== state.generation) return;
                        emit(this, state, 'abort');
                        if (generation !== state.generation) return;
                        emit(this, state, 'loadend');
                    }
                    if (generation === state.generation) state.readyState = 0;
                }
                overrideMimeType(mime) {
                    const state = stateOf(this);
                    if (state.readyState >= 3) fail('data.invalid-mime-state');
                    if (typeof mime !== 'string' || mime.length > 128) fail('data.invalid-mime');
                    if (mime !== 'application/json' && mime !== 'text/plain' && mime !== 'text/plain; charset=utf-8')
                        fail('data.mime-unsupported');
                    state.mime = mime;
                }
                setRequestHeader() { fail('data.http-headers-unsupported'); }
                getResponseHeader(name) {
                    const state = stateOf(this);
                    return state.status === 200 && typeof name === 'string' && name.toLowerCase() === 'content-type'
                        ? 'application/json; charset=utf-8' : null;
                }
                getAllResponseHeaders() {
                    return stateOf(this).status === 200 ? 'content-type: application/json; charset=utf-8\r\n' : '';
                }
                addEventListener(type, callback, options = false) {
                    const state = stateOf(this);
                    if (events.indexOf(type) < 0 || typeof callback !== 'function') fail('data.listener-unsupported');
                    const once = !!(options && options.once);
                    if (state.listeners.some(item => item.type === type && item.callback === callback)) return;
                    if (state.listeners.length >= 64) fail('data.listener-limit');
                    state.listeners.push({ type, callback, once });
                }
                removeEventListener(type, callback) {
                    const state = stateOf(this);
                    state.listeners = state.listeners.filter(item => item.type !== type || item.callback !== callback);
                }
                get readyState() { return stateOf(this).readyState; }
                get status() { return stateOf(this).status; }
                get statusText() { return stateOf(this).status === 200 ? 'OK' : ''; }
                get responseURL() { return stateOf(this).responseURL; }
                get urpgErrorCode() { return stateOf(this).errorCode; }
                get responseText() {
                    const state = stateOf(this);
                    if (state.responseType === 'json') fail('data.response-is-json');
                    return state.text;
                }
                get response() { return stateOf(this).response; }
                get responseType() { return stateOf(this).responseType; }
                set responseType(value) {
                    const state = stateOf(this);
                    if (state.sending || state.readyState >= 3) fail('data.invalid-response-type-state');
                    if (value !== '' && value !== 'text' && value !== 'json') fail('data.response-type-unsupported');
                    state.responseType = value;
                }
                get timeout() { return 0; }
                set timeout(value) { if (value !== 0) fail('data.timeout-unsupported'); }
                get withCredentials() { return false; }
                set withCredentials(value) { if (value !== false) fail('data.credentials-unsupported'); }
            }
            const names = ['UNSENT', 'OPENED', 'HEADERS_RECEIVED', 'LOADING', 'DONE'];
            for (let i = 0; i < names.length; i++) {
                define(LocalDataRequest, names[i], { value: i });
                define(LocalDataRequest.prototype, names[i], { value: i });
            }
            define(root, 'XMLHttpRequest', { value: LocalDataRequest, writable: true, configurable: true });
        })
        """;
    // JS_NATIVE_DATA_END
}
