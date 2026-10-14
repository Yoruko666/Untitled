/*
 * EventCenter：全局事件中心，支持 0~3 个参数的订阅和调用
 *
 * 用法：
 *   // 无参数
 *   EventCenter.Subscribe(GameEventType.GameStart, OnGameStart);
 *   EventCenter.Unsubscribe(GameEventType.GameStart, OnGameStart);
 *   EventCenter.Invoke(GameEventType.GameStart);
 *
 *   // 含参数
 *   EventCenter.Subscribe<int, Vector2>(GameEventType.PlayerDamaged, OnPlayerDamaged);
 *   EventCenter.Invoke(GameEventType.PlayerDamaged, 10, dir);
 *   private void OnPlayerDamaged(int damage, Vector2 direction) { }
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    public static class EventCenter
    {
        private static readonly Dictionary<GameEventType, Delegate> handlers = new();

        public static void Clear() => handlers.Clear();

        // ---------- 0 个参数 ----------

        public static void Subscribe(GameEventType eventType, Action handler)
        {
            Add(eventType, handler, typeof(Action));
        }

        public static void Unsubscribe(GameEventType eventType, Action handler)
        {
            Remove(eventType, handler, typeof(Action));
        }

        public static void Invoke(GameEventType eventType)
        {
            if (!TryGetInvocationList(eventType, typeof(Action), out Delegate[] invocationList)) return;

            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action)invocationList[i]).Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // ---------- 1 个参数 ----------

        public static void Subscribe<T1>(GameEventType eventType, Action<T1> handler)
        {
            Add(eventType, handler, typeof(Action<T1>));
        }

        public static void Unsubscribe<T1>(GameEventType eventType, Action<T1> handler)
        {
            Remove(eventType, handler, typeof(Action<T1>));
        }

        public static void Invoke<T1>(GameEventType eventType, T1 arg1)
        {
            if (!TryGetInvocationList(eventType, typeof(Action<T1>), out Delegate[] invocationList)) return;

            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<T1>)invocationList[i]).Invoke(arg1);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // ---------- 2 个参数 ----------

        public static void Subscribe<T1, T2>(GameEventType eventType, Action<T1, T2> handler)
        {
            Add(eventType, handler, typeof(Action<T1, T2>));
        }

        public static void Unsubscribe<T1, T2>(GameEventType eventType, Action<T1, T2> handler)
        {
            Remove(eventType, handler, typeof(Action<T1, T2>));
        }

        public static void Invoke<T1, T2>(GameEventType eventType, T1 arg1, T2 arg2)
        {
            if (!TryGetInvocationList(eventType, typeof(Action<T1, T2>), out Delegate[] invocationList)) return;

            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<T1, T2>)invocationList[i]).Invoke(arg1, arg2);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // ---------- 3 个参数 ----------

        public static void Subscribe<T1, T2, T3>(GameEventType eventType, Action<T1, T2, T3> handler)
        {
            Add(eventType, handler, typeof(Action<T1, T2, T3>));
        }

        public static void Unsubscribe<T1, T2, T3>(GameEventType eventType, Action<T1, T2, T3> handler)
        {
            Remove(eventType, handler, typeof(Action<T1, T2, T3>));
        }

        public static void Invoke<T1, T2, T3>(GameEventType eventType, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!TryGetInvocationList(eventType, typeof(Action<T1, T2, T3>), out Delegate[] invocationList)) return;

            for (int i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<T1, T2, T3>)invocationList[i]).Invoke(arg1, arg2, arg3);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // ---------- 公共逻辑 ----------

        private static void Add(GameEventType eventType, Delegate handler, Type handlerType)
        {
            if (handler == null)
            {
                Debug.LogError($"[EventCenter] 订阅 {eventType} 时传入的处理器为 null。");
                return;
            }

            if (!handlers.TryGetValue(eventType, out Delegate existing))
            {
                handlers[eventType] = handler;
                return;
            }

            if (existing.GetType() != handlerType)
            {
                Debug.LogError($"[EventCenter] 事件 {eventType} 的参数是（{PayloadName(existing.GetType())}），" +
                               $"不能用（{PayloadName(handlerType)}）订阅。");
                return;
            }

            handlers[eventType] = Delegate.Combine(existing, handler);
        }

        private static void Remove(GameEventType eventType, Delegate handler, Type handlerType)
        {
            if (handler == null) return;
            if (!handlers.TryGetValue(eventType, out Delegate existing)) return;
            if (existing.GetType() != handlerType) return;

            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
            {
                handlers.Remove(eventType);
            }
            else
            {
                handlers[eventType] = remaining;
            }
        }

        private static bool TryGetInvocationList(GameEventType eventType, Type handlerType, out Delegate[] invocationList)
        {
            invocationList = null;

            if (!handlers.TryGetValue(eventType, out Delegate existing)) return false;

            if (existing.GetType() != handlerType)
            {
                Debug.LogError($"[EventCenter] 事件 {eventType} 的参数是（{PayloadName(existing.GetType())}），" +
                               $"但本次发布用的是（{PayloadName(handlerType)}）。");
                return false;
            }

            // 调用列表是快照，派发过程中增删订阅不会影响本次派发
            invocationList = existing.GetInvocationList();
            return true;
        }

        private static string PayloadName(Type handlerType)
        {
            if (!handlerType.IsGenericType) return "无参数";

            Type[] payloadTypes = handlerType.GetGenericArguments();
            string name = payloadTypes[0].Name;
            for (int i = 1; i < payloadTypes.Length; i++)
            {
                name += ", " + payloadTypes[i].Name;
            }

            return name;
        }
    }
}
