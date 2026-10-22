using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UniversalRPG.Rgss;

public enum RgssExecutionState
{
    Created,
    Running,
    AtFrame,
    Completed,
    Cancelled,
    Faulted,
}

/// <summary>
/// Owns a single Ruby stack. The worker pauses inside Graphics.update until the
/// caller requests the next frame. No Godot objects may be accessed by the worker.
/// </summary>
/// <remarks>
/// The interpreter and host transfer to this session for their remaining lifetime.
/// Cancellation unwinds them permanently; neither may be reused after disposal.
/// Only immutable diagnostics and frame numbers cross to the calling thread.
/// An existing graphics.Rahmen callback is a trusted worker-side computation hook,
/// not a UI event. It must be thread-safe and must never call Godot APIs; a future
/// renderer must publish owned frame data for the Godot thread to consume.
/// </remarks>
public sealed class RgssFrameSession : IDisposable
{
    private readonly object _gate = new();
    private readonly RubyInterpreter _interpreter;
    private readonly RgssGrafikHost _graphics;
    private readonly IReadOnlyList<RubyNode> _program;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Action<int>? _previousFrame;
    private Thread? _worker;
    private RgssExecutionState _state;
    private bool _advancing;
    private bool _disposed;
    private int _frames;
    private long _droppedDiagnostics;
    private string? _error;
    private IReadOnlyList<string> _diagnostics = Array.Empty<string>();

    public RgssFrameSession(RubyInterpreter interpreter, RgssGrafikHost graphics,
        IReadOnlyList<RubyNode> program)
    {
        _interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _program = program ?? throw new ArgumentNullException(nameof(program));
        _previousFrame = graphics.Rahmen;
        _interpreter.ExecutionCancellation = _cancellation.Token;
        _interpreter.DiagnosticLimit = 64;
        _graphics.Rahmen = OnFrame;
    }

    public RgssExecutionState State { get { lock (_gate) return _state; } }
    public int Frames { get { lock (_gate) return _frames; } }
    public long DroppedDiagnosticCount { get { lock (_gate) return _droppedDiagnostics; } }
    public string? Error { get { lock (_gate) return _error; } }
    public IReadOnlyList<string> Diagnostics { get { lock (_gate) return _diagnostics; } }
    public bool IsWorkerAlive { get { lock (_gate) return _worker?.IsAlive == true; } }

    /// <summary>Run to one new frame or completion; never queue multiple permits.</summary>
    public bool Advance(TimeSpan timeout)
    {
        ValidateTimeout(timeout);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_advancing) throw new InvalidOperationException("A frame advance is already in progress.");
            if (_state is RgssExecutionState.Completed or RgssExecutionState.Cancelled or RgssExecutionState.Faulted)
                return false;

            _advancing = true;
            try
            {
                var timer = Stopwatch.StartNew();
                if (_state == RgssExecutionState.Created)
                {
                    _state = RgssExecutionState.Running;
                    _worker = new Thread(Run) { IsBackground = true, Name = "UniversalRPG RGSS scene" };
                    _worker.Start();
                }
                else
                {
                    _state = RgssExecutionState.Running;
                    Monitor.PulseAll(_gate);
                }

                while (_state == RgssExecutionState.Running)
                {
                    var remaining = timeout - timer.Elapsed;
                    if (remaining <= TimeSpan.Zero)
                    {
                        _error = "The RGSS scene did not reach a frame within the allowed time.";
                        _state = RgssExecutionState.Faulted;
                        _cancellation.Cancel();
                        Monitor.PulseAll(_gate);
                        return false;
                    }
                    Monitor.Wait(_gate, remaining);
                }
                return _state == RgssExecutionState.AtFrame;
            }
            finally
            {
                _advancing = false;
            }
        }
    }

    private void Run()
    {
        var terminal = RgssExecutionState.Completed;
        string? error = null;
        try
        {
            _interpreter.RunProgram(_program);
        }
        catch (RgssFrameBudgetErreicht)
        {
            // A diagnostic frame limit terminates the stack normally; it is not a script error.
            terminal = RgssExecutionState.Completed;
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
            terminal = RgssExecutionState.Cancelled;
        }
        catch (Exception failure)
        {
            terminal = RgssExecutionState.Faulted;
            error = failure.GetType().Name + ": " + failure.Message;
        }
        finally
        {
            lock (_gate)
            {
                CaptureDiagnostics();
                if (_state != RgssExecutionState.Faulted)
                {
                    _state = terminal;
                    _error = error;
                }
                Monitor.PulseAll(_gate);
            }
        }
    }

    private void OnFrame(int frame)
    {
        _cancellation.Token.ThrowIfCancellationRequested();
        _previousFrame?.Invoke(frame);
        lock (_gate)
        {
            _cancellation.Token.ThrowIfCancellationRequested();
            _frames = frame;
            CaptureDiagnostics();
            _state = RgssExecutionState.AtFrame;
            Monitor.PulseAll(_gate);
            while (_state == RgssExecutionState.AtFrame && !_cancellation.IsCancellationRequested)
                Monitor.Wait(_gate);
            _cancellation.Token.ThrowIfCancellationRequested();
        }
        // The next frame has its own instruction budget, not a new Ruby stack.
        _interpreter.ResetFrameStepBudget();
    }

    private void CaptureDiagnostics()
    {
        _diagnostics = Array.AsReadOnly(_interpreter.Diagnostics.ToArray());
        _droppedDiagnostics = _interpreter.DroppedDiagnosticCount;
    }

    /// <summary>Cancel and join only this session's worker, within a fixed bound.</summary>
    public bool Stop(TimeSpan timeout)
    {
        ValidateTimeout(timeout);
        Thread? worker;
        lock (_gate)
        {
            if (_disposed) return true;
            _cancellation.Cancel();
            if (_state is RgssExecutionState.Created or RgssExecutionState.AtFrame)
                _state = RgssExecutionState.Cancelled;
            Monitor.PulseAll(_gate);
            worker = _worker;
        }
        if (worker == Thread.CurrentThread)
            throw new InvalidOperationException("The scene worker cannot join itself.");
        return worker == null || worker.Join(timeout);
    }

    public void Dispose()
    {
        if (!Stop(TimeSpan.FromSeconds(2)))
            throw new TimeoutException("The RGSS worker did not stop; its live resources were retained.");
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _graphics.Rahmen = _previousFrame;
            _cancellation.Dispose();
        }
    }

    private static void ValidateTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(timeout));
    }
}
