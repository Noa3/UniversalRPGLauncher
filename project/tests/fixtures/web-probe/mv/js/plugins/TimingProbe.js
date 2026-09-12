/* Synthetic project-owned MV fixture; no copyrighted engine or game assets. */
(function() {
    'use strict';
    const source = document.currentScript;
    if (!source || !source.src.endsWith('/TimingProbe.js')) throw Error('currentScript unavailable');
    if (PluginManager.parameters('TimingProbe').Greeting !== 'hello') throw Error('parameters unavailable');
    let timerFinished = false;
    setTimeout(function(value) {
        if (this !== window || value !== 'hello' || document.currentScript !== null) throw Error('timer semantics');
        timerFinished = true;
    }, 0, 'hello');
    requestAnimationFrame(function(time) {
        if (!timerFinished || time !== performance.now()) throw Error('frame timing');
        globalThis.URPG_TIMING_PROBE_PASSED = true;
    });
})();
