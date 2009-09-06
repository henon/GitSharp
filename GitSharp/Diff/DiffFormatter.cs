using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GitSharp.Patch;
using GitSharp.Util;

namespace GitSharp.Diff
{
	/// <summary>
	/// Format an <seealso cref="EditList"/> as a Git style unified patch script.
	/// </summary>
	public class DiffFormatter
	{
		private static readonly byte[] NoNewLine = 
			Constants.encodeASCII("\\ No newline at end of file\n");

		private int _context;

		/// <summary>
		/// Create a new formatter with a default level of context.
		/// </summary>
		public DiffFormatter()
		{
			setContext(3);
		}

		/// <summary>
		/// Change the number of lines of context to display.
		///	</summary>
		/// </summary>
		///	<param name="lineCount">
		/// Number of lines of context to see before the first
		/// modification and After the last modification within a hunk of
		/// the modified file.
		/// </param>
		public virtual void setContext(int lineCount)
		{
			if (lineCount < 0)
			{
				throw new ArgumentException("context must be >= 0");
			}

			_context = lineCount;
		}

		/// <summary>
		/// Format a patch script, reusing a previously parsed FileHeader.
		///	<p>
		///	This formatter is primarily useful for editing an existing patch script
		///	to increase or reduce the number of lines of context within the script.
		///	All header lines are reused as-is from the supplied FileHeader.
		///	</summary>
		/// </summary>
		/// <param name="out">stream to write the patch script out to.</param>
		/// <param name="head">existing file header containing the header lines to copy.</param>
		/// <param name="a">
		/// Text source for the pre-image version of the content. 
		/// This must match the content of <seealso cref="FileHeader.getOldId()"/>.
		/// </param>
		/// <param name="b">writing to the supplied stream failed.</param>
		public virtual void format(Stream @out, FileHeader head, RawText a, RawText b)
		{
			// Reuse the existing FileHeader as-is by blindly copying its
			// header lines, but avoiding its hunks. Instead we recreate
			// the hunks from the text instances we have been supplied.
			//
			int start = head.getStartOffset();
			int end = head.getEndOffset();

			if (!head.getHunks().isEmpty())
			{
				end = head.getHunks()[0].StartOffset;
			}

			@out.Write(head.getBuffer(), start, end - start);

			FormatEdits(@out, a, b, head.ToEditList());
		}

		private void FormatEdits(Stream @out, RawText a, RawText b, EditList edits)
		{
			for (int curIdx = 0; curIdx < edits.Count; /* */)
			{
				Edit curEdit = edits.get(curIdx);
				int endIdx = FindCombinedEnd(edits, curIdx);
				Edit endEdit = edits.get(endIdx);

				int aCur = Math.Max(0, curEdit.BeginA - _context);
				int bCur = Math.Max(0, curEdit.BeginB - _context);
				int aEnd = Math.Min(a.size(), endEdit.EndA + _context);
				int bEnd = Math.Min(b.size(), endEdit.EndB + _context);

				WriteHunkHeader(@out, aCur, aEnd, bCur, bEnd);

				while (aCur < aEnd || bCur < bEnd)
				{
					if (aCur < curEdit.BeginA || endIdx + 1 < curIdx)
					{
						WriteLine(@out, ' ', a, aCur);
						aCur++;
						bCur++;
					}
					else if (aCur < curEdit.EndA)
					{
						WriteLine(@out, '-', a, aCur++);

					}
					else if (bCur < curEdit.EndB)
					{
						WriteLine(@out, '+', b, bCur++);
					}

					if (End(curEdit, aCur, bCur) && ++curIdx < edits.Count)
					{
						curEdit = edits.get(curIdx);
					}
				}
			}
		}

		private static void WriteHunkHeader(Stream @out, int aCur, int aEnd, int bCur, int bEnd)
		{
			@out.WriteByte(Convert.ToByte('@'));
			@out.WriteByte(Convert.ToByte('@'));
			WriteRange(@out, '-', aCur + 1, aEnd - aCur);
			WriteRange(@out, '+', bCur + 1, bEnd - bCur);
			@out.WriteByte(Convert.ToByte(' '));
			@out.WriteByte(Convert.ToByte('@'));
			@out.WriteByte(Convert.ToByte('@'));
			@out.WriteByte(Convert.ToByte('\n'));
		}

		private static void WriteRange(Stream @out, char prefix, int begin, int cnt)
		{
			@out.WriteByte(Convert.ToByte(' '));
			@out.WriteByte(Convert.ToByte(prefix));

			switch (cnt)
			{
				case 0:
					// If the range is empty, its beginning number must be the
					// line just before the range, or 0 if the range is at the
					// start of the file stream. Here, begin is always 1 based,
					// so an empty file would produce "0,0".
					//
					WriteInteger(@out, begin - 1);
					@out.WriteByte(Convert.ToByte(','));
					@out.WriteByte(Convert.ToByte('0'));
					break;

				case 1:
					// If the range is exactly one line, produce only the number.
					//
					@out.WriteByte(Convert.ToByte(begin));
					break;

				default:
					@out.WriteByte(Convert.ToByte(begin));
					@out.WriteByte(Convert.ToByte(','));
					WriteInteger(@out, cnt);
					break;
			}
		}

		private static void WriteInteger(Stream @out, int count)
		{
			var buffer = Constants.encodeASCII(count);
			@out.Write(buffer, 0, buffer.Length);
		}

		private static void WriteLine(Stream @out, char prefix, RawText text, int cur)
		{
			@out.WriteByte(Convert.ToByte(prefix));
			text.writeLine(@out, cur);
			@out.WriteByte(Convert.ToByte('\n'));
			if (cur + 1 == text.size() && text.isMissingNewlineAtEnd())
			{
				@out.Write(NoNewLine, 0, NoNewLine.Length);
			}
		}

		private int FindCombinedEnd(IList<Edit> edits, int i)
		{
			int end = i + 1;
			while (end < edits.Count && (CombineA(edits, end) || CombineB(edits, end)))
			{
				end++;
			}
			return end - 1;
		}

		private bool CombineA(IList<Edit> e, int i)
		{
			return e[i].BeginA - e[i - 1].EndA <= 2 * _context;
		}

		private bool CombineB(IList<Edit> e, int i)
		{
			return e[i].BeginB - e[i - 1].EndB <= 2 * _context;
		}

		private static bool End(Edit edit, int a, int b)
		{
			return edit.EndA <= a && edit.EndB <= b;
		}
	}
}