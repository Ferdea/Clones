using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;

namespace Clones;

internal record StackItem<TValue>(StackItem<TValue> previous, TValue value);

internal class Stack<TValue>
{
	private StackItem<TValue> _peek;
	public int Count { get; private set; }
	public bool IsEmpty => Count == 0;
	
	public void Push(TValue value)
	{
		var newItem = new StackItem<TValue>(_peek, value);
		_peek = newItem;
		Count++;
	}

	public TValue Pop()
	{
		if (_peek == null)
			throw new InvalidOperationException();
		
		var result = _peek.value;
		_peek = _peek.previous;
		Count--;
		
		return result;
	}

	public TValue Peek()
	{
		if (_peek == null)
			throw new InvalidOperationException();

		return _peek.value;
	}

	public Stack<TValue> Clone()
	{
		return new Stack<TValue>
		{
			_peek = _peek,
			Count = Count
		};
	}
}

public class Clone<TValue>
{
	private Stack<TValue> _learnedPrograms = new ();
	private Stack<TValue> _rollbackPrograms = new ();

	public void Learn(TValue program)
	{
		_learnedPrograms.Push(program);
		_rollbackPrograms = new Stack<TValue>();
	}

	public void Rollback()
	{
		if (_learnedPrograms.IsEmpty)
			throw new InvalidOperationException();
		_rollbackPrograms.Push(_learnedPrograms.Pop());
	}

	public void Relearn()
	{
		if (_rollbackPrograms.IsEmpty)
			throw new InvalidOperationException();
		_learnedPrograms.Push(_rollbackPrograms.Pop());
	}

	public Clone<TValue> GetCopy()
	{
		return new Clone<TValue>
		{
			_learnedPrograms = _learnedPrograms.Clone(),
			_rollbackPrograms = _rollbackPrograms.Clone(),
		};
	}

	public string Check()
	{
		return _learnedPrograms.Count == 0 ? "basic" : _learnedPrograms.Peek().ToString();
	}
}

public class CloneVersionSystem<TValue> : ICloneVersionSystem where TValue : IConvertible
{
	private List<Clone<TValue>> _clones = new() { new Clone<TValue>() };

	private readonly Dictionary<string, Func<int, TValue, List<Clone<TValue>>, string>> _actions =
		new Dictionary<string, Func<int, TValue, List<Clone<TValue>>, string>>
		{
			{
				"learn", (cloneNumber, programName, clones) =>
				{
					Learn(cloneNumber, programName, clones);
					return null;
				}
			},
			{
				"rollback", (cloneNumber, _, clones) =>
				{
					Rollback(cloneNumber, clones);
					return null;
				}
			},
			{
				"relearn", (cloneNumber, _, clones) =>
				{
					Relearn(cloneNumber, clones);
					return null;
				}
			},
			{
				"clone", (cloneNumber, _, clones) =>
				{
					Clone(cloneNumber, clones);
					return null;
				}
			},
			{
				"check", Check
			}
		};
	
	public string Execute(string action, int cloneNumber, TValue program)
	{
		return _actions[action](cloneNumber, program, _clones);
	}
	
	public string Execute(string query)
	{
		var parsedInput = ParseInput(query);
		return _actions[parsedInput.Item1](parsedInput.Item2, parsedInput.Item3, _clones);
	}

	private Tuple<string, int, TValue> ParseInput(string query)
	{
		var splitInput = query.Split(' ',3);
		var command = splitInput[0];
		var cloneNumber = int.Parse(splitInput[1]);
		var program = default(TValue);
		if (splitInput.Length == 3)
			program = (TValue)Convert.ChangeType(splitInput[2].Replace("\"", ""), typeof(TValue));
		return Tuple.Create(command, cloneNumber, program);
	}

	private static void Learn(int cloneNumber, TValue program, IReadOnlyList<Clone<TValue>> clones) =>
		clones[cloneNumber - 1].Learn(program);

	private static void Rollback(int cloneNumber, IReadOnlyList<Clone<TValue>> clones) => clones[cloneNumber - 1].Rollback();

	private static void Relearn(int cloneNumber, IReadOnlyList<Clone<TValue>> clones) => clones[cloneNumber - 1].Relearn();

	private static void Clone(int cloneNumber, List<Clone<TValue>> clones) => clones.Add(clones[cloneNumber - 1].GetCopy());

	private static string Check(int cloneNumber, TValue program, List<Clone<TValue>> clones) =>
		clones[cloneNumber - 1].Check();
}

public class CloneVersionSystem : CloneVersionSystem<int>, ICloneVersionSystem
{
}

[TestFixture]
public class CloneVersionSystemGenerics_should
{
	[Test]
	public void ExecuteSample_Int()
	{
		var queries = new []
		{
			"learn 1 5",
			"learn 1 7",
			"rollback 1",
			"check 1",
			"clone 1",
			"relearn 2",
			"check 2",
			"rollback 1",
			"check 1"
		};
		
		var cvs = new CloneVersionSystem<int>();
		var results = queries
			.Select(command => cvs.Execute(command))
			.Where(result => result != null)
			.ToList();

		Assert.AreEqual(new[] { "5", "7", "basic" }, results);
	}
	
	[Test]
	public void ExecuteSample_String()
	{
		var queries = new []
		{
			"learn 1 Aboba",
			"learn 1 Amogus",
			"rollback 1",
			"check 1",
			"clone 1",
			"relearn 2",
			"check 2",
			"rollback 1",
			"check 1"
		};
		
		var cvs = new CloneVersionSystem<string>();
		var results = queries
			.Select(command => cvs.Execute(command))
			.Where(result => result != null)
			.ToList();

		Assert.AreEqual(new[] { "Aboba", "Amogus", "basic" }, results);
	}
	
	[Test]
	public void ExecuteSample_DateTime()
	{
		var firstDate = DateTime.Now;
		var secondDate = new DateTime(2008, 5, 1, 8, 30, 52);
		
		var queries = new []
		{
			$"learn 1 \"{firstDate}\"",
			$"learn 1 \"{secondDate}\"",
			"rollback 1",
			"check 1",
			"clone 1",
			"relearn 2",
			"check 2",
			"rollback 1",
			"check 1"
		};
		
		var cvs = new CloneVersionSystem<DateTime>();
		var results = queries
			.Select(command => cvs.Execute(command))
			.Where(result => result != null)
			.ToList();

		Assert.AreEqual(new[] { firstDate.ToString(), secondDate.ToString(), "basic" }, results);
	}
}