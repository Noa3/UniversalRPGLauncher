using System;
using System.Collections.Generic;
using Godot;

namespace UniversalRPG.Core;

/// <summary>
/// Deterministic virtual timing system for RPG Maker games.
/// Separates simulation from rendering to preserve original game timing.
/// </summary>
public partial class VirtualClock : RefCounted
{
	public enum SpeedMode
	{
		Normal,
		FastForward,
		SlowMotion,
		Paused,
	}

	public class TimingEvent
	{
		public int Id;
		public int TargetTicks;
		public int IntervalTicks;
		public Action Callback;
		public bool Repeat;
		public bool Active = true;

		public TimingEvent(int pId, int pTicks, int pIntervalTicks, Action pCallback, bool pRepeat = false)
		{
			Id = pId;
			TargetTicks = pTicks;
			IntervalTicks = Math.Max(1, pIntervalTicks);
			Callback = pCallback;
			Repeat = pRepeat;
		}
	}

	public const float OriginalTickRate = 60.0f;
	public const float MinSpeedMultiplier = 0.01f;
	public const float MaxSpeedMultiplier = 10.0f;
	public const int MaxStepsPerFrame = 256;

	private SpeedMode _speedMode = SpeedMode.Normal;
	private float _speedMultiplier = 1.0f;

	private int _simulationTicks = 0;
	private double _accumulator = 0.0;

	private readonly List<TimingEvent> _timingEvents = new();
	private int _nextEventId = 1;

	private readonly double _targetFrameTime = 1.0 / OriginalTickRate;
	private double _renderTimeAccumulator = 0.0;
	private int _renderFrameCount = 0;
	private int _simulationTicksAtLastFpsSample = 0;
	private double _simulationTimeAtLastFpsSample = 0.0;

	public float SimulationFps { get; private set; } = 0.0f;
	public float RenderFps { get; private set; } = 0.0f;

	public VirtualClock()
	{
		_simulationTimeAtLastFpsSample = GetMonotonicTime();
	}

	public int GetSimulationTicks() => _simulationTicks;

	public float GetSimulationTime() => _simulationTicks / OriginalTickRate;

	/// <summary>
	/// pMultiplier is always expressed as a speed factor: 2.0 = 2x, 0.5 = half.
	/// </summary>
	public void SetSpeedMode(SpeedMode pMode, float pMultiplier = 1.0f)
	{
		_speedMode = pMode;
		switch (_speedMode)
		{
			case SpeedMode.Normal:
				_speedMultiplier = 1.0f;
				break;
			case SpeedMode.FastForward:
				_speedMultiplier = Mathf.Clamp(Mathf.Max(1.0f, pMultiplier), 1.0f, MaxSpeedMultiplier);
				break;
			case SpeedMode.SlowMotion:
				_speedMultiplier = Mathf.Clamp(pMultiplier, MinSpeedMultiplier, 1.0f);
				break;
			case SpeedMode.Paused:
				_speedMultiplier = 0.0f;
				break;
		}
	}

	public SpeedMode GetSpeedMode() => _speedMode;

	public float GetSpeedMultiplier() => _speedMultiplier;

	public List<Action> ProcessFrame(double pDeltaTime)
	{
		var delta = Math.Max(0.0, pDeltaTime);
		UpdateRenderFps(delta);

		var executed = new List<Action>();
		if (_speedMode == SpeedMode.Paused)
		{
			UpdateSimulationFps();
			return executed;
		}

		var speed = _speedMultiplier;
		if (speed <= 0.0)
		{
			UpdateSimulationFps();
			return executed;
		}

		var targetInterval = _targetFrameTime / speed;
		_accumulator += delta;

		var stepsThisFrame = 0;
		while (_accumulator >= targetInterval && stepsThisFrame < MaxStepsPerFrame)
		{
			_accumulator -= targetInterval;
			executed.AddRange(ProcessSimulationTick());
			stepsThisFrame += 1;
		}

		// Prevent a stalled host frame from creating an unbounded catch-up spiral.
		if (stepsThisFrame == MaxStepsPerFrame && _accumulator >= targetInterval)
		{
			_accumulator %= targetInterval;
		}

		UpdateSimulationFps();
		return executed;
	}

	private List<Action> ProcessSimulationTick()
	{
		var callbacks = new List<Action>();
		_simulationTicks += 1;

		foreach (var timingEvent in _timingEvents)
		{
			if (!timingEvent.Active || _simulationTicks < timingEvent.TargetTicks)
			{
				continue;
			}
			callbacks.Add(timingEvent.Callback);
			if (timingEvent.Repeat)
			{
				// Reschedule from the previous deadline so the cadence stays stable.
				// Catch up only to the next future deadline; never fire repeatedly in
				// the same simulation tick.
				while (timingEvent.TargetTicks <= _simulationTicks)
				{
					timingEvent.TargetTicks += timingEvent.IntervalTicks;
				}
			}
			else
			{
				timingEvent.Active = false;
			}
		}

		_timingEvents.RemoveAll(timingEvent => !timingEvent.Active);
		return callbacks;
	}

	public int ScheduleCallback(Action pCallback, int pAfterTicks)
	{
		var delay = Math.Max(1, pAfterTicks);
		var eventId = _nextEventId;
		_nextEventId += 1;
		_timingEvents.Add(new TimingEvent(eventId, _simulationTicks + delay, delay, pCallback, false));
		return eventId;
	}

	public int ScheduleRepeatingCallback(Action pCallback, int pEveryTicks)
	{
		var interval = Math.Max(1, pEveryTicks);
		var eventId = _nextEventId;
		_nextEventId += 1;
		_timingEvents.Add(new TimingEvent(eventId, _simulationTicks + interval, interval, pCallback, true));
		return eventId;
	}

	public bool CancelCallback(int pEventId)
	{
		for (var index = 0; index < _timingEvents.Count; index++)
		{
			if (_timingEvents[index].Id == pEventId && _timingEvents[index].Active)
			{
				_timingEvents.RemoveAt(index);
				return true;
			}
		}
		return false;
	}

	public int GetRemainingTicks(int pEventId)
	{
		foreach (var timingEvent in _timingEvents)
		{
			if (timingEvent.Id == pEventId && timingEvent.Active)
			{
				return Math.Max(0, timingEvent.TargetTicks - _simulationTicks);
			}
		}
		return 0;
	}

	public List<Action> StepSingleTick()
	{
		var callbacks = ProcessSimulationTick();
		UpdateSimulationFps();
		return callbacks;
	}

	public void Reset()
	{
		_simulationTicks = 0;
		_accumulator = 0.0;
		_timingEvents.Clear();
		_nextEventId = 1;
		SimulationFps = 0.0f;
		RenderFps = 0.0f;
		_renderTimeAccumulator = 0.0;
		_renderFrameCount = 0;
		_simulationTicksAtLastFpsSample = 0;
		_simulationTimeAtLastFpsSample = GetMonotonicTime();
	}

	public Godot.Collections.Dictionary GetStats()
	{
		return new Godot.Collections.Dictionary
		{
			{ "simulation_ticks", _simulationTicks },
			{ "simulation_time", GetSimulationTime() },
			{ "simulation_fps", SimulationFps },
			{ "render_fps", RenderFps },
			{ "speed_mode", (int)_speedMode },
			{ "speed_multiplier", _speedMultiplier },
			{ "accumulator", _accumulator },
			{ "scheduled_events", _timingEvents.Count },
		};
	}

	private void UpdateRenderFps(double pDeltaTime)
	{
		_renderFrameCount += 1;
		_renderTimeAccumulator += pDeltaTime;
		if (_renderTimeAccumulator >= 1.0)
		{
			RenderFps = (float)(_renderFrameCount / _renderTimeAccumulator);
			_renderFrameCount = 0;
			_renderTimeAccumulator = 0.0;
		}
	}

	private void UpdateSimulationFps()
	{
		var now = GetMonotonicTime();
		var elapsed = now - _simulationTimeAtLastFpsSample;
		if (elapsed < 1.0)
		{
			return;
		}
		var ticksSinceSample = _simulationTicks - _simulationTicksAtLastFpsSample;
		SimulationFps = (float)(ticksSinceSample / elapsed);
		_simulationTicksAtLastFpsSample = _simulationTicks;
		_simulationTimeAtLastFpsSample = now;
	}

	private static double GetMonotonicTime()
	{
		return Time.GetTicksUsec() / 1_000_000.0;
	}

	public RandomNumberGenerator CreateDeterministicRng(ulong pSeed)
	{
		var rng = new RandomNumberGenerator();
		rng.Seed = pSeed;
		return rng;
	}
}
