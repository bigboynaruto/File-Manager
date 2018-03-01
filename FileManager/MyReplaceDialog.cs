using System;
using Gtk;

public class MyReplaceDialog : Dialog
{
	Entry[] entries;
	// The read-only properties.
	public string TextBefore
	{ get { return entries[0].Text == "" ? null : entries[0].Text; } }

	public string TextAfter
	{ get { return entries[1].Text; } }

	public MyReplaceDialog(string filename)
	{
		this.DestroyWithParent = true;
		// Set up basic look and feel.
		this.Title = "Replace in " + filename;
		this.BorderWidth = 0;
		this.Resizable = false;

		// Now build the overall UI.
		BuildDialogUI();
	}

	void BuildDialogUI()
	{
		entries = new Entry[2];
		
		// Add an HBox to the dialog's VBox.
		HBox hbox = new HBox(false, 8);
		hbox.BorderWidth = 8;
		this.VBox.PackStart(hbox, false, false, 0);

		// Add an Image widget to the HBox using a stock 'info' icon.
		Image stock = new Image(Stock.DialogInfo, IconSize.Dialog);
		hbox.PackStart(stock, false, false, 0);

		// Here we are using a Table to contain the other widgets.
		// Notice that the Table is added to the hBox.
		Table table = new Table(3, 2, false);
		table.RowSpacing = 4;
		table.ColumnSpacing = 4;
		hbox.PackStart(table, true, true, 0);

		entries[0] = new Entry();
		Label label1 = new Label("Знайти: ");
		table.Attach(label1, 0, 1, 0, 1);
		table.Attach(entries[0], 1, 2, 0, 1);
		label1.MnemonicWidget = entries[0];

		entries[1] = new Entry();
		Label label2 = new Label("Замінити на: ");
		table.Attach(label2, 0, 1, 1, 2);
		table.Attach(entries[1], 1, 2, 1, 2);
		label2.MnemonicWidget = entries[1];

		// Add OK and Cancel Buttons.
		Button replace = AddButton(Stock.Ok, ResponseType.Ok) as Button;
		Button cancel = AddButton(Stock.Cancel, ResponseType.Cancel) as Button;
		cancel.Label = "Відмінити";
		replace.Label = "ОК";

		cancel.Clicked += (sender, e) => {
			entries[0].Text = "";
			OnClose();
		};
		entries[0].Activated += (sender, e) => {
			entries[1].GrabFocus();
		};
		entries[1].Activated += (sender, e) => {
			cancel.Click();
		};

		replace.Clicked += (sender, e) => OnClose();

		ShowAll();
	}

	[GLib.ConnectBefore]
	void OnKeyPressEvent(object sender, KeyPressEventArgs args) {
		
	}
}

