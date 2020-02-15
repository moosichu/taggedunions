# Tagged Unions in Unity

This is a [Unity Package](https://docs.unity3d.com/Manual/PackagesList.html) that allows users to implement something akin to tagged unions in C#, abusing as much unsafe code and as many language features as possible.

The whilst the programmer has to do more work than would be desirable to get things setup, this library does implement the pointer-manipulation required for this feature - including providing runtime checks to ensure that the tagged union has enough capacity for all of its possible types.

## Setup

Add the following line to your Unity project's `manifest.json` file:

```
"com.moosichu.taggedunions": "https://github.com/moosichu/taggedunions.git"
```

This will import the package*, allowing you to get started.

*It's worth noting that you should create your own copy of this repository and reference that as a package if you are using in production, in-case this repo isn't available to you for whichever reason.

## Defining a tagged union

The code below shows how to define a new tagged union, this defines a tagged union called `Event`, which can either be an `AudioTriggerEvent` or a `ChangeGameStateEvent`.

`Event` needs to be a `struct` that contains a tag (`EventTag`) and some data (`EventData`). The `EventTag` is an enum, which an entry for each possible type of event. `EventData` is simply a `struct` whose size is at least as large as the largest possible event (member names or anything don't matter).

This is works by having `Event` implement `ITaggedUnion`, providing the `EventTag`, `EventData` and itself as generics arguments for the tagged union's tag type, data struct and the tagged union type respectively.

Then the events themselves simply each implement `ITaggedUnionValue`, providing the `EventTag` and themselves as generics arguments in the same manner. These just need to implement the `Tag` property, ensuring that they each return a unique static tag value corresponding to their type. It's up to the programmer to ensure a one-to-one correlation with the possible tag values and the union data types.

Finally helper functions need to implemented for event packing and unpacking, this is because C# cannot infer generic type values from type constraints, so we have to explicitly do that ourselves.

```CSharp
using TMRC.TaggedUnion;
using Unity.Collections;

public enum EventTag : byte
{
    ChangeGameState,
    AudioTrigger,
}

public struct EventData : IUnionData
{
    private Bytes126 _data;
}

public struct Event : ITaggedUnion<EventTag, EventData, Event>
{
    public EventTag Tag { get; set; }
    public EventData Data { get; set; }
}

public struct ChangeGameStateEvent : ITaggedUnionValue<EventTag, ChangeGameStateEvent>
{
    public EventTag Tag => EventTag.ChangeGameState;
    public int NewGameState;
}

public struct AudioTriggerEvent : ITaggedUnionValue<EventTag, AudioTriggerEvent>
{
    public EventTag Tag => EventTag.AudioTrigger;
    public int AudioId;
    public float AudioVolume;
}

public static class EventExtensions
{
    public static bool Unpack<TEventValue>(this Event e, out TEventValue value)
        where TEventValue : struct, ITaggedUnionValue<EventTag, TEventValue>
    {
        return TaggedUnionExtension.Unpack<TEventValue, Event, EventTag, EventData>(e, out value);
    }

    public static Event Pack<TEventValue>(this TEventValue value)
        where TEventValue : struct, ITaggedUnionValue<EventTag, TEventValue>
    {
        return TaggedUnionExtension.Pack<TEventValue, Event, EventTag, EventData>(value);
    }

}
```

## Using tagged unions

The code below shows tagged unions being used: pushing them onto a queue and
then popping them back-out and reading their values.

```CSharp
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;

public static class ExampleCode
{
    public static void Main()
    {
        NativeQueue<Event> eventQueue = new NativeQueue<Event>(Allocator.Temp);
        {
            Event changeGameStateEvent = new ChangeGameStateEvent()
            {
                NewGameState = 100,
            }.Pack();

            eventQueue.Enqueue(changeGameStateEvent);
        }

        {
            Event audioEvent = new AudioTriggerEvent()
            {
                AudioId = 12,
                AudioVolume = 0.5f,
            }.Pack();

            eventQueue.Enqueue(audioEvent);
        }

        while (eventQueue.TryDequeue(out Event e))
        {
            if (e.Unpack(out AudioTriggerEvent audioTriggerEvent))
            {
                Debug.Log($"Have unpacked AudioTriggerEvent {audioTriggerEvent.AudioId}, {audioTriggerEvent.AudioVolume}");
            }
            else if (e.Unpack(out ChangeGameStateEvent changeGameStateEvent))
            {
                Debug.Log($"Have unpacked ChangeGameStateEvent {changeGameStateEvent.NewGameState}");
            }
            else
            {
                Debug.LogError("Unrecognised tagged union type!");
            }
        }

        eventQueue.Dispose();

    }
}
```

# Safety checks

Calls to `Pack()` will run a safety check to ensure that a given tagged union value will fit into the root type. These can be disabled (for production purposes, for example) by defining `TMRC_TAGGED_UNION_SAFETY_DISABLED`.
