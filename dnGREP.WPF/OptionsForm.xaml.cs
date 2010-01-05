﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;
using dnGREP.Common;
using System.Reflection;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for OptionsForm.xaml
    /// </summary>
    public partial class OptionsForm : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public OptionsForm()
        {
            InitializeComponent();
            oldShellUnregister();
            openFileDialog.Title = "Path to custom editor...";
			DiginesisHelpProvider.HelpNamespace = "Doc\\dnGREP.chm";
			DiginesisHelpProvider.ShowHelp = true;
        }

        private System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        private static string SHELL_KEY_NAME = "dnGREP";
        private static string OLD_SHELL_KEY_NAME = "nGREP";
        private static string SHELL_MENU_TEXT = "dnGREP...";
        private bool isAdministrator = true;

        public bool IsAdministrator
        {
            get { return isAdministrator; }
            set { isAdministrator = value; changeState(); }
        }     

        private void changeState()
        {
            if (!isAdministrator)
            {
                cbRegisterShell.IsEnabled = false;
                cbRegisterShell.ToolTip = "To set shell integration run dnGREP as Administrator.";
                grShell.ToolTip = "To set shell integration run dnGREP as Administrator.";
            }
            else
            {
                cbRegisterShell.IsEnabled = true;
                cbRegisterShell.ToolTip = "Shell integration enables running an application from shell context menu.";
                grShell.ToolTip = "Shell integration enables running an application from shell context menu.";
            }

            if (Properties.Settings.Default.EnableUpdateChecking)
            {
                tbUpdateInterval.IsEnabled = true;
            }
            else
            {
                tbUpdateInterval.IsEnabled = false;
            }
            if (Properties.Settings.Default.ShowLinesInContext)
            {
                tbLinesAfter.IsEnabled = true;
                tbLinesBefore.IsEnabled = true;
            }
            else
            {
                tbLinesAfter.IsEnabled = false;
                tbLinesBefore.IsEnabled = false;
            }
            if (Properties.Settings.Default.UseCustomEditor)
            {
                rbSpecificEditor.IsChecked = true;
                rbDefaultEditor.IsChecked = false;
                tbEditorPath.IsEnabled = true;
                btnBrowse.IsEnabled = true;
                tbEditorArgs.IsEnabled = true;
            }
            else
            {
                rbSpecificEditor.IsChecked = false;
                rbDefaultEditor.IsChecked = true;
                tbEditorPath.IsEnabled = false;
                btnBrowse.IsEnabled = false;
                tbEditorArgs.IsEnabled = false;
            }
        }

        private bool isShellRegistered(string location)
        {
            if (!isAdministrator)
                return false;

            string regPath = string.Format(@"{0}\shell\{1}",
                                       location, SHELL_KEY_NAME);
            try
            {
                return Registry.ClassesRoot.OpenSubKey(regPath) != null;
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdministrator = false;
                return false;
            }
        }

        private void shellRegister(string location)
        {
            if (!isAdministrator)
                return;

            if (!isShellRegistered(location))
            {
                string regPath = string.Format(@"{0}\shell\{1}", location, SHELL_KEY_NAME);

                // add context menu to the registry

                using (RegistryKey key =
                       Registry.ClassesRoot.CreateSubKey(regPath))
                {
                    key.SetValue(null, SHELL_MENU_TEXT);
                }

                // add command that is invoked to the registry
                string menuCommand = string.Format("\"{0}\" \"%1\"",
                                       Assembly.GetAssembly(typeof(OptionsForm)).Location);
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(
                    string.Format(@"{0}\command", regPath)))
                {
                    key.SetValue(null, menuCommand);
                }
            }
        }

        private void shellUnregister(string location)
        {
            if (!isAdministrator)
                return;

            if (isShellRegistered(location))
            {
                string regPath = string.Format(@"{0}\shell\{1}", location, SHELL_KEY_NAME);
                Registry.ClassesRoot.DeleteSubKeyTree(regPath);
            }
        }

        private void oldShellUnregister()
        {
            if (!isAdministrator)
                return;

            string regPath = string.Format(@"Directory\shell\{0}", OLD_SHELL_KEY_NAME);
            if (Registry.ClassesRoot.OpenSubKey(regPath) != null)
            {
                Registry.ClassesRoot.DeleteSubKeyTree(regPath);
            }
        }

        private void checkIfAdmin()
        {
            WindowsIdentity wi = WindowsIdentity.GetCurrent();
            WindowsPrincipal wp = new WindowsPrincipal(wi);

            if (wp.IsInRole("Administrators"))
            {
                isAdministrator = true;
            }
            else
            {
                isAdministrator = false;
            }
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            checkIfAdmin();
            cbRegisterShell.IsChecked = isShellRegistered("Directory");
            cbCheckForUpdates.IsChecked = Properties.Settings.Default.EnableUpdateChecking;
            cbShowPath.IsChecked = Properties.Settings.Default.ShowFilePathInResults;
            cbShowContext.IsChecked = Properties.Settings.Default.ShowLinesInContext;
            tbLinesBefore.Text = Properties.Settings.Default.ContextLinesBefore.ToString();
            tbLinesAfter.Text = Properties.Settings.Default.ContextLinesAfter.ToString();
            cbSearchFileNameOnly.IsChecked = Properties.Settings.Default.AllowSearchingForFileNamePattern;
            tbEditorPath.Text = Properties.Settings.Default.CustomEditor;
            tbEditorArgs.Text = Properties.Settings.Default.CustomEditorArgs;
			cbPreviewResults.IsChecked = Properties.Settings.Default.PreviewResults;
			tbUpdateInterval.Text = Properties.Settings.Default.UpdateCheckInterval;
            changeState();
        }

        private void cbRegisterShell_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cbRegisterShell.IsChecked == true)
            {
                shellRegister("Directory");
                shellRegister("Drive");
                shellRegister("*");
            }
            else if (!cbRegisterShell.IsChecked == true)
            {
                shellUnregister("Directory");
                shellUnregister("Drive");
                shellUnregister("*");
            }
        }

        private void rbEditorCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rbDefaultEditor.IsChecked == true)
                Properties.Settings.Default.UseCustomEditor = false;
            else
                Properties.Settings.Default.UseCustomEditor = true;

            changeState();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.ContextLinesBefore = int.Parse(tbLinesBefore.Text);
            Properties.Settings.Default.ContextLinesAfter = int.Parse(tbLinesAfter.Text);
            Properties.Settings.Default.AllowSearchingForFileNamePattern = cbSearchFileNameOnly.IsChecked == true;
            Properties.Settings.Default.CustomEditor = tbEditorPath.Text;
            Properties.Settings.Default.CustomEditorArgs = tbEditorArgs.Text;
			Properties.Settings.Default.PreviewResults = cbPreviewResults.IsChecked == true;
			Properties.Settings.Default.UpdateCheckInterval = Utils.ParseInt(tbUpdateInterval.Text, 1).ToString();
            Properties.Settings.Default.Save();
        }

        public static string GetEditorPath(string file, int line)
        {
            if (!Properties.Settings.Default.UseCustomEditor)
            {
                return file;
            }
            else
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.CustomEditor))
                {
                    string path = Properties.Settings.Default.CustomEditor.Replace("%file", "\"" + file + "\"").Replace("%line", line.ToString());
                    return path;
                }
                else
                {
                    return file;
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbEditorPath.Text = openFileDialog.FileName;
            }
        }

        private void cbCheckForUpdates_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.EnableUpdateChecking = cbCheckForUpdates.IsChecked == true;
            if (tbUpdateInterval.Text.Trim() == "")
                tbUpdateInterval.Text = "1";
            changeState();
        }

        private void cbShowPath_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShowFilePathInResults = cbShowPath.IsChecked == true;
        }

        private void cbShowContext_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShowLinesInContext = cbShowContext.IsChecked == true;
            changeState();
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
