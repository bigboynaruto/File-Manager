using System;
using Gtk;

public class DirectoryTreeElem : TreeElem
{
	public DirectoryTreeElem(TreeIter iter, string fullName)
		: base(iter, fullName)
	{
		IsOpen = false;
	}

	public bool IsOpen { get; set; }
}

