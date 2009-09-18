using System;
using Xunit;

namespace GitSharp.Tests
{
	public abstract class XunitBaseFact : IDisposable
	{
		public const int TestTimeout = 3000;

		protected XunitBaseFact()
		{
			SetUp();
		}

		#region Implementation of IDisposable

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			TearDown();
		}

		#endregion

		#region Test Setup and TearDown

		protected virtual void SetUp()
		{
		}

		protected virtual void TearDown()
		{
		}

		#endregion
	}

	/// <summary>
	/// This modified attribute is just to make sure that every test does not
	/// take more than <see cref="XunitBaseFact.TestTimeout"/> to run.
	/// </summary>
	public class StrictFactAttribute : FactAttribute
	{
		public StrictFactAttribute()
		{
			Timeout = XunitBaseFact.TestTimeout;
		}
	}
}