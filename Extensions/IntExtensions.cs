using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Extensions
{
    public static class IntExtensions
    {
        public static long UnsignedRightShift(this long n, int s)  //Overloaded function where n is a long
        {
            if (n > 0)
            {
                return n >> s;
            }
            else
            {
                return (n >> s) + (((long)2) << ~s);
            }
        }

        public static int UnsignedRightShift(this int n, int s){
            if (n > 0)
            {
                return n >> s;
            }
            else
            {
                return (n >> s) + (2 << ~s);
            }
        }
    }
}
