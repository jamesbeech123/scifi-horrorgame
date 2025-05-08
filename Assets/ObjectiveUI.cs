using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    private TextMeshProUGUI objectiveDescriptionText;
    public GameObject objectiveDescription;
    public GameObject objectiveCompletedElement;
    public AudioClip objectiveCompletedSound;
    public AudioClip objectiveSetSound;
    public float fadeDuration = 1f;
    public float displayTime = 3f;  // Time the UI remains visible

    private bool isObjectiveCompleted = false;
    private AudioSource audioSource;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        // Get or add the AudioSource for playing sound effects
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Get or add the CanvasGroup for fading UI elements
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }



        objectiveDescriptionText = objectiveDescription.GetComponent<TextMeshProUGUI>();

        // Initially hide the UI
        canvasGroup.alpha = 0f;
        objectiveCompletedElement.SetActive(false);


        StartCoroutine(StartDelay());
    }

    IEnumerator StartDelay()
    {
        yield return new WaitForSeconds(3f);
        SetObjective("Find and Complete 3 Objectives to open the door");
    }

    public void SetObjective(string description)
    {
        isObjectiveCompleted = false;
        objectiveDescriptionText.text = description;
        objectiveCompletedElement.SetActive(false);

        PlaySetSound();
        StopAllCoroutines();  // Prevent overlapping fades
        StartCoroutine(FadeInAndOut());
    }

    public void CompleteObjective()
    {
        if (!isObjectiveCompleted)
        {
            isObjectiveCompleted = true;
            objectiveCompletedElement.SetActive(true);
            PlayCompletionSound();
            StopAllCoroutines();
            StartCoroutine(FadeOutAfterDelay());
        }
    }

    private void PlayCompletionSound()
    {
        if (audioSource && objectiveCompletedSound)
            audioSource.PlayOneShot(objectiveCompletedSound);
    }

    private void PlaySetSound()
    {
        if (audioSource && objectiveSetSound)
            audioSource.PlayOneShot(objectiveSetSound);
    }

    //Coruotine to animate the objective UI
    private IEnumerator FadeInAndOut()
    {
        yield return StartCoroutine(FadeUI(0f, 1f));  
        yield return new WaitForSeconds(displayTime); 
        yield return StartCoroutine(FadeUI(1f, 0f));  
    }

    //Fades out the objective UI
    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(2f); 
        yield return StartCoroutine(FadeUI(1f, 0f));  
    }

    //Fades in the objective UI
    private IEnumerator FadeUI(float startAlpha, float endAlpha)
    {
        float timeElapsed = 0f;
        while (timeElapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }
}
