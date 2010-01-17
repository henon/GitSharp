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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitSharp.Core.Diff;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Diff
{
    /*
     * Test cases for the performance of the diff implementation. The tests test
     * that the performance of the MyersDiff algorithm is really O(N*D). Means the
     * time for computing the diff between a and b should depend on the product of
     * a.length+b.length and the number of found differences. The tests compute
     * diffs between chunks of different length, measure the needed time and check
     * that time/(N*D) does not differ more than a certain factor (currently 10)
     */
    [TestFixture]
    public class MyersDiffPerformanceTest  {
        private static long longTaskBoundary = 5000000000L;

        private static int minCPUTimerTicks = 10;

        private static int maxFactor = 15;

        private CPUTimeStopWatch stopwatch=CPUTimeStopWatch.createInstance();

        public class PerfData : IComparable<PerfData>
        {
            private Func<double, string> fmt = d => d.ToString("#.##E0");

            public long runningTime;

            public long D;

            public long N;

            private double p1 = -1;

            private double p2 = -1;

            public double perf1() {
                if (p1 < 0)
                    p1 = runningTime / ((double) N * D);
                return p1;
            }

            public double perf2() {
                if (p2 < 0)
                    p2 = runningTime / ((double) N * D * D);
                return p2;
            }

            public string toString() {
                return ("diffing " + N / 2 + " bytes took " + runningTime
                        + " ns. N=" + N + ", D=" + D + ", time/(N*D):"
                        + fmt(perf1()) + ", time/(N*D^2):" + fmt
                                                                 (perf2()));
            }

            public int CompareTo(PerfData o2)
            {
                int _whichPerf = 1;
                PerfData o1 = this;

                double p1 = (_whichPerf == 1) ? o1.perf1() : o1.perf2();
                double p2 = (_whichPerf == 1) ? o2.perf1() : o2.perf2();
                return (p1 < p2) ? -1 : (p1 > p2) ? 1 : 0;
            }
        }

        [Test]
        public void test() {
            if (stopwatch!=null) {
                var perfData = new List<PerfData>();
                perfData.Add(test(10000));
                perfData.Add(test(20000));
                perfData.Add(test(50000));
                perfData.Add(test(80000));
                perfData.Add(test(99999));
                perfData.Add(test(999999));

                double factor = perfData.Max().perf1()
                                / perfData.Min().perf1();
                Assert.IsTrue(factor < maxFactor, 
                              "minimun and maximum of performance-index t/(N*D) differed too much. Measured factor of "
                              + factor
                              + " (maxFactor="
                              + maxFactor
                              + "). Perfdata=<" + perfData.ToString() + ">");
            }
        }

        /**
	 * Tests the performance of MyersDiff for texts which are similar (not
	 * random data). The CPU time is measured and returned. Because of bad
	 * accuracy of CPU time information the diffs are repeated. During each
	 * repetition the interim CPU time is checked. The diff operation is
	 * repeated until we have seen the CPU time clock changed its value at least
	 * {@link #minCPUTimerTicks} times.
	 *
	 * @param characters
	 *            the size of the diffed character sequences.
	 * @return performance data
	 */
        private PerfData test(int characters) {
            PerfData ret = new PerfData();
            string a = DiffTestDataGenerator.generateSequence(characters, 971, 3);
            string b = DiffTestDataGenerator.generateSequence(characters, 1621, 5);
            CharArray ac = new CharArray(a);
            CharArray bc = new CharArray(b);
            MyersDiff myersDiff = null;
            int cpuTimeChanges = 0;
            long lastReadout = 0;
            long interimTime = 0;
            int repetitions = 0;
            stopwatch.start();
            while (cpuTimeChanges < minCPUTimerTicks && interimTime < longTaskBoundary) {
                myersDiff = new MyersDiff(ac, bc);
                repetitions++;
                interimTime = stopwatch.readout();
                if (interimTime != lastReadout) {
                    cpuTimeChanges++;
                    lastReadout = interimTime;
                }
            }
            ret.runningTime = stopwatch.stop() / repetitions;
            ret.N = (ac.size() + bc.size());
            ret.D = myersDiff.getEdits().size();

            return ret;
        }

        private class CharArray : Sequence {
            private char[] array;

            public CharArray(string s) {
                array = s.ToCharArray();
            }

            public int size() {
                return array.Length;
            }

            public bool equals(int i, Sequence other, int j) {
                CharArray o = (CharArray) other;
                return array[i] == o.array[j];
            }
        }
    }
}