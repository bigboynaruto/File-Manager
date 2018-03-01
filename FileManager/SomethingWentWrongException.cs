using System;

public class SomethingWentWrongException : Exception
{
	public SomethingWentWrongException()
		: base("Щось пішло не так!!!!")
	{
	}
}

