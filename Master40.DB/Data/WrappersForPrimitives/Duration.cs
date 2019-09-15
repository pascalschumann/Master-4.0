using System;

namespace Master40.DB.Data.WrappersForPrimitives
{
    public class Duration : IComparable<Duration>,IComparable 
    {
        private readonly int _duration;

        public Duration(int duration)
        {
            _duration = duration;
        }

        public int GetValue()
        {
            return _duration;
        }
        
        public int CompareTo(Duration that)
        {
            return _duration.CompareTo(that.GetValue());
        }

        public int CompareTo(object obj)
        {
            Duration otherDuration = (Duration)obj;
            return _duration.CompareTo(otherDuration.GetValue());
        }

        public override bool Equals(object obj)
        {
            Duration otherDuration = (Duration) obj;
            return _duration.Equals(otherDuration._duration);
        }

        public override int GetHashCode()
        {
            return _duration.GetHashCode();
        }

        public override string ToString()
        {
            return _duration.ToString();
        }

        public bool IsNull()
        {
            return _duration.Equals(0);
        }

        public static Duration Null()
        {
            return new Duration(0);
        }

    }
}