﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using DG.Tweening;

public enum AIExpression
{
    None,
    Happy,
    Neutral,
    Sad,
    Excited,
    Shocked
}

[Serializable]
public class ColourTag
{
    [SerializeField] private char openingTag;
    [SerializeField] private char closingTag;
    [SerializeField] private Color colour;
    private string colourName;

    public char OpeningTag { get => openingTag; }
    public char ClosingTag { get => closingTag; }
    public Color Colour { get => colour; }
    public string ColourName { get => colourName; set => colourName = value; }
}

[Serializable]
public class ExpressionDialoguePair
{
    //Serialized Fields
    [SerializeField] private AIExpression aiExpression = AIExpression.Neutral;
    [SerializeField, TextArea(15, 20)] private string dialogue;

    //Public Properties
    public AIExpression AIExpression { get => aiExpression; }
    public string Dialogue { get => dialogue; }
}

[Serializable]
public class DialogueSet
{
    //Serialized Fields
    [SerializeField] private string key;
    [SerializeField] private List<ExpressionDialoguePair> expressionDialoguePairs;

    //Public Properties
    public string Key { get => key; }
    public List<ExpressionDialoguePair> ExpressionDialoguePairs { get => expressionDialoguePairs; }
}

/// <summary>
/// Allows other classes to submit dialogue for the dialogue box to display.
/// </summary>
public class DialogueBox : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------

    //Serialized Fields----------------------------------------------------------------------------

    [Header("Text Box")]
    [SerializeField] private TextMeshProUGUI textBox;
    [SerializeField] private Image aiImage;

    [Header("Tween/Lerp Speeds")]
    [SerializeField] float popUpSpeed = 0.5f;
    [SerializeField] private int lerpTextInterval = 3;

    [Header("Available Expressions")]
    [SerializeField] private AIExpression currentExpression;
    [SerializeField] private Sprite aiHappy;
    [SerializeField] private Sprite aiNeutral;
    [SerializeField] private Sprite aiSad;
    [SerializeField] private Sprite aiExcited;
    [SerializeField] private Sprite aiShocked;

    [Header("Images")]
    [SerializeField] private Image completeArrow;
    [SerializeField] private Image continueArrow;

    [Header("Dialogue")]
    [SerializeField] private List<ColourTag> colourTags;
    [SerializeField] private List<DialogueSet> dialogue;

    [Header("Objective Buttons")]
    [SerializeField] private Image countdown;
    [SerializeField] private Image objButton;

    //Non-Serialized Fields------------------------------------------------------------------------

    [Header("Testing")]

    private Vector2 originalRectTransformPosition;
    private RectTransform dialogueRectTransform;
    private Vector2 arrowInitialPosition;
    [SerializeField] private bool activated = false;
    [SerializeField] private bool clickable = false;

    private Dictionary<string, List<ExpressionDialoguePair>> dialogueDictionary = new Dictionary<string, List<ExpressionDialoguePair>>();
    private string currentDialogueKey = "";
    private string lastDialogueKey = "";
    private int dialogueIndex = 0;
    private string currentText = "";
    private string pendingText = "";
    private string pendingColouredText = "";
    //private int lerpTextMinIndex = 0;
    private int lerpTextMaxIndex = 0;

    private ColourTag colourTag = null;
    private bool lerpFinished = true;

    private string nextDialogueKey = "";
    private float nextInvokeDelay = 0f;
    private bool deactivationSubmitted = false;
    private bool nextDialogueSetReady = false;

    private bool dialogueRead = false;
    private bool deactivating = false;

    private RectTransform continueArrowTransform;
    private RectTransform completeArrowTransform;

    private float dialogueTimer = 0;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Basic Public Properties----------------------------------------------------------------------

    /// <summary>
    /// Has dialogue been submitted to be displayed and is the dialogue box either moving on-screen or on-screen displaying the submitted dialogue?
    /// </summary>
    public bool Activated { get => activated; }

    /// <summary>
    /// Gets the key of the dialogue set that is currently being displayed.
    /// </summary>
    public string CurrentDialogueSet { get => currentDialogueKey; }

    /// <summary>
    /// Is the dialogue box in the process of moving off-screen?
    /// </summary>
    public bool Deactivating { get => deactivating; }

    /// <summary>
    /// The index of the currently displayed line of dialogue within the current dialogue set.
    /// </summary>
    public int DialogueIndex { get => dialogueIndex; }

    /// <summary>
    /// Gets the amount of time the current dialogue set has been displayed.
    /// </summary>
    public float DialogueTimer { get => dialogueTimer; }

    /// <summary>
    /// Gets the key of the dialogue set submitted before the currently displayed dialogue set.
    /// </summary>
    public string LastDialogueSet { get => lastDialogueKey; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    private void Awake()
    {
        aiImage.sprite = aiNeutral;
        arrowInitialPosition = completeArrow.GetComponent<RectTransform>().anchoredPosition;

        foreach (ColourTag c in colourTags)
        {
            c.ColourName = $"#{ColorUtility.ToHtmlStringRGB(c.Colour)}";
        }
        
        //TODO: looks like aiImage.sprite is already set manually just above. Therefore the if statement here is unnecessary.
        if (aiImage.sprite == aiHappy)
        {
            currentExpression = AIExpression.Happy;
        }
        else if (aiImage.sprite == aiNeutral)
        {
            currentExpression = AIExpression.Neutral;
        }
        else if (aiImage.sprite == aiSad)
        {
            currentExpression = AIExpression.Sad;
        }
        else if (aiImage.sprite == aiExcited)
        {
            currentExpression = AIExpression.Excited;
        }
        else if (aiImage.sprite == aiShocked)
        {
            currentExpression = AIExpression.Shocked;
        }

        foreach (DialogueSet ds in dialogue)
        {
            if (dialogueDictionary.ContainsKey(ds.Key))
            {
                Debug.Log($"DialogueBox has multiple dialogue sets with the dialogue key {ds.Key}. Each dialogue key should be unique.");
            }
            else
            {
                dialogueDictionary[ds.Key] = ds.ExpressionDialoguePairs;
            }
        }
    }

    /// <summary>
    /// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
    /// Start() runs after Awake().
    /// </summary>
    private void Start()
    {
        //WorldController.Instance.Inputs.InputMap.ProceedDialogue.performed += ctx => RegisterDialogueRead();

        continueArrowTransform = continueArrow.GetComponent<RectTransform>();
        completeArrowTransform = completeArrow.GetComponent<RectTransform>();
    }

    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update() is run every frame.
    /// </summary>
    private void Update()
    {
        if (clickable)
        {
            dialogueTimer += Time.deltaTime;
            UpdateArrow();
        }

        UpdateDialogueBoxState();
        LerpDialogue();
    }

    //Recurring Methods (Update())-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Updates the arrow prompting the player to continue / finish reading the dialogue.
    /// </summary>
    private void UpdateArrow()
    {
        if (dialogueDictionary.ContainsKey(currentDialogueKey))
        {
            if (!continueArrow.enabled && dialogueIndex < dialogueDictionary[currentDialogueKey].Count)
            {
                if (completeArrow.enabled)
                {
                    DOTween.Kill(completeArrowTransform);
                    completeArrowTransform.anchoredPosition = arrowInitialPosition;
                    completeArrow.enabled = false;
                }

                continueArrow.enabled = true;
                continueArrowTransform.DOAnchorPosX(arrowInitialPosition.x + 5, 0.3f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
            else if (!completeArrow.enabled && dialogueIndex == dialogueDictionary[currentDialogueKey].Count)
            {
                if (continueArrow.enabled)
                {
                    DOTween.Kill(continueArrowTransform);
                    continueArrowTransform.anchoredPosition = arrowInitialPosition;
                    continueArrow.enabled = false;
                }

                completeArrow.enabled = true;
                completeArrowTransform.DOAnchorPosY(arrowInitialPosition.y - 5, 0.3f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
        }
        else
        {
            Debug.Log($"Cannot display dialogue set {currentDialogueKey}; Dialogue Key {currentDialogueKey} doesn't exist.");
        }
    }

    /// <summary>
    /// Progresses the dialogue box's state depending on user input, dialogue submissions, etc. and reports when all of the dialogue has been read.
    /// </summary>
    private void UpdateDialogueBoxState()
    {
        //Activates the dialogue box if dialogue has been submitted to be displayed.
        if (nextDialogueKey != "" && !deactivating)
        {
            if (!activated)
            {
                ActivateDialogueBox(nextDialogueKey, nextInvokeDelay);

                nextDialogueKey = "";
                nextInvokeDelay = 0f;
            }
            else
            {
                ChangeDialogue(nextDialogueKey);

                nextDialogueKey = "";
                nextInvokeDelay = 0f;
            }
        }
        else if (deactivationSubmitted && activated)
        {
            lastDialogueKey = currentDialogueKey;
            currentDialogueKey = "";
            clickable = false;
            DeactivateDialogueBox();
        }

        deactivationSubmitted = false;

        //Triggers the lerping of the next line of dialogue onto the screen, or informs the classes that use the dialogue box that all of the dialogue in the current dialogue set has been read.
        if (nextDialogueSetReady && dialogueDictionary.ContainsKey(currentDialogueKey) && dialogueIndex < dialogueDictionary[currentDialogueKey].Count)
        {
            DisplayNext();
            nextDialogueSetReady = false;
        }
        else
        {
            if (nextDialogueSetReady && (dialogueDictionary.ContainsKey(currentDialogueKey) || dialogueIndex >= dialogueDictionary[currentDialogueKey].Count))
            {
                nextDialogueSetReady = false;

                if (dialogueDictionary.ContainsKey(currentDialogueKey))
                {
                    Debug.Log($"Warning: nextDialogueSetReady was true, but dialogue key {currentDialogueKey} doesn't exist in dialogueDictionary.");
                }
                else
                {
                    Debug.Log($"Warning: nextDialogueSetReady was true and dialogue key {currentDialogueKey} exists, but dialogueIndex {dialogueIndex} is an invalid index, given dialogueDictionary[{currentDialogueKey}].Count ({dialogueDictionary[currentDialogueKey].Count}).");
                }
            }

            if (clickable && dialogueRead)
            {
                RegisterDialogueRead();
            }
        }
    }

    /// <summary>
    /// Lerps dialogue on-to the screen
    /// </summary>
    private void LerpDialogue()
    {
        if (!lerpFinished)
        {
            pendingText = "";
            pendingColouredText = "";
            colourTag = null;

            foreach (char c in currentText.Substring(0, lerpTextMaxIndex))
            {
                if (colourTag != null)
                {
                    if (c == colourTag.ClosingTag)
                    {
                        pendingText += $"<color={colourTag.ColourName}><b>{pendingColouredText}</b></color>";
                        pendingColouredText = "";
                        colourTag = null;
                    }
                    else
                    {
                        pendingColouredText += c;
                    }
                }
                else
                {
                    foreach (ColourTag t in colourTags)
                    {
                        if (c == t.OpeningTag)
                        {
                            colourTag = t;
                            break;
                        }
                    }

                    if (colourTag == null)
                    {
                        pendingText += c;
                    }
                }
            }

            if (colourTag != null)
            {
                pendingText += $"<color={colourTag.ColourName}><b>{pendingColouredText}</b></color>";
            }

            textBox.text = pendingText;

            if (lerpTextMaxIndex < currentText.Length)// - 1)
            {
                lerpTextMaxIndex = Mathf.Min(lerpTextMaxIndex + lerpTextInterval, currentText.Length);// - 1);
            }
            else
            {
                lerpFinished = true;
            }
        }
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    //Change over the dialogue list----------------------------------------------------------------

    /// <summary>
    /// Submit a dialogue set for the dialogue box to display during the next update.
    /// </summary>
    /// <param name="key">The key of the dialogue set to be displayed.</param>
    /// <param name="invokeDelay">How long the dialogue box should wait to display the new dialogue set.</param>
    public void SubmitDialogueSet(string key, float invokeDelay)
    {
        if (dialogueDictionary.ContainsKey(key))
        {
            nextDialogueKey = key;
            nextInvokeDelay = invokeDelay;
        }
        else
        {
            Debug.Log("Dialogue key '" + key + "' is invalid.");
        }
    }

    /// <summary>
    /// Activates the dialogue box, prompting it to appear on-screen.
    /// </summary>
    /// <param name="key">The key of the dialogue set to be displayed.</param>
    /// <param name="invokeDelay">How long the dialogue box should wait to display the new dialogue set.</param>
    private void ActivateDialogueBox(string key, float invokeDelay)
    {
        if (dialogueDictionary.ContainsKey(key) && dialogueDictionary[key].Count > 0)
        {
            //Caches required tweening information for performance saving
            //TODO: see if this can be cached once to improve performance
            dialogueRectTransform = GetComponent<RectTransform>();
            originalRectTransformPosition = GetComponent<RectTransform>().anchoredPosition;

            dialogueIndex = 0;
            lastDialogueKey = currentDialogueKey == "" ? lastDialogueKey : currentDialogueKey;
            currentDialogueKey = key;

            activated = true;
            nextDialogueSetReady = false;

            Invoke(nameof(ShowDialogueBox), invokeDelay);
        }
        else
        {
            Debug.LogError($"dialogueDictionary[{key}] contains no dialogue set for DialogueBox to display.");
        }
    }

    /// <summary>
    /// Displays the dialogue box and the submitted dialogue once the invocation delay has elapsed.
    /// </summary>
    private void ShowDialogueBox()
    {
        nextDialogueSetReady = true;

        countdown.rectTransform.DOAnchorPosY(-18 + 150, popUpSpeed).SetEase(Ease.OutBack);
        objButton.rectTransform.DOAnchorPosY(-18 + 150, popUpSpeed).SetEase(Ease.OutBack);
        dialogueRectTransform.DOAnchorPosY(-18, popUpSpeed).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(
            delegate
            {
                clickable = true;
                dialogueTimer = 0;
            });
    }

    /// <summary>
    /// Changes over the dialogue set to display when the dialogue box is already active.
    /// </summary>
    /// <param name="key">The key of the dialogue set to be displayed.</param>
    private void ChangeDialogue(string key)
    {
        if (dialogueDictionary.ContainsKey(key) && dialogueDictionary[key].Count > 0)
        {
            lastDialogueKey = currentDialogueKey;
            currentDialogueKey = key;
            dialogueIndex = 0;
            LerpNext();
        }
        else
        {
            Debug.LogError($"dialogueDictionary[{key}] contains no dialogue set for DialogueBox to display.");
        }
    }

    //Display the next set of content--------------------------------------------------------------

    /// <summary>
    /// Displays the next line of dialogue in one hit.
    /// </summary>
    private void DisplayNext()
    {
        lerpFinished = false;
        textBox.text = "";
        currentText = dialogueDictionary[currentDialogueKey][dialogueIndex].Dialogue;

        if (dialogueDictionary[currentDialogueKey][dialogueIndex].AIExpression != currentExpression)
        {
            ChangeAIExpression(dialogueDictionary[currentDialogueKey][dialogueIndex].AIExpression);
        }
        //else
        //{
        //    Debug.Log($"Keeping dialogue expression as {currentExpression}");
        //}

        dialogueIndex++;
        lerpTextMaxIndex = currentText.Length - 1;
        dialogueTimer = 0;
    }

    /// <summary>
    /// Displays the next line of dialogue by lerping it into the dialogue box
    /// </summary>
    private void LerpNext()
    {
        lerpFinished = false;
        textBox.text = "";
        currentText = dialogueDictionary[currentDialogueKey][dialogueIndex].Dialogue;

        if (dialogueDictionary[currentDialogueKey][dialogueIndex].AIExpression != currentExpression)
        {
            ChangeAIExpression(dialogueDictionary[currentDialogueKey][dialogueIndex].AIExpression);
        }
        //else
        //{
        //    Debug.Log($"Keeping dialogue expression as {currentExpression}");
        //}

        dialogueIndex++;
        lerpTextMaxIndex = 0;
        dialogueTimer = 0;
    }

    /// <summary>
    /// Updates the AI sprite.
    /// </summary>
    /// <param name="expression">The expression that the AI should have. The enum value corresponds to a matching sprite.</param>
    private void ChangeAIExpression(AIExpression expression)
    {
        //Debug.Log($"Changing AIExpression from {currentExpression} to {expression}");
        currentExpression = expression;

        switch (expression)
        {
            case AIExpression.Happy:
                aiImage.sprite = aiHappy;
                break;
            case AIExpression.Neutral:
                aiImage.sprite = aiNeutral;
                break;
            case AIExpression.Sad:
                aiImage.sprite = aiSad;
                break;
            case AIExpression.Excited:
                aiImage.sprite = aiExcited;
                break;
            case AIExpression.Shocked:
                aiImage.sprite = aiShocked;
                break;
        }

        //Debug.Log($"AIExpression is now {currentExpression}");
    }

    //Progress / Finish Dialogue-------------------------------------------------------------------

    /// <summary>
    /// Called by OnClick to register that the player has read the currently displayed dialogue
    /// </summary>
    public void RegisterDialogueRead()
    {
        if (clickable)
        {
            DOTween.Kill(continueArrowTransform);
            continueArrowTransform.anchoredPosition = arrowInitialPosition;
            continueArrow.enabled = false;

            DOTween.Kill(completeArrowTransform);
            completeArrowTransform.anchoredPosition = arrowInitialPosition;
            completeArrow.enabled = false;

            if (dialogueIndex < dialogueDictionary[currentDialogueKey].Count)
            {
                LerpNext();
            }
            else if (activated)
            {
                lastDialogueKey = currentDialogueKey;
                currentDialogueKey = "";
                clickable = false;
                DeactivateDialogueBox();
            }
        }
    }

    /// <summary>
    /// Tweens the dialogue box off the screen.
    /// </summary>
    private void DeactivateDialogueBox()
    {
        dialogueTimer = 0;
        deactivating = true;
        countdown.rectTransform.DOAnchorPosY(20, popUpSpeed).SetEase(Ease.InBack);
        objButton.rectTransform.DOAnchorPosY(20, popUpSpeed).SetEase(Ease.InBack);
        dialogueRectTransform.DOAnchorPosY(originalRectTransformPosition.y, popUpSpeed).SetEase(Ease.InBack).SetUpdate(true).OnComplete(
            delegate
            {
                //Reset position after tweening
                textBox.text = "";
                deactivating = false;

                if (TutorialController.Instance.Stage != TutorialStage.Finished)
                {
                    TutorialController.Instance.RegisterDialogueRead();
                }
                else
                {
                    ObjectiveController.Instance.RegisterDialogueRead();
                }

                activated = false;
            });
    }

    /// <summary>
    /// Lets other classes call for the dialogue box to be deactivated.
    /// </summary>
    public void SubmitDeactivation()
    {
        deactivationSubmitted = true;
    }
}
