using System;
using System.Runtime.CompilerServices;
using System.Linq;
using System.IO;

public class Utils
{
	public static StreamWriter LogFileWriter { get; private set; }

	public static void Log(String message = "", [CallerFilePath] String callerFilename = null, 
	                       [CallerMemberName] String methodName = null, [CallerLineNumber] int lineNumber = 0)
	{

		String filename = Enumerable.Last(callerFilename.Split('/'));
		Console.WriteLine($"[{filename}:{lineNumber} ({methodName})]: {message}");
	}

	public static void FileLog(String message = "", [CallerFilePath] String callerFilename = null, 
		[CallerMemberName] String methodName = null, [CallerLineNumber] int lineNumber = 0)
	{
		Utils.Log(message, callerFilename, methodName, lineNumber);
		//String filename = Enumerable.Last(callerFilename.Split('/'));
		try {
			LogFileWriter = File.AppendText("log.txt");
			String filename = Enumerable.Last(callerFilename.Split('/'));

			//LogFileWriter.WriteLine($"[{filename}:{lineNumber} ({methodName})]: {message}");
			LogFileWriter.Write("Log Entry : ");
			LogFileWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
			LogFileWriter.WriteLine($"\t:[{filename}: {methodName}: {lineNumber}]");
			LogFileWriter.WriteLine("\t:{0}", message);
			LogFileWriter.WriteLine("-------------------------------------------------------------");
			LogFileWriter.Flush();
		} catch {
		} finally {
			LogFileWriter.Close();
		}
	}

	public static void  LogNewSession() {
		try {
			/*if (!File.Exists("log.txt")) {
				LogFileWriter = File.Create("log.txt");
			} else */
				LogFileWriter = File.AppendText("log.txt");

			LogFileWriter.WriteLine("------------------------START SESSION------------------------");
			LogFileWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
			LogFileWriter.WriteLine("-------------------------------------------------------------");
			LogFileWriter.Flush();
		} catch {
		} finally {
			LogFileWriter.Close();
		}
	}

	public static void LogEndSession() {
		try {
			LogFileWriter.WriteLine("-------------------------END SESSION-------------------------");
			LogFileWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
			LogFileWriter.WriteLine("-------------------------------------------------------------");
			LogFileWriter.Close();
		} catch {
		}
	}

	public static void ClearLogFile() {
		//LogFileWriter.Cl
	}
}
