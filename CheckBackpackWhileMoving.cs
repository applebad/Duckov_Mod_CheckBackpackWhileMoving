using Duckov.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CheckBackpackWhileMoving
{
    public class CheckBackpackWhileMoving
    {
        private static readonly CheckBackpackWhileMoving _instance = new CheckBackpackWhileMoving();
        public static CheckBackpackWhileMoving Instance => _instance;
        public bool disableAttack { get; set; }
        public bool IsMerchant { get; set; }
        public GameObject? currentLootBox { get; set; }
        public List<String> interactObjectNames { get; set; }
        public InputActionAsset actions { get; }
        private CheckBackpackWhileMoving()
        {
            actions = GameManager.MainPlayerInput.actions;
            disableAttack = false;
            IsMerchant = false;
            interactObjectNames = new List<String>();
            interactObjectNames.Add("LootBox");
            interactObjectNames.Add("Merchant");
        }
        public void ClearCurrentLootBox()
        {
            currentLootBox = null;
        }
    }

    [HarmonyPatch(typeof(CharacterInputControl))]
    public class CharacterInputControlPatch
    {
        //交互相关
        [HarmonyPatch("OnInteractInput")]
        [HarmonyPrefix]
        static bool OnInteractInput_Prefix(CharacterInputControl __instance, InputAction.CallbackContext context)
        {
            try
            {
                // 当是商人模式且背包打开时，阻止交互输入
                if (CheckBackpackWhileMoving.Instance.IsMerchant && CheckBackpackWhileMoving.Instance.disableAttack)
                {
                    return false; // 跳过原方法
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
                Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
            }
            return true; // 执行原方法
        }
        //瞄准相关
        [HarmonyPatch("OnPlayerAdsInput")]
        [HarmonyPrefix]
        static bool OnPlayerAdsInput_Prefix(CharacterInputControl __instance, ref InputAction.CallbackContext context)
        {
            try
            {
                if (CheckBackpackWhileMoving.Instance.disableAttack)
                {
                    if (context.canceled)
                    {
                        FieldInfo adsField = AccessTools.Field(typeof(CharacterInputControl), "adsInput");
                        MethodInfo methodInfo = AccessTools.Method(typeof(CharacterInputControl), "SetAdsInput");
                        methodInfo.Invoke(__instance, new object[] { false });
                        if (adsField != null)
                        {
                            adsField.SetValue(__instance, false);
                        }
                    }
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
                if (targetName != null)
                {
                    foreach (String name in CheckBackpackWhileMoving.Instance.interactObjectNames)
                    {
                        if (targetName.Contains(name))
                        {
                            CheckBackpackWhileMoving.Instance.currentLootBox = __result.gameObject;
                            CheckBackpackWhileMoving.Instance.disableAttack = true;
                        }
                    }
                    if (targetName.Contains("Merchant"))
                    {
                        CheckBackpackWhileMoving.Instance.IsMerchant = true;
                    }
                }
            }
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
            return !CheckBackpackWhileMoving.Instance.disableAttack;
        }

        [HarmonyPatch("UpdateAimOffsetUsingBound")]
        [HarmonyPrefix]
        static bool UpdateAimOffsetUsingBound_Prefix()
        {
            // 只阻止边界偏移计算，但允许相机其他更新
            return !CheckBackpackWhileMoving.Instance.disableAttack;
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
                if (!CheckBackpackWhileMoving.Instance.disableAttack)
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
            if (CheckBackpackWhileMoving.Instance.disableAttack)
            {
                return false; // 跳过原方法
            }
            return true; // 执行原方法
        }
    }


    [HarmonyPatch(typeof(View))]
    public class ViewPatch
    {
        //打开背包
        [HarmonyPatch("OnOpen")]
        [HarmonyPostfix]
        static void OnOpenPatch(View __instance)
        {
            try
            {
                CheckBackpackWhileMoving.Instance.disableAttack = true;
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
            CheckBackpackWhileMoving.Instance.disableAttack = false;
            CheckBackpackWhileMoving.Instance.IsMerchant = false;
            CheckBackpackWhileMoving.Instance.ClearCurrentLootBox();
            Debug.Log("[Merchant] Merchant mode deactivated");
        }
    }
}
