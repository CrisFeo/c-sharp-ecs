using System;
using System.Collections.Generic;
using System.Reflection;

public static partial class ArchetypeEcs {

// Classes
///////////////////////////

public class Ecs {

  // Internal classes
  ///////////////////////////

  class Resource<T> {
    public T value;
  }

  // Internal structs
  ///////////////////////////

  public struct Internals {

    // Internal vars
    ///////////////////////////

    public EntityIndex index;
    public EntityId[] dataTypes;
    public Dictionary<Type, object> resources;

    // Public methods
    ///////////////////////////

    public EntityIndex GetIndex() {
      return index;
    }

    public EntityId GetDataTypeReflected(Type type) {
      var typeIndex = TypeId.GetReflected(type);
      return dataTypes[typeIndex];
    }

    public bool HasResource(Type type) {
      return resources.ContainsKey(type);
    }

    public ref T GetResource<T>() {
      var wrapper = (Resource<T>)resources[typeof(T)];
      return ref wrapper.value;
    }

  }

  // Internal vars
  ///////////////////////////

  EntityTracker tracker = new EntityTracker();
  EntityIndex index = new EntityIndex();
  EntityId[] dataTypes = new EntityId[1];
  Dictionary<Type, object> resources = new Dictionary<Type, object>();
  List<Action> systems = new List<Action>();

  // Public methods
  ///////////////////////////

  public EntityId RegisterDataType<T>() {
    var type = typeof(T);
    var component = tracker.Create();
    index.Register(component, type);
    var typeIndex = TypeId.Get<T>();
    if (dataTypes.Length <= typeIndex) {
      var newSize = dataTypes.Length * 2;
      while (newSize <= typeIndex) newSize *= 2;
      Array.Resize(ref dataTypes, newSize);
    }
    dataTypes[typeIndex] = component;
    return component;
  }

  public ref T RegisterResource<T>() {
    var wrapper = new Resource<T> {
      value = Activator.CreateInstance<T>(),
    };
    resources[typeof(T)] = wrapper;
    return ref wrapper.value;
  }

  public void RegisterSystem(
    Type systemType,
    string systemName,
    BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static
  ) {
    var systemInfo = systemType.GetMethod(systemName, bindingFlags);
    systems.Add(ArchetypeEcs.SystemRunner.Create(this.GetInternals(), systemInfo));
  }

  public void Step() {
    foreach (var s in systems) s.Invoke();
  }

  public EntityId CreateEntity() {
    return tracker.Create();
  }

  public void DestroyEntity(EntityId entity) {
    index.RemoveEntity(entity);
    tracker.Destroy(entity);
  }

  public void RemoveComponents(
    EntityId entity,
    params EntityId[] components
  ) {
    index.Remove(entity, components);
  }

  public void AddComponent<A>(
    EntityId entity,
    A dataA
  ) {
    index.Add(entity, new (EntityId, object)[] {
      dataA is EntityId eidA ? (eidA, null) : (GetDataType<A>(), dataA),
    });
  }

  public void AddComponents<A, B>(
    EntityId entity,
    A dataA,
    B dataB
  ) {
    index.Add(entity, new (EntityId, object)[] {
      dataA is EntityId eidA ? (eidA, null) : (GetDataType<A>(), dataA),
      dataB is EntityId eidB ? (eidB, null) : (GetDataType<B>(), dataB),
    });
  }

  public void AddComponents<A, B, C>(
    EntityId entity,
    A dataA,
    B dataB,
    C dataC
  ) {
    index.Add(entity, new (EntityId, object)[] {
      dataA is EntityId eidA ? (eidA, null) : (GetDataType<A>(), dataA),
      dataB is EntityId eidB ? (eidB, null) : (GetDataType<B>(), dataB),
      dataC is EntityId eidC ? (eidC, null) : (GetDataType<C>(), dataC),
    });
  }

  // Internal methods
  ///////////////////////////

  internal Internals GetInternals() {
    return new Internals {
      index = index,
      dataTypes = dataTypes,
      resources = resources,
    };
  }


  EntityId GetDataType<T>() {
    return dataTypes[TypeId.Get<T>()];
  }

}

}
