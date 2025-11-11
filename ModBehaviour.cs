using Duckov.Modding;
using Duckov.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
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
        private CheckBackpackWhileMoving? yhpm4CheckBackpackWhileMoving { get; set; }
        private Harmony? harmony;
        private bool IsInGame, hasShoulderSurfing;
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
            this.yhpm4CheckBackpackWhileMoving = CheckBackpackWhileMoving.Instance;
            this.ApplyMyPatches();
            
        }

        void OnDisable()
        {
            try
            {
                this.IsInGame = false;
                CheckBackpackWhileMoving.Instance.IsInGame = this.IsInGame;
                SceneLoader.onStartedLoadingScene -= this.OnSceneLoadStarted;
                if (this.harmony != null)
                {
                    this.harmony.UnpatchAll(Id);
                    this.harmony = null;
                }
                if (this.yhpm4CheckBackpackWhileMoving != null)
                {
                    yhpm4CheckBackpackWhileMoving.disableAttack = false;
                    yhpm4CheckBackpackWhileMoving.ClearCurrentLootBox();
                }
                View activeView = View.ActiveView;
                //InputManager.ActiveInput((activeView != null) ? activeView.gameObject : null);
                this.yhpm4CheckBackpackWhileMoving = null;
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
                if (yhpm4CheckBackpackWhileMoving == null) return;
                bool disableAttack = yhpm4CheckBackpackWhileMoving.disableAttack;
                if (disableAttack)
                {
                    bool flag = Time.frameCount % 60 == 0;
                    if (flag)
                    {
                        CharacterMainControl main = CharacterMainControl.Main;
                        if (main != null)
                        {
                            if (yhpm4CheckBackpackWhileMoving.currentLootBox != null && this.calDistanceIsOutOfRange(main, yhpm4CheckBackpackWhileMoving.currentLootBox))
                            {
                                this.ForceCloseView();
                                if (yhpm4CheckBackpackWhileMoving != null)
                                {
                                    yhpm4CheckBackpackWhileMoving.ClearCurrentLootBox();
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
                if (distance > 2.2f)//可根据需要调整距离阈值
                {
                    return true;
                }
            }
            return false;
        }

        private bool ApplyMyPatches()
        {
            //环境检测：检测是否安装了第三人称mod ShoulderSurfing
            List<ModInfo> modlist = ModManager.modInfos;
            bool hasShoulderSurfing = modlist.Exists(mod => mod.name == "ShoulderSurfing");
            CheckBackpackWhileMoving.Instance.hasShoulderSurfing = hasShoulderSurfing;

            this.harmony = new Harmony(Id);
            this.harmony.PatchAll(Assembly.GetExecutingAssembly());
            this.ApplyKeypadBindings(this.IsInGame);
            return true;
        }

        private void OnSceneLoadStarted(SceneLoadingContext context)
        {
            try
            {
                //Debug.Log($"CheckBackpackWhileMoving检测到场景加载: {context.sceneName}");
                CheckBackpackWhileMoving.Instance.initStatus();
                bool flag = "Base".Equals(context.sceneName);
                if (!flag)
                {
                    this.IsInGame = true;
                    CheckBackpackWhileMoving.Instance.IsInGame = this.IsInGame;
                    this.ApplyKeypadBindings(this.IsInGame);
                }
                else
                {
                    this.IsInGame = false;
                    CheckBackpackWhileMoving.Instance.IsInGame = this.IsInGame;
                    this.ApplyKeypadBindings(false);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[CheckBackpackWhileMoving] OnSceneLoadStarted error:"+ex.Message);
            }
        }
        void ForceCloseView()
        {
            if (View.ActiveView != null)
            {
                View.ActiveView.Close();
                CheckBackpackWhileMoving.Instance.ClearCurrentLootBox();
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


