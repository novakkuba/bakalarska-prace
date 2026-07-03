using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Statická utilita pro bezpečné generování unikátního hardwarového identifikátoru konkrétního VR headsetu.
/// Primárně využívá nativní Unity Device ID, při selhání sestavuje záložní SHA-256 hash z fyzických specifikací zařízení.
/// </summary>

public static class DeviceUniqueIdGenerator
{
    private static string cachedUniqueId = null;

    /// <summary>
    /// Generuje unik�tn� ID na z�klad� informac� o za��zen�
    /// </summary>
    public static string GenerateUniqueId()
    {
        // 1. OCHRANA BĚHEM HRY (Zabránění zbytečnému výpočtu)
        if (!string.IsNullOrEmpty(cachedUniqueId))
            return cachedUniqueId;

        // 2. OCHRANA PO RESTARTU (Zkusíme najít trvale uložené ID)
        // Pokud jsme už někdy ID vygenerovali a uložili, prostě ho vezmeme a končíme!
        if (PlayerPrefs.HasKey("VR_HARDWARE_ID"))
        {
            cachedUniqueId = PlayerPrefs.GetString("VR_HARDWARE_ID");
            return cachedUniqueId;
        }

        // 3. GENERACE (Sem kód dojde jen při úplně prvním spuštění aplikace v životě)
        try
        {
            var deviceId = SystemInfo.deviceUniqueIdentifier;
            if (!string.IsNullOrEmpty(deviceId) && deviceId != "n/a")
            {
                cachedUniqueId = CreateHashedId(deviceId);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to get device unique identifier: {ex.Message}");
        }

        // Pokud selhalo nativní ID, použijeme fallback
        if (string.IsNullOrEmpty(cachedUniqueId))
        {
            var fallbackString = CreateFallbackIdentifier();
            cachedUniqueId = CreateHashedId(fallbackString);
        }

        // 4. ULOŽENÍ NA VĚČNÉ ČASY!
        // Uložíme vygenerované ID do paměti brýlí, takže příště už ho nebudeme počítat
        PlayerPrefs.SetString("VR_HARDWARE_ID", cachedUniqueId);
        PlayerPrefs.Save();

        Debug.Log($"Generated and SAVED device unique ID: {cachedUniqueId}");
        return cachedUniqueId;
    }

    private static string CreateFallbackIdentifier()
    {
        var sb = new StringBuilder();
        
        // Z�kladn� info o za��zen�
        sb.Append(SystemInfo.deviceName ?? "unknown");
        sb.Append("-");
        sb.Append(SystemInfo.deviceModel ?? "unknown");
        sb.Append("-");
        sb.Append(SystemInfo.operatingSystem ?? "unknown");
        sb.Append("-");
        sb.Append(SystemInfo.processorType ?? "unknown");
        sb.Append("-");
        sb.Append(SystemInfo.graphicsDeviceName ?? "unknown");
        sb.Append("-");
        sb.Append(SystemInfo.systemMemorySize);
        sb.Append("-");
        sb.Append(Application.companyName ?? "Unity");
        sb.Append("-");
        sb.Append(Application.productName ?? "Game");

        return sb.ToString();
    }

    private static string CreateHashedId(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            
            // Zkr�tit na prvn�ch 12 znak� pro �itelnost
            return hash.Substring(0, 12);
        }
    }

    /// <summary>
    /// Generuje �iteln� ID kombinuj�c� prefix s device hash
    /// </summary>
    public static string GenerateReadableId()
    {
        string prefix = "User";
        var uniqueHash = GenerateUniqueId();
        return $"{prefix}_{uniqueHash}";
    }

    /// <summary>
    /// Vyma�e cache - u�ite�n� pro testov�n�
    /// </summary>
    public static void ClearCache()
    {
        cachedUniqueId = null;
    }

    /// <summary>
    /// Z�sk� informace o za��zen� pro debug
    /// </summary>
    public static string GetDeviceInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Device Name: {SystemInfo.deviceName}");
        sb.AppendLine($"Device Model: {SystemInfo.deviceModel}");
        sb.AppendLine($"Device Type: {SystemInfo.deviceType}");
        sb.AppendLine($"Operating System: {SystemInfo.operatingSystem}");
        sb.AppendLine($"Processor: {SystemInfo.processorType}");
        sb.AppendLine($"Memory: {SystemInfo.systemMemorySize} MB");
        sb.AppendLine($"Graphics: {SystemInfo.graphicsDeviceName}");
        sb.AppendLine($"Unity Device ID: {SystemInfo.deviceUniqueIdentifier}");
        sb.AppendLine($"Generated Unique ID: {GenerateUniqueId()}");
        
        return sb.ToString();
    }
}