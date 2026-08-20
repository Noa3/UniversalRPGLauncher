extends SceneTree

const TEST_DIRS := ["res://tests/core"]

var _failures: Array[String] = []
var _total := 0
var _passed := 0


func _initialize() -> void:
	TranslationServer.set_locale("en")
	for dir in TEST_DIRS:
		_run_directory(dir)
	if _failures.is_empty():
		print("All %d tests passed" % _total)
	else:
		print("%d/%d tests failed" % [_failures.size(), _total])
		for failure in _failures:
			push_error(failure)
	quit(_failures.size())


func _run_directory(p_dir: String) -> void:
	var directory := DirAccess.open(p_dir)
	if directory == null:
		push_error("Cannot open test directory " + p_dir)
		_failures.append("Cannot open " + p_dir)
		return
	directory.list_dir_begin()
	var file_name := directory.get_next()
	while file_name != "":
		if file_name.ends_with(".gd"):
			_run_suite(p_dir.path_join(file_name))
		file_name = directory.get_next()


func _run_suite(p_script_path: String) -> void:
	var suite = load(p_script_path)
	if suite == null:
		_failures.append("Cannot load " + p_script_path)
		return
	if not suite.can_instantiate():
		_failures.append("Cannot instantiate " + p_script_path)
		return
	var instance = suite.new()
	var result: Dictionary = instance.run_all()
	_total += result["tests"]
	_passed += result["passed"]
	var label := p_script_path.get_file().trim_suffix(".gd")
	if result["failed"] > 0:
		_failures.append("%s: %d/%d tests failed" % [label, result["failed"], result["tests"]])
		for failure in result["failures"]:
			_failures.append("  " + failure)
	print("%s: %d/%d passed" % [label, result["passed"], result["tests"]])