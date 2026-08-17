## class_name VirtualClock
## src/core/virtual_clock.gd
##
## Deterministic virtual timing system for RPG Maker games.
## Separates simulation from rendering to preserve original game timing.

extends RefCounted


## Simulation speed multiplier
enum SpeedMode {
	NORMAL,    ## 1x original speed
	FastForward, ## Variable speed (see speed_multiplier)
	SlowMotion,  ## Variable slow (see speed_multiplier)
	Paused,    ## Simulation frozen
}


## Timing event for scheduled callbacks
class TimingEvent:
	var target_ticks: int
	var callback: Callable
	var repeat: bool
	var active: bool
	
	func _init(p_ticks: int, p_cb: Callable, p_repeat: bool = false) -> void:
		target_ticks = p_ticks
		callback = p_cb
		repeat = p_repeat
		active = true


# Original game tick rate (typically 60 Hz for most RPG Maker games)
const ORIGINAL_TICK_RATE: float = 60.0

# Current simulation speed
var _speed_mode: SpeedMode = SpeedMode.NORMAL
var _speed_multiplier: float = 1.0

# Virtual time tracking
var _simulation_ticks: int = 0
var _last_tick_time: double = 0.0
var _accumulator: float = 0.0

# Scheduled timing events
var _timing_events: Array[TimingEvent] = []

# Frame timing
var _target_frame_time: float = 1.0 / ORIGINAL_TICK_RATE
var _actual_frame_time: float = 0.0

# Debug info
var simulation_fps: float = 0.0
var render_fps: float = 0.0
var frame_count: int = 0


func _init() -> void:
	_last_tick_time = _get_os_time()


## Get the current simulation tick count
func get_simulation_ticks() -> int:
	return _simulation_ticks


## Get the current simulation time in seconds
func get_simulation_time() -> float:
	return _simulation_ticks / ORIGINAL_TICK_RATE


## Set the simulation speed mode
func set_speed_mode(p_mode: SpeedMode, p_multiplier: float = 1.0) -> void:
	_speed_mode = p_mode
	_speed_multiplier = p_multiplier


## Get the current speed mode
func get_speed_mode() -> SpeedMode:
	return _speed_mode


## Get the current speed multiplier
func get_speed_multiplier() -> float:
	return _speed_multiplier


## Process one frame of the simulation
## Call this from your game loop every frame
func process_frame(p_delta_time: float) -> Array[Callable]:
	# Track render FPS
	frame_count += 1
	if _actual_frame_time >= 1.0:
		render_fps = float(frame_count)
		frame_count = 0
		_actual_frame_time = 0.0
	_actual_frame_time += p_delta_time
	
	var executed: Array[Callable] = []
	
	# Handle paused state
	if _speed_mode == SpeedMode.Paused:
		return executed
	
	# Calculate target simulation interval based on speed
	var target_interval: float = 0.0
	
	match _speed_mode:
		SpeedMode.NORMAL:
			target_interval = _target_frame_time
		SpeedMode.FastForward:
			target_interval = _target_frame_time / _speed_multiplier
		SpeedMode.SlowMotion:
			target_interval = _target_frame_time * _speed_multiplier
		_:
			target_interval = _target_frame_time
	
	# Accumulate time and run simulation steps
	_accumulator += p_delta_time
	
	var steps_this_frame: int = 0
	var max_steps: int = 256  # Prevent infinite loops
	
	while _accumulator >= target_interval and steps_this_frame < max_steps:
		_accumulator -= target_interval
		executed.append_array(_process_simulation_tick())
		steps_this_frame += 1
	
	# If we fell too far behind, reset accumulator to prevent spiral
	if _accumulator > target_interval * 10:
		_accumulator = 0.0
	
	return executed


## Process a single simulation tick
## Returns list of callbacks to execute
func _process_simulation_tick() -> Array[Callable]:
	var callbacks: Array[Callable] = []
	
	_simulation_ticks += 1
	
	# Check for timing events
	var expired: Array[int] = []
	for i in range(_timing_events.size()):
		if _timing_events[i].active and _simulation_ticks >= _timing_events[i].target_ticks:
			callbacks.append(_timing_events[i].callback)
			if not _timing_events[i].repeat:
				expired.append(i)
	
	# Remove expired events (reverse order to preserve indices)
	for i in expired:
		_timing_events.remove_at(i)
	
	return callbacks


## Schedule a callback after N ticks
func schedule_callback(p_callback: Callable, p_after_ticks: int) -> int:
	var event := TimingEvent.new(_simulation_ticks + p_after_ticks, p_callback)
	_timing_events.append(event)
	return _timing_events.size() - 1


## Schedule a repeating callback every N ticks
func schedule_repeating_callback(p_callback: Callable, p_every_ticks: int) -> int:
	var event := TimingEvent.new(_simulation_ticks + p_every_ticks, p_callback, true)
	_timing_events.append(event)
	return _timing_events.size() - 1


## Cancel a scheduled callback by index
func cancel_callback(p_index: int) -> bool:
	if p_index >= 0 and p_index < _timing_events.size():
		_timing_events[p_index].active = false
		return true
	return false


## Get the remaining ticks until a scheduled callback
func get_remaining_ticks(p_index: int) -> int:
	if p_index >= 0 and p_index < _timing_events.size():
		var diff := _timing_events[p_index].target_ticks - _simulation_ticks
		return max(0, diff)
	return 0


## Single step the simulation by one tick (for debugging)
func step_single_tick() -> Array[Callable]:
	return _process_simulation_tick()


## Reset the virtual clock
func reset() -> void:
	_simulation_ticks = 0
	_accumulator = 0.0
	_timing_events.clear()
	_last_tick_time = _get_os_time()


## Get simulation statistics
func get_stats() -> Dictionary:
	return {
		simulation_ticks = _simulation_ticks,
		simulation_time = get_simulation_time(),
		simulation_fps = _calc_simulation_fps(),
		render_fps = render_fps,
		speed_mode = _speed_mode,
		speed_multiplier = _speed_multiplier,
		accumulator = _accumulator,
		scheduled_events = _timing_events.size(),
	}


## Calculate simulation FPS (updated periodically)
func _calc_simulation_fps() -> float:
	var elapsed := _get_os_time() - _last_tick_time
	if elapsed >= 1.0:
		simulation_fps = float(_simulation_ticks) / elapsed
		_last_tick_time = _get_os_time()
		return simulation_fps
	return simulation_fps


## Get current OS time in seconds
func _get_os_time() -> double:
	return Time.get_unix_time_from_system()


## Create a deterministic random number generator
## Use this for reproducible behavior in save states
func create_deterministic_rng(seed: int) -> RandomNumberGenerator:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed
	return rng
