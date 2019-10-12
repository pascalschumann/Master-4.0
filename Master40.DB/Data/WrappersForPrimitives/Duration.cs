using System;

namespace Master40.DB.Data.WrappersForPrimitives
{
    public class Duration : IntPrimitive<Duration>
    {
        public Duration(int @int) : base(@int)
        {
        }

        public Duration()
        {
        }

        public DueTime ToDueTime()
        {
            return new DueTime(Int);
        }
    }
}