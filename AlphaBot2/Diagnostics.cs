using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AlphaBot2
{
    class Debug
    {
        /// <summary>
        /// Class used to record, display and export debug data
        /// </summary>
        string line_temp;
        int i;

        public Debug()
        {
            i = 0;
        }

        public void CSV_Write(string filename, string line)
        {
            i++;
            line_temp += line;
            if(i % 1000 == 0)
            {
                File.AppendAllText($@"/home/pi/{filename}.csv", line_temp);
                line_temp = "";
            }
                
        }

    }
}
