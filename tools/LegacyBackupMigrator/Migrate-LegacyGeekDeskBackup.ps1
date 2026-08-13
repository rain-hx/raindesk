param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$LegacyAppPath
)

$ErrorActionPreference = 'Stop'

$inputFile = Get-Item -LiteralPath $InputPath
$legacyExe = Get-Item -LiteralPath $LegacyAppPath
$legacyDirectory = $legacyExe.DirectoryName
$newtonsoftPath = Join-Path $legacyDirectory 'lib\Newtonsoft.Json.dll'
if (-not (Test-Path -LiteralPath $newtonsoftPath)) {
    throw "Missing legacy Newtonsoft.Json.dll: $newtonsoftPath"
}

$assemblyName = [Reflection.AssemblyName]::GetAssemblyName($legacyExe.FullName)
if ($assemblyName.Name -ne 'GeekDesk' -or $assemblyName.Version.ToString() -ne '2.5.1.5') {
    throw "Expected GeekDesk 2.5.1.5, found $($assemblyName.FullName)"
}

[void][Reflection.Assembly]::LoadFrom($newtonsoftPath)
$legacyAssembly = [Reflection.Assembly]::LoadFrom($legacyExe.FullName)

if (-not ('LegacyGeekDeskAllowListBinder' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

public sealed class LegacyGeekDeskAllowListBinder : SerializationBinder
{
    private readonly System.Reflection.Assembly legacyAssembly;
    private static readonly HashSet<string> LegacyTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "GeekDesk.ViewModel.AppData",
        "GeekDesk.ViewModel.AppConfig",
        "GeekDesk.ViewModel.GradientBGParam",
        "GeekDesk.ViewModel.IconInfo",
        "GeekDesk.ViewModel.MenuInfo",
        "GeekDesk.ViewModel.ToDoInfo",
        "GeekDesk.Constant.AppHideType",
        "GeekDesk.Constant.BGStyle",
        "GeekDesk.Constant.IconType",
        "GeekDesk.Constant.MenuType",
        "GeekDesk.Constant.SearchType",
        "GeekDesk.Constant.SortType",
        "GeekDesk.Constant.UpdateType",
        "GeekDesk.Util.GlobalHotKey+HotkeyModifiers"
    };

    public LegacyGeekDeskAllowListBinder(System.Reflection.Assembly legacyAssembly)
    {
        if (legacyAssembly == null)
        {
            throw new ArgumentNullException("legacyAssembly");
        }

        this.legacyAssembly = legacyAssembly;
    }

    public override Type BindToType(string assemblyName, string typeName)
    {
        Type resolved;
        if (typeName.StartsWith("GeekDesk.", StringComparison.Ordinal))
        {
            resolved = legacyAssembly.GetType(typeName, true, false);
        }
        else
        {
            resolved = Type.GetType(typeName + ", " + assemblyName, true, false);
        }

        if (!IsAllowed(resolved))
        {
            throw new SerializationException("Blocked legacy backup type: " + typeName);
        }

        return resolved;
    }

    private bool IsAllowed(Type type)
    {
        if (type == null) return false;
        if (type.Assembly == legacyAssembly) return LegacyTypes.Contains(type.FullName);
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(byte[])) return true;
        if (type.IsArray) return IsAllowed(type.GetElementType());
        if (type.FullName == "System.Windows.Visibility"
            || type.FullName == "System.Windows.Input.Key") return true;
        if (type.FullName != null
            && type.FullName.StartsWith(
                "System.Collections.ObjectModel.ObservableCollection`1+SimpleMonitor",
                StringComparison.Ordinal)) return true;
        if (!type.IsGenericType) return false;

        Type definition = type.GetGenericTypeDefinition();
        if (definition != typeof(List<>)
            && definition != typeof(Nullable<>)
            && definition.FullName != "System.Collections.ObjectModel.ObservableCollection`1")
        {
            return false;
        }

        foreach (Type argument in type.GetGenericArguments())
        {
            if (!IsAllowed(argument)) return false;
        }

        return true;
    }
}
'@
}

if (-not ('LegacyModelFieldContractResolver' -as [type])) {
    Add-Type -ReferencedAssemblies $newtonsoftPath -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public sealed class LegacyModelFieldContractResolver : DefaultContractResolver
{
    private readonly Assembly legacyAssembly;

    public LegacyModelFieldContractResolver(Assembly legacyAssembly)
    {
        if (legacyAssembly == null)
        {
            throw new ArgumentNullException("legacyAssembly");
        }

        this.legacyAssembly = legacyAssembly;
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        if (type.Assembly != legacyAssembly)
        {
            return base.CreateProperties(type, memberSerialization);
        }

        return type
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => !field.IsNotSerialized && !typeof(Delegate).IsAssignableFrom(field.FieldType))
            .Select(field =>
            {
                JsonProperty property = base.CreateProperty(field, MemberSerialization.Fields);
                property.Readable = true;
                property.Writable = false;
                return property;
            })
            .ToList();
    }
}
'@
}

$formatter = New-Object System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
$formatter.Binder = New-Object LegacyGeekDeskAllowListBinder -ArgumentList $legacyAssembly
$stream = [IO.File]::Open($inputFile.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    $legacyData = $formatter.Deserialize($stream)
}
finally {
    $stream.Dispose()
}

if ($null -eq $legacyData -or $legacyData.GetType().FullName -ne 'GeekDesk.ViewModel.AppData') {
    throw 'The backup did not contain a GeekDesk.ViewModel.AppData root object.'
}

$settings = New-Object Newtonsoft.Json.JsonSerializerSettings
$settings.Formatting = [Newtonsoft.Json.Formatting]::Indented
$settings.ReferenceLoopHandling = [Newtonsoft.Json.ReferenceLoopHandling]::Ignore
$settings.TypeNameHandling = [Newtonsoft.Json.TypeNameHandling]::None
$settings.NullValueHandling = [Newtonsoft.Json.NullValueHandling]::Include
$settings.ContractResolver = New-Object LegacyModelFieldContractResolver -ArgumentList $legacyAssembly
$json = [Newtonsoft.Json.JsonConvert]::SerializeObject($legacyData, $settings)

$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [IO.Path]::GetDirectoryName($outputFullPath)
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

$utf8WithoutBom = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText($outputFullPath, $json, $utf8WithoutBom)

$menuCount = @($legacyData.MenuList).Count
$iconCount = 0
foreach ($menu in @($legacyData.MenuList)) {
    $iconCount += @($menu.IconList).Count
}
$todoCount = @($legacyData.ToDoList).Count
$historyTodoCount = @($legacyData.HiToDoList).Count

[pscustomobject]@{
    OutputPath = $outputFullPath
    MenuCount = $menuCount
    IconCount = $iconCount
    TodoCount = $todoCount
    HistoryTodoCount = $historyTodoCount
    JsonBytes = $utf8WithoutBom.GetByteCount($json)
} | ConvertTo-Json -Compress
