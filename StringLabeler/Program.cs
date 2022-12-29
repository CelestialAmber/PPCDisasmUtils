using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StringLabeler {
    class Program {

        public class FileData {
            public string name;
            public string path;
            public List<string> lines;

            public FileData(string name, string path, List<string> lines) {
                this.name = name;
                this.path = path;
                this.lines = lines;
            }
        }


        public static void Main(string[] args) {
            //RemoveUnusedLabels();
            //AddLabelsToFile();
            FixFunctionLabels();
        }

        public static void FixFunctionLabels() {
            List<string> functionLabels = new List<string>();
            List<FileData> files = new List<FileData>();

            foreach (var file in Directory.EnumerateFiles("asm", "*.s", SearchOption.AllDirectories)) {
                string name = Path.GetFileName(file).Replace(".s", "");
                List<string> lines = File.ReadAllLines(file).ToList();

                files.Add(new FileData(name, file, lines));
            }

            foreach (FileData file in files) {
                Console.WriteLine(file.path);

                for (int i = 0; i < file.lines.Count; i++) {
                    string rawLine = file.lines[i];
                    string line = rawLine.Trim();

                    if (line.StartsWith("#")) {
                        continue;
                    }

                    string[] tokens = line.Split(' ');

                    if (line.StartsWith(".global lbl_")) {
                        if (file.lines[i + 2].StartsWith("/*")) functionLabels.Add(tokens[1]);
                        i += 2;
                    }
                }
            }

            foreach (FileData file in files) {
                string name = file.name;
                bool edited = false;
                int newFileLineIndex = 0;

                List<string> newFileLines = new List<string>();

                //Copy the list to the new list
                for (int i = 0; i < file.lines.Count; i++) {
                    newFileLines.Add(file.lines[i]);
                }

                for (int i = 0; i < file.lines.Count; i++) {
                    string rawLine = file.lines[i];
                    string line = rawLine.Trim();

                    if (line.StartsWith("#")) {
                        newFileLineIndex++;
                        continue;
                    }

                    string[] tokens = line.Split(' ');

                    if (line.Contains("lbl_")) {
                        string label = line.Substring(line.IndexOf("lbl_"), 12);

                        //If the label is a function, change the prefix to func
                        if (functionLabels.Contains(label)) {
                            newFileLines[i] = newFileLines[i].Replace("lbl_", "func_");
                            edited = true;
                        }
                    }

                    newFileLineIndex++;
                }

                if (edited) {
                    //string basePath = Path.GetDirectoryName(file.path);

                    File.WriteAllLines(file.path, newFileLines.ToArray());
                }
            }
        }

        public static void RemoveUnusedLabels() {
            List<string> referencedLabels = new List<string>();
            List<FileData> files = new List<FileData>();

            foreach (var file in Directory.EnumerateFiles("asm", "*.s", SearchOption.AllDirectories)) {
                string name = Path.GetFileName(file).Replace(".s", "");
                List<string> lines = File.ReadAllLines(file).ToList();

                files.Add(new FileData(name, file, lines));
            }

            foreach (FileData file in files) {
                Console.WriteLine(file.path);

                for (int i = 0; i < file.lines.Count; i++) {
                    string rawLine = file.lines[i];
                    string line = rawLine.Trim();

                    if (line.StartsWith("#")) {
                        continue;
                    }

                    string[] tokens = line.Split(' ');

                    if (line.StartsWith(".4byte lbl_")) {
                        if (!referencedLabels.Contains(tokens[1])) referencedLabels.Add(tokens[1]);
                    }

                    if (line.Contains(", lbl_") && !line.Contains("bne") && !line.Contains("beq") && !line.Contains("bge") && !line.Contains("ble") && !line.Contains("blt") && !line.Contains("bgt")) {
                        string label = line.Substring(line.IndexOf("lbl_"), line.IndexOf("@") - line.IndexOf("lbl_"));
                        referencedLabels.Add(label);
                    }
                }
            }

            foreach (FileData file in files) {
                string name = file.name;

                if (name == "data" || name.Contains("sdata") || name == "rodata") {
                    int newFileLineIndex = 0;

                    List<string> newFileLines = new List<string>();

                    //Copy the list to the new list
                    for (int i = 0; i < file.lines.Count; i++) {
                        newFileLines.Add(file.lines[i]);
                    }

                    for (int i = 0; i < file.lines.Count; i++) {
                        string rawLine = file.lines[i];
                        string line = rawLine.Trim();

                        if (line.StartsWith("#")) {
                            newFileLineIndex++;
                            continue;
                        }

                        string[] tokens = line.Split(' ');

                        if (line.StartsWith(".global lbl_")) {
                            string label = tokens[1];

                            //If the label isn't referenced anywhere, delete it
                            if (!referencedLabels.Contains(label)) {
                                //Get rid of both the .global and label lines
                                newFileLines.RemoveAt(newFileLineIndex);
                                newFileLines.RemoveAt(newFileLineIndex);
                                newFileLineIndex--;
                                i++;
                            }
                        }

                        newFileLineIndex++;
                    }

                    string basePath = Path.GetDirectoryName(file.path);

                    File.WriteAllLines(basePath + file.name + ".edited.s", newFileLines.ToArray());
                }
            }
        }

        public static void AddLabelsToFile() {
            Console.Write("Input the asm file path: ");
            string path = Console.ReadLine();

            FixStringLabels(path);
            //FixStringByteSequences(path);
        }

        /*
		Fixes all instances of part of a string being a list of bytes before the string itself
		Example:
		.byte 0x68, 0x65
		.asciz "llo"
		Fixed string: .asciz "hello"
		*/
        public static void FixStringByteSequences(string path) {
            List<string> lines = File.ReadAllLines(path).ToList();
            List<string> newFileLines = File.ReadAllLines(path).ToList();

            int newFileLineIndex = 0;
            int edits = 0;

            for (int i = 0; i < lines.Count; i++) {
                string rawLine = lines[i];
                string line = rawLine.Trim();

                if (line.StartsWith("#")) {
                    newFileLineIndex++;
                    continue;
                }

                string[] tokens = line.Split(' ');

                if (i > 0 && lines[i - 1] != newFileLines[newFileLineIndex - 1]) {
                    Console.WriteLine(lines[i - 1]);
                    Console.WriteLine(newFileLines[newFileLineIndex - 1]);
                    throw new Exception("Desync");
                }

                if (tokens[0] == ".4byte" || tokens[0] == ".byte") {

                    if (i < lines.Count - 1 && !line.StartsWith(".4byte 0x0000")) {
                        int byteLines = 1;

                        int startIndex = i;
                        int index = startIndex + 1;
                        string currentLine = lines[index];
                        bool foundPointer = false;

                        while ((currentLine.Contains(".4byte") || currentLine.Contains(".byte")) && index < lines.Count - 1) {
                            if (currentLine.Contains(".4byte 0x80")) {
                                foundPointer = true;
                            }

                            byteLines++;
                            index++;
                            currentLine = lines[index];
                        }

                        //TODO: make it so pointers are ignored and it will still look for strings in the rest of the data
                        if (currentLine.Contains(".asciz") && byteLines <= 5 && !foundPointer) {
                            List<byte> bytes = new List<byte>();
                            bool foundInvalidByte = false;

                            for (int j = startIndex; j < startIndex + byteLines; j++) {
                                if (lines[j].Contains(".4byte")) {
                                    string byteString = GetLineParts(lines[j])[1];
                                    uint val = Convert.ToUInt32(byteString, 16);

                                    for (int k = 0; k < 4; k++) {
                                        byte b = (byte)((val >> (8 * (3 - k))) & 0xFF);
                                        if (IsInvalidChar(b)) {
                                            foundInvalidByte = true;
                                            break;
                                        }
                                        bytes.Add(b);
                                    }
                                } else if (lines[j].Contains(".byte")) {
                                    string[] byteStrings = lines[j].Trim().Replace(" ", "").Replace(".byte", "").Split(",");
                                    int bytesNum = byteStrings.Length;

                                    for (int k = 0; k < bytesNum; k++) {
                                        string byteString = byteStrings[k].Replace(",", "");
                                        byte b = Convert.ToByte(byteString, 16);
                                        if (IsInvalidChar(b)) {
                                            foundInvalidByte = true;
                                            break;
                                        }
                                        bytes.Add(b);
                                    }
                                }

                                if (foundInvalidByte) break;
                            }

                            if (!foundInvalidByte) {

                                string lastLine = lines[startIndex + byteLines];
                                string lastLineString = ReadStringFromLine(lastLine);

                                List<string> newLines = new List<string>();
                                //whether or not we are currently in a string
                                bool inString = false;
                                string curString = "";
                                bool addedToLastLineString = false;

                                for (int j = 0; j < bytes.Count; j++) {
                                    byte curByte = bytes[j];

                                    if (!inString) {
                                        if (curByte == 0) {
                                            int zeroBytes = 1;
                                            int offset = 1;

                                            while (j + offset < bytes.Count && bytes[j + offset] == 0) {
                                                zeroBytes++;
                                                offset++;
                                            }

                                            int bytesLeft = zeroBytes;

                                            while (bytesLeft > 0) {
                                                if (bytesLeft >= 4) {
                                                    newLines.Add("\t.4byte 0");
                                                    bytesLeft -= 4;
                                                } else if (bytesLeft == 3) {
                                                    newLines.Add("\t.byte 0x00, 0x00, 0x00");
                                                    bytesLeft -= 3;
                                                } else if (bytesLeft == 2) {
                                                    newLines.Add("\t.2byte 0");
                                                    bytesLeft -= 2;
                                                } else {
                                                    newLines.Add("\t.byte 0x00");
                                                    bytesLeft--;
                                                }
                                            }

                                            j += zeroBytes - 1;

                                        } else inString = true;
                                    }

                                    if (inString) {
                                        //If the current byte is 0, we reached the end of the string
                                        if (curByte == 0) {
                                            inString = false;
                                            newLines.Add("\t.asciz \"" + curString + "\"");
                                            curString = "";
                                            edits++;
                                        } else {
                                            string charToAdd = ((char)curByte).ToString();
                                            curString += FixSpecialChars(charToAdd);

                                            if (j == bytes.Count - 1) {
                                                //Add the current string to the beginning of the string on the last line
                                                lastLineString = curString + lastLineString;
                                                newLines.Add("\t.asciz \"" + lastLineString + "\"");
                                                addedToLastLineString = true;
                                                edits++;
                                            }
                                        }

                                    }
                                }

                                for (int j = 0; j < byteLines; j++) {
                                    newFileLines.RemoveAt(newFileLineIndex);
                                }

                                if (addedToLastLineString) {
                                    newFileLines.RemoveAt(newFileLineIndex);
                                    i++;
                                }

                                newFileLines.InsertRange(newFileLineIndex, newLines);
                                newFileLineIndex += newLines.Count;
                                i += byteLines;
                            } else {
                                newFileLineIndex += byteLines;
                                i += byteLines;
                            }
                        } else {
                            newFileLineIndex += byteLines;
                            i += byteLines;
                        }
                    }
                }

                /*
				if (tokens[0] == ".byte"){

					//Check if the next line has a string
					if (lines[i + 1].Contains(".asciz")){
						string nextLine = lines[i + 1];
						int bytes = tokens.Length - 1;
						string newLine = nextLine.Substring(0,nextLine.IndexOf("\"") + 1);
						string s = ReadStringFromLine(nextLine);
						string nextLineEndText = nextLine.Substring(nextLine.LastIndexOf("\"") + 1);
						bool firstByteIsZero = line.StartsWith(".byte 0x00") || line == ".byte 0";
						bool lastByteIsZero = line.EndsWith("0x00");

						//If the bytes line only has a zero byte or ends with a zero byte, keep going
						if ((bytes == 1 && firstByteIsZero) || lastByteIsZero)
						{
							newFileLineIndex++;
						}
						else {

							for (int j = 0; j < bytes; j++)
							{
								//Skip the first byte if it's zero
								if (j == 0 && firstByteIsZero) continue;

								string byteString = tokens[j + 1].Replace("0x", "").Replace(",", "");
								byte charByte = Convert.ToByte(byteString, 16);
								string charString = ((char)charByte).ToString();

								newLine += FixSpecialChars(charString);
							}

							newLine += s + "\"" + nextLineEndText;

							//If the first byte is zero, keep the zero byte in the .byte line
							if (firstByteIsZero)
							{
								newFileLines[newFileLineIndex] = rawLine.Substring(0, rawLine.IndexOf(","));
								newFileLineIndex++;
								newFileLines[newFileLineIndex] = newLine;

							} else {
								newFileLines.RemoveAt(newFileLineIndex);
								newFileLines[newFileLineIndex] = newLine;
							}

							edits++;
						}

						i++;
					}
					
				}
				*/

                newFileLineIndex++;
            }

            File.WriteAllLines(path + ".edited.txt", newFileLines.ToArray());
            Console.WriteLine("Fixed " + edits + " strings");

        }

        public static void FixStringLabels(string path) {
            List<string> lines = File.ReadAllLines(path).ToList();
            List<string> newFileLines = File.ReadAllLines(path).ToList();

            Console.Write("Enter start address: ");
            long startAddress = Convert.ToInt64(Console.ReadLine(), 16);

            long address = startAddress;
            int offset = 0;
            int newFileLineIndex = 0;

            for (int i = 0; i < lines.Count; i++) {
                string addressString = address.ToString("X");
                string rawLine = lines[i];
                string line = rawLine.Trim();

                //If we reach an existing label and the current address doesn't match, something went wrong
                if (line.Contains(".global lbl_") && !line.Contains(addressString)) {
                    throw new Exception("Address misaligned");
                }

                if (line.StartsWith("#")) {
                    newFileLineIndex++;
                    continue;
                }

                string[] tokens = line.Split(' ');

                if (tokens[0] == ".4byte" || tokens[0] == ".float") {
                    address += 4;
                    offset += 4;

                    if (!newFileLines[newFileLineIndex - 1].Contains(":")) { //&& line != ".4byte 0") {
                        string labelString = "lbl_" + addressString;
                        newFileLines.Insert(newFileLineIndex, ".global " + labelString);
                        newFileLineIndex++;
                        newFileLines.Insert(newFileLineIndex, labelString + ":");
                        newFileLineIndex++;
                    }

                } else if (tokens[0] == ".2byte") {
                    address += 2;
                    offset += 2;
                } else if (tokens[0] == ".byte") {
                    string[] byteStrings = line.Replace(" ", "").Replace(".byte", "").Split(",");
                    int bytes = byteStrings.Length;
                    address += bytes;
                    offset += bytes;
                } else if (tokens[0] == ".asciz") {
                    string s = line.Substring(line.IndexOf("\"") + 1, line.LastIndexOf("\"") - line.IndexOf("\"") - 1);
                    s = s.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\\'", "\'");

                    //The string's actual length is 1 byte more because strings are zero terminated
                    int length = s.Length + 1;

                    //Console.WriteLine("Current address: " + addressString);
                    Console.WriteLine("\"" + s + "\", length: " + length);

                    if (newFileLineIndex == 0 || !newFileLines[newFileLineIndex - 1].Contains(":")) {
                        string labelString = "lbl_" + addressString;
                        newFileLines.Insert(newFileLineIndex, ".global " + labelString);
                        newFileLineIndex++;
                        newFileLines.Insert(newFileLineIndex, labelString + ":");
                        newFileLineIndex++;
                    }

                    address += length;
                    offset += length;
                } else if (line.Contains(".balign 4")) {
                    //The string data is padded with zeros so that the start address of the next data is a multiple of 4
                    int bytesToAdd = 4 - (int)(address % 4);

                    address += bytesToAdd;
                    offset += bytesToAdd;
                }
                newFileLineIndex++;
            }

            File.WriteAllLines(path + ".edited.txt", newFileLines.ToArray());
        }

        public static string[] GetLineParts(string line) {
            return line.Trim().Split(" ");
        }

        //Parses a string in an .asciz line, correcting all special characters.
        public static string ReadStringFromLine(string line) {
            string s = line.Substring(line.IndexOf("\"") + 1, line.LastIndexOf("\"") - line.IndexOf("\"") - 1);
            s = FixSpecialChars(s);
            return s;
        }

        public static string FixSpecialChars(string s) {
            //Replace all special character sequences with the internal ones
            return s.Replace("\n", "\\n").Replace("\t", "\\t").Replace("\"", "\\\"").Replace("\\", "\\\\").Replace("\'", "\\\'");
        }

        public static bool IsInvalidChar(byte b) {
            return (b >= 0x7F || b < 0x20) && b != 0x00 && b != 0x09 && b != 0x0A;
        }
    }
}
