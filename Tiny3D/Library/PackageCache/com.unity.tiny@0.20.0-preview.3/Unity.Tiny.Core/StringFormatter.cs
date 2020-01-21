using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Tiny.Utils
{
    public static class StringFormatter
    {
        internal unsafe ref struct StringBuilder
        {
            public int Position;
            public int Length;
            public char* Buffer;

            public StringBuilder(int length)
            {
                Position = 0;
                Length = length;
                Buffer = (char*)UnsafeUtility.Malloc(length * sizeof(char), 1, Allocator.TempJob);
            }

            public void Append(string source)
            {
                fixed (char* sourcePtr = source)
                {
                    Append(sourcePtr, source.Length);
                }
            }

            public void Append(char* source, int sourceLength)
            {
                EnsureCapacity(sourceLength);
                Copy(source, sourceLength);
            }

            public void Append(char source)
            {
                EnsureCapacity(1);
                Buffer[Position++] = source;
            }

            private void EnsureCapacity(int requestedLength)
            {
                int totalRequiredLength = Position + requestedLength;
                if (totalRequiredLength > Length)
                {
                    var newLength = math.max(totalRequiredLength, Length * 2);
                    var newDestination = (char*)UnsafeUtility.Malloc(newLength * sizeof(char), 1, Allocator.TempJob);

                    UnsafeUtility.MemCpy(newDestination, Buffer, Position * sizeof(char));
                    UnsafeUtility.Free(Buffer, Allocator.TempJob);

                    Buffer = newDestination;
                    Length = newLength;
                }
            }

            private void Copy(char* source, int sourceLength)
            {
                UnsafeUtility.MemCpy(Buffer + Position, source, sourceLength * sizeof(char));
                Position += sourceLength;
            }

            public override string ToString()
            {
                return new string(Buffer, 0, Position);
            }

            public void Free()
            {
                UnsafeUtility.Free(Buffer, Allocator.TempJob);
            }
        }

        // Implements similar functionality to string.Format.
        public static unsafe string Format(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
                return format;

            var builder = new StringBuilder(format.Length * 2);

            try
            {
                Format(format, args, ref builder);
                return builder.ToString();
            }
            finally
            {
                builder.Free();
            }
        }

        private static unsafe void Format(string format, object[] args, ref StringBuilder builder)
        {
            int index = 0;
            int argIndex = 0;
            bool isParsingArg = false;
            bool isParsingArgIndex = false;

            while (index < format.Length)
            {
                if (!isParsingArg && format[index] == '{')
                {
                    argIndex = 0;
                    isParsingArg = true;
                    isParsingArgIndex = true;
                    index++;
                    continue;
                }

                if (isParsingArg && format[index] == '}')
                {
                    isParsingArg = false;
                    isParsingArgIndex = false;
                    if (argIndex < args.Length)
                    {
                        ResolveParam(args[argIndex], ref builder);
                    }
                    else
                    {
                        builder.Append("[Bad argument index in Debug.Format]");
                    }

                    index++;
                    continue;
                }

                if (isParsingArg && format[index] != '}')
                {
                    if (isParsingArgIndex && format[index] >= '0' && format[index] <= '9')
                    {
                        argIndex *= 10;
                        argIndex += format[index] - '0';
                    }
                    else
                        isParsingArgIndex = false;

                    index++;
                    continue;
                }

                builder.Append(format[index]);
                index++;
            }
        }
        private unsafe static void ResolveParam(object param, ref StringBuilder builder)
        {
            if (param is int intParam)
            {
                builder.Append(intParam.ToString());
            }
            else if (param is float floatParam)
            {
                builder.Append(NumberConverter.FloatToString(floatParam));
            }
            else if (param is double doubleParam)
            {
                builder.Append(NumberConverter.DoubleToString(doubleParam));
            }
            else if (param is string strParam)
            {
                builder.Append(strParam);
            }
            else if (param is char charParam)
            {
                builder.Append(charParam);
            }
            else if (param is bool boolParam)
            {
                builder.Append(boolParam ? "true" : "false");
            }
            else if (param is Entity entity)
            {
                string desc;
#if false
                var mgr = World.Active.EntityManager;
                if (mgr.Exists(entity))
                {
                    if (mgr.HasComponent<EntityName>(entity))
                        desc = mgr.GetBufferAsString<EntityName>(entity);
                    else
                        desc = "Unnamed Entity";
                }
                else
                {
                    desc = "Non Existing Entity";
                }
#else
                desc = "Entity";
#endif
                Format("[{0} {1}:{2}]", new object[] { desc, entity.Index, entity.Version }, ref builder);

            }
            else if (param is float2 float2Param)
            {
                Format("({0}, {1})", new object[] { float2Param.x, float2Param.y }, ref builder);
            }
            else if (param is float3 float3Param)
            {
                Format("({0}, {1}, {2})", new object[] { float3Param.x, float3Param.y, float3Param.z }, ref builder);
            }
            else if (param is float4 float4Param)
            {
                Format("({0}, {1}, {2}, {3})", new object[] { float4Param.x, float4Param.y, float4Param.z, float4Param.w }, ref builder);
            }
            else if (param is Unity.Entities.TypeManager.TypeInfo)
            {
                builder.Append("[TypeInfo]");
            }
            else if (param is IComponentData)
            {
                builder.Append("[IComponentData]");
            }
            else
            {
                builder.Append("[type not supported]");
            }
        }
    }
}
