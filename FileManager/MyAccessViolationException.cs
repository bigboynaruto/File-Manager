using System;

public class MyAccessViolationException : AccessViolationException
{
	public MyAccessViolationException(string message)
		: base(message)
	{
	}

	public MyAccessViolationException()
		: base("Файл зайнятий іншим потоком! Звільніть його і повторіть знову!")
	{
	}
}
