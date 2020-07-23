using DocXFolderToEpub;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DocXFolderToEpubGUI
{
    public partial class MainView : Form
    {
        public string DocPath { get; set; }
        public string DocCover { get; set; }

        public List<Settings> SettingsList { get; set; }

        public MainView()
        {
            InitializeComponent();
            btnCover.Enabled = false;
            SettingsList = GetAllSettings();
            lstSavedSettings.Items.AddRange(SettingsList.ToArray());
            lstSavedSettings.DisplayMember = "Title";
        }

        public List<Settings> GetAllSettings()
        {
            string settingsPath = $"{Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)}.json";
            if (!File.Exists(settingsPath))
            {
                List<Settings> SettingsObjects = new List<Settings>();

                Console.WriteLine("No settings detected. Creating default settings file at location.");
                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(SettingsObjects.ToArray(), Formatting.Indented));
                Console.WriteLine($"Settings file created. {settingsPath} Please edit the file and run again to generate.");
            }
            return JsonConvert.DeserializeObject<Settings[]>(File.ReadAllText(settingsPath)).ToList();
        }
        public void SaveAllSettings()
        {
            string settingsPath = $"{Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)}.json";
            File.WriteAllText(settingsPath, JsonConvert.SerializeObject(SettingsList, Formatting.Indented));
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                DocPath = folderBrowserDialog1.SelectedPath;
                lblPath.Text = DocPath;
                btnCover.Enabled = true;

                UpdateFileList();
            }
        }

        private void UpdateFileList()
        {
            lstChapters.Items.Clear();
            lstChapters.Items.AddRange(DocXFolderToEpub.Program.GetFiles(DocPath));
            for (int i = 0; i < lstChapters.Items.Count; i++)
                lstChapters.SetItemChecked(i, true);
        }

        private void btnCover_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog1 = new OpenFileDialog();
            fileDialog1.Filter = "Image Files(*.png, *.jpg; *.jpeg; *.gif; *.bmp)|*.png; *.jpg; *.jpeg; *.gif; *.bmp";
            fileDialog1.InitialDirectory = DocPath;
            if (fileDialog1.ShowDialog() == DialogResult.OK)
            {
                DocCover = fileDialog1.FileName;
                lblCover.Text = DocCover;
            }

        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            btnGenerate.Enabled = false;
            Settings newSettings = new Settings()
            {
                Author = txtAuthor.Text,
                Title = txtTitle.Text,
                Path = DocPath,
                Cover = DocCover,
                WaitAtEnd = false
            };

            if (DocPath == null || DocPath.Length < 1)
            {
                btnPath.Focus();
                MessageBox.Show("File Path Required.");
                btnGenerate.Enabled = true;
                return;
            }

            if (newSettings.Title.Length < 1)
            {
                txtTitle.Focus();
                MessageBox.Show("Title required.");
                btnGenerate.Enabled = true;
                return;
            }

            if (newSettings.Author.Length < 1)
            {
                txtAuthor.Focus();
                MessageBox.Show("Author Name required.");
                btnGenerate.Enabled = true;
                return;
            }

            SettingsList.RemoveAll(item => item.Title == newSettings.Title);
            SettingsList.Add(newSettings);
            lstSavedSettings.Items.Clear();
            lstSavedSettings.Items.AddRange(SettingsList.ToArray());
            SaveAllSettings();

            DocXFolderToEpub.Program.GenerateEpub(newSettings, lstChapters.CheckedItems.OfType<string>().ToArray());
            MessageBox.Show($"Generation of epub complete\n{newSettings.GetTitlePath()}");

            btnGenerate.Enabled = true;
        }

        private void lstSavedSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItem = (Settings)lstSavedSettings.SelectedItem;
            if (selectedItem != null)
            {
                txtAuthor.Text = selectedItem.Author;
                txtTitle.Text = selectedItem.Title;
                DocPath = selectedItem.Path;
                lblPath.Text = DocPath;
                DocCover = selectedItem.Cover;
                lblCover.Text = DocCover;
                UpdateFileList();
            }
        }
    }
}
