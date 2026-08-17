# Virtual Filesystem Plugin

A non-destructive virtual filesystem layer for UniversalRPG.

## Features

- Game directory mounting
- RTP asset mounting
- Override asset mounting
- Save directory isolation
- Case-insensitive path resolution (Windows compatibility)
- Archive access (ZIP, RPG Maker archives)
- Path normalization
- Platform-independent path separators

## Architecture

```
Game Directory (read-only)
    ↓
Override Directory (read-only)
    ↓
RTP Directory (read-only)
    ↓
Save Directory (read/write)
    ↓
VirtualFileSystem (merged view)
```

## Usage

```gdscript
var vfs = VirtualFileSystem.new()
vfs.add_mount("game", "/path/to/game")
vfs.add_mount("override", "/path/to/overrides")
vfs.add_mount("rtp", "/path/to/rtp")
vfs.add_mount("save", "/path/to/saves", true)  # writable

# Access files
var file = vfs.open("Data/Map001.json")
var content = file.get_as_text()

# Safe path resolution
var resolved = vfs.resolve("Graphics//Characters/../Maps/Map001.json")
# Returns: "Graphics/Maps/Map001.json"
```

## Path Resolution Order

1. Game directory (highest priority)
2. Override directory
3. RTP directory
4. Save directory (for write operations)

## Case Sensitivity

- Windows games expect case-insensitive paths
- Linux is case-sensitive by default
- VFS normalizes paths to lowercase for lookup
- Original case is preserved in returned paths

## Security

- No access to system directories
- No symlink following outside mount points
- Path traversal (..) blocked at mount boundaries
- All paths validated before access
