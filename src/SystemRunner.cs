using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static partial class ArchetypeEcs {

// Classes
///////////////////////////

public static class SystemRunner {

  // Structs
  ///////////////////////////

  struct ArgumentData {
    public bool isReference;
    public bool isResource;
    public string name;
    public string type;
    public string baseType;
  }

  // Public methods
  ///////////////////////////

  public static Action Create(Ecs.Internals ecs, MethodInfo systemInfo) {
    var systemName = $"{systemInfo.DeclaringType}.{systemInfo.Name}";
    var arguments = systemInfo.GetParameters()
      .Select(p => GetArgumentData(ecs, p))
      .ToArray();
    if (arguments.Length == 0) return () => systemInfo.Invoke(null, null);
    var resources = arguments
      .Where(a => a.isResource)
      .ToArray();
    var components = arguments
      .Where(a => !a.isResource)
      .ToArray();
    var src = default(string);
    if (components.Length > 0) {
      src = ForEachSource(ecs, arguments, resources, components);
    } else {
      src = ResourceOnlySource(ecs, resources);
    }
    var asmGen = new AssemblyGenerator();
    asmGen.ReferenceAssemblyByName("System");
    asmGen.ReferenceAssemblyByName("System.Collections");
    asmGen.ReferenceAssemblyByName("System.Reflection");
    asmGen.ReferenceAssemblyContainingType(typeof(Ecs));
    src = asmGen.Format(src);
    var startTime = TimeNow();
    var (assembly, errors) = asmGen.Generate(src);
    if (errors != null) {
      var sb = new System.Text.StringBuilder();
      sb.Append($"encountered error(s) compiling system runner for {systemName}");
      foreach (var error in errors) {
        sb.Append($"\n  {error.Replace("\n", "\n  ")}");
      }
      var srcLines = src.Split("\n");
      for (var i = 0; i < srcLines.Length; i++) {
        sb.Append($"\n{i+1}\t{srcLines[i]}");
      }
      throw new Exception(sb.ToString());
    }
    var runner = assembly.GetType("Runner");
    var duration = TimeNow() - startTime;
    Console.WriteLine($"generated system runner for {systemName} in {duration}ms:\n  {src.Replace("\n", "\n  ")}");
    runner.GetMethod("Initialize").Invoke(null, new object[] { ecs, systemInfo });
    return (Action)runner.GetMethod("Run").CreateDelegate(typeof(Action));
  }

  // Internal methods
  ///////////////////////////

  static string ForEachSource(
    Ecs.Internals ecs,
    ArgumentData[] arguments,
    ArgumentData[] resources,
    ArgumentData[] components
  ) {
    return $@"
      using System;
      using System.Reflection;

      using static ArchetypeEcs;

      public delegate void SystemDelegate(
        {LinesFor(0, arguments.Length, ",\n", i => $"{arguments[i].type} {arguments[i].name}")}
      );

      public static class Runner {{

        static Ecs.Internals ecs;
        static SystemDelegate system;
        static EntityId[] componentIds;
        static EntityType entityType;

        public static void Initialize(Ecs.Internals ecsInstance, MethodInfo systemInfo) {{
          ecs = ecsInstance;
          system = (SystemDelegate)systemInfo.CreateDelegate(typeof(SystemDelegate));
          componentIds = new EntityId[]{{
          {LinesFor(0, components.Length, ",\n", i => $@"
            ecs.GetDataTypeReflected(typeof({components[i].baseType}))
          ")}
          }};
          entityType = new EntityType(componentIds);
        }}

        public static void Run() {{
          {LinesFor(0, resources.Length, "\n", i => {
            var refPrefix = resources[i].isReference ? "ref " : "";
            return $@"{refPrefix}var {resources[i].name}Resource = {refPrefix}ecs.GetResource<{resources[i].baseType}>();";
          })}
          foreach (var (archetype, row) in ecs.GetIndex().All(entityType)) {{
            var e = archetype.GetEntity(row);
            {LinesFor(0, components.Length, "\n", i => {
              var refPrefix = components[i].isReference ? "ref " : "";
              return $@"
                {refPrefix}var {components[i].name}Data = {refPrefix}archetype.GetData<{components[i].baseType}>(
                  row,
                  componentIds[{i}]
                );
              ";
            })}
            system(
            {LinesFor(0, arguments.Length, ",\n", i => {
              if (arguments[i].isResource) {
                var resource = $"{arguments[i].name}Resource";
                if (arguments[i].isReference) {
                  return "ref " + resource;
                }
                return resource;
              } else {
                var data = $"{arguments[i].name}Data";
                if (arguments[i].isReference) {
                  return "ref " + data;
                }
                return data;
              }
            })}
            );
          }}
        }}

      }}
    ";
  }

  static string ResourceOnlySource(Ecs.Internals ecs, ArgumentData[] arguments) {
    return $@"
      using System;
      using System.Reflection;

      using static ArchetypeEcs;

      public delegate void SystemDelegate(
        {LinesFor(0, arguments.Length, ",\n", i => $"{arguments[i].type} {arguments[i].name}")}
      );

      public static class Runner {{

        static Ecs.Internals ecs;
        static SystemDelegate system;

        public static void Initialize(Ecs.Internals ecsInstance, MethodInfo systemInfo) {{
          ecs = ecsInstance;
          system = (SystemDelegate)systemInfo.CreateDelegate(typeof(SystemDelegate));
        }}

        public static void Run() {{
          system(
          {LinesFor(0, arguments.Length, ",\n", i => {
            var refPrefix = arguments[i].isReference ? "ref " : "";
            return $@"{refPrefix}ecs.GetResource<{arguments[i].baseType}>()";
          })}
          );
        }}

      }}
    ";
  }

  static string LinesFor(int from, int to, string sep, Func<int, string> fn) {
    return string.Join(sep, Enumerable.Range(from, to).Select(fn));
  }

  static string LinesFor(int from, int to, string sep, Func<int, string[]> fn) {
    return string.Join(sep, Enumerable.Range(from, to).SelectMany(fn));
  }

  static ArgumentData GetArgumentData(Ecs.Internals ecs, ParameterInfo info) {
    var type = info.ParameterType.FullName.Replace("+", ".");
    var baseType = type;
    var isResource = ecs.HasResource(info.ParameterType);
    if (info.ParameterType.IsByRef) {
      var elementType = info.ParameterType.GetElementType();
      type = "ref " + elementType.FullName.Replace("+", ".");
      baseType = elementType.FullName.Replace("+", ".");
      isResource = ecs.HasResource(elementType);
    }
    return new ArgumentData {
      isReference = info.ParameterType.IsByRef,
      isResource = isResource,
      name = info.Name,
      type = type,
      baseType = baseType,
    };
  }

  static double TimeNow() {
    return System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;
  }

}

}
