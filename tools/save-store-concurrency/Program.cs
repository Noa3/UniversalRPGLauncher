using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UniversalRPG.Web;

internal static class Program
{
    private const int WorkerCount = 8;
    private const int WritesPerWorker = 32;

    private static int Main(string[] args)
    {
        if (args.Length == 4 && args[0] == "worker")
        {
            return Write(args[1], int.Parse(args[2]), args[3]);
        }
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: SaveStoreConcurrency <scratch-root>");
            return 2;
        }
        var directory = Path.Combine(Path.GetFullPath(args[0]), "urpg-cross-process-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var ready = Path.Combine(directory, "start.signal");
        var workers = new List<Process>();
        try
        {
            for (var i = 0; i < WorkerCount; i++)
            {
                var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
                start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
                start.ArgumentList.Add("worker");
                start.ArgumentList.Add(directory);
                start.ArgumentList.Add(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                start.ArgumentList.Add(ready);
                workers.Add(Process.Start(start) ?? throw new IOException("Worker did not start."));
            }
            File.WriteAllText(ready, "start");
            var timeout = Stopwatch.StartNew();
            var allSucceeded = true;
            foreach (var worker in workers)
            {
                var remaining = Math.Max(1, 60000 - (int)timeout.ElapsedMilliseconds);
                if (!worker.WaitForExit(remaining)) { throw new TimeoutException("Save workers timed out."); }
                allSucceeded &= worker.ExitCode == 0;
            }
            if (!allSucceeded) { throw new IOException("At least one concurrent writer failed."); }
            if (!MzSaveStore.TryLoad(directory, "file1", out var value, out var problem))
            {
                throw new IOException("Final save is unreadable: " + problem);
            }
            if (value == null || value.Kind != MzKind.Number || value.Number != Math.Floor(value.Number)
                || value.Number < 0 || value.Number >= WorkerCount * WritesPerWorker)
            {
                throw new InvalidDataException("Final save is not one complete submitted value.");
            }
            if (Directory.GetFiles(directory).Length != 2)
            {
                throw new IOException("Save staging files were left behind.");
            }
            Console.WriteLine($"PASS: {WorkerCount} independent processes, {WorkerCount * WritesPerWorker} committed writes; final value {value.Number}; no staging files remain.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            foreach (var worker in workers)
            {
                if (!worker.HasExited) { worker.Kill(entireProcessTree: true); worker.WaitForExit(); }
                worker.Dispose();
            }
            Directory.Delete(directory, recursive: true);
        }
    }

    private static int Write(string directory, int worker, string ready)
    {
        var timer = Stopwatch.StartNew();
        while (!File.Exists(ready))
        {
            if (timer.ElapsedMilliseconds > 10000) { return 3; }
            System.Threading.Thread.Sleep(5);
        }
        for (var i = 0; i < WritesPerWorker; i++)
        {
            var value = new MzValue(MzKind.Number) { Number = worker * WritesPerWorker + i };
            if (!MzSaveStore.Save(directory, "file1", value, out var problem))
            {
                Console.Error.WriteLine($"Worker {worker} write {i}: {problem}");
                return 1;
            }
        }
        return 0;
    }
}
