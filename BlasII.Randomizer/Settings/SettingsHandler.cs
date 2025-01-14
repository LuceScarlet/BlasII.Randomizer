﻿using BlasII.ModdingAPI.Audio;
using BlasII.ModdingAPI.Input;
using BlasII.ModdingAPI.UI;
using BlasII.Randomizer.Extensions;
using Il2CppTGK.Game;
using Il2CppTGK.Game.Components.UI;
using Il2CppTGK.Game.PopupMessages;
using Il2CppTMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlasII.Randomizer.Settings
{
    public class SettingsHandler
    {
        private MainMenuWindowLogic _mainMenu;
        private GameObject _slotsMenu;
        private GameObject _settingsMenu;

        private int _currentSlot;
        private Clickable _clickedSetting = null;
        private bool _closeNextFrame = false;

        private bool PressedEnter => Main.Randomizer.InputHandler.GetButtonDown(ButtonType.UIConfirm);
        private bool PressedCancel => Main.Randomizer.InputHandler.GetButtonDown(ButtonType.UICancel);

        // Forgot we cant use null coalescing  :(
        private bool SettingsMenuActive => _settingsMenu != null && _settingsMenu.activeInHierarchy;

        public void Update()
        {
            Cursor.visible = SettingsMenuActive;

            if (!SettingsMenuActive)
                return;

            if (_closeNextFrame)
            {
                _closeNextFrame = false;
                CloseSettingsMenu();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }

            if (PressedEnter)
            {
                StartNewGame();
            }
            else if (PressedCancel)
            {
                _closeNextFrame = true;
            }
        }

        private void HandleClick()
        {
            _clickedSetting?.OnUnclick();
            _clickedSetting = null;

            foreach (var click in _clickables)
            {
                if (click.Rect.OverlapsPoint(Input.mousePosition))
                {
                    _clickedSetting = click;
                    click.OnClick();
                    break;
                }
            }
        }

        /// <summary>
        /// Displays the settings menu and stores the current slot
        /// </summary>
        public void OpenSettingsMenu(int slot)
        {
            if (SettingsMenuActive)
                return;

            if (_settingsMenu == null)
                CreateSettingsMenu();

            Main.Randomizer.Log("Opening settings menu");
            _settingsMenu.SetActive(true);
            _slotsMenu.SetActive(false);

            MenuSettings = RandomizerSettings.DefaultSettings;
            CoreCache.Input.ClearAllInputBlocks();
            _currentSlot = slot;
            _clickedSetting = null;
        }

        /// <summary>
        /// Closes the settings menu
        /// </summary>
        private void CloseSettingsMenu()
        {
            if (!SettingsMenuActive)
                return;

            Main.Randomizer.Log("Closing settings menu");
            _settingsMenu.SetActive(false);
            _mainMenu.OpenSlotMenu();
            _mainMenu.slotsList.SelectElement(_currentSlot);
        }

        /// <summary>
        /// Begins the game with the stored slot
        /// </summary>
        private void StartNewGame()
        {
            Main.Randomizer.LogWarning("Starting new game");
            Main.Randomizer.AudioHandler.PlayEffectUI(UISFX.OpenMenu);

            Main.Randomizer.CurrentSettings = MenuSettings;
            NewGame_Settings_Patch.NewGameFlag = true;
            Object.FindObjectOfType<MainMenuWindowLogic>().NewGame(_currentSlot);
            NewGame_Settings_Patch.NewGameFlag = false;
        }

        /// <summary>
        /// Displays certain randomizer settings in an info popup
        /// </summary>
        public void DisplaySettings()
        {
            foreach (var mid in Resources.FindObjectsOfTypeAll<PopupMessageID>())
            {
                if (mid.name == "TESTPOPUP_id")
                    CoreCache.UINavigationHelper.ShowPopupMessage(mid);
            }
        }

        /// <summary>
        /// Stores or loads the entire settings menu into or from a settings object
        /// </summary>
        private RandomizerSettings MenuSettings
        {
            get
            {
                int logicDifficulty = 1;
                int requiredKeys = _setRequiredKeys.CurrentOption;
                int startingWeapon = _setStartingWeapon.CurrentOption;
                bool shuffleLongQuests = _setShuffleLongQuests.Toggled;
                bool shuffleShops = _setShuffleShops.Toggled;

                int seed = _setSeed.CurrentNumericValue == 0 ? RandomizerSettings.RandomSeed : _setSeed.CurrentNumericValue;
                return new RandomizerSettings(seed, logicDifficulty, requiredKeys, 0, startingWeapon, 0, shuffleLongQuests, shuffleShops, true, 0, 0);
            }
            set
            {
                _setLogicDifficulty.CurrentOption = 0;
                _setRequiredKeys.CurrentOption = value.requiredKeys;
                _setStartingWeapon.CurrentOption = value.startingWeapon;
                _setShuffleLongQuests.Toggled = value.shuffleLongQuests;
                _setShuffleShops.Toggled = value.shuffleShops;

                _setSeed.CurrentValue = string.Empty;
            }
        }

        /// <summary>
        /// Creates the ui for the settings menu
        /// </summary>
        private void CreateSettingsMenu()
        {
            Main.Randomizer.LogWarning("Creating settings menu");

            // Find slots menu and allow clicking buttons
            var mainMenu = Object.FindObjectOfType<MainMenuWindowLogic>();
            var slotsMenu = mainMenu.slotsMenuView.transform.parent.gameObject;
            _clickables.Clear();

            // Create copy for settings menu
            var settingsMenu = Object.Instantiate(slotsMenu, slotsMenu.transform.parent);
            Object.Destroy(settingsMenu.transform.Find("SlotsList").gameObject);

            // Change text of title
            var title = settingsMenu.transform.Find("Header").GetComponent<UIPixelTextWithShadow>();
            Main.Randomizer.LocalizationHandler.AddPixelTextLocalizer(title, "head");

            // Change text of 'new' button
            var begin = settingsMenu.transform.Find("Buttons/Button A/New/label").GetComponent<UIPixelTextWithShadow>();
            Main.Randomizer.LocalizationHandler.AddPixelTextLocalizer(begin, "btnb");

            // Change text of 'cancel' button
            var cancel = settingsMenu.transform.Find("Buttons/Back/label").GetComponent<UIPixelTextWithShadow>();
            Main.Randomizer.LocalizationHandler.AddPixelTextLocalizer(cancel, "btnc");

            // Create holder for options and all settings
            RectTransform mainSection = UIModder.CreateRect("Main Section", settingsMenu.transform)
                .SetSize(1800, 750)
                .SetPosition(0, -30);

            _setSeed = CreateTextOption("Seed", mainSection, new Vector2(0, 300), 150,
                "seed", true, false, 6);

            _setLogicDifficulty = CreateArrowOption("LD", mainSection, new Vector2(-300, 80),
                "opld", _opLogic);

            _setRequiredKeys = CreateArrowOption("RQ", mainSection, new Vector2(-300, -80),
                "oprq", _opKeys);

            _setStartingWeapon = CreateArrowOption("SW", mainSection, new Vector2(-300, -240),
                "opsw", _opWeapon);

            _setShuffleLongQuests = CreateToggleOption("SL", mainSection, new Vector2(150, 70),
                "opsl");

            _setShuffleShops = CreateToggleOption("SS", mainSection, new Vector2(150, -10),
                "opss");

            _mainMenu = mainMenu;
            _settingsMenu = settingsMenu;
            _slotsMenu = slotsMenu;
        }

        private UIPixelTextWithShadow CreateShadowText(string name, Transform parent, Vector2 position, int size, Color color, Vector2 pivot, TextAlignmentOptions alignment, string text)
        {
            // Create shadow
            var shadow = UIModder.CreateRect(name, parent)
                .SetPosition(position)
                .SetPivot(pivot)
                .AddText()
                .SetAlignment(alignment)
                .SetColor(new Color(0.004f, 0.008f, 0.008f))
                .SetFontSize(size)
                .SetContents(text);

            // Create normal
            var normal = UIModder.CreateRect(name, shadow.transform)
                .SetPosition(0, 4)
                .AddText()
                .SetAlignment(alignment)
                .SetColor(color)
                .SetFontSize(size)
                .SetContents(text);

            // Create component
            var pixelText = shadow.gameObject.AddComponent<UIPixelTextWithShadow>();
            pixelText.normalText = normal;
            pixelText.shadowText = shadow;

            return pixelText;
        }

        private ToggleOption CreateToggleOption(string name, Transform parent, Vector2 position, string header)
        {
            // Create ui holder
            var holder = UIModder.CreateRect(name, parent).SetPosition(position);

            // Create text and images
            var headerText = CreateShadowText("header", holder, position + Vector2.right * 12 + Vector2.down * 3,
                TEXT_SIZE, SILVER,
                new Vector2(0, 0.5f), TextAlignmentOptions.Left, string.Empty);
            Main.Randomizer.LocalizationHandler.AddPixelTextLocalizer(headerText, header);

            var toggleBox = CreateToggleImage("box", holder, position);

            // Initialize toggle option
            var selectable = holder.gameObject.AddComponent<ToggleOption>();
            selectable.Initialize(toggleBox);

            // Add click events
            _clickables.Add(new Clickable(toggleBox.rectTransform, () => selectable.Toggle()));

            return selectable;

            // Creates the toggle box
            Image CreateToggleImage(string name, Transform parent, Vector2 position)
            {
                return UIModder.CreateRect(name, parent)
                    .SetPosition(position)
                    .SetPivot(1, 0.5f)
                    .SetSize(55, 55)
                    .AddImage();
            }
        }

        private ArrowOption CreateArrowOption(string name, Transform parent, Vector2 position, string header, string[] options)
        {
            // Create ui holder
            var holder = UIModder.CreateRect(name, parent).SetPosition(position);

            // Create text and images
            var headerText = CreateShadowText("header", holder, Vector2.up * 60,
                TEXT_SIZE, SILVER,
                new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, string.Empty);
            Main.Randomizer.LocalizationHandler.AddPixelTextLocalizer(headerText, header);

            var optionText = CreateShadowText("option", holder, Vector2.zero,
                TEXT_SIZE - 5, YELLOW,
                new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, string.Empty);

            var leftArrow = CreateArrowImage("left", holder, Vector2.left * 150);
            var rightArrow = CreateArrowImage("right", holder, Vector2.right * 150);

            // Initialize arrow option
            var selectable = holder.gameObject.AddComponent<ArrowOption>();
            selectable.Initialize(optionText, leftArrow, rightArrow, options);

            // Add click events
            _clickables.Add(new Clickable(leftArrow.rectTransform, () => selectable.ChangeOption(-1)));
            _clickables.Add(new Clickable(rightArrow.rectTransform, () => selectable.ChangeOption(1)));

            return selectable;

            // Creates the left and right arrows
            Image CreateArrowImage(string name, Transform parent, Vector2 position)
            {
                return UIModder.CreateRect(name, parent)
                    .SetPosition(position + Vector2.up * 5)
                    .SetSize(55, 55)
                    .AddImage();
            }
        }

        private TextOption CreateTextOption(string name, Transform parent, Vector2 position, int lineSize, string header, bool numeric, bool allowZero, int max)
        {
            // Create ui holder
            var holder = UIModder.CreateRect(name, parent).SetPosition(position);

            // Create text and images
            var headerText = CreateShadowText("header", holder, Vector2.left * 10,
                TEXT_SIZE, SILVER,
                new Vector2(1, 0.5f), TextAlignmentOptions.Right, string.Empty);
            Main.Randomizer.LocalizationHandler.AddPixelTextLocalizer(headerText, header);

            var valueText = CreateShadowText("value", holder, Vector2.right * lineSize / 2,
                TEXT_SIZE - 5, YELLOW,
                new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, string.Empty);

            var underline = CreateLineImage("line", holder, Vector2.zero, lineSize);

            // Initialize text option
            var selectable = holder.gameObject.AddComponent<TextOption>();
            selectable.Initialize(underline, valueText, numeric, allowZero, max);

            // Add click events
            _clickables.Add(new Clickable(underline.rectTransform,
                () => selectable.SetSelected(true),
                () => selectable.SetSelected(false)));

            return selectable;

            // Creates the underline image
            Image CreateLineImage(string name, Transform parent, Vector2 position, int size)
            {
                return UIModder.CreateRect(name, parent)
                    .SetPosition(position)
                    .SetSize(size, 50)
                    .SetPivot(0, 0.5f)
                    .AddImage();
            }
        }

        private const int TEXT_SIZE = 55;
        private readonly Color SILVER = new Color32(192, 192, 192, 255);
        private readonly Color YELLOW = new Color32(255, 231, 65, 255);

        private readonly string[] _opLogic = new string[] { "o2ld" }; // "Easy", "Normal", "Hard"
        private readonly string[] _opKeys = new string[] { "rand", "o1rq", "o2rq", "o3rq", "o4rq", "o5rq", "o6rq" };
        private readonly string[] _opWeapon = new string[] { "rand", "o1sw", "o2sw", "o3sw" };

        private ArrowOption _setLogicDifficulty;
        private ArrowOption _setRequiredKeys;
        private ArrowOption _setStartingWeapon;

        private ToggleOption _setShuffleLongQuests;
        private ToggleOption _setShuffleShops;

        private TextOption _setSeed;

        private readonly List<Clickable> _clickables = new();
    }
}
