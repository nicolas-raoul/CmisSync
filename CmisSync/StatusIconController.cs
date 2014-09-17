//-----------------------------------------------------------------------
// <copyright file="StatusIconController.cs" company="GRAU DATA AG">
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


using System;
using System.IO;
using System.Timers;

using Threading = System.Threading;

using System.Globalization;

using System.Diagnostics;
using CmisSync.Lib.Config;

namespace CmisSync {

    /// <summary>
    /// State of the CmisSync status icon.
    /// </summary>
    public enum IconState {
        Idle,
        SyncingUp,
        SyncingDown,
        Syncing,
        Error
    }


    /// <summary>
    /// MVC controller for the CmisSync status icon.
    /// </summary>
    public class StatusIconController {

        // Event handlers.

        public event UpdateIconEventHandler UpdateIconEvent = delegate { };
        public delegate void UpdateIconEventHandler (int icon_frame);

        public event UpdateMenuEventHandler UpdateMenuEvent = delegate { };
        public delegate void UpdateMenuEventHandler (IconState state);

        public event UpdateStatusItemEventHandler UpdateStatusItemEvent = delegate { };
        public delegate void UpdateStatusItemEventHandler (string state_text);

        public event UpdateSuspendSyncFolderEventHandler UpdateSuspendSyncFolderEvent = delegate { };
        public delegate void UpdateSuspendSyncFolderEventHandler(string reponame);

        public event UpdateTransmissionMenuEventHandler UpdateTransmissionMenuEvent = delegate { };
        public delegate void UpdateTransmissionMenuEventHandler();

        /// <summary>
        /// Current state of the CmisSync tray icon.
        /// </summary>
        public IconState CurrentState = IconState.Idle;


        /// <summary>
        /// Warn some anormal cases
        /// </summary>
        public bool Warning
        {
            get
            {
                return warning;
            }
            set
            {
                warning = value;
                if (CurrentState == IconState.Idle)
                {
                    UpdateIconEvent(0);
                }
            }
        }
        private bool warning;


        /// <summary>
        /// Short text shown at the top of the menu of the CmisSync tray icon.
        /// </summary>
        public string StateText = String.Format(Properties_Resources.Welcome, Properties_Resources.ApplicationName);


        /// <summary>
        /// Maximum number of remote folders in the menu before the overflow menu appears.
        /// </summary>
        public readonly int MenuOverflowThreshold   = 9;


        /// <summary>
        /// Minimum number of remote folders to populate the overflow menu.
        /// </summary>
        public readonly int MinSubmenuOverflowCount = 3;


        /// <summary>
        /// The list of remote folders to show in the CmisSync tray menu.
        /// </summary>
        public string [] Folders {
            get {
                int overflow_count = (Program.Controller.Folders.Count - MenuOverflowThreshold);

                if (overflow_count >= MinSubmenuOverflowCount)
                    return Program.Controller.Folders.GetRange (0, MenuOverflowThreshold).ToArray ();
                else
                    return Program.Controller.Folders.ToArray ();
            }
        }


        /// <summary>
        /// The list of remote folders to show in the CmisSync tray's overflow menu.
        /// </summary>
        public string[] OverflowFolders
        {
            get {
                int overflow_count = (Program.Controller.Folders.Count - MenuOverflowThreshold);

                if (overflow_count >= MinSubmenuOverflowCount)
                    return Program.Controller.Folders.GetRange (MenuOverflowThreshold, overflow_count).ToArray ();
                else
                    return new string [0];
            }
        }

        /// <summary>
        /// Timer for the animation that appears when downloading/uploading a file.
        /// </summary>
        private Timer animation;


        /// <summary>
        /// Current frame of the animation being shown.
        /// First frame is the still icon.
        /// </summary>
        private int animation_frame_number;


        /// <summary>
        /// Constructor.
        /// </summary>
        public StatusIconController ()
        {
            InitAnimation ();

            // A remote folder has been added.
            Program.Controller.FolderListChanged += delegate {
                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;

                    if (Program.Controller.Folders.Count == 0)
                        StateText = String.Format(Properties_Resources.Welcome, Properties_Resources.ApplicationName);
                    else
                        StateText = Properties_Resources.FilesUpToDate;
                }

                UpdateStatusItemEvent (StateText);
                UpdateMenuEvent (CurrentState);
            };

            // No more download/upload.
            Program.Controller.OnIdle += delegate {
                if (CurrentState != IconState.Error) {
                    CurrentState = IconState.Idle;

                    if (Program.Controller.Folders.Count == 0)
                        StateText = String.Format(Properties_Resources.Welcome, Properties_Resources.ApplicationName);
                    else
                        StateText = Properties_Resources.FilesUpToDate;
                }

                UpdateStatusItemEvent (StateText);

                this.animation.Stop ();

                UpdateIconEvent (0);
//                UpdateMenuEvent (CurrentState);
            };

            Program.Controller.OnTransmissionListChanged += delegate {
                UpdateTransmissionMenuEvent();
            };

            // Syncing.
            Program.Controller.OnSyncing += delegate {
                if (CurrentState != IconState.Syncing)
                {
                    CurrentState = IconState.Syncing;
                    StateText = Properties_Resources.SyncingChanges;
                    UpdateStatusItemEvent(StateText);

                    this.animation.Start();
                }
            };
        }


        /// <summary>
        /// With the local file explorer, open the folder where the local synchronized folders are.
        /// </summary>
        public void LocalFolderClicked (string reponame)
        {
            Program.Controller.OpenCmisSyncFolder (reponame);
        }

        /*
        /// <summary>
        /// With the default web browser, open the remote folder of a CmisSync synchronized folder.
        /// </summary>
        public void RemoteFolderClicked(string reponame)
        {
            Program.Controller.OpenRemoteFolder(reponame);
        }*/


        /// <summary>
        /// Open the remote folder addition wizard.
        /// </summary>
        public void AddRemoteFolderClicked ()
        {
            Program.Controller.ShowSetupWindow (PageType.Add1);
        }


        /// <summary>
        /// Show the Setting dialog.
        /// </summary>
        public void SettingClicked()
        {
            Program.Controller.ShowSettingWindow();
        }


        /// <summary>
        /// Open the CmisSync log with a text file viewer.
        /// </summary>
        public void LogClicked()
        {
            Program.Controller.ShowLog(ConfigManager.CurrentConfig.GetLogFilePath());
        }


        /// <summary>
        /// Show the About dialog.
        /// </summary>
        public void AboutClicked()
        {
            Program.Controller.ShowAboutWindow();
        }


        /// <summary>
        /// Quit CmisSync.
        /// </summary>
        public void QuitClicked()
        {
                Program.Controller.Quit ();
        }


        /// <summary>
        /// Suspend synchronization for a particular folder.
        /// </summary>
        public void SuspendSyncClicked(string reponame)
        {
            Program.Controller.StartOrSuspendRepository(reponame);
            UpdateSuspendSyncFolderEvent(reponame);
        }
        /// <summary>
        /// Tries to remove a given repo from sync
        /// </summary>
        /// <param name="reponame"></param>
        public void RemoveFolderFromSyncClicked(string reponame)
        {
            Program.Controller.RemoveRepositoryFromSync(reponame);
        }

        /// <summary>
        /// Edit a particular folder.
        /// </summary>
        public void EditFolderClicked(string reponame)
        {
            Program.Controller.EditRepositoryFolder(reponame);
        }

        /// <summary>
        /// Start the tray icon animation.
        /// </summary>
        private void InitAnimation ()
        {
            this.animation_frame_number = 0;

            this.animation = new Timer () {
                Interval = 100
            };

            this.animation.Elapsed += delegate {
                if (this.animation_frame_number < 4)
                    this.animation_frame_number++;
                else
                    this.animation_frame_number = 0;

                UpdateIconEvent (this.animation_frame_number);
            };
        }
    }
}
