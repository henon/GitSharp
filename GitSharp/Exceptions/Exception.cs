using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Exceptions
{
    public static class ExceptionExtensions
    {
        public static void printStackTrace(this Exception self)
        {
            Console.Error.WriteLine(self.FormatPretty());
        }

        public static string FormatPretty(this Exception exception)
        {
            if (exception == null)
                return "";
            m_string_builder = new StringBuilder();
            PrintRecursive(exception, "");
            return m_string_builder.ToString();
        }

        private static StringBuilder m_string_builder;

        private static void PrintRecursive(Exception exception, string indent)
        {
            string stars = new string('*', 80);
            m_string_builder.AppendLine(indent + stars);
            m_string_builder.AppendFormat(indent + "{0}: \"{1}\"\n", exception.GetType().Name, exception.Message);
            m_string_builder.AppendLine(indent + new string('-', 80));
            if (exception.InnerException != null)
            {
                m_string_builder.AppendLine(indent + "InnerException:");
                PrintRecursive(exception.InnerException, indent + "   ");
            }
            foreach (string line in exception.StackTrace.Split(new string[] { " at " }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrEmpty(line.Trim())) continue;
                string[] parts;
                parts = line.Trim().Split(new string[] { " in " }, StringSplitOptions.RemoveEmptyEntries);
                string class_info = parts[0];
                if (parts.Length == 2)
                {
                    parts = parts[1].Trim().Split(new string[] { "line" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string src_file = parts[0];
                        int line_nr = int.Parse(parts[1]);
                        m_string_builder.AppendFormat(indent + "  {0}({1},1):   {2}\n", src_file.TrimEnd(':'), line_nr, class_info);
                    }
                    else
                        m_string_builder.AppendLine(indent + "  " + class_info);
                }
                else
                    m_string_builder.AppendLine(indent + "  " + class_info);
            }
            m_string_builder.AppendLine(indent + stars);
        }

    }
}
