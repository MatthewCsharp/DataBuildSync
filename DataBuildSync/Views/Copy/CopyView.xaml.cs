using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using DataBuildSync.Models;

namespace DataBuildSync.Views.Copy {
    public partial class CopyView : Window {
        private readonly List<ProjectLink> _projectsToCopy;
        private int _cloudPhotosCopiedCount;
        private int _cloudPhotosCount;
        private double _currentProjectPoint;

        private string _currentSupervisorFolderName;

        private List<string> _lines;
        private string _mostRecentLogFile;
        private int _projectFileCopiedCount;
        private int _projectFileCount;

        public CopyView(List<ProjectLink> projectsToCopy) {
            InitializeComponent();
            _projectsToCopy = projectsToCopy;
        }

        private bool CopyBackMode { get; set; }

        private void ExecuteCopyBtnClick(object sender, RoutedEventArgs e) {
            Task.Run(() => {
                try {
                    _lines = new List<string>();
                    var overallProgressPoint = 100 / (double) _projectsToCopy.Count;
                    Dispatcher.BeginInvoke(new Action(() => { OverallProgressBar.Value = 0; }));

                    foreach (var project in _projectsToCopy) {
                        NullFields();

                        // Check that the project is valid
                        _supervisorFolder = ValidateFolder(Path.Combine(project.ProjectPath, "Plans", "Supervisor File"));
                        if (_supervisorFolder != null) {
                            Dispatcher.BeginInvoke(new Action(() => { CurrentFolderNameTxt.Text = project.ProjectCode; }));

                            // Re-zero
                            _projectFileCount = 0;
                            _projectFileCopiedCount = 0;
                            _lines.Add($"============ Start of Project {project.ProjectCode} ============");

                            // Locate project folders
                            _projectCode = project.ProjectCode;
                            _parentFolder = ValidateFolder(project.ProjectPath);
                            _finalVarFolder = ValidateFolder(Path.Combine(project.ProjectPath, "Variations - Final Account"));
                            _photosFolder = ValidateFolder(Path.Combine(project.ProjectPath, "Photos"));
                            _supervisorJobFolder = ReturnSupervisorJobFolder(_supervisorFolder);

                            if (_supervisorJobFolder != null) {
                                _purchaseOrdersFolder = ValidateFolder(Path.Combine(_supervisorJobFolder, "5 Purchase Orders"));
                                _signedVarFolder = ValidateFolder(Path.Combine(_supervisorJobFolder, "6 Signed Variations"));
                            }

                            // Locate cloud folders
                            _cloudFolder = MainWindow.Config.DefaultDestinationFolder;

                            if (_currentSupervisorFolderName != null) {
                                Directory.CreateDirectory(Path.Combine(_cloudFolder, $"Jobs Folder {MainWindow.SelectedRep.Initial}", _currentSupervisorFolderName, "7 Photos"));
                                _cloudSupervisorFolder = ValidateFolder(Path.Combine(_cloudFolder, $"Jobs Folder {MainWindow.SelectedRep.Initial}"));
                                _cloudJobFolder = ValidateFolder(Path.Combine(_cloudFolder, $"Jobs Folder {MainWindow.SelectedRep.Initial}", _currentSupervisorFolderName));
                                _cloudJobPhotosFolder = ValidateFolder(Path.Combine(_cloudFolder, $"Jobs Folder {MainWindow.SelectedRep.Initial}", _currentSupervisorFolderName, "7 Photos"));
                            }

                            // Write logs
                            WhiteFolderLogs();

                            // Current progress bar
                            var dir = new DirectoryInfo(project.ProjectPath);
                            var dirs = dir.GetDirectories();
                            _currentProjectPoint = 100 / (double) (dirs.Length != 0 ? dirs.Length : 1);
                            Dispatcher.BeginInvoke(new Action(() => { CurrentProjectProgressBar.Value = 0; }));

                            // Start copy of selected folders
                            CopyJobFoldersToCloud();

                            // Start copy of cloud photos back to source
                            CopyPhotosFolderBack();

                            if (_projectFileCopiedCount > _projectFileCount) {
                                _projectFileCopiedCount = _projectFileCount;
                            }

                            if (_cloudPhotosCopiedCount > _cloudPhotosCount) {
                                _cloudPhotosCopiedCount = _cloudPhotosCount;
                            }

                            // Completion lines
                            _lines.Add($"----------------------------- Overview -----------------------------");
                            _lines.Add($"Total files: {_projectFileCount}. Total copied: {_projectFileCopiedCount}. Total Failed: {_projectFileCount - _projectFileCopiedCount}");
                            if (_cloudPhotosCount != 0) {
                                _lines.Add($"Total photos to sync: {_cloudPhotosCount}. Total copied: {_cloudPhotosCopiedCount}. Total Failed: {_cloudPhotosCount - _cloudPhotosCopiedCount}");
                            }
                            _lines.Add($"====================================================================");
                            _lines.Add($" ");
                            _lines.Add($" ");

                            // Add to overall value
                            Dispatcher.BeginInvoke(new Action(() => { OverallProgressBar.Value += overallProgressPoint; }));
                        }
                        else {
                            MessageBox.Show($"{project.ProjectName} is not a valid project folder.");
                        }
                    }

                    if (MainWindow.Config.LoggingLevel != "None") {
                        try {
                            Directory.CreateDirectory("./Logs");
                            var date = DateTime.Now;
                            var currentExtention = 1;
                            var logFileName = $"{date.Day} {date.Month} {date.Year} - {currentExtention}";

                            while (File.Exists($"./Logs/{logFileName}.txt")) {
                                currentExtention++;
                                var hyphenIndex = logFileName.IndexOf('-') + 1;

                                logFileName = logFileName.Substring(0, hyphenIndex);
                                logFileName += " " + currentExtention;
                            }
                            File.WriteAllLines($"./Logs/{logFileName}.txt", _lines);
                            _mostRecentLogFile = logFileName;
                            Dispatcher.BeginInvoke(new Action(() => { ShowLogBtn.Visibility = Visibility.Visible; }));
                        }
                        catch (Exception exception) {
                            MessageBox.Show(exception.Message);
                        }
                    }
                }
                catch (Exception exception) {
                    MessageBox.Show(exception.Message);
                }
            });
            Dispatcher.BeginInvoke(new Action(() => { CurrentFolderNameTxt.Text = ""; }));
        }

        private string ValidateFolder(string path) {
            if (Directory.Exists(path)) {
                return path;
            }
            return null;
        }

        private string ReturnSupervisorJobFolder(string supervisorFolder) {
            _currentSupervisorFolderName = null;
            if (Directory.Exists(supervisorFolder)) {
                var directories = Directory.GetDirectories(supervisorFolder);
                if (directories.Length != 0) {
                    _currentSupervisorFolderName = Path.GetFileName(directories[0]);
                    return directories[0];
                }
            }
            return null;
        }

        private void WhiteFolderLogs() {
            _lines.Add($"Project Code: {_projectCode}");
            _lines.Add($"Parent: {_parentFolder}");
            _lines.Add($"Final Var: {_finalVarFolder}");
            _lines.Add($"Photos: {_photosFolder}");
            _lines.Add($"Supervisor: {_supervisorFolder}");
            _lines.Add($"Supervisor Job: {_supervisorJobFolder}");
            _lines.Add($"Purchase Orders: {_purchaseOrdersFolder}");
            _lines.Add($"Signed Var: {_signedVarFolder}");
            _lines.Add($"Cloud: {_cloudFolder}");
            _lines.Add($"Cloud Supervisor: {_cloudSupervisorFolder}");
            _lines.Add($"Cloud Supervisor Job: {_cloudJobFolder}");
            _lines.Add($"Cloud Photos: {_cloudJobPhotosFolder}");
        }

        private void NullFields() {
            _projectCode = null;
            _parentFolder = null;
            _finalVarFolder = null;
            _photosFolder = null;
            _supervisorFolder = null;
            _supervisorJobFolder = null;
            _purchaseOrdersFolder = null;
            _signedVarFolder = null;
            _cloudFolder = null;
            _cloudSupervisorFolder = null;
            _cloudJobFolder = null;
            _cloudJobPhotosFolder = null;
        }

        private void CopyJobFoldersToCloud() {
            if (MainWindow.Config.LoggingLevel == "Verbose") {
                _lines.Add($"----------------------------- Begin File Transfer -----------------------------");
            }

            //CopyDirectory(_finalVarFolder, Path.Combine(_cloudJobFolder, "Variations - Final Account"));
            CopyDirectory(_photosFolder, Path.Combine(_cloudJobFolder, "7 Photos"));
            CopyDirectory(Directory.GetParent(_purchaseOrdersFolder).FullName, _cloudJobFolder);
            CopyDirectory(Directory.GetParent(_signedVarFolder).FullName, _cloudJobFolder);
        }

        private void CopyPhotosFolderBack() {
            if (MainWindow.Config.LoggingLevel == "Verbose") {
                _lines.Add($"------------------------------ Begin Photos Sync -------------------------------");
            }

            _cloudPhotosCopiedCount = 0;
            _cloudPhotosCount = 0;

            if (Directory.Exists(_cloudJobPhotosFolder)) {
                CopyBackMode = true;
                CopyDirectory(_cloudJobPhotosFolder, _photosFolder);
                CopyBackMode = false;
            }
        }

        private void CopyDirectory(string sourceDirName, string destDirName) {
            try {
                // Get the subdirectories for the specified directory.
                var dir = new DirectoryInfo(sourceDirName);

                var dirs = dir.GetDirectories();

                // If the destination directory doesn't exist, create it.
                if (!Directory.Exists(destDirName)) {
                    Directory.CreateDirectory(destDirName);
                }

                // Get the files in the directory and copy them to the new location.
                var files = dir.GetFiles();
                // Async copy destroyed logging. //TODO
                foreach (var file in files) {
                    CopyFile(destDirName, file);
                }
                //if (MainWindow.Config.ParallelTransfer) {
                //    Parallel.ForEach(files, file => { CopyFile(destDirName, file); });
                //}
                //else {
                //    foreach (var file in files) {
                //        CopyFile(destDirName, file);
                //    }
                //}

                // Copy sub directories
                foreach (var subdir in dirs) {
                    try {
                        var temppath = Path.Combine(destDirName, subdir.Name);
                        CopyDirectory(subdir.FullName, temppath);
                        Dispatcher.BeginInvoke(new Action(() => { CurrentProjectProgressBar.Value += _currentProjectPoint; }));
                    }
                    catch (UnauthorizedAccessException) {
                        _lines.Add($"Error: {subdir} folder is denied.");
                    }
                    catch (Exception x) {
                        _lines.Add($"Error: {subdir} failed to copy. Message: {x.Message}.");
                    }
                }
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }

        private void CopyFile(string destDirName, FileInfo file) {
            if (CopyBackMode) {
                _cloudPhotosCount++;
            }
            else {
                _projectFileCount++;
            }

            try {
                var tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);

                if (CopyBackMode) {
                    _cloudPhotosCopiedCount++;
                    if (MainWindow.Config.LoggingLevel == "Verbose") {
                        _lines.Add($"Success: {file} - Copied back to source");
                    }
                }
                else {
                    _projectFileCopiedCount++;
                    if (MainWindow.Config.LoggingLevel == "Verbose") {
                        _lines.Add($"Success: {file}");
                    }
                }
            }
            catch (UnauthorizedAccessException) {
                _lines.Add($"Error: {file} was denied.");
            }
            catch (IOException x) {
                if (x.Message.Contains("already exists")) {
                    _lines.Add($"Error: {file} already exists.");
                }
                else {
                    _lines.Add($"Error: {file} is being used by another process.");
                }
            }
            catch (Exception x) {
                _lines.Add($"Error: {file} failed to copy. Message: {x.Message}.");
            }
        }

        private void ShowLogBtnClick(object sender, RoutedEventArgs e) {
            try {
                var location = Path.Combine(Directory.GetCurrentDirectory(), $"Logs/{_mostRecentLogFile}.txt");
                Process.Start(location);
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }

        #region FolderVariables

        private string _projectCode;
        private string _parentFolder;
        private string _finalVarFolder;
        private string _photosFolder;
        private string _supervisorFolder;
        private string _supervisorJobFolder;
        private string _purchaseOrdersFolder;
        private string _signedVarFolder;
        private string _cloudFolder;
        private string _cloudSupervisorFolder;
        private string _cloudJobFolder;
        private string _cloudJobPhotosFolder;

        #endregion
    }
}