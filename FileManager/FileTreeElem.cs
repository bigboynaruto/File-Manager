using System;
using Gtk;

public class FileTreeElem : TreeElem
{
	public FileTreeElem(TreeIter iter, string fullName)
		: base(iter, fullName)
	{
	}
}