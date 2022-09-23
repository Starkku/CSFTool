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
using System.Linq;
using NDesk.Options;
using Starkku.Utilities;

namespace CSFTool
{
    class Program
    {
        private static OptionSet options;
        private static Settings settings = new Settings();
        private const string labelSeparator = "|";
        private const string newLineString = @"\n";

        static void Main(string[] args)
        {
            options = new OptionSet
            {
                { "h|?|help", "Show help.", v => settings.ShowHelp = true},
                { "i|infile=", "Input string table file.", v => settings.FilenameInput = v},
                { "o|outfile=", "Output string table file.", v => settings.FilenameOutput = v},
                { "t|textfile=", "Input/output text file name. If not specified, defaults to name of input string table with extension .txt.", v => settings.FilenameText = v},
                { "a|addlines", "Add lines from text file to string table as strings. Settings this sets -e to false.", v => settings.AddStrings = true},
                { "e|exportlines", "Export strings from string table to lines in a text file.", v => settings.ExportStrings = true},
                { "l=|language-override", "Set to an integer to override saved string table language ID. Valid values range from 0 to 9 and special value of -1 (language independent).", (int v) => settings.LanguageIDOverride = v},
                { "d|debug-logging", "If set, writes a log to a file in program directory.", v => settings.EnableDebugLogging = true}
            };
            try
            {
                options.Parse(args);
            }
            catch (Exception e)
            {
                ConsoleColor defcolor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Encountered an error while parsing command-line parameters. Message: " + e.Message);
                Console.ForegroundColor = defcolor;
                ShowHelp();
                return;
            }

            Logger.Initialize(settings.EnableDebugLogging);

            bool error = false;
            bool createNew = false;

            if (settings.ShowHelp)
            {
                ShowHelp();
                return;
            }

            if (!settings.AddStrings && !settings.ExportStrings)
            {
                Logger.Error("Not enough parameters. Must provide either -a or -e.");
                ShowHelp();
                return;
            }

            if (string.IsNullOrEmpty(settings.FilenameInput))
            {
                if (settings.AddStrings)
                {
                    Logger.Info("No valid input file specified. Creating new string table.");
                    createNew = true;
                }
                else
                {
                    Logger.Error("No valid input file specified.");
                    ShowHelp();
                    error = true;
                }
            }
            else if (!string.IsNullOrEmpty(settings.FilenameInput) && !File.Exists(settings.FilenameInput))
            {
                Logger.Error("Specified input string table does not exist.");
                ShowHelp();
                error = true;
            }

            if (error)
                return;

            if (!createNew)
                Logger.Info("Input string table path OK.");

            if (settings.AddStrings)
            {
                Logger.Info("Mode set (-a): Add strings to string table.");
                settings.ExportStrings = false;
            }
            else
            {
                Logger.Info("Mode set (-e): Export strings from string table.");
            }

            if (settings.AddStrings)
            {
                if (string.IsNullOrEmpty(settings.FilenameOutput))
                {
                    if (!createNew)
                    {
                        Logger.Warn("No output file path available. Using input file as output.");
                        settings.FilenameOutput = settings.FilenameInput;
                    }
                    else
                    {
                        Logger.Error("No output file path available.");
                        ShowHelp();
                        error = true;
                    }
                }
                else
                {
                    Logger.Info("Output file path OK.");
                }
            }

            if (settings.AddStrings)
            {
                if (!File.Exists(settings.FilenameText))
                {
                    string deftext = Path.ChangeExtension(settings.FilenameInput, ".txt");
                    Logger.Warn("Specified input text file does not exist - looking for default text file '" + deftext + "'.");
                    if (File.Exists(deftext))
                    {
                        Logger.Info("Text file '" + deftext + "' exists - using as input text file.");
                        settings.FilenameText = deftext;
                    }
                    else
                    {
                        Logger.Error("Input text file does not exist.");
                        ShowHelp();
                        return;
                    }
                }
                else
                {
                    Logger.Info("Input text file path OK.");
                }
            }

            if (string.IsNullOrEmpty(settings.FilenameText) && settings.ExportStrings)
            {
                string deftext = Path.ChangeExtension(settings.FilenameInput, ".txt");
                Logger.Warn("Output text file path invalid - using default output text file path '" + deftext + "'.");
                settings.FilenameText = deftext;
            }
            else if (!string.IsNullOrEmpty(settings.FilenameText) && settings.ExportStrings)
            {
                Logger.Info("Output text file path OK.");
            }

            CSFFile csfFile = new CSFFile(settings.FilenameInput);

            if (!createNew)
            {
                string errorMsg = csfFile.Load();

                if (errorMsg != null)
                {
                    Logger.Error("Failed to load string table file! Error message: " + errorMsg);
                    return;
                }
            }

            if (settings.AddStrings)
                AddStrings(csfFile);
            else if (settings.ExportStrings)
                ExportStrings(csfFile);

            if (csfFile.Altered)
            {
                if (settings.LanguageIDOverride != int.MinValue && settings.LanguageIDOverride < 10 && settings.LanguageIDOverride >= -1)
                    csfFile.Language = (CSFLanguage)settings.LanguageIDOverride;

                string errorMsg = csfFile.Save(settings.FilenameOutput);

                if (errorMsg != null)
                {
                    Logger.Error("Failed to save string table file! Error message: " + errorMsg);
                    return;
                }
            }
        }

        private static void AddStrings(CSFFile csfFile)
        {
            if (!File.Exists(settings.FilenameText))
            {
                Logger.Error("Text file '" + settings.FilenameText + "' does not exist! Cannot add strings to the string table.");
                return;
            }

            string[] lines;

            try
            {
                lines = File.ReadAllLines(settings.FilenameText);
            }
            catch (Exception e)
            {
                Logger.Error("Error encountered while parsing text file'" + settings.FilenameText + "'. Error message: " + e.Message);
                return;
            }

            foreach (string line in lines)
            {
                ProcessTextLine(csfFile, line);
            }
        }

        private static void ProcessTextLine(CSFFile csfFile, string line)
        {

            int idx = line.IndexOf(labelSeparator);

            if (idx < 1 || string.IsNullOrEmpty(line) || line.StartsWith(";") || (line.IndexOf('*') == 0 && idx == 1))
            {
                return;
            }

            if (line.IndexOf('*') == 0 && idx == 1)
            {
                return;
            }
            
            line = line.Replace(newLineString, "\n");
            string label = line.Substring(0, idx);
            string str = line.Substring(idx + labelSeparator.Length, line.Length - label.Length - labelSeparator.Length);
            csfFile.AddLabel(label, str);
        }

        private static void ExportStrings(CSFFile csfFile)
        {
            var labels = csfFile.GetLabels();
            Array.Sort(labels, labels, 0, 0, new LabelNameComparer());
            string[] lines = labels.Select(x => x.Item1.ToUpper() + labelSeparator + (x.Item2 != null && x.Item2.Length > 0 ? x.Item2[0].Replace("\n", newLineString).Replace("\r\n", newLineString) : string.Empty)).ToArray();
            
            try
            {
                File.WriteAllLines(settings.FilenameText, lines);
            }
            catch (Exception e)
            {
                Logger.Error("Error encountered while writing text file'" + settings.FilenameText + "'. Error message: " + e.Message);
                return;
            }
        }

        private static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Usage: ");
            Console.WriteLine("");
            var sb = new System.Text.StringBuilder();
            var sw = new StringWriter(sb);
            options.WriteOptionDescriptions(sw);
            Console.WriteLine(sb.ToString());
        }
    }
}
