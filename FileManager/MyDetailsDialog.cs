using System;
using Gtk;
using System.IO;
using Gdk;

public class MyDetailsDialog : Gtk.Window
{
	Entry nameEntry;
	Pixbuf icon;
	Table t;
	MyFileSystemManager mfs;
	FileSystemInfo fsi;

	public MyDetailsDialog(MyFileSystemManager mfs, FileSystemInfo fsi)
		: base("Деталі " + fsi.Name)
	{
		this.mfs = mfs;
		this.Modal = true;
		BorderWidth = 10;
		SetDefaultSize(400, 400);
		Resizable = false;
		this.fsi = fsi;

		if (fsi as DirectoryInfo != null)
			GetDirectoryInfo();
		else if (fsi as FileInfo != null)
			GetFileInfo();
	
		SetUpGUI();
		ShowAll();
	}

	void SetUpGUI()
	{
		HBox hbox = new HBox(false, 10);

		Fixed f = new Fixed();
		f.Put(new Gtk.Image(icon), 0, 0);
		hbox.PackStart(f);
		hbox.PackStart(t);

		Add(hbox);
	}

	void GetFileInfo()
	{
		FileInfo fi = fsi as FileInfo;
		nameEntry = new Entry();
		icon = MainClass.GetIcon(fi.Extension, MyTreeView.ICON_SIZE);

		t = new Table(4, 2, false);

		for (uint i = 0; i < t.NColumns; i++)
			t.SetColSpacing(i, 15);
		for (uint i = 0; i < t.NRows; i++)
			t.SetRowSpacing(i, 10);

		t.Attach(GetLabelLeftJustified("Ім’я:", true), 0, 1, 0, 1);
		t.Attach(nameEntry, 1, 2, 0, 1);
		nameEntry.Text = System.IO.Path.GetFileNameWithoutExtension(fi.Name);
		//nameEntry.KeyPressEvent += OnKeyPressEvent;

		t.Attach(GetLabelLeftJustified("Тип:", true), 0, 1, 1, 2);
		t.Attach(GetLabelLeftJustified(MyFileSystem.GetContentType(fi.FullName)), 1, 2, 1, 2);

		t.Attach(GetLabelLeftJustified("Знаходження:", true), 0, 1, 2, 3);
		t.Attach(GetLabelLeftJustified(fi.Directory.FullName), 1, 2, 2, 3);

		t.Attach(GetLabelLeftJustified("Розмір:", true), 0, 1, 3, 4);
		t.Attach(GetLabelLeftJustified(MyFileSystem.ToFileSize(fi.Length)), 1, 2, 3, 4);

		t.Attach(GetLabelLeftJustified("Доступ:", true), 0, 1, 4, 5);
		t.Attach(GetLabelLeftJustified(fi.LastAccessTime.ToLongDateString() + " " + fi.LastAccessTime.ToLongTimeString()), 1, 2, 4, 5);

		t.Attach(GetLabelLeftJustified("Зміни:", true), 0, 1, 5, 6);
		t.Attach(GetLabelLeftJustified(fi.LastWriteTime.ToLongDateString() + " " + fi.LastWriteTime.ToLongTimeString()), 1, 2, 5, 6);
	}

	Label GetLabelLeftJustified(string s, bool bold = false)
	{
		Label l = new Label(s);
		if (bold)
			l.Markup = "<b>" + l.Text + "</b>";
		l.Justify = Justification.Left;
		l.Xalign = 0;
		l.SingleLineMode = false;
		l.LineWrap = true;
		//l.WidthRequest = 200;

		return l;
	}

	void GetDirectoryInfo()
	{
		DirectoryInfo di = fsi as DirectoryInfo;

		nameEntry = new Entry();
		icon = MainClass.GetIcon(".folder-3", MyTreeView.ICON_SIZE);

		t = new Table(4, 2, false);

		for (uint i = 0; i < t.NColumns; i++)
			t.SetColSpacing(i, 15);
		for (uint i = 0; i < t.NRows; i++)
			t.SetRowSpacing(i, 10);

		t.Attach(GetLabelLeftJustified("Ім’я:", true), 0, 1, 0, 1);
		t.Attach(nameEntry, 1, 2, 0, 1);
		nameEntry.Text = System.IO.Path.GetFileNameWithoutExtension(di.Name);
		//nameEntry.KeyPressEvent += OnKeyPressEvent;

		if (di.Parent != null) {
			t.Attach(GetLabelLeftJustified("Знаходження:", true), 0, 1, 1, 2);
			t.Attach(GetLabelLeftJustified(di.Parent.FullName), 1, 2, 1, 2);
		}

		t.Attach(GetLabelLeftJustified("Вміст:", true), 0, 1, 2, 3);
		t.Attach(GetLabelLeftJustified(di.GetFiles("*.*").Length + " файлів, " + di.GetDirectories("*.*").Length + " тек"), 1, 2, 2, 3);

		t.Attach(GetLabelLeftJustified("Доступ:", true), 0, 1, 3, 4);
		t.Attach(GetLabelLeftJustified(di.LastAccessTime.ToLongDateString() + " " + di.LastAccessTime.ToLongTimeString()), 1, 2, 3, 4);

		t.Attach(GetLabelLeftJustified("Зміни:", true), 0, 1, 4, 5);
		t.Attach(GetLabelLeftJustified(di.LastWriteTime.ToLongDateString() + " " + di.LastWriteTime.ToLongTimeString()), 1, 2, 4, 5);
	}

	[GLib.ConnectBefore]
	protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
	{
		if (!nameEntry.IsFocus)
			return false;
		if (evnt.Key.ToString().Equals("Return") || evnt.Key.ToString().Equals("KP_Enter")) {
			Console.WriteLine(nameEntry.Text);
			if (!nameEntry.Text.Trim().Equals(System.IO.Path.GetFileNameWithoutExtension(fsi.FullName))) {
				string fname = nameEntry.Text.Trim();

				//fi.MoveTo (System.IO.Path.Combine (fi.Directory, fname));
				FileSystemInfo _fsi = mfs.Rename(fsi.FullName, fname);
				if (_fsi as FileInfo != null || _fsi as DirectoryInfo != null) {
					fsi = _fsi;
					Title = "Деталі " + fname;
				} 
			}
		}

		return base.OnKeyPressEvent(evnt);
	}
}

