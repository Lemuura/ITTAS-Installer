﻿using ITTAS_Installer.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ITTAS_Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool scriptExists = false;
        string temp = "mods\\temp";
        string script = "nuts\\script";
        string lad = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        List<string> engineLinesList = new List<string>();
        List<string> engineWrite = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            CheckScript();
            if (scriptExists)
            {
                InitialCheck();
                GetModFiles();
            }
        }

        private void CheckScript()
        {
            if (!Directory.Exists("nuts\\script"))
            {
                MessageBoxResult result = MessageBox.Show("Can't find the \"Nuts\\Script\" directory. Make sure the installer is placed in the It Takes Two root folder.", "Directory not found");
                if (result == MessageBoxResult.OK)
                {
                    scriptExists = false;
                    System.Windows.Application.Current.Shutdown();
                }
                return;
            }
            else
                scriptExists = true;
        }

        private void InitialCheck()
        {
            Precomp();

            if (Settings.Default.FirstRunTime)
            {
                MessageBoxResult result = MessageBox.Show("Seems like this is your first time running the It Takes Two Angelscript mod installer. " +
                    "Want to create a backup of your script folder?", "", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    if (!Directory.Exists("mods"))
                        Directory.CreateDirectory("Mods");

                    try
                    {
                        if (!System.IO.File.Exists("mods\\Default-Game-Backup.zip"))
                        {
                            MessageBox.Show("This will take a moment...");
                            ZipFile.CreateFromDirectory("nuts\\script", "mods\\Default-Game-Backup.zip");
                            MessageBox.Show("Created backup!");
                        }
                        else
                            MessageBox.Show("Seems like you already have a backup.");
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, "Error");
                    }
                }

                Settings.Default.FirstRunTime = false;
                Settings.Default.Save();
            }

        }

        private bool Precomp()
        {
            if (System.IO.File.Exists(script + "\\PrecompiledScript.Cache"))
            {
                precompiledBtn.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x4E, 0xAE, 0x4A));
                precompiledText.Text = "Disable Precompiled Script Cache";
                return true;
            }
            else
            {
                precompiledBtn.Background = Brushes.IndianRed;
                precompiledText.Text = "Enable Precompiled Script Cache";
                return false;
            }
        }

        public string GetNextAvailableFileName(string filename)
        {
            if (!System.IO.File.Exists(filename)) return filename;

            string alternateFilename;
            int fileNameIndex = 1;
            do
            {
                fileNameIndex += 1;
                alternateFilename = CreateNumberedFilename(filename, fileNameIndex);
            } while (System.IO.File.Exists(alternateFilename));

            return alternateFilename;
        }

        private string CreateNumberedFilename(string filename, int number)
        {
            string plainName = System.IO.Path.GetFileNameWithoutExtension(filename);
            string extension = System.IO.Path.GetExtension(filename);
            return string.Format("{0}{1}{2}{3}{4}", "mods\\", plainName, "-", number, extension);
        }

        private void CreateBackup()
        {
            CheckScript();

            try
            {
                MessageBox.Show("This will take a moment...");
                string filename = GetNextAvailableFileName("mods\\Backup.zip");
                Console.Out.WriteLine(filename);
                ZipFile.CreateFromDirectory("nuts\\script", filename);
                MessageBox.Show("Created backup!");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error");
            }
        }

        private bool CheckModFolder()
        {
            if (!Directory.Exists("mods"))
            {
                MessageBoxResult result = MessageBox.Show("Can't find the \"Mods\" directory, want to create it?", "Directory not found", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    Directory.CreateDirectory("Mods");
                    MessageBox.Show("Created \"Mods\" directory!");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private void GetModFiles()
        {
            if (!CheckModFolder())
                return;

            string[] files = Directory.GetFiles("mods");
            saveListBox.Items.Clear();
            foreach (string file in files)
            {
                if (!file.EndsWith(".zip")) continue;
                string modName = file.Replace("mods\\", "").Replace(".zip", "");
                saveListBox.Items.Add(new ListBoxItem() { Content = modName });
            }
        }

        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                System.IO.File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        private void saveListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        private void saveListBox_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    foreach (string file in files)
                    {
                        if (!file.EndsWith(".zip")) continue;
                        string plainName = System.IO.Path.GetFileNameWithoutExtension(file);
                        System.IO.File.Copy(file, "Mods\\" + plainName + ".zip", true);
                    }
                    GetModFiles();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Error");
                }
            }
        }

        private void refreshBtn_Click(object sender, RoutedEventArgs e)
        {
            GetModFiles();
            Precomp();
        }

        private void installBtn_Click(object sender, RoutedEventArgs e)
        {
            if (saveListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a mod to install!", "No mod selected");
                return;
            }
            string modToLoad = string.Empty;
            int lastColonIndex = saveListBox.SelectedItem.ToString().LastIndexOf(": ");
            if (lastColonIndex != -1)
            {
                string modName = saveListBox.SelectedItem.ToString().Substring(lastColonIndex + 2);
                
                modToLoad = "mods\\" + modName + ".zip";
            }

            if (modToLoad == string.Empty)
            {
                MessageBox.Show("Index of \": \" was not valid.\nSelected Item: " + saveListBox.SelectedItem.ToString(), "Error");
                return;
            }

            if (!System.IO.File.Exists(modToLoad))
            {
                MessageBox.Show(modToLoad + " can't be located. Has it been moved or deleted?", "File not found");
                GetModFiles();
                return;
            }

            CheckScript();

            MessageBoxResult installresult = MessageBox.Show("Are you sure you want to install \"" + modToLoad.Replace("mods\\", "") + "\"? \nThis will take a moment...", "", MessageBoxButton.YesNo);
            if (installresult == MessageBoxResult.Yes)
            {
                try
                {
                    ExtractModFile(modToLoad);

                    if (!System.IO.File.Exists(temp + "\\vino\\characters\\PlayerCharacter.as"))
                    {
                        MessageBoxResult result = MessageBox.Show("Can't locate \"PlayerCharacter.as\", are you sure this is an It Takes Two mod?", "File not found", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.No)
                        {
                            Directory.Delete(temp, true);
                            return;
                        }
                    }

                    DeleteModsFolders();

                    if (!Directory.Exists(script + "\\PrecompiledScript"))
                        Directory.CreateDirectory(script + "\\PrecompiledScript");

                    if (System.IO.File.Exists(script + "\\PrecompiledScript.Cache"))
                        System.IO.File.Move(script + "\\PrecompiledScript.Cache", script + "\\PrecompiledScript\\PrecompiledScript.Cache", true);

                    CopyFilesRecursively(temp, script);
                    if (File.Exists("mods\\settings\\speed\\SpeedSettings.as"))
                        CopyFilesRecursively("mods\\settings", script);

                    Directory.Delete(temp, true);

                    MessageBox.Show("Installed mod!");
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Error");
                }
            } 

            // TODO
            // Move settings file to script folder
        }

        private void ExtractModFile(string modToLoad)
        {
            if (Directory.Exists(temp))
                Directory.Delete(temp, true);
            Directory.CreateDirectory(temp);
            ZipFile.ExtractToDirectory(modToLoad, temp);
        }

        private void DeleteModsFolders()
        {
            if (Directory.Exists(script + "\\mods"))
                Directory.Delete(script + "\\mods", true);
            if (Directory.Exists(script + "\\speed"))
                Directory.Delete(script + "\\speed", true);
        }

        private void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckScript();
                MessageBox.Show("This will take a moment...");

                // Delete Mods and Speed folders
                DeleteModsFolders();
                if (Directory.Exists(script + "\\PrecompiledScript"))
                {
                    if (System.IO.File.Exists(script + "\\PrecompiledScript\\PrecompiledScript.Cache"))
                        System.IO.File.Move(script + "\\PrecompiledScript\\PrecompiledScript.Cache", script + "\\PrecompiledScript.Cache", true);
                    Directory.Delete(script + "\\PrecompiledScript", true);
                }
                Precomp();

                // Check if Default Game Backup exists
                if (System.IO.File.Exists("mods\\Default-Game-Backup.zip"))
                {
                    ExtractModFile("mods\\Default-Game-Backup.zip");
                    CopyFilesRecursively(temp, script);
                    Directory.Delete(temp, true);
                    MessageBox.Show("Uninstalled mods!");
                }
                else
                {
                    MessageBox.Show("Can't locate the \"Default-Game-Backup\" file. You will need to verify the integrity of game files in Steam or repair game files through Origin.", "Can't locate file");
                }
               
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error");
            }
        }

        private void backupBtn_Click(object sender, RoutedEventArgs e)
        {
            CreateBackup();
            GetModFiles();
        }

        private void precompiledBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(script + "\\PrecompiledScript"))
                    Directory.CreateDirectory(script + "\\PrecompiledScript");

                if (Precomp())
                    System.IO.File.Move(script + "\\PrecompiledScript.Cache", script + "\\PrecompiledScript\\PrecompiledScript.Cache", true);

                else
                    System.IO.File.Move(script + "\\PrecompiledScript\\PrecompiledScript.Cache", script + "\\PrecompiledScript.Cache", true);

                Precomp();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error");
            }
        }

        private void ImportModBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (!CheckModFolder())
                return;

            try
            {
                if (openFileDialog.ShowDialog() == true)
                    System.IO.File.Copy(openFileDialog.FileName, "mods\\" + openFileDialog.SafeFileName);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error");
            }

            GetModFiles();
        }

        private void DownloadModBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/Lemuura/It-Takes-Two-Mods/releases",
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error");
            }
        }

        private void OpenModBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckModFolder())
            {
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = "mods",
                FileName = "explorer.exe",
            };

            Process.Start(startInfo);


        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            Settings.Default.Reload();
            settingsWindow.Owner = this;
            settingsWindow.CheckSettings();
            settingsWindow.ShowDialog();
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (saveListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a mod to delete!", "No mod selected");
                return;
            }

            string modToDelete = "mods\\" + saveListBox.SelectedItem.ToString().Replace("System.Windows.Controls.ListBoxItem: ", "") + ".zip";

            if (!System.IO.File.Exists(modToDelete))
            {
                MessageBox.Show("Mod can't be located. Has it been moved or deleted?", "File not found");
                GetModFiles();
                return;
            }

            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete \"" + modToDelete.Replace("mods\\", "").Replace(".zip", "") + "\"?", "Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                System.IO.File.Delete(modToDelete);
                GetModFiles();
            }
        }

        private void MenuItemRename_Click(object sender, RoutedEventArgs e)
        {
            if (saveListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a mod to rename!", "No mod selected");
                return;
            }

            string modName = saveListBox.SelectedItem.ToString().Replace("System.Windows.Controls.ListBoxItem: ", "");
            string modToRename = "mods\\" + modName + ".zip";

            if (!System.IO.File.Exists(modToRename))
            {
                MessageBox.Show("Mod can't be located. Has it been moved or deleted?", "File not found");
                GetModFiles();
                return;
            }

            DialogWindow inputDialog = new DialogWindow("Rename mod:", modName);
            if(inputDialog.ShowDialog() == true)
            {
                System.IO.File.Move(modToRename, "mods\\" + inputDialog.Answer + ".zip");
            }
            GetModFiles();
        }
    }
}
