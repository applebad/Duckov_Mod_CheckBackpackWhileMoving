using Duckov.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CheckBackpackWhileMoving
{
    public class CheckBackpackWhileMoving
    {
        public static bool disableAttack { get; set; }
        public static GameObject? currentLootBox { get; set; }
        public CheckBackpackWhileMoving()
        {
            disableAttack = false;
        }
        public void ClearCurrentLootBox()
        {
            currentLootBox = null;
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl))]
    public class CharacterMainCorolPatch
    {

        [HarmonyPatch("GetInteractableTargetToInteract")]
        [HarmonyPostfix]
        static void GetInteractableTargetToInteract(InteractableBase __result)
        {
            if (__result != null && __result.gameObject != null)
            {
                string targetName = __result.gameObject.name;
                //Debug.Log("获取到的可交互目标"+ targetName);//包含LootBox为战利品箱或搜索箱

                if (targetName != null && targetName.Contains("LootBox"))
                {
                    CheckBackpackWhileMoving.currentLootBox = __result.gameObject;
                }
            }
        }

        [HarmonyPatch("IsAiming")]
        [HarmonyPostfix]
        static bool IsAiming_Prefix(bool __result)
        {
            try
            {
                if (CheckBackpackWhileMoving.disableAttack)
                {
                    __result = false;
                    return false; // 跳过原方法
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
                return true;
            }
            return true; // 执行原方法
        }
        [HarmonyPatch("CanControlAim")]
        [HarmonyPostfix]
        static bool CanControlAim_Prefix(bool __result)
        {
            try
            {
                if (CheckBackpackWhileMoving.disableAttack)
                {
                    __result = false;
                    return false; // 跳过原方法
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
                return true;
            }
            return true; // 执行原方法
        }
    }

    [HarmonyPatch(typeof(GameCamera))]
    public class GameCameraPatch
    {
        [HarmonyPatch("UpdateAimOffsetNormal")]
        [HarmonyPrefix]
        static bool UpdateAimOffsetNormal_Prefix()
        {
            // 只阻止鼠标偏移计算，但允许相机其他更新
            return !CheckBackpackWhileMoving.disableAttack;
        }

        [HarmonyPatch("UpdateAimOffsetUsingBound")]
        [HarmonyPrefix]
        static bool UpdateAimOffsetUsingBound_Prefix()
        {
            // 只阻止边界偏移计算，但允许相机其他更新
            return !CheckBackpackWhileMoving.disableAttack;
        }
    }

    //屏蔽触发器
    [HarmonyPatch(typeof(InputManager))]
    public class InputManagerPatch
    {
        [HarmonyPatch("SetTrigger")]
        [HarmonyPrefix]
        static void Trigger_Prefix(ref bool trigger, ref bool triggerThisFrame, ref bool releaseThisFrame)
        {
            try
            {
                if (!CheckBackpackWhileMoving.disableAttack)
                    return;
                trigger = false;
                triggerThisFrame = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
            }
        }

        [HarmonyPatch("SetAimInputUsingMouse")]
        [HarmonyPrefix]
        static bool SetAimInputUsingMouse_Prefix(Vector2 mouseDelta)
        {
            // 当背包打开时，阻止鼠标瞄准输入处理
            if (CheckBackpackWhileMoving.disableAttack)
            {
                return false; // 跳过原方法
            }
            return true; // 执行原方法
        }
    }


    [HarmonyPatch(typeof(View))]
    public class ViewPatch
    {
        private static Dictionary<Type, bool> viewHasTabsCache = new Dictionary<Type, bool>();
        private static FieldInfo viewTabsField;

        static ViewPatch()
        {
            viewTabsField = AccessTools.Field(typeof(View), "viewTabs");
        }
        public static void ClearViewHasTabsCache()
        {
            viewHasTabsCache.Clear();
        }
        [HarmonyPatch("OnOpen")]
        [HarmonyPostfix]
        static void OnOpenPatch(View __instance)
        {
            try
            {
                CheckBackpackWhileMoving.disableAttack = true;
                InputManager.ActiveInput(__instance.gameObject);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
            }
        }

        [HarmonyPatch("OnClose")]
        [HarmonyPostfix]
        public static void OnClose_Postfix()
        {
            CheckBackpackWhileMoving.disableAttack = false;
        }
    }
}
