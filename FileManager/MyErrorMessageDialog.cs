using System;
using Gdk;
using Gtk;

public class MyErrorMessageDialog : MyMessageDialog
{
	public MyErrorMessageDialog(Gtk.Window parent_window, String message)
		:base(parent_window, MessageType.Error, ButtonsType.Cancel, message)
	{
	}
}
