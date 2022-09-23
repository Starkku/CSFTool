/*
 * Copyright 2017-2022 by Starkku
 * This file is part of CSFTool, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see LICENSE.txt.
 */

using System;
using System.Collections.Generic;

namespace CSFTool
{
    internal class LabelNameComparer : IComparer<Tuple<string, string[]>>
    {
        int IComparer<Tuple<string, string[]>>.Compare(Tuple<string, string[]> x, Tuple<string, string[]> y)
        {
            return x.Item1.CompareTo(y.Item1);
        }
    }
}