using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace TMRC.TaggedUnion
{
    public interface IUnionData { }

    public interface ITaggedUnionValue<TTag, TValue>
        where TTag : System.Enum
        where TValue : struct, ITaggedUnionValue<TTag, TValue>
    {
        TTag Tag { get; }
    }

    public unsafe interface ITaggedUnion<TTag, TData, TTaggedUnion>
        where TTag : System.Enum
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

    public static class TaggedUnionExtension
    {
        public static bool Unpack<TValue, TTaggedUnion, TTag, TData>(TTaggedUnion taggedUnion, out TValue value)
            where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : System.Enum
            where TData : struct, IUnionData
        {
            value = default;
            if (!value.Tag.Equals(taggedUnion.Tag))
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

        public static TTaggedUnion Pack<TValue, TTaggedUnion, TTag, TData>(TValue value)
            where TTaggedUnion : struct, ITaggedUnion<TTag, TData, TTaggedUnion>
            where TValue : struct, ITaggedUnionValue<TTag, TValue>
            where TTag : System.Enum
            where TData : struct, IUnionData
        {
            TTaggedUnion taggedUnion = default;
            taggedUnion.Tag = value.Tag;
#if !TMRC_TAGGED_UNION_SAFETY_DISABLED
            if (UnsafeUtility.SizeOf<TValue>() > UnsafeUtility.SizeOf<TData>())
            {
                throw new TaggedUnionIsTooBigException(
                    $"The tagged union value {typeof(TValue)} is too large to fit inside {typeof(TData)} ({UnsafeUtility.SizeOf<TValue>()} vs {UnsafeUtility.SizeOf<TData>()} bytes)"
                ); // TODO: Only run this with safety checks enabled
            }
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
