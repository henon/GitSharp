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
using System.Text;


namespace GitSharp.CLI
{

    /// <summary>
    /// Description of a command subcommand.
    ///
    /// These descriptions are lightweight compared to creating a command instance
    /// and are therefore suitable for catalogs of "known" commands without linking
    /// the command's implementation and creating a dummy instance of the command.
    /// </summary>
    public class CommandRef
    {
        private TextBuiltin impl;
        private String name;
        private String usage;
        private String cmdHelp;
        bool complete;
        bool common;

        public CommandRef(TextBuiltin clazz)
            : this(clazz, clazz.getCommandName())
        {

        }

        public CommandRef(TextBuiltin clazz, String cn) 
        {
            impl = (TextBuiltin)clazz;
            name = cn;
            Command cmd = impl.GetCommand();
            if (cmd != null)
            {
                common = cmd.common;
                complete = cmd.complete;
                usage = cmd.usage;
                cmdHelp = clazz.getCommandHelp();
            }
        }

        /// <summary>
        /// Returns the friendly command name. The command as invoked from the command line.
        /// </summary>
        public String getName()
        {
            return name;
        }

        /// <summary>
        /// Returns a one line description of the command's feature set.
        /// </summary>
        public String getUsage()
        {
            return usage;
        }
         
        /// <summary>
        /// Returns true if this command is considered to be commonly used.
        /// </summary>
        /// <returns></returns>
        public bool isCommon()
        {
            return common;
        }

        /// <summary>
        /// Returns true if this command is considered to be completed.
        /// </summary>
        /// <returns></returns>
        public bool isComplete()
        {
            return complete;
        }

        
        /// <summary>
        /// Returns the name of the class which implements this command.
        /// </summary>
        /// <returns></returns>
        public String getImplementationClassName()
        {
            return impl.ToString();
        }

        /// <summary>
        /// Returns a new instance of the command implementation.
        /// </summary>
        /// <returns></returns>
        public TextBuiltin Create()
        {
            TextBuiltin c = Activator.CreateInstance(Type.GetType(impl.ToString())) as TextBuiltin;
            if (c != null)
            {
                c.setCommandHelp(cmdHelp);
                return c;
            }
            else
            {
                return null;
            }
        }
    }
}