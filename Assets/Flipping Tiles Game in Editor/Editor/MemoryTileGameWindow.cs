using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Unity.EditorCoroutines.Editor;

namespace FlippingTiles
{
    public class MemoryTileGameWindow : EditorWindow
    {
        private string[] colourStringsInUse =
        {
            "red", "orange", "yellow", "green", "blue", "indigo", "magenta", "cyan", "dark-green"
        };

        private Button previouslyOpenedTile;
        private bool isOpeningAllowed = true;
        private int unmatchedTilesLeft = 36;
        private int numFlips = 0;
        private float timeSpent = 0;
        private bool hasRoundStarted = false;
        private EditorCoroutine timerCoroutine;

        [MenuItem("MemoryTile/Open")]
        public static void OpenMemoryTileGameWindow()
        {
            var window = GetWindow<MemoryTileGameWindow>();
            window.titleContent = new GUIContent("Memory Tile");
        }

        private void OnEnable()
        {
            // creates a List containing 4 of each possible colours
            var colourStrings = new List<string>();

            foreach (var colourString in colourStringsInUse)
            {
                for (int i = 0; i < 4; i++)
                {
                    colourStrings.Add(colourString);
                }
            }

            // shuffle list
            var rand = new System.Random();
            colourStrings = colourStrings.OrderBy(str => rand.Next()).ToList();

            var gameWindowTreeAsset = Resources.Load<VisualTreeAsset>("MemoryTileGameWindow");
            rootVisualElement.Add(gameWindowTreeAsset.Instantiate());

            for (int i = 0; i < 6; i++)
            {
                var tileBoxTreeAsset = Resources.Load<VisualTreeAsset>("TileBox");
                rootVisualElement.Add(tileBoxTreeAsset.Instantiate());
            }

            var tileBoxes = rootVisualElement.Query<VisualElement>(className: "tile-box");
            tileBoxes.ForEach(
                ve =>
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var tileTreeAsset = Resources.Load<VisualTreeAsset>("Tile");
                        ve.Add(tileTreeAsset.Instantiate());
                    }
                }
            );

            // all tile boxes will need to do the following
            rootVisualElement.Query<Button>(name: "TileButton").ForEach(
                btn =>
                {
                    // indicates that it isn't open
                    btn.text = "?";

                    // randomly assigning a colour
                    btn.Q<VisualElement>(className: "tile-colour-visual").ToggleInClassList(colourStrings[0]);
                    btn.Q<VisualElement>(name: "TileColour").ToggleInClassList(colourStrings[0]);

                    // so our next button can access the next colour string
                    colourStrings.RemoveAt(0);

                    // hide the visual until we need to display it
                    btn.Q<VisualElement>(className: "tile-colour-visual").style.display = DisplayStyle.None;

                    // every button needs to perform an action when clicked
                    btn.clicked += () => OnTileButtonClicked(btn);
                }
            );
        }

        private void OnTileButtonClicked(Button tileButton)
        {
            if (!isOpeningAllowed)
            {
                return;
            }

            if (!hasRoundStarted)
            {
                hasRoundStarted = true;
                timerCoroutine = this.StartCoroutine(TimerCoroutine());
            }

            if (previouslyOpenedTile == tileButton)
            {
                // close the tile
                var tileButtonVisual = tileButton.Q<VisualElement>(className: "tile-colour-visual");
                tileButtonVisual.style.display = DisplayStyle.None;
                previouslyOpenedTile = null;
            }
            else if (previouslyOpenedTile == null)
            {
                previouslyOpenedTile = tileButton;

                // open the tile
                var tileButtonVisual = tileButton.Q<VisualElement>(className: "tile-colour-visual");
                tileButtonVisual.style.display = DisplayStyle.Flex;

                numFlips++;
                UpdateFlipCounterUI(numFlips);
            }
            else if (previouslyOpenedTile != null)
            {
                // open the tile
                var tileButtonVisual = tileButton.Q<VisualElement>(className: "tile-colour-visual");
                tileButtonVisual.style.display = DisplayStyle.Flex;
                numFlips++;
                UpdateFlipCounterUI(numFlips);

                // check for a match
                string previousTileColourString = previouslyOpenedTile.Q<VisualElement>(name: "TileColour").GetClasses().First();
                string currentTileColourString = tileButton.Q<VisualElement>(name: "TileColour").GetClasses().First();

                if (previousTileColourString == currentTileColourString)
                {
                    // to temporarily disallow opening other tiles
                    isOpeningAllowed = false;

                    this.StartCoroutine(MatchFoundCoroutine(previouslyOpenedTile, tileButton));
                }
                else
                {
                    isOpeningAllowed = false;
                    this.StartCoroutine(NotAMatchCoroutine(previouslyOpenedTile, tileButton));
                }

                previouslyOpenedTile = null;
            }
        }

        private IEnumerator TimerCoroutine()
        {
            while (true)
            {
                yield return new EditorWaitForSeconds(0.1f);
                timeSpent += 0.1f;
                UpdateNumberOfSecondsUI(timeSpent);
            }
        }

        private IEnumerator MatchFoundCoroutine(Button first, Button second)
        {
            // give the player 1 second of time to look
            yield return new EditorWaitForSeconds(1);

            // then we mark both tiles as matched
            first.Q<VisualElement>(className: "tile-colour-visual").style.display = DisplayStyle.None;
            second.Q<VisualElement>(className: "tile-colour-visual").style.display = DisplayStyle.None;

            first.visible = false;
            second.visible = false;

            // we now have 2 less tiles to match
            unmatchedTilesLeft -= 2;

            // and we allow opening of tiles
            isOpeningAllowed = true;

            if (unmatchedTilesLeft <= 0)
            {
                titleContent = new GUIContent("Memory Tile - YOU WIN");
                this.StopCoroutine(timerCoroutine);
            }
        }

        private IEnumerator NotAMatchCoroutine(Button first, Button second)
        {
            // give the player 1 second of time to look
            yield return new EditorWaitForSeconds(1);

            // then we cover those tiles back
            first.Q<VisualElement>(className: "tile-colour-visual").style.display = DisplayStyle.None;
            second.Q<VisualElement>(className: "tile-colour-visual").style.display = DisplayStyle.None;

            // and we allow opening of tiles
            isOpeningAllowed = true;
        }

        private void UpdateFlipCounterUI(int flipCount)
        {
            rootVisualElement.Q<Label>("number-of-flips").text = flipCount.ToString();
        }

        private void UpdateNumberOfSecondsUI(float timeCount)
        {
            rootVisualElement.Q<Label>("number-of-seconds").text = timeSpent.ToString("F1");
        }
    }
}