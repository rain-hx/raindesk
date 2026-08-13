using System;
using System.Collections.Generic;

namespace rainspace.Util
{
    /// <summary>
    /// 操作记录
    /// </summary>
    public class UndoAction
    {
        public string Description { get; set; }
        public Action Undo { get; set; }
        public Action Redo { get; set; }
        public DateTime Timestamp { get; set; }

        public UndoAction(string description, Action undo, Action redo)
        {
            Description = description;
            Undo = undo;
            Redo = redo;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 撤销/重做辅助工具类
    /// </summary>
    public static class UndoHelper
    {
        private static readonly Stack<UndoAction> _undoStack = new Stack<UndoAction>();
        private static readonly Stack<UndoAction> _redoStack = new Stack<UndoAction>();
        private static readonly object _lock = new object();
        private const int MaxHistorySize = 50;

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public static bool CanUndo { get { lock (_lock) { return _undoStack.Count > 0; } } }

        /// <summary>
        /// 是否可以重做
        /// </summary>
        public static bool CanRedo { get { lock (_lock) { return _redoStack.Count > 0; } } }

        /// <summary>
        /// 获取撤销历史
        /// </summary>
        public static IEnumerable<UndoAction> UndoHistory { get { lock (_lock) { return _undoStack.ToArray(); } } }

        /// <summary>
        /// 获取重做历史
        /// </summary>
        public static IEnumerable<UndoAction> RedoHistory { get { lock (_lock) { return _redoStack.ToArray(); } } }

        /// <summary>
        /// 记录操作
        /// </summary>
        public static void RecordAction(string description, Action undo, Action redo)
        {
            lock (_lock)
            {
                var action = new UndoAction(description, undo, redo);
                _undoStack.Push(action);

                // 清空重做栈
                _redoStack.Clear();

                // 限制历史大小
                if (_undoStack.Count > MaxHistorySize)
                {
                    var temp = new Stack<UndoAction>();
                    foreach (var item in _undoStack)
                    {
                        temp.Push(item);
                    }
                    _undoStack.Clear();
                    for (int i = 0; i < MaxHistorySize && temp.Count > 0; i++)
                    {
                        _undoStack.Push(temp.Pop());
                    }
                }
            }
        }

        /// <summary>
        /// 撤销操作
        /// </summary>
        public static bool Undo()
        {
            UndoAction action;
            lock (_lock)
            {
                if (_undoStack.Count == 0) return false;
                action = _undoStack.Pop();
                _redoStack.Push(action);
            }

            action.Undo?.Invoke();
            ToastHelper.Info($"已撤销：{action.Description}");
            return true;
        }

        /// <summary>
        /// 重做操作
        /// </summary>
        public static bool Redo()
        {
            UndoAction action;
            lock (_lock)
            {
                if (_redoStack.Count == 0) return false;
                action = _redoStack.Pop();
                _undoStack.Push(action);
            }

            action.Redo?.Invoke();
            ToastHelper.Info($"已重做：{action.Description}");
            return true;
        }

        /// <summary>
        /// 清空历史
        /// </summary>
        public static void ClearHistory()
        {
            lock (_lock)
            {
                _undoStack.Clear();
                _redoStack.Clear();
            }
        }

        /// <summary>
        /// 获取上次操作描述
        /// </summary>
        public static string GetLastActionDescription()
        {
            lock (_lock)
            {
                if (_undoStack.Count > 0)
                {
                    return _undoStack.Peek().Description;
                }
                return null;
            }
        }
    }
}
