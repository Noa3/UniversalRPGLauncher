## class_name VirtualClock
## src/core/virtual_clock.gd
##
## Deterministic virtual timing system for RPG Maker games.
## Separates simulation from rendering to preserve original game timing.

class_name VirtualClock
extends RefCounted


enum SpeedMode {
	NORMAL,       ## 1x original speed
	FastForward,  ## >1x simulation speed
	SlowMotion,   ## 0..1x simulation speed
	Paused,       ## Simulation frozen
}


class TimingEvent:
	var id: int
	var target_ticks: int
	var interval_ticks: int
	var callback: Callable
	var repeat: bool
	var active: bool

	func _init(p_id: int, p_ticks: int, p_interval_ticks: int, p_cb: Callable, p_repeat: bool = false) -> void:
		id = p_id
		target_ticks = p_ticks
		interval_ticks = max(1, p_interval_ticks)
		callback = p_cb
		repeat = p_repeat
		active = true


const ORIGINAL_TICK_RATE: float = 60.0
const MIN_SPEED_MULTIPLIER: float = 0.01
const MAX_SPEED_MULTIPLIER: float = 10.0
const MAX_STEPS_PER_FRAME: int = 256

var _speed_mode: SpeedMode = SpeedMode.NORMAL
var _speed_multiplier: float = 1.0

var _simulation_ticks: int = 0
var _accumulator: float = 0.0

var _timing_events: Array[TimingEvent] = []
var _next_event_id: int = 1

var _target_frame_time: float = 1.0 / ORIGINAL_TICK_RATE
var _render_time_accumulator: float = 0.0
var _render_frame_count: int = 0
var _simulation_ticks_at_last_fps_sample: int = 0
var _simulation_time_at_last_fps_sample: float = 0.0

var simulation_fps: float = 0.0
var render_fps: float = 0.0


func _init() -> void:
	_simulation_time_at_last_fps_sample = _get_monotonic_time()


func get_simulation_ticks() -> int:
	return _simulation_ticks


func get_simulation_time() -> float:
	return float(_simulation_ticks) / ORIGINAL_TICK_RATE


## p_multiplier is always expressed as a speed factor:
## 2.0 = 2x, 0.5 = half speed.
func set_speed_mode(p_mode: SpeedMode, p_multiplier: float = 1.0) -> void:
	_speed_mode = p_mode
	match _speed_mode:
		SpeedMode.NORMAL:
			_speed_multiplier = 1.0
		SpeedMode.FastForward:
			_speed_multiplier = clampf(maxf(1.0, p_multiplier), 1.0, MAX_SPEED_MULTIPLIER)
		SpeedMode.SlowMotion:
			_speed_multiplier = clampf(p_multiplier, MIN_SPEED_MULTIPLIER, 1.0)
		SpeedMode.Paused:
			_speed_multiplier = 0.0


func get_speed_mode() -> SpeedMode:
	return _speed_mode


func get_speed_multiplier() -> float:
	return _speed_multiplier


func process_frame(p_delta_time: float) -> Array[Callable]:
	var delta := maxf(0.0, p_delta_time)
	_update_render_fps(delta)

	var executed: Array[Callable] = []
	if _speed_mode == SpeedMode.Paused:
		_update_simulation_fps()
		return executed

	var speed := _speed_multiplier
	if speed <= 0.0:
		_update_simulation_fps()
		return executed

	var target_interval := _target_frame_time / speed
	_accumulator += delta

	var steps_this_frame := 0
	while _accumulator >= target_interval and steps_this_frame < MAX_STEPS_PER_FRAME:
		_accumulator -= target_interval
		executed.append_array(_process_simulation_tick())
		steps_this_frame += 1

	# Prevent a stalled host frame from creating an unbounded catch-up spiral.
	if steps_this_frame == MAX_STEPS_PER_FRAME and _accumulator >= target_interval:
		_accumulator = fmod(_accumulator, target_interval)

	_update_simulation_fps()
	return executed


func _process_simulation_tick() -> Array[Callable]:
	var callbacks: Array[Callable] = []
	_simulation_ticks += 1

	for event in _timing_events:
		if not event.active or _simulation_ticks < event.target_ticks:
			continue
		callbacks.append(event.callback)
		if event.repeat:
			# Reschedule from the previous deadline so the cadence stays stable.
			# Catch up only to the next future deadline; never fire repeatedly in
			# the same simulation tick.
			while event.target_ticks <= _simulation_ticks:
				event.target_ticks += event.interval_ticks
		else:
			event.active = false

	for i in range(_timing_events.size() - 1, -1, -1):
		if not _timing_events[i].active:
			_timing_events.remove_at(i)
	return callbacks


func schedule_callback(p_callback: Callable, p_after_ticks: int) -> int:
	var delay: int = maxi(1, p_after_ticks)
	var event_id := _next_event_id
	_next_event_id += 1
	_timing_events.append(TimingEvent.new(event_id, _simulation_ticks + delay, delay, p_callback, false))
	return event_id


func schedule_repeating_callback(p_callback: Callable, p_every_ticks: int) -> int:
	var interval: int = maxi(1, p_every_ticks)
	var event_id := _next_event_id
	_next_event_id += 1
	_timing_events.append(TimingEvent.new(event_id, _simulation_ticks + interval, interval, p_callback, true))
	return event_id


func cancel_callback(p_event_id: int) -> bool:
	for i in range(_timing_events.size()):
		if _timing_events[i].id == p_event_id and _timing_events[i].active:
			_timing_events.remove_at(i)
			return true
	return false


func get_remaining_ticks(p_event_id: int) -> int:
	for event in _timing_events:
		if event.id == p_event_id and event.active:
			return maxi(0, event.target_ticks - _simulation_ticks)
	return 0


func step_single_tick() -> Array[Callable]:
	var callbacks := _process_simulation_tick()
	_update_simulation_fps()
	return callbacks


func reset() -> void:
	_simulation_ticks = 0
	_accumulator = 0.0
	_timing_events.clear()
	_next_event_id = 1
	simulation_fps = 0.0
	render_fps = 0.0
	_render_time_accumulator = 0.0
	_render_frame_count = 0
	_simulation_ticks_at_last_fps_sample = 0
	_simulation_time_at_last_fps_sample = _get_monotonic_time()


func get_stats() -> Dictionary:
	return {
		simulation_ticks = _simulation_ticks,
		simulation_time = get_simulation_time(),
		simulation_fps = simulation_fps,
		render_fps = render_fps,
		speed_mode = _speed_mode,
		speed_multiplier = _speed_multiplier,
		accumulator = _accumulator,
		scheduled_events = _timing_events.size(),
	}


func _update_render_fps(p_delta_time: float) -> void:
	_render_frame_count += 1
	_render_time_accumulator += p_delta_time
	if _render_time_accumulator >= 1.0:
		render_fps = float(_render_frame_count) / _render_time_accumulator
		_render_frame_count = 0
		_render_time_accumulator = 0.0


func _update_simulation_fps() -> void:
	var now: float = _get_monotonic_time()
	var elapsed: float = now - _simulation_time_at_last_fps_sample
	if elapsed < 1.0:
		return
	var ticks_since_sample := _simulation_ticks - _simulation_ticks_at_last_fps_sample
	simulation_fps = float(ticks_since_sample) / elapsed
	_simulation_ticks_at_last_fps_sample = _simulation_ticks
	_simulation_time_at_last_fps_sample = now


func _get_monotonic_time() -> float:
	return Time.get_ticks_usec() / 1_000_000.0


func create_deterministic_rng(seed: int) -> RandomNumberGenerator:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed
	return rng
