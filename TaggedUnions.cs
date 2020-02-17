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

        public static bool UnpackInt<TValue, TTaggedUnion, TTag, TData>(TTaggedUnion taggedUnion, out TValue value)
            where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : struct, System.Enum
            where TData : struct, IUnionData
        {

#if !TMRC_TAGGED_UNION_SAFETY_DISABLED
            TagTypeCheck<TTag, int>();
#endif
            value = default;
            if (UnsafeUtility.EnumToInt(value.Tag) != UnsafeUtility.EnumToInt(taggedUnion.Tag))
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

#if !TMRC_TAGGED_UNION_SAFETY_DISABLED
            TagTypeCheck<TTag, short>();
#endif
            value = default;
            if ((UnsafeUtility.EnumToInt(value.Tag) & ushort.MaxValue) != (UnsafeUtility.EnumToInt(taggedUnion.Tag) & ushort.MaxValue))
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

#if !TMRC_TAGGED_UNION_SAFETY_DISABLED
            TagTypeCheck<TTag, byte>();
#endif
            value = default;
            if ((UnsafeUtility.EnumToInt(value.Tag) & byte.MaxValue) != (UnsafeUtility.EnumToInt(taggedUnion.Tag) & byte.MaxValue))
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
    }
}
