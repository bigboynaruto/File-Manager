using System;
using Gtk;
using System.IO;
using Gdk;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Security.Cryptography;
using Pango;
using System.Runtime.CompilerServices;
using System.Linq;
using System.ComponentModel;
using System.Collections.Specialized;
using Glade;

public class MainClass : Gtk.Window
{
	public static int TREES_COUNT = 2;

	List<ComboBox> cb;


	List<TreeView> trees;
	List<ScrolledWindow> sw;
	// sw[0], sw[1], sw[2], sw[3];
	List<Frame> frames;
	// frame[0], frame[1]
	List<Entry> entries;
	Label StatusLabel;

	MyFileSystem fs;

	HBox panelHBox;

	int w, h;

	public static MainClass AppWindow { get; private set; }
	public string Status { get { return StatusLabel.Text; } set { StatusLabel.Text = value; } }

	public MainClass()
		: base("Anime Explorer")
	{
		Utils.LogNewSession();
		Screen.ActiveWindow.GetSize(out w, out h);
		//SetIconFromFile("pokemon-icon.png");
		Icon = Gtk.IconTheme.Default.LoadIcon(Stock.Harddisk, (int)IconSize.Dialog, IconLookupFlags.NoSvg);
		//Icon = Gtk.IconTheme.Default.LoadIcon (Stock.Harddisk, 48, (IconLookupFlags) 0);//new Gtk.Image(Stock.DialogError, IconSize.Dialog).Pixbuf;
		SetPosition(WindowPosition.Center);
		DeleteEvent += delegate {
			Utils.LogEndSession();
			Gtk.Application.Quit();
		};

		fs = new MyFileSystemManager(); 

		SetupGUI();
		FillComboBoxes();

		AppWindow = this;
	}

	void SetupGUI()
	{
		entries = new List<Entry>(TREES_COUNT);
		sw = new List<ScrolledWindow>(TREES_COUNT * 2);
		trees = new List<TreeView>(TREES_COUNT * 2);
		cb = new List<ComboBox>(TREES_COUNT);
		frames = new List<Frame>(TREES_COUNT);

		VBox vbox = new VBox(false, 2);
		vbox.PackStart(CreateMenu(), false, false, 0);

		VBox vbox1 = new VBox(false, 5);

		//HBox hbox1 = new HBox(true, 5);
		panelHBox = new HBox(true, 5);

		for (int i = 0; i < TREES_COUNT * 2; i += 2) {
			panelHBox.PackStart(CreateFrame(i, $"Панель {i/2}:"), true, true, 5);
		}
		panelHBox.SetSizeRequest(w, h);
		vbox1.PackStart(panelHBox, true, true, 0);

		StatusLabel = new Label();
		StatusLabel.Text = GetRandomAnimeTitle();
		StatusLabel.SetSizeRequest(w, 20);
		StatusLabel.Justify = Justification.Left;
		StatusLabel.Xalign = 0;

		vbox.PackStart(vbox1, true, true, 0);
		vbox.PackEnd(StatusLabel, false, false, 0);

		Add(vbox);

		ShowAll();
	}

	Frame CreateFrame(int index, string title = "") {
		frames.Add(new Frame(title));
		//frames[index / 2].SetSizeRequest((int)(w * 1.0 / TREES_COUNT), (int)(h * 0.9));

		VBox vbox12 = new VBox(false, 5); 
		HBox hbox13 = new HBox(false, 5);

		trees.Add(((fs as MyFileSystemManager).Trees[index / 2] as MyTreeView).FolderTree);
		trees.Add(((fs as MyFileSystemManager).Trees[index / 2] as MyTreeView).FileTree);

		sw.Add(new ScrolledWindow());
		sw[index].Add(trees[index]);
		//sw[index].SetSizeRequest((int)(w * (1.0 / TREES_COUNT) * 0.3), (int)(h * 0.9 * 0.9));
		hbox13.PackStart(sw[index]);

		sw.Add(new ScrolledWindow());
		sw[index + 1].Add(trees[index + 1]);
		hbox13.PackStart(sw[index + 1]);

		HBox hbox14 = new HBox(true, 5);

		cb.Add(new ComboBox());
		entries.Add(new Entry());
		entries[index / 2].Changed += delegate {
			foreach (MyTreeView mtv in (fs as MyFileSystemManager).Volumes[index / 2].Values)
				mtv.Filter = entries[index / 2].Text.Trim().Equals(string.Empty) ? "*" : entries[index / 2].Text;
		};
		hbox14.PackStart(cb[index / 2], true, true, 0);
		hbox14.PackStart(entries[index / 2], true, true, 0);
		hbox14.PackStart(new Label(), false, false, 0);

		vbox12.PackStart(hbox13, true, true, 0);
		vbox12.PackStart(hbox14, false, false, 0);

		frames[index / 2].Add(vbox12);

		return frames[index / 2];
	}

	void FillComboBoxes()
	{
		for (int j = 0; j < TREES_COUNT * 2; j += 2) {
			AddComboBox(j);
		}
	}

	private void AddComboBox(int j) {
		int i = j;
		cb[i / 2].Clear();

		CellRendererText cell = new CellRendererText();
		cb[i / 2].PackStart(cell, false);
		cb[i / 2].AddAttribute(cell, "text", 0);
		ListStore store = new ListStore(typeof(string));
		cb[i / 2].Model = store;

		foreach (string vol in (fs as MyFileSystemManager).Volumes[i / 2].Keys)
			store.AppendValues(vol);


		cb[i / 2].Changed += (sender, e) => {
			if ((fs as MyFileSystemManager).Trees[i / 2] == null) {
				Utils.Log("NOT GOOD");
				return;
			}
			sw[i].Remove(trees[i]);
			sw[i + 1].Remove(trees[i + 1]);
			(fs as MyFileSystemManager).Trees[i / 2] = (fs as MyFileSystemManager).Volumes[i / 2][cb[i / 2].ActiveText];
			trees[i] = ((fs as MyFileSystemManager).Trees[i / 2] as MyTreeView).FolderTree;
			trees[i + 1] = ((fs as MyFileSystemManager).Trees[i / 2] as MyTreeView).FileTree;

			sw[i].Add(trees[i]);
			sw[i + 1].Add(trees[i + 1]);

			/*
			if (TREES_COUNT == 2) {
				bool rhidden = frames[((i + 2) % (TREES_COUNT * 2)) / 2].Visible;
				ShowAll();
				frames[((i + 2) % (TREES_COUNT * 2)) / 2].Visible = rhidden;
			} else*/ ShowAll();
		};

		TreeIter iter;
		cb[i / 2].Model.GetIterFirst(out iter);
		cb[i / 2].SetActiveIter(iter);
		(fs as MyFileSystemManager).Trees[i / 2] = (fs as MyFileSystemManager).Volumes[i / 2][cb[i / 2].ActiveText];
	}

	MenuBar CreateMenu()
	{
		MenuBar mb = new MenuBar();

		AccelGroup agr = new AccelGroup();
		AddAccelGroup(agr);

		//ImageMenuItem imi = new ImageMenuItem(Stock.New, agr);

		// mod1_mask - alt
		// FILE BEGIN
		Menu fileMenu = new Menu();
		MenuItem fileMenuItem = new MenuItem("Файл");
		fileMenuItem.Submenu = fileMenu;
		MenuItem newMenuItem = new MenuItem("Новий");
		Menu newMenu = new Menu();
		newMenuItem.Submenu = newMenu;
		MenuItem newFileMenuItem = new MenuItem("Файл...");
		newFileMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.n, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
		MenuItem newFolderMenuItem = new MenuItem("Тека...");
		newFolderMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.N, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
		//MenuItem openMenuItem = new MenuItem("Відкрити");
		MenuItem searchMenuItem = new MenuItem("Пошук");
		MenuItem detailsMenuItem = new MenuItem("Деталі");
		detailsMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.i, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
		MenuItem hyperMenuItem = new MenuItem("Файли з гіперпосилань");
		MenuItem exitMenuItem = new MenuItem("Вийти");
		exitMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.Q, Gdk.ModifierType.ControlMask, AccelFlags.Visible));

		newMenu.Add(newFileMenuItem);
		newMenu.Add(newFolderMenuItem);

		fileMenu.Add(newMenuItem);
		fileMenu.Add(new SeparatorMenuItem());
		//fileMenu.Add (searchMenuItem);
		fileMenu.Add(detailsMenuItem);
		fileMenu.Add(hyperMenuItem);
		fileMenu.Add(new SeparatorMenuItem());
		fileMenu.Add(exitMenuItem);

		newFileMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).NewFile(); };
		newFolderMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).NewFolder(); };
		searchMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).Search(); };
		detailsMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).ShowDetails(); };
		hyperMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).FindAllHtmlLinks(); };
		exitMenuItem.Activated += delegate {
			MessageDialog md = new MessageDialog(this, 
				                   DialogFlags.DestroyWithParent, MessageType.Question, 
				                   ButtonsType.YesNo, "Ви впевнені?");
			md.Response += delegate (object o, ResponseArgs resp) {
				if (resp.ResponseId == ResponseType.Yes) {
					Application.Quit();
				}
			};
			md.Run();
			md.Destroy();
		};
		// FILE END

		// EDIT BEGIN
		Menu editMenu = new Menu();
		MenuItem editMenuItem = new MenuItem("Редагувати");
		editMenuItem.Submenu = editMenu;
		MenuItem moveMenuItem = new MenuItem("Перемістити");
		moveMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.F7, Gdk.ModifierType.None, AccelFlags.Visible));
		MenuItem copyMenuItem = new MenuItem("Копіювати");
		copyMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.F6, Gdk.ModifierType.None, AccelFlags.Visible));
		MenuItem pasteMenuItem = new MenuItem("Вставити");
		pasteMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.F5, Gdk.ModifierType.None, AccelFlags.Visible));
		MenuItem deleteMenuItem = new MenuItem("Видалити");
		deleteMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.F8, Gdk.ModifierType.None, AccelFlags.Visible));
		MenuItem renameMenuItem = new MenuItem("Перейменувати");
		renameMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.F2, Gdk.ModifierType.None, AccelFlags.Visible));
		MenuItem splitFileMenuItem = new MenuItem("Розділити");
		/*splitFileMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.F1, Gdk.ModifierType.None, AccelFlags.Visible));*/
		MenuItem editorMenuItem = new MenuItem("Текстовий редактор");
		editorMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.F3, Gdk.ModifierType.None, AccelFlags.Visible));

		moveMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).Move(); };
		copyMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).Copy(); };
		pasteMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).Paste(); };
		deleteMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).Delete(); };
		renameMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).Rename(); };
		splitFileMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).SplitFile(); };
		editorMenuItem.Activated += (o, args) => { (fs as MyFileSystemManager).OpenTextFile(); };

		editMenu.Add(moveMenuItem);
		editMenu.Add(copyMenuItem);
		editMenu.Add(pasteMenuItem);
		editMenu.Add(deleteMenuItem);
		editMenu.Add(new SeparatorMenuItem());
		editMenu.Add(renameMenuItem);
		editMenu.Add(new SeparatorMenuItem());
		editMenu.Add(splitFileMenuItem);
		editMenu.Add(new SeparatorMenuItem());
		editMenu.Add(editorMenuItem);
		// EDIT END

		// ARCHIVE BEGIN
		Menu archiveMenu = new Menu();
		MenuItem archiveMenuItem = new MenuItem("Архів");
		archiveMenuItem.Submenu = archiveMenu;
		MenuItem archMenuItem = new MenuItem("Архівувати");
		MenuItem dearchMenuItem = new MenuItem("Розархівувати");

		archMenuItem.Activated += (sender, e) => (fs as MyFileSystemManager).CreateArchive();
		dearchMenuItem.Activated += (sender, e) => (fs as MyFileSystemManager).ExtractArchive();

		archiveMenu.Add(archMenuItem);
		archiveMenu.Add(dearchMenuItem);
		// ARCHIVE END

		// VIEW BEGIN
		Menu viewMenu = new Menu();
		MenuItem viewMenuItem = new MenuItem("Вид");
		viewMenuItem.Submenu = viewMenu;
		MenuItem addPanelMenuItem = new MenuItem("Додати панель");
		addPanelMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.plus, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
		MenuItem refreshMenuItem = new MenuItem("Оновити");
		refreshMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.F4, Gdk.ModifierType.None, AccelFlags.Visible));
		MenuItem showhiddenMenuItem = new MenuItem("Показати/сховати файли");
		showhiddenMenuItem.AddAccelerator("activate", agr, new AccelKey(
			Gdk.Key.H, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
		/*
		MenuItem unfoldMenuItem = new MenuItem("Розгорнути");
		MenuItem foldMenuItem = new MenuItem("Згорнути");
		*/

		addPanelMenuItem.Activated += (sender, e) => {
			(fs as MyFileSystemManager).AddVolume(TREES_COUNT);
			panelHBox.PackStart(CreateFrame(TREES_COUNT * 2, "Панель " + TREES_COUNT), true, true, 5);
			AddComboBox(TREES_COUNT * 2);
			TREES_COUNT++;
		};
		refreshMenuItem.Activated += (sender, e) => (fs as MyFileSystemManager).Refresh();
		showhiddenMenuItem.Activated += (sender, e) => (fs as MyFileSystemManager).ShowHiddenFiles();

		viewMenu.Add(addPanelMenuItem);
		viewMenu.Add(new SeparatorMenuItem());
		viewMenu.Add(refreshMenuItem);
		viewMenu.Add(showhiddenMenuItem);
		/*
		viewMenu.Add(new SeparatorMenuItem());
		viewMenu.Add(unfoldMenuItem);
		viewMenu.Add(foldMenuItem);
		*/
		// VIEW END

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
		mb.Append(archiveMenuItem);
		mb.Append(viewMenuItem);
		mb.Append(helpMenuItem);

		return mb;
	}

	public void DeletePanel(int index) {
		frames[index].Visible = false;
	}

	[GLib.ConnectBefore]
	protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
	{
		// F2 - rename - 65471
		// F4 - refresh - 65473
		// F5 - copy - 65474
		// F6 - move - 65475
		// F8 - delete - 65477
		// F7 - paste ???????? - 65476
		// f9 - clean HTML - 65478
		// f12 - edit - 65481
		// find
		//Console.WriteLine (evnt.Key.ToString ());
		for (int i = 0; i < TREES_COUNT; i++) {
			if (entries[i].IsFocus && (evnt.Key.ToString().Equals("Return") || evnt.Key.ToString().Equals("KB_Enter"))) {
				foreach (MyTreeView mtv in (fs as MyFileSystemManager).Volumes[i].Values) {
					try {
						mtv.Filter = entries[i].Text.Trim().Equals(string.Empty) ? "*" : entries[i].Text;
						mtv.SetFileTree();
					} catch {
					}
				}
			}
		}
		return base.OnKeyPressEvent(evnt);
	}

	public static Pixbuf GetIcon(string extension, int size)
	{
		return Gtk.IconTheme.Default.LoadIcon(Stock.File, size/*IconSize.Button*/, IconLookupFlags.NoSvg);
	}

	public static Pixbuf GetDirectoryIcon(bool open, int size) 
	{
		return Gtk.IconTheme.Default.LoadIcon(open ? Stock.Open : Stock.Directory, size, IconLookupFlags.NoSvg);
	}

	string GetRandomAnimeTitle()
	{
		return "天元突破グレンラガン";
	}

	public static void Main(string[] args)
	{
		Gtk.Application.Init();
		new MainClass();
		Gtk.Application.Run();
	}
}