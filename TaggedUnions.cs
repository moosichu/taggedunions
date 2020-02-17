using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace TMRC.TaggedUnion
{
    public interface IUnionData { }

    public interface ITaggedUnionValue<TTag, TValue>
        where TTag : struct, System.Enum
        where TValue : struct, ITaggedUnionValue<TTag, TValue>
    {
        TTag Tag { get; }
    }

    public unsafe interface ITaggedUnion<TTag, TData, TTaggedUnion>
        where TTag : struct, System.Enum
        where TData : struct, IUnionData
        where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
    {
        TTag Tag { get; set; }
        TData Data { get; set; }
    }

    public class TaggedUnionIsTooBigException : System.Exception
    {
        public TaggedUnionIsTooBigException(string s) : base(s) { }
    }

    public class BadTagEnumTypeException : System.Exception
    {
        public BadTagEnumTypeException(string s) : base(s) { }
    }



    public static class TaggedUnionExtension
    {
        [BurstDiscard]
        private static void TagTypeCheck<TTag, TExpectedUnderlyingType>()
                where TTag : struct, System.Enum
                where TExpectedUnderlyingType : struct
        {
            if (UnsafeUtility.SizeOf<TTag>() != UnsafeUtility.SizeOf<TExpectedUnderlyingType>())
            {
                throw new System.Exception($"The tag type {typeof(TTag)} uses the wrong underlying type for the Unpack() method called, please call the one with the correct specific underlying type.");
            }
        }

        private static bool IntEquals<TTag>(TTag tag1, TTag tag2) where TTag : struct, System.Enum
        {
#if !TMRC_TAGGED_UNION_SAFETY_DISABLED
            TagTypeCheck<TTag, int>();
#endif
            return UnsafeUtility.EnumToInt(tag1) == UnsafeUtility.EnumToInt(tag2);
        }

        private static bool ShortEquals<TTag>(TTag tag1, TTag tag2) where TTag : struct, System.Enum
        {
#if !TMRC_TAGGED_UNION_SAFETY_DISABLED
            TagTypeCheck<TTag, short>();
#endif
            return (UnsafeUtility.EnumToInt(tag1) & ushort.MaxValue) == (UnsafeUtility.EnumToInt(tag2) & ushort.MaxValue);
        }

        private static bool ByteEquals<TTag>(TTag tag1, TTag tag2) where TTag : struct, System.Enum
        {
#if !TMRC_TAGGED_UNION_SAFETY_DISABLED
            TagTypeCheck<TTag, short>();
#endif
            return (UnsafeUtility.EnumToInt(tag1) & byte.MaxValue) == (UnsafeUtility.EnumToInt(tag2) & byte.MaxValue);
        }

        public static bool UnpackInt<TValue, TTaggedUnion, TTag, TData>(TTaggedUnion taggedUnion, out TValue value)
            where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
            where TData : struct, IUnionData
        {
            value = default;
            if (!IntEquals(value.Tag, taggedUnion.Tag))
            {
                return false;
            }

            unsafe
            {
                TData data = taggedUnion.Data;
                void* dataPtr = UnsafeUtility.AddressOf(ref data);
                UnsafeUtility.CopyPtrToStructure(dataPtr, out value);
            }

            return true;
        }



        public static bool UnpackShort<TValue, TTaggedUnion, TTag, TData>(TTaggedUnion taggedUnion, out TValue value)
            where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
            where TData : struct, IUnionData
        {
            value = default;
            if (!ShortEquals(value.Tag, taggedUnion.Tag))
            {
                return false;
            }

            unsafe
            {
                TData data = taggedUnion.Data;
                void* dataPtr = UnsafeUtility.AddressOf(ref data);
                UnsafeUtility.CopyPtrToStructure(dataPtr, out value);
            }

            return true;
        }



        public static bool UnpackByte<TValue, TTaggedUnion, TTag, TData>(TTaggedUnion taggedUnion, out TValue value)
            where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
            where TData : struct, IUnionData
        {
            value = default;
            if (!ByteEquals(value.Tag, taggedUnion.Tag))
            {
                return false;
            }

            unsafe
            {
                TData data = taggedUnion.Data;
                void* dataPtr = UnsafeUtility.AddressOf(ref data);
                UnsafeUtility.CopyPtrToStructure(dataPtr, out value);
            }

            return true;
        }

        [BurstDiscard]
        private static void SizeCheck<TValue, TTaggedUnion, TTag, TData>()
            where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
            where TData : struct, IUnionData
        {
            if (UnsafeUtility.SizeOf<TValue>() > UnsafeUtility.SizeOf<TData>())
            {
                throw new TaggedUnionIsTooBigException(
                    $"The tagged union value {typeof(TValue)} is too large to fit inside {typeof(TData)} ({UnsafeUtility.SizeOf<TValue>()} vs {UnsafeUtility.SizeOf<TData>()} bytes)"
                ); // TODO: Only run this with safety checks enabled
            }
        }

        public static TTaggedUnion Pack<TValue, TTaggedUnion, TTag, TData>(TValue value)
            where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
            where TData : struct, IUnionData
        {
            TTaggedUnion taggedUnion = default;
            taggedUnion.Tag = value.Tag;
#if !TMRC_TAGGED_UNION_SAFETY_DISABLED
            SizeCheck<TValue, TTaggedUnion, TTag, TData>();
#endif

            unsafe
            {
                TData data = default;
                void* dataPtr = UnsafeUtility.AddressOf(ref data);
                UnsafeUtility.CopyStructureToPtr(ref value, dataPtr);
                taggedUnion.Data = data;
            }

            return taggedUnion;
        }

        public static void Write<TValue, TTag>(NativeStream.Writer nativeStreamWriter, TValue value)
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
        {
            nativeStreamWriter.Write<TTag>(value.Tag);
            nativeStreamWriter.Write<TValue>(value);
        }

        public static bool TryReadInt<TValue, TTag>(NativeStream.Reader nativeStreamReader, out TValue value)
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
        {
            value = default;
            if(IntEquals(nativeStreamReader.Peek<TTag>(), value.Tag))
            {
                nativeStreamReader.Read<TTag>();
                value = nativeStreamReader.Read<TValue>();
                return true;
            }
            return false;
        }

        public static bool TryReadShort<TValue, TTag>(NativeStream.Reader nativeStreamReader, out TValue value)
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
        {
            value = default;
            if(ShortEquals(nativeStreamReader.Peek<TTag>(), value.Tag))
            {
                nativeStreamReader.Read<TTag>();
                value = nativeStreamReader.Read<TValue>();
                return true;
            }
            return false;
        }

        public static bool TryReadByte<TValue, TTag>(NativeStream.Reader nativeStreamReader, out TValue value)
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
        {
            value = default;
            if(ByteEquals(nativeStreamReader.Peek<TTag>(), value.Tag))
            {
                nativeStreamReader.Read<TTag>();
                value = nativeStreamReader.Read<TValue>();
                return true;
            }
            return false;
        }
    }
}
