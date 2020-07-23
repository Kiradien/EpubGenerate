using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DocXFolderToEpubGUI
{
    public partial class MainView : Form
    {
        public string Path { get; set; }
        public string Cover { get; set; }

        public MainView()
        {
            InitializeComponent();
            btnCover.Enabled = false;
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Path = folderBrowserDialog1.SelectedPath;
                lblPath.Text = Path;
                btnCover.Enabled = true;

                lstChapters.Items.Clear();
                lstChapters.Items.AddRange(DocXFolderToEpub.Program.GetFiles(Path));
                for (int i = 0; i < lstChapters.Items.Count; i++)
                    lstChapters.SetItemChecked(i, true);

                //lstChapters.CheckedItems
            }
        }

        private void btnCover_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog1 = new OpenFileDialog();
            fileDialog1.Filter = "Image Files(*.png, *.jpg; *.jpeg; *.gif; *.bmp)|*.png; *.jpg; *.jpeg; *.gif; *.bmp";
            fileDialog1.InitialDirectory = Path;
            if (fileDialog1.ShowDialog() == DialogResult.OK)
            {
                Cover = fileDialog1.FileName;
                lblCover.Text = Cover;
            }

        }
    }
}
