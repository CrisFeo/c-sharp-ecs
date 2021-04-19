using System;
using System.Collections.Generic;
using System.Reflection;

public static partial class ArchetypeEcs {

// Classes
///////////////////////////

public static class TypeId {

  // Internal classes
  ///////////////////////////

  static class Id<T> {

    // Public vars
    ///////////////////////////

    public static int index;

    // Constructors
    ///////////////////////////

    static Id() {
      index = TypeId.Next();
    }

  }

  // Public properties
  ///////////////////////////

  public static int Count { get; private set; }

  // Public methods
  ///////////////////////////

  public static int Next() {
    return Count++;
  }

  public static int Get<T>() {
    return Id<T>.index;
  }

  public static int GetReflected(Type type) {
    var genericId = typeof(Id<>);
    var typeId = genericId.MakeGenericType(new[] { type });
    var idField = typeId.GetField("index");
    return (int)idField.GetValue(null);
  }

}

}
