using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Gtk;
using System.Linq;
using System.Collections;
using GLib;
using System.Runtime.InteropServices;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Security.Cryptography;

public class MyFileSystemManager : MyFileSystem
{
	bool move;
	// true - move, fasle -copy
	bool movingIsFolder;
	string movingFile = null;

	public MyFileSystemManager()
		: base()
	{
	}

	public void Delete()
	{
		// delete
		try {
			MyTreeView tv = (Trees[currentTree / 2] as MyTreeView);
			FileSystemInfo fsi = null;

			string thereAreFiles = "";
			if (currentTree % 2 == 0 && !tv.SelectedFolder.Equals(string.Empty)) {
				fsi = new DirectoryInfo(tv.SelectedFolder);
				thereAreFiles = (fsi as DirectoryInfo).EnumerateFileSystemInfos().ToList().Count < 1
					? ""
					: " Тека містить файли!";
			} else if (currentTree % 2 == 1 && !tv.SelectedFile.Equals(string.Empty)) {
				fsi = new FileInfo(tv.SelectedFile);
			} else
				throw new FileNotChosenException();

			if (!fsi.Exists)
				throw new FileNotFoundException();

			new MyMessageDialog(MainClass.AppWindow, MessageType.Question, ButtonsType.YesNo, "Ви впевнені, що хочете видалити " + fsi.Name + "?" + thereAreFiles, delegate (object o, ResponseArgs resp) {
				if (resp.ResponseId == ResponseType.Yes) {
					try {
						if (fsi is DirectoryInfo) {
							//Copy all the files & Replaces any files with the same name
							foreach (FileInfo fi in (fsi as DirectoryInfo).GetFiles ("*", SearchOption.AllDirectories)) {
								try {
									fi.Delete();
								} catch {
								}
							}

							foreach (DirectoryInfo di in (fsi as DirectoryInfo).GetDirectories ("*", SearchOption.AllDirectories)) {
								try {
									di.Delete(true);
								} catch {
								}
							}
						}

						fsi.Delete();

						for (int i = 0; i < MainClass.TREES_COUNT; i++) {
							foreach (MyTreeView mytreeview in Volumes[i].Values) {
								try {
									mytreeview.Update(UpdateType.DELETE, fsi.FullName);
								} catch (Exception e) {
									Utils.Log(e.Message);
								}
							}
						}

						Utils.FileLog("OK DELETE " + fsi.FullName);
					} catch (FileNotChosenException e) {
						Utils.FileLog("ERROR " + e.Message);
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						Utils.FileLog("ERROR " + e.Message);
					} catch (ArgumentException e) {
						Utils.FileLog("ERROR " + e.Message);
						new MyErrorMessageDialog(MainClass.AppWindow, new FileNotChosenException().Message);
					} catch (Exception e) {
						Utils.Log(e.Message);
					}
				}
				else 
					Utils.FileLog("CANCEL DELETE " + fsi.FullName);
			});
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotChosenException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
		} catch (ArgumentException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new FileNotChosenException().Message);
		} catch (Exception e) {
			Utils.Log(e.Message);
		}

	}

	public void Refresh()
	{
		for (int i = 0; i < MainClass.TREES_COUNT; i++) {
			foreach (MyTreeView mtv in Volumes[i].Values) {
				try {
					mtv.Update();
				} catch (Exception e) {
					Utils.Log(e.Message);
				}
			}
		}
		Utils.FileLog("OK REFRESH");
	}

	public void Rename()
	{
		// rename
		try {
			FileSystemInfo fsi = null;
			MyTreeView tv = (Trees[currentTree / 2] as MyTreeView);
			if (currentTree % 2 == 0 && tv.SelectedFolder != "") {
				fsi = new DirectoryInfo(tv.SelectedFolder);
			} else if (tv.SelectedFile != "") {
				fsi = new FileInfo(tv.SelectedFile);
			} else
				throw new FileNotChosenException();

			if (!fsi.Exists)
				throw new FileNotFoundException();
			string oldname = fsi.FullName;

			MyInputDialog dialog = new MyInputDialog("Перейменувати " + fsi.Name, "Нове ім’я", fsi.Name);
			dialog.Response += delegate (object o, ResponseArgs resp) {
				if (resp.ResponseId == ResponseType.Ok) {
					try {
						if (!IsValidFileName(dialog.Text))
							throw new InvalidFileNameException();
						else if (FileExists(fsi.FullName.Substring(0, fsi.FullName.LastIndexOf(Path.DirectorySeparatorChar)), dialog.Text))
							throw new FileAlreadyExistsException();

						if (fsi.GetType() == typeof(DirectoryInfo))
							(fsi as DirectoryInfo).MoveTo(Path.Combine(fsi.FullName.Substring(0, fsi.FullName.LastIndexOf(Path.DirectorySeparatorChar)), dialog.Text));
						else
							(fsi as FileInfo).MoveTo(Path.Combine(fsi.FullName.Substring(0, fsi.FullName.LastIndexOf(Path.DirectorySeparatorChar)), dialog.Text));

						for (int i = 0; i < MainClass.TREES_COUNT; i++) {
							foreach (MyTreeView mytreeview in Volumes[i].Values) {
								try {
									mytreeview.Update(UpdateType.RENAME, oldname, dialog.Text);
								} catch (Exception e) {
									Utils.Log(e.Message);
								}
							}
						}

						Utils.FileLog($"OK RENAME {oldname} TO {Path.Combine((fsi as FileInfo).Directory.FullName, dialog.Text)}");
					} catch (FileNotFoundException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
						Utils.FileLog("ERROR " + e.Message);
						dialog.Destroy();
						Rename();
					} catch (InvalidFileNameException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						Utils.FileLog("ERROR " + e.Message);
						dialog.Destroy();
						Rename();
					} catch (FileAlreadyExistsException e) {
						//Console.WriteLine(dialog.Text);
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						Utils.FileLog("ERROR " + e.Message);
						dialog.Destroy();
						Rename();
					} catch (FileNotChosenException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						Utils.FileLog("ERROR " + e.Message);
						dialog.Destroy();
						Rename();
					} catch (AccessViolationException) {
						Utils.FileLog(new MyAccessViolationException().Message);
						new MyErrorMessageDialog(MainClass.AppWindow, new MyAccessViolationException().Message);
					} catch (Exception e) {
						Utils.FileLog("ERROR " + e.Message);
					}
				} else if (resp.ResponseId == ResponseType.Cancel)
					Utils.FileLog($"CANCEL RENAME {fsi.FullName}");
			};
			dialog.Run();
			dialog.Destroy();
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (InvalidFileNameException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileAlreadyExistsException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotChosenException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
		} catch (AccessViolationException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new MyAccessViolationException().Message);
		} catch (Exception e) {
			Utils.FileLog("ERROR " + e.Message);
		}

	}

	public FileSystemInfo Rename(string namefrom, string nameto)
	{
		// rename
		FileSystemInfo fsi = null;
		try {
			string directory = namefrom.Substring(0, namefrom.LastIndexOf(Path.DirectorySeparatorChar));
			if (directory.Equals(string.Empty))
				directory = "/";
			string newname = Path.Combine(directory, nameto);

			if (FileExists(newname))
				throw new FileAlreadyExistsException();

			if (Directory.Exists(namefrom)) {
				fsi = new DirectoryInfo(namefrom);
			} else if (File.Exists(namefrom)) {
				fsi = new FileInfo(namefrom);
			} else
				throw new FileNotFoundException();

			if (!IsValidFileName(nameto))
				throw new InvalidFileNameException();

			if (fsi.GetType() == typeof(DirectoryInfo))
				(fsi as DirectoryInfo).MoveTo(Path.Combine(newname));
			else
				(fsi as FileInfo).MoveTo(newname);

			for (int i = 0; i < MainClass.TREES_COUNT; i++) {
				foreach (MyTreeView mytreeview in Volumes[i].Values) {
					try {
						mytreeview.Update(UpdateType.RENAME, namefrom, nameto);
						//mytreeview.Update(namefrom, nameto);
					} catch (Exception e) {
						Utils.Log(e.Message);
					}
				}
			}
		//	Refresh();

		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
			fsi = null;
		} catch (InvalidFileNameException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Неправильне ім’я файлу!");
			Utils.FileLog("ERROR " + e.Message);
			fsi = null;
		} catch (FileAlreadyExistsException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файл з таким ім’ям вже існує!");
			Utils.FileLog("ERROR " + e.Message);
			fsi = null;
		} catch (AccessViolationException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new MyAccessViolationException().Message);
		} catch (Exception e) {
			Utils.FileLog("ERROR " + e.Message);
			fsi = null;
		}

		return fsi;
	}

	public void Copy()
	{
		// copy
		try {
			if (currentTree % 2 == 0) {
				movingFile = (Trees[currentTree / 2] as MyTreeView).SelectedFolder;
				movingIsFolder = true;
				move = false;
			} else {
				movingFile = (Trees[currentTree / 2] as MyTreeView).SelectedFile;
				movingIsFolder = false;
				move = false;
			}

			//Console.WriteLine(movingFile);
			if (movingFile.Equals(string.Empty))
				throw new FileNotChosenException();

			Utils.FileLog($"OK COPY {movingFile}");
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу aбо теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotChosenException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
		} catch (ArgumentException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new FileNotChosenException().Message);
		} catch (UnauthorizedAccessException) {
			Utils.FileLog("ERROR У вас немає прав на копіювання цього файлу або теки!");
			new MyErrorMessageDialog(MainClass.AppWindow, "У вас немає прав на копіювання цього файлу або теки!");
		} catch (AccessViolationException) {
			Utils.FileLog(new MyAccessViolationException().Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new MyAccessViolationException().Message);
		} catch (Exception e) {
			Utils.Log(e.Message); 
		}
	}

	public void Move()
	{
		// copy
		try {
			if (currentTree % 2 == 0) {
				movingFile = (Trees[currentTree / 2] as MyTreeView).SelectedFolder;
				movingIsFolder = true;
				move = true;
			} else {
				movingFile = (Trees[currentTree / 2] as MyTreeView).SelectedFile;
				movingIsFolder = false;
				move = true;
			}

			if (movingFile.Equals(string.Empty))
				throw new FileNotChosenException();
			
			Utils.FileLog($"OK MOVE {movingFile}");
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotChosenException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (ArgumentException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new FileNotChosenException().Message);
		} catch (UnauthorizedAccessException) {
			Utils.FileLog("ERROR У вас немає прав на копіювання цього файлу або теки!");
			new MyErrorMessageDialog(MainClass.AppWindow, "У вас немає прав на копіювання цього файлу або теки!");
		} catch (AccessViolationException) {
			Utils.FileLog(new MyAccessViolationException().Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new MyAccessViolationException().Message);
		} catch (Exception e) {
			Utils.Log(e.Message); 
		}
	}

	public void Paste()
	{
		try {
			if (movingFile == null)
				throw new FileNotChosenException();

			string newname = Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, Enumerable.Last(movingFile.Split('/')));
			if (movingIsFolder && newname.StartsWith(movingFile))
				throw new IOException("Не можна копіювати/переміщати папку в саму себе ж!");
			if (FileExists(newname))
				throw new FileAlreadyExistsException();
			if (move) {
				if (movingIsFolder)
					System.IO.Directory.Move(movingFile, newname);
				else
					System.IO.File.Move(movingFile, newname);

				for (int i = 0; i < MainClass.TREES_COUNT; i++) {
					foreach (MyTreeView mytreeview in Volumes[i].Values) {
						try {
							mytreeview.Update(UpdateType.DELETE, movingFile);
						} catch (Exception e) {
							Utils.Log(e.Message);
						}
					}
				}
				//Refresh();
				movingFile = null;
				move = false;
			} else {
				if (movingIsFolder) {
					Directory.CreateDirectory(newname);
					//Now Create all of the directories
					foreach (string dirPath in Directory.GetDirectories(movingFile, "*", SearchOption.AllDirectories)) {
						try {
							//Console.WriteLine(dirPath.Replace(movingFile, newname));
							Directory.CreateDirectory(dirPath.Replace(movingFile, newname));
						} catch (Exception e) {
							Utils.Log(e.Message);
						}
					}

					//Copy all the files & Replaces any files with the same name
					foreach (string newPath in Directory.GetFiles(movingFile, "*", SearchOption.AllDirectories)) {
						try {
							File.Copy(newPath, newPath.Replace(movingFile, newname), true);
						} catch (Exception e) {
							Utils.Log(e.Message);
						}
					}
				} else
					System.IO.File.Copy(movingFile, newname, true);
			}

			Utils.FileLog($"OK PASTE {movingFile} TO {newname}");
			Refresh();
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotChosenException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (ArgumentException e) {
			Utils.FileLog("ERROR " + e.Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new FileNotChosenException().Message);
		} catch (UnauthorizedAccessException) {
			Utils.FileLog("ERROR У вас немає прав на копіювання цього файлу або теки!");
			new MyErrorMessageDialog(MainClass.AppWindow, "У вас немає прав на копіювання цього файлу або теки!");
		} catch (AccessViolationException) {
			Utils.FileLog(new MyAccessViolationException().Message);
			new MyErrorMessageDialog(MainClass.AppWindow, new MyAccessViolationException().Message);
		} catch (FileAlreadyExistsException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (NullReferenceException) {
			new MyErrorMessageDialog(MainClass.AppWindow, new FileNotChosenException().Message);
			Utils.FileLog(new FileNotChosenException().Message);
		} catch (IOException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (Exception e) {
			Utils.FileLog("ERROR " + e.Message);
		}
	}

	public void Search()
	{
		/*
		if (Trees [currentTree / 2].SelectedFolder.Equals (TreeIter.Zero))
			new MyErrorMessageDialog (MainClass.AppWindow, "Ви не обрали файл!");
		else {
			Trees [currentTree / 2].Search ();
		}
		*/
	}

	public void NewFile()
	{
		try {
			if ((Trees[currentTree / 2] as MyTreeView).SelectedFolder.Equals(String.Empty)) 
				throw new FileNotChosenException("Ви не обрал теку!");

			MyInputDialog dialog = new MyInputDialog("Новий файл", "Введіть ім’я файлу:", "Untitled Document");
			dialog.Response += (o, args) => {
				if (dialog.ResponseId == ResponseType.Ok) {
					try {
						string path = Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, dialog.Text);
						if (!FileExists(path)) {
							FileStream fs = File.Create(path);
							fs.Close();
							Refresh();
						} else {
							new MyErrorMessageDialog(MainClass.AppWindow, new FileAlreadyExistsException().Message);
							dialog.Destroy();
							NewFile();
							return;
						}
						Utils.FileLog($"OK NEWFILE {path}");
					} catch (InvalidFileNameException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						NewFile();
					} catch (FileAlreadyExistsException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						NewFile();
					} catch (FileNotChosenException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						NewFile();
					} catch (AccessViolationException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						NewFile();
					} catch (Exception e) {
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						NewFile();
					}
				} else 
					Utils.FileLog($"CANCEL NEWFILE");
			};
			dialog.Run();
			dialog.Destroy();
		} catch (FileNotChosenException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} 
		catch (Exception e) {
			Utils.Log(e.Message);
		}
	}

	public void NewFolder()
	{
		try {
			if ((Trees[currentTree / 2] as MyTreeView).SelectedFolder.Equals(String.Empty)) 
				throw new FileNotChosenException("Ви не обрал теку!");
			MyInputDialog dialog = new MyInputDialog("Нова тека", "Введіть ім’я теки:", "Untitled Folder");
			dialog.Response += (o, args) => {
				try {
					string path = Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, dialog.Text.Trim());
					if (args.ResponseId.Equals(ResponseType.Ok)) {
						if (!FileExists(path)) {
							DirectoryInfo di = Directory.CreateDirectory(path);
							if (!di.Exists) 
								throw new FileNotFoundException("Файлу або теки з таким ім’ям більше не існує!");
							Refresh();
						} else
							throw new FileAlreadyExistsException();
						Utils.FileLog($"OK NEWFOLDER {path}");
					} else 
						Utils.FileLog($"CANCEL NEWFOLDER");
				} catch (InvalidFileNameException e) {
					new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
					dialog.Destroy();
					Utils.FileLog("ERROR " + e.Message);
					NewFolder();
				} catch (FileAlreadyExistsException e) {
					new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
					dialog.Destroy();
					Utils.FileLog("ERROR " + e.Message);
					NewFolder();
				} catch (FileNotChosenException e) {
					new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
					dialog.Destroy();
					Utils.FileLog("ERROR " + e.Message);
					NewFolder();
				} catch (AccessViolationException e) {
					new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
					dialog.Destroy();
					Utils.FileLog("ERROR " + e.Message);
					NewFolder();
				} 
			};
			dialog.Run();
			dialog.Destroy();
		} catch (SomethingWentWrongException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotChosenException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (Exception e) {
			//new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		}
	}

	public void ShowDetails()
	{
		try {
			if (currentTree % 2 == 0) {
				new MyDetailsDialog(this, new DirectoryInfo((Trees[currentTree / 2] as MyTreeView).SelectedFolder));
				Utils.FileLog($"OK SHOWDETAILS {(Trees[currentTree / 2] as MyTreeView).SelectedFolder}");
			} else {
				new MyDetailsDialog(this, new FileInfo((Trees[currentTree / 2] as MyTreeView).SelectedFile));
				Utils.FileLog($"OK SHOWDETAILS {(Trees[currentTree / 2] as MyTreeView).SelectedFile}");
			}
		} catch (FileNotChosenException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (ArgumentException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Ви не обрали файлу!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (AccessViolationException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (Exception e) {
			Utils.Log(e.ToString());
		}
	}

	public void PartitionFile() 
	{
		// последний файл пустой и access violation
		try {
			if ((Trees[currentTree / 2] as MyTreeView).SelectedFile.Equals(String.Empty))
				throw new FileNotChosenException();
			Utils.FileLog($"OK PARTITION {(Trees[currentTree / 2] as MyTreeView).SelectedFile}");
		} catch (FileNotChosenException e) {
			Utils.FileLog("ERROR " + e.Message);
		}
	}

	public void FindAllHtmlLinks()
	{
		try {
			if ((Trees[currentTree / 2] as MyTreeView).SelectedFile.Equals(String.Empty) || !IsValidHtmlFile(new FileInfo((Trees[currentTree / 2] as MyTreeView).SelectedFile)))
				throw new InvalidFileNameException();

			HtmlDocument doc = new HtmlWeb().Load((Trees[currentTree / 2] as MyTreeView).SelectedFile);///home/sakura/Desktop/js/index1.html
			ArrayList files = new ArrayList();

			foreach (HtmlNode node in doc.DocumentNode.Descendants("link")) {
				if (File.Exists(Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, node.GetAttributeValue("href", null))))
					files.Add(new FileInfo(Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, node.GetAttributeValue("href", null))));
			}
			foreach (string node in doc.DocumentNode.Descendants("a").Select(a => a.GetAttributeValue("href", null)).Where(u => !String.IsNullOrEmpty(u))) {
				if (File.Exists(Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, node)))
					files.Add(new FileInfo(Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, node)));
			}

			(Trees[currentTree / 2] as MyTreeView).SetFileTree(files);
			Utils.FileLog($"OK FIND_HTML_LINKS {(Trees[currentTree / 2] as MyTreeView).SelectedFile}");
		} catch (FileNotChosenException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
			//...
		} catch (InvalidFileNameException) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Обрано недопустимий html-файл!");
			Utils.FileLog("ERROR Обрано недопустимий html-файл!");
		} catch (Exception e) {
			Utils.FileLog("ERROR " + e.Message);
		}
	}

	public void SplitFile() {
		try {
			if ((Trees[currentTree / 2] as MyTreeView).SelectedFile.Equals(string.Empty))
				throw new FileNotChosenException();
			FileInfo fi = new FileInfo(Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, (Trees[currentTree / 2] as MyTreeView).SelectedFile));
			if (!fi.Exists)
				throw new FileNotFoundException();
			MyInputDialog namedialog = new MyInputDialog("Створіть теку для нових файлів", "Ім’я теки:", "Untitled Folder " + DateTime.Now.ToString(@"MM.dd.yyyy HH:mm"));
			namedialog.Response += (o, args) => {
				if (!args.ResponseId.Equals(ResponseType.Ok))
					return;

				string dirname = Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, namedialog.Text);
				if (FileExists(dirname)) {
					new MyErrorMessageDialog(MainClass.AppWindow, new FileAlreadyExistsException().Message);
					namedialog.Destroy();
					SplitFile();
					return;
				} else if (!IsValidFileName(namedialog.Text)) {
					new MyErrorMessageDialog(MainClass.AppWindow, new InvalidFileNameException().Message);
					namedialog.Destroy();
					SplitFile();
					return;
				}
				Directory.CreateDirectory(dirname);

				SplitFileNumber(dirname);
			};
			namedialog.Run();
			namedialog.Destroy();
		} catch (KeyNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (UnauthorizedAccessException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "У вас немає прав на читання цього файлу!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (ArgumentException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Ви не обрали файл!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotChosenException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Ви не обрали файл!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (AccessViolationException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, new MyAccessViolationException().Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (Exception e) {
			Utils.FileLog("ERROR " + e.Message);
		}
	}

	private void SplitFileNumber(string dirname) {
		MyInputDialog numdialog = new MyInputDialog("Введіть число файлів (>1 та <11)", "Файлів:", 2.ToString());
		numdialog.Response += (_o, _args) => {
			if (!_args.ResponseId.Equals(ResponseType.Ok))
				return;

			int filec = 0;
			int.TryParse(numdialog.Text, out filec);
			if (filec < 2 || filec > 10) {
				new MyErrorMessageDialog(MainClass.AppWindow, "Введіть число від 2 до 10!");
				numdialog.Destroy();
				SplitFileNumber(dirname);
				return;
			}
			string[] lines = File.ReadAllLines((Trees[currentTree / 2] as MyTreeView).SelectedFile);
			int linesPerFile = lines.Length / filec;

			filec = filec > lines.Length ? lines.Length : filec;
			StreamWriter writer;
			string file = Path.Combine(dirname, (Trees[currentTree / 2] as MyTreeView).SelectedFile);
			for (int i = 1; i <= filec; i++) {
				FileStream fs = File.Create(Path.Combine(dirname, Path.GetFileNameWithoutExtension(file) + "_" + i + Path.GetExtension(file)));
				fs.Close();
				writer = new System.IO.StreamWriter(Path.Combine(dirname, Path.GetFileNameWithoutExtension(file) + "_" + i + Path.GetExtension(file)));
				for (int j = (i - 1) * linesPerFile; j < lines.Length && j < i * linesPerFile; j++) {
					writer.WriteLine(lines[j]);
				}
				writer.Close();
			}

			writer = new System.IO.StreamWriter(Path.Combine(dirname, Path.GetFileNameWithoutExtension(file) + "_" + filec + Path.GetExtension(file)), true);
			for (int i = filec * linesPerFile; i < lines.Length; i++) {
				writer.WriteLine(lines[i]);
			}
			writer.Close();

			Refresh();
		};
		numdialog.Run();
		numdialog.Destroy();
	}

	public void CreateArchive() {
		try {
			string name;
			if (currentTree % 2 == 0) {
				if ((Trees[currentTree / 2] as MyTreeView).SelectedFolder.Equals(String.Empty)) 
					throw new FileNotChosenException();
				name = (Trees[currentTree / 2] as MyTreeView).SelectedFolder;
			}
			else {
				if ((Trees[currentTree / 2] as MyTreeView).SelectedFile.Equals(String.Empty)) 
					throw new FileNotChosenException();
				name = (Trees[currentTree / 2] as MyTreeView).SelectedFile;
			}
			if (!FileExists(name))
				throw new FileNotFoundException();

			MyInputDialog dialog = new MyInputDialog("Створити архів", "Введіть ім’я архіву:", Path.GetFileNameWithoutExtension(name));
			dialog.Response += (o, args) => {
				if (dialog.ResponseId == ResponseType.Ok) {
					try {
						string path = Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, dialog.Text + ".zip");
						if (!FileExists(path)) {
							if (currentTree % 2 == 0) {
								//Console.WriteLine(Path.Combine(Path.GetDirectoryName(name), dialog.Text + ".zip"));
								ZipFile.CreateFromDirectory(name, Path.Combine(new DirectoryInfo((Trees[currentTree / 2] as MyTreeView).SelectedFolder).Parent.FullName, dialog.Text + ".zip"), CompressionLevel.Fastest, true);
								Console.WriteLine(name + " " + Path.Combine(new DirectoryInfo((Trees[currentTree / 2] as MyTreeView).SelectedFolder).Parent.FullName, dialog.Text + ".zip"));
							} else {
								using (FileStream fs = new FileStream(path,FileMode.Create))
								using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
								{
									arch.CreateEntryFromFile(name, new FileInfo(name).Name, CompressionLevel.Fastest);
								}
							}
							Refresh();
						} else {
							new MyErrorMessageDialog(MainClass.AppWindow, new FileAlreadyExistsException().Message);
							Utils.FileLog($"ERROR File {path} already exists.");
							dialog.Destroy();
							CreateArchive();
							return;
						}
						Utils.FileLog($"OK CREATE_ARCHIVE {path}");
					} catch (InvalidFileNameException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						CreateArchive();
					} catch (FileAlreadyExistsException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						CreateArchive();
					} catch (FileNotChosenException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
					} catch (AccessViolationException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
					} catch (FileNotFoundException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, "Обраного вами архіву не існує!");
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
					} catch (Exception e) {
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
					}
				} else
					Utils.FileLog($"CANCEL CREATE_ARCHIVE");
			};
			dialog.Run();
			dialog.Destroy();
		} catch (FileNotChosenException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Файлу або теки з таким ім’ям більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (Exception e) {
			Utils.Log(e.Message);
		}
	}

	public void ExtractArchive() {
		try {
			if (currentTree % 2 == 0) {
				if ((Trees[currentTree / 2] as MyTreeView).SelectedFolder.Equals(String.Empty)) 
					throw new FileNotChosenException("Ви не обрали теку!");
				//(Trees[currentTree / 2] as MyTreeView).SelectedFolder;
			}
			else {
				if ((Trees[currentTree / 2] as MyTreeView).SelectedFile.Equals(String.Empty)) 
					throw new FileNotChosenException("Ви не обрали .zip файл!");
				if (Path.GetExtension((Trees[currentTree / 2] as MyTreeView).SelectedFile) != ".zip")
					throw new InvalidFileNameException("Обрано файл неправильного розширення!");
			}

			if (!FileExists((Trees[currentTree / 2] as MyTreeView).SelectedFolder))
				throw new FileNotFoundException("Обраної вами директорії більше не існує!");
			else if (!FileExists((Trees[currentTree / 2] as MyTreeView).SelectedFile))
				throw new FileNotFoundException("Обраного вами архіву не існує!");

			MyInputDialog dialog = new MyInputDialog("Видобути архів", "Введіть ім’я теки:", Path.GetFileNameWithoutExtension((Trees[currentTree / 2] as MyTreeView).SelectedFile));
			dialog.Response += (o, args) => {
				if (dialog.ResponseId == ResponseType.Ok) {
					try {
						string path = Path.Combine((Trees[currentTree / 2] as MyTreeView).SelectedFolder, dialog.Text);
						if (!FileExists(path)) {
							ZipFile.ExtractToDirectory((Trees[currentTree / 2] as MyTreeView).SelectedFile, path);
							Refresh();
						} else {
							new MyErrorMessageDialog(MainClass.AppWindow, new FileAlreadyExistsException().Message);
							dialog.Destroy();
							CreateArchive();
							return;
						}
						Utils.FileLog($"OK EXTRACT_ARCHIVE {(Trees[currentTree / 2] as MyTreeView).SelectedFile} TO {path}");
					} catch (InvalidFileNameException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						ExtractArchive();
					} catch (FileAlreadyExistsException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
						ExtractArchive();
					} catch (FileNotChosenException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
					} catch (AccessViolationException e) {
						new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
					} catch (FileNotFoundException) {
						new MyErrorMessageDialog(MainClass.AppWindow, "Обраного вами архіву не існує!");
						dialog.Destroy();
						Utils.FileLog("ERROR " + "Обраного вами архіву не існує!");
					} catch (Exception e) {
						dialog.Destroy();
						Utils.FileLog("ERROR " + e.Message);
					}
				} else 
					Utils.FileLog($"CANCEL EXTRACT_ARCHIVE");
			};
			dialog.Run();
			dialog.Destroy();
		} catch (FileNotChosenException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (InvalidFileNameException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (FileNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (Exception e) {
			Utils.Log(e.Message);
		}
	}

	public void ShowHiddenFiles() {
		(Trees[currentTree / 2] as MyTreeView).ShowHiddenFiles = !(Trees[currentTree / 2] as MyTreeView).ShowHiddenFiles;
		Refresh();
	}

	public void OpenTextFile()
	{
		try {
			if ((Trees[currentTree / 2] as MyTreeView).SelectedFile.Equals(string.Empty))
				throw new FileNotChosenException();
			new MyTextEditor((Trees[currentTree / 2] as MyTreeView).SelectedFile);
			Refresh();
			Utils.FileLog($"OK OPEN_TEXT_FILE {(Trees[currentTree / 2] as MyTreeView).SelectedFile}");
		} catch (FileNotChosenException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, e.Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (KeyNotFoundException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "Ви не обрали файл або його більше не існує!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (UnauthorizedAccessException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, "У вас немає прав на відкриття цього файлу!");
			Utils.FileLog("ERROR " + e.Message);
		} catch (ArgumentException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, new FileNotChosenException().Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (AccessViolationException e) {
			new MyErrorMessageDialog(MainClass.AppWindow, new MyAccessViolationException().Message);
			Utils.FileLog("ERROR " + e.Message);
		} catch (Exception e) {
			Utils.FileLog("ERROR " + e.Message);
		}
	}
}

