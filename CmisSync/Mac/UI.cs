//-----------------------------------------------------------------------
// <copyright file="UI.cs" company="GRAU DATA AG">
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see http://www.gnu.org/licenses/.
//
// </copyright>
//-----------------------------------------------------------------------
//   CmisSync, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

namespace CmisSync {
    using System;
    using System.Drawing;
    using System.IO;

    using MonoMac.AppKit;
    using MonoMac.Foundation;
    using MonoMac.ObjCRuntime;

    public class UI : AppDelegate {
        public StatusIcon StatusIcon;
        public SetupWizardController Setup;
        public About About;
        public GeneralSettingsController Settings;
        public TransmissionWidgetController Transmission;

        public static NSFont Font = NSFontManager.SharedFontManager.FontWithFamily(
            "Lucida Grande",
            NSFontTraitMask.Condensed,
            0,
            13);
        public static NSFont BoldFont = NSFontManager.SharedFontManager.FontWithFamily(
            "Lucida Grande",
            NSFontTraitMask.Bold,
            0,
            13);

        public UI() {
            using (var a = new NSAutoreleasePool()) {
                NSApplication.SharedApplication.ApplicationIconImage = NSImage.ImageNamed("cmissync-app.icns");

                this.SetFolderIcon();

                this.Setup = new SetupWizardController();
                this.About = new About();
                this.StatusIcon = new StatusIcon();
                this.Settings = new GeneralSettingsController();
                this.Transmission = new TransmissionWidgetController();
                this.Transmission.LoadWindow();
                this.Transmission.Window.IsVisible = false;

                Program.Controller.UIHasLoaded();
            }
        }

        public void SetFolderIcon() {
            using (var a = new NSAutoreleasePool()) {
                NSImage folder_icon = NSImage.ImageNamed("cmissync-folder.icns");
                NSWorkspace.SharedWorkspace.SetIconforFile(folder_icon, Program.Controller.FoldersPath, 0);
            }
        }

        public void Run() {
            NSApplication.Main(new string[0]);
        }

        public void UpdateDockIconVisibility() {
            if (this.Setup.IsWindowLoaded && this.Setup.Window.IsVisible) {
                this.ShowDockIcon();
            } else if (this.About.IsVisible) {
                this.ShowDockIcon();
            } else if (Program.Controller.IsEditWindowVisible) {
                this.ShowDockIcon();
            } else if (this.Settings.IsWindowLoaded && this.Settings.Window.IsVisible) {
                this.ShowDockIcon();
            } else if (this.Transmission.IsWindowLoaded && this.Transmission.Window.IsVisible) {
                this.ShowDockIcon();
            } else {
                this.HideDockIcon();
            }
        }

        private void HideDockIcon() {
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Prohibited;
        }

        private void ShowDockIcon() {
            NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Regular;
        }
    }

    public partial class AppDelegate : NSApplicationDelegate {
        public override void WillBecomeActive(NSNotification notification) {
            if (NSApplication.SharedApplication.DockTile.BadgeLabel != null) {
                // Program.Controller.ShowEventLogWindow();
                NSApplication.SharedApplication.DockTile.BadgeLabel = null;
            }
        }

        public override void WillTerminate(NSNotification notification) {
            Program.Controller.Quit();
        }
    }
}