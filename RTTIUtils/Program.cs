using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RTTIUtils {

    class ClassData {
        public string name;
        public bool isBaseClass;
        public bool hasVTable;
        public List<List<string>> vTableFunctionGroups = new List<List<string>>();
        public List<string> inheritedClasses = new List<string>();

        public ClassData()
        {

        }

        public ClassData(string name)
        {
            this.name = name;
        }
    }


    class Program {

        static Dictionary<string, string> classNames = new Dictionary<string, string>();
        static  Dictionary<string,List<List<string>>> vTableFunctionGroupsDictionary = new Dictionary<string, List<List<string>>>();
        static Dictionary<string, List<string>> inheritanceTables = new Dictionary<string, List<string>>();

        public static void Main(string[] args)
        {
            ParseNameData();
            ParseInheritanceTables();
            ParseVTableData();

            List<ClassData> classList = new List<ClassData>();

            foreach(string classLabel in classNames.Keys)
            {
                ClassData classData = new ClassData(classNames[classLabel]);

                //If this class isn't a base class, get its list of inherited classes
                if (inheritanceTables.ContainsKey(classLabel)) {
                    classData.inheritedClasses = inheritanceTables[classLabel];
                } else {
                    classData.isBaseClass = true;
                }


                //If this class has a vtable, add it to the entry
                if (vTableFunctionGroupsDictionary.ContainsKey(classLabel)) {
                    classData.vTableFunctionGroups = vTableFunctionGroupsDictionary[classLabel];
                    classData.hasVTable = true;
                }

                classList.Add(classData);

            }

            string text = JValue.Parse(JsonConvert.SerializeObject(classList, new Newtonsoft.Json.Converters.StringEnumConverter())).ToString(Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("classData.json", text);
        }

        public static void ParseNameData()
        {
            string[] lines = File.ReadAllLines("names.txt");

            foreach(string line in lines)
            {
                string[] parts = line.Split(": \"");
                parts[1] = parts[1].Trim().Replace("\"", "");
                classNames.Add(parts[0], parts[1]);
            }
        }

        public static void ParseInheritanceTables()
        {
             string[] lines = File.ReadAllLines("inheritancetables.txt");

            int i = 0;
            while(i < lines.Length) {
                string line = lines[i++].Trim();
                if (line == "") continue;

                if (i == lines.Length) break;

                string label = line.Replace(":","");

                List<string> inheritedClasses = new List<string>();

                while (lines[i].Trim() != "")
                {
                    inheritedClasses.Add(classNames[lines[i].Trim()]);
                    i++;
                    if (i == lines.Length) break;
                }

                inheritanceTables.Add(label, inheritedClasses);
            }
        }


        public static void ParseVTableData() {
            string[] lines = File.ReadAllLines("vtables.txt");
            bool parsingVTable = false;
     
            List<string> currentFunctionGroup = new List<string>();
            List<List<string>> currentFunctionGroupList = new List<List<string>>();
            string currentLabel = "";

            int i = 0;
            while(i < lines.Length) {
                if (!parsingVTable)
                {
                    string line = lines[i++].Trim();
                    if (line != "")
                    {

                        currentLabel = line.Replace("__vt__", "").Replace(":", "");
                        string name = classNames[currentLabel];

                        i++;
                        parsingVTable = true;
                    }
                }
                else
                {
                    while (lines[i].Trim() != "") {
                        string curLine = lines[i].Trim();

                        if (curLine == "start_group") {
                            currentFunctionGroupList.Add(currentFunctionGroup);
                            currentFunctionGroup = new List<string>();
                        } else {
                            currentFunctionGroup.Add(curLine);
                        }

                        i++;
                        if (i == lines.Length) break;
                    }

                    parsingVTable = false;
                    currentFunctionGroupList.Add(currentFunctionGroup);
                    vTableFunctionGroupsDictionary.Add(currentLabel,currentFunctionGroupList);
                    currentFunctionGroup = new List<string>();
                    currentFunctionGroupList = new List<List<string>>();
                }
           
            }
        }

    }
}
