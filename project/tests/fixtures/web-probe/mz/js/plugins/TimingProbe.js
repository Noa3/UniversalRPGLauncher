/* Synthetic project-owned MZ fixture; no copyrighted engine or game assets. */
(function() {
    'use strict';
    if (!document.currentScript || !document.currentScript.src.endsWith('/TimingProbe.js')) throw Error('script metadata');
    const owner = { greeting: null };
    PluginManager.registerCommand('TimingProbe', 'Greet', function(args) { this.greeting = args.message; });
    setTimeout(function() {
        PluginManager.callCommand(owner, 'TimingProbe', 'Greet', { message: PluginManager.parameters('TimingProbe').Greeting });
    }, 0);
    requestAnimationFrame(function(time) {
        if (owner.greeting !== 'hello' || time !== performance.now()) throw Error('command/timer ordering');
        globalThis.URPG_TIMING_PROBE_PASSED = true;
    });
})();
