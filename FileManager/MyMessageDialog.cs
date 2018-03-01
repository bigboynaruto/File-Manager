using System;
using Gtk;

public class MyMessageDialog : MessageDialog
{
	public MyMessageDialog(Window parent_window, MessageType type, ButtonsType butts, string message, ResponseHandler resp = null)
		: base(parent_window, DialogFlags.DestroyWithParent, type, butts, message)
	{ 
		if (resp != null)
			Response += resp;
		Run();
		Destroy();
	}
}
