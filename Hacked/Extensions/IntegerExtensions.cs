namespace Hacked.Extensions
{
    using System;

    public static class IntegerExtensions
    {
        public static TimeSpan Seconds(this int source)
        {
            return TimeSpan.FromSeconds(source);
        }
    }
}