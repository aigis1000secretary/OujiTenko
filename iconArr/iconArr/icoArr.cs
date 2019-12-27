using OujiTenko;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace iconArr
{
    public partial class icoArr : Form
    {
        public icoArr()
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            InitializeComponent();
            this.SuspendLayout();
            InitializeDataGridView();
            this.ResumeLayout(false);

            GetIDListFromCsv();
            GetClassListFromCsv();

            {
                // get all new icon list & hash
                string resource = @".\Resources";
                ArrayList dir = GetDirList(resource);
                foreach (string path in dir)
                {
                    // get ID / type / name
                    string filename = Path.GetFileNameWithoutExtension(path);
                    string extension = Path.GetExtension(path);
                    if (filename == "altx" || extension.ToLower() != ".png") continue;
                    Console.WriteLine(path);

                    string[] data = filename.Split('_');
                    string charaId = data[0];

                    // get chara type/name/base class
                    int i = Int32.Parse(charaId);
                    data = IDList[i];
                    string charaType = (data != null && data.Length > 1) ? data[1] : "---";
                    string charaName = (data != null && data.Length > 2) ? data[2] : "UNKNOWN";
                    string charaClass = (data != null && data.Length > 3) ? data[3] : "UNKNOWN";

                    // get aw class
                    string pathIndex = path.Replace(resource + @"\ico_", "").Substring(0, 2);
                    int pIndex = Int32.Parse(pathIndex);
                    foreach (string[] classData in ClassList)
                    { if (classData[0] == charaClass && classData[pIndex] != null) charaClass = classData[pIndex]; }

                    // get new icon Hash
                    string[] hashs = OujiTenko.GetIconHash(path);

                    dataGridView.Rows.Add(new string[] { charaId, charaType, charaName, charaClass, hashs[0], hashs[1], hashs[2], path });
                }
            }
        }

        public void InitializeDataGridView()
        {
            DataGridViewTextBoxColumn Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Column8 = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Column1, Column2, Column3, Column4, Column5, Column6, Column7, Column8 });
            Column1.HeaderText = "ID";
            Column2.HeaderText = "Type";
            Column3.HeaderText = "Name";
            Column4.HeaderText = "Class";
            Column5.HeaderText = "AHash";
            Column6.HeaderText = "DHash";
            Column7.HeaderText = "PHash";
            Column8.HeaderText = "Path";
        }

        private string[][] IDList = new string[1500][];
        private ArrayList ClassList = new ArrayList();
        private ArrayList HashList = new ArrayList();
        private void GetIDListFromCsv()
        {
            string csv = @".\IDList.txt";
            StreamReader sr = new StreamReader(csv, System.Text.Encoding.UTF8);
            string s;
            while ((s = sr.ReadLine()) != null)
            {
                string[] datals = s.Trim().Split(',');

                string index = datals[0];
                if (index == "") continue;

                int id = Int32.Parse(datals[0]);
                if (id < 0 || IDList.Length <= id) continue;

                IDList[id] = datals;
            }
        }
        private void GetClassListFromCsv()
        {
            string csv = @".\ClassList.txt";
            StreamReader sr = new StreamReader(csv, System.Text.Encoding.UTF8);
            string s;
            while ((s = sr.ReadLine()) != null)
            {
                string[] datals = s.Trim().Split(',');
                if (datals[0] == "") continue;

                ClassList.Add(datals);
            }
        }
        private void OutputHashList()
        {
            try
            {
                string csv = @".\HashList.txt";
                StreamWriter sw = new StreamWriter(csv, false, System.Text.Encoding.UTF8);

                for (int i = 0; i < dataGridView.Rows.Count; ++i)
                {
                    string[] tempArray = new string[dataGridView.ColumnCount];
                    for (int j = 0; j < dataGridView.ColumnCount; ++j)
                    {
                        object temp = dataGridView.Rows[i].Cells[j].Value;
                        if (temp == null)
                        {
                            tempArray[j] = "";
                        }
                        else
                        {
                            tempArray[j] = temp.ToString();
                        }
                    }
                    sw.WriteLine(string.Join(",", tempArray));
                }

                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private ArrayList GetDirList(string dir)
        {
            ArrayList result = new ArrayList();

            foreach (string path in Directory.GetDirectories(dir)) { result.AddRange(GetDirList(path)); }
            foreach (string file in Directory.GetFiles(dir)) { result.Add(file); }

            return result;
        }

        private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            OutputHashList();
        }

        private void DataGridView_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            if (rowIndex == -1) return;

            string path = (string)dataGridView.Rows[rowIndex].Cells[7].Value;
            if (this.iconBox.Image != null) this.iconBox.Image.Dispose();
            if (path != null) this.iconBox.Image = new Bitmap(path);

            //string text = (string)dataGridView.Rows[rowIndex].Cells[columnIndex].Value;
            //if (text != "") Clipboard.SetData(DataFormats.Text, @"https://wikiwiki.jp/aigiszuki/" + text);
            //else dataGridView.Rows[rowIndex].Cells[columnIndex].Value = Clipboard.GetText();
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value != null) e.Value = ((string)e.Value).Trim().Replace("	", "");
        }

        private void icoArr_FormClosing(object sender, FormClosingEventArgs e)
        {
            OutputHashList();
        }
    }
}
