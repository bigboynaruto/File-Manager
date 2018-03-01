using System;
using System.Collections.Generic;
using System.IO;
using Gtk;
using System.Linq;
using System.Collections;
using GLib;
using System.Runtime.InteropServices;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Security.Cryptography;
using Gdk;

public class MyFileSystem
{
	//public MyTreeView[] Trees { get; private set;	 }
	public List<IObserver> Trees { get; private set; }
	protected int currentTree;

	public List<Dictionary<string, MyTreeView>> Volumes;

	public MyFileSystem()
	{
		Volumes = new List<Dictionary<string, MyTreeView>>(MainClass.TREES_COUNT);
		Trees = new List<IObserver>(MainClass.TREES_COUNT);

		for (int i = 0; i < MainClass.TREES_COUNT; i++)
			AddVolume(i);
	}

	public void AddVolume(int i) {
		Volumes.Add(new Dictionary<string, MyTreeView>());

		DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
		foreach (DriveInfo di in drives) {
			try {
				di.RootDirectory.GetFiles();
			} catch (Exception e) {
				Utils.Log(e.Message);
				continue;
			}
			if (di.TotalFreeSpace <= 0 || !di.IsReady || (di.DriveType != DriveType.Ram && di.RootDirectory.Name != "/"))
				continue;

			Volumes[i].Add(di.RootDirectory.Name, new MyTreeView(di.RootDirectory.FullName));
			Volumes[i][di.RootDirectory.Name].FolderTree.FocusGrabbed += (sender, e) => {
				currentTree = i * 2;
				MainClass.AppWindow.Status = (Trees[i] as MyTreeView).SelectedFolder;
			};
			Volumes[i][di.RootDirectory.Name].FileTree.FocusGrabbed += (sender, e) => {
				currentTree = i * 2 + 1;
				MainClass.AppWindow.Status = (Trees[i] as MyTreeView).SelectedFile;
			};
		}

		Trees.Add(new MyTreeView(Volumes[i].Values.First().Root.FullName));
	}

	public void ChangeTree(int i = 1) {
		currentTree = (currentTree + (i > 0 ? 2 : -2 + MainClass.TREES_COUNT)) % (MainClass.TREES_COUNT * 2);
		if (currentTree % 2 == 0)
			(Trees[currentTree] as MyTreeView).FolderTree.GrabFocus();
		else 
			(Trees[currentTree] as MyTreeView).FileTree.GrabFocus();
	}

	public bool IsValidFileName(string file)
	{
		return !string.IsNullOrEmpty(file) && file.IndexOfAny(Path.GetInvalidFileNameChars()) < 0; 
	}

	public static bool FileExists(string folder, string file)
	{
		return FileExists(Path.Combine(folder, file));
	}

	public static bool FileExists(string file)
	{
		return File.Exists(file) || Directory.Exists(file);
	}

	public static bool IsValidHtmlFile(FileInfo fi)
	{
		return Array.IndexOf(new string[]{ ".htm", ".html" }, fi.Extension) != -1;
	}

	public static string ToFileSize(double value)
	{
		string[] suffixes = { "b", "KB", "MB", "GB",
			"TB", "PB", "EB", "ZB", "YB"
		};
		for (int i = 0; i < suffixes.Length; i++) {
			if (value <= (Math.Pow(1024, i + 1))) {
				return ThreeNonZeroDigits(value /
				Math.Pow(1024, i)) +
				" " + suffixes[i];
			}
		}

		return ThreeNonZeroDigits(value /
		Math.Pow(1024, suffixes.Length - 1)) +
		" " + suffixes[suffixes.Length - 1];
	}

	public static string ThreeNonZeroDigits(double value)
	{
		if (value >= 100) {
			// No digits after the decimal.
			return value.ToString("0,0");
		} else if (value >= 10) {
			// One digit after the decimal.
			return value.ToString("0.0");
		} else {
			// Two digits after the decimal.
			return value.ToString("0.00");
		}
	}

	public static string ToShortPath(String path) 
	{
		String[] shortDirPathArray = path.Split ('/');
		String shortDirPath = "";
		if (path.StartsWith (Path.DirectorySeparatorChar.ToString())) shortDirPath += Path.DirectorySeparatorChar.ToString();
		for (int i = 0; i < shortDirPathArray.Length - 1; i++) 
		{
			if (!shortDirPathArray[i].Equals (String.Empty))
				shortDirPath += shortDirPathArray[i][0] + Path.DirectorySeparatorChar.ToString();
		}
		shortDirPath += shortDirPathArray[shortDirPathArray.Length - 1];

		return shortDirPath;
	}


	[DllImport("libfmutil.so", EntryPoint = "guess_content_type")]
	public static extern string GetContentType(string path);
}