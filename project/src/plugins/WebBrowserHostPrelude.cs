using System;
using System.Text;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>Limits for the opt-in, frame-pumped MV/MZ compatibility subset.</summary>
public sealed class WebBrowserHostOptions
{
    public int MaxPendingTimers { get; init; } = 2048;
    public int MaxPendingAnimationFrames { get; init; } = 2048;
    public int MaxCallbacksPerFrame { get; init; } = 4096;
    public int MaxTimerArguments { get; init; } = 32;
    public int MaxFrameMilliseconds { get; init; } = 1000;

    public SdkOperationResult Validate()
    {
        if (MaxPendingTimers is < 1 or > 4096 || MaxPendingAnimationFrames is < 1 or > 4096
            || MaxCallbacksPerFrame is < 1 or > 8192 || MaxTimerArguments is < 0 or > 256
            || MaxFrameMilliseconds is < 1 or > 60000)
            return SdkOperationResult.Failed("web.host.invalid-limits", "Browser-host limits are outside their bounded ranges.");
        return SdkOperationResult.Succeeded();
    }
}

/// <summary>
/// Real timer/animation scheduling and script metadata, not a fake browser.
/// No DOM rendering, I/O, network, Node or CLR objects are supplied. The host
/// explicitly advances virtual milliseconds; no operating-system timer runs.
/// </summary>
public static class WebBrowserHostPrelude
{
    public const string ModuleId = "compat:browser-frame-host";
    public const string ControlTarget = "__urpgBrowserHost";

    // JS_BROWSER_HOST_BEGIN
    internal const string FactorySource = """
        ((limits) => {
            'use strict';
            const root = globalThis;
            const apply = Reflect.apply;
            const create = Object.create;
            const define = Object.defineProperty;
            const freeze = Object.freeze;
            const keys = Object.keys;
            const descriptor = Object.getOwnPropertyDescriptor;
            const sort = Array.prototype.sort;
            const toNumber = Number;
            const finite = Number.isFinite;
            const HostError = Error;
            const ownLimits = {
                timers: limits.timers, frames: limits.frames, callbacks: limits.callbacks,
                arguments: limits.arguments, delta: limits.delta
            };
            const fail = (code) => { throw new HostError(code); };
            const validLimit = (n, minimum, maximum) => typeof n === 'number'
                && finite(n) && n === (n | 0) && n >= minimum && n <= maximum;
            if (!validLimit(ownLimits.timers, 1, 4096) || !validLimit(ownLimits.frames, 1, 4096)
                || !validLimit(ownLimits.callbacks, 1, 8192) || !validLimit(ownLimits.arguments, 0, 256)
                || !validLimit(ownLimits.delta, 1, 60000)) fail('web.host.invalid-limits');
            const reserved = ['__urpgBrowserHost', 'window', 'self', 'performance', 'document',
                'setTimeout', 'clearTimeout', 'setInterval', 'clearInterval',
                'requestAnimationFrame', 'cancelAnimationFrame'];
            for (let i = 0; i < reserved.length; i++) {
                if (descriptor(root, reserved[i])) fail('web.host.global-conflict: ' + reserved[i]);
            }

            let timers = create(null);
            let frames = create(null);
            let timerCount = 0;
            let frameCount = 0;
            let nextId = 1;
            let now = 0;
            let nesting = 0;
            let pumping = false;
            let faulted = false;
            let currentScript = null;
            const ensureLive = () => { if (faulted) fail('web.host.faulted'); };
            const allocateId = () => {
                if (nextId > 2147483647) fail('web.host.id-limit');
                return nextId++;
            };
            const checkedCallback = (callback) => {
                // String handlers need eval and remain explicitly unsupported.
                if (typeof callback !== 'function') fail('web.host.function-callback-required');
            };
            const delayValue = (value, level) => {
                // WebIDL long conversion: truncate/wrap to signed 32-bit.
                let result = toNumber(value) | 0;
                if (result < 0) result = 0;
                if (level > 5 && result < 4) result = 4;
                return result;
            };
            const scheduleTimer = (callback, timeout, args, repeat) => {
                ensureLive();
                checkedCallback(callback);
                if (args.length > ownLimits.arguments) fail('web.host.timer-argument-limit');
                const delay = delayValue(timeout, nesting);
                // Coercion of timeout may execute game code; check capacity AFTER it.
                ensureLive();
                if (timerCount >= ownLimits.timers) fail('web.host.timer-limit');
                const id = allocateId();
                timers[id] = { id, callback, args, delay, due: now + delay, repeat, level: nesting + 1 };
                timerCount++;
                return id;
            };
            const clearTimer = (handle) => {
                const id = toNumber(handle) | 0;
                if (timers[id]) { delete timers[id]; timerCount--; }
            };
            const setTimeoutFn = function(callback, timeout = 0, ...args) {
                return scheduleTimer(callback, timeout, args, false);
            };
            const setIntervalFn = function(callback, timeout = 0, ...args) {
                return scheduleTimer(callback, timeout, args, true);
            };
            const requestFrame = function(callback) {
                ensureLive();
                checkedCallback(callback);
                if (frameCount >= ownLimits.frames) fail('web.host.animation-frame-limit');
                const id = allocateId();
                frames[id] = callback;
                frameCount++;
                return id;
            };
            const cancelFrame = function(handle) {
                const id = toNumber(handle) >>> 0;
                if (frames[id]) { delete frames[id]; frameCount--; }
            };

            const advance = function(deltaMilliseconds) {
                ensureLive();
                if (pumping) fail('web.host.reentrant-frame');
                if (currentScript !== null) fail('web.host.frame-during-script');
                if (typeof deltaMilliseconds !== 'number' || !finite(deltaMilliseconds)
                    || deltaMilliseconds < 0 || deltaMilliseconds > ownLimits.delta
                    || now + deltaMilliseconds > 9007197000000000)
                    fail('web.host.invalid-delta');

                pumping = true;
                now += deltaMilliseconds;
                let callbacks = 0;
                const charge = () => {
                    if (++callbacks > ownLimits.callbacks) fail('web.host.callback-budget');
                };
                try {
                    // Snapshot BOTH queues before callbacks. New work is deferred
                    // to the next host pump; cancellation still affects this batch.
                    const timerIds = keys(timers);
                    const frameIds = keys(frames);
                    apply(sort, timerIds, [(a, b) => timers[a].due - timers[b].due || (+a) - (+b)]);
                    for (let i = 0; i < timerIds.length; i++) {
                        const id = timerIds[i];
                        const task = timers[id];
                        if (!task || task.due > now) continue;
                        charge();
                        if (!task.repeat) { delete timers[id]; timerCount--; }
                        nesting = task.level;
                        apply(task.callback, root, task.args);
                        nesting = 0;
                        if (task.repeat && timers[id] === task) {
                            // One interval firing per pump. Missed wall-time periods
                            // are coalesced, not replayed in an unbounded catch-up loop.
                            task.delay = delayValue(task.delay, task.level);
                            task.level++;
                            task.due = now + task.delay;
                        }
                    }
                    for (let i = 0; i < frameIds.length; i++) {
                        const id = frameIds[i];
                        const callback = frames[id];
                        if (!callback) continue;
                        charge();
                        delete frames[id];
                        frameCount--;
                        apply(callback, undefined, [now]);
                    }
                } catch (error) {
                    faulted = true;
                    timers = create(null); frames = create(null);
                    timerCount = 0; frameCount = 0;
                    throw error;
                } finally { pumping = false; nesting = 0; }
            };
            const enterScript = function(src) {
                ensureLive();
                if (pumping || currentScript !== null) fail('web.host.script-scope-busy');
                if (typeof src !== 'string' || src.length === 0 || src.length > 8192)
                    fail('web.host.script-url-invalid');
                // Metadata only. This URL is never fetched or resolved on the host.
                currentScript = freeze({ src, type: 'text/javascript', async: false });
            };
            const leaveScript = function() {
                ensureLive();
                if (pumping) fail('web.host.script-scope-busy');
                currentScript = null;
            };
            const documentMetadata = create(null);
            define(documentMetadata, 'currentScript', { enumerable: true, get: () => currentScript });
            freeze(documentMetadata);
            const performanceClock = { now: () => now };
            const publish = (name, value) => define(root, name, {
                value, writable: true, configurable: true, enumerable: true
            });
            publish('window', root);
            publish('self', root);
            publish('performance', performanceClock);
            publish('document', documentMetadata);
            publish('setTimeout', setTimeoutFn);
            publish('clearTimeout', clearTimer);
            publish('setInterval', setIntervalFn);
            publish('clearInterval', clearTimer);
            publish('requestAnimationFrame', requestFrame);
            publish('cancelAnimationFrame', cancelFrame);
            // Realm-local control plumbing, not a privileged OS boundary. It is
            // deliberately immutable but game code can still see/call it.
            define(root, '__urpgBrowserHost', { value: freeze({ advance, enterScript, leaveScript }) });
        })
        """;
    // JS_BROWSER_HOST_END

    public static ScriptModule Build(string pLanguageId, WebBrowserHostOptions pOptions)
    {
        if (pLanguageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            throw new ArgumentException("Browser timing host requires an MV or MZ language ID.", nameof(pLanguageId));
        if (pOptions == null) throw new ArgumentNullException(nameof(pOptions));
        var result = pOptions.Validate();
        if (!result.Success) throw new ArgumentException(result.ErrorMessage, nameof(pOptions));
        var limits = JsonSerializer.Serialize(new
        {
            timers = pOptions.MaxPendingTimers, frames = pOptions.MaxPendingAnimationFrames,
            callbacks = pOptions.MaxCallbacksPerFrame, arguments = pOptions.MaxTimerArguments,
            delta = pOptions.MaxFrameMilliseconds,
        });
        return new ScriptModule
        {
            Descriptor = new EngineScriptDescriptor
            {
                Id = ModuleId, DisplayName = "URPG frame-pumped browser subset",
                LanguageId = pLanguageId, Origin = ScriptOrigin.CompatibilityShim,
                Required = true, LoadOrder = int.MinValue,
            },
            Source = Encoding.UTF8.GetBytes(FactorySource + "(" + limits + ");\n"),
        };
    }
}
