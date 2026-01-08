using System;
using System.Collections.Generic;
using Verse;

namespace SPExtended;

public static class SPTuples
{
    /// <summary>
    ///     SmashPhil's implementation of Tuples
    ///     <para>Create a Tuple in .Net3.5 that functions the same as Tuple while avoiding boxing.</para>
    ///     <seealso cref="SPTuple{T1, T2}" />
    ///     <seealso cref="SPTuple{T1, T2, T3}" />
    /// </summary>
    public static class SPTuple
    {
        public static SPTuple<T1, T2> Create<T1, T2>(T1 first, T2 second)
        {
            return new SPTuple<T1, T2>(first, second);
        }

        public static SPTuple<T1, T2, T3> Create<T1, T2, T3>(T1 first, T2 second, T3 third)
        {
            return new SPTuple<T1, T2, T3>(first, second, third);
        }
    }

    /// <summary>
    ///     SPTuple of 2 Types
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class SPTuple<T1, T2>(T1 first, T2 second)
    {
        private static readonly IEqualityComparer<T1> FirstComparer = EqualityComparer<T1>.Default;
        private static readonly IEqualityComparer<T2> SecondComparer = EqualityComparer<T2>.Default;

        public T1 First { get; } = first;
        public T2 Second { get; } = second;

        public static bool operator ==(SPTuple<T1, T2> o1, SPTuple<T1, T2> o2)
        {
            return o1 != null && o1.Equals(o2);
        }

        public static bool operator !=(SPTuple<T1, T2> o1, SPTuple<T1, T2> o2)
        {
            return !(o1 == o2);
        }

        public override int GetHashCode()
        {
            var hash = 0;
            if (First is object)
            {
                hash = FirstComparer.GetHashCode(First);
            }

            if (Second is object)
            {
                hash = ((hash << 5) + hash) ^ SecondComparer.GetHashCode(Second);
            }

            return hash;
        }

        public override bool Equals(object o)
        {
            if (o is not SPTuple<T1, T2> o2)
            {
                return false;
            }

            return FirstComparer.Equals(First, o2.First) && SecondComparer.Equals(Second, o2.Second);
        }
    }

    /// <summary>
    ///     SPTuple of 3 types
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    public class SPTuple<T1, T2, T3>(T1 first, T2 second, T3 third) : SPTuple<T1, T2>(first, second)
    {
        private static readonly IEqualityComparer<T1> FirstComparer = EqualityComparer<T1>.Default;
        private static readonly IEqualityComparer<T2> SecondComparer = EqualityComparer<T2>.Default;
        private static readonly IEqualityComparer<T3> ThirdComparer = EqualityComparer<T3>.Default;

        public T3 Third { get; } = third;

        public static bool operator ==(SPTuple<T1, T2, T3> o1, SPTuple<T1, T2, T3> o2)
        {
            return o1 != null && o1.Equals(o2);
        }

        public static bool operator !=(SPTuple<T1, T2, T3> o1, SPTuple<T1, T2, T3> o2)
        {
            return !(o1 == o2);
        }

        public override int GetHashCode()
        {
            var hash = 0;
            if (First is object)
            {
                hash = FirstComparer.GetHashCode(First);
            }

            if (Second is object)
            {
                hash = ((hash << 3) + hash) ^ SecondComparer.GetHashCode(Second);
            }

            if (Third is object)
            {
                hash = ((hash << 5) + hash) ^ ThirdComparer.GetHashCode(Third);
            }

            return hash;
        }

        public override bool Equals(object o)
        {
            if (o is not SPTuple<T1, T2, T3> o2)
            {
                return false;
            }

            return FirstComparer.Equals(First, o2.First) && SecondComparer.Equals(Second, o2.Second) &&
                   ThirdComparer.Equals(Third, o2.Third);
        }
    }

    public struct SPTuple2<T1, T2>(T1 first, T2 second) : IEquatable<SPTuple2<T1, T2>>
    {
        public T1 First
        {
            get => first;
            set
            {
                if (value != null)
                {
                    first = value;
                }

                Log.Error($"Tried to assign value of different type to Tuple of type {typeof(T1)}");
            }
        }

        public T2 Second
        {
            get => second;
            set
            {
                if (value != null)
                {
                    second = value;
                }

                Log.Error($"Tried to assign value of different type to Tuple of type {typeof(T2)}");
            }
        }

        public override int GetHashCode()
        {
            var hash = 0;
            if (First is object)
            {
                hash = EqualityComparer<T1>.Default.GetHashCode(First);
            }

            if (Second is object)
            {
                hash = ((hash << 3) + hash) ^ EqualityComparer<T2>.Default.GetHashCode(Second);
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            return obj is SPTuple2<T1, T2> tuple2 && Equals(tuple2) ||
                   obj is Pair<T1, T2> pair && Equals(pair);
        }

        public bool Equals(SPTuple2<T1, T2> other)
        {
            return EqualityComparer<T1>.Default.Equals(first, other.first) &&
                   EqualityComparer<T2>.Default.Equals(second, other.second);
        }

        public bool Equals(Pair<T1, T2> other)
        {
            return EqualityComparer<T1>.Default.Equals(first, other.First) &&
                   EqualityComparer<T2>.Default.Equals(second, other.Second);
        }

        public static bool operator ==(SPTuple2<T1, T2> lhs, SPTuple2<T1, T2> rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(SPTuple2<T1, T2> lhs, SPTuple2<T1, T2> rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(Pair<T1, T2> lhs, SPTuple2<T1, T2> rhs)
        {
            return false;
        }

        public static bool operator !=(Pair<T1, T2> lhs, SPTuple2<T1, T2> rhs)
        {
            return !(lhs == rhs);
        }

        private T1 first = first;

        private T2 second = second;
    }
}