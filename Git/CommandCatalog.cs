/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using GitSharp;

namespace GitSharp.CLI
{

    /// <summary>
    ///  List of all commands known by the command line tools.
    ///  Commands are implementations of the TextBuiltin class, with a required
    ///  command attribute to insert additional documentation and add some extra
    ///  information such as if the command is common and completed.
    ///  
    ///  Commands may be registered by adding them to the Commands.xml file.
    ///  The Commands.xml file should contain:
    ///      a. The command name including namespace.
    ///      b. The website address for command specific online help.(optional)
    /// </summary>
    public class CommandCatalog
    {
        /// <summary>
        /// Stores the command catalog.
        /// </summary>
        private SortedList<String, CommandRef> commands = new SortedList<string, CommandRef>();

        /// <summary>
        /// Creates the command catalog from the Commands.xml file.
        /// </summary>
        public CommandCatalog()
        {
            XmlDocument doc = new XmlDocument();
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase),"Commands.xml");
            doc.Load(path);

            XmlNodeList xmlNodeList = doc.SelectNodes("/root/CommandList/Command");
            foreach (XmlNode node in xmlNodeList)
            {
                XmlElement nameElement = node["Name"];
                XmlElement helpElement = node["Help"];
                if (nameElement != null)
                    Load(nameElement.InnerText, helpElement.InnerText);
            }
        }

        /// <summary>
        /// Returns all commands starting with a specified string, sorted by command name.
        /// </summary>
        public List<CommandRef> StartsWith(String s)
        {
            List<CommandRef> matches = new List<CommandRef>();
            foreach (CommandRef c in commands.Values)
            {
                if (c.getName().StartsWith(s))
                    matches.Add(c);
            }

            return toSortedArray(matches);
        }

        /// <summary>
        /// Create and loads the command name into the command catalog.
        /// </summary>
        /// <param name="commandName">Specifies the command name to load.</param>
        /// <param name="commandHelp">Specifies the command's website for faster reference.</param>
        public void Load(String commandName, String commandHelp)
        {
            TextBuiltin clazz;

            Type commandType = Type.GetType(commandName);
            if (commandType == null)
                return;

            clazz = Activator.CreateInstance(commandType) as TextBuiltin;
            if (clazz == null)
                return;

            int index = clazz.ToString().LastIndexOf(".");
            string cmdName = clazz.ToString().Substring(index + 1).ToLower();
            clazz.setCommandName(cmdName);
            clazz.setCommandHelp(commandHelp);

            CommandRef cr = new CommandRef(clazz);
            if (cr != null)
                commands.Add(cr.getName(), cr);
        }

        /// <summary>
        /// Locates a single command by its user friendly name.
        /// </summary>
        /// <param name="name">Specifies the name of the command.</param>
        /// <returns>Returns the CommandRef containing the command's information.</returns>
        public CommandRef Get(String name)
        {
            CommandRef value = null;
            commands.TryGetValue(name, out value);
            return value;
        }

        /// <summary>
        /// Returns all known commands, sorted by command name.
        /// </summary>
        public IList<CommandRef> All()
        {
            return commands.Values;
        }

        /// <summary>
        /// Returns all common commands, sorted by command name.
        /// </summary>
        public List<CommandRef> Common()
        {
            List<CommandRef> common = new List<CommandRef>();
            foreach (CommandRef c in commands.Values)
            {
                if (c.isCommon())
                    common.Add(c);
            }

            return toSortedArray(common);
        }

        /// <summary>
        /// Returns all incomplete commands, sorted by command name.
        /// </summary>
        public List<CommandRef> Incomplete()
        {
            List<CommandRef> incomplete = new List<CommandRef>();
            foreach (CommandRef c in commands.Values)
            {
                if (!c.isComplete())
                    incomplete.Add(c);
            }

            return toSortedArray(incomplete);
        }
        
        /// <summary>
        /// Returns all complete commands, sorted by command name.
        /// </summary>
        public List<CommandRef> Complete()
        {
            List<CommandRef> complete = new List<CommandRef>();
            foreach (CommandRef c in commands.Values)
            {
                if (c.isComplete())
                    complete.Add(c);
            }

            return toSortedArray(complete);
        }

        /// <summary>
        /// Sorts a list of specified commands by command name.
        /// </summary>
        /// <param name="c">Specifies the list of commands to be sorted.</param>
        /// <returns>Returns the sorted list of commands.</returns>
        private List<CommandRef> toSortedArray(List<CommandRef> c)
        {
            c.Sort(
                delegate(CommandRef ref1, CommandRef ref2)
                {
                    return ref1.getName().CompareTo(ref2.getName());
                }
            );
            return c;
        }
    }
}
