using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace LegacyBackupMigrator
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: LegacyBackupMigrator <input.bak> [output.json] [raindesk.exe]");
                return 1;
            }

            string inputPath = Path.GetFullPath(args[0]);
            string outputPath = args.Length > 1
                ? Path.GetFullPath(args[1])
                : Path.ChangeExtension(inputPath, ".json");
            string raindeskExe = args.Length > 2
                ? Path.GetFullPath(args[2])
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "raindesk.exe");

            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine("Input not found: " + inputPath);
                return 1;
            }

            if (!File.Exists(raindeskExe))
            {
                Console.Error.WriteLine("raindesk.exe not found: " + raindeskExe);
                return 1;
            }

            try
            {
                Assembly raindeskAssembly = Assembly.LoadFrom(raindeskExe);
                AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
                    e.Name.StartsWith("raindesk,", StringComparison.Ordinal) ? raindeskAssembly : null;

                Type appDataType = raindeskAssembly.GetType("raindesk.ViewModel.AppData", true);
                var formatter = new BinaryFormatter
                {
                    Binder = new raindeskVersionBinder(raindeskAssembly)
                };

                object appData;
                using (var stream = File.OpenRead(inputPath))
                {
                    appData = formatter.Deserialize(stream);
                }

                if (appData == null || !appDataType.IsInstanceOfType(appData))
                {
                    Console.Error.WriteLine("Backup did not contain raindesk AppData.");
                    return 1;
                }

                int menuCount = 0;
                int iconCount = 0;
                var menuListMember = GetMember(appDataType, "MenuList", "menuList");
                if (menuListMember != null)
                {
                    foreach (var menu in (System.Collections.IEnumerable)GetValue(appData, menuListMember))
                    {
                        menuCount++;
                        if (menu == null) continue;
                        var iconMember = GetMember(menu.GetType(), "IconList", "iconList");
                        if (iconMember == null) continue;
                        var icons = GetValue(menu, iconMember) as System.Collections.ICollection;
                        if (icons != null) iconCount += icons.Count;
                    }
                }

                string json = JsonConvert.SerializeObject(appData, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    Error = (_, error) => error.ErrorContext.Handled = true
                });
                File.WriteAllText(outputPath, json);

                Console.WriteLine("Imported {0} menus, {1} shortcuts.", menuCount, iconCount);
                Console.WriteLine("Saved: " + outputPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Import failed: " + ex.Message);
                return 1;
            }
        }

        private static MemberInfo GetMember(Type type, string propertyName, string fieldName)
        {
            return (MemberInfo)type.GetProperty(propertyName) ?? type.GetField(fieldName);
        }

        private static object GetValue(object target, MemberInfo member)
        {
            return member is PropertyInfo property ? property.GetValue(target) : ((FieldInfo)member).GetValue(target);
        }

        private sealed class raindeskVersionBinder : SerializationBinder
        {
            private readonly Assembly _assembly;

            public raindeskVersionBinder(Assembly assembly)
            {
                _assembly = assembly;
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName.StartsWith("raindesk.", StringComparison.Ordinal))
                {
                    string shortName = typeName.Split(',')[0].Trim();
                    int plus = shortName.LastIndexOf('+');
                    if (plus > 0)
                    {
                        string outer = shortName.Substring(0, plus);
                        string nested = shortName.Substring(plus + 1);
                        Type outerType = _assembly.GetType(outer, true);
                        return outerType.GetNestedType(nested, BindingFlags.Public | BindingFlags.NonPublic);
                    }

                    return _assembly.GetType(shortName, true);
                }

                return null;
            }
        }
    }
}
