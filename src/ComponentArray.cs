using System;
using System.Collections.Generic;

public static partial class ArchetypeEcs {

// Interfaces
///////////////////////////

public interface ComponentArray {

  void SetObj(int index, object value);

  object GetObj(int index);

}

// Classes
///////////////////////////

public class ComponentArray<T> : ComponentArray {

  // Fields
  ///////////////////////////

  public T[] data;

  // Constructors
  ///////////////////////////

  public ComponentArray() {
    data = new T[INITIAL_CAPACITY];
  }

  // Public methods
  ///////////////////////////

  public void Set(int index, T value) => GrowAndSet(ref data, index, value);

  public ref T Get(int index) => ref data[index];

  // Implementations
  ///////////////////////////

  public void SetObj(int index, object value) => Set(index, (T)value);

  public object GetObj(int index) => Get(index);

  public override string ToString() =>  $"ComponentArray<{typeof(T).Name}> {data.Length}";

}

// Public methods
///////////////////////////

public static ComponentArray<T> As<T>(this ComponentArray ca) {
  return (ComponentArray<T>)ca;
}

// Internal methods
///////////////////////////

static void GrowAndSet<T>(ref T[] array, int index, T value) {
  var newSize = array.Length;
  while (newSize <= index) newSize *= 2;
  Array.Resize(ref array, newSize);
  array[index] = value;
}

}
