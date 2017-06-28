using System;
using System.Windows;
using System.Windows.Controls;
using DataBuildSync.Models;

namespace DataBuildSync.Views.Settings {
    public partial class SettingsView : Window {
        public delegate void SettingChanged();

        public static AddRepView AddRepView;

        public SettingsView() {
            InitializeComponent();
            HandleConfig();
        }

        public static event SettingChanged SettingChangedEvent;

        private void HandleConfig() {
            ParallelTransfersCheckBox.IsChecked = MainWindow.Config.ParallelTransfer;

            switch (MainWindow.Config.LoggingLevel) {
                case "Verbose":
                    VerboseRadio.IsChecked = true;
                    break;
                case "Standard":
                    StandardRadio.IsChecked = true;
                    break;
                case "None":
                    NoneRadio.IsChecked = true;
                    break;
            }
        }

        private void AddRepClick(object sender, RoutedEventArgs e) {
            if (AddRepView == null) {
                AddRepView = new AddRepView();
                AddRepView.Show();
            }
            else {
                AddRepView.Activate();
            }
        }

        private void ToggleSettings(object sender, RoutedEventArgs e) {
            try {
                var checkBox = (CheckBox) sender;

                switch (checkBox.Name) {
                    case "ParallelTransfersCheckBox":
                        MainWindow.Config.ParallelTransfer = !MainWindow.Config.ParallelTransfer;
                        break;
                }
                XmlHandler.UpdateConfig(MainWindow.Config);
                SettingChangedEvent?.Invoke();
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }

        private void ToggleLoggingSettings(object sender, RoutedEventArgs e) {
            try {
                var radio = (RadioButton) sender;

                switch (radio.Name) {
                    case "VerboseRadio":
                        MainWindow.Config.LoggingLevel = MainWindow.Config.LoggingLevel = "Verbose";
                        break;
                    case "StandardRadio":
                        MainWindow.Config.LoggingLevel = MainWindow.Config.LoggingLevel = "Standard";
                        break;
                    case "NoneRadio":
                        MainWindow.Config.LoggingLevel = MainWindow.Config.LoggingLevel = "None";
                        break;
                }
                XmlHandler.UpdateConfig(MainWindow.Config);
                SettingChangedEvent?.Invoke();
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }
    }
}