using System;

public class FileNotChosenException : Exception
{
	public FileNotChosenException()
		: base("Ви не обрали файл або теку!")
	{
	}

	public FileNotChosenException(string message)
		:base(message)
	{
	}
}

