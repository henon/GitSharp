using System;

namespace GitSharp.Tests
{
	public abstract class XunitBaseFact : IDisposable
	{
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
}