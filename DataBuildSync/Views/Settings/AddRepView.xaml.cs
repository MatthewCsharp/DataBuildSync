using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataBuildSync.Models;

namespace DataBuildSync.Views.Settings {
    public partial class AddRepView : Window {
        private Rep _selectedRep;

        public AddRepView() {
            InitializeComponent();

            GatherReps();

            Closing += (sender, args) => { SettingsView.AddRepView = null; };
        }

        private ObservableCollection<Rep> Reps { get; set; }

        private void GatherReps() {
            Reps = new ObservableCollection<Rep>(XmlHandler.GetReps());
            foreach (var rep in Reps) {
                RepListBox.Items.Add(rep.Initial);
            }
        }

        private void InitialsTxtBoxChanged(object sender, TextChangedEventArgs e) {
            SuccessTxt.Visibility = Visibility.Collapsed;
            var txtBox = (TextBox) sender;

            if (txtBox.Text.Length == 2) {
                if (Reps.Any(r => r.Initial.ToLower() == txtBox.Text.ToLower())) {
                    AddRepBtn.IsEnabled = false;
                    ErrorAlert(true, "Duplicate Initials");
                }
                else {
                    AddRepBtn.IsEnabled = true;
                    ErrorAlert(false);
                }
            }
            else {
                AddRepBtn.IsEnabled = false;
            }
        }

        private void ErrorAlert(bool show, string error = "") {
            if (show) {
                ErrorTxt.Text = error;
                ErrorTxt.Visibility = Visibility.Visible;
            }
            else {
                ErrorTxt.Text = "";
                ErrorTxt.Visibility = Visibility.Collapsed;
            }
        }

        private void AddRepClick(object sender, RoutedEventArgs e) {
            try {
                var newRep = new Rep {
                    Initial = InitialsTxtBox.Text.ToUpper()
                };
                XmlHandler.CreateRep(newRep);
                InitialsTxtBox.Text = "";
                RepListBox.Items.Add(newRep.Initial);
                SuccessTxt.Visibility = Visibility.Visible;
                Reps.Add(newRep);
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message);
            }
        }

        private void RemoveRepClick(object sender, RoutedEventArgs e) {
            if (_selectedRep != null) {
                var result = MessageBox.Show($"Are you sure you wish to delete {_selectedRep.Initial}\nand all of their project links?", "Delete Rep", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes) {
                    var repToRemove = Reps.SingleOrDefault(r => r.Initial == _selectedRep.Initial);
                    if (repToRemove != null) {
                        XmlHandler.RemoveRep(repToRemove);

                        Reps.Remove(repToRemove);
                        RepListBox.Items.Remove(repToRemove.Initial);
                    }
                }
            }
        }

        private void RepListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            RemoveRepBtn.Visibility = Visibility.Visible;
            _selectedRep = Reps.SingleOrDefault(r => r.Initial == (string) RepListBox.SelectedItem);
        }

        private void InitialsKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                if (InitialsTxtBox.Text.Length == 2 && Reps.All(r => r.Initial.ToLower() != InitialsTxtBox.Text.ToLower())) {
                    AddRepClick(null, null);
                }
            }
        }
    }
}