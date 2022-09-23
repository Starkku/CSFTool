/*
 * Copyright 2017-2022 by Starkku
 * This file is part of CSFTool, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see LICENSE.txt.
 */

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CSFTool
{
    /// <summary>
    /// Command & Conquer: Red Alert 2 / Command & Conquer: Generals CSF file class.
    /// </summary>
    public class CSFFile
    {
        /// <summary>
        /// Current filename of CSF file.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// CSF file format version number.
        /// </summary>
        public int CSFFormatVersion { get; set; } = 3;

        /// <summary>
        /// Total number of labels in this CSF file.
        /// </summary>
        public int LabelCount => labels.Count;

        /// <summary>
        /// Total number of strings in this CSF file.
        /// </summary>
        public int StringCount => GetStringCount();

        /// <summary>
        /// CSF file language ID.
        /// </summary>
        public CSFLanguage Language { get; set; } = CSFLanguage.EnglishUS;

        /// <summary>
        /// Has the CSF file been altered after initialization or not.
        /// </summary>
        public bool Altered { get; private set; }

        private readonly List<CSFLabel> labels = new List<CSFLabel>();

        /// <summary>
        /// Initializes a new, empty CSF file with default settings and specified filename.
        /// </summary>
        /// <param name="filename">Filename of the CSF file.</param>
        public CSFFile(string filename)
        {
            Filename = filename;
        }

        /// <summary>
        /// Initializes a new, empty CSF file with specified language, CSF file format version and filename.
        /// </summary>
        /// <param name="filename">Filename of the CSF file.</param>
        /// <param name="version">CSF file version number.</param>
        /// <param name="language">CSF file language ID.</param>
        public CSFFile(string filename, int version, int language)
        {
            Filename = filename;
            CSFFormatVersion = version;
            Language = (CSFLanguage)language;
        }

        /// <summary>
        /// Attempts to load CSF file information from file.
        /// </summary>
        /// <returns>Error message if something went wrong, otherwise null.</returns>
        public string Load()
        {
            labels.Clear();

            FileStream fs = null;
            try
            {
                fs = new FileStream(Filename, FileMode.Open);
                byte[] b = new byte[4];

                fs.Read(b, 0, b.Length);
                string ID = Encoding.ASCII.GetString(b);

                if (!ID.Equals(" FSC", StringComparison.InvariantCultureIgnoreCase))
                    return "File '" + Filename + "' is not a valid CSF string table file.";

                fs.Read(b, 0, b.Length);
                CSFFormatVersion = BitConverter.ToInt32(b, 0);
                fs.Read(b, 0, b.Length);
                int labelCount = BitConverter.ToInt32(b, 0);
                fs.Read(b, 0, b.Length);
                int stringCount = BitConverter.ToInt32(b, 0);
                fs.Read(b, 0, b.Length); // Unused data
                fs.Read(b, 0, b.Length);

                int languageID = BitConverter.ToInt32(b, 0);

                if (languageID < -1 || languageID > 9)
                    Language = CSFLanguage.Unknown;
                else
                    Language = (CSFLanguage)languageID;

                byte[] data = null;
                byte[] dataEx = null;

                while (fs.Read(b, 0, b.Length) != 0)
                {
                    fs.Read(b, 0, b.Length);
                    int labelStringCount = BitConverter.ToInt32(b, 0);
                    fs.Read(b, 0, b.Length);
                    int labelnameLength = BitConverter.ToInt32(b, 0);
                    data = new byte[labelnameLength];
                    fs.Read(data, 0, data.Length);
                    string labelName = Encoding.ASCII.GetString(data);

                    CSFLabel label = new CSFLabel(labelName);

                    for (int i = 0; i < labelStringCount; i++)
                    {
                        fs.Read(b, 0, b.Length);
                        string stringID = Encoding.ASCII.GetString(b);
                        fs.Read(b, 0, b.Length);
                        int stringLength = BitConverter.ToInt32(b, 0);

                        if (stringLength > 0)
                        {
                            data = new byte[stringLength * 2];
                            fs.Read(data, 0, stringLength * 2);
                        }
                        else
                        {
                            data = new byte[0];
                        }

                        int dataExLen = 0;

                        if (stringID.Equals("WRTS", StringComparison.InvariantCultureIgnoreCase))
                        {
                            fs.Read(b, 0, b.Length);
                            dataExLen = BitConverter.ToInt32(b, 0);
                            dataEx = new byte[dataExLen];
                            fs.Read(dataEx, 0, dataExLen);
                        }
                        else
                        {
                            dataEx = new byte[0];
                        }

                        CSFString str = new CSFString(stringID, data, dataEx);
                        label.AddString(str);

                    }
                    labels.Add(label);
                }
            }
            catch (IOException e)
            {
                return e.Message;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            Altered = false;
            return null;
        }

        /// <summary>
        /// Attempts to save CSF file information to a file.
        /// </summary>
        /// <param name="filename">Filename to save CSF information to instead of currently set filename. Will be set as current filename upon successfully saving the file.</param>
        /// <returns>Error message if something went wrong, otherwise null.</returns>
        public string Save(string filename = null)
        {
            string filenameOutput;

            if (!string.IsNullOrEmpty(filename))
                filenameOutput = filename;
            else
                filenameOutput = Filename;

            FileStream fs = null;
            try
            {
                fs = new FileStream(filenameOutput, FileMode.Create);
                fs.Write(Encoding.ASCII.GetBytes(" FSC"), 0, 4);
                fs.Write(BitConverter.GetBytes(CSFFormatVersion), 0, 4);
                fs.Write(BitConverter.GetBytes(LabelCount), 0, 4);
                fs.Write(BitConverter.GetBytes(StringCount), 0, 4);
                fs.Write(new byte[4], 0, 4);
                fs.Write(BitConverter.GetBytes((int)Language), 0, 4);

                foreach (CSFLabel label in labels)
                {
                    fs.Write(Encoding.ASCII.GetBytes(" LBL"), 0, 4);
                    fs.Write(BitConverter.GetBytes(label.StringCount), 0, 4);
                    fs.Write(BitConverter.GetBytes(label.NameLength), 0, 4);
                    fs.Write(Encoding.ASCII.GetBytes(label.Name), 0, label.NameLength);

                    foreach (CSFString str in label.GetStrings())
                    {
                        fs.Write(Encoding.ASCII.GetBytes(str.ID), 0, str.ID.Length);
                        fs.Write(BitConverter.GetBytes(str.DataLength), 0, 4);

                        if (str.DataLength > 0)
                        {
                            byte[] data = str.GetEncodedStringData();
                            fs.Write(data, 0, data.Length);
                        }
                        if (str.ID == "WRTS" && str.ExtraDataLength > 0)
                        {
                            fs.Write(BitConverter.GetBytes(str.ExtraDataLength), 0, 4);
                            fs.Write(Encoding.ASCII.GetBytes(str.ExtraData), 0, str.ExtraDataLength);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            Altered = false;
            Filename = filenameOutput;
            return null;
        }

        /// <summary>
        /// Adds a string label to CSF file.
        /// </summary>
        /// <param name="labelName">Label name.</param>
        /// <param name="labelString">Label string data.</param>
        /// <returns>True if successfully added, otherwise false.</returns>
        public bool AddLabel(string labelName, string labelString)
        {
            if (string.IsNullOrEmpty(labelName) || labelString == null)
                return false;

            CSFLabel label = new CSFLabel(labelName);
            label.AddString(" RTS", labelString, null);
            labels.Add(label);
            Altered = true;
            return true;
        }

        /// <summary>
        /// Adds a string label with multiple strings to CSF file.
        /// </summary>
        /// <param name="labelName">Label name.</param>
        /// <param name="labelStrings">Collection of label string data.</param>
        /// <returns>True if successfully added, otherwise false.</returns>
        public bool AddLabel(string labelName, ICollection<string> labelStrings)
        {
            if (string.IsNullOrEmpty(labelName) || labelStrings == null || labelStrings.Count < 1)
                return false;

            CSFLabel label = new CSFLabel(labelName);

            foreach (string labelString in labelStrings)
            {
                label.AddString(" RTS", labelString, null);
            }

            labels.Add(label);
            Altered = true;
            return true;
        }

        /// <summary>
        /// Adds a string label with extra data to CSF file.
        /// </summary>
        /// <param name="labelName">Label name.</param>
        /// <param name="labelString">Label string data.</param>
        /// <param name="labelStringExtraData">Label string extra data.</param>
        /// <returns>True if successfully added, otherwise false.</returns>
        public bool AddLabel(string labelName, string labelString, string labelStringExtraData)
        {
            if (string.IsNullOrEmpty(labelName) || labelString == null || labelStringExtraData == null)
                return false;

            CSFLabel label = new CSFLabel(labelName);
            label.AddString("WRTS", labelString, labelStringExtraData);
            labels.Add(label);
            Altered = true;
            return true;
        }

        /// <summary>
        /// Adds a string label with multiple strings and extra data to CSF file.
        /// </summary>
        /// <param name="labelName">Label name.</param>
        /// <param name="labelStrings">Collection of label string data.</param>
        /// <param name="labelStringsExtraData">Collection of label string extra data.</param>
        /// <returns>True if successfully added, otherwise false.</returns>
        public bool AddLabel(string labelName, ICollection<string> labelStrings, ICollection<string> labelStringsExtraData)
        {
            if (string.IsNullOrEmpty(labelName) || labelStrings == null || labelStrings.Count < 1 ||
                labelStringsExtraData == null || labelStringsExtraData.Count < 1 || labelStrings.Count != labelStringsExtraData.Count)
                return false;

            CSFLabel label = new CSFLabel(labelName);

            var dataEnumerator = labelStrings.GetEnumerator();
            var extraDataEnumerator = labelStringsExtraData.GetEnumerator();
            bool enumeratingData = false;
            while (dataEnumerator.MoveNext())
            {
                if (!enumeratingData)
                {
                    extraDataEnumerator.MoveNext();
                    enumeratingData = true;
                }

                string currentData = dataEnumerator.Current;
                string currentExtraData = extraDataEnumerator.Current;
                label.AddString("WRTS", currentData, currentExtraData);
                extraDataEnumerator.MoveNext();
            }

            labels.Add(label);
            Altered = true;
            return true;
        }

        /// <summary>
        /// Gets string label string data based on label name.
        /// </summary>
        /// <param name="labelName">Name of the label.</param>
        /// <returns>Array containing string data of each string belonging to the string label if found, otherwise null.</returns>
        public string[] GetLabelStringData(string labelName)
        {
            CSFLabel label = labels.Find(x => x.Name.Equals(labelName, StringComparison.InvariantCultureIgnoreCase));

            if (label == null || label.StringCount < 1)
                return null;

            string[] stringData = new string[label.StringCount];

            for (int i = 0; i < stringData.Length; i++)
            {
                stringData[i] = label.GetString(i).Data;
            }

            return stringData;
        }

        /// <summary>
        /// Gets string label string data and string extra data based on label name.
        /// </summary>
        /// <param name="labelName">Name of the label.</param>
        /// <param name="stringData">Will be set to an array containing string data of each string belonging to the string label if found, otherwise null.</param>
        /// <param name="stringExtraData">Will be set to an array containing string extra data of each string belonging to the string label if found, otherwise null.</param>
        /// <returns>Returns true if label matching name was found, otherwise false.</returns>
        public bool GetLabelStringDataAndExtraData(string labelName, out string[] stringData, out string[] stringExtraData)
        {
            CSFLabel label = labels.Find(x => x.Name.Equals(labelName, StringComparison.InvariantCultureIgnoreCase));
            stringData = null;
            stringExtraData = null;

            if (label == null || label.StringCount < 1)
                return false;

            stringData = new string[label.StringCount];
            stringExtraData = new string[label.StringCount];

            for (int i = 0; i < stringData.Length; i++)
            {
                stringData[i] = label.GetString(i).Data;
                stringExtraData[i] = label.GetString(i).ExtraData;
            }

            return true;
        }

        /// <summary>
        /// Returns all labels in current CSF file.
        /// </summary>
        /// <returns>Array of pairs consisting of all labels and array of string data for all strings of the label.</returns>
        public Tuple<string, string[]>[] GetLabels()
        {
            Tuple<string, string[]>[] labelPairs = new Tuple<string, string[]>[LabelCount];

            for (int i = 0; i < LabelCount; i++)
            {
                CSFLabel label = labels[i];
                string[] data = new string[label.StringCount];

                for (int j = 0; j < data.Length; j++)
                {
                    data[j] = label.GetString(j).Data;
                }

                labelPairs[i] = new Tuple<string, string[]>(label.Name, data);
            }

            return labelPairs;
        }

        /// <summary>
        /// Returns all labels in current CSF file.
        /// </summary>
        /// <returns>Array of pairs consisting of all labels and arrays of string data & string extra data for all strings of the label.</returns>
        public Tuple<string, string[], string[]>[] GetLabelsWithExtraData()
        {
            Tuple<string, string[], string[]>[] labelPairs = new Tuple<string, string[], string[]>[LabelCount];

            for (int i = 0; i < LabelCount; i++)
            {
                CSFLabel label = labels[i];
                string[] data = new string[label.StringCount];
                string[] extraData = new string[label.StringCount];

                for (int j = 0; j < data.Length; j++)
                {
                    data[j] = label.GetString(j).Data;
                    extraData[j] = label.GetString(j).ExtraData;
                }

                labelPairs[i] = new Tuple<string, string[], string[]>(label.Name, data, extraData);
            }

            return labelPairs;
        }

        private int GetStringCount()
        {
            int stringCount = 0;

            foreach (CSFLabel label in labels)
            {
                stringCount += label.StringCount;
            }

            return stringCount;
        }
    }

    /// <summary>
    /// CSF string label.
    /// </summary>
    internal class CSFLabel
    {
        /// <summary>
        /// This CSF string label's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Length of this CSF string label's name.
        /// </summary>
        public int NameLength => Name.Length;

        /// <summary>
        /// Number of strings belonging to this string label.
        /// </summary>
        public int StringCount => strings.Count;

        private readonly List<CSFString> strings;

        /// <summary>
        /// Initializes a new CSF string label.
        /// </summary>
        /// <param name="name">Name of CSF string label.</param>
        /// <param name="stringCount"></param>
        public CSFLabel(string name)
        {
            Name = name;
            strings = new List<CSFString>();
        }

        /// <summary>
        /// Adds a new string to the CSF string label.
        /// </summary>
        /// <param name="str">CSF string to add to the string label.</param>
        public void AddString(CSFString str)
        {
            strings.Add(str);
        }

        /// <summary>
        /// Adds a new string to the CSF string label with encoded string data.
        /// </summary>
        /// <param name="ID">String ID of the added string.</param>
        /// <param name="stringData">Encoded string data for the string to add to the string label.</param>
        /// <param name="extraStringData">Encoded extra string data for the string to add to the string label.</param>
        public void AddString(string ID, byte[] stringData, byte[] extraStringData)
        {
            CSFString csfStr = new CSFString(ID, stringData, extraStringData);
            strings.Add(csfStr);
        }

        /// <summary>
        /// Adds a new string to the CSF string label with decoded string data.
        /// </summary>
        /// <param name="ID">String ID of the added string.</param>
        /// <param name="stringData">Decoded string data for the string to add to the string label.</param>
        /// <param name="extraStringData">Decoded extra string data for the string to add to the string label.</param>
        public void AddString(string ID, string stringData, string extraStringData)
        {
            CSFString csfStr = new CSFString(ID, stringData, extraStringData);
            strings.Add(csfStr);
        }

        /// <summary>
        /// Gets a string belonging to this string label, based on given string index value.
        /// </summary>
        /// <param name="index">String index value.</param>
        /// <returns>String at given index if valid, otherwise null.</returns>
        public CSFString GetString(int index)
        {
            if (strings == null || index > strings.Count - 1 || index < 0)
                return null;

            return strings[index];
        }

        /// <summary>
        /// Gets all strings belonging to this string label. 
        /// </summary>
        /// <returns>All strings belonging to this string label.</returns>
        public CSFString[] GetStrings()
        {
            return strings.ToArray();
        }
    }

    /// <summary>
    /// CSF string.
    /// </summary>
    internal class CSFString
    {
        /// <summary>
        /// String ID.
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// String data.
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// String data length.
        /// </summary>
        public int DataLength => Data != null ? Data.Length : 0;

        /// <summary>
        /// String extra data.
        /// </summary>
        public string ExtraData { get; private set; }

        /// <summary>
        /// String extra data length.
        /// </summary>
        public int ExtraDataLength => ExtraData != null ? ExtraData.Length : 0;

        /// <summary>
        /// Initializes a new CSF string with encoded string data.
        /// </summary>
        /// <param name="ID">String ID.</param>
        /// <param name="data">Encoded string data.</param>
        /// <param name="extraData">Encoded string extra data.</param>
        public CSFString(string ID, byte[] data, byte[] extraData)
        {
            this.ID = ID;
            Data = Decode(data);

            if (ID == "WRTS")
                ExtraData = Decode(extraData);
        }

        /// <summary>
        /// Initializes a new CSF string with decoded string data.
        /// </summary>
        /// <param name="ID">String ID.</param>
        /// <param name="data">Decoded string data.</param>
        /// <param name="extraData">Decoded string extra data.</param>
        public CSFString(string ID, string data, string extraData)
        {
            this.ID = ID;
            Data = data;

            if (ID == "WRTS")
                ExtraData = extraData;
        }

        /// <summary>
        /// Gets encoded string data of this CSF string.
        /// </summary>
        /// <returns>Encoded string data, or null if existing string data is not valid.</returns>
        public byte[] GetEncodedStringData()
        {
            if (Data == null || Data.Length < 1)
                return null;

            return Encode(Data);
        }

        /// <summary>
        /// Gets encoded string extra data of this CSF string.
        /// </summary>
        /// <returns>Encoded string extra data, or null if existing string extra data is not valid.</returns>
        public byte[] GetEncodedStringExtraData()
        {
            if (ExtraData == null || ExtraData.Length < 1)
                return null;

            return Encode(ExtraData);
        }

        /// <summary>
        /// Decode string data.
        /// </summary>
        /// <param name="data">Encoded string data.</param>
        /// <returns>Decoded string data.</returns>
        private string Decode(byte[] data)
        {
            if (data.Length == 0)
            {
                return string.Empty;
            }

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)~data[i];
            }

            return Encoding.Unicode.GetString(data);
        }

        /// <summary>
        /// Encode string data.
        /// </summary>
        /// <param name="data">Decoded string data.</param>
        /// <returns>Encoded string data.</returns>
        private byte[] Encode(string data)
        {
            if (data.Length == 0)
            {
                return null;
            }

            byte[] dataEncoded = Encoding.Unicode.GetBytes(data);

            for (int i = 0; i < dataEncoded.Length; i++)
            {
                dataEncoded[i] = (byte)~dataEncoded[i];
            }

            return dataEncoded;
        }
    }

    /// <summary>
    /// CSF file language.
    /// </summary>
    public enum CSFLanguage
    {
        LanguageIndependent = -1,
        EnglishUS = 0,
        EnglishUK = 1,
        German = 2,
        French = 3,
        Spanish = 4,
        Italian = 5,
        Japanese = 6,
        Jabberwockie = 7,
        Korean = 8,
        Chinese = 9,
        Unknown = 10
    }
}