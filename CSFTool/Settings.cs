/*
 * Copyright 2017-2022 by Starkku
 * This file is part of CSFTool, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see LICENSE.txt.
 */

namespace CSFTool
{
    class Settings
    {
        public bool ShowHelp { get; set; }

        public string FilenameInput { get; set; }

        public string FilenameOutput { get; set; }

        public string FilenameText { get; set; }

        public bool AddStrings { get; set; }

        public bool ExportStrings { get; set; }

        public bool EnableDebugLogging { get; set; }

        public int LanguageIDOverride { get; set; } = int.MinValue;
    }
}
