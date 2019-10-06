using Master40.DB.Data.WrappersForPrimitives;

namespace Zpp.Scheduling.impl
{
    public class Priority: IntPrimitive<Priority>
    {
        public Priority(int @int) : base(@int)
        {
        }

        public Priority() : base()
        {
        }
    }
}