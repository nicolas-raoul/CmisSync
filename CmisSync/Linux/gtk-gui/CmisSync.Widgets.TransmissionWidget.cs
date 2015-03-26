
// This file has been generated by the GUI designer. Do not modify.
namespace CmisSync.Widgets
{
	public partial class TransmissionWidget
	{
		private global::Gtk.HBox hbox1;
		private global::Gtk.Image fileTypeImage;
		private global::Gtk.VBox vbox1;
		private global::Gtk.Label fileNameLabel;
		private global::Gtk.ProgressBar transmissionProgressBar;
		private global::Gtk.HBox hbox3;
		private global::Gtk.Label statusDetailsLabel;
		private global::Gtk.Label bandwidthLabel;
		private global::Gtk.Label repoLabel;
		private global::Gtk.Label lastModificationLabel;
		private global::Gtk.Button openFileInFolderButton;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget CmisSync.Widgets.TransmissionWidget
			global::Stetic.BinContainer.Attach (this);
			this.CanFocus = true;
			this.Name = "CmisSync.Widgets.TransmissionWidget";
			// Container child CmisSync.Widgets.TransmissionWidget.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.fileTypeImage = new global::Gtk.Image ();
			this.fileTypeImage.Name = "fileTypeImage";
			this.fileTypeImage.Xalign = 0F;
			this.hbox1.Add (this.fileTypeImage);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.fileTypeImage]));
			w1.Position = 0;
			w1.Expand = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox ();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.fileNameLabel = new global::Gtk.Label ();
			this.fileNameLabel.Name = "fileNameLabel";
			this.fileNameLabel.Xalign = 0F;
			this.fileNameLabel.UseMarkup = true;
			this.fileNameLabel.Selectable = true;
			this.vbox1.Add (this.fileNameLabel);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.fileNameLabel]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.transmissionProgressBar = new global::Gtk.ProgressBar ();
			this.transmissionProgressBar.Name = "transmissionProgressBar";
			this.vbox1.Add (this.transmissionProgressBar);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.transmissionProgressBar]));
			w3.Position = 1;
			w3.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox ();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.statusDetailsLabel = new global::Gtk.Label ();
			this.statusDetailsLabel.Sensitive = false;
			this.statusDetailsLabel.Name = "statusDetailsLabel";
			this.statusDetailsLabel.Xalign = 0F;
			this.statusDetailsLabel.UseMarkup = true;
			this.statusDetailsLabel.Selectable = true;
			this.statusDetailsLabel.SingleLineMode = true;
			this.hbox3.Add (this.statusDetailsLabel);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.hbox3 [this.statusDetailsLabel]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.bandwidthLabel = new global::Gtk.Label ();
			this.bandwidthLabel.Sensitive = false;
			this.bandwidthLabel.Name = "bandwidthLabel";
			this.hbox3.Add (this.bandwidthLabel);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox3 [this.bandwidthLabel]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.repoLabel = new global::Gtk.Label ();
			this.repoLabel.Sensitive = false;
			this.repoLabel.Name = "repoLabel";
			this.repoLabel.Xalign = 0F;
			this.hbox3.Add (this.repoLabel);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox3 [this.repoLabel]));
			w6.Position = 3;
			w6.Expand = false;
			w6.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.lastModificationLabel = new global::Gtk.Label ();
			this.lastModificationLabel.Sensitive = false;
			this.lastModificationLabel.Name = "lastModificationLabel";
			this.lastModificationLabel.Xalign = 0F;
			this.hbox3.Add (this.lastModificationLabel);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.hbox3 [this.lastModificationLabel]));
			w7.PackType = ((global::Gtk.PackType)(1));
			w7.Position = 5;
			w7.Expand = false;
			w7.Fill = false;
			this.vbox1.Add (this.hbox3);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hbox3]));
			w8.Position = 2;
			w8.Expand = false;
			w8.Fill = false;
			this.hbox1.Add (this.vbox1);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.vbox1]));
			w9.Position = 1;
			// Container child hbox1.Gtk.Box+BoxChild
			this.openFileInFolderButton = new global::Gtk.Button ();
			this.openFileInFolderButton.Name = "openFileInFolderButton";
			this.openFileInFolderButton.UseStock = true;
			this.openFileInFolderButton.UseUnderline = true;
			this.openFileInFolderButton.FocusOnClick = false;
			this.openFileInFolderButton.Relief = ((global::Gtk.ReliefStyle)(2));
			this.openFileInFolderButton.Label = "gtk-open";
			this.hbox1.Add (this.openFileInFolderButton);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.openFileInFolderButton]));
			w10.Position = 2;
			w10.Expand = false;
			w10.Fill = false;
			this.Add (this.hbox1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Show ();
		}
	}
}
