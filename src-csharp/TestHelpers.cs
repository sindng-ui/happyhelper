using System;
using System.Collections.Generic;

namespace HappyHelper
{
    internal static class TestKeyMap
    {
        private static readonly Dictionary<int, string> _map = new Dictionary<int, string>
        {
            { 1, "ESC" }, { 2, "1" }, { 3, "2" }, { 4, "3" }, { 5, "4" }, { 6, "5" },
            { 1001, "좌클릭 (L-Click)" }, { 1002, "우클릭 (R-Click)" }, { 1003, "휠클릭 (M-Click)" }, { 1004, "마우스4 (X1)" }, { 1005, "마우스5 (X2)" },
            { 2001, "Pad A" }, { 2002, "Pad B" }, { 2003, "Pad X" }, { 2004, "Pad Y" },
            { 2005, "Pad LB" }, { 2006, "Pad RB" }, { 2007, "Pad LT" }, { 2008, "Pad RT" },
            { 2009, "Pad D-Up" }, { 2010, "Pad D-Down" }, { 2011, "Pad D-Left" }, { 2012, "Pad D-Right" },
            { 2013, "Pad L3 (LS)" }, { 2014, "Pad R3 (RS)" }, { 2015, "Pad View (Back)" }, { 2016, "Pad Menu (Start)" }
        };

        public static string GetKeyLabel(int keyCode)
        {
            string val;
            if (_map.TryGetValue(keyCode, out val)) return val;
            return "Key(" + keyCode + ")";
        }
    }
}
