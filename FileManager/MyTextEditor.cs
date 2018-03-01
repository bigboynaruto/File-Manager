using System;
using Gtk;
using System.IO;
using System.Linq;
using System.Diagnostics;
using GLib;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections;

public class MyTextEditor : Window
{
	TextView text;
	FileInfo file;
	bool saved;

	public MyTextEditor(string fileName/*, string fileSaveToName = ""*/)
		: base("Anime editor - " + Enumerable.Last(fileName.Split('/')))
	{
		file = new FileInfo(fileName);
		//fileSaveTo = new FileInfo(fileSaveToName);
		if (!file.Exists)
			throw new FileNotFoundException("Not found", fileName);
		if (file.IsReadOnly)
			throw new UnauthorizedAccessException("Unauthorised access to file " + fileName);

		saved = true;

		DeleteEvent += (o, args) => {
			if (!saved)
				new MyMessageDialog(this, MessageType.Question, ButtonsType.YesNo, "Зберегти перед виходом?", (sender, e) => {
					if (e.ResponseId == ResponseType.Yes) {
						Save(file.FullName);
					}
				});
		};
		SetPosition(WindowPosition.Center);
		//Modal = true;
		SetDefaultSize(400, 400);

		SetupGUI();
	}

	void SetupGUI()
	{
		VBox vbox = new VBox(false, 5);

		vbox.PackStart(CreateMenu(), false, false, 0);

		ScrolledWindow sw = new ScrolledWindow();
		text = new TextView();
		text.Buffer.Text = string.Join("\n", System.IO.File.ReadAllLines(file.FullName));
		text.BorderWidth = 5;
		text.Buffer.Changed += (sender, e) => {
			//Console.WriteLine ("TEXT CHANGED"); 
			saved = false;
		};

		sw.Add(text);
		vbox.PackStart(sw, true, true, 0);

		/*
		this.tv.SizeAllocated += new SizeAllocatedHandler(Scroll2);
		public void Scroll2(object sender, Gtk.SizeAllocatedArgs e)
		{
		    tv.ScrollToIter(tv.Buffer.EndIter, 0, false, 0, 0);
		}

		*/

		Label status = new Label();
		status.Text = "みんなもそうなのかな、と思うことくらいしかできない。";
		status.HeightRequest = 20;
		status.Justify = Justification.Left;
		status.Xalign = 0;
		vbox.PackEnd(status, false, false, 0);

		Add(vbox);
		ShowAll();
	}

	MenuBar CreateMenu()
	{
		MenuBar mb = new MenuBar();
		mb.HeightRequest = 20;
		AccelGroup agr = new AccelGroup();
		AddAccelGroup(agr);

		// FILE BEGIN
		Menu fileMenu = new Menu();
		MenuItem fileMenuItem = new MenuItem("Файл");
		fileMenuItem.Submenu = fileMenu;
		MenuItem saveMenuItem = new MenuItem("Зберегти...");
		saveMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.S, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
		MenuItem saveAsMenuItem = new MenuItem("Зберегти як...");
		saveAsMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.S, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
		MenuItem exitMenuItem = new MenuItem("Вийти");
		exitMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.Q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
		
		fileMenu.Add(saveMenuItem);
		fileMenu.Add(saveAsMenuItem);
		fileMenu.Add(new SeparatorMenuItem());
		fileMenu.Add(exitMenuItem);
		// FILE END

		saveMenuItem.Activated += (sender, e) => {
			saved = true;
			Save(file.FullName);
		};
		saveAsMenuItem.Activated += (sender, e) => {
			FileChooserDialog fcd = new FileChooserDialog("Save as...", this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
			if (fcd.Run() == (int)ResponseType.Accept) 
			{
				//saved = true;
				try {
					Save(fcd.Filename);
					saved = saved && fcd.Filename == file.FullName;
				} catch (Exception err) {
					Utils.Log(err.Message);
				}
			}

			fcd.Destroy();
		};
		exitMenuItem.Activated += (sender, e) => {
			Destroy();
		};

		// EDIT BEGIN
		Menu editMenu = new Menu();
		MenuItem editMenuItem = new MenuItem("Редагувати");
		editMenuItem.Submenu = editMenu;
		MenuItem replaceMenuItem = new MenuItem("Замінити");
		MenuItem longestWordsMenuItem = new MenuItem("10 найдовших слів");
		replaceMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.R, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
		
		editMenu.Add(replaceMenuItem);
		editMenu.Add(new SeparatorMenuItem());
		editMenu.Add(longestWordsMenuItem);

		replaceMenuItem.Activated += (sender, e) => {
			try {
				MyReplaceDialog dialog = new MyReplaceDialog(file.Name);
				dialog.Response += (o, args) => {
					if (dialog.TextBefore == null)
						return;
					string[] lines = text.Buffer.Text.Split('\n');
					//text.Buffer.Text.Split("\n");
					//string[] lines = System.IO.File.ReadAllLines (path);
					for (int i = 0; i < lines.Length; i++)
						lines[i] = lines[i].Replace(dialog.TextBefore, dialog.TextAfter);
					this.text.Buffer.Text = String.Join("\n", lines);
				};
				dialog.Run();
				dialog.Destroy();
			} catch (Exception ex) {
				Utils.Log(ex.Message); 
			}
		};

		longestWordsMenuItem.Activated += (sender, e) => {
			string[] delimiters = { ",", " ", ".", "!", "?", "\"", "'", " - ", ";", "(", ")", "\n", "\t", ":", "»", "<<", ">>", "<", ">", "`", "*", "@", null };
			SortedSet<string> words = new SortedSet<string>(new MyStringComparer());
			foreach (string line in text.Buffer.Text.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)) {
				Console.WriteLine(line + " -> " + words.Count + "[" + String.Join( ", ", words) + "] -> " + words.Min + " -> " + (words.Count > 0 ? new MyStringComparer().Compare(line, words.Min) : 12));
				words.Add(line);
				if (words.Count > 10) {
					Console.Write(words.Count + " ");
					words.Remove(words.Min);
					Console.WriteLine(words.Count);
				}
			}
			new MyMessageDialog(this, MessageType.Info, ButtonsType.Close,
				"Найдовші слова:\n" +
				String.Join("\n",
					words.Select((word) => '"' + word + '"')));
		};
		// EDIT END


		// HELP BEGIN
		Menu helpMenu = new Menu();
		MenuItem helpMenuItem = new MenuItem("Довідка");
		MenuItem aboutMenuItem = new MenuItem("Про автора");
		aboutMenuItem.Activated += delegate {
			new MyAboutDialog();
		};
		helpMenuItem.Submenu = helpMenu;
		helpMenu.Add(aboutMenuItem);
		// HELP END

		mb.Append(fileMenuItem);
		mb.Append(editMenuItem);
		mb.Append(helpMenuItem);

		return mb;
	}

	void Save(string path)
	{
		if (System.IO.File.Exists(path))
			System.IO.File.WriteAllText(path, text.Buffer.Text);
		else {
			//string path = "/media/sakura/Tony Stark/terminal";
			StreamWriter writer = System.IO.File.CreateText(path);
			foreach (string line in text.Buffer.Text.Split ('\n'))
				writer.WriteLine(line);
			writer.Flush();
			writer.Close();
		}
	}
}

public class MyStringComparer : IComparer<string>
{
	public MyStringComparer()
	{
	}
	
	public int Compare(string x, string y)
	{
		if (x.Length < y.Length)
			return -1;
		if (x.Length > y.Length)
			return 1;
		return x.CompareTo(y);
	}
}

