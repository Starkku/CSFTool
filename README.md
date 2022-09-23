# CSFTool

Command line tool for converting text files to string table files used by Westwood Studios' Command & Conquer games, and vice-versa.

Download for latest build (automatically generated from latest commit in `master` branch) can be found [here](https://github.com/Starkku/CSFTool/releases/tag/latest).

Accepted parameters:
```
-h, -?, --help                  Show help.
-i, --infile=VALUE              Input string table file.
-o, --outfile=VALUE             Output string table file.
-t, --textfile=VALUE            Input/output text file name. If not specified, defaults to name of input string table with extension .txt.
-a, --addlines                  Add lines from text file to string table as strings. Settings this sets -e to false.
-e, --exportlines               Export strings from string table to lines in a text file.
-l, --language-override=VALUE   Set to an integer to override saved string table language ID. Valid values range from 0 to 9 and special value of -1 (language independent).
-d, --debug-logging             If set, writes a log to a file in program directory.
```

The text files to convert into string table should contain string labels and values in format `LABEL|VALUE`, each on their own line. Characters `\n` together get converted into a newline.

## Acknowledgements

CSFTool uses code from the following open-source projects to make its functionality possible.

* Starkku.Utilities: https://github.com/Starkku/Starkku.Utilities
* NDesk.Options: http://www.ndesk.org/Options

## License

This program is licensed under GPL Version 3 or any later version.

See [LICENSE.txt](LICENSE.txt) for more information.
