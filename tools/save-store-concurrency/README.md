# Save-store cross-process verification

This harness calls the actual `MzSaveStore` and `MzValue` from the current Godot Debug assembly. It does not copy the storage implementation and does not execute any imported game scripts or native game plugins.

First build `project/UniversalRPG.csproj` with `-t:Rebuild`. Do not rebuild it while a Godot validator is running. Then, from the repository root:

    dotnet build tools/save-store-concurrency/SaveStoreConcurrency.csproj -v minimal
    dotnet tools/save-store-concurrency/bin/Debug/net8.0/SaveStoreConcurrency.dll <scratch-root>

For the Hermes Windows environment, keep generated output in the profile scratch directory:

    dotnet build tools/save-store-concurrency/SaveStoreConcurrency.csproj -v minimal -p:OutputPath=C:/Users/noa3/AppData/Local/hermes/profiles/code/cache/scratch/save-store-concurrency/bin/ -p:BaseIntermediateOutputPath=C:/Users/noa3/AppData/Local/hermes/profiles/code/cache/scratch/save-store-concurrency/obj/
    dotnet C:/Users/noa3/AppData/Local/hermes/profiles/code/cache/scratch/save-store-concurrency/bin/SaveStoreConcurrency.dll C:/Users/noa3/AppData/Local/hermes/profiles/code/cache/scratch

The parent starts eight independent .NET processes. Each commits 32 writes to the same scratch slot, so the static in-process lock cannot hide process-level contention. Every worker must exit successfully, the final file must contain one whole submitted numeric value, and no staging files may remain. Expected successful output begins:

    PASS: 8 independent processes, 256 committed writes

The harness bounds worker completion to 60 seconds, terminates only its own children on failure, and removes only its unique scratch directory. The caller must provide a writable scratch root. This is a separate integration check, not part of the Godot unit-test count and not proof of full save-game state compatibility.

The pre-fix implementation with only an in-process gate failed this scenario with sharing/access-denied errors during atomic replacement. A per-file named mutex is required in addition to unique staging paths and destination replacement. Locks apply within the OS session and are not a defense against another local process maliciously renaming directory components.
