using System;

namespace raindesk.ViewModel;

/// <summary>
/// 强类型 ID 值对象，替代松散的 string/int ID。
/// 使用 record struct 实现值语义和零分配比较。
///
/// 用法示例:
///   MenuId id = MenuId.New();
///   if (menu.MenuId == id.Value) { ... }
///
/// 注意：现有模型（MenuInfo, IconInfo, ToDoInfo）的 ID 属性仍为 string，
/// 此类型用于新代码中的类型安全场景。待后续全面迁移。
/// </summary>

/// <summary>
/// 菜单/分组 ID。
/// </summary>
public readonly record struct MenuId(string Value)
{
    public static MenuId New() => new(Guid.NewGuid().ToString());
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static implicit operator string(MenuId id) => id.Value;
    public static implicit operator MenuId(string value) => new(value);
}

/// <summary>
/// 图标/快捷方式 ID。
/// </summary>
public readonly record struct IconId(string Value)
{
    public static IconId New() => new(Guid.NewGuid().ToString());
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static implicit operator string(IconId id) => id.Value;
    public static implicit operator IconId(string value) => new(value);
}

/// <summary>
/// 待办事项 ID。
/// </summary>
public readonly record struct ToDoId(string Value)
{
    public static ToDoId New() => new(Guid.NewGuid().ToString());
    public override string ToString() => Value;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static implicit operator string(ToDoId id) => id.Value;
    public static implicit operator ToDoId(string value) => new(value);
}
