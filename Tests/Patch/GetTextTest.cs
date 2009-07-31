/*
 * Copyright (C) 2008, Google Inc.
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
    public class GetTextTest
    {
#if false
	public void testGetText_BothISO88591() throws IOException {
		final Charset cs = Charset.forName("ISO-8859-1");
		final Patch p = parseTestPatchFile();
		assertTrue(p.getErrors().isEmpty());
		assertEquals(1, p.getFiles().size());
		final FileHeader fh = p.getFiles().get(0);
		assertEquals(2, fh.getHunks().size());
		assertEquals(readTestPatchFile(cs), fh.getScriptText(cs, cs));
	}

	public void testGetText_NoBinary() throws IOException {
		final Charset cs = Charset.forName("ISO-8859-1");
		final Patch p = parseTestPatchFile();
		assertTrue(p.getErrors().isEmpty());
		assertEquals(1, p.getFiles().size());
		final FileHeader fh = p.getFiles().get(0);
		assertEquals(0, fh.getHunks().size());
		assertEquals(readTestPatchFile(cs), fh.getScriptText(cs, cs));
	}

	public void testGetText_Convert() throws IOException {
		final Charset csOld = Charset.forName("ISO-8859-1");
		final Charset csNew = Charset.forName("UTF-8");
		final Patch p = parseTestPatchFile();
		assertTrue(p.getErrors().isEmpty());
		assertEquals(1, p.getFiles().size());
		final FileHeader fh = p.getFiles().get(0);
		assertEquals(2, fh.getHunks().size());

		// Read the original file as ISO-8859-1 and fix up the one place
		// where we changed the character encoding. That makes the exp
		// string match what we really expect to get back.
		//
		String exp = readTestPatchFile(csOld);
		exp = exp.replace("\303\205ngstr\303\266m", "\u00c5ngstr\u00f6m");

		assertEquals(exp, fh.getScriptText(csOld, csNew));
	}

	public void testGetText_DiffCc() throws IOException {
		final Charset csOld = Charset.forName("ISO-8859-1");
		final Charset csNew = Charset.forName("UTF-8");
		final Patch p = parseTestPatchFile();
		assertTrue(p.getErrors().isEmpty());
		assertEquals(1, p.getFiles().size());
		final CombinedFileHeader fh = (CombinedFileHeader) p.getFiles().get(0);
		assertEquals(1, fh.getHunks().size());

		// Read the original file as ISO-8859-1 and fix up the one place
		// where we changed the character encoding. That makes the exp
		// string match what we really expect to get back.
		//
		String exp = readTestPatchFile(csOld);
		exp = exp.replace("\303\205ngstr\303\266m", "\u00c5ngstr\u00f6m");

		assertEquals(exp, fh
				.getScriptText(new Charset[] { csNew, csOld, csNew }));
	}

	private Patch parseTestPatchFile() throws IOException {
		final String patchFile = getName() + ".patch";
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

	private String readTestPatchFile(final Charset cs) throws IOException {
		final String patchFile = getName() + ".patch";
		final InputStream in = getClass().getResourceAsStream(patchFile);
		if (in == null) {
			fail("No " + patchFile + " test vector");
			return null; // Never happens
		}
		try {
			final InputStreamReader r = new InputStreamReader(in, cs);
			char[] tmp = new char[2048];
			final StringBuilder s = new StringBuilder();
			int n;
			while ((n = r.read(tmp)) > 0)
				s.append(tmp, 0, n);
			return s.toString();
		} finally {
			in.close();
		}
	}
#endif
    }
}
