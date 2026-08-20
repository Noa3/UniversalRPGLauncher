## class_name VirtualFileSystemTest
## tests/core/test_virtual_filesystem.gd
##
## Unit tests for VirtualFileSystem.
## Uses synthetic file system operations for deterministic testing.

extends Test

var vfs: VirtualFileSystem
var temp_dir: String


func setup() -> void:
	vfs = VirtualFileSystem.new()
	temp_dir = "user://vfs_test"
	if not DirAccess.dir_exists_absolute(temp_dir):
		DirAccess.make_dir_recursive_absolute(temp_dir)
		DirAccess.make_dir_recursive_absolute("user://vfs_test/game")
		DirAccess.make_dir_recursive_absolute("user://vfs_test/override")
		DirAccess.make_dir_recursive_absolute("user://vfs_test/rtp")
		DirAccess.make_dir_recursive_absolute("user://vfs_test/save")
	
	# Create test files
	_create_test_file("user://vfs_test/game/Map001.json", '{"map":1}')
	_create_test_file("user://vfs_test/game/Map002.json", '{"map":2}')
	_create_test_file("user://vfs_test/game/data/config.ini", '[Game]\nTitle=Test')
	_create_test_file("user://vfs_test/override/Map001.json", '{"map":1,"override":true}')
	_create_test_file("user://vfs_test/rtp/BGM001.ogg", 'rtp_audio_data')
	_create_test_file("user://vfs_test/save/Save001.rvdata2", 'save_data')


func teardown() -> void:
	# Clean up test files
	_cleanup_dir("user://vfs_test")


func _create_test_file(p_path: String, p_content: String) -> void:
	var dir := p_path.get_base_dir()
	if not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)
	var file := FileAccess.open(p_path, FileAccess.WRITE)
	if file:
		file.store_string(p_content)
		file.close()


func _cleanup_dir(p_dir: String) -> void:
	if not DirAccess.dir_exists_absolute(p_dir):
		return
	var dir := DirAccess.open(p_dir)
	if dir == null:
		return
	dir.list_dir_begin()
	var file_name := dir.get_next()
	while file_name != "":
		var full_path := p_dir + "/" + file_name
		if DirAccess.dir_exists_absolute(full_path):
			_cleanup_dir(full_path)
			DirAccess.remove_absolute(full_path)
		elif FileAccess.file_exists(full_path):
			DirAccess.remove_absolute(full_path)
		file_name = dir.get_next()


# === TESTS: Path Normalization ===

func test_normalize_path_forwards_slashes() -> void:
	var result := vfs.normalize_path("Graphics/Characters/Hero.png")
	assert_eq(result, "Graphics/Characters/Hero.png")


func test_normalize_path_backslashes() -> void:
	var result := vfs.normalize_path("Graphics\\Characters\\Hero.png")
	assert_eq(result, "Graphics/Characters/Hero.png")


func test_normalize_path_double_slashes() -> void:
	var result := vfs.normalize_path("Graphics//Characters///Hero.png")
	assert_eq(result, "Graphics/Characters/Hero.png")


func test_normalize_path_trailing_slash() -> void:
	var result := vfs.normalize_path("Graphics/Characters/")
	assert_eq(result, "Graphics/Characters")


func test_normalize_path_root() -> void:
	var result := vfs.normalize_path("/")
	assert_eq(result, "/")


# === TESTS: Path Safety ===

func test_safe_path_normal() -> void:
	assert_true(vfs.is_safe_path("Graphics/Characters/Hero.png"))


func test_safe_path_with_dots() -> void:
	assert_false(vfs.is_safe_path("../../../etc/passwd"))


func test_safe_path_absolute() -> void:
	assert_false(vfs.is_safe_path("/etc/passwd"))


func test_safe_path_null_byte() -> void:
	assert_false(vfs.is_safe_path("Graphics/../../../etc/passwd\u0000.png"))


# === TESTS: Mount Management ===

func test_add_mount() -> void:
	var mount := vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	assert_ne(mount, null)
	assert_eq(mount.path, "user://vfs_test/game")
	assert_eq(mount.mount_type, VirtualFileSystem.MountType.GAME)


func test_add_writable_mount() -> void:
	var mount := vfs.add_mount("save", "user://vfs_test/save", VirtualFileSystem.MountType.SAVE, true)
	assert_true(mount.writable)


func test_remove_mount() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	assert_true(vfs.remove_mount(VirtualFileSystem.MountType.GAME))
	assert_eq(vfs.get_game_directory(), "")


func test_get_mount() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	var mount := vfs.get_mount(VirtualFileSystem.MountType.GAME)
	assert_ne(mount, null)
	assert_eq(mount.path, "user://vfs_test/game")


func test_get_nonexistent_mount() -> void:
	var mount := vfs.get_mount(VirtualFileSystem.MountType.GAME)
	assert_eq(mount, null)


# === TESTS: File Resolution ===

func test_resolve_existing_file() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	var resolved := vfs.resolve("Map001.json")
	assert_ne(resolved, "")
	assert_true(FileAccess.file_exists(resolved))


func test_resolve_nonexistent_file() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	var resolved := vfs.resolve("NonExistent.json")
	assert_eq(resolved, "")


func test_resolve_unsafe_path() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	var resolved := vfs.resolve("../../../etc/passwd")
	assert_eq(resolved, "")


func test_resolve_case_insensitive() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	var resolved := vfs.resolve("map001.json")  # lowercase
	assert_ne(resolved, "")
	assert_true(FileAccess.file_exists(resolved))


func test_resolve_priority_override() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	vfs.add_mount("override", "user://vfs_test/override", VirtualFileSystem.MountType.OVERRIDE)
	var resolved := vfs.resolve("Map001.json")
	## Override should take priority
	assert_true(resolved.begins_with("user://vfs_test/override/"))


# === TESTS: File Operations ===

func test_file_exists() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	assert_true(vfs.file_exists("Map001.json"))
	assert_false(vfs.file_exists("NonExistent.json"))


func test_dir_exists() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	assert_true(vfs.dir_exists("data"))
	assert_false(vfs.dir_exists("NonExistent"))


func test_list_dir() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	var files := vfs.list_dir("")
	assert_true("Map001.json" in files)
	assert_true("Map002.json" in files)
	assert_true("data" in files)


func test_open_file() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	var file := vfs.open("Map001.json", FileAccess.READ)
	assert_ne(file, null)
	assert_true(file.is_open())
	assert_eq(file.get_as_text(), '{"map":1}')
	file.close()


func test_open_nonexistent_file() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	var file := vfs.open("NonExistent.json", FileAccess.READ)
	assert_eq(file, null)


# === TESTS: Game Directory ===

func test_get_game_directory() -> void:
	vfs.add_mount("game", "user://vfs_test/game", VirtualFileSystem.MountType.GAME)
	assert_eq(vfs.get_game_directory(), "user://vfs_test/game")


func test_get_save_directory() -> void:
	vfs.add_mount("save", "user://vfs_test/save", VirtualFileSystem.MountType.SAVE)
	assert_eq(vfs.get_save_directory(), "user://vfs_test/save")


func test_get_nonexistent_save_directory() -> void:
	assert_eq(vfs.get_save_directory(), "")
