using System;
using Gtk;
using System.IO;

abstract public class TreeElem
{

	public TreeElem (TreeIter iter, string fullName)
	{
		Iter = iter;
		FullName = fullName;
	}

	public TreeIter Iter { get; set; }
	public string FullName { get; set; }

	public string Name
	{
		get
		{
			string[] arr = FullName.Split ('/');
			return arr [arr.Length - 1];
		}
	}

	public string DirPath
	{
		get 
		{
			string[] arr = FullName.Split ('/');
			string path = "";
			for (int i = 0; i < arr.Length - 1; i++)
				path += arr [i] + Path.DirectorySeparatorChar.ToString();
			return path;
		}
	}

	public override bool Equals(System.Object obj)
	{
		if (obj == null) {
			return false;
		}

		TreeElem p = obj as TreeElem;
		if ((TreeElem)p == null) {
			return false;
		}

		return FullName.Equals(p.FullName);
	}

	public bool Equals(TreeElem p)
	{
		// If parameter is null return false:
		if ((object)p == null)
		{
			return false;
		}

		// Return true if the fields match:
		return FullName.Equals(p.FullName);
	}

	public override int GetHashCode()
	{
		return 0;
	}
}
