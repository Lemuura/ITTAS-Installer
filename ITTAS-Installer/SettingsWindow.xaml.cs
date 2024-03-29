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
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        //string temp = "mods\\temp";
        string settings = "mods\\settings\\speed\\SpeedSettings.as";

        string enginePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ItTakesTwo\\Saved\\Config\\WindowsNoEditor\\Engine.ini";

        List<string> engineLines = new List<string>();

        private List<string> settingsContents = new List<string>();
        private List<string> ActionNames = new List<string>()
        {
            "Sprint",
            "Jump",
            "Dash",
            "Crouch",
            "Interact",
            "Rail Grab",
            "Fire",
            "Aim",
            "Find Other Player",
            "Cancel",
            "Enter",                  
            "C"  
        };

        public SettingsWindow()
        {
            InitializeComponent();
            
            Window_Loaded();
        }
        public void CheckSettings()
        {
            Speedtools.IsChecked = Settings.Default.Speedtools;

            Save1.SelectedIndex = Settings.Default.Save1;
            Save2.SelectedIndex = Settings.Default.Save2;
            Load1.SelectedIndex = Settings.Default.Load1;
            Load2.SelectedIndex = Settings.Default.Load2;
            Teleport1.SelectedIndex = Settings.Default.Teleport1;
            Teleport2.SelectedIndex = Settings.Default.Teleport2;

            ReadEngineIni();
            forceDebugView.IsChecked = engineLines.Contains("r.ForceDebugViewModes=1");
            diskCache.IsChecked = (engineLines.Contains("D3D12.PSO.DiskCache=1") || engineLines.Contains("D3D12.PSO.DriverOptimizedDiskCache=1"));
        }

        private void ReadEngineIni()
        {
            if (!System.IO.File.Exists(enginePath))
            {
                MessageBox.Show("Engine.ini file can't be located. Has it been moved or deleted?", "File not found");
                return;
            }

            engineLines.Clear();
            engineLines = System.IO.File.ReadAllLines(enginePath).ToList();

            Console.WriteLine("Contents of Engine.ini = ");
            foreach (string line in engineLines)
            {
                Console.WriteLine("\t" + line);
            }
        }

        private void WriteEngineIni()
        {
            // Read file again to make sure it's 100% up to date
            ReadEngineIni();

            // Find or create [/script/engine.renderersettings]
            int renderIndex = engineLines.IndexOf("[/script/engine.renderersettings]");
            if (renderIndex == -1)
            {
                renderIndex = 0;
                engineLines.Insert(renderIndex, "[/script/engine.renderersettings]");
                engineLines.Insert(renderIndex + 1, "");
            }

            List<string> engineWrite = new List<string>();

            // Add various lines we want in renderersettings.
            if (forceDebugView.IsChecked == true && !engineLines.Contains("r.ForceDebugViewModes=1"))
            {
                engineWrite.Add("r.ForceDebugViewModes=1");
            }
            if (diskCache.IsChecked == true)
            {
                if (!engineLines.Contains("D3D12.PSO.DiskCache=1"))
                {
                    engineWrite.Add("D3D12.PSO.DiskCache=1");
                }

                if (!engineLines.Contains("D3D12.PSO.DriverOptimizedDiskCache=1"))
                {
                    engineWrite.Add("D3D12.PSO.DriverOptimizedDiskCache=1");
                }
            }

            // Or remove if we don't want them anymore
            if (forceDebugView.IsChecked != true)
            {
                int i = engineLines.IndexOf("r.ForceDebugViewModes=1");
                if (i != -1)
                {
                    engineLines.RemoveAt(i);
                }
            }
            if (diskCache.IsChecked != true)
            {
                int i = engineLines.IndexOf("D3D12.PSO.DiskCache=1");
                if (i != -1)
                {
                    engineLines.RemoveAt(i);
                }

                i = engineLines.IndexOf("D3D12.PSO.DriverOptimizedDiskCache=1");
                if (i != -1)
                {
                    engineLines.RemoveAt(i);
                }
            }

            // Insert these into the final list under the correct header
            engineLines.InsertRange(renderIndex + 1, engineWrite);
            System.IO.File.WriteAllLinesAsync(enginePath, engineLines);
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reload();
            CheckSettings();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Option_Checked(object sender, RoutedEventArgs e)
        {
            SetCheckedState();
        }

        private void Option_Unchecked(object sender, RoutedEventArgs e)
        {
            SetCheckedState();
        }

        private void SetCheckedState()
        {
            if (Speedtools != null)
            {
                if (Speedtools.IsChecked == true)
                    Settings.Default.Speedtools = true;
                else
                    Settings.Default.Speedtools = false;

                //Settings.Default.Save();
            }
        }

        private void Window_Loaded()
        {
            Save1.ItemsSource = ActionNames;
            Save2.ItemsSource = ActionNames;
            Load1.ItemsSource = ActionNames;
            Load2.ItemsSource = ActionNames;
            Teleport1.ItemsSource = ActionNames;
            Teleport2.ItemsSource = ActionNames;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = sender as ComboBox;

            if (item == null)
                return;

            var name = item.Name;
            var index = item.SelectedIndex;
                
            switch (name)
            {
                case "Save1":
                    Settings.Default.Save1 = index;
                    break;
                case "Save2":
                    Settings.Default.Save2 = index;
                    break;
                case "Load1":
                    Settings.Default.Load1 = index;
                    break;
                case "Load2":
                    Settings.Default.Load2 = index;
                    break;
                case "Teleport1":
                    Settings.Default.Teleport1 = index;
                    break;
                case "Teleport2":
                    Settings.Default.Teleport2 = index;
                    break;
            }   
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveSettingsFile();
            WriteEngineIni();
            Settings.Default.Save();
        }

        private void DefaultBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Speedtools = false;
            Settings.Default.Save1 = 5;
            Settings.Default.Save2 = 8;
            Settings.Default.Load1 = 0;
            Settings.Default.Load2 = 5;
            Settings.Default.Teleport1 = 9;
            Settings.Default.Teleport2 = 8;

            CheckSettings();
        }

        private void SaveSettingsFile()
        {
            settingsContents.Clear();

            string contents = "const bool SpeedToolsActiveOnLaunch = ";
            if (Settings.Default.Speedtools)
                contents += "true;";
            else contents += "false;";
            settingsContents.Add(contents);
            settingsContents.Add("");

            WriteFNames("SaveState1");
            WriteFNames("SaveState2");
            WriteFNames("LoadState1");
            WriteFNames("LoadState2");
            WriteFNames("TeleportOther1");
            WriteFNames("TeleportOther2");

            if (!Directory.Exists("mods\\settings\\Speed"))
                Directory.CreateDirectory("Mods\\Settings\\Speed");
            File.WriteAllLines(settings, settingsContents);

            MainWindow.CopyFilesRecursively("Mods\\Settings", "nuts\\script");
        }

        private void WriteFNames(string fname)
        {
            string contents = "const FName " + fname + " = ActionNames::";
            switch (fname)
            {
                case "SaveState1":
                    contents += States(Settings.Default.Save1);
                    break;
                case "SaveState2":
                    contents += States(Settings.Default.Save2);
                    break;
                case "LoadState1":
                    contents += States(Settings.Default.Load1);
                    break;
                case "LoadState2":
                    contents += States(Settings.Default.Load2);
                    break;
                case "TeleportOther1":
                    contents += States(Settings.Default.Teleport1);
                    break;
                case "TeleportOther2":
                    contents += States(Settings.Default.Teleport2);
                    break;
            }
            settingsContents.Add(contents);
        }

        private string States(int i)
        {
            switch (i)
            {
                case 0:
                    return "MovementSprintToggle;";
                case 1:
                    return "MovementJump;";
                case 2:
                    return "MovementDash;";
                case 3:
                    return "MovementCrouch;";
                case 4:
                    return "InteractionTrigger;";
                case 5:
                    return "SwingAttach;";
                case 6:
                    return "WeaponFire;";
                case 7:
                    return "WeaponAim;";
                case 8:
                    return "FindOtherPlayer;";
                case 9:
                    return "Cancel;";
                case 10:
                    return "GUIOk;";
                case 11:
                    return "MusicFlyingTightTurnRight;";
                default:
                    return "";
            }
        }
    }
}
