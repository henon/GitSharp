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
using System.Text;

/**
 * Description of a command (a {@link TextBuiltin} subclass.
 * <p>
 * These descriptions are lightweight compared to creating a command instance
 * and are therefore suitable for catalogs of "known" commands without linking
 * the command's implementation and creating a dummy instance of the command.
 */
namespace GitSharp.CLI
{
    public class CommandRef
    {
        private TextBuiltin impl;
        private String name;
        private String usage;
        private String cmdHelp;
        bool complete;
        bool common;

        public CommandRef(TextBuiltin clazz)
            : this(clazz, clazz.getCommandName()) //guessName(clazz))
        {

        }
#if ported
        /*public CommandRef(TextBuiltin clazz, Command cmd) //final Class<? extends TextBuiltin> clazz, final Command cmd) {
            : this(clazz, cmd.Name().Length > 0 ? cmd.Name() : guessName(clazz))
        {
            usage = cmd.Usage();
            common = cmd.Common();
        }*/
#endif
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

#if ported
        private static String guessName(TextBuiltin clazz)
        {
            StringBuilder s = new StringBuilder();
            //if (clazz.GetName().startsWith("org.spearce.jgit.pgm.debug."))
            //		s.Append("debug-");

            bool lastWasDash = true;
            foreach (Char c in clazz.GetType().ToString())
            {
                if (Char.IsUpper(c))
                {
                    if (!lastWasDash)
                        s.Append('-');
                    lastWasDash = !lastWasDash;
                    s.Append(Char.ToLower(c));
                }
                else
                {
                    s.Append(c);
                }
            }
            return s.ToString();
        }
#endif
        /**
         * @return name the command is invoked as from the command line.
         */
        public String getName()
        {
            return name;
        }

        /**
         * @return one line description of the command's feature set.
         */
        public String getUsage()
        {
            return usage;
        }

        /**
         * @return true if this command is considered to be commonly used.
         */
        public bool isCommon()
        {
            return common;
        }

        public bool isComplete()
        {
            return complete;
        }

        /**
         * @return name of the Java class which implements this command.
         */
        public String getImplementationClassName()
        {
            return impl.ToString();
        }

        /**
         * @return loader for {@link #getImplementationClassName()}.
         */
        //public ClassLoader getImplementationClassLoader() {
        //	return impl.getClassLoader();
        //}

        /**
         * @return a new instance of the command implementation.
         */
        public TextBuiltin create()
        {
            TextBuiltin c;

            try
            {
                c = (TextBuiltin)Activator.CreateInstance(Type.GetType(impl.ToString()));
                c.setCommandHelp(cmdHelp);
            }
            catch (Exception)
            {

                return null;
            }

            return c;
        }
    }
}