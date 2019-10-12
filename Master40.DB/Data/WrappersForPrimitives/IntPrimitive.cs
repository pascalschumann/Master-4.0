using System;

namespace Master40.DB.Data.WrappersForPrimitives
{
    public class IntPrimitive<T> : INumericPrimitive<T> where T : IntPrimitive<T>, new()
    {
        protected int Int;

        public IntPrimitive(int @int)
        {
            Int = @int;
        }

        public IntPrimitive()
        {
        }

        public int GetValue()
        {
            return Int;
        }
        
        public void Increment()
        {
            Int++;
        }

        public void Decrement()
        {
            Int--;
        }

        public void IncrementBy(T t)
        {
            Int += t.Int;
        }

        public void DecrementBy(T t)
        {
            Int -= t.Int;
        }


        public bool IsGreaterThanOrEqualTo(T t)
        {
            return Int >= t.Int;
        }

        public bool IsGreaterThan(T t)
        {
            return Int > t.Int;
        }

        public bool IsGreaterThanNull()
        {
            return IsGreaterThan(Null());
        }

        public bool IsSmallerThan(T t)
        {
            return Int < t.Int;
        }

        public bool IsSmallerThanOrEqualTo(T t)
        {
            return Int <= t.Int;
        }

        public T Minus(T t)
        {
            int newValue = Int - t.Int;
            T newObject = (T) Activator.CreateInstance(typeof(T));
            newObject.Int = newValue;
            return newObject;
        }

        public T Plus(T t)
        {
            int newValue = Int + t.Int;
            T newObject = (T) Activator.CreateInstance(typeof(T));
            newObject.Int = newValue;
            return newObject;
        }

        public T AbsoluteValue()
        {
            int newValue = Math.Abs(Int);
            T newObject = (T) Activator.CreateInstance(typeof(T));
            newObject.Int = newValue;
            return newObject;
        }

        public bool IsNull()
        {
            return Int.Equals(0);
        }

        public bool IsNegative()
        {
            return Int < 0;
        }

        public static T Null()
        {
            int newValue = 0;
            T newObject = (T) Activator.CreateInstance(typeof(T));
            newObject.Int = newValue;
            return newObject;
        }

        public int CompareTo(T that)
        {
            return Int.CompareTo(that.Int);
        }

        public int CompareTo(object obj)
        {
            T other = (T) obj;
            return Int.CompareTo(other.Int);
        }

        public override bool Equals(object obj)
        {
            T other = (T) obj;
            if (other == null)
            {
                return false;
            }

            return Int.Equals(other.Int);
        }

        public override int GetHashCode()
        {
            return Int.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Int}";
        }
    }
}