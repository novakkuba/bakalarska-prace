# Dokumentace nasazení systému 

Tento dokument obsahuje stručný návod k nasazení kompletní infrastruktury systému, která se skládá ze serverové části (backend, frontend, databáze) a klientské MR aplikace pro Meta Quest 3.

## Předpoklady 
Před začátkem nasazení se ujistěte, že máte k dispozici:
* Nainstalovaný **Docker**.
* Vývojové prostředí **Unity** (s modulem Android Build Support).
* Headset **Meta Quest 3** připojený k počítači.
* Přístupný externí **MQTT broker**. *(Pokud nemáte k dispozici vlastní externí broker, je nutné si jej předem zprovoznit, např. přes cloudové služby HiveMQ nebo lokální Mosquitto).*
* **Společná lokální síť:** Pro správné fungování video streamu (WebRTC) musí být headset i počítač s asistenční aplikací připojeny do **stejné lokální sítě (stejná Wi-Fi)**.

---

## Část 1: Nasazení serverové části (Docker)

Serverová architektura je plně kontejnerizována, instalaci závislostí a prostředí řeší automaticky Docker.

**1. Konfigurace prostředí (.env)**
Ve složce s projektem vytvořte soubor `.env` na základě přiložené předlohy `.env-template` a doplňte do něj vaše vlastní přístupové údaje k databázi a MQTT brokeru.

**2. Spuštění kontejnerů**
Otevřete příkazovou řádku ve složce projektu a spusťte příkaz:
```bash
docker-compose up -d --build
```
*Tento příkaz automaticky sestaví a spustí backend, frontend i databázi. Backend logicky vyčká na inicializaci databázového stroje.*

---

## Část 2: Nasazení klientské aplikace (Unity)

Klientská aplikace je nasazována přímo z vývojového prostředí.

**1. Nastavení MQTT v Unity**
Otevřete projekt klientské aplikace v prostředí Unity. Přejděte do složky `data/` v modulu MQTT, kde se nachází šablonový soubor s konfigurací. Tento soubor v Unity zduplikujte (Ctrl+D) nebo zkopírujte, odstraňte z jeho názvu `_example` a v Inspectoru vyplňte vaše vlastní připojovací parametry (IP brokera, port, login a heslo). 
**Důležité:** Nezasahujte do prefixů, prefix `unitymap` musí zůstat zachován pro správné směrování zpráv!

**2. Build do zařízení**
Ujistěte se, že máte headset Meta Quest 3 připojený k PC a v Unity je zvolena cílová platforma Android. Následně v menu zvolte `File -> Build and Run`. Aplikace se zkompiluje a automaticky spustí v brýlích.

