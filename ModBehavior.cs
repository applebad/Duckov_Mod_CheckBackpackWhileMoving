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

        void OnAwake()
        {
            Debug.Log("CheckBackpackWhileMoving Loaded");
        }
        void OnDestroy()
        {

        }
        void OnEnable()
        {
            checkBackpackWhileMoving = new CheckBackpackWhileMoving();
            System.Threading.Tasks.Task.Delay(1000);
            harmony = new Harmony(Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ApplyKeypadBindings();
        }

        void OnDisable()
        {
            try
            {
                if (harmony != null)
                {
                    harmony.UnpatchAll(Id);
                    harmony = null;
                }
                CheckBackpackWhileMoving.disableAttack = false;
                ViewPatch.ClearViewHasTabsCache();
                checkBackpackWhileMoving?.ClearCurrentLootBox();
                InputManager.ActiveInput(View.ActiveView?.gameObject);

                checkBackpackWhileMoving = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
            }
        }

        void Update()
        {
            try
            {
                // 实时监控关键变量
                if (Time.frameCount % 60 == 0) // 每240帧输出一次
                {
                    //Debug.Log($"=== 帧 {Time.frameCount} 状态监控 ===");
                    //Debug.Log($"攻击阻止: {CheckBackpackWhileMoving.disableAttack}");
                    //Debug.Log($"活动视图: {View.ActiveView?.GetType().Name ?? "无"}");
                    // 监控玩家位置（用于距离检测）
                    var player = CharacterMainControl.Main;
                    if (player != null)
                    {
                        //Debug.Log($"玩家位置: {player.transform.position}");
                    }
                    if (CheckBackpackWhileMoving.currentLootBox != null && calDistanceIsOutOfRange(player, CheckBackpackWhileMoving.currentLootBox))
                    {
                        ForceCloseView();
                        checkBackpackWhileMoving?.ClearCurrentLootBox();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Harmony] 在补丁中出错: {ex.Message}");
                Debug.LogError("Error at mod:CheckBackpackWhileMoving");
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

        void ForceCloseView()
        {
            if (View.ActiveView != null)
            {
                View.ActiveView.Close();
            }
        }
        private void ApplyKeypadBindings()
        {
            try
            {
                if (UIInputManager.Instance == null)
                {
                    Debug.LogWarning("UIInputManager 尚未初始化");
                    return;
                }

                var manager = UIInputManager.Instance;
                var type = typeof(UIInputManager);

                var nextPageField = type.GetField("inputActionNextPage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var previousPageField = type.GetField("inputActionPreviousPage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (nextPageField != null)
                {
                    var nextPageAction = (InputAction)nextPageField.GetValue(manager);
                    if (nextPageAction != null)
                    {
                        ReplaceBindings(nextPageAction, "<Keyboard>/downArrow", "<Keyboard>/numpad2");
                        //Debug.Log("下一页绑定修改成功");
                    }
                }

                if (previousPageField != null)
                {
                    var previousPageAction = (InputAction)previousPageField.GetValue(manager);
                    if (previousPageAction != null)
                    {
                        ReplaceBindings(previousPageAction, "<Keyboard>/upArrow", "<Keyboard>/numpad8");
                        //Debug.Log("上一页绑定修改成功");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"应用小键盘绑定失败: {e}");
            }
        }
        private void ReplaceBindings(InputAction action, params string[] bindings)
        {
            // 禁用Action
            action.Disable();

            // 清空所有绑定
            for (int i = action.bindings.Count - 1; i >= 0; i--)
            {
                action.ChangeBinding(i).Erase();
            }

            // 添加新绑定
            foreach (string binding in bindings)
            {
                action.AddBinding(binding);
            }

            // 重新启用
            action.Enable();
        }
    }
}


