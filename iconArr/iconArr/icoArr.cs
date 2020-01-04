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
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace iconArr
{
    public partial class icoArr : Form
    {
        string resource = @".\Resources";
        public icoArr()
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            InitializeComponent();
            this.SuspendLayout();
            InitializeDataGridView();
            this.ResumeLayout(false);

            GetIDListFromRaw();
            GetClassListFromRaw();

            {
                // get all new icon list & hash
                ArrayList dir = GetDirList(resource);
                foreach (string path in dir)
                {
                    // get ID / type / name
                    string filename = Path.GetFileNameWithoutExtension(path);
                    string extension = Path.GetExtension(path);
                    if (filename == "altx" || extension != ".png") continue;
                    Console.WriteLine(path);

                    // get id data
                    string[] data = filename.Split('_');
                    int id = Int32.Parse(data[0]);
                    int classType =
                        (path.IndexOf("00.aar") != -1) ? 0 :  // normal or CC
                        (path.IndexOf("01.aar") != -1) ? 1 :  // AW 1
                        (path.IndexOf("02.aar") != -1) ? 2 :  // AW 2A
                        (path.IndexOf("03.aar") != -1) ? 3 : -1;  // AW 2B

                    // get chara name/baseClass
                    string charaName = "Name";
                    int charaClassID = -1;
                    if ((data = IDList[id]) != null) //  { name, initClassID }
                    {
                        charaName = data[0];
                        charaClassID = Int32.Parse(data[1]);
                    }

                    // get true class id/name
                    string charaClass = "Class";
                    //if (id == 539) Console.ReadLine();
                    //Console.WriteLine(classType + ", " + data[0]);
                    data = GetClassData(charaClassID);    // { className, jobChange, awakeType1, awakeType2 }
                    if (classType == 0)
                    {
                        // charaClassID = charaClassID;
                        charaClass = data[0];
                    }
                    else if (classType == 1 || classType == 2 || classType == 3)    // aw1~2
                    {
                        int ccID;
                        while ((ccID = Int32.Parse(data[1])) != 0) { charaClassID = ccID; data = GetClassData(ccID); }
                        if (classType == 1) // aw1
                        {
                            // charaClassID = charaClassID;
                            charaClass = data[0];
                        }
                        else if (classType == 2 || classType == 3) // aw2A
                        {
                            // charaClassID = charaClassID;
                            if ((ccID = Int32.Parse(data[classType])) != 0)
                            {
                                charaClassID = ccID;
                                data = GetClassData(ccID);
                                charaClass = data[0];
                            }
                        }
                    }

                    // get new icon Hash
                    string[] hashs = OujiTenko.GetIconHash(path);

                    dataGridView.Rows.Add(new string[] { id.ToString("000"), charaName, charaClass, hashs[0], hashs[1], hashs[2], path });
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

            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Column1, Column2, Column3, Column4, Column5, Column6, Column7 });
            Column1.HeaderText = "ID";
            Column2.HeaderText = "Name";
            Column3.HeaderText = "Class";
            Column4.HeaderText = "AHash";
            Column5.HeaderText = "DHash";
            Column6.HeaderText = "PHash";
            Column7.HeaderText = "Path";
        }

        private string[][] IDList = new string[1500][];
        private void GetIDListFromRaw()
        {
            // read file
            string raw = resource + @".\cards.txt";
            string csv = @".\cards.csv";
            try
            {
                StreamReader sr = new StreamReader(raw, System.Text.Encoding.UTF8);
                StreamWriter sw = new StreamWriter(csv, false, System.Text.Encoding.UTF8);
                string line;
                while ((line = sr.ReadLine()) != null && line != "")
                {
                    line = line.Trim();
                    // format data
                    Match match;
                    while ((match = Regex.Match(line, @"\s+[\d.A-z-,]+")).Success)
                    {
                        line = line.Replace(match.Value, match.Value.Replace(Regex.Match(match.Value, @"\s+,?").Value, ","));
                    }

                    // write data to csv
                    sw.WriteLine(line);

                    if (line.IndexOf("ame") != -1) continue;
                    // put data to array
                    string[] data = line.Split(',');
                    //Console.WriteLine(data[1]);
                    int id = Int32.Parse(data[1]);
                    string name = data[0];
                    string initClassID = data[2];
                    IDList[id] = new string[] { name, initClassID };
                }
                sr.Close();
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private Dictionary<int, string[]> ClassList = new Dictionary<int, string[]>();
        private void GetClassListFromRaw()
        {
            // read file
            string raw = resource + @".\PlayerUnitTable.aar\002_ClassData.atb\ALTB_cldt.txt";
            string csv = @".\class.csv";
            try
            {
                StreamReader sr = new StreamReader(raw, System.Text.Encoding.UTF8);
                StreamWriter sw = new StreamWriter(csv, false, System.Text.Encoding.UTF8);
                string line;
                while ((line = sr.ReadLine()) != null && line != "")
                {
                    line = line.Trim();
                    // format data
                    Match match;
                    while ((match = Regex.Match(line, @"\s\x22\s")).Success)
                    {
                        line = Regex.Replace(line, @"\s\x22\s", "\"");
                    }
                    line = Regex.Replace(line, @"\s+", ",");
                    while ((match = Regex.Match(line, @"\x22[^,\x22][^\s\x22]+[,][^\s\x22]+\x22")).Success)
                    {
                        line = line.Replace(match.Value, match.Value.Replace(Regex.Match(match.Value, @",").Value, " "));
                    }
                    line = Regex.Replace(line, @"\x22", "");

                    // write data to csv
                    sw.WriteLine(line);

                    if (line.IndexOf("ame") != -1) continue;
                    // put data to array
                    string[] data = line.Split(',');
                    //Console.WriteLine(data[0]);
                    int id = Int32.Parse(data[0]);
                    string className = data[1];
                    string jobChange = data[14];
                    string awakeType1 = data[34];
                    string awakeType2 = data[35];
                    ClassList.Add(id, new string[] { className, jobChange, awakeType1, awakeType2 });
                }
                sr.Close();
                sw.Close();
                ClassList.Add(0, new string[] { "UNKNOWN", "0", "0", "0" });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }
        private string[] GetClassData(int id)
        {
            string[] data;
            if (ClassList.ContainsKey(id) && (data = ClassList[id]) != null)
            {
                return data;
            }
            return ClassList[0];
        }

        //private ArrayList HashList = new ArrayList();
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

        // Form event
        private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            OutputHashList();
        }

        private void DataGridView_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            if (rowIndex == -1) return;

            string path = (string)dataGridView.Rows[rowIndex].Cells[6].Value;
            if (this.iconBox.Image != null) this.iconBox.Image.Dispose();
            if (path != null) this.iconBox.Image = new Bitmap(path);

            //string text = (string)dataGridView.Rows[rowIndex].Cells[columnIndex].Value;
            //if (text != "") Clipboard.SetData(DataFormats.Text, @"https://wikiwiki.jp/aigiszuki/" + text);
            //else dataGridView.Rows[rowIndex].Cells[columnIndex].Value = Clipboard.GetText();
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            //if (e.Value != null) e.Value = ((string)e.Value).Trim().Replace("	", "");
            if (e.Value != null) e.Value = ((string)e.Value).Trim().Replace("\t", "");
        }

        private void IcoArr_FormClosing(object sender, FormClosingEventArgs e)
        {
            OutputHashList();
        }
    }
}
