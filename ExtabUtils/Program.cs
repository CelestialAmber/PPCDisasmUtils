using System;
using System.Collections.Generic;
using System.IO;

namespace ExtabUtils
{


    class FileExtabData
    {
        public string filename;
        public List<string> entries;
        public List<string> extabData;

        public FileExtabData(string name, List<string> entries)
        {
            filename = name;
            this.entries = entries;
            extabData = new List<string>();
            extabData.Add(".section extab, \"wa\"  # 0x800066E0 - 0x80021020");
            extabData.Add("");
        }
    }


    class Program {

        static List<FileExtabData> fileExtabDataList = new List<FileExtabData>();

        public static void Main(string[] args) {
            ParseFileExtabEntriesData();
            ParseFileExtabData();

            foreach(FileExtabData file in fileExtabDataList)
            {
                string path = file.filename.Substring(0, file.filename.LastIndexOf("/"));
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                File.WriteAllLines(file.filename, file.extabData);
            }
        }

        public static void ParseFileExtabData()
        {
            string[] lines = File.ReadAllLines("extab.txt");

            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i].Trim();

                if (line.StartsWith(".global")){
                    string labelName = line.Replace(".global ", "");
                    foreach(FileExtabData fileExtabData in fileExtabDataList)
                    {
                        if (fileExtabData.entries.Contains(labelName))
                        {
                            //If the current file contains this label, add the entry lines to the list
                            fileExtabData.extabData.Add(line);
                            i++;

                            while (i < lines.Length - 1 && !lines[i + 1].Contains(".global"))
                            {
                                string currentLine = lines[i];
                                fileExtabData.extabData.Add(currentLine);
                                i++;
                                
                                //If this is the last line, add it to the list
                                if(i == lines.Length - 1)
                                {
                                    fileExtabData.extabData.Add(lines[i]);
                                }
                            }

                            fileExtabData.extabData.Add("");

                        }
                    }
                }

                i++;
            }
           }


        public static void ParseFileExtabEntriesData() {
            string[] lines = File.ReadAllLines("extabdata.txt");

            List<string> currentExtabEntriesList = new List<string>();

            int i = 0;
            while (i < lines.Length) {
                string filename = lines[i++];

                while (i < lines.Length && lines[i].Trim() != "")
                {
                    string line = lines[i].Trim();
                    currentExtabEntriesList.Add(line);
                    i++;
                }

                i++;

                fileExtabDataList.Add(new FileExtabData(filename, currentExtabEntriesList));
                currentExtabEntriesList = new List<string>();
            }
        }

    }
}
