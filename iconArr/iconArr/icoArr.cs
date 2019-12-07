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
            InitializeComponent();
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            ArrayList dir = GetDirList(@".\Resources");
            foreach (string path in dir)
            {
                Console.WriteLine(string.Join(", ", OujiTenko.GetIconHash(path)));
                Console.WriteLine("");
            }
        }
        private ArrayList GetDirList(string dir)
        {
            ArrayList result = new ArrayList();

            foreach (string path in Directory.GetDirectories(dir)) { result.AddRange(GetDirList(path)); }
            foreach (string file in Directory.GetFiles(dir)) { result.Add(file); }

            return result;
        }
    }
}
