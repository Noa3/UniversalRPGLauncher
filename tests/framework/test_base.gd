## class_name Test
## tests/framework/test_base.gd
##
## Minimal test base class for the `extends Test` suites in tests/core/.
## Suites define setup()/teardown() and test_* methods; assertions record
## failures instead of aborting, so every test in a suite runs.

class_name Test
extends RefCounted

var _failures: Array[String] = []
var _assertions := 0
var _current_test := ""


func setup() -> void:
	pass


func teardown() -> void:
	pass


func run_all() -> Dictionary:
	var result := {
		"tests": 0,
		"passed": 0,
		"failed": 0,
		"failures": [] as Array[String],
	}
	var methods := get_method_list()
	for method in methods:
		var name: String = method["name"]
		if not name.begins_with("test_"):
			continue
		result["tests"] += 1
		_run_test(name, result)
	return result


func _run_test(p_name: String, p_result: Dictionary) -> void:
	_current_test = p_name
	_failures.clear()
	_assertions = 0
	setup()
	if not _failures.is_empty():
		_fail("setup() failed before test ran")
	call(p_name)
	teardown()
	if _failures.is_empty():
		p_result["passed"] += 1
	else:
		p_result["failed"] += 1
		for failure in _failures:
			p_result["failures"].append("%s: %s" % [p_name, failure])
	_current_test = ""


func _fail(p_message: String) -> void:
	_failures.append(p_message)


func assert_true(p_condition: bool, p_message: String = "Expected true") -> void:
	_assertions += 1
	if not p_condition:
		_fail(p_message)


func assert_false(p_condition: bool, p_message: String = "Expected false") -> void:
	_assertions += 1
	if p_condition:
		_fail(p_message)


func assert_eq(p_actual: Variant, p_expected: Variant, p_message: String = "") -> void:
	_assertions += 1
	if p_actual != p_expected:
		var detail := "Expected %s, got %s" % [str(p_expected), str(p_actual)]
		_fail(p_message if not p_message.is_empty() else detail)


func assert_ne(p_actual: Variant, p_expected: Variant, p_message: String = "Values should differ") -> void:
	_assertions += 1
	if p_actual == p_expected:
		_fail("%s (both are %s)" % [p_message, str(p_actual)])