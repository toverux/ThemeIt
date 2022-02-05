using System;
using System.Collections.Generic;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ThemeIt;

internal class Locator {
    private readonly Dictionary<Type, object?> container = new();

    internal static Locator Current { get; } = new();

    internal T? TryFind<T>() {
        return this.container.TryGetValue(typeof(T), out var value) ? (T?) value : default;
    }

    internal T Find<T>() {
        return (T) this.container[typeof(T)]!;
    }

    internal void Register<T>(T value) {
        this.container.Add(typeof(T), value);
    }

    internal void Unregister<T>() {
        this.container.Remove(typeof(T));
    }
}
