using System;
using System.Collections.Generic;

public static partial class ArchetypeEcs {

// Constants
///////////////////////////

const int INITIAL_CAPACITY = 4;

// Classes
///////////////////////////

public class SparseSet<K, V> {

  // Fields
  ///////////////////////////

  public Func<K,int> indexer;

  public Action<int,int> onMoveValue;

  public int[] forwardLookup;

  public int[] reverseLookup;

  public K[] keys;

  public V[] values;

  public int nextValueIndex;

  // Constructors
  ///////////////////////////

  public SparseSet(Func<K, int> indexer, Action<int, int> onMoveValue = null) {
    this.indexer = indexer;
    this.onMoveValue = onMoveValue;
    forwardLookup = new int[INITIAL_CAPACITY];
    Array.Fill(forwardLookup, -1);
    reverseLookup = new int[INITIAL_CAPACITY];
    Array.Fill(reverseLookup, -1);
    keys = new K[INITIAL_CAPACITY];
    values = new V[INITIAL_CAPACITY];
    nextValueIndex = 0;
  }

}

// Public methods
///////////////////////////

public static void Add<K, V>(this SparseSet<K, V> ss, K key, V value) {
  var keyIndex = ss.indexer(key);
  if (keyIndex >= ss.forwardLookup.Length) {
    GrowAndFill(ref ss.forwardLookup, keyIndex, -1);
  }
  if (ss.nextValueIndex >= ss.values.Length) {
    GrowAndFill(ref ss.reverseLookup, ss.nextValueIndex, -1);
    Array.Resize(ref ss.keys, ss.reverseLookup.Length);
    Array.Resize(ref ss.values, ss.reverseLookup.Length);
  }
  ss.keys[ss.nextValueIndex] = key;
  ss.values[ss.nextValueIndex] = value;
  ss.reverseLookup[ss.nextValueIndex] = keyIndex;
  ss.forwardLookup[keyIndex] = ss.nextValueIndex;
  ss.nextValueIndex++;
}

public static void Remove<K, V>(this SparseSet<K, V> ss, K key) {
  var keyIndex = ss.indexer(key);
  if (keyIndex >= ss.forwardLookup.Length) return;
  if (ss.forwardLookup[keyIndex] == -1) return;
  var valueToRemove = ss.forwardLookup[keyIndex];
  if (ss.nextValueIndex != 0) {
    var valueToSwap = --ss.nextValueIndex;
    var keyIndexToSwap = ss.reverseLookup[valueToSwap];
    ss.keys[valueToRemove] = ss.keys[valueToSwap];
    ss.values[valueToRemove] = ss.values[valueToSwap];
    if (ss.onMoveValue != null) ss.onMoveValue(valueToSwap, valueToRemove);
    ss.reverseLookup[valueToRemove] = keyIndexToSwap;
    ss.forwardLookup[keyIndexToSwap] = valueToRemove;
    ss.reverseLookup[valueToSwap] = -1;
  } else {
    ss.reverseLookup[valueToRemove] = -1;
  }
  ss.forwardLookup[keyIndex] = -1;
}

public static bool Has<K, V>(this SparseSet<K, V> ss, K key) {
  var keyIndex = ss.indexer(key);
  if (keyIndex >= ss.forwardLookup.Length) return false;
  return ss.forwardLookup[keyIndex] != -1;
}

public static ref V Get<K, V>(this SparseSet<K, V> ss, K key) {
  return ref ss.values[ss.forwardLookup[ss.indexer(key)]];
}

public static int Size<K, V>(this SparseSet<K, V> ss) {
  return ss.nextValueIndex;
}

public static K At<K, V>(this SparseSet<K, V> ss, int index) {
  return ss.keys[index];
}

public static ref V GetAt<K, V>(this SparseSet<K, V> ss, int index) {
  return ref ss.Get(ss.At(index));
}

// Internal methods
///////////////////////////

static void GrowAndFill<T>(ref T[] array, int index, T value) {
  var oldLength = array.Length;
  var newSize = array.Length;
  while (newSize <= index) newSize *= 2;
  Array.Resize(ref array, newSize);
  Array.Fill(array, value, oldLength, array.Length - oldLength);
}

}
