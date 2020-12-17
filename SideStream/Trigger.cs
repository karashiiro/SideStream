using System;
using System.Text.RegularExpressions;

namespace SideStream
{
    public class Trigger
    {
        public string Text { get; set; }
        public bool IsRegex { get; set; }

        public bool Match(string test)
        {
            if (!IsRegex) return test.Contains(Text);

            try
            {
                return Regex.Match(test, Text).Success;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}