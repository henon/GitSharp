/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.Text;
using System.Xml;
using GitSharp;

/**
 * List of all commands known by jgit's command line tools.
 * <p>
 * Commands are implementations of {@link TextBuiltin}, with an optional
 * {@link Command} class annotation to insert additional documentation or
 * override the default command name (which is guessed from the class name).
 * <p>
 * Commands may be registered by adding them to a services file in the same JAR
 * (or classes directory) as the command implementation. The service file name
 * is <code>META-INF/services/org.spearce.jgit.pgm.TextBuiltin</code> and it
 * contains one concrete implementation class name per line.
 * <p>
 * Command registration is identical to Java 6's services, however the catalog
 * uses a lightweight wrapper to delay creating a command instance as much as
 * possible. This avoids initializing the AWT or SWT GUI toolkits even if the
 * command's constructor might require them.
 */

namespace GitSharp.CLI
{

    public class CommandCatalog
    {

        /**
         * Locate a single command by its user friendly name.
         *
         * @param name
         *            name of the command. Typically in dash-lower-case-form, which
         *            was derived from the DashLowerCaseForm class name.
         * @return the command instance; null if no command exists by that name.
         */

        public CommandRef Get(String name)
        {
            CommandRef value = null;
            commands.TryGetValue(name, out value);
            return value;
        }

        /**
         * @return all known commands, sorted by command name.
         */
        public IList<CommandRef> All()
        {
            return commands.Values;
        }

        /**
         * @return all common commands, sorted by command name.
         */
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

        private SortedList<String, CommandRef> commands = new SortedList<string, CommandRef>();

        public CommandCatalog()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Commands.xml");

            XmlNodeList xmlNodeList = doc.SelectNodes("/root/CommandList/Command");
            foreach (XmlNode node in xmlNodeList)
            {
                XmlElement nameElement = node["Name"];
                XmlElement helpElement = node["Help"];
                if (nameElement != null)
                    load(nameElement.InnerText, helpElement.InnerText);
            }
        }

        public void load(String commandName, String commandHelp)
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
    }
}
