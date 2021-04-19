using System;
using System.Text;
using System.Collections.Generic;

public static partial class ArchetypeEcs {

// Internal vars
///////////////////////////

static StringBuilder sb = new StringBuilder();

// Public methods
///////////////////////////

public static string ArrayToString<T>(T[] arr) {
  return ArrayToString<T>(arr, arr.Length);
}

public static string ArrayToString<T>(T[] arr, int length) {
    sb.Clear();
    sb.Append("[");
    for (var i = 0; i < length; i++) {
      sb.Append(arr[i].ToString());
      if (i != length - 1) sb.Append(", ");
    }
    sb.Append("]");
  return sb.ToString();
}

}
