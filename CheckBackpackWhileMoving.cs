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
        public bool disableAttack, hasShoulderSurfing, IsInGame;//局内状态
        public bool DisableInteract { get; set; }
        public GameObject? currentLootBox { get; set; }
        public List<String> interactObjectNames { get; set; }
        public InputActionAsset actions { get; }
        private CheckBackpackWhileMoving()
        {
            actions = GameManager.MainPlayerInput.actions;
            disableAttack = false;
            IsInGame = false;
            DisableInteract = false;
            interactObjectNames = new List<String>();
            interactObjectNames.Add("LootBoxLoader");
            interactObjectNames.Add("Inventory");
        }
        public void initStatus() {
            disableAttack = false;
            IsInGame = false;
            DisableInteract = false;
        }
        public void ClearCurrentLootBox()
        {
            currentLootBox = null;
        }
    }

    // 第三人称mod兼容
    [HarmonyPatch]
    static class ShoulderCameraPatch
    {
        private static MethodBase _cachedMethod;
        private static bool _hasChecked = false;
        private static bool _shouldPatch = false;

        static ShoulderCameraPatch()
        {
            // 在静态构造函数中预先检查并缓存结果
            if (!CheckBackpackWhileMoving.Instance.hasShoulderSurfing)
            {
                _shouldPatch = false;
                _hasChecked = true;
                return;
            }
            try
            {
                Type shoulderCameraType = AccessTools.TypeByName("ShoulderSurfing.ShoulderCamera");
                if (shoulderCameraType != null)
                {
                    _cachedMethod = AccessTools.Method(shoulderCameraType, "LateUpdate");
                    _shouldPatch = _cachedMethod != null;
                }
                else
                {
                    _shouldPatch = false;
                }
            }
            catch
            {
                _shouldPatch = false;
            }
            _hasChecked = true;
        }

        static bool Prepare()
        {
            // 直接返回缓存的结果
            return _hasChecked && _shouldPatch;
        }

        static MethodBase TargetMethod()
        {
            // 直接返回缓存的方法
            return _cachedMethod;
        }

        [HarmonyPrefix]
        static bool Prefix() => !CheckBackpackWhileMoving.Instance.disableAttack;
    }

    [HarmonyPatch(typeof(CharacterInputControl))]
    public class CharacterInputControlPatch
    {
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
                //Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
                //Debug.LogError("Error at mod:CheckBackpackWhileMoving");
                return true;
            }
            return true; // 执行原方法
        }

        //取消动作
        [HarmonyPatch("OnPlayerStopAction")]
        [HarmonyPrefix]
        static bool OnPlayerStopAction_Prefix(InputAction.CallbackContext context)
        {
            if (CheckBackpackWhileMoving.Instance.IsInGame && context.started)
            {
                CheckBackpackWhileMoving.Instance.DisableInteract = false;
                CheckBackpackWhileMoving.Instance.ClearCurrentLootBox();
                if (CheckBackpackWhileMoving.Instance.disableAttack)
                {
                    return false; // 跳过原方法
                }
            }
            return true; // 执行原方法
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl))]
    public class CharacterMainCorolPatch
    {
        [HarmonyPatch("GetInteractableTargetToInteract")]
        [HarmonyPostfix]
        static void GetInteractableTargetToInteract(ref InteractableBase __result)
        {
            if(!CheckBackpackWhileMoving.Instance.IsInGame) return;
            if (__result != null && __result.gameObject != null)
            {
                GameObject target = __result.gameObject;
                //Debug.Log("检测到新的交互对象:" + target.name);
                //Debug.Log("检测到新的交互对象:" + __result.gameObject.ToString());
                if (target != null)
                {
                    //交互对象如果和当前的对象相同，则禁止本次交互
                    if (CheckBackpackWhileMoving.Instance.currentLootBox != null)
                    {
                        if (__result.gameObject == CheckBackpackWhileMoving.Instance.currentLootBox)
                        {
                            //Debug.Log("判断是否相等:" + (__result.gameObject == CheckBackpackWhileMoving.Instance.currentLootBox));
                            CheckBackpackWhileMoving.Instance.DisableInteract = true;
                        }
                        else CheckBackpackWhileMoving.Instance.DisableInteract = false;

                    }
                    else CheckBackpackWhileMoving.Instance.DisableInteract = false;
                    //当前交互对象为 空 或者为 新的对象 ，可以交互
                    if (!CheckBackpackWhileMoving.Instance.DisableInteract)
                    {   //判断交互类型
                        //商人的组件名比较特别
                        if (target.name.Contains("Merchant") || target.name.Contains("Shop"))
                        {
                            CheckBackpackWhileMoving.Instance.currentLootBox = __result.gameObject;
                        }
                        else
                        {
                            //LootBoxLoader Inventory 
                            Component[] allComponents = __result.gameObject.GetComponents<Component>();
                            bool flag = false;
                            foreach (Component comp in allComponents)
                            {
                                if (comp != null && !flag)
                                {
                                    //Debug.Log($"组件类型: {comp.GetType().Name}");
                                    foreach (String name in CheckBackpackWhileMoving.Instance.interactObjectNames)
                                    {
                                        if (comp.GetType().Name.Contains(name))
                                        {
                                            CheckBackpackWhileMoving.Instance.currentLootBox = __result.gameObject;
                                            flag = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //Debug.Log("获取到的可交互目标:" + target?.name);//包含LootBox为战利品箱或搜索箱
                //Debug.Log("当前交互对象:" + CheckBackpackWhileMoving.Instance.currentLootBox);
                //Debug.Log("禁用开火状态:" + CheckBackpackWhileMoving.Instance.disableAttack);
                //Debug.Log("禁止交互状态:" + CheckBackpackWhileMoving.Instance.DisableInteract);
                if (CheckBackpackWhileMoving.Instance.DisableInteract)
                {
                    __result = null; //禁止交互
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
            return !CheckBackpackWhileMoving.Instance.disableAttack;
        }

        [HarmonyPatch("UpdateAimOffsetUsingBound")]
        [HarmonyPrefix]
        static bool UpdateAimOffsetUsingBound_Prefix()
        {
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
            if (CheckBackpackWhileMoving.Instance.disableAttack && CheckBackpackWhileMoving.Instance.IsInGame)
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
                if (CheckBackpackWhileMoving.Instance.IsInGame)
                {
                    CheckBackpackWhileMoving.Instance.disableAttack = true;
                    InputManager.ActiveInput(__instance.gameObject); 
                }
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
            if (CheckBackpackWhileMoving.Instance.IsInGame)
            {
                CheckBackpackWhileMoving.Instance.disableAttack = false;
                CheckBackpackWhileMoving.Instance.ClearCurrentLootBox(); 
            }
            //Debug.Log("[Merchant] Merchant mode deactivated");
        }
    }
}
