using Folder2Epub;
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
        public BaseFolder2Epub folder2Epub;

        public string DocPath { get; set; }
        public string DocCover { get; set; }

        public List<Settings> SettingsList { get; set; }
        public static string[] DocumentTypes = { "docx", "txt" };

        public MainView()
        {
            InitializeComponent();
            btnCover.Enabled = false;
            SettingsList = GetAllSettings();
            lstSavedSettings.Items.AddRange(SettingsList.ToArray());
            lstSavedSettings.DisplayMember = "Title";

            lblCover.Text = string.Empty;
            lblPath.Text = string.Empty;
            cmbFileType.Items.AddRange(DocumentTypes);
            cmbFileType.SelectedIndex = 0;
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

        public Settings GetSettings()
        {
            Settings newSettings = new Settings()
            {
                Author = txtAuthor.Text,
                Title = txtTitle.Text,
                Path = DocPath,
                Cover = DocCover,
                WaitAtEnd = false
            };
            return newSettings;
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
            switch (this.cmbFileType.SelectedItem.ToString())
            {
                case "docx":
                    this.folder2Epub = new DocX2Epub(GetSettings());
                    break;
                case "txt":
                    this.folder2Epub = new Txt2Epub(GetSettings());
                    break;
                default:
                    MessageBox.Show("The selected type has not been configured.");
                    break;
            }
            lstChapters.Items.Clear();
            lstChapters.Items.AddRange(folder2Epub.GetFiles());
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
            var newSettings = GetSettings();

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

            folder2Epub.OnProgress += (current, total) => {
                progressBar1.Minimum = 0;
                progressBar1.Maximum = total;
                progressBar1.Value = current;
            };
            folder2Epub.OnUpdate += (text) =>
            {
                txtStatus.Text = text;
            };
            folder2Epub.GenerateEpub(lstChapters.CheckedItems.OfType<string>().ToArray());

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

        private void cmbFileType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(GetSettings().Path))
                UpdateFileList();
        }
    }
}
