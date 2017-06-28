using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataBuildSync.Models;
using DataBuildSync.Views.Copy;
using DataBuildSync.Views.Settings;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DataBuildSync.Views {
    /// <summary>
    ///     Ultimately this should have been built using SQLite or something, but at the time of development
    ///     VS 2017 did not support EF code-first migrations so I just built a little xmlHandler to store the
    ///     data. Shouldn't take too long to port if whoever is reading this decides to in the future.
    /// </summary>
    public partial class MainWindow : Window {
        public static Rep SelectedRep;
        private ObservableCollection<ProjectLink> _projectLinks;

        public MainWindow() {
            InitializeComponent();
            HandleConfiguration();
        }

        #region DefaultFolders

        private void ChangeFolders(object sender, RoutedEventArgs e) {
            var btnName = ((Button) sender).Name;
            var dialog = new CommonOpenFileDialog {IsFolderPicker = true, AddToMostRecentlyUsedList = false, InitialDirectory = btnName == "ChangeProjectBtn" ? ProjectLocationTxt.Text : DestinationLocationTxt.Text};

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                if (btnName == "ChangeProjectBtn") {
                    Config.DefaultProjectFolder = dialog.FileName;
                    ProjectLocationTxt.Text = dialog.FileName;
                }
                else {
                    Config.DefaultDestinationFolder = dialog.FileName;
                    DestinationLocationTxt.Text = dialog.FileName;
                }

                XmlHandler.UpdateConfig(Config);
            }
        }

        #endregion

        #region Copy

        private void CopyBtnClick(object sender, RoutedEventArgs e) {
            var projectsToCopy = _projectLinks.Where(pl => pl.Backup).ToList();
            if (projectsToCopy.Count != 0) {
                var copyView = new CopyView(projectsToCopy);

                copyView.ShowDialog();
            }
            else {
                MessageBox.Show("No projects have been selected.");
            }
        }

        #endregion

        private void OpenSettingsFile(object sender, RoutedEventArgs e) {
            try {
                Process.Start("Settings.xml");
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }

        private void OpenLogFolder(object sender, RoutedEventArgs e) {
            Directory.CreateDirectory("./Logs");
            try {
                Process.Start(new ProcessStartInfo {FileName = $"{AppDomain.CurrentDomain.BaseDirectory}/Logs", UseShellExecute = true, Verb = "open"});
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }

        #region Configuration

        public static Configuration Config;

        private void HandleConfiguration() {
            XmlHandler.CreateDatabase();
            Config = XmlHandler.GetConfig();

            ProjectLocationTxt.Text = Config.DefaultProjectFolder;
            DestinationLocationTxt.Text = Config.DefaultDestinationFolder;
            UpdateMainViewSettings();

            SettingsView.SettingChangedEvent += UpdateMainViewSettings;
        }

        private void UpdateMainViewSettings() {
            ParallelTransfersTxt.Text = $"Parallel Tranfers: {Config.ParallelTransfer}";
            LoggingTxt.Text = $"Logging Level: {Config.LoggingLevel}";
        }

        #endregion

        #region Projects

        private void OpenProjectListBtn(object sender, RoutedEventArgs e) {
            try {
                var menu = new ContextMenu();

                var reps = XmlHandler.GetReps();
                if (reps.Count == 0) {
                    var item = new MenuItem {Header = "No reps", IsEnabled = false};
                    menu.Items.Add(item);
                }
                else {
                    foreach (var rep in reps) {
                        var item = new MenuItem {Header = rep.Initial};
                        item.Click += OpenRepProjectsClick;

                        menu.Items.Add(item);
                    }
                }

                menu.IsOpen = true;
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }

        private void OpenRepProjectsClick(object sender, RoutedEventArgs routedEventArgs) {
            try {
                var initial = (string) ((MenuItem) sender).Header;
                SelectedRepTxt.Text = $"Selected Rep: {initial}";
                SelectedRepTxt.Visibility = Visibility.Visible;

                SelectedRep = XmlHandler.GetRep(initial);

                _projectLinks = new ObservableCollection<ProjectLink>(XmlHandler.GetRepProjects(initial));
                AddProjectBtn.IsEnabled = true;
                CopyBtn.IsEnabled = _projectLinks.Count() != 0;
                ProjectListItemControl.ItemsSource = _projectLinks;
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        private void AddProjectBtnClick(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog {IsFolderPicker = true, AddToMostRecentlyUsedList = false, InitialDirectory = ProjectLocationTxt.Text, Multiselect = true};

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                var folders = dialog.FileNames;
                var failedList = "";
                foreach (var folder in folders) {
                    // Get the last 5 digits of parent folder
                    var projectCode = Path.GetFileName(folder)?.Substring(Path.GetFileName(folder).Length - 5);
                    var rgx = new Regex(@"^[a-zA-Z0-9]\d{4}$");
                    // Check the string matches convention
                    if (projectCode != null && rgx.IsMatch(projectCode)) {
                        var projectName = Path.GetFileName(folder);
                        var projectPath = folder;
                        var projectLinks = XmlHandler.GetRepProjects(SelectedRep.Initial);

                        // Attempt to name the project by the proper project name if the folder is available
                        var supervisorFolder = Path.Combine(folder, "Plans", "Supervisor File");
                        if (Directory.Exists(supervisorFolder)) {
                            var dirs = Directory.GetDirectories(supervisorFolder);
                            if (dirs.Length == 1) {
                                projectName = Path.GetFileName(dirs[0]);
                            }
                        }

                        if (projectLinks.Any(pl => pl.ProjectCode == projectCode)) {
                            failedList += projectName + "\n";
                        }
                        else {
                            var newProjectLink = new ProjectLink {Backup = true, RepInitials = SelectedRep.Initial, ProjectName = projectName, ProjectPath = projectPath, ProjectCode = projectCode};
                            XmlHandler.CreateProjectLink(newProjectLink);
                            _projectLinks.Add(newProjectLink);
                            CopyBtn.IsEnabled = _projectLinks.Count() != 0;
                        }
                    }
                }

                if (failedList != "") {
                    MessageBox.Show($"The following projects were not added.\n \n {failedList} \n Reason: Duplicates");
                }
            }
        }

        private void ChangeBackupCheckBox(object sender, RoutedEventArgs e) {
            var checkBox = (CheckBox) sender;

            var projectLinks = XmlHandler.GetRepProjects(SelectedRep.Initial);

            var projectLink = projectLinks.SingleOrDefault(pl => pl.ProjectName == checkBox.Uid && pl.RepInitials == SelectedRep.Initial);
            if (projectLink != null) {
                projectLink.Backup = !projectLink.Backup;
                XmlHandler.UpdateProjectLink(projectLink);
            }
        }

        private void RemoveProjectBtnClick(object sender, RoutedEventArgs e) {
            try {
                var itemsToRemove = new List<ProjectLink>();
                foreach (ProjectLink i in ProjectListItemControl.SelectedItems) {
                    itemsToRemove.Add(i);
                }
                if (itemsToRemove.Count() != 0) {
                    var message = "Are you sure you wish to remove:\n\n";

                    foreach (var selectedItem in itemsToRemove) {
                        message += selectedItem.ProjectName + "\n";
                    }

                    message += "\n";

                    var result = MessageBox.Show(message, "Remove Projects", MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes) {
                        foreach (var selectedItem in itemsToRemove) {
                            try {
                                var item = _projectLinks.SingleOrDefault(pl => pl.ProjectName == selectedItem.ProjectName);
                                if (item != null) {
                                    item.RepInitials = SelectedRep.Initial;
                                    XmlHandler.RemoveProjectLink(item);
                                    _projectLinks.Remove(item);
                                }
                            }
                            catch (Exception exception) {
                                MessageBox.Show(exception.Message);
                            }
                        }
                        CopyBtn.IsEnabled = _projectLinks.Count() != 0;
                    }
                }
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }

        #endregion

        #region Window Functions

        private void DragTopBar(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                DragMove();
            }
        }

        private void Quit(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void Maximize(object sender, RoutedEventArgs e) {
            if (WindowState == WindowState.Maximized) {
                WindowState = WindowState.Normal;
                MainGrid.Margin = new Thickness(0);
                TopMenu.Padding = new Thickness(0);
            }
            else {
                WindowState = WindowState.Maximized;
                MainGrid.Margin = new Thickness(5, 0, 5, 5);
                TopMenu.Padding = new Thickness(5);
            }
        }

        private SettingsView _settingsView;

        private void Settings(object sender, RoutedEventArgs e) {
            _settingsView = new SettingsView();

            _settingsView.ShowDialog();
        }

        #endregion
    }
}