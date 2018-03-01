using System;

public class FileAlreadyExistsException : Exception
{
	public FileAlreadyExistsException(string message)
		: base(message)
	{
	}

	public FileAlreadyExistsException()
		: base("Файл або тека з таким ім’ям уже існує!")
	{
	}
}

