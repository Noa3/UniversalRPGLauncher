## tests/core/test_virtual_clock.gd
## Regression tests for deterministic timing and scheduled callbacks.

extends Test

var clock: VirtualClock
var callback_count := 0


func setup() -> void:
	clock = VirtualClock.new()
	callback_count = 0


func _on_callback() -> void:
	callback_count += 1


func test_normal_speed_advances_one_tick_per_sixtieth() -> void:
	clock.process_frame(1.0 / 60.0)
	assert_eq(clock.get_simulation_ticks(), 1)


func test_fast_forward_uses_speed_factor() -> void:
	clock.set_speed_mode(VirtualClock.SpeedMode.FastForward, 2.0)
	clock.process_frame(1.0 / 60.0)
	assert_eq(clock.get_simulation_ticks(), 2)
	assert_eq(clock.get_speed_multiplier(), 2.0)


func test_slow_motion_half_speed() -> void:
	clock.set_speed_mode(VirtualClock.SpeedMode.SlowMotion, 0.5)
	clock.process_frame(1.0 / 60.0)
	assert_eq(clock.get_simulation_ticks(), 0)
	clock.process_frame(1.0 / 60.0)
	assert_eq(clock.get_simulation_ticks(), 1)
	assert_eq(clock.get_speed_multiplier(), 0.5)


func test_pause_does_not_advance_simulation() -> void:
	clock.set_speed_mode(VirtualClock.SpeedMode.Paused)
	clock.process_frame(10.0)
	assert_eq(clock.get_simulation_ticks(), 0)


func test_one_shot_callback_fires_once() -> void:
	clock.schedule_callback(_on_callback, 2)
	clock.step_single_tick()
	assert_eq(callback_count, 0)
	var callbacks := clock.step_single_tick()
	for callback in callbacks:
		callback.call()
	assert_eq(callback_count, 1)
	callbacks = clock.step_single_tick()
	for callback in callbacks:
		callback.call()
	assert_eq(callback_count, 1)


func test_repeating_callback_preserves_interval() -> void:
	clock.schedule_repeating_callback(_on_callback, 3)
	for i in range(9):
		var callbacks := clock.step_single_tick()
		for callback in callbacks:
			callback.call()
	assert_eq(callback_count, 3)


func test_callback_handles_are_stable_after_other_event_expires() -> void:
	clock.schedule_callback(_on_callback, 1)
	var repeating_id := clock.schedule_repeating_callback(_on_callback, 10)
	clock.step_single_tick()
	assert_eq(clock.get_remaining_ticks(repeating_id), 9)
	assert_true(clock.cancel_callback(repeating_id))
	assert_eq(clock.get_remaining_ticks(repeating_id), 0)


func test_reset_clears_events_and_ticks() -> void:
	clock.schedule_repeating_callback(_on_callback, 1)
	clock.step_single_tick()
	clock.reset()
	assert_eq(clock.get_simulation_ticks(), 0)
	assert_eq(clock.get_stats()["scheduled_events"], 0)
