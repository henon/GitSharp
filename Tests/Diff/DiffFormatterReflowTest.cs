/*
 * Copyright (C) 2009, Google Inc.
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


using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]

    public class DiffFormatterReflowTest
    {
#if false
	private RawText a;

	private RawText b;

	private FileHeader file;

	private ByteArrayOutputStream out;

	private DiffFormatter fmt;

	protected void setUp() throws Exception {
		super.setUp();
		out = new ByteArrayOutputStream();
		fmt = new DiffFormatter();
	}

	public void testNegativeContextFails() throws IOException {
		init("X");
		try {
			fmt.setContext(-1);
			fail("accepted negative context");
		} catch (IllegalArgumentException e) {
			// pass
		}
	}

	public void testContext0() throws IOException {
		init("X");
		fmt.setContext(0);
		assertFormatted();
	}

	public void testContext1() throws IOException {
		init("X");
		fmt.setContext(1);
		assertFormatted();
	}

	public void testContext3() throws IOException {
		init("X");
		fmt.setContext(3);
		assertFormatted();
	}

	public void testContext5() throws IOException {
		init("X");
		fmt.setContext(5);
		assertFormatted();
	}

	public void testContext10() throws IOException {
		init("X");
		fmt.setContext(10);
		assertFormatted();
	}

	public void testContext100() throws IOException {
		init("X");
		fmt.setContext(100);
		assertFormatted();
	}

	public void testEmpty1() throws IOException {
		init("E");
		assertFormatted("E.patch");
	}

	public void testNoNewLine1() throws IOException {
		init("Y");
		assertFormatted("Y.patch");
	}

	public void testNoNewLine2() throws IOException {
		init("Z");
		assertFormatted("Z.patch");
	}

	private void init(final String name) throws IOException {
		a = new RawText(readFile(name + "_PreImage"));
		b = new RawText(readFile(name + "_PostImage"));
		file = parseTestPatchFile(name + ".patch").getFiles().get(0);
	}

	private void assertFormatted() throws IOException {
		assertFormatted(getName() + ".out");
	}

	private void assertFormatted(final String name) throws IOException {
		fmt.format(out, file, a, b);
		final String exp = RawParseUtils.decode(readFile(name));
		assertEquals(exp, RawParseUtils.decode(out.toByteArray()));
	}

	private byte[] readFile(final String patchFile) throws IOException {
		final InputStream in = getClass().getResourceAsStream(patchFile);
		if (in == null) {
			fail("No " + patchFile + " test vector");
			return null; // Never happens
		}
		try {
			final byte[] buf = new byte[1024];
			final ByteArrayOutputStream temp = new ByteArrayOutputStream();
			int n;
			while ((n = in.read(buf)) > 0)
				temp.write(buf, 0, n);
			return temp.toByteArray();
		} finally {
			in.close();
		}
	}

	private Patch parseTestPatchFile(final String patchFile) throws IOException {
		final InputStream in = getClass().getResourceAsStream(patchFile);
		if (in == null) {
			fail("No " + patchFile + " test vector");
			return null; // Never happens
		}
		try {
			final Patch p = new Patch();
			p.parse(in);
			return p;
		} finally {
			in.close();
		}
	}
#endif
    }
}
