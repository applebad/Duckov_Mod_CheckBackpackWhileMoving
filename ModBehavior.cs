using Duckov.UI;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
/**
 * 目标效果：打开背包时，禁用鼠标输入的开火，只允许WASD移动角色
 *          角色距离Loot过远时，自动关闭背包界面
**/
namespace CheckBackpackWhileMoving
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private const string Id = "yhpm4.CheckBackpackWhileMoving";
        private CheckBackpackWhileMoving? checkBackpackWhileMoving { get; set; }
        private Harmony? harmony;
        private bool IsInGame;
        void OnAwake()
        {
            Debug.Log("CheckBackpackWhileMoving Loaded");
        }
        void OnDestroy()
        {

        }
        void OnEnable()
        {
            this.IsInGame = false;
            SceneLoader.onStartedLoadingScene += this.OnSceneLoadStarted;
            this.checkBackpackWhileMoving = new CheckBackpackWhileMoving();
            bool isInGame = this.IsInGame;
            if (isInGame)
            {
                this.ApplyMyPatches();
            }
        }

        void OnDisable()
        {
            try
            {
                this.IsInGame = false;
                SceneLoader.onStartedLoadingScene -= this.OnSceneLoadStarted;
                if (this.harmony != null)
                {
                    this.harmony.UnpatchAll("yhpm4.CheckBackpackWhileMoving");
                    this.harmony = null;
                }
                if (this.checkBackpackWhileMoving != null)
                {
                    CheckBackpackWhileMoving.disableAttack = false;
                    CheckBackpackWhileMoving checkBackpackWhileMoving = this.checkBackpackWhileMoving;
                    if (checkBackpackWhileMoving != null)
                    {
                        checkBackpackWhileMoving.ClearCurrentLootBox();
                    }
                }
                ViewPatch.ClearViewHasTabsCache();
                View activeView = View.ActiveView;
                InputManager.ActiveInput((activeView != null) ? activeView.gameObject : null);
                this.checkBackpackWhileMoving = null;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Harmony] 在补丁中出错: " + ex.Message);
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
            }
        }

        void Update()
        {
            try
            {
                bool disableAttack = CheckBackpackWhileMoving.disableAttack;
                if (disableAttack)
                {
                    bool flag = Time.frameCount % 60 == 0;
                    if (flag)
                    {
                        CharacterMainControl main = CharacterMainControl.Main;
                        bool flag2 = main == null;
                        if (!flag2)
                        {
                            bool flag3 = CheckBackpackWhileMoving.currentLootBox != null && this.calDistanceIsOutOfRange(main, CheckBackpackWhileMoving.currentLootBox);
                            if (flag3)
                            {
                                this.ForceCloseView();
                                CheckBackpackWhileMoving checkBackpackWhileMoving = this.checkBackpackWhileMoving;
                                if (checkBackpackWhileMoving != null)
                                {
                                    checkBackpackWhileMoving.ClearCurrentLootBox();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        bool calDistanceIsOutOfRange(CharacterMainControl player, GameObject lootBox)
        {
            if (player != null && lootBox != null)
            {
                float distance = Vector3.Distance(player.transform.position, lootBox.transform.position);
                //Debug.Log($"玩家与战利品箱距离: {distance}");
                if (distance > 2.0f) // 假设5.0f是关闭背包的距离阈值
                {
                    //Debug.Log("距离过远，强制关闭背包视图");
                    return true;
                }
            }
            return false;
        }

        private bool ApplyMyPatches()
        {
            this.harmony = new Harmony("yhpm4.CheckBackpackWhileMoving");
            this.harmony.PatchAll(Assembly.GetExecutingAssembly());
            this.ApplyKeypadBindings(true);
            return true;
        }

        private void OnSceneLoadStarted(SceneLoadingContext context)
        {
            bool flag = "Base".Equals(context.sceneName);
            if (flag)
            {
                this.IsInGame = false;
                if (this.harmony != null)
                {
                    this.harmony.UnpatchAll("yhpm4.CheckBackpackWhileMoving");
                    this.harmony = null;
                }
                this.ApplyKeypadBindings(false);
            }
            else
            {
                this.IsInGame = true;
                this.ApplyMyPatches();
            }
        }
        void ForceCloseView()
        {
            if (View.ActiveView != null)
            {
                View.ActiveView.Close();
            }
        }
        private void ApplyKeypadBindings(bool flag)
        {
            try
            {
                if (UIInputManager.Instance == null)
                {
                    return;
                    //Debug.LogWarning("UIInputManager 尚未初始化");
                }
                else
                {
                    UIInputManager instance = UIInputManager.Instance;
                    Type typeFromHandle = typeof(UIInputManager);
                    FieldInfo field = typeFromHandle.GetField("inputActionNextPage", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo field2 = typeFromHandle.GetField("inputActionPreviousPage", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        InputAction inputAction = (InputAction)field.GetValue(instance);
                        if (inputAction != null)
                        {
                            if (flag)
                            {
                                this.ReplaceBindings(inputAction, new string[]
                                {
                                    "<Keyboard>/downArrow"
                                });
                            }
                            else
                            {
                                this.ReplaceBindings(inputAction, new string[]
                                {
                                    "<Keyboard>/s"
                                });
                            }
                        }
                    }
                    if (field2 != null)
                    {
                        InputAction inputAction2 = (InputAction)field2.GetValue(instance);
                        if (inputAction2 != null)
                        {
                            if (flag)
                            {
                                this.ReplaceBindings(inputAction2, new string[]
                                {
                                    "<Keyboard>/upArrow"
                                });
                            }
                            else
                            {
                                this.ReplaceBindings(inputAction2, new string[]
                                {
                                    "<Keyboard>/w"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception arg)
            {
                Debug.LogError(string.Format("应用按键绑定失败: {0}", arg));
            }
        }
        private void ReplaceBindings(InputAction action, params string[] bindings)
        {
            action.Disable();
            for (int i = action.bindings.Count - 1; i >= 0; i--)
            {
                action.ChangeBinding(i).Erase();
            }
            foreach (string path in bindings)
            {
                action.AddBinding(path, null, null, null);
            }
            action.Enable();
        }
    }
}


