using System;
using System.IO;
using Gtk;
using System.Collections.Generic;
using Gdk;
using Mono.Unix.Native;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections;
using System.Runtime.CompilerServices;
using Glade;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using Mono.Unix;
using System.Collections.ObjectModel;

public enum UpdateType {
	REFRESH, DELETE, RENAME
};

public interface IObserver
{
	void Update(UpdateType ut, string s1 = "", string s2 = "");
}

public class MyTreeView : IObserver
{
	const int IDENTATION = 10;
	public const int ICON_SIZE = 40;


	Gdk.Pixbuf folderIcon, folderOpenIcon;

	TreeIter selectedFolder, selectedFile;
	Dictionary<TreeIter, TreeElem> paths;

	public DirectoryInfo Root { get; private set; }
	public string Filter { get; set; }
	public String SelectedFolder { get { return selectedFolder.Equals (TreeIter.Zero) ? "" : paths [selectedFolder].FullName; } }
	public String SelectedFile { get { return selectedFile.Equals (TreeIter.Zero) ? "" : paths [selectedFile].FullName; } }
	public bool ShowHiddenFiles { get; set; }
	public TreeView FolderTree { get; private set; }
	public TreeView FileTree { get; private set; }

	public MyTreeView (string rootDir) : base()
	{
		Filter = "*";
		Root = new DirectoryInfo (rootDir);
		
		folderIcon = MainClass.GetDirectoryIcon (false, ICON_SIZE);//new Gdk.Pixbuf ("folder_close1.png");
		if (folderIcon == null)
			throw new Exception ("Closed folder icon not found!");
		
		folderOpenIcon = MainClass.GetDirectoryIcon (true, ICON_SIZE);//new Gdk.Pixbuf ("folder_open1.png");
		folderIcon = folderIcon.ScaleSimple (ICON_SIZE / 2, ICON_SIZE / 2, Gdk.InterpType.Bilinear);
		folderOpenIcon = folderOpenIcon.ScaleSimple (ICON_SIZE / 2, ICON_SIZE / 2, Gdk.InterpType.Bilinear);
		if (folderOpenIcon == null)
			throw new Exception ("Open folder icon not found!");
		
		paths = new Dictionary<TreeIter, TreeElem>();
		selectedFolder = TreeIter.Zero;
		
		if (!Root.Exists)
			throw new Exception ("Folder " + Root + " not found!");
		
		SetUpTree ();
	}

	private void ClearFileTree() 
	{
		FileTree.Columns[0].Title = "";
		TreeIter iter;
		(FileTree.Model as TreeStore).GetIterFirst (out iter);
		while (paths.ContainsKey (iter)) {
			Unselect (iter);
			paths.Remove (iter);
			TreeIter iter2 = iter;
			(FileTree.Model as TreeStore).IterNext (ref iter2);
			if ((FileTree.Model as TreeStore).IterIsValid(iter))
				(FileTree.Model as TreeStore).Remove (ref iter);
			iter = iter2;
		}
	}

	public void SetFileTree()
	{
		ClearFileTree();

		DirectoryInfo di;
		try {
			di = new DirectoryInfo (paths [selectedFolder].FullName);
			if (!di.Exists)
				throw new Exception("Folder " + paths [selectedFolder].FullName + " doesn't exist!");
		} 
		catch (Exception e) {
			Utils.Log (e.Message);
			return;
		}

		AppendFiles (di);
	}

	public void SetFileTree(ICollection files)
	{
		ClearFileTree();

		try
		{
			foreach (FileInfo file in files)
			{
				if (file.Name.StartsWith (".") && !ShowHiddenFiles) continue;
				//Console.WriteLine ("APPEND " + file.FullName);
				TreeIter iter = (FileTree.Model as TreeStore).AppendValues (MainClass.GetIcon (file.Extension, ICON_SIZE), file.Name);
				paths[iter] = new FileTreeElem (iter, file.FullName);
			}
		}
		catch (Exception e) {
			Utils.Log (e.Message); 
		}
	}

	void SetUpTree() 
	{
		FileTree = new TreeView ();
		FolderTree = new TreeView ();

		FolderTree.ShowExpanders = false;
		FolderTree.LevelIndentation = IDENTATION;

		TreeViewColumn column = new TreeViewColumn();
		column.Title = @Root.FullName;
		CellRendererPixbuf iconRenderer = new CellRendererPixbuf(); 
		column.PackStart(iconRenderer, false); 
		column.AddAttribute(iconRenderer, "pixbuf", 0); 
		CellRendererText nameRenderer = new CellRendererText(); 
		//iconRenderer.SetFixedSize (ICON_SIZE, ICON_SIZE);
		nameRenderer.SizePoints = 8;
		column.PackStart(nameRenderer, true);
		column.AddAttribute(nameRenderer, "text", 1);

		column.SetCellDataFunc (nameRenderer, new TreeCellDataFunc((col, cell, model, it) => {
			TreeIter selected;
			if (FolderTree.IsFocus && FolderTree.Selection.GetSelected (out selected))
				(cell as Gtk.CellRendererText).Foreground = "black";
			else if (!FolderTree.IsFocus)
				(cell as Gtk.CellRendererText).Foreground = "darkgray";
		}));

		TreeStore folderstore = new TreeStore(typeof(Gdk.Pixbuf), typeof(string));

		TreeIter iter = folderstore.AppendValues (folderIcon, Root.Name);
		paths[iter] = new DirectoryTreeElem (iter, Root.FullName);

		//AppendFolders (Root, iter);

		FolderTree.AppendColumn(column);
		FolderTree.Model = folderstore;

		FileTree = new TreeView ();

		FileTree.ShowExpanders = false;
		FileTree.LevelIndentation = IDENTATION;

		TreeViewColumn columnFile = new TreeViewColumn(); 
		CellRendererPixbuf iconRendererFile = new CellRendererPixbuf(); 
		columnFile.PackStart(iconRendererFile, false); 
		columnFile.AddAttribute(iconRendererFile, "pixbuf", 0); 
		CellRendererText nameRendererFile = new CellRendererText();
		//iconRendererFile.SetFixedSize (ICON_SIZE, ICON_SIZE);
		//nameRendererFile.SizePoints = 8;
		columnFile.PackStart(nameRendererFile, true); 
		columnFile.AddAttribute(nameRendererFile, "text", 1); 

		columnFile.SetCellDataFunc (nameRendererFile, new TreeCellDataFunc((col, cell, model, it) => {
			TreeIter selected;
			if (FileTree.IsFocus && FileTree.Selection.GetSelected (out selected))
				(cell as Gtk.CellRendererText).Foreground = "black";
			else if (!FileTree.IsFocus)
				(cell as Gtk.CellRendererText).Foreground = "darkgray";
		}));

		TreeStore filestore = new TreeStore(typeof(Gdk.Pixbuf), typeof(string));

		FileTree.AppendColumn(columnFile);
		FileTree.Model = filestore;

		FileTree.ButtonPressEvent += OnFileTreeButtonPressEvent;
		FileTree.CursorChanged += (sender, e) => {
			TreeIter selected;
			if (FileTree.Selection.GetSelected(out selected))
			{
				selectedFile = selected;
				FileInfo fi = new FileInfo(paths[selectedFile].FullName);
				//MainClass.Status.Text = fi.Name;

				columnFile.Title = @fi.Name;
			}
		};

		TreeIter prevSelectedFolder = TreeIter.Zero;
		FolderTree.KeyPressEvent += OnFolderTreeKeyPressEvent;
		FolderTree.Selection.Changed += (sender, e) => {
			TreeIter selected;
			if (FolderTree.Selection.GetSelected(out selected))
			{
				prevSelectedFolder = selectedFolder;
				selectedFolder = selected;
				SetFileTree();
			}
		};
		FolderTree.CursorChanged += (sender, e) => {
			if (folderstore.IterIsValid(selectedFolder))
			{
                //selectedFolder = selected;
                //SetFileTree ();
                DirectoryTreeElem elem = paths[selectedFolder] as DirectoryTreeElem;
				/*
				if (!elem.IsVisited)
				{
					AppendFolders (new DirectoryInfo(elem.FullName), selectedFolder);
					elem.IsVisited = true;
				}
				*/

				column.Title = @MyFileSystem.ToShortPath (paths[selectedFolder].FullName);
				// = shortDirPath;//[selectedFolder].FullName;

				TreePath tp = (FolderTree.Model as TreeStore).GetPath (selectedFolder);//new TreePath(elem.TreePath);
				if (prevSelectedFolder.Equals(selectedFolder)) {
					if (!elem.IsOpen) {
						AppendFolders (new DirectoryInfo(elem.FullName), selectedFolder);
						FolderTree.ExpandRow(tp, false);
						if ((FolderTree.Model as TreeStore).IterIsValid (selectedFolder))
							FolderTree.Model.SetValue (selectedFolder, 0, folderOpenIcon);
						elem.IsOpen = true;
					} else {
						FolderTree.CollapseRow(tp);
						if ((FolderTree.Model as TreeStore).IterIsValid (selectedFolder))
							FolderTree.Model.SetValue (selectedFolder, 0, folderIcon);
						RemoveChildIters (selectedFolder, (FolderTree.Model as TreeStore));
						elem.IsOpen = false;
					}
				}
				prevSelectedFolder = selectedFolder;
			}
		};
	}

	[GLib.ConnectBefore]
	void OnFileTreeButtonPressEvent(object sender, ButtonPressEventArgs e)
	{
		if (selectedFile.Equals (TreeIter.Zero))
			return;
		if (e.Event.Type == Gdk.EventType.TwoButtonPress)
		{
			Process myProcess = new Process();    
			myProcess.StartInfo.FileName = paths[selectedFile].FullName;
			myProcess.Start();
		}
	}

	[GLib.ConnectBefore]
	void OnFolderTreeKeyPressEvent(object sender, KeyPressEventArgs args) {
		if (args.Event.Key.ToString().Equals("Return") || args.Event.Key.ToString().Equals("KB_Enter")) {
			if ((FolderTree.Model as TreeStore).IterIsValid(selectedFolder)) {
				DirectoryTreeElem elem = paths[selectedFolder] as DirectoryTreeElem;

				TreePath tp = (FolderTree.Model as TreeStore).GetPath(selectedFolder);//new TreePath(elem.TreePath);
				if (!elem.IsOpen) {
					AppendFolders(new DirectoryInfo(elem.FullName), selectedFolder);
					FolderTree.ExpandRow(tp, false);
					if ((FolderTree.Model as TreeStore).IterIsValid(selectedFolder))
						FolderTree.Model.SetValue(selectedFolder, 0, folderOpenIcon);
					elem.IsOpen = true;
				} else {
					FolderTree.CollapseRow(tp);
					if ((FolderTree.Model as TreeStore).IterIsValid(selectedFolder))
						FolderTree.Model.SetValue(selectedFolder, 0, folderIcon);
					RemoveChildIters(selectedFolder, (FolderTree.Model as TreeStore));
					elem.IsOpen = false;
				}
			}
		}
	}

	void AppendFiles(DirectoryInfo di, TreeIter i = default(TreeIter), SearchOption opt = SearchOption.TopDirectoryOnly) 
	{
		try
		{
			foreach (FileInfo file in di.GetFiles(Filter, opt))
			{
				if (file.Name.StartsWith (".") && !ShowHiddenFiles) continue;
				//Console.WriteLine ("APPEND " + file.FullName);
				TreeIter iter = i.Equals (TreeIter.Zero) ? (FileTree.Model as TreeStore).AppendValues (MainClass.GetIcon (file.Extension, ICON_SIZE), file.Name) 
					: (FileTree.Model as TreeStore).AppendValues (i, MainClass.GetIcon (file.Extension, ICON_SIZE), file.Name);
				paths[iter] = new FileTreeElem (iter, file.FullName);
			}
		}
		catch (Exception e) {
			Utils.Log (e.Message); 
		}
	}

	void AppendFolders(DirectoryInfo di, TreeIter i = default(TreeIter), SearchOption opt = SearchOption.TopDirectoryOnly) 
	{
		try
		{
			foreach (DirectoryInfo d in di.GetDirectories("*", opt))
			{
				try 
				{
					if (d.Name.StartsWith (".") && !ShowHiddenFiles) continue;
					d.EnumerateFiles();
					TreeIter iter = i.Equals (TreeIter.Zero) ? (FolderTree.Model as TreeStore).AppendValues (folderIcon, d.Name) 
						: (FolderTree.Model as TreeStore).AppendValues (i, folderIcon, d.Name);
					paths[iter] = new DirectoryTreeElem (iter, d.FullName);
				}
				catch (Exception e) {
					Utils.Log (e.Message); 
				}
			}
		}
		catch (Exception e) {
			Utils.Log (e.Message); 
			// DELETE IT
		}
	}

	private void Delete(string name)
	{
		TreeIter iter = TreeIter.Zero;
		try
		{
			foreach (TreeIter i in paths.Keys) {
				if (paths [i].FullName.Equals (name)) {
					iter = i;
					Unselect (iter);
					RemoveChildIters (iter, FolderTree.Model as TreeStore);
					if (paths [i] is DirectoryTreeElem && (FolderTree.Model as TreeStore).IterIsValid (iter)) 
						(FolderTree.Model as TreeStore).Remove (ref iter);
					else if ((FileTree.Model as TreeStore).IterIsValid (iter)) 
						(FileTree.Model as TreeStore).Remove (ref iter);
					//paths.Remove (iter);
					return;
				}
			}

		}
		catch (Exception e) {
			Utils.Log (e.Message);
		}
	}

	private void Rename(string oldfullname, string newname)
	{
		string newfullname = Path.Combine(oldfullname.Substring (0, oldfullname.LastIndexOf (Path.DirectorySeparatorChar.ToString())), newname);
		try
		{
			foreach (TreeIter iter in paths.Keys) {
				if (!paths[iter].FullName.StartsWith(oldfullname + Path.DirectorySeparatorChar) && paths[iter].FullName != oldfullname) 
					continue;
				//Unselect(iter);
				if (paths[iter].FullName != oldfullname) {
					paths[iter].FullName = Path.Combine(newfullname, paths[iter].FullName.Substring(oldfullname.Length + 1));
				} else 
					paths[iter].FullName = newfullname;

				if ((FolderTree.Model as TreeStore).IterIsValid(iter)) {
					(FolderTree.Model as TreeStore).SetValue(iter, 1, paths[iter].Name);
				} else if ((FileTree.Model as TreeStore).IterIsValid(iter)) {
					(FileTree.Model as TreeStore).SetValue(iter, 1, paths[iter].Name);
					(FileTree.Model as TreeStore).SetValue(iter, 0, MainClass.GetIcon(new FileInfo(newfullname).Extension, ICON_SIZE));
				}
			}

			SetFileTree();
		}
		catch (Exception e) {
			Utils.Log (e.Message);
		}
	}

	private void Refresh()
	{
		Queue<TreeIter> iterRemove = new Queue<TreeIter> ();
		Queue<TreeElem> iterAdd = new Queue<TreeElem> (); 
		foreach (TreeIter iter in new ArrayList (paths.Keys)) {
			try {
				if (!paths.ContainsKey(iter))
					continue;
				if (paths[iter].Name.StartsWith(".")) {
					if (!IsValidIter(iter) && ShowHiddenFiles) {
						iterAdd.Enqueue(paths[iter]);
					} else if (IsValidIter(iter) && !ShowHiddenFiles) {
						iterRemove.Enqueue(iter);
					}
				} else if (paths [iter] is DirectoryTreeElem) {
					if (!new DirectoryInfo (paths [iter].FullName).Exists) {
						iterRemove.Enqueue (iter);
					} 
					else if ((paths [iter] as DirectoryTreeElem).IsOpen) {
						if (!(FolderTree.Model as TreeStore).IterIsValid (iter)) continue;
						//if (!paths[iter].IsOpen) continue;
						foreach (DirectoryInfo di in new DirectoryInfo (paths[iter].FullName).GetDirectories ()) {
							try {
								//if (di.Name.StartsWith (".")) Console.WriteLine(di.Name);
								if (di.Name.StartsWith (".") && !ShowHiddenFiles) continue;
								di.EnumerateFiles();
								TreeIter iter_;
								(FolderTree.Model as TreeStore).IterChildren (out iter_, iter);
								for (int i = 0; i < (FolderTree.Model as TreeStore).IterNChildren (iter); i++) {
									if (!paths.ContainsKey(iter_))
										continue;
									if (di.FullName.Equals (paths[iter_].FullName))
										goto breakloop;
									(FolderTree.Model as TreeStore).IterNext(ref iter_);   
								}
								iter_ = (FolderTree.Model as TreeStore).AppendValues (iter, folderIcon, di.Name);
								iterAdd.Enqueue (new DirectoryTreeElem (iter_, di.FullName));
								//paths.Add (iter_, new TreeElem (iter_, di.FullName, (FolderTree.Model as TreeStore).GetPath (iter_).ToString (), true));
								FolderTree.ExpandRow (FolderTree.Model.GetPath (iter), false);
								breakloop:;
							} 
							catch (Exception e) {
								Utils.Log (e.Message); 
							}
						}
					}
				}
				else {
					if (!new FileInfo (paths [iter].FullName).Exists) {
						iterRemove.Enqueue (iter);
					}
				}
			}
			catch (Exception e) {
				Utils.Log (e.Message);
			}
		}

		while (iterRemove.Count > 0) {
			TreeIter i = iterRemove.Dequeue ();
			Unselect (i);
			//paths.Remove (i);/////////////////////////////////////////////////////////////////////////////////////////////////////////////////TUT MOZHET I NE VILETAT
			RemoveChildIters (i, FolderTree.Model as TreeStore);
			if ((FileTree.Model as TreeStore).IterIsValid(i))
				(FileTree.Model as TreeStore).Remove (ref i);
			else if ((FolderTree.Model as TreeStore).IterIsValid(i))
				(FolderTree.Model as TreeStore).Remove (ref i);
		}

		while (iterAdd.Count > 0) {
			TreeElem el = iterAdd.Dequeue ();
			paths[el.Iter] = el;
		}

		SetFileTree ();
	}

	bool IsValidIter(TreeIter i) {
		return (FileTree.Model as TreeStore).IterIsValid(i) || (FolderTree.Model as TreeStore).IterIsValid(i);
	}

	public void Update(UpdateType ut = UpdateType.REFRESH, string s1 = "", string s2 = "") {
		switch (ut) {
			case UpdateType.REFRESH:
				Refresh();
				break;
			case UpdateType.DELETE:
				Delete(s1);
				break;
			case UpdateType.RENAME:
				Rename(s1, s2);
				break;
			default:
				break;
		}
	}

	public void Search() {
		/*
		DirectoryInfo di;
		try {
			di = new DirectoryInfo (paths [selectedFolder].FullName);
		} 
		catch (Exception e) {
			Utils.Log (e.Message);
			return;
		}

		TreeIter iter;
		(FileTree.Model as TreeStore).GetIterFirst (out iter);
		while (paths.ContainsKey (iter)) {
			paths.Remove (iter);
			TreeIter iter2 = iter;
			(FileTree.Model as TreeStore).IterNext (ref iter2);
			(FileTree.Model as TreeStore).Remove (ref iter);
			iter = iter2;
		}

		//foreach (DirectoryInfo
		AppendAllFiles (di);
		*/
	}

	void AppendAllFiles(DirectoryInfo di) {
		/*
		List<FileInfo> files = ShowHiddenFiles 
			? di.EnumerateFiles ().ToList () 
			: di.EnumerateFiles ().Where(x => !x.Name.StartsWith(".")).ToList ();
		List<DirectoryInfo> dirs = ShowHiddenFiles 
			? di.EnumerateDirectories ().ToList () 
			: di.EnumerateDirectories ().Where(x => !x.Name.StartsWith(".")).ToList ();
		*/
		foreach (FileInfo file in di.GetFiles ()) {
			if (file.Name.StartsWith (".") && !ShowHiddenFiles) continue;
			TreeIter iter = (FileTree.Model as TreeStore).AppendValues (MainClass.GetIcon (file.Extension, ICON_SIZE), file.Name);
			paths[iter] = new FileTreeElem (iter, file.FullName);
		}
		foreach (DirectoryInfo dir in di.GetDirectories ()) {
			if (dir.Name.StartsWith (".") && !ShowHiddenFiles) continue;
			AppendAllFiles (dir);
		}
	}

	void RemoveChildIters(TreeIter iter, TreeStore treestore) 
	{
		
		if (!treestore.IterIsValid (iter) || !treestore.IterHasChild (iter))
			return;
		Queue<TreeIter> iterRemove = new Queue<TreeIter> ();

		TreeIter iter1;
		treestore.IterNthChild (out iter1, iter, 0);
		for (int j = 0; j < treestore.IterNChildren (iter); j++) {
			RemoveChildIters (iter1, treestore);
			iterRemove.Enqueue (iter1);
			treestore.IterNext (ref iter1);
		}


		TreeIter i;
		while (iterRemove.Count > 0) {
			i = iterRemove.Dequeue ();
			Unselect (i);
			//Console.Write (paths [i].FullName);
			paths.Remove (i); ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////TUT MOZHET I NE VILETAT
			if (treestore.IterIsValid (i)) {
				treestore.Remove (ref i);
			}
		}

	}
		
	void Unselect (TreeIter iter) {
		if (selectedFolder.Equals (iter)) {
			FolderTree.Selection.UnselectIter (iter);
			selectedFolder = TreeIter.Zero;
			ClearFileTree();
		} else if (selectedFile.Equals (iter)) {
			FileTree.Selection.UnselectIter (iter);
			selectedFile = TreeIter.Zero;
		}
	}
}