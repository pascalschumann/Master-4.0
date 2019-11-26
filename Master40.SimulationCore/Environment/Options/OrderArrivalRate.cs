using Master40.SimulationCore.Environment.Abstractions;
using System;

namespace Master40.SimulationCore.Environment.Options
{
    public class OrderArrivalRate : Option<double>
    {
        public static Type Type => typeof(OrderArrivalRate);
        /**
         * (Menge der zu erzeugenden auftrage im intervall +1)  / (die dauer des intervalls)
         */
        public OrderArrivalRate(double value)
        {
            _value = value;
        }
    }
}
