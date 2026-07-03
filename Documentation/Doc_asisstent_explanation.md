# Uživatelská dokumentace asistenční aplikace

Tato kapitola slouží jako průvodce obsluhou webové asistenční aplikace z pohledu lékaře či operátora. Popisuje kompletní životní cyklus tréninkového sezení od úvodní konfigurace až po finální export nasbíraných telemetrických dat.

## 1. Příprava a spuštění sezení
Po úspěšném přihlášení do systému je operátor přesměrován na hlavní řídicí panel. Pro zahájení práce je nutné v horním ovládacím panelu nejprve vybrat konkrétního pacienta (např. 01, 02) a následně zvolit dostupné MR zařízení (headset Meta Quest 3). 

Dokud nejsou tyto parametry vybrány, systém z bezpečnostních důvodů blokuje spuštění tréninku a upozorňuje uživatele textovou výzvou. Jakmile je zařízení úspěšně připojeno, aplikace automaticky naváže spojení a iniciuje živý video stream z pohledu první osoby (POV) pacienta.

![Výchozí stav aplikace s rozbaleným menu pro výběr pacienta a čekáním na zařízení.](images/1.png)

## 2. Konfigurace a průběh tréninku
Veškerá konfigurace probíhá přímo v levém ovládacím panelu aktivního sezení. Operátor má k dispozici výběr z dostupných kognitivních modulů (např. *Location Recall*, *Attention Tracking*). Pro vybranou hru následně nastaví požadovanou úroveň obtížnosti a počet iterací pomocí posuvníků. 

Tréninkové sezení se následně odešle do brýlí pacienta stisknutím modrého tlačítka **DEPLOY TO HEADSET**. Během tréninku operátor primárně sleduje video stream, aby měl přehled o reakcích seniora v reálném čase. Aplikace také zobrazuje aktuální stav připojení (např. zelený indikátor *Connected* a *STREAMING*).

![Aktivní sezení s běžícím video streamem, možností konfigurace a živými logy.](images/2.png)

## 3. Monitorování telemetrie a logy
Kromě vizuální kontroly přes video jsou klíčovým prvkem asistenční aplikace živé logy, které se dynamicky vypisují v levém krajním panelu rozhraní. Tyto logy obsahují detailní telemetrii o průběhu hry, interakcích pacienta a případných chybách.

Při sledování logů je pro operátora stěžejní analyzovat především události a chybové stavy (např. atribut `event_type`), které okamžitě indikují, co se v aplikaci děje. 

Typickým příkladem z praxe, který je k vidění i na snímku aktivního sezení výše, je chybová událost `error_no_space`. Tento log operátora upozorňuje, že systém v daném prostředí detekoval nedostatek volného fyzického prostoru pro bezpečné vygenerování (spawn) herního objektu. V takové situaci musí operátor zasáhnout a instruovat seniora, aby se například mírně přesunul, otočil nebo si udělal kolem sebe více místa.