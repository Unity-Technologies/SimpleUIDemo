namespace Unity.Tiny
{
    public static class ArrayExtensions
    {
        public static T[] Concat<T>(this T[] array, T item)
        {
            var result = new T[array.Length + 1];

            // FIXME: tiny mscorlib does not support Array.CopyTo
            //array.CopyTo(result, 0);
            for (var i = 0; i < array.Length; ++i)
            {
                result[i] = array[i];
            }

            result[array.Length] = item;
            return result;
        }

        public static T[] Concat<T>(this T[] array, T[] other)
        {
            var result = new T[array.Length + other.Length];

            // FIXME: tiny mscorlib does not support Array.CopyTo
            //array.CopyTo(result, 0);
            //other.CopyTo(result, array.Length);
            for (var i = 0; i < array.Length; ++i)
            {
                result[i] = array[i];
            }
            for (var i = 0; i < other.Length; ++i)
            {
                result[array.Length + i] = other[i];
            }

            return result;
        }
    }
}
