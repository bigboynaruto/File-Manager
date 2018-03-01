using System;
using Gtk;

public class MyInputDialog : Dialog
{
	// The Entry widgets for the first / last name.
	Entry entry;
	string prompt;
	string defaultText;
	Button ok, cancel;

	public ResponseType ResponseId { get; private set; }
	public string Text { get { return entry.Text; } }

	public MyInputDialog(String title, string prompt, string defaultText = "")
	{
		// Set up basic look and feel.
		this.Title = title;
		this.BorderWidth = 0;
		this.Resizable = false;
		this.prompt = prompt;
		this.defaultText = defaultText;

		BuildDialogUI();
	}


	void BuildDialogUI()
	{
		// Add an HBox to the dialog's VBox.
		HBox hbox = new HBox(false, 8);
		hbox.BorderWidth = 8;
		this.VBox.PackStart(hbox, false, false, 0);

		// Add an Image widget to the HBox using a stock 'info' icon.
		Image stock = new Image(Stock.DialogInfo, IconSize.Dialog);
		hbox.PackStart(stock, false, false, 0);

		// Here we are using a Table to contain the other widgets.
		// Notice that the Table is added to the hBox.
		Table table = new Table(2, 2, false);
		table.RowSpacing = 4;
		table.ColumnSpacing = 4;
		hbox.PackStart(table, true, true, 0);

		entry = new Entry(defaultText);
		entry.KeyPressEvent +=  (o, args) => {
			if (args.Event.Key.ToString().Equals("Return") || args.Event.Key.ToString().Equals("KB_Enter")) {
				ok.Click();
			}
		};
		Label label = new Label(prompt);
		table.Attach(label, 0, 1, 0, 1);
		table.Attach(entry, 1, 2, 0, 1);
		label.MnemonicWidget = entry;

		// Add OK and Cancel Buttons.
		ok = AddButton(Stock.Ok, ResponseType.Ok) as Button;
		cancel = AddButton(Stock.Cancel, ResponseType.Cancel) as Button;
		cancel.Label = "Відмінити";
		ok.Label = "ОК";

		cancel.Clicked += (sender, e) => {
			ResponseId = ResponseType.Cancel;
			entry.Text = "";
			OnClose();
		};

		ok.Clicked += (sender, e) => {
			if (entry.Text != string.Empty) {
				ResponseId = ResponseType.Ok;
				Close -= close;
				OnClose();
			} 
		};

		Close += close;

		ShowAll();
		entry.Text = defaultText;
	}

	void close(object o, EventArgs e)
	{
		ResponseId = ResponseType.None;
		entry.Text = string.Empty;
	}
}