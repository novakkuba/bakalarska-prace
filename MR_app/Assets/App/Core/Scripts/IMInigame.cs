using UnityEngine;

/// <summary>
/// Standardizované rozhraní (kontrakt) pro všechny minihry v systému.
/// Definice těchto metod zaručuje, že MinigameController může jednotným způsobem spouštět a ukončovat jakýkoliv herní modul bez ohledu na jeho vnitřní logiku.
/// </summary>

public interface IMiniGame
{
    
    void StartGame(string configJson);

    void StopGame();
}