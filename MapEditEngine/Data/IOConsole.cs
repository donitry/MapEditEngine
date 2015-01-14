using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace TextDx5.Data
{
    public class IOConsole
    {
        public void saveData(ArrayList arry)
        {
            string fileName = @"../test.map";
            FileStream fs = new FileStream(fileName,FileMode.OpenOrCreate);
            BinaryWriter w = new BinaryWriter(fs);
            for (int i = 0; i < arry.Count; i++)
            {
                w.Write(arry[i].ToString());
            }
            w.Close();
            fs.Close();
        }
    }
}
