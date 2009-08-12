/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Dan Rigby <dan@danrigby.com>
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
using System.Linq;
using System.Runtime.CompilerServices;

namespace GitSharp.Merge
{
    /**
     * A method of combining two or more trees together to form an output tree.
     * <p>
     * Different strategies may employ different techniques for deciding which paths
     * (and ObjectIds) to carry from the input trees into the final output tree.
     */
    public abstract class MergeStrategy
    {
        /** Simple strategy that sets the output tree to the first input tree. */
	    public static readonly MergeStrategy OURS = new StrategyOneSided("ours", 0);

	    /** Simple strategy that sets the output tree to the second input tree. */
	    public static readonly MergeStrategy THEIRS = new StrategyOneSided("theirs", 1);

	    /** Simple strategy to merge paths, without simultaneous edits. */
	    public static readonly ThreeWayMergeStrategy SIMPLE_TWO_WAY_IN_CORE = new StrategySimpleTwoWayInCore();

        private static readonly Dictionary<String, MergeStrategy> STRATEGIES = new Dictionary<String, MergeStrategy>();

	    static MergeStrategy()
        {
		    register(OURS);
		    register(THEIRS);
		    register(SIMPLE_TWO_WAY_IN_CORE);
	    }

	    /**
	     * Register a merge strategy so it can later be obtained by name.
	     *
	     * @param imp
	     *            the strategy to register.
	     * @throws IllegalArgumentException
	     *             a strategy by the same name has already been registered.
	     */
	    public static void register(MergeStrategy imp) {
		    register(imp.getName(), imp);
	    }

	    /**
	     * Register a merge strategy so it can later be obtained by name.
	     *
	     * @param name
	     *            name the strategy can be looked up under.
	     * @param imp
	     *            the strategy to register.
	     * @throws IllegalArgumentException
	     *             a strategy by the same name has already been registered.
	     */
        [MethodImpl(MethodImplOptions.Synchronized)]
	    public static void register(String name, MergeStrategy imp) 
        {
		    if (STRATEGIES.ContainsKey(name))
			    throw new ArgumentException("Merge strategy \"" + name
					    + "\" already exists as a default strategy");
		    STRATEGIES.Add(name, imp);
	    }

	    /**
	     * Locate a strategy by name.
	     *
	     * @param name
	     *            name of the strategy to locate.
	     * @return the strategy instance; null if no strategy matches the name.
	     */
        [MethodImpl(MethodImplOptions.Synchronized)]
	    public static MergeStrategy get(String name) 
        {
		    return STRATEGIES[name];
	    }

	    /**
	     * Get all registered strategies.
	     *
	     * @return the registered strategy instances. No inherit order is returned;
	     *         the caller may modify (and/or sort) the returned array if
	     *         necessary to obtain a reasonable ordering.
	     */
        [MethodImpl(MethodImplOptions.Synchronized)]
	    public static MergeStrategy[] get() 
        {
		    MergeStrategy[] r = new MergeStrategy[STRATEGIES.Count];
		    r = STRATEGIES.Values.ToArray();
		    return r;
	    }

	    /** @return default name of this strategy implementation. */
	    public abstract String getName();

	    /**
	     * Create a new merge instance.
	     *
	     * @param db
	     *            repository database the merger will read from, and eventually
	     *            write results back to.
	     * @return the new merge instance which implements this strategy.
	     */
	    public abstract Merger newMerger(Repository db);
    }
}