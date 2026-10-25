# eFinanzer Backend

## Local Development

### Backend starten und Secrets setzen
Zum lokalen starten müssen die notwendigen Secrets angegeben werden. Dafür kann die appsettings.TEMPLATE.secrets.json Datei kopiert werden und als appsettings.Development.secrets.json Datei erstellt werden. Diese Secret Dateien gehören NICHT in die Versionierung. In dieser Datei werden Secrets wie die Verbindung zur DB oder Keycloak eingefügt. Im Folgenden wie die Secrets erlangt werden:
- <b>eFinanzer DB ConnectionString</b> - Format: ``Host=[host];Port=[port];Database=efinanzer;Username=[username];Password=[password]``. Wird das Backend mit den Containern aus src/local_dev verknüpft, entspricht Host=localhost, Port=5432 und username und password den gesetzten Variablen aus dem docker-compose.yml (efinanzer und efinanzer)
- <b>Keycloak Secret</b> - Dieses Secret wird für die Authentifizierung des Backends beim Aufruft der Keycloak API benötigt. Es muss aus dem Keycloak FE ausgelesen werden. Folgende Schritte:
  - Öffnen von Keycloak (standardmäßig localhost:5532)
  - Anmelden mit Admin Anmeldedaten (im docker-compose.yml hinterlegt, standardmäßig admin und admin)
  - Im Select oben links den realm wechseln, von ``master`` auf ``efinanzer``
  - Auf Clients klicken, den Client ``efinanzer-backend`` aus der Liste auswählen
  - Oben rechts auf ``Action`` und dann ``Download adapter config`` klicken
  - Den secret string bei credentials.secret kopieren und in die Appsettings.Development.secrets.json Datei einfügen

Die eFinanzer DB ist beim erstmaligen starten noch unkonfiguriert und muss erst mit dem Datenschema initialisiert werden. Dazu im folgenden Kapitel mehr.

Diese Schritte sind nur bei der Erstkonfiguration nötig, da die Daten anschließend lokal gespeichert werden.



### Datenbank Migrationen

Eine Datenbank Migration ist eine Änderung am Datenschema (z.B. Hinzufügen einer neuen Spalte/Tabelle, Löschen einer Spalte, Ändern eines Datentyps einer Spalte, Hinzufügen eines Indexes...). Das Datenbankschema ist in diesem Projekt Code-first mittels dotnet EntityFramework Core. Die DB darft also nicht direkt durch eigenes SQL geändert werden. Stattdessen werden die Änderungen an den Klassen vorgenommen (Klassen in Common/Database/Models entsprechen weitestgehend einer DB Tabelle) und anschließend programmatisch eine DB Migration erstellt. Dafür wird der Befehl ``dotnet ef migrations add [MigrationsName]`` verwendet (muss src/eFinanzer/Web Ordner aufgerufen werden), welcher eine Migrations Klasse im Migrations/ Ordner erstellt. Mittel `dotnet ef migrations script` kann anschließend SQL für die Migration(en) generiert werden. Standardmäßig wird das SQL für alle bestehenden Migrationen erstellt. Verwendet werden sollte bei einer neuen Migration nur der letzte Teil des SQL (zwischen START TRANSACTION; und COMMIT;)

#### Initiale Migration

Um mit dem lokalen Entwickeln zu starten, muss das DB Schema auf den aktuellen Stand gebracht werden. Dafür wird der oben genannte ``dotnet ef migrations script`` Befehl aufgerufen und das gesamte SQL bei der lokalen DB ausgeführt.