/*
 * Copyright (C) 2009, Christian Halstrick <christian.halstrick@sap.com>
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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

using System.Diagnostics;

/*
 * A simple stopwatch which measures elapsed CPU time of the current thread. CPU
 * time is the time spent on executing your own code plus the time spent on
 * executing operating system calls triggered by your application.
 * <p>
 * This stopwatch needs a VM which supports getting CPU Time information for the
 * current thread. The static method createInstance() will take care to return
 * only a new instance of this class if the VM is capable of returning CPU time.
 */
public class CPUTimeStopWatch {
    private Stopwatch _stopWatch;
	/**
	 * use this method instead of the constructor to be sure that the underlying
	 * VM provides all features needed by this class.
	 *
	 * @return a new instance of {@link #CPUTimeStopWatch()} or
	 *         <code>null</code> if the VM does not support getting CPU time
	 *         information
	 */
	public static CPUTimeStopWatch createInstance() {
	    return new CPUTimeStopWatch();
	}

	/**
	 * Starts the stopwatch. If the stopwatch is already started this will
	 * restart the stopwatch.
	 */
	public void start() {
        _stopWatch = new Stopwatch();
        _stopWatch.Start();
	}

	/**
	 * Stops the stopwatch and return the elapsed CPU time in nanoseconds.
	 * Should be called only on started stopwatches.
	 *
	 * @return the elapsed CPU time in nanoseconds. When called on non-started
	 *         stopwatches (either because {@link #start()} was never called or
	 *         {@link #stop()} was called after the last call to
	 *         {@link #start()}) this method will return 0.
	 */
	public long stop() {
        _stopWatch.Stop();
	    return _stopWatch.ElapsedTicks;
    }

	/**
	 * Return the elapsed CPU time in nanoseconds. In contrast to
	 * {@link #stop()} the stopwatch will continue to run after this call.
	 *
	 * @return the elapsed CPU time in nanoseconds. When called on non-started
	 *         stopwatches (either because {@link #start()} was never called or
	 *         {@link #stop()} was called after the last call to
	 *         {@link #start()}) this method will return 0.
	 */
	public long readout() {
		return (!_stopWatch.IsRunning) ? 0 : _stopWatch.ElapsedTicks;
	}
}
