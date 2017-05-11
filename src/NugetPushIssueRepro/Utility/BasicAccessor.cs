using System;

namespace NugetPushIssueRepro.Utility
{
    /// <summary>
    /// This accessor class allows dependency-injected access to an object whose value cannot be determined at the time of service initialization, such as
    /// a parsed CLI argument (or value based of of such argument). To help ensure proper implementation, the accessor enforces some basic behavior to ensure
    /// that the value can be set only once and that it has been set before it is used.
    /// </summary>
    internal class BasicAccessor<TValue> : IAccessor<TValue>
        where TValue : class
    {
        private TValue _value;
        private bool _hasBeenSet;
        public TValue Value => _hasBeenSet ?
            _value :
            throw new InvalidOperationException($"Value for {typeof(TValue).Name} must be explicitly set using {nameof(SetValue)} before it can be accessed.");

        public void SetValue(TValue value)
        {
            if (_hasBeenSet)
            {
                throw new InvalidOperationException("Value has already been set. It may be set only once.");
            }
            _value = value.ThrowIfNull(nameof(Value));
            _hasBeenSet = true;
        }
    }
}