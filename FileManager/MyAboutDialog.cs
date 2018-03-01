using System;
using Gtk;
using System.IO;
using Gdk;

public class MyAboutDialog : AboutDialog
{
	public MyAboutDialog()
		: base()
	{
		ProgramName = "File Manager";
		Copyright = "(c) Перепелиця Антон";
		Website = "https://github.com/bigboynaruto";
		WebsiteLabel = "bigboynaruto - GitHub";
		Comments = "Звичайний файловий менеджер";
		Run();
		Destroy();
	}
}

