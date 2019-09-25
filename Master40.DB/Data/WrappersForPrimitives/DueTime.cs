using System;

namespace Master40.DB.Data.WrappersForPrimitives
{
    public class DueTime : IntPrimitive<DueTime>
    {
        public DueTime(int @int) : base(@int)
        {
        }

        public DueTime()
        {
        }
    }
}