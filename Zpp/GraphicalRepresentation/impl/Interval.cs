using Master40.DB.Data.WrappersForPrimitives;

namespace Zpp.GraphicalRepresentation.impl
{
    public class Interval
    {
        public DueTime Start { get; }
        public DueTime End { get; }

        public Interval(DueTime start, DueTime end)
        {
            Start = start;
            End = end;
        }

        public bool Intersects(Interval other)
        {
            return (other.Start.IsGreaterThanOrEqualTo(Start) &&
                    End.IsGreaterThanOrEqualTo(other.Start)) ||
                   (other.End.IsGreaterThanOrEqualTo(Start) &&
                    End.IsGreaterThanOrEqualTo(other.End));
        }
    }
}