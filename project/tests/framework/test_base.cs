using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace UniversalRPG.Tests.Framework;

/// <summary>
/// Minimal test base class for C# suites. Suites define Setup()/Teardown() and
/// Test* methods; assertions record failures instead of aborting, so every
/// test in a suite runs.
/// </summary>
public abstract partial class TestBase : RefCounted
{
	public class SuiteResult
	{
		public int Tests;
		public int Passed;
		public int Failed;
		public List<string> Failures { get; } = new();
	}

	private readonly List<string> _failures = new();
	private int _assertions;

	public virtual void Setup()
	{
	}

	public virtual void Teardown()
	{
	}

	public SuiteResult RunAll()
	{
		var result = new SuiteResult();
		var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
		Array.Sort(methods, (pLeft, pRight) => string.CompareOrdinal(pLeft.Name, pRight.Name));
		foreach (var method in methods)
		{
			if (!method.Name.StartsWith("Test_", StringComparison.Ordinal))
			{
				continue;
			}
			result.Tests += 1;
			RunTest(method, result);
		}
		return result;
	}

	private void RunTest(MethodInfo pMethod, SuiteResult pResult)
	{
		_failures.Clear();
		_assertions = 0;
		try
		{
			Setup();
			if (_failures.Count > 0)
			{
				Fail("Setup() failed before test ran");
			}
			else
			{
				pMethod.Invoke(this, null);
			}
		}
		catch (Exception exception)
		{
			Fail($"Unhandled exception: {exception.InnerException?.Message ?? exception.Message}");
		}
		finally
		{
			try
			{
				Teardown();
			}
			catch (Exception teardownException)
			{
				Fail($"Teardown() failed: {teardownException.Message}");
			}
		}

		if (_failures.Count == 0)
		{
			pResult.Passed += 1;
		}
		else
		{
			pResult.Failed += 1;
			foreach (var failure in _failures)
			{
				pResult.Failures.Add($"{pMethod.Name}: {failure}");
			}
		}
	}

	protected void Fail(string pMessage)
	{
		_failures.Add(pMessage);
	}

	protected void AssertTrue(bool pCondition, string pMessage = "Expected true")
	{
		_assertions += 1;
		if (!pCondition)
		{
			Fail(pMessage);
		}
	}

	protected void AssertFalse(bool pCondition, string pMessage = "Expected false")
	{
		_assertions += 1;
		if (pCondition)
		{
			Fail(pMessage);
		}
	}

	protected void AssertEq<T>(T pActual, T pExpected, string pMessage = "")
	{
		_assertions += 1;
		if (!EqualityComparer<T>.Default.Equals(pActual, pExpected))
		{
			var detail = $"Expected {pExpected}, got {pActual}";
			Fail(string.IsNullOrEmpty(pMessage) ? detail : pMessage);
		}
	}

	protected void AssertNe<T>(T pActual, T pExpected, string pMessage = "Values should differ")
	{
		_assertions += 1;
		if (EqualityComparer<T>.Default.Equals(pActual, pExpected))
		{
			Fail($"{pMessage} (both are {pActual})");
		}
	}
}
