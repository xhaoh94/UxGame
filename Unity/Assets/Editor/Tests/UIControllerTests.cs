using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ux.Editor.Tests
{
    /// <summary>控制器页面切换、属性覆盖与默认状态回落。</summary>
    public sealed class UIControllerTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ControllerRoot", typeof(RectTransform));
            var rect = (RectTransform)_root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(400f, 400f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void SwitchPageAppliesVisibleState()
        {
            var controller = CreateController();
            var target = CreateTarget(controller, out var binding);
            binding.States = new[]
            {
                new ControllerState { PageIndex = 1, SetVisible = true, Visible = false },
            };

            controller.SetSelectedIndex(1);
            Assert.IsFalse(target.activeSelf, "第 1 页应隐藏");

            controller.SetSelectedIndex(0);
            Assert.IsTrue(target.activeSelf, "第 0 页没有配置，应回落到默认可见");
        }

        [Test]
        public void PageOverridesPositionAndRestoresDefault()
        {
            var controller = CreateController();
            var target = CreateTarget(controller, out var binding);
            var rect = (RectTransform)target.transform;
            binding.States = new[]
            {
                new ControllerState
                {
                    PageIndex = 1,
                    SetPosition = true,
                    Position = new Vector2(10f, 20f),
                },
            };

            controller.SetSelectedIndex(1);
            Assert.AreEqual(new Vector2(10f, 20f), rect.anchoredPosition);

            controller.SetSelectedIndex(0);
            Assert.AreEqual(Vector2.zero, rect.anchoredPosition, "未配置的页应恢复默认位置");
        }

        [Test]
        public void PageOverridesColorAndRestoresDefault()
        {
            var controller = CreateController();
            var target = CreateTarget(controller, out var binding);
            var image = target.GetComponent<Image>();
            var defaultColor = image.color;

            binding.States = new[]
            {
                new ControllerState { PageIndex = 1, SetColor = true, Color = Color.red },
            };

            controller.SetSelectedIndex(1);
            Assert.AreEqual(Color.red, image.color);

            controller.SetSelectedIndex(0);
            Assert.AreEqual(defaultColor, image.color);
        }

        [Test]
        public void PageOverridesText()
        {
            var controller = CreateController();
            var target = CreateTextTarget(controller, out var binding);
            var text = target.GetComponent<Text>();
            text.text = "A";
            binding.RecaptureDefault();

            binding.States = new[]
            {
                new ControllerState { PageIndex = 1, SetText = true, Text = "B" },
            };

            controller.SetSelectedIndex(1);
            Assert.AreEqual("B", text.text);

            controller.SetSelectedIndex(0);
            Assert.AreEqual("A", text.text);
        }

        [Test]
        public void UnconfiguredPageRestoresDefaultAlphaAndScale()
        {
            var controller = CreateController();
            var target = CreateTarget(controller, out var binding);
            var rect = (RectTransform)target.transform;

            binding.States = new[]
            {
                new ControllerState
                {
                    PageIndex = 1,
                    SetAlpha = true,
                    Alpha = 0.3f,
                    SetScale = true,
                    Scale = new Vector3(2f, 2f, 1f),
                },
            };

            controller.SetSelectedIndex(1);
            var image = target.GetComponent<Image>();
            Assert.AreEqual(0.3f, image.color.a, 0.001f, "没有 CanvasGroup 时透明度并入顶点色");
            Assert.AreEqual(2f, rect.localScale.x, 0.001f);

            controller.SetSelectedIndex(0);
            Assert.AreEqual(1f, image.color.a, 0.001f);
            Assert.AreEqual(1f, rect.localScale.x, 0.001f);
        }

        [Test]
        public void ReplacingStatesAppliesCurrentPageImmediately()
        {
            var controller = CreateController();
            var target = CreateTarget(controller, out var binding);
            controller.SetSelectedIndex(1);

            binding.States = new[]
            {
                new ControllerState { PageIndex = 1, SetVisible = true, Visible = false },
            };

            Assert.IsFalse(target.activeSelf, "运行时替换状态后应立即应用当前页");
        }

        [Test]
        public void ChangingControllerWhileHiddenRebindsImmediately()
        {
            var controllerA = CreateController();
            var controllerBObject = new GameObject("ControllerB", typeof(RectTransform));
            controllerBObject.transform.SetParent(_root.transform, false);
            var controllerB = controllerBObject.AddComponent<UIController>();
            controllerB.AddPage("page1");

            var target = CreateTarget(controllerA, out var binding);
            binding.States = new[]
            {
                new ControllerState { PageIndex = 1, SetVisible = true, Visible = false },
            };
            controllerA.SetSelectedIndex(1);
            Assert.IsFalse(target.activeSelf);

            binding.Controller = controllerB;
            Assert.IsTrue(target.activeSelf, "切换到新控制器后应立即应用新控制器当前页");

            controllerA.SetSelectedIndex(0, true);
            Assert.IsTrue(target.activeSelf, "旧控制器不应再修改已转移的绑定对象");
        }

        [Test]
        public void ChangedEventReportsOldAndNewIndex()
        {
            var controller = CreateController();
            int oldIndex = -1;
            int newIndex = -1;
            controller.Changed += (_, oldValue, newValue) =>
            {
                oldIndex = oldValue;
                newIndex = newValue;
            };

            controller.SetSelectedIndex(1);

            Assert.AreEqual(0, oldIndex);
            Assert.AreEqual(1, newIndex);
        }

        [Test]
        public void SetSelectedPageByName()
        {
            var controller = CreateController();
            controller.AddPage("red");

            Assert.IsTrue(controller.SetSelectedPage("red"));
            Assert.AreEqual(1, controller.SelectedIndex);
            Assert.AreEqual("red", controller.SelectedPageName);
            Assert.IsFalse(controller.SetSelectedPage("missing"));
        }

        [Test]
        public void SetSelectedPageById()
        {
            var controller = CreateController();
            controller.AddPage("red", 42);
            controller.AddPage("blue", 43);

            Assert.IsTrue(controller.SetSelectedPageId(43));
            Assert.AreEqual(2, controller.SelectedIndex);
            Assert.AreEqual(43, controller.SelectedPageId);
        }

        [Test]
        public void RemovingLastPageClampsSelection()
        {
            var controller = CreateController();
            controller.AddPage("red");
            controller.SetSelectedIndex(1);

            controller.RemovePage(1);

            Assert.AreEqual(1, controller.PageCount);
            Assert.AreEqual(0, controller.SelectedIndex);
        }

        [Test]
        public void BindingRegistersAndUnregisters()
        {
            var controller = CreateController();
            var target = CreateTarget(controller, out _);
            Assert.AreEqual(1, controller.BindingCount);

            Object.DestroyImmediate(target);
            Assert.AreEqual(0, controller.BindingCount);
        }

        private UIController CreateController()
        {
            var controller = _root.GetComponent<UIController>();
            if (controller == null) controller = _root.AddComponent<UIController>();
            if (controller.PageCount < 2) controller.AddPage("page1");
            return controller;
        }

        private GameObject CreateTarget(UIController controller, out UIControllerBinding binding)
        {
            var go = new GameObject("Target", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(100f, 40f);
            rect.anchoredPosition = Vector2.zero;

            binding = go.AddComponent<UIControllerBinding>();
            binding.Controller = controller;
            return go;
        }

        private GameObject CreateTextTarget(UIController controller, out UIControllerBinding binding)
        {
            var go = new GameObject("TextTarget", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_root.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(100f, 40f);
            rect.anchoredPosition = Vector2.zero;

            binding = go.AddComponent<UIControllerBinding>();
            binding.Controller = controller;
            return go;
        }
    }
}
