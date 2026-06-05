namespace BuberDinner.Infrastructure.Persistence.Memory;

using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Sets the framework-internal <c>Aggregate&lt;TId&gt;.ETag</c> property via reflection.
///
/// Trellis V3 documents <c>ETag</c> as "Persistence-managed". The setter lives on the
/// concrete <c>Aggregate&lt;TId&gt;</c> base class (not on the <c>IAggregate</c>
/// interface) and is <c>internal</c> — only Trellis-internal persistence assemblies
/// (Trellis.EntityFrameworkCore in the box) can write it through normal C#. Consumers
/// using non-EF backends (in-memory, custom JSON store, Cosmos via Microsoft.Azure.Cosmos
/// directly, etc.) have no public hook for writing the ETag, so the framework helpers
/// <c>AggregateETagExtensions.OptionalETag</c> / <c>RequireETag</c> can never see the
/// repository's persisted ETag value.
///
/// This helper bridges the gap by reflecting against the concrete aggregate's
/// runtime type (which has the setter, even if internal) — or falling back to the
/// compiler-generated backing field <c>&lt;ETag&gt;k__BackingField</c> when the
/// property has no exposed setter at all.
///
/// Local to BuberDinner's in-memory persistence layer; not exposed beyond the Memory
/// namespace. When the framework ships a public infrastructure hook, delete this file.
///
/// Tracked as framework feedback: in-memory / custom-persistence ETag write-hook gap
/// (reg-005-aggregate-etag-write-hook).
/// </summary>
internal static class AggregateETagWriter
{
    private static readonly ConcurrentDictionary<Type, Action<object, string>> s_setters = new();

    public static void SetETag<TAggregate>(TAggregate aggregate, string etag)
        where TAggregate : global::Trellis.IAggregate
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(etag);
        var setter = s_setters.GetOrAdd(aggregate.GetType(), BuildSetter);
        setter(aggregate, etag);
    }

    private static Action<object, string> BuildSetter(Type aggregateType)
    {
        // Walk up the hierarchy looking for an ETag property with a settable backer.
        for (var t = aggregateType; t is not null; t = t.BaseType)
        {
            var prop = t.GetProperty("ETag",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (prop is not null)
            {
                var setMethod = prop.GetSetMethod(nonPublic: true);
                if (setMethod is not null)
                    return (obj, val) => setMethod.Invoke(obj, new object[] { val });

                // Property has no setter at all (init-only or get-only auto-prop).
                // Fall through to the backing field below.
                break;
            }
        }

        // Fallback: the compiler-generated backing field for `ETag`.
        for (var t = aggregateType; t is not null; t = t.BaseType)
        {
            var field = t.GetField("<ETag>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null)
                return (obj, val) => field.SetValue(obj, val);
        }

        throw new InvalidOperationException(
            $"Cannot find a way to set Aggregate<TId>.ETag on type {aggregateType.FullName}. " +
            "The framework's ETag setter shape may have changed; revisit AggregateETagWriter.");
    }
}

