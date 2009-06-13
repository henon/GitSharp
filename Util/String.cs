using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Core.Util
{
    public static class StringExtension
    {
        // this is a helper to easily replace all occurences of the incompatible String.Substring method in ported java code
        public static string Slice(this string longstring, int index1, int index2) {
            return longstring.Substring(index1, index2 - index1);
        }
    }
}
