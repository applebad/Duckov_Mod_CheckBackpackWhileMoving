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

        // 不阻止 LateUpdate 和 UpdatePosition，这样相机仍然跟随角色
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
            // 当背包打开时，阻止鼠标输入处理
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
                Type viewType = __instance.GetType();

                if (!viewHasTabsCache.TryGetValue(viewType, out bool hasTabs))
                {
                    var viewTabs = viewTabsField?.GetValue(__instance);
                    hasTabs = viewTabs != null;
                    viewHasTabsCache[viewType] = hasTabs;
                }

                if (hasTabs)
                {
                    CheckBackpackWhileMoving.disableAttack = true;
                    InputManager.ActiveInput(__instance.gameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
            }
        }
        ////不懂IL代码，使用AI
        //[HarmonyPatch("OnOpen")]
        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        //{
        //    var codes = new List<CodeInstruction>(instructions);

        //    // 查找 InputManager.DisableInput 调用的位置
        //    int disableInputIndex = -1;
        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        if (codes[i].opcode == OpCodes.Call &&
        //            codes[i].operand is MethodInfo method &&
        //            method.Name == "DisableInput" &&
        //            method.DeclaringType == typeof(InputManager))
        //        {
        //            disableInputIndex = i;
        //            break;
        //        }
        //    }

        //    if (disableInputIndex == -1)
        //    {
        //        Debug.LogWarning("⚠️ 未找到 InputManager.DisableInput 调用");
        //        return instructions;
        //    }

        //    // 创建标签
        //    Label skipDisableInputLabel = generator.DefineLabel();
        //    Label continueLabel = generator.DefineLabel();

        //    var newCodes = new List<CodeInstruction>();

        //    // 复制所有代码直到 DisableInput 的参数之前
        //    for (int i = 0; i < disableInputIndex - 1; i++)
        //    {
        //        newCodes.Add(codes[i]);
        //    }

        //    // 此时栈上应该有 base.gameObject（DisableInput 的参数）
        //    // 我们需要保存这个参数，因为条件检查可能会改变栈

        //    // 保存 gameObject 参数到本地变量
        //    LocalBuilder gameObjectVar = generator.DeclareLocal(typeof(GameObject));
        //    newCodes.Add(new CodeInstruction(OpCodes.Stloc, gameObjectVar));

        //    // 检查条件：viewTabs != null
        //    newCodes.Add(new CodeInstruction(OpCodes.Ldarg_0)); // 加载 this
        //    newCodes.Add(new CodeInstruction(OpCodes.Ldfld, viewTabsField)); // 加载 viewTabs 字段
        //    newCodes.Add(new CodeInstruction(OpCodes.Brfalse, continueLabel)); // 如果 viewTabs == null，跳转到继续执行 DisableInput

        //    // viewTabs != null 的情况：跳过 DisableInput，设置攻击阻止
        //    newCodes.Add(new CodeInstruction(OpCodes.Ldarg_0));
        //    newCodes.Add(new CodeInstruction(OpCodes.Call,
        //        AccessTools.Method(typeof(ViewPatch), "SetAttackBlockAndActiveInput")));
        //    newCodes.Add(new CodeInstruction(OpCodes.Br, skipDisableInputLabel));

        //    // viewTabs == null 的情况：执行原逻辑
        //    newCodes.Add(new CodeInstruction(OpCodes.Ldloc, gameObjectVar) { labels = new List<Label> { continueLabel } }); // 恢复 gameObject 参数
        //    newCodes.Add(new CodeInstruction(OpCodes.Call,
        //        AccessTools.Method(typeof(InputManager), "DisableInput"))); // 调用 DisableInput

        //    // 跳过标签
        //    newCodes.Add(new CodeInstruction(OpCodes.Nop) { labels = new List<Label> { skipDisableInputLabel } });

        //    // 复制剩余的所有代码
        //    for (int i = disableInputIndex + 1; i < codes.Count; i++)
        //    {
        //        newCodes.Add(codes[i]);
        //    }
        //    return newCodes;
        //}

        //public static void SetAttackBlockAndActiveInput(View instance)
        //{
        //    try
        //    {
        //        CheckBackpackWhileMoving.disableAttack = true;
        //        InputManager.ActiveInput(instance.gameObject);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogError($"设置攻击阻止失败: {ex.Message}");
        //    }
        //}

        [HarmonyPatch("OnClose")]
        [HarmonyPostfix]
        public static void OnClose_Postfix()
        {
            CheckBackpackWhileMoving.disableAttack = false;
        }
    }
}
