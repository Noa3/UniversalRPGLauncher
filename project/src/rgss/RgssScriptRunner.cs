using System;
using System.Linq;
using System.Text;

namespace UniversalRPG.Rgss;

public enum RgssScriptRunState { Pending, Running, Completed, Cancelled, FrameBudget, Faulted }

/// <summary>Immutable execution receipt. A running entry script has not completed, even after many frames.</summary>
public sealed record RgssScriptProgress(int Total, int Attempted, int Completed, int Failed,
    string? CurrentScript, RgssScriptRunState State);

/// <summary>Executes the project's ordered script list once, on its interpreter's owner thread.</summary>
internal sealed class RgssScriptRunner
{
    private readonly object _gate = new();
    private RgssScriptProgress _progress = new(0, 0, 0, 0, null, RgssScriptRunState.Pending);
    public RgssScriptProgress Progress { get { lock (_gate) return _progress; } }

    public void Prepare(int count)
    {
        lock (_gate)
        {
            if (_progress.State != RgssScriptRunState.Pending)
                throw new InvalidOperationException("Project script execution cannot be restarted.");
            _progress = _progress with { Total = count };
        }
    }

    public void Run(RgssSkriptHost host, RubyInterpreter interpreter, Action<string, Exception> report)
    {
        var names = host.Namen.ToArray();
        Prepare(names.Length);
        lock (_gate) _progress = _progress with { State = RgssScriptRunState.Running };
        try
        {
            foreach (var name in names)
            {
                interpreter.ExecutionCancellation.ThrowIfCancellationRequested();
                lock (_gate) _progress = _progress with
                {
                    Attempted = _progress.Attempted + 1,
                    CurrentScript = name,
                };
                try
                {
                    var bytes = host.ReadScript(name, true)
                        ?? throw new InvalidOperationException("The project script source is unavailable.");
                    var nodes = new RubyParser(new RubyLexer(Encoding.UTF8.GetString(bytes)).Tokenize()).ParseProgram();
                    interpreter.RunProgram(nodes);
                    lock (_gate) _progress = _progress with { Completed = _progress.Completed + 1 };
                }
                catch (Exception error) when (error is not OperationCanceledException and not RgssFrameBudgetErreicht)
                {
                    report(name, error);
                    lock (_gate) _progress = _progress with { Failed = _progress.Failed + 1 };
                }
            }
            lock (_gate) _progress = _progress with { State = RgssScriptRunState.Completed, CurrentScript = null };
        }
        catch (OperationCanceledException)
        {
            lock (_gate) _progress = _progress with { State = RgssScriptRunState.Cancelled };
            throw;
        }
        catch (RgssFrameBudgetErreicht)
        {
            lock (_gate) _progress = _progress with { State = RgssScriptRunState.FrameBudget };
            throw;
        }
        catch
        {
            lock (_gate) _progress = _progress with { State = RgssScriptRunState.Faulted };
            throw;
        }
    }
}
