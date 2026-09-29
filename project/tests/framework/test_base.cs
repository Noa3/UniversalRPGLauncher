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

	/// <summary>
	/// The error a script raised, and a failure when it did not.
	/// </summary>
	/// <param name="pWas">What to run.</param>
	/// <returns>The error.</returns>
	/// <remarks>
	/// <strong>And a missing error is a failure and not a nil.</strong>
	/// <c>raise</c> that does not stop the script is the whole thing the
	/// tests that use this are about,
	/// <strong>and a test that went quietly on when no error came would not
	/// see the very thing it is looking for</strong> — and it would pass on a
	/// reader that runs a game past its own error path.
	/// <strong>And it lives here and not in one test file</strong>, because
	/// three of them ask the same question
	/// **and a helper a second file has to borrow is a helper whose home
	/// was wrong.**
	/// </remarks>
	protected static T FehlerAus<T>(System.Action pWas)
		where T : System.Exception
	{
		try
		{
			pWas();
		}
		catch (T ausnahme)
		{
			return ausnahme;
		}

		throw new System.InvalidOperationException(
			"the script did not raise, and that is the thing under test");
	}
}
