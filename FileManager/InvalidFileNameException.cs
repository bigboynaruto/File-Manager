using System;

public class InvalidFileNameException : Exception
{
	public InvalidFileNameException(string message)
		: base(message)
	{
	}

	public InvalidFileNameException()
		: base("Недопустиме ім’я файлу!")
	{
	}
}

