using System;
using System.Text;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Installs a bounded standard-gamepad snapshot provider for original MV/MZ
/// polling code. State is supplied by the Godot platform bridge; this module
/// exposes no OS/controller handles and does not implement game logic.
/// </summary>
public static class NativeGamepadHostPrelude
{
    public const string ModuleId = "compat:native-gamepad-host";
    public const string ControlTarget = "__urpgGamepadHost";
    public const int MaxDevices = 16;
    public const int StandardButtons = 17;
    public const int StandardAxes = 4;

    // JS_NATIVE_GAMEPAD_HOST_BEGIN
    internal const string FactorySource = """
        (() => {
            'use strict';
            const root = globalThis;
            const freeze = Object.freeze;
            const create = Object.create;
            const fail = code => { throw new Error(code); };
            if (Object.prototype.hasOwnProperty.call(root, '__urpgGamepadHost')) fail('gamepad.host-conflict');
            const pads = create(null);
            const validDevice = index => Number.isInteger(index) && index >= 0 && index < 16;
            const ensure = index => {
                if (!validDevice(index)) fail('gamepad.device-invalid');
                let pad = pads[index];
                if (!pad) {
                    pad = pads[index] = {
                        index,
                        buttons: Array.from({length:17}, () => ({pressed:false,value:0})),
                        axes: [0,0,0,0]
                    };
                }
                return pad;
            };
            const setButton = (device, button, pressed, value) => {
                if (!Number.isInteger(button) || button < 0 || button >= 17 || typeof pressed !== 'boolean'
                    || typeof value !== 'number' || !Number.isFinite(value) || value < 0 || value > 1)
                    fail('gamepad.button-invalid');
                const pad = ensure(device);
                pad.buttons[button] = { pressed, value };
            };
            const setAxis = (device, axis, value) => {
                if (!Number.isInteger(axis) || axis < 0 || axis >= 4 || typeof value !== 'number'
                    || !Number.isFinite(value) || value < -1 || value > 1) fail('gamepad.axis-invalid');
                ensure(device).axes[axis] = value;
            };
            const disconnect = device => {
                if (!validDevice(device)) fail('gamepad.device-invalid');
                delete pads[device];
            };
            const getGamepads = () => {
                const result = [];
                for (const key of Object.keys(pads)) {
                    const pad = pads[key];
                    const buttons = pad.buttons.map(button => freeze({
                        pressed: button.pressed, touched: button.pressed, value: button.value
                    }));
                    const snapshot = freeze({
                        id: 'UniversalRPG Gamepad ' + pad.index,
                        index: pad.index,
                        connected: true,
                        mapping: 'standard',
                        timestamp: typeof performance !== 'undefined' && performance.now ? performance.now() : 0,
                        buttons: freeze(buttons),
                        axes: freeze(pad.axes.slice())
                    });
                    result[pad.index] = snapshot;
                }
                return result;
            };
            const oldNavigator = root.navigator || {};
            root.navigator = freeze({
                userAgent: oldNavigator.userAgent || 'UniversalRPG',
                platform: oldNavigator.platform || 'UniversalRPG',
                language: oldNavigator.language || 'en',
                getGamepads
            });
            Object.defineProperty(root, '__urpgGamepadHost', {
                value: freeze({ setButton, setAxis, disconnect }), configurable: false, writable: false
            });
        })()
        """;
    // JS_NATIVE_GAMEPAD_HOST_END

    public static ScriptModule Build(string pLanguageId)
    {
        if (pLanguageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            throw new ArgumentException("Gamepad host requires MV or MZ.", nameof(pLanguageId));
        return new ScriptModule
        {
            Descriptor = new EngineScriptDescriptor
            {
                Id = ModuleId,
                DisplayName = "URPG native standard gamepad bridge",
                LanguageId = pLanguageId,
                Origin = ScriptOrigin.CompatibilityShim,
                Required = true,
                LoadOrder = int.MinValue + 2,
            },
            Source = Encoding.UTF8.GetBytes(FactorySource),
        };
    }
}
