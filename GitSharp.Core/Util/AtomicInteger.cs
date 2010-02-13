namespace GitSharp.Core.Util
{
    public class AtomicInteger
    {
        private int _value;
        private readonly object _padLock = new object();

        public AtomicInteger(int value)
        {
            _value = value;
        }

        public AtomicInteger()
            : this(0)
        {
        }

        /// <summary>
        /// Get the current value.
        /// </summary>
        /// <returns>the current value</returns>
        public int get()
        {
            lock (_padLock)
            {
                return _value;
            }
        }

        /// <summary>
        /// Atomically set the value to the given updated value if the current value == the expected value. the expected value
        /// </summary>
        /// <param name="expect">the expected value</param>
        /// <param name="curr">the new value</param>
        /// <returns>true if successful. False return indicates that the actual value was not equal to the expected value.</returns>
        public bool compareAndSet(int expect, int curr)
        {
            lock (_padLock)
            {
                if (_value != expect)
                {
                    return false;
                }

                _value = curr;
                return true;
            }
        }

        /// <summary>
        /// Atomically increment by one the current value.
        /// </summary>
        /// <returns>the updated value</returns>
        public int incrementAndGet()
        {
            lock (_padLock)
            {
                _value++;
                return _value;
            }
        }

        /// <summary>
        /// Atomically decrement by one the current value.
        /// </summary>
        /// <returns>the updated value</returns>
        public int decrementAndGet()
        {
            lock (_padLock)
            {
                _value--;
                return _value;
            }
        }
    }
}
