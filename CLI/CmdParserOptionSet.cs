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
using System.Collections.Generic; 
using NDesk.Options;

namespace GitSharp.CLI
{
    public class CmdParserOptionSet : OptionSet
    {
        protected override void InsertItem(int index, Option item) 
        {
            if (item.Prototype.ToLower () != item.Prototype)
                throw new ArgumentException ("prototypes must be lower-case!");
            base.InsertItem (index, item);     
        }     
        
        protected override OptionContext CreateOptionContext()
        {         
            return new OptionContext (this);     
        }     
        
        protected override bool Parse(string option, OptionContext c)     
        {         
            string f, n, s, v;         
            bool haveParts = GetOptionParts (option, out f, out n, out s, out v);         
            Option nextOption = null;         
            string newOption  = option;   
      
            if (haveParts) 
            {             
                string nl = n.ToLower ();             
                nextOption = Contains (nl) ? this [nl] : null;             
                newOption = f + n.ToLower () + (v != null ? s + v : "");         
            }   
      
            if (c.Option != null) 
            {             
                // Prevent --a --b             
                if (c.Option != null && haveParts) 
                {                 
                    throw new OptionException (                         
                        string.Format ("Found option `{0}' as value for option `{1}'.",                             
                        option, c.OptionName), c.OptionName);             
                }             

                // have a option w/ required value; try to concat values.             
                if (AppendValue (option, c)) 
                {                 
                    if (!option.EndsWith ("\\") &&                          
                        c.Option.MaxValueCount == c.OptionValues.Count) 
                    {                     
                        c.Option.Invoke (c);                 
                    }                 
                    
                    return true;             
                }             
                else                 
                    base.Parse (newOption, c);         
            }         
            if (!haveParts || v == null) 
            {             
                // Not an option; let base handle as a non-option argument.             
                return base.Parse (newOption, c);         
            }         
            if (nextOption.OptionValueType != OptionValueType.None &&                  
                v.EndsWith ("\\")) 
            {             
                c.Option = nextOption;             
                c.OptionValues.Add (v);             
                c.OptionName = f + n;             
                return true;         
            }         
            return base.Parse (newOption, c);     
        }     
        
        private bool AppendValue(string value, OptionContext c)     
        {         
            bool added = false;         
            string[] seps = c.Option.GetValueSeparators ();
            foreach (var o in seps.Length != 0 ? value.Split(seps, StringSplitOptions.None) : new string[] { value })
            {
                int idx = c.OptionValues.Count - 1;
                if (idx == -1 || !c.OptionValues[idx].EndsWith("\\"))
                {
                    c.OptionValues.Add(o);
                    added = true;
                }
                else
                {
                    c.OptionValues[idx] += value;
                    added = true;
                }
            }         
            return added;     
        } 
    }
}
