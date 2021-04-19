using System;
using System.Collections.Generic;

public static partial class ArchetypeEcs {

// Constants
///////////////////////////

const int MINIMUM_FREE = 1024;

// Classes
///////////////////////////

public class EntityTracker {

  // Fields
  ///////////////////////////

  public List<uint> generations;

  public Queue<int> free;

  // Constructors
  ///////////////////////////

  public EntityTracker() {
    generations = new List<uint>();
    free = new Queue<int>();
  }

}

// Public methods
///////////////////////////

public static EntityId Create(this EntityTracker m) {
  var index = 0;
  if (m.free.Count > MINIMUM_FREE) {
    index = m.free.Dequeue();
  } else {
    m.generations.Add(0);
    index = m.generations.Count - 1;
  }
  return NewEntityId(index, m.generations[index]);
}

public static bool Valid(this EntityTracker m, EntityId e) {
  return m.generations[e.Index()] == e.Generation();
}

public static void Destroy(this EntityTracker m, EntityId e) {
  var idx = e.Index();
  m.generations[idx]++;
  m.free.Enqueue(idx);
}

}
