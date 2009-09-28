using System;
using System.IO;

namespace GitSharp.Core.Util
{
	/// <summary>
	/// A normal Stream might provide a timeout on a specific read opreation.
	/// However, using StreamReader.ReadToEnd() on it can still get stuck for a long time.
	/// 
	/// This class offers a timeout from the moment of it's construction to the read.
	/// Every read past the timeout <b>from the stream's construction</b> will fail.
	/// 
	/// If the timeout elapsed while a read is in progress TimeoutStream is not responsible for aborting
	/// the read (there is no known good way in .NET to do it)
	/// 
	/// See
	/// <list>
	/// <item>http://www.dotnet247.com/247reference/msgs/36/182553.aspx and </item>
	/// <item>http://www.google.co.il/search?q=cancel+async+Stream+read+.net</item>
	/// </list>
	/// <example>
	/// <code>
	/// Stream originalStream = GetStream();
	/// StreamReader reader = new StreamReader(new TimeoutStream(originalStream, 5000));
	/// 
	/// // assuming the originalStream has a per-operation timeout, then ReadToEnd()
	/// // will return in (5000 + THAT_TIMEOUT)
	/// string foo = reader.ReadToEnd();
	/// </code></example>
	/// </summary>
	public class TimeoutStream : Stream
	{
		#region Public

		public TimeoutStream(Stream stream, int timeoutMillis)
		{
			_stream = stream;
			_endTime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, timeoutMillis));
		}

		public override bool CanRead
		{
			get { return _stream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _stream.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return _stream.CanWrite; }
		}

		public override long Length
		{
			get { return _stream.Length; }
		}

		public override long Position
		{
			get { return _stream.Position; }
			set { _stream.Position = value; }
		}

		public override void Flush()
		{
			_stream.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			CheckTimeout();
			return _stream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_stream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckTimeout();
			return _stream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckTimeout();
			Write(buffer, offset, count);
		}

		#endregion Public

		#region Private

		private void CheckTimeout()
		{
			if (DateTime.Now.CompareTo(_endTime) > 0)
				throw new TimeoutException("Timeout elapsed waiting for read");
		}

		private readonly Stream _stream;
		private readonly DateTime _endTime;

		#endregion
	}
}