using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class InGameController : MonoBehaviour
{
    [Header("Menu Pause")]
    public GameObject pauseMenu;

    [Header("Botão Continuar")]
    public Button botaoContinuar;

    [Header("UI HUD")]
    public GameObject healthBarUI;
    public GameObject staminaBarUI;
    public GameObject queijoCounterUI;

    private bool isPaused = false;
    private bool staminaWasActive;
    private AudioController audioController;

    void Start()
    {
        audioController = FindObjectOfType<AudioController>();

        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        LockCursor();

        if (botaoContinuar != null)
        {
            botaoContinuar.onClick.RemoveAllListeners();
            botaoContinuar.onClick.AddListener(Pause);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Vitória/derrota: a partida já acabou, o Esc não pausa nem despausa
            if (GameManager.Instance != null && GameManager.Instance.JogoTerminou)
                return;

            Pause();
        }
    }

    public void Pause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    // 🔥 ABRIR PAUSE
    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);

        if (audioController != null)
            audioController.TocarEfeito(6); // Clique

        // Esconde HUD
        if (healthBarUI != null) healthBarUI.SetActive(false);
        if (staminaBarUI != null)
        {
            staminaWasActive = staminaBarUI.activeSelf;
            staminaBarUI.SetActive(false);
        }
        if (queijoCounterUI != null) queijoCounterUI.SetActive(false);

        UnlockCursor();
    }

    // 🔥 FECHAR PAUSE / CONTINUAR
    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);

        if (audioController != null)
            audioController.TocarEfeito(6); // Confirmação

        // Restaura HUD
        if (healthBarUI != null) healthBarUI.SetActive(true);
        if (staminaBarUI != null) staminaBarUI.SetActive(staminaWasActive);
        if (queijoCounterUI != null) queijoCounterUI.SetActive(true);

        LockCursor();
        InputSystem.ResetHaptics();
    }

    // 🔥 BOTÃO VOLTAR AO MENU PRINCIPAL
    public void MainMenu()
    {
        if (audioController != null)
            audioController.TocarEfeito(6); // Confirmação

        StartCoroutine(VoltarMenuDelay());
    }

    IEnumerator VoltarMenuDelay()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        Time.timeScale = 1f;
        UnlockCursor();
        SceneManager.LoadScene(0); // 0 = índice da cena do Menu
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDisable()
    {
        UnlockCursor();
    }
}